using System;
using System.Collections.Generic;
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
                    while (true)
                    {                    var files = new List<string>()
                                    {   "https://cloud-hoth-data-east.s3.amazonaws.com/4ad11a9c45cb4caa8e7da24b01177cb2/tophat_fusion/read_alignments/main_chr1-chr1_207308124_207495030_ff?AWSAccessKeyId=AKIAIYDIF27GS5AAXHQQ&Expires=1414267041&Signature=1%2FKdTQ2X9RSZMYYzurcuX8Cihes%3D",
                                        "https://cloud-hoth-data-east.s3.amazonaws.com/4ad11a9c45cb4caa8e7da24b01177cb2/tophat_fusion/read_alignments/main_chr1-chr1_248100685_248262659_ff?AWSAccessKeyId=AKIAIYDIF27GS5AAXHQQ&Expires=1414267041&Signature=mxXUtHbnzMTi50r35daizZeKp7o%3D",
                                        "https://cloud-hoth-data-east.s3.amazonaws.com/4ad11a9c45cb4caa8e7da24b01177cb2/tophat_fusion/read_alignments/main_chr1-chr1_65352023_65532310_ff?AWSAccessKeyId=AKIAIYDIF27GS5AAXHQQ&Expires=1414267041&Signature=z%2FEfEsqYedhWE%2BDTpEUF5%2FYHGFA%3D",
                                        "https://cloud-hoth-data-east.s3.amazonaws.com/4ad11a9c45cb4caa8e7da24b01177cb2/tophat_fusion/read_alignments/main_chr10-chr10_116361719_116527819_ff?AWSAccessKeyId=AKIAIYDIF27GS5AAXHQQ&Expires=1414267041&Signature=Vm5zAxA2zUC91%2FdppUJi4bEsHBw%3D",
                                        "https://cloud-hoth-data-east.s3.amazonaws.com/4ad11a9c45cb4caa8e7da24b01177cb2/tophat_fusion/read_alignments/main_chr10-chr10_118466795_118594445_ff?AWSAccessKeyId=AKIAIYDIF27GS5AAXHQQ&Expires=1414267041&Signature=Dp2e%2FfUaL3u%2BJFGJt61gC4uH4Tk%3D",
                                        "https://cloud-hoth-data-east.s3.amazonaws.com/4ad11a9c45cb4caa8e7da24b01177cb2/tophat_fusion/read_alignments/main_chr10-chr10_118466795_118609076_ff?AWSAccessKeyId=AKIAIYDIF27GS5AAXHQQ&Expires=1414267041&Signature=Z08WMGzcW9R2q%2FRbNqFcx4X2g%2Bo%3D",
                                        "https://cloud-hoth-data-east.s3.amazonaws.com/4ad11a9c45cb4caa8e7da24b01177cb2/tophat_fusion/read_alignments/main_chr10-chr10_65140241_65281497_ff?AWSAccessKeyId=AKIAIYDIF27GS5AAXHQQ&Expires=1414267041&Signature=Ob0sV6ybWo%2F00VGgh%2FU%2FCQ0HQG0%3D",
                                        "https://cloud-hoth-data-east.s3.amazonaws.com/4ad11a9c45cb4caa8e7da24b01177cb2/tophat_fusion/read_alignments/main_chr11-chr11_132081915_132812820_rf?AWSAccessKeyId=AKIAIYDIF27GS5AAXHQQ&Expires=1414267041&Signature=hb48cqWzqE2AB2fK8k0gScmmOKo%3D",
                                        "https://cloud-hoth-data-east.s3.amazonaws.com/4ad11a9c45cb4caa8e7da24b01177cb2/tophat_fusion/read_alignments/main_chr12-chr12_106893959_107168467_ff?AWSAccessKeyId=AKIAIYDIF27GS5AAXHQQ&Expires=1414267041&Signature=%2Ft9mEvWzFZkI1vZei5Rm%2BKcv6NQ%3D",
                                        "https://cloud-hoth-data-east.s3.amazonaws.com/4ad11a9c45cb4caa8e7da24b01177cb2/tophat_fusion/read_alignments/main_chr12-chr12_11001005_11126253_ff?AWSAccessKeyId=AKIAIYDIF27GS5AAXHQQ&Expires=1414267041&Signature=8tccGIuGucarJq2OvV3uKcsnNeI%3D",
                                        "https://cloud-hoth-data-east.s3.amazonaws.com/4ad11a9c45cb4caa8e7da24b01177cb2/tophat_fusion/read_alignments/main_chr12-chr12_77966045_78334098_ff?AWSAccessKeyId=AKIAIYDIF27GS5AAXHQQ&Expires=1414267041&Signature=4buijR2wVluhrR047aFN%2Fb619v8%3D",
                                        "https://cloud-hoth-data-east.s3.amazonaws.com/4ad11a9c45cb4caa8e7da24b01177cb2/tophat_fusion/read_alignments/main_chr15-chr15_101590983_101775286_ff?AWSAccessKeyId=AKIAIYDIF27GS5AAXHQQ&Expires=1414267041&Signature=UrSTB4117ElrAIo1AFQK7RhGdnQ%3D",
                                        "https://cloud-hoth-data-east.s3.amazonaws.com/4ad11a9c45cb4caa8e7da24b01177cb2/tophat_fusion/read_alignments/main_chr15-chr15_31008422_32162709_rf?AWSAccessKeyId=AKIAIYDIF27GS5AAXHQQ&Expires=1414267041&Signature=9Rb5SxhmvY8T%2FEqClmJZc99tKyM%3D",
                                        "https://cloud-hoth-data-east.s3.amazonaws.com/4ad11a9c45cb4caa8e7da24b01177cb2/tophat_fusion/read_alignments/main_chr17-chr17_28030079_28256955_ff?AWSAccessKeyId=AKIAIYDIF27GS5AAXHQQ&Expires=1414267041&Signature=35%2FeVU0w9DxSQ3GSP9vROm0muIo%3D",
                                        "https://cloud-hoth-data-east.s3.amazonaws.com/4ad11a9c45cb4caa8e7da24b01177cb2/tophat_fusion/read_alignments/main_chr2-chr2_172749767_172864585_ff?AWSAccessKeyId=AKIAIYDIF27GS5AAXHQQ&Expires=1414267041&Signature=4AvnVX%2F77dy9oaEzCXRjkVCv3VM%3D",
                                        "https://cloud-hoth-data-east.s3.amazonaws.com/4ad11a9c45cb4caa8e7da24b01177cb2/tophat_fusion/read_alignments/main_chr2-chr2_68444262_68544288_ff?AWSAccessKeyId=AKIAIYDIF27GS5AAXHQQ&Expires=1414267041&Signature=kegfiUjLSFVqCuhm3S7oywPkQdM%3D",
                                        "https://cloud-hoth-data-east.s3.amazonaws.com/4ad11a9c45cb4caa8e7da24b01177cb2/tophat_fusion/read_alignments/main_chr3-chr3_112738552_112862700_ff?AWSAccessKeyId=AKIAIYDIF27GS5AAXHQQ&Expires=1414267041&Signature=UF9LOe9vfMDPjef3wVuF8soLedk%3D",
                                        "https://cloud-hoth-data-east.s3.amazonaws.com/4ad11a9c45cb4caa8e7da24b01177cb2/tophat_fusion/read_alignments/main_chr3-chr3_24378861_24536285_ff?AWSAccessKeyId=AKIAIYDIF27GS5AAXHQQ&Expires=1414267041&Signature=MllwdPcq4BEuQX05tZKBO%2Fb5Qpw%3D",
                                        "https://cloud-hoth-data-east.s3.amazonaws.com/4ad11a9c45cb4caa8e7da24b01177cb2/tophat_fusion/read_alignments/main_chr3-chr3_24378861_24536623_ff?AWSAccessKeyId=AKIAIYDIF27GS5AAXHQQ&Expires=1414267041&Signature=qycSQSJI7uKRs4nhGnhLe1YICaU%3D",
                                        "https://cloud-hoth-data-east.s3.amazonaws.com/4ad11a9c45cb4caa8e7da24b01177cb2/tophat_fusion/read_alignments/main_chr3-chr3_3841602_4044112_ff?AWSAccessKeyId=AKIAIYDIF27GS5AAXHQQ&Expires=1414267041&Signature=Z0RX0KjhLNVvERNRJZrAY0%2B3AnY%3D",
                                        "https://cloud-hoth-data-east.s3.amazonaws.com/4ad11a9c45cb4caa8e7da24b01177cb2/tophat_fusion/read_alignments/main_chr4-chr16_114095571_29093361_rf?AWSAccessKeyId=AKIAIYDIF27GS5AAXHQQ&Expires=1414267041&Signature=JfBOMR%2F3Naxtwdl2ct19xdAxttQ%3D",
                                        "https://cloud-hoth-data-east.s3.amazonaws.com/4ad11a9c45cb4caa8e7da24b01177cb2/tophat_fusion/read_alignments/main_chr4-chr4_102117272_102269389_ff?AWSAccessKeyId=AKIAIYDIF27GS5AAXHQQ&Expires=1414267041&Signature=0PfYW4IaEflj0uYYCYvVW6tdxBU%3D",
                                        "https://cloud-hoth-data-east.s3.amazonaws.com/4ad11a9c45cb4caa8e7da24b01177cb2/tophat_fusion/read_alignments/main_chr4-chr4_113627372_114095571_ff?AWSAccessKeyId=AKIAIYDIF27GS5AAXHQQ&Expires=1414267041&Signature=pRTAlrdjZrboUoF1q7LCZ96ne8U%3D",
                                        "https://cloud-hoth-data-east.s3.amazonaws.com/4ad11a9c45cb4caa8e7da24b01177cb2/tophat_fusion/read_alignments/main_chr4-chr4_154073763_154191486_ff?AWSAccessKeyId=AKIAIYDIF27GS5AAXHQQ&Expires=1414267041&Signature=snz8a8Z%2FYsg%2Fg3e7%2FiDF06zIf4o%3D",
                                        "https://cloud-hoth-data-east.s3.amazonaws.com/4ad11a9c45cb4caa8e7da24b01177cb2/tophat_fusion/read_alignments/main_chr4-chr4_169815915_169923221_ff?AWSAccessKeyId=AKIAIYDIF27GS5AAXHQQ&Expires=1414267041&Signature=2EphsKlHEVIY7sjQ1qldK%2FWIB8A%3D",
                                        "https://cloud-hoth-data-east.s3.amazonaws.com/4ad11a9c45cb4caa8e7da24b01177cb2/tophat_fusion/read_alignments/main_chr4-chr4_56342127_56496624_rr?AWSAccessKeyId=AKIAIYDIF27GS5AAXHQQ&Expires=1414267041&Signature=7WR2TGKYiVKnv%2BlvPlzt%2FtijTgQ%3D",
                                        "https://cloud-hoth-data-east.s3.amazonaws.com/4ad11a9c45cb4caa8e7da24b01177cb2/tophat_fusion/read_alignments/main_chr5-chr5_120022541_120125648_ff?AWSAccessKeyId=AKIAIYDIF27GS5AAXHQQ&Expires=1414267041&Signature=2laOSqQlT0WesFYMMfXZhMXB1wU%3D",
                                        "https://cloud-hoth-data-east.s3.amazonaws.com/4ad11a9c45cb4caa8e7da24b01177cb2/tophat_fusion/read_alignments/main_chr5-chr5_148207567_148340479_ff?AWSAccessKeyId=AKIAIYDIF27GS5AAXHQQ&Expires=1414267041&Signature=PWl6hLQeZ0nu%2FHIZsN30V9%2Bt%2Bd0%3D",
                                        "https://cloud-hoth-data-east.s3.amazonaws.com/4ad11a9c45cb4caa8e7da24b01177cb2/tophat_fusion/read_alignments/main_chr6-chr6_114384040_114489514_ff?AWSAccessKeyId=AKIAIYDIF27GS5AAXHQQ&Expires=1414267041&Signature=LsvXM8MdgE6KMk8lZsvtMDrwIYE%3D",
                                        "https://cloud-hoth-data-east.s3.amazonaws.com/4ad11a9c45cb4caa8e7da24b01177cb2/tophat_fusion/read_alignments/main_chr6-chr6_167447458_167549525_ff?AWSAccessKeyId=AKIAIYDIF27GS5AAXHQQ&Expires=1414267041&Signature=XC3sEjKfaK7%2FusGFpTRBXa4X%2BN0%3D",
                                        "https://cloud-hoth-data-east.s3.amazonaws.com/4ad11a9c45cb4caa8e7da24b01177cb2/tophat_fusion/read_alignments/main_chr6-chr6_32230585_32333556_ff?AWSAccessKeyId=AKIAIYDIF27GS5AAXHQQ&Expires=1414267041&Signature=lBL0ajKdh7PRLX6Gpk2iDNWe9ac%3D",
                                        "https://cloud-hoth-data-east.s3.amazonaws.com/4ad11a9c45cb4caa8e7da24b01177cb2/tophat_fusion/read_alignments/main_chr7-chr7_102448797_102550667_ff?AWSAccessKeyId=AKIAIYDIF27GS5AAXHQQ&Expires=1414267041&Signature=Wnh%2BYtYRwJvf%2BCy5tt1NO3HpINc%3D",
                                        "https://cloud-hoth-data-east.s3.amazonaws.com/4ad11a9c45cb4caa8e7da24b01177cb2/tophat_fusion/read_alignments/main_chrY-chrY_21039090_21203255_ff?AWSAccessKeyId=AKIAIYDIF27GS5AAXHQQ&Expires=1414267041&Signature=jDdIxYhtEztQiJjwJj6J47S69u0%3D",
                                        
                                        "https://cloud-hoth-data-east.s3.amazonaws.com/4ad11a9c45cb4caa8e7da24b01177cb2/tophat_fusion/mRNA-Brain1.fusion_seq.bwtout?AWSAccessKeyId=AKIAIYDIF27GS5AAXHQQ&Expires=1414269024&Signature=lSpiNzgnhCRNwlj133iYxpI1eJg%3D",
"https://cloud-hoth-data-east.s3.amazonaws.com/4ad11a9c45cb4caa8e7da24b01177cb2/tophat_fusion/mRNA-Brain1.fusion_seq.fa?AWSAccessKeyId=AKIAIYDIF27GS5AAXHQQ&Expires=1414269024&Signature=FwGiQTpDp5S6QvHo%2Fc%2B6vlI3Us4%3D",
"https://cloud-hoth-data-east.s3.amazonaws.com/4ad11a9c45cb4caa8e7da24b01177cb2/tophat_fusion/mRNA-Brain1.fusion_seq.map?AWSAccessKeyId=AKIAIYDIF27GS5AAXHQQ&Expires=1414269024&Signature=X7%2FxQ9aYC45jc9JbDufYK32TKfw%3D",
"https://cloud-hoth-data-east.s3.amazonaws.com/4ad11a9c45cb4caa8e7da24b01177cb2/tophat_fusion/mRNA-Brain1.potential_fusion.txt?AWSAccessKeyId=AKIAIYDIF27GS5AAXHQQ&Expires=1414269024&Signature=L%2Fr4CpA6OUwTwpJvh5pzHqnmlho%3D",
"https://cloud-hoth-data-east.s3.amazonaws.com/4ad11a9c45cb4caa8e7da24b01177cb2/tophat_fusion/mRNA-Brain1.result.html?AWSAccessKeyId=AKIAIYDIF27GS5AAXHQQ&Expires=1414269024&Signature=px%2FZd41UdEm8g1r5B3vL4TjmTe0%3D",
"https://cloud-hoth-data-east.s3.amazonaws.com/4ad11a9c45cb4caa8e7da24b01177cb2/tophat_fusion/mRNA-Brain1.result.txt?AWSAccessKeyId=AKIAIYDIF27GS5AAXHQQ&Expires=1414269024&Signature=w%2BNXKRo%2FYQYTULRkzo%2FpElYe6uw%3D",
"https://cloud-hoth-data-east.s3.amazonaws.com/4ad11a9c45cb4caa8e7da24b01177cb2/tophat_fusion/mRNA-Brain1.sample_list.txt?AWSAccessKeyId=AKIAIYDIF27GS5AAXHQQ&Expires=1414269024&Signature=S24%2FmbRH9jNfYr%2FqvELijvLoQRI%3D",
"https://cloud-hoth-data-east.s3.amazonaws.com/4ad11a9c45cb4caa8e7da24b01177cb2/tophat_fusion/renaming_log.txt?AWSAccessKeyId=AKIAIYDIF27GS5AAXHQQ&Expires=1414269024&Signature=JnybnbhfI1fDlcTXd4kol2Pz0Lw%3D",

"https://cloud-hoth-data-east.s3.amazonaws.com/4ad11a9c45cb4caa8e7da24b01177cb2/filtered/mRNA-Brain1.mRNA-Brain1_S1_L001_R1_001.fastq.gz.info?AWSAccessKeyId=AKIAIYDIF27GS5AAXHQQ&Expires=1414267978&Signature=Uy2QFpjZYEWx5byD1aO1uQqXnvY%3D",
"https://cloud-hoth-data-east.s3.amazonaws.com/4ad11a9c45cb4caa8e7da24b01177cb2/filtered/mRNA-Brain1.read_1_files.txt?AWSAccessKeyId=AKIAIYDIF27GS5AAXHQQ&Expires=1414267978&Signature=%2BwalMugvJPUqVAYGwG8SKvlSE5Y%3D",
"https://cloud-hoth-data-east.s3.amazonaws.com/4ad11a9c45cb4caa8e7da24b01177cb2/filtered/mRNA-Brain1.read_2_files.txt?AWSAccessKeyId=AKIAIYDIF27GS5AAXHQQ&Expires=1414267978&Signature=AfwZlYIofbilyKRxbXaUgmDDrmA%3D",
"https://cloud-hoth-data-east.s3.amazonaws.com/4ad11a9c45cb4caa8e7da24b01177cb2/filtered/renaming_log.txt?AWSAccessKeyId=AKIAIYDIF27GS5AAXHQQ&Expires=1414267978&Signature=mAfgjQ7JZAU%2Fvqc2CSDc4XyaEXI%3D",

"https://cloud-hoth-data-east.s3.amazonaws.com/4ad11a9c45cb4caa8e7da24b01177cb2/metrics/sampled_alignment/combined_InsertLengthData.csv?AWSAccessKeyId=AKIAIYDIF27GS5AAXHQQ&Expires=1414268059&Signature=g6iujHnwDZCD6BS1owZUVlYuU00%3D",
"https://cloud-hoth-data-east.s3.amazonaws.com/4ad11a9c45cb4caa8e7da24b01177cb2/metrics/sampled_alignment/combined_Metrics.csv?AWSAccessKeyId=AKIAIYDIF27GS5AAXHQQ&Expires=1414268059&Signature=45UWfN%2F%2BLoYec9lIxPY2lzRv5Z8%3D",
"https://cloud-hoth-data-east.s3.amazonaws.com/4ad11a9c45cb4caa8e7da24b01177cb2/metrics/sampled_alignment/combined_PicardCoverage.csv?AWSAccessKeyId=AKIAIYDIF27GS5AAXHQQ&Expires=1414268059&Signature=aew%2Ba%2B5jLJMXbuY395u6NjU3Y1c%3D",
"https://cloud-hoth-data-east.s3.amazonaws.com/4ad11a9c45cb4caa8e7da24b01177cb2/metrics/sampled_alignment/read1_Metrics.csv?AWSAccessKeyId=AKIAIYDIF27GS5AAXHQQ&Expires=1414268059&Signature=JXjjMvlNakc8iqQjg2aprOfPD24%3D",
"https://cloud-hoth-data-east.s3.amazonaws.com/4ad11a9c45cb4caa8e7da24b01177cb2/metrics/sampled_alignment/read1_PicardCoverage.csv?AWSAccessKeyId=AKIAIYDIF27GS5AAXHQQ&Expires=1414268059&Signature=VqSbcvGy4Co%2FE8mbNkRxi3%2BJJf0%3D",
"https://cloud-hoth-data-east.s3.amazonaws.com/4ad11a9c45cb4caa8e7da24b01177cb2/metrics/sampled_alignment/read2_Metrics.csv?AWSAccessKeyId=AKIAIYDIF27GS5AAXHQQ&Expires=1414268059&Signature=mzpP0SdZytM1F7jIsdnobrLs4bU%3D",
"https://cloud-hoth-data-east.s3.amazonaws.com/4ad11a9c45cb4caa8e7da24b01177cb2/metrics/sampled_alignment/read2_PicardCoverage.csv?AWSAccessKeyId=AKIAIYDIF27GS5AAXHQQ&Expires=1414268059&Signature=0t4pKSqnYh6fpw1ClXz19JSJXg8%3D",

                                    };

                    foreach (var file in files)
                    {
                        options.Uri = new Uri(file);
                        options.FileSize = null;
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
                            IAsyncProgress<LargeFileDownloadProgressChangedEventArgs> progress = new AsyncProgress<LargeFileDownloadProgressChangedEventArgs>(Handler);
                            var task = parameters.DownloadAsync(token, progress, logger);
                            task.Wait();
                            watch.Stop();
                            ClearCurrentConsoleLine();

                            Console.WriteLine("done in {0}ms ({1}m {2}s {3}ms", watch.ElapsedMilliseconds, watch.Elapsed.Minutes, watch.Elapsed.Seconds, watch.Elapsed.Milliseconds);

                        }
                    }
                    }
                    
                    if (options.IsInteractive)
                    {
                        return true;
                    }
                    Environment.Exit(0);
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
