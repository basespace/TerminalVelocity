using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Illumina.TerminalVelocity
{
    public static class Downloader
    {
        public static Task DownloadAsync(this ILargeFileDownloadParameters parameters,
                                         CancellationToken? cancellationToken = null,
                                         IAsyncProgress<LargeFileDownloadProgressChangedEventArgs> progress = null,
                                         Action<string> logger = null)
        {
            CancellationToken ct = (cancellationToken != null) ? cancellationToken.Value : CancellationToken.None;
            Task task = Task.Factory.StartNew(
                () =>
                    {
                        //figure out number of chunks
                        int chunkCount = GetChunkCount(parameters.FileSize, parameters.MaxChunkSize);
                        int numberOfThreads = Math.Min(parameters.MaxThreads, chunkCount);
                        //create the file
                        Stream stream = parameters.GetOutputStream();
                        List<Task> downloadTasks = null;
                        try
                        {
                         
                            int nextChunkToRead = -1;
                            int currentChunk = 0;
                            //next chunk function
                            Func<int> getNextChunk = () =>
                                                         {
                                                             int next = Interlocked.Increment(ref nextChunkToRead);
                                                             if (next < chunkCount)
                                                             {
                                                                 return next;
                                                             }
                                                             return -1;
                                                         };

                            // ReSharper disable AccessToModifiedClosure
                            Func<int, bool> shouldISlow = (int c) => (c - currentChunk) > (numberOfThreads*2);
                            // ReSharper restore AccessToModifiedClosure


                            var contentDic = new ConcurrentDictionary<int, byte[]>(numberOfThreads, 20);
                            var addEvent = new AutoResetEvent(false);

                             downloadTasks = new List<Task>(numberOfThreads);

                            for (int i = 0; i < numberOfThreads; i++)
                            {
                                downloadTasks.Add(CreateDownloadTask(parameters, contentDic, addEvent, getNextChunk,
                                                                     shouldISlow, logger, ct));
                            }
                            //start all the download threads
                            downloadTasks.ForEach(x => x.Start());
                          
                            //start the write loop
                            while (currentChunk < chunkCount && !ct.IsCancellationRequested)
                            {
                                byte[] currentWrittenChunk;
                                if (contentDic.TryRemove(currentChunk, out currentWrittenChunk))
                                {
                                    //retry?
                                    logger(string.Format("writing: {0}", currentChunk));
                                    stream.Write(currentWrittenChunk, 0, currentWrittenChunk.Length);
                                    if (progress != null)
                                    {
                                        progress.Report(new LargeFileDownloadProgressChangedEventArgs((int)Math.Round(100 * (currentChunk/(float)chunkCount), 0), null));
                                    }
                                    currentChunk++;
                                }
                                else
                                {
                                    //are any of the treads alive?
                                    if (downloadTasks.Any(x => x != null && (x.Status == TaskStatus.Running || x.Status == TaskStatus.WaitingToRun)))
                                    {
                                        //wait for something that was added
                                        addEvent.WaitOne(100);
                                        addEvent.Reset();
                                    }
                                    else
                                    {
                                        throw new Exception("All threads were killed");
                                    }
                                }
                            }
                          
                        }
                        finally
                        {
                            //kill all the tasks if exist
                            if (downloadTasks != null)
                            {
                                downloadTasks.ForEach(x =>
                                                          {
                                                              if (x == null) return;
                                                              ExecuteAndSquash(x.Dispose);
                                                          });
                            }
                            if (parameters.AutoCloseStream)
                            {
                                stream.Close();
                            }
                        }
                       
                    },
                ct);

            return task;
        }

      //  internal static LargeFileDownloadProgressChangedEventArgs CreateProgress(int currentChunk, int totalChunks, )

      
        internal static Task CreateDownloadTask(ILargeFileDownloadParameters parameters,
                                                ConcurrentDictionary<int, byte[]> contentDic,
                                                AutoResetEvent reset, Func<int> getNextChunk,
                                                Func<int, bool> shouldSlowDown, Action<string> logger = null,
                                                CancellationToken? cancellation = null,
                                                Func<ILargeFileDownloadParameters, ISimpleHttpGetByRangeClient>
                                                    clientFactory = null)
        {
            cancellation = (cancellation != null) ? cancellation.Value : CancellationToken.None;
            var t = new Task(() =>
                                 {
                                     
                                     clientFactory = clientFactory ?? ((p) => new SimpleHttpGetByRangeClient(p.Uri));
                                     logger = logger ?? ((s) => { });

                                     ISimpleHttpGetByRangeClient client = clientFactory(parameters);
                                     int currentChunk = getNextChunk();
                                     int tries = 0;

                                     while (currentChunk >= 0 && tries < 3 && !cancellation.Value.IsCancellationRequested) //-1 when we are done
                                     {
                                         logger(string.Format("downloading: {0}", currentChunk));
                                         SimpleHttpResponse response = null;
                                         try
                                         {
                                             response = client.Get(parameters.Uri,
                                                                   GetChunkStart(currentChunk, parameters.MaxChunkSize),
                                                                   GetChunkSizeForCurrentChunk(parameters.FileSize,
                                                                                               parameters.MaxChunkSize,
                                                                                               currentChunk));
                                         }
                                         catch (Exception e)
                                         {
                                             logger(e.Message);
                                             ExecuteAndSquash(client.Dispose);
                                             client = clientFactory(parameters);
                                         }

                                         if (response != null && response.WasSuccessful)
                                         {
                                             contentDic.AddOrUpdate(currentChunk, response.Content,
                                                                    (i, bytes) => response.Content);
                                             tries = 0;
                                             logger(string.Format("downloaded: {0}", currentChunk));
                                             reset.Set();
                                             currentChunk = getNextChunk();
                                             while (shouldSlowDown(currentChunk))
                                             {
                                                 logger(string.Format("throttling for chunk: {0}", currentChunk));
                                                 if (!cancellation.Value.IsCancellationRequested)
                                                 {
                                                     Thread.Sleep(500);
                                                 }
                                             }
                                         }
                                         else if (response == null || response.IsStatusCodeRetryable)
                                         {
                                             logger(string.Format("sleeping: {0}", currentChunk));
                                             if (!cancellation.Value.IsCancellationRequested)
                                             {
                                                 Thread.Sleep((int)Math.Pow(100, tries)); //progressively slow down, don't do this if tries is more than 3 :)
                                                 tries++; 
                                             }
                                         }
                                         else
                                         {
                                             throw new SimpleHttpClientException(response);
                                         }
                                     }
                                     if (client != null)
                                     {
                                            ExecuteAndSquash(client.Dispose);
                                     }
                                 }, cancellation.Value);
            return t;
        }

        internal static void ExecuteAndSquash( Action a)
        {
            try
            {
                a();
            }
            catch 
            {
            }
        }

        internal static long GetChunkStart(int currentChunk, int maxChunkSize)
        {
            return currentChunk*(maxChunkSize);
        }

        internal static int GetChunkCount(long fileSize, int chunkSize)
        {
            return (int) (fileSize/chunkSize + (fileSize%chunkSize > 0 ? 1 : 0));
        }

        internal static int GetChunkSizeForCurrentChunk(long fileSize, int maxChunkSize, int zeroBasedChunkNumber)
        {
            int chunkCount = GetChunkCount(fileSize, maxChunkSize);

            if (zeroBasedChunkNumber + 1 < chunkCount)
            {
                return maxChunkSize;
            }

            if (zeroBasedChunkNumber >= chunkCount)
            {
                return 0;
            }

            var remainder = (int) (fileSize%maxChunkSize);
            return remainder > 0 ? remainder : maxChunkSize;
        }
    }
}