using System;
using System.Diagnostics;
using System.Threading;

namespace Illumina.TerminalVelocity.Host
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            while (ProcessArgs(args))
            {
                args = Console.ReadLine().Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            }
        }

        private static bool ProcessArgs(string[] args)
        {
            var options = new DownloadOptions();
            try
            {

                var optionSet = CreateOptions(options);
                optionSet.Parse(args);

                if (options.ShowHelp || (options.Uri == null))
                {
                    optionSet.WriteOptionDescriptions(Console.Out);
                }
                else if (options.HasException)
                {
                    Console.WriteLine(options.Exception.Message);
                }
                else
                {
                    //go get file size
                    GetSizeAndSource(options);
                    LargeFileDownloadParameters parameters = null;
                    if (options.Uri != null && options.FileSize.HasValue && options.FileSize.Value > 0)
                    {
                        parameters = new LargeFileDownloadParameters(options.Uri, options.OutputFile,
                                                                     options.FileSize.Value,
                                                                     options.MaxThreads, options.MaxChunkSize);
                    }


                    if (parameters != null)
                    {
                        Action<string> logger = (message) => Debug.WriteLine(message);
                        var token = new CancellationToken();
                        var watch = new Stopwatch();
                        watch.Start();
                        IAsyncProgress<LargeFileDownloadProgressChangedEventArgs> progress =new AsyncProgress<LargeFileDownloadProgressChangedEventArgs>(Handler);
                        var task = parameters.DownloadAsync(token, progress, logger);
                        task.Wait();
                        watch.Stop();
                       ClearCurrentConsoleLine();

                        Console.WriteLine("done in {0}ms", watch.ElapsedMilliseconds);
                        if (options.IsInteractive)
                        {
                            return true;
                        }
                        Environment.Exit(0);
                       
                    }

                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                if (options.IsInteractive)
                {
                    return true;
                }
                Environment.Exit(-1);
            }
            return !options.ShouldExit;
        }

        private static void ClearCurrentConsoleLine()
        {
            int currentLineCursor = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop);
            for (int i = 0; i < Console.WindowWidth; i++)
                Console.Write(" ");
            Console.SetCursorPosition(0, currentLineCursor);
        }

        private static OptionSet CreateOptions(DownloadOptions options)
        {
            var optionSet = new OptionSet()
                                {
                                    {"h|?|help", "Show available options", o => options.ShowHelp = true},
                                    {"i|interactive", "interactive mode, no exit codes",o => options.IsInteractive = true},
                                    {"fs|filesize|size=", "The expected file size", (long o) => options.FileSize = o},
                                    {
                                        "mc|maxchunksize|maxchunk|chunkschnizzle=", "Max Chunk Size",
                                        (int o) => options.MaxChunkSize = o
                                    },
                                    {
                                        "mt|maxthreads|threadschnizzle=", "Max thread count",
                                        (int o) => options.MaxThreads = o
                                    },
                                    {"q|quit", "Quit", v => options.ShouldExit = true},
                                    {
                                        "f|file=", "the file to be created, will overwrite if exists",
                                        f => options.OutputFile =  f.Replace(@"""", "")
                                    },
                                    {
                                        "<>", "the url to retrieve", f =>
                                                                         {
                                                                             try
                                                                             {
                                                                                 options.Uri = new Uri(f.Replace(@"""", ""));
                                                                             }
                                                                             catch (Exception e)
                                                                             {
                                                                                 options.Exception = e;
                                                                             }
                                                                         }
                                    }
                                };
            return optionSet;
        }

        private static void Handler(LargeFileDownloadProgressChangedEventArgs progress)
        {
            if (progress != null)
            {
                if (progress.IsFailed)
                    Console.WriteLine("download failed:{0}", progress.ReasonForFailure);
                Console.Write("\rpercentComplete {0}%   ", progress.ProgressPercentage);
            }
        }

        private static void GetSizeAndSource(DownloadOptions options)
        {
            var client = new SimpleHttpGetByRangeClient(options.Uri);
            var response = client.Get(options.Uri, 0, 1);

            if (response != null)
            {
                if (response.IsStatusCodeRedirect && !String.IsNullOrWhiteSpace(response.Location))
                {
                    if (response.Location != options.Uri.AbsoluteUri)
                    {
                        options.Uri = new Uri(response.Location);
                        Console.WriteLine("Detected Redirect: " + options.Uri);
                        GetSizeAndSource(options);
                    }
                    else
                    {
                        throw new ArgumentException("Supplied Url has no source");
                    }
                }
                 else if (response.WasSuccessful && response.ContentRangeLength >= 0)
                {
                    options.FileSize = response.ContentRangeLength;
                }
                else
                {
                    throw new Exception("Response was not successful, status code: " + response.StatusCode);
                }

            }
        }
    }


}
