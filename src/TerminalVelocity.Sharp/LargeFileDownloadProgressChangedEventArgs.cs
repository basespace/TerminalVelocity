using System.ComponentModel;

namespace Illumina.TerminalVelocity
{

    public class LargeFileDownloadProgressChangedEventArgs : ProgressChangedEventArgs
    {
        public LargeFileDownloadProgressChangedEventArgs(int progressPercentage, object userState, bool isSuccess = false)
            : base(progressPercentage, userState)
        {
            IsFailed = isSuccess;
        }

        public LargeFileDownloadProgressChangedEventArgs(int progressPercentage, double downloadBitRate, double writeBitRate, long bytesWritten, long bytesDownloaded, string url, string id, object userState, bool isFailed = false)
            : base(progressPercentage, userState)
        {
            this.DownloadBitRate = downloadBitRate;
            this.WriteBitRate = writeBitRate;
            this.BytesWritten = bytesWritten;
            this.BytesDownloaded = bytesDownloaded;
            this.Url = url;
            this.Id = id;
            IsFailed = isFailed;
        }

        #region Properties

        /// <summary>
        /// The instantaneous bit rate of the download in bits per second.
        /// </summary>
        public double DownloadBitRate { get; private set; }

        /// <summary>
        /// The instantaneous bit rate of the write in bits per second.
        /// </summary>
        public double WriteBitRate { get; private set; }

        /// <summary>
        /// Number of Bytes Written to Disk
        /// </summary>
        public long BytesWritten { get; private set; }

        /// <summary>
        /// Number of Bytes downloaded
        /// </summary>
        public long BytesDownloaded { get; private set; }


        /// <summary>
        /// The file url associated with this download.
        /// </summary>
        public string Url { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public string Id { get; private set; }

        /// <summary>
        /// Download currently successful (if false then the download has failed)
        /// </summary>
        public bool IsFailed { get; private set; }

        #endregion
    }
}