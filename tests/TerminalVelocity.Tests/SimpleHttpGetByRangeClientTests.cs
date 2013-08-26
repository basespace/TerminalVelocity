﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace Illumina.TerminalVelocity.Tests
{
    [TestFixture]
    public class SimpleHttpGetByRangeClientTests
    {
        [Test]
        [ExpectedException(typeof(IOException))]
        public void TimeoutQuitsAsExpected()
        {
            var client =  new SimpleHttpGetByRangeClient(new Uri(Constants.ONE_GIG_FILE_S_SL), timeout: 1);
            var response = client.Get(0, 1024*10);//something large
            Assert.Fail("Timeout didn't happen");
        }

        [Test]
        public void ReasonableTimeoutPreventsIOException()
        {
            var client = new SimpleHttpGetByRangeClient(new Uri(Constants.ONE_GIG_FILE_S_SL), timeout: 1000 * 10);
            var response = client.Get(0, 1024 * 2);//something large
            Assert.NotNull(response);
        }

        [Test]
        public void ChangeInTimeoutWorks()
        {
            SimpleHttpGetByRangeClient client = null;
            SimpleHttpResponse response;
            try
            {
                client = new SimpleHttpGetByRangeClient(new Uri(Constants.ONE_GIG_FILE_S_SL), timeout: 1);
                 response = client.Get(0, 1024*10); //something large
            }
            catch (IOException ex)
            {
                
            }
            client.ConnectionTimeout = 1000*10;
             response = client.Get(0, 1024 * 10); //gave it enough time
            Assert.NotNull(response);



        }
    }
}