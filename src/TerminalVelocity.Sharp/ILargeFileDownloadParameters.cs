using System;
using System.IO;

namespace Illumina.TerminalVelocity
{
    public interface ILargeFileDownloadParameters
    {
        Uri Uri { get; }
        string Id { get; }
        Stream GetOutputStream();
        int MaxChunkSize { get; }
        int MaxRetries { get; }
        long FileSize { get; }
        int MaxThreads { get; }
        bool AutoCloseStream { get; }
    }
}
