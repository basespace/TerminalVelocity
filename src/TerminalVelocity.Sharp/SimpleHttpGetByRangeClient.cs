using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Linq;

namespace Illumina.TerminalVelocity
{
    public class SimpleHttpGetByRangeClient : ISimpleHttpGetByRangeClient
    {
        public const string REQUEST_TEMPLATE = @"GET {0} HTTP/1.1
Host: {1}
Connection: keep-alive
Cache-Control: no-cache
User-Agent: Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/537.22 (KHTML, like Gecko) Chrome/25.0.1364.97 Safari/537.22
Accept: */*
Accept-Encoding: gzip
Accept-Language: en-US,en;q=0.8
Accept-Charset: utf-8;q=0.7,*;q=0.3
Range: bytes={2}-{3}

";
        internal const string INVALID_HEADER_LENGTH = "Invalid Header length";
        public static readonly byte[] BODY_INDICATOR = new byte[] { 13, 10, 13, 10 };
        public const int BUFFER_SIZE = 1024 * 8;
        public const int DEFAULT_TIMEOUT = 1000 * 200; //200 seconds
        private TcpClient tcpClient;
        private Uri baseUri;
        private Stream stream;
        private BufferManager bufferManager;
        private int timeout;
        private Uri proxy;

        public SimpleHttpGetByRangeClient(Uri baseUri, BufferManager bufferManager = null, int timeout = DEFAULT_TIMEOUT, Uri proxy = null)
        {
            this.baseUri = baseUri;

            this.proxy = proxy;
            CreateTcpClient(proxy);

            if (bufferManager == null)
            {
                bufferManager = new BufferManager(new[] { new BufferQueueSetting(BUFFER_SIZE, 1) });
            }
            this.bufferManager = bufferManager;
            this.timeout = timeout;
        }

        private void CreateTcpClient(Uri proxy)
        {
            if (proxy != null)
            {
                tcpClient = new TcpClient(proxy.DnsSafeHost, proxy.Port);
            }
            else
            {
                tcpClient = new TcpClient();
            }
        }

        public SimpleHttpResponse Get(long start, long length)
        {
            return Get(baseUri, start, length);
        }

        public SimpleHttpResponse Get(Uri uri, long start, long length)
        {
            byte[] request;
            try
            {
                EnsureConnection(uri);
                request = Encoding.UTF8.GetBytes(BuildHttpRequest(uri, start, length));
                stream.Write(request, 0, request.Length);
                return ParseResult(stream, length);
            }
            catch (IOException exc)
            {
                if (exc.Message == INVALID_HEADER_LENGTH || exc.Message.Contains("The authentication or decryption has failed"))  //sometimes mono flakes out and throws this error
                {
                    EnsureConnection(uri, true);  //rebuild the connection
                    request = Encoding.UTF8.GetBytes(BuildHttpRequest(uri, start, length));
                    stream.Write(request, 0, request.Length);
                    return ParseResult(stream, length);
                }
                else
                {
                    throw;
                }
            }

        }

        public void Dispose()
        {
            if (tcpClient != null && tcpClient.Connected)
            {
                tcpClient.Close();
            }
            if (stream != null)
            {
                stream.Dispose();
            }
        }

        public int ConnectionTimeout
        {
            get { return timeout; }
            set
            {
                if (tcpClient != null)
                {
                    tcpClient.ReceiveTimeout = value;
                }
                timeout = value;
            }
        }

        protected void EnsureConnection(Uri uri, bool forceRebuild = false)
        {
            if (forceRebuild || uri.Host != baseUri.Host || uri.Port != baseUri.Port || (tcpClient.Connected && stream == null))
            {
                if (stream != null)
                {
                    stream.Dispose();
                }
                if (forceRebuild || tcpClient.Connected)
                {
                    tcpClient.Close();
                    CreateTcpClient(proxy);

                }
                baseUri = uri;
            }

            if (!tcpClient.Connected)
            {

                tcpClient.ReceiveTimeout = timeout;
                tcpClient.Connect(baseUri.Host, baseUri.Port);
                if (baseUri.Scheme.ToLower() == "https")
                {
                    var sslStream = new SslStream(tcpClient.GetStream());
                    sslStream.AuthenticateAsClient(baseUri.Host);
                    stream = sslStream;
                }
                else
                {
                    stream = tcpClient.GetStream();
                }
            }

        }


        internal static string BuildHttpRequest(Uri uri, long start, long length)
        {
            string hostHeader;
            // see if they provided a port explicitly in the URI. If so, that must be set in the header
            // default ports must NOT be set in the header
            var port = uri.GetComponents(UriComponents.Port, UriFormat.Unescaped);
            if (string.IsNullOrEmpty(port))
                hostHeader = uri.Host;
            else
                hostHeader = uri.Host + ":" + uri.Port;
            return string.Format(REQUEST_TEMPLATE, uri.PathAndQuery, hostHeader, start, start + length - 1);
        }

        public SimpleHttpResponse ParseResult(Stream stream, long length)
        {
            var buffer = bufferManager.GetBuffer(BUFFER_SIZE);
            Dictionary<string, string> headers;
            int statusCode;
            int bodyStarts = -1;

            int bytesread = stream.Read(buffer, 0, buffer.Length);

            byte[] initialReadBytes = bufferManager.GetBuffer((uint)bytesread);
            if (bytesread < 10)
            {
                throw new IOException(INVALID_HEADER_LENGTH);
            }

            Buffer.BlockCopy(buffer, 0, initialReadBytes, 0, bytesread);

            //some calculations to determine how much data we are getting;

            int bodyIndex = initialReadBytes.IndexOf(BODY_INDICATOR);
            bodyStarts = bodyIndex + BODY_INDICATOR.Length;

            headers = HttpParser.GetHttpHeaders(initialReadBytes, bodyIndex, out statusCode);


            if (statusCode >= 200 && statusCode <= 300)
            {

                long contentLength = long.Parse(headers[HttpParser.HttpHeaders.ContentLength]);

                var dest = bufferManager.GetBuffer((uint)contentLength);
                using (var outputStream = new MemoryStream(dest))
                {
                    int destPlace = initialReadBytes.Length - bodyStarts;
                    long totalContent = contentLength + bodyStarts;
                    long left = totalContent - bytesread;

                    outputStream.Write(initialReadBytes, bodyStarts, destPlace);

                    while (left > 0)
                    {
                        // Trace.WriteLine(string.Format("reading buffer {0}", (int)(left < buffer.Length ? left : buffer.Length)));
                        bytesread = stream.Read(buffer, 0, (int)(left < buffer.Length ? left : buffer.Length));
                        outputStream.Write(buffer, 0, bytesread);
                        left -= bytesread;
                    }
                    bufferManager.FreeBuffer(buffer);
                    bufferManager.FreeBuffer(initialReadBytes);
                    return new SimpleHttpResponse(statusCode, dest, headers);
                }
            }
            bufferManager.FreeBuffer(buffer);

            return new SimpleHttpResponse(statusCode, null, headers);
        }
    }
}