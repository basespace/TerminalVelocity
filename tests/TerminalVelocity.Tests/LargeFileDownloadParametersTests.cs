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

    }
}
