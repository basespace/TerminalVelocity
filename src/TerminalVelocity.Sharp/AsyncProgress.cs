using System;
using System.Threading;

namespace Illumina.TerminalVelocity
{
    /// <summary>
    /// Aysnch progress reporting
    /// 4.5 class, ported to 4.0 and renamed
    /// http://msdn.microsoft.com/en-us/library/hh193692.aspx
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class AsyncProgress<T> : IAsyncProgress<T> where T : EventArgs
    {
        private readonly Action<T> handler;

        private readonly SendOrPostCallback invokeHandlers;
        private SynchronizationContext synchronizationContext;


        public AsyncProgress()
        {
            AsyncProgress<T> progress = this;
            SynchronizationContext currentNoFlow = SynchronizationContext.Current;
            SynchronizationContext defaultContext = currentNoFlow;
            if (currentNoFlow == null)
            {
                defaultContext = AsyncProgressStatics.DefaultContext;
            }
            progress.synchronizationContext = defaultContext;
            invokeHandlers = InvokeHandlers;
        }


        public AsyncProgress(Action<T> handler)
            : this()
        {
            if (handler != null)
            {
                this.handler = handler;
                return;
            }
            else
            {
                throw new ArgumentNullException("handler");
            }
        }

        void IAsyncProgress<T>.Report(T value)
        {
            OnReport(value);
        }

        private void InvokeHandlers(object state)
        {
            var t = (T) state;
            Action<T> mHandler = handler;
            EventHandler<T> eventHandler = ProgressChanged;
            if (mHandler != null)
            {
                mHandler(t);
            }
            if (eventHandler != null)
            {
                eventHandler(this, t);
            }
        }


        protected virtual void OnReport(T value)
        {
            Action<T> mHandler = handler;
            EventHandler<T> eventHandler = ProgressChanged;
            if (mHandler != null || eventHandler != null)
            {
                synchronizationContext.Post(invokeHandlers, value);
            }
        }


        public event EventHandler<T> ProgressChanged;
    }

    public interface IAsyncProgress<in T> where T : EventArgs
    {
        void Report(T value);
    }

    internal static class AsyncProgressStatics
    {
        internal static readonly SynchronizationContext DefaultContext;

        static AsyncProgressStatics()
        {
            DefaultContext = new SynchronizationContext();
        }
    }
}