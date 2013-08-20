using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.GZip;
using Moq;
using NUnit.Framework;

namespace Illumina.TerminalVelocity.Tests
{
    
    [TestFixture]
    public class DownloadTests
    {
        public DownloadTests()
        {
            Debug.Listeners.Add(new DefaultTraceListener());
        }
        public const string ONE_GIG_FILE_S_SL = @"https://1000genomes.s3.amazonaws.com/release/20110521/ALL.wgs.phase1_release_v3.20101123.snps_indels_sv.sites.vcf.gz?AWSAccessKeyId=AKIAIYDIF27GS5AAXHQQ&Expires=1425600785&Signature=KQ3qGSqFYN0z%2BHMTGLAGLUejtBw%3D";
        public const string ONE_GIG_FILE = @"http://1000genomes.s3.amazonaws.com/release/20110521/ALL.wgs.phase1_release_v3.20101123.snps_indels_sv.sites.vcf.gz?AWSAccessKeyId=AKIAIYDIF27GS5AAXHQQ&Expires=1425600785&Signature=KQ3qGSqFYN0z%2BHMTGLAGLUejtBw%3D";
        public const string ONE_GIG_CHECKSUM = "24b9f9d41755b841eaf8d0faeab00a6c";//24b9f9d41755b841eaf8d0faeab00a6c
        public const string TWENTY_CHECKSUM = "11db70c5bd445c4b41de6cde9d655ee8";
        public const string TWENTY_MEG_FILE =
            @"https://1000genomes.s3.amazonaws.com/release/20100804/ALL.chrX.BI_Beagle.20100804.sites.vcf.gz?AWSAccessKeyId=AKIAIYDIF27GS5AAXHQQ&Expires=1425620139&Signature=h%2BIqHbo2%2Bjk0jIbR2qKpE3iS8ts%3D";
        public const string THIRTY_GIG_FILE = @"https://1000genomes.s3.amazonaws.com/data/HG02484/sequence_read/SRR404082_2.filt.fastq.gz?AWSAccessKeyId=AKIAIYDIF27GS5AAXHQQ&Expires=1362529020&Signature=l%2BS3sA1vkZeFqlZ7lD5HrQmY5is%3D";

        [Test]
        public void SimpleGetClientGetsFirst100Bytes()
        {
            var timer = new Stopwatch();
            timer.Start();
            var uri = new Uri(ONE_GIG_FILE);
            var client = new SimpleHttpGetByRangeClient(uri);
            var response =client.Get(uri, 0, 100);
            timer.Stop();
            Debug.WriteLine(string.Format("total {0}ms or {1}secs", timer.ElapsedMilliseconds, timer.ElapsedMilliseconds/1000));
            Assert.NotNull(response);
            Assert.True(response.ContentLength == 100);
            Assert.True(response.ContentRangeLength == 1297662912);
            Assert.True(response.ContentRangeStart == 0);
            Assert.True(response.ContentRangeStop == 99);
            Assert.True(response.StatusCode == 206);
            Assert.NotNull(response.Content);
            Assert.True(response.ContentLength == response.Content.Length);
        }

        [Test]
        public void ReadStackReturnsSequentially()
        {
            var readStack = new ConcurrentStack<int>();
            //add all of the chunks to the stack
            readStack.PushRange(Enumerable.Range(0, 5).Reverse().ToArray());
            for (int i = 0; i < 5; i++)
            {
                int current;
                readStack.TryPop(out current);
                Assert.True(current == i);
            }
           
        }

        [Test]
        public void ChunkCalculationsForLargeFiles()
        {
            int maxChunkSize = 5242880;
            int currentChunk = 411;
            long fileSize = 141705080397;
            long chunkStart = Downloader.GetChunkStart(currentChunk, maxChunkSize);
            Assert.True(chunkStart == 2154823680);
            long chunkSize = Downloader.GetChunkSizeForCurrentChunk(fileSize, maxChunkSize, currentChunk);
            Assert.True(chunkSize == maxChunkSize);
        }

        
        [TestCase(16 * 1024, 61), TestCase(0,61), TestCase(2, 61), TestCase(1024, 61), TestCase(128*1024, 62), TestCase(1024*1024, 77)]

        public void ExpectedDownloadTimeCalculation([Values()] int chunkSize, int expected)
        {
            Assert.AreEqual( expected, Downloader.ExpectedDownloadTimeInSeconds(chunkSize));
        }

        [Test]
        public void ThrottleDownloadWhenQueueIsFull()
        {
            var parameters = new LargeFileDownloadParameters(new Uri(@"http://www.google.com"), "blah", 1000);
            var dict = new ConcurrentDictionary<int, byte[]>();
            var e = new AutoResetEvent(false);
            
            byte[] sampleResponse = Encoding.UTF8.GetBytes("hello world");
            var mockClient = new Mock<ISimpleHttpGetByRangeClient>();

            mockClient.Setup(x => x.Get(It.IsAny<Uri>(), It.IsAny<long>(), It.IsAny<long>()))
                      .Returns(new SimpleHttpResponse(206, sampleResponse, null));
            int timesAskedForSlow = -1;
            
            var readStack = new ConcurrentStack<int>();
            //add all of the chunks to the stack
            readStack.PushRange(Enumerable.Range(0, 5).Reverse().ToArray());
            Func<int, bool> shouldSlw = i =>
                                            {
                                                timesAskedForSlow++;
                                                return true;
                                            };

            var task = Downloader.CreateDownloadTask(parameters, dict,e, readStack, shouldSlw,Downloader.ExpectedDownloadTimeInSeconds(parameters.MaxChunkSize), clientFactory: (x) => mockClient.Object );
            task.Start();
            task.Wait(2000);
            try
            {
                task.Dispose();
            }catch{}
            Assert.True(timesAskedForSlow > 1);
            int next;
            readStack.TryPop(out next);
            Assert.True(next == 2);
            Assert.True(Encoding.UTF8.GetString(dict[0]) == "hello world");

        }

        [Test]
        public void PutBackOnStackWhenFailed()
        {
            var parameters = new LargeFileDownloadParameters(new Uri(@"http://www.google.com"), "blah", 1000);
            var dict = new ConcurrentDictionary<int, byte[]>();
            var e = new AutoResetEvent(false);

            byte[] sampleResponse = Encoding.UTF8.GetBytes("hello world");
            var mockClient = new Mock<ISimpleHttpGetByRangeClient>();

            mockClient.Setup(x => x.Get(It.IsAny<Uri>(), It.IsAny<long>(), It.IsAny<long>()))
                      .Throws(new Exception("hahaha"));
            int timesAskedForSlow = -1;

            var readStack = new ConcurrentStack<int>();
            //add all of the chunks to the stack
            readStack.PushRange(Enumerable.Range(0,2).Reverse().ToArray());
            Func<int, bool> shouldSlw = i =>
            {
                timesAskedForSlow++;
                return true;
            };
            try
            {
                var ct = new CancellationTokenSource();
                var task = Downloader.CreateDownloadTask(parameters, dict, e, readStack, shouldSlw, Downloader.ExpectedDownloadTimeInSeconds(parameters.MaxChunkSize),
                                                         clientFactory: (x) => mockClient.Object, cancellation: ct.Token);
                task.Start();
                task.Wait(5000);
                ct.Cancel();
                task.Wait(10000);
                task.Dispose();

            }
            catch
            {
            }
            Assert.True(readStack.Count == 2);
            int next;
            readStack.TryPop(out next);
            Assert.True(next == 0);
        }

        [Test]
        public void CancellationTokenWillCancel()
        {
            var parameters = new LargeFileDownloadParameters(new Uri(@"http://www.google.com"), "blah", 1000);
            var dict = new ConcurrentDictionary<int, byte[]>();
            var e = new AutoResetEvent(false);

            byte[] sampleResponse = Encoding.UTF8.GetBytes("hello world");
            var mockClient = new Mock<ISimpleHttpGetByRangeClient>();

            mockClient.Setup(x => x.Get(It.IsAny<Uri>(), It.IsAny<long>(), It.IsAny<long>()))
                      .Returns(() =>
            {
                Thread.Sleep(200);
                return new SimpleHttpResponse(206, sampleResponse, null);
            });

            int timesAskedForSlow = -1;
            var readStack = new ConcurrentStack<int>();
            //add all of the chunks to the stack
            readStack.PushRange(Enumerable.Range(0, 5).Reverse().ToArray());
            Func<int, bool> shouldSlw = i =>
            {
                timesAskedForSlow++;
                return false;
            };
            var tokenSource = new CancellationTokenSource();
            
            var task = Downloader.CreateDownloadTask(parameters, dict, e, readStack, shouldSlw,Downloader.ExpectedDownloadTimeInSeconds(parameters.MaxChunkSize), clientFactory: (x) => mockClient.Object, cancellation: tokenSource.Token);
            task.Start();
            Thread.Sleep(500);
            tokenSource.Cancel();
            task.Wait(TimeSpan.FromMinutes(2));
            int current;
            readStack.TryPop(out current);
            Assert.True(current != 10); //we shouldn't get to 10 before the cancel works

        }

        [Test]
        public void CalculateChunkCalcs()
        {
            long fileSize = 29996532;
            int maxChunk = LargeFileDownloadParameters.DEFAULT_MAX_CHUNK_SIZE;
            var chunkCount =Downloader.GetChunkCount(fileSize, maxChunk);
            Assert.True(chunkCount == 6);
            long totalBytes = 0;
            long lastChunkStart = 0;
            int lastChunkLength = maxChunk;
            for (int i = 0; i < chunkCount; i++)
            {
                var chunkStart = Downloader.GetChunkStart(i, maxChunk);
                var chunkLength = Downloader.GetChunkSizeForCurrentChunk(fileSize, maxChunk, i);
                Debug.WriteLine(string.Format("chunk {0} start {1} length {2}", i, chunkStart, chunkLength));
                totalBytes += chunkLength;
                lastChunkStart = chunkStart;
                lastChunkLength = chunkLength;
            }
            Assert.True(totalBytes == fileSize);
            Assert.True(lastChunkStart + lastChunkLength == fileSize);

        }

          [TestCase(1), TestCase(2),  TestCase(4),  TestCase(8)]
        public void ParallelChunkedDownload(int threadCount)
        {
          
            var uri = new Uri(TWENTY_MEG_FILE);
            var path = SafePath("sites_vcf.gz");
            Action<string> logger = ( message) => Trace.WriteLine(message);
            var timer = new Stopwatch();
              timer.Start();
            ILargeFileDownloadParameters parameters = new LargeFileDownloadParameters(uri, path, 29996532, maxThreads: threadCount);
           Task task = parameters.DownloadAsync(logger: logger);
            task.Wait(TimeSpan.FromMinutes(5));
            timer.Stop();
            Debug.WriteLine("Took {0} threads {1} ms", threadCount, timer.ElapsedMilliseconds);
            //try to open the file
            ValidateGZip(path, parameters.FileSize, TWENTY_CHECKSUM);
        }

          [TestCase(32)]
          public void ParallelChunkedOneGig(int threadCount)
          {
              var uri = new Uri(ONE_GIG_FILE_S_SL);
              var path = SafePath("sites_vcf.gz");
              Action<string> logger = (message) => Trace.WriteLine(message);
              var timer = new Stopwatch();
              timer.Start();
              ILargeFileDownloadParameters parameters = new LargeFileDownloadParameters(uri, path,  1297662912,  maxThreads: threadCount);
              Task task = parameters.DownloadAsync(logger: logger);
              task.Wait(TimeSpan.FromMinutes(5));
              timer.Stop();
              Debug.WriteLine("Took {0} threads {1} ms", threadCount, timer.ElapsedMilliseconds);
              //try to open the file
              ValidateGZip(path, parameters.FileSize, ONE_GIG_CHECKSUM);
          }

        private static void ValidateGZip(string path, long fileSize, string checksum)
        {
            using (Stream fs = File.OpenRead(path))
            {
                Assert.True(fs.Length == fileSize);
               string actualChecksum = Md5SumByProcess(path);
                Assert.AreEqual(checksum, actualChecksum);

            }
        }

        [Test]
        public void ValidateSpeedOfWebRequest()
        {
            var uri = new Uri(TWENTY_MEG_FILE);
            var path = SafePath("sites_vcf.gz");
            Action<string> logger = (message) => Trace.WriteLine(message);
            var timer = new Stopwatch();
            timer.Start();
            var client = new WebClient();
            client.DownloadFile(uri, path);
            timer.Stop();
            Debug.WriteLine("Took {0} threads {1} ms", 1, timer.ElapsedMilliseconds);
            ValidateGZip(path, 29996532, TWENTY_CHECKSUM);
            
        }

        //[Test]
        //public void CheckMd5()
        //{
        //    FileInfo info =
        //        new FileInfo(@"C:\github\TerminalVelocity\tests\TerminalVelocity.Tests\bin\Debug\sites_vcf.gz");
        //    var length = info.Length;
        //    string result = Md5SumByProcess(
        //        @"C:\github\TerminalVelocity\tests\TerminalVelocity.Tests\bin\Debug\ALL.wgs.phase1_release_v3.20101123.snps_indels_sv.sites.vcf.gz");
        //    Assert.True(result != null);
        //}

        public static string Md5SumByProcess(string file)
        {
            var p = new Process();
            string md5Path = Path.Combine(new DirectoryInfo(Environment.CurrentDirectory).Parent.Parent.Parent.Parent.FullName,"lib","fciv.exe");
            p.StartInfo.FileName = md5Path;
            p.StartInfo.Arguments = string.Format(@"-add ""{0}""",file);
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.Start();
            p.WaitForExit();
            string output = p.StandardOutput.ReadToEnd().Replace(@"//
// File Checksum Integrity Verifier version 2.05.
//
", "");
            return output.Split(' ')[0];
        }

       

        [Test]
        public void SimpleGetClientCanDownloadTwentyMegFileSynchronously()
        {
            var timer = new Stopwatch();
            var uri = new Uri(TWENTY_MEG_FILE);
            var client = new SimpleHttpGetByRangeClient(uri);
            var path = SafePath("sites_vcf.gz");
            timer.Start();

            using (FileStream output = File.Create(path))
            {
                const int chunksize = 1024*1000*2;
                var response = client.Get(uri, 0, chunksize);
                output.Write(response.Content, 0, (int)response.ContentLength);
              
                long currentFileSize = (int)response.ContentLength;
                long fileSize = response.ContentRangeLength.Value;
                while (currentFileSize < fileSize)
                {
                    SimpleHttpResponse loopResponse;
                    long left = fileSize - currentFileSize;
                    Debug.WriteLine("chunk start {0} length {1} ", currentFileSize, left < chunksize ? left : chunksize);
                    loopResponse = client.Get(new Uri(TWENTY_MEG_FILE), currentFileSize, left < chunksize ? left : chunksize);
                    output.Write(loopResponse.Content, 0, (int)loopResponse.ContentLength);
                    currentFileSize += loopResponse.ContentLength;
                }

            }
            
            timer.Stop();
            Debug.WriteLine("total {0}ms or {1}secs", timer.ElapsedMilliseconds, timer.ElapsedMilliseconds / 1000);
            
            using (Stream fs = File.OpenRead(path))
            {
                using (Stream gzipStream =  new GZipInputStream(fs) )
                {
                    using (var reader = new StreamReader(gzipStream))
                    {
                        reader.Read();
                    }
                }
            }
        }

        private static string SafePath(string fileName)
        {
            string path = Path.Combine(Environment.CurrentDirectory,fileName);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            return path;
        }
    }
}
