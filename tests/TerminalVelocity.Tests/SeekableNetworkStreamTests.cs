using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using NUnit.Framework;


namespace Illumina.TerminalVelocity.Tests
{
   [TestFixture]
    public class SeekableNetworkStreamTests
    {
        [Test]
        public void CanReadTheWholeFile()
        {
            var stream = new SeekableNetworkStream(Constants.TWENTY_MEG_FILE);
            byte[] source = new byte[stream.Length];
            stream.Read(source, 0, (int)stream.Length);
            string tempFile = Path.GetTempFileName();
            try
            {
                File.WriteAllBytes(tempFile, source);
                string checkSum = DownloadTests.Md5SumByProcess(tempFile);
                Assert.True(checkSum == Constants.TWENTY_CHECKSUM);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Test]
        public void CanReadPartsOfFileCorrectly()
        {
            //download file then read different parts of each
            string tempFile = Path.GetTempFileName();
            try
            {

                new WebClient().DownloadFile(Constants.TWENTY_MEG_FILE, tempFile);
                byte[] expected;
                using (var binaryReader = new BinaryReader(File.OpenRead(tempFile)))
                {
                    binaryReader.BaseStream.Position = 100;
                    expected = binaryReader.ReadBytes(5);
                }
                var stream = new SeekableNetworkStream(Constants.TWENTY_MEG_FILE);
                var actual = new byte[5];
                stream.Seek(100, SeekOrigin.Begin);
                stream.Read(actual, 0, 5);

                //make sure bytes are the same
                Assert.True(actual.SequenceEqual(expected));
            }
            finally
            {
                File.Delete(tempFile);
            }
        }
    }
}
