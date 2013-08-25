using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Illumina.TerminalVelocity
{
  
    /// <summary>
    /// Allows the caller to specify a stream.  This stream may not be optimized
    /// </summary>
    [Obsolete("Best performance is achieved when TV is allowed to create the file", false)]
    public class LargeFileDownloadWithStreamParameters : ILargeFileDownloadParameters
    {

      
        public const int DEFAULT_MAX_CHUNK_SIZE = 5242880;
        private Stream stream;

        public LargeFileDownloadWithStreamParameters(Uri uri, Stream output, long fileSize, string id = null, int maxThreads = 16, int maxChunkSize = DEFAULT_MAX_CHUNK_SIZE, bool autoCloseStream = true, bool verifyLength = true)
        {
            Uri = uri;
            if (output == null)
            {
                throw new ArgumentNullException("output", "stream cannot be null");
            }
            if (string.IsNullOrWhiteSpace(id))
            {
                Id = Guid.NewGuid().ToString().Replace("-", "");
            }

            MaxChunkSize = fileSize > maxChunkSize ? maxChunkSize : (int)fileSize;
            MaxRetries = 3;
            stream = output;
            MaxThreads = maxThreads;
            AutoCloseStream = autoCloseStream;
            FileSize = verifyLength ? GetFileSizeFromSource(uri) : fileSize;

        }

      
        public Uri Uri { get; private set; }
        public string Id { get; private set; }
        public bool AutoCloseStream { get; private set; }

        public Stream GetOutputStream()
        {
            return stream;
        }

        public int MaxChunkSize { get; private set; }
        public int MaxRetries { get; private set; }
        public long FileSize { get; private set; }
        public int MaxThreads { get; private set; }

        protected static long GetFileSizeFromSource(Uri uri)
        {
            return uri.GetContentLength();
        }

    }
}
