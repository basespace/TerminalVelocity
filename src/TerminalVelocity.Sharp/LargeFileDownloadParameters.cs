using System;
using System.IO;

namespace Illumina.TerminalVelocity
{
   

    public class LargeFileDownloadParameters : ILargeFileDownloadParameters
    {
       
        private readonly Lazy<FileStream> fileStream = null;
        public const int DEFAULT_MAX_CHUNK_SIZE = 5242880;

        public LargeFileDownloadParameters(Uri uri, string outputFile, 
                                          int? maxThreads, int? maxChunkSize, string id = null)
            : this(uri, outputFile, 0, id, maxThreads ?? 16, maxChunkSize ?? DEFAULT_MAX_CHUNK_SIZE, true)
        {

        }

        public LargeFileDownloadParameters(Uri uri, string outputFile, long fileSize,
                                           int? maxThreads, int? maxChunkSize,  string id = null ): this(uri, outputFile, fileSize, id, maxThreads ?? 16, maxChunkSize ?? DEFAULT_MAX_CHUNK_SIZE)
        {

        }

        public LargeFileDownloadParameters(Uri uri, string outputFile, long fileSize, string id = null, int maxThreads = 16, int maxChunkSize = DEFAULT_MAX_CHUNK_SIZE, bool autoCloseStream = true, bool verifyLength = true)
        {
            Uri = uri;
            if (string.IsNullOrWhiteSpace(id))
            {
                Id = Guid.NewGuid().ToString().Replace("-", "");
            }

            OutputFile = outputFile;
            MaxChunkSize = fileSize > maxChunkSize ? maxChunkSize : (int)fileSize;
            MaxRetries = 3;
            fileStream = new Lazy<FileStream>(() => CreateOutputStream(outputFile, fileSize));
            MaxThreads = maxThreads;
            AutoCloseStream = autoCloseStream;
            FileSize = verifyLength ? GetFileSizeFromSource(uri) : fileSize;

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

        protected static long GetFileSizeFromSource(Uri uri)
        {
            return uri.GetContentLength();
        } 

        protected static FileStream CreateOutputStream(string filePath, long fileLength, bool deleteIfExists = false)
        {
           
            if ((File.Exists(filePath) && deleteIfExists) || !File.Exists(filePath))
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                string directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }
            }
            //first try to create a sparse file (only important on windows, this will be ignored elsewhere
            SparseFile.CreateSparse(filePath, fileLength);
           
            var stream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
           //we are pre-allocating hence the above call to SparseFile
             stream.SetLength(fileLength);
            return stream;
        }
    }

    

}
