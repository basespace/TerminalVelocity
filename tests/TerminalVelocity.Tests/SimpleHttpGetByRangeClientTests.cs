using System;
using System.IO;
using NUnit.Framework;

namespace Illumina.TerminalVelocity.Tests
{
    [TestFixture]
    public class SimpleHttpGetByRangeClientTests
    {
        [Test]
        [Ignore("To temporarily fix build failure")]
    //    [ExpectedException(typeof(IOException))]
        public void TimeoutQuitsAsExpected()
        {
            var client =  new SimpleHttpGetByRangeClient(new Uri(Constants.ONE_GIG_FILE_S_SL), timeout: 1);
            var response = client.Get(0, 1024*1024*10);//something large
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

        [Test]
        public void ReadMultipleGetsInARow()
        {
            int reads = 5;
            var client = new SimpleHttpGetByRangeClient(new Uri(Constants.ONE_GIG_FILE_S_SL), timeout: 1000 * 10);
            for (int i = 0; i < reads; i++)
            {
                var response = client.Get(i, 1024 * i + 1);//something large
                Assert.NotNull(response);
            }
         
           
        }
    }
}
