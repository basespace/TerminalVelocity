using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace Illumina.TerminalVelocity
{
    public class SimpleHttpGetByRangeClientWithWebRequest : ISimpleHttpGetByRangeClient
    {
        public const int DEFAULT_TIMEOUT = 1000 * 30; //30 seconds
        public const int BUFFER_SIZE = 9000;
        private readonly BufferManager bufferManager;
        public SimpleHttpGetByRangeClientWithWebRequest(BufferManager bufferManager = null, int timeout = DEFAULT_TIMEOUT)
        {
            ConnectionTimeout = timeout;
            if (bufferManager == null)
            {
                bufferManager = new BufferManager(new[] { new BufferQueueSetting(BUFFER_SIZE, 1) });
            }
            this.bufferManager = bufferManager;
        }
      
        static SimpleHttpGetByRangeClientWithWebRequest()
        {
            if (ServicePointManager.DefaultConnectionLimit <= 2)
            {
                ServicePointManager.DefaultConnectionLimit = 1000;
            }
        }

        public void Dispose()
        {
           
        }

        public SimpleHttpResponse Get(Uri uri, long start, long length)
        {
            var wr = (HttpWebRequest) WebRequest.Create(uri);
            try
            {
                
                wr.AddRange(start, start + length);
                wr.ServicePoint.Expect100Continue = false;
              //  wr.ConnectionGroupName = "TerminalVelocity-" + Guid.NewGuid();
                wr.Method = "GET";
                wr.Timeout = ConnectionTimeout;
                wr.ProtocolVersion = HttpVersion.Version11;
                wr.KeepAlive = false;
                wr.AllowAutoRedirect = false;
                wr.AllowWriteStreamBuffering = false;
             //   wr.ServicePoint.ConnectionLeaseTimeout = 0;
                wr.Proxy = null;
                // wr.ServicePoint = new ServicePoint();
                using (var response = (HttpWebResponse) wr.GetResponse())
                {
                    return ConvertWebResponseToSimpleResponse(response, bufferManager);
                }
            }finally
            {
                wr.ServicePoint.CloseConnectionGroup(wr.ConnectionGroupName);
            }

        }

        public int ConnectionTimeout { get; set; }

        public static SimpleHttpResponse ConvertWebResponseToSimpleResponse(HttpWebResponse response, BufferManager bufferManager)
        {
            SimpleHttpResponse simpleResponse = null;
            if (response != null)
            {
                int statusCode = (int) response.StatusCode;
                if (statusCode >= 200 && statusCode <= 300)
                {
                    long contentLength = response.ContentLength;
                    
                    byte[] dest = bufferManager.GetBuffer((uint) contentLength);
                    Console.WriteLine(Thread.CurrentThread.Name + " - content length + " + contentLength); 
                    using (var outputStream = new MemoryStream(dest))
                    {
                        response.GetResponseStream().CopyTo(outputStream);
                        simpleResponse = new SimpleHttpResponse(statusCode, dest, ConvertHeaders(response.Headers));
                    }
                }
                else
                {
                    simpleResponse = new SimpleHttpResponse(statusCode, null, ConvertHeaders(response.Headers));
                }

            }
            return simpleResponse;
        }

        public static Dictionary<string, string> ConvertHeaders(WebHeaderCollection collection)
        {
            if (collection != null)
            {
                return collection.Keys.Cast<string>().ToDictionary(key => key, key => collection[key]);
            }
            return null;
        }
    }
}
