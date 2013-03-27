using System;
using System.IO;

namespace Illumina.TerminalVelocity.Host
{
    public class DownloadOptions
    {
        private string outputFile = null;

        public bool ShowHelp { get; set; }

        public bool IsInteractive { get; set; }

        public string OutputFile
        {
            get
            {
                if (outputFile == null && Uri != null)
                {
                    return Path.Combine(Environment.CurrentDirectory, Path.GetFileName(Uri.LocalPath));
                }
                return outputFile;
            }
            set { outputFile = value; }
        }

        public int? MaxChunkSize { get; set; }
        public int? MaxThreads { get; set; }
        public Uri Uri { get; set; }
        public bool ShouldExit { get; set; }
        public long? FileSize { get; set; }
        public Exception Exception { get; set; }

        public bool HasException
        {
            get { return Exception != null; }
        }
    }
}
