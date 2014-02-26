using System;
using System.Threading;

namespace Illumina.TerminalVelocity
{
    internal class FailureToken 
    {
        public bool FailureDetected { get; private set; }        

        public void TriggerFailure()
        {
            FailureDetected = true;
        }
        
        public FailureToken()
        {
            FailureDetected = false;
        }
    }
}