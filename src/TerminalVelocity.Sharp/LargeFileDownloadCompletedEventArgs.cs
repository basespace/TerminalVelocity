using System;
using System.ComponentModel;

namespace Illumina.TerminalVelocity
{
    public class LargeFileDownloadCompletedEventArgs : AsyncCompletedEventArgs
    {
        public LargeFileDownloadCompletedEventArgs(Exception error, bool cancelled, object userState) : base(error, cancelled, userState)
        {
            
        }
    }
}
