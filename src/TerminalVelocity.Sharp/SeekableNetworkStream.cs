#define PREFETCH
#define BUFFER_SEEK
using System;
using System.IO;
using System.Threading.Tasks;

namespace Illumina.TerminalVelocity
{
    /// <summary>
    /// Seekable, optimized chunked download stream based on the SimpleByteRangeClient
    /// </summary>
    public class SeekableNetworkStream : Stream, IDisposable
    {
        public const int MAX_CACHE = 5*1024*1024; // 5MB max cache
        public const int NOM_CACHE = 65536;
        public const int MIN_CACHE = 1;
        public const int TIMEOUT = 20*60*1000; // 20 minutes

        private readonly uint cacheSize;
        private readonly string url;
        private int cacheMisses;
        private uint currentCacheSize;
        private bool firstRead = true;
        private readonly long length;
        private int numReads;
        private int numSeeks;
        private Task pendingGetObject;
        private long position;
        private bool prefetchEnabled = true;
        private MemoryStream stream = new MemoryStream();

        public SeekableNetworkStream(string url, long? resourceLength = null, uint cacheSize = NOM_CACHE)
        {
            if (!resourceLength.HasValue)
            {
                length = GetSizeOfResource(new Uri(url));
            }
            else
            {
                length = resourceLength.Value;
            }
            this.url = url;
            this.cacheSize = cacheSize;
            currentCacheSize = cacheSize;
        }


        public int NumReads
        {
            get { return numReads; }
        }

        public int NumSeeks
        {
            get { return numSeeks; }
        }

        public int CacheMisses
        {
            get { return cacheMisses; }
        }

        public bool PrefetchEnabled
        {
            get { return prefetchEnabled; }
            set { prefetchEnabled = value; }
        }

        private long BytesCached
        {
            get { return stream.Length - stream.Position; }
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override long Length
        {
            get { return length; }
        }

        public override long Position
        {
            get { return position; }
            set { Seek(value, SeekOrigin.Begin); }
        }

        private bool IsEof
        {
            get { return position >= length; }
        }

        public override void Flush()
        {
            // noop
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int bytesRead = stream.Read(buffer, offset, count);
            int totalRead = bytesRead;
            position = Math.Min(length, position + bytesRead);
            while (!IsEof && totalRead < count)
            {
                RefillBuffer(count - totalRead);
                bytesRead = stream.Read(buffer, offset + totalRead, count - totalRead);
                totalRead += bytesRead;
                position = Math.Min(length, position + bytesRead);
            }
            if (!IsEof && ShouldPreFetch())
            {
                // we're going to run out! - refill
                TriggerRefillAsync(currentCacheSize);
            }
            return totalRead;
        }

        private bool ShouldPreFetch()
        {
#if PREFETCH
            return PrefetchEnabled && (stream.Position > stream.Length*0.6);
#else
			return false;
#endif
        }

        private void TriggerRefillAsync(uint amount)
        {
#if PREFETCH

            if (pendingGetObject != null || position + BytesCached == length)
                return;
            numReads++;


            pendingGetObject = new Task(() =>
                                             {
                                                 long start = position + BytesCached;
                                                 long end = Math.Min(position + BytesCached + amount, length);
                                                 GetChunk(start, end);
                                             });
            pendingGetObject.Start();
#endif
        }

        internal void GetChunk(long start, long end, uint retryCount = 2)
        {
            using (var client = new SimpleHttpGetByRangeClient(new Uri(url)))
            {
                SimpleHttpResponse response = client.Get(start, end - start);
                if (response.WasSuccessful)
                {
                    HandleResponse(response);
                }
                else if (retryCount > 0)
                {
                    //retry
                    GetChunk(start, end, --retryCount);
                }
                else
                {
                    throw new HttpException(string.Format("Could not retrieve content from {0} between {1} and {2}",
                                                          url, start, end));
                }
            }
        }

        private static long GetSizeOfResource(Uri url)
        {
            var client = new SimpleHttpGetByRangeClient(url);
            var response = client.Get(url, 0, 1);

            if (response != null)
            {
                if (response.IsStatusCodeRedirect && !String.IsNullOrWhiteSpace(response.Location))
                {
                    if (response.Location != url.AbsoluteUri)
                    {
                        url = new Uri(response.Location);
                        Console.WriteLine("Detected Redirect: " + url);
                        GetSizeOfResource(url);
                    }
                    else
                    {
                        throw new ArgumentException("Supplied Url has no source");
                    }
                }
                else if (response.WasSuccessful && response.ContentRangeLength.HasValue && response.ContentRangeLength >= 0)
                {
                    return response.ContentRangeLength.Value;
                }
                else
                {
                    throw new Exception("Response was not successful, status code: " + response.StatusCode);
                }

            }
            throw new Exception("Response was not successful, status code: unknown");
        }

        private bool FinishAsyncRead()
        {
            if (pendingGetObject != null)
            {
                try
                {
                    pendingGetObject.Wait(TimeSpan.FromSeconds(30));
                    pendingGetObject.Dispose();
                    return true;
                }
                catch
                {
                    // eat any exception. If it fails again (synchronously) it that exception will be
                    // thrown to the caller
                }
                finally
                {
                    pendingGetObject = null;
                }
            }

            return false;
        }

        private void RefillBuffer(long minLength)
        {
            if (FinishAsyncRead())
                return;
            cacheMisses++;

            // cache miss - double up
            if (!firstRead)
            {
                currentCacheSize = Math.Min(currentCacheSize*2, MAX_CACHE);
                firstRead = false;
            }


            if (minLength < currentCacheSize)
                minLength = currentCacheSize;

            long start = position;
            long end = Math.Min(position + minLength, length);
            GetChunk(start, end);
            numReads++;
        }

        private void HandleResponse(SimpleHttpResponse response)
        {
            if (BytesCached > 0)
            {
                var newStream = new MemoryStream((int) BytesCached);
                stream.CopyTo(newStream);
                stream.Dispose();
                stream = newStream;
            }
            else
            {
                stream.Position = 0;
            }
            new MemoryStream(response.Content).CopyTo(stream);
            stream.SetLength(stream.Position); // the last buffer may be smaller than the current length!
            stream.Position = 0;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            FinishAsyncRead();
            numSeeks++;
            currentCacheSize = cacheSize;

            long newPosition;
            switch (origin)
            {
                case SeekOrigin.Begin:
                    newPosition = Math.Min(length, Math.Max(0, offset));
                    break;
                case SeekOrigin.Current:
                    newPosition = Math.Max(0, Math.Min(offset + position, length));
                    break;
                case SeekOrigin.End:
                    newPosition = Math.Max(0, Math.Min(length, length - offset));
                    break;
                default:
                    throw new NotImplementedException();
            }
            long delta = newPosition - position;
            position = newPosition;
#if BUFFER_SEEK

            if (delta < 0 && stream.Position > Math.Abs(delta))
                stream.Seek(delta, SeekOrigin.Current);
            else if (delta > 0 && (stream.Length - stream.Position) > delta)
                stream.Seek(delta, SeekOrigin.Current);
            else if (delta != 0)
            {
                stream.Position = 0;
                stream.SetLength(0);
                TriggerRefillAsync(cacheSize);
            }
#else
			_stream.Position = 0;
			_stream.SetLength(0);
#endif

            return position;
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);


            if (stream != null)
            {
                if (disposing)
                {
                    FinishAsyncRead();
                    stream.Dispose();
                }
                stream = null;
            }
        }
    }
}