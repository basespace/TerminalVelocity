using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using NUnit.Framework;

namespace Illumina.TerminalVelocity.Tests
{
    [TestFixture]
    public class HelpersTests
    {
        
        [Test]
        public void GetContentLengthHandlesRedirect()
        {
            //with wrong file size
            var fileSize = new Uri(Constants.ONE_GIG_REDIRECT).GetContentLength();
            Assert.AreEqual(Constants.ONE_GIG_FILE_LENGTH, fileSize);


        }

        [Test]
        public void GetContentLengthSSLFile()
        {
            //with wrong file size
            var fileSize = new Uri(Constants.ONE_GIG_FILE_S_SL).GetContentLength();
            Assert.AreEqual(Constants.ONE_GIG_FILE_LENGTH, fileSize);


        }

        [Test]
        public void GetContentLengthNonSSLFile()
        {
            //with wrong file size
            var fileSize = new Uri(Constants.ONE_GIG_FILE).GetContentLength();
            Assert.AreEqual(Constants.ONE_GIG_FILE_LENGTH, fileSize);

        }

        [Test]
        [ExpectedException(typeof(SocketException))]
        public void GetContentLengthThrowsOnInvalidUrl()
        {
            var fileSize = new Uri("http://blah.com").GetContentLength();
        }
    }
}
