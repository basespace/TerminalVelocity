using System;
using System.IO;

namespace Illumina.TerminalVelocity
{
   

    public class LargeFileDownloadParameters : ILargeFileDownloadParameters
    {
        private string outputFile;
        private readonly Lazy<FileStream> fileStream = null;
        public const int DEFAULT_MAX_CHUNK_SIZE = 5242880;

        public LargeFileDownloadParameters(Uri uri, string outputFile, long fileSize,
                                           int? maxThreads, int? maxChunkSize,  string id = null ): this(uri, outputFile, fileSize, id, maxThreads ?? 16, maxChunkSize ?? DEFAULT_MAX_CHUNK_SIZE)
        {

        }

        public LargeFileDownloadParameters(Uri uri, string outputFile, long fileSize, string id = null, int maxThreads = 16, int maxChunkSize = DEFAULT_MAX_CHUNK_SIZE, bool autoCloseStream = true)
        {
            Uri = uri;
            if (string.IsNullOrWhiteSpace(id))
            {
                Id = Guid.NewGuid().ToString().Replace("-", "");
            }
            this.outputFile = outputFile;
            FileSize = fileSize;
            MaxChunkSize = fileSize > maxChunkSize ? maxChunkSize : (int)fileSize;
            MaxRetries = 3;
            fileStream = new Lazy<FileStream>(() => new FileStream(outputFile, FileMode.OpenOrCreate));
            MaxThreads = maxThreads;
            AutoCloseStream = autoCloseStream;
        }
        public string OutputFile { get; private set; }
        public Uri Uri { get; private set; }
        public string Id { get; private set; }
        public bool AutoCloseStream { get; private set; }

        public Stream GetOutputStream()
        {
            return fileStream.Value;
        }

        public int MaxChunkSize { get; private set; }
        public int MaxRetries { get; private set; }
        public long FileSize { get; private set; }
        public int MaxThreads { get; private set; }
    }

    

}
