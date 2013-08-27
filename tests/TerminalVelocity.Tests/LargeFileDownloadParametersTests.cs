using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace Illumina.TerminalVelocity.Tests
{
    [TestFixture]
    public class LargeFileDownloadParametersTests
    {
        [Test]
        public void SparseFileIsCreatedOnWindows()
        {
                string newFile = DownloadTests.SafePath("testing.test");
                try
                {
                    var timer = new Stopwatch();
                    timer.Start();
                    var parameters = new LargeFileDownloadParameters(new Uri(Constants.ONE_GIG_FILE_S_SL), newFile,
                                                                     Constants.ONE_GIG_FILE_LENGTH);
                    Stream s = parameters.GetOutputStream();
                    //write to the end of the file
                    s.Seek((Constants.ONE_GIG_FILE_LENGTH) - 100, SeekOrigin.Begin);
                    var encoding = new System.Text.ASCIIEncoding();
                    Byte[] bytes = encoding.GetBytes("hello");
                    s.Write(bytes, 0, bytes.Length);
                    s.Close();

                    var info = new FileInfo(newFile);
                    timer.Stop();
                    //we should've pre-allocated file length
                    Assert.AreEqual(info.Length, Constants.ONE_GIG_FILE_LENGTH );
                    Assert.True(timer.ElapsedMilliseconds < 1500);  //should happen in less than a second since it's sparse

                }
                finally
                {
                   File.Delete(newFile);
                }

        }

        [Test]
        public void GivenWrongLengthWillFix()
        {
            string newFile = DownloadTests.SafePath("testing.test");
            var parameters = new LargeFileDownloadParameters(new Uri(Constants.ONE_GIG_FILE_S_SL), newFile,1);
            Assert.AreEqual(Constants.ONE_GIG_FILE_LENGTH, parameters.FileSize);
        }

        [Test]
        public void GivenWrongLengthWillNotFixIfTurnedOff()
        {
            string newFile = DownloadTests.SafePath("testing.test");
            var parameters = new LargeFileDownloadParameters(new Uri(Constants.ONE_GIG_FILE_S_SL), newFile, 1,verifyLength: false);
            Assert.AreEqual(1, parameters.FileSize);
        }

        [TestCase(10, 1000, 10)]
        [TestCase(0, 1000, 0)]
        [TestCase(2000, 1000, 1000)]
        public void MaxChunkSizeCalculation(int fileSize, int chunkSize, int expected)
        {
            var parameters = new LargeFileDownloadParameters(new Uri("http://blah.com"), "", fileSize, verifyLength: false, maxChunkSize: chunkSize);
            Assert.AreEqual(expected, parameters.MaxChunkSize);
        }

        [Test]
        public void IdIsAssignedWhenSupplied()
        {
            var parameters = new LargeFileDownloadParameters(new Uri("http://blah.com"), "", 20, verifyLength: false, maxChunkSize: 1, id:"test");
            Assert.True(parameters.Id == "test");
        }

        [Test]
        public void IdIsAssignedWhenNotSupplied()
        {
      
            var parameters = new LargeFileDownloadParameters(new Uri("http://blah.com"), "", 20, verifyLength: false, maxChunkSize: 1);
            Assert.NotNull( parameters.Id);
            Assert.True(parameters.Id.Length > 10);
      
        }

    }
}
