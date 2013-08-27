using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Illumina.TerminalVelocity.Tests
{
    [TestFixture]
    public class LargeFileDownloadWithStreamParametersTests
    {
        [TestCase(8, Category = "time-consuming")]
        public void DownloadLargeFilesWithNonOptimizedStream(int threadCount)
        {
            var uri = new Uri(Constants.ONE_GIG_FILE_S_SL);
            var path = DownloadTests.SafePath("sites_vcf.gz");
            Action<string> logger = (message) => { };
            var timer = new Stopwatch();
            timer.Start();
            var manager = new BufferManager(new[] { new BufferQueueSetting(SimpleHttpGetByRangeClient.BUFFER_SIZE, (uint)threadCount), new BufferQueueSetting(LargeFileDownloadParameters.DEFAULT_MAX_CHUNK_SIZE) });
            LargeFileDownloadParameters.EnsureCleanFile(path, true);
            using (var stream = new FileStream(path, FileMode.OpenOrCreate))
            {
                ILargeFileDownloadParameters parameters = new LargeFileDownloadWithStreamParameters(uri, stream,
                                                                                                    Constants.ONE_GIG_FILE_LENGTH,
                                                                                                    maxThreads:
                                                                                                        threadCount);
                Task task = parameters.DownloadAsync(logger: logger, bufferManager: manager);
                task.Wait(TimeSpan.FromMinutes(15));
                timer.Stop();

                Debug.WriteLine("Took {0} threads {1} ms", threadCount, timer.ElapsedMilliseconds);
                //try to open the file
                DownloadTests.ValidateGZip(path, parameters.FileSize, Constants.ONE_GIG_CHECKSUM);
            }
        }

        [TestCase(8)]
        public void DownloadSmallFilesWithNonOptimizedStream(int threadCount)
        {
            var uri = new Uri(Constants.TWENTY_MEG_FILE);
            var path = DownloadTests.SafePath("sites_vcf.gz");
            Action<string> logger = (message) => { };
            var timer = new Stopwatch();
            timer.Start();
            var manager = new BufferManager(new[] { new BufferQueueSetting(SimpleHttpGetByRangeClient.BUFFER_SIZE, (uint)threadCount), new BufferQueueSetting(LargeFileDownloadParameters.DEFAULT_MAX_CHUNK_SIZE) });
            LargeFileDownloadParameters.EnsureCleanFile(path, true);
            using (var stream = new FileStream(path, FileMode.OpenOrCreate))
            {
                ILargeFileDownloadParameters parameters = new LargeFileDownloadWithStreamParameters(uri, stream,
                                                                                                    Constants.TWENTY_MEG_FILE_LENGTH,
                                                                                                    maxThreads:
                                                                                                        threadCount);
                Task task = parameters.DownloadAsync(logger: logger, bufferManager: manager);
                task.Wait(TimeSpan.FromMinutes(5));
                timer.Stop();

                Debug.WriteLine("Took {0} threads {1} ms", threadCount, timer.ElapsedMilliseconds);
                //try to open the file
                DownloadTests.ValidateGZip(path, parameters.FileSize, Constants.TWENTY_CHECKSUM);
            }
        }

        [TestCase(10, 1000, 10)]
        [TestCase(0, 1000, 0)]
        [TestCase(2000, 1000, 1000)]
        public void MaxChunkSizeCalculation(int fileSize, int chunkSize, int expected)
        {
            var parameters = new LargeFileDownloadWithStreamParameters(new Uri("http://blah.com"),new MemoryStream(),fileSize,verifyLength:false,maxChunkSize:chunkSize);
            Assert.AreEqual(expected, parameters.MaxChunkSize);
        }


        [Test]
        public void IdIsAssignedWhenSupplied()
        {
            var parameters = new LargeFileDownloadWithStreamParameters(new Uri("http://blah.com"), new MemoryStream(), 20, verifyLength: false, maxChunkSize: 1, id: "test");
            Assert.True(parameters.Id == "test");
        }

        [Test]
        public void IdIsAssignedWhenNotSupplied()
        {

            var parameters = new LargeFileDownloadWithStreamParameters(new Uri("http://blah.com"), new MemoryStream(), 20, verifyLength: false, maxChunkSize: 1);
            Assert.NotNull(parameters.Id);
            Assert.True(parameters.Id.Length > 10);

        }
        
    }
}
