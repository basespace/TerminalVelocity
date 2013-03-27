using System;
using System.Diagnostics;
using System.Net.Sockets;

namespace Illumina.TerminalVelocity
{
  
    internal static class SimpleHttpClientRetryLogic
    {
       
        public static Func<SimpleHttpResponse, bool> GenericRetryHandler = (wse) => wse.IsStatusCodeRetryable;

        
        public static void DoWithRetry(uint maxAttempts, string description, Func<SimpleHttpResponse> op, Action<string, bool> logger,
                                       double retryIntervalBaseSecs = 5, Action error = null,
                                        Func<SimpleHttpResponse, bool> retryHandler = null)
        {

            retryIntervalBaseSecs = Math.Max(1, retryIntervalBaseSecs);
            var triesLeft = maxAttempts;
            int whichAttempt = 0;
            Exception ex = null;
            if (retryHandler == null)
                retryHandler = GenericRetryHandler;

            var timer = new Stopwatch();

            while (triesLeft-- > 0)
            {
                timer.Start();
                var delay = (int)Math.Min(1800, Math.Pow(retryIntervalBaseSecs, whichAttempt++));

                try
                {

                    logger(string.Format("operation starting: {0} attempt {1}", description, whichAttempt), false);
                    var response = op();
                    if (!response.WasSuccessful)
                    {
                        bool allowRetry = retryHandler(response);
                        if (
                            !HandleException(description, logger, allowRetry, whichAttempt, delay, "Failed Response",
                                             null, response.StatusCode, timer))
                            throw new SimpleHttpClientException(response);
                    }
                    timer.Stop();
                    logger(
                        string.Format("{0} completed after {1} attempts and {2}ms", description, whichAttempt,
                                      timer.ElapsedMilliseconds), false);
                    return;
                }
                catch (SocketException exc)
                {
                    var errorCode = (SocketErrorCodes) exc.ErrorCode;
                    bool allowRetry = errorCode.RecoverableErrors();
                    if (
                        !HandleException(description, logger, allowRetry, whichAttempt, delay, exc.Message, exc,
                                         exc.ErrorCode, timer))
                        throw;

                    ex = exc;
                }
                catch (SimpleHttpClientException)
                {
                    throw;
                }
                catch (OperationCanceledException)
                {
                    timer.Stop();
                    logger(
                        string.Format("Operation canceled exception: {0}, do not retry, ran for {1}ms", description,
                                      timer.ElapsedMilliseconds), false);
                    return;
                }
                catch (Exception exc)
                {
                    HandleException(description, logger, true, whichAttempt, delay, exc.ToString(), exc, 0, timer);
                    ex = exc;
                }
            }
            if (ex != null)
            {
                if (timer.IsRunning)
                {
                    timer.Stop();
                }
                if (error != null)
                    error();
                logger(string.Format("Maximum retries exceeded, total time {0}ms", timer.ElapsedMilliseconds), true);
                throw ex;
            }
        }

        private static bool HandleException(string description,Action<string, bool> logger, bool allowRetry, int whichAttempt, int delay,
                                           string message, Exception exc, int statusCode, Stopwatch timer)
        {
            if (allowRetry)
            {
                logger(string.Format("Error while {0}, attempt {1}, elapsed {4}ms, retrying in {2} seconds: \r\n{3}", description,
                                   whichAttempt, delay, message, timer.ElapsedMilliseconds), true);
                System.Threading.Thread.Sleep(1000 * delay);
                return true;
            }
            timer.Stop();
            logger(string.Format("HTTP Response code {0} : {1}, elapsed time {2}ms", statusCode, exc, timer.ElapsedMilliseconds), true);
            return false;
        }
    }
}
