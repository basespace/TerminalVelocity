﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Illumina.TerminalVelocity
{
    public static class DownloaderExtensions
    {
        public static Task DownloadAsync(this ILargeFileDownloadParameters parameters,
                                         CancellationToken? cancellationToken = null,
                                         IAsyncProgress<LargeFileDownloadProgressChangedEventArgs> progress = null,
                                         Action<string> logger = null, BufferManager bufferManager = null)
        {
            CancellationToken ct = (cancellationToken != null) ? cancellationToken.Value : CancellationToken.None;
            FailureToken ft = new FailureToken();
            Task task = Task.Factory.StartNew(() => Downloader.StartDownloading(ct, ft, parameters, progress, logger), ct);

            return task;
        }
    }
    
    public class Downloader
    {
        public Thread DownloadWorkerThread { get; set; }
        public DateTime HeartBeat { get; set; }

        internal bool NonRetryableError { get; set; }

        // needed for Unit Testing
        internal bool SimulateTimedOut { get; set; }
        // needed for Unit Testing

        internal static void StartDownloading(CancellationToken ct,  FailureToken ft,
            ILargeFileDownloadParameters parameters, 
            IAsyncProgress<LargeFileDownloadProgressChangedEventArgs> progress = null,
            Action<string> logger = null,
            BufferManager bufferManager = null)
        {
            //create the file
            Stream stream = parameters.GetOutputStream();
            if (parameters.FileSize == 0) // Teminate Zero size files
            {
                if (progress != null)
                {
                    progress.Report(new LargeFileDownloadProgressChangedEventArgs(100, 0, 0,
                                                                                  parameters.FileSize,
                                                                                  parameters.FileSize, "",
                                                                                  "",
                                                                                  null));
                }
                if (parameters.AutoCloseStream)
                    stream.Close();
                return;
            }

            //figure out number of chunks
            int chunkCount = GetChunkCount(parameters.FileSize, parameters.MaxChunkSize);
            int numberOfThreads = Math.Min(parameters.MaxThreads, chunkCount);
            logger = logger ?? ((s) => { });


            var downloadWorkers = new List<Downloader>(numberOfThreads);

            bool isFailed = false;
            long totalBytesWritten = 0;
            double byteWriteRate = 0.0;
            try
            {

                int writtenChunkZeroBased = 0;
                var readStack = new ConcurrentStack<int>();

                //add all of the chunks to the stack
                readStack.PushRange(Enumerable.Range(0, chunkCount).Reverse().ToArray());

                var writeQueue = new ConcurrentQueue<ChunkedFilePart>();

                // ReSharper disable AccessToModifiedClosure
                Func<int, bool> downloadThrottle = (int c) => writeQueue.Count > 30;
                // ReSharper restore AccessToModifiedClosure
                if (bufferManager == null)
                {

                    bufferManager = new BufferManager(new[]
                    {
                        new BufferQueueSetting(SimpleHttpGetByRangeClient.BUFFER_SIZE, (uint) numberOfThreads),
                        new BufferQueueSetting((uint) parameters.MaxChunkSize, (uint) numberOfThreads)
                    });
                }
                
                int expectedChunkDownloadTime = ExpectedDownloadTimeInSeconds(parameters.MaxChunkSize);

                for (int i = 0; i < numberOfThreads; i++)
                {
                    downloadWorkers.Add(new Downloader(bufferManager, parameters, writeQueue, readStack,
                                        downloadThrottle, expectedChunkDownloadTime, ft, logger, ct));
                }
                //start all the download threads
                downloadWorkers.ForEach(x => x.Start());

                var watch = new System.Diagnostics.Stopwatch();
                watch.Start();
                long oldElapsedMilliSeconds = watch.ElapsedMilliseconds;
                long lastPointInFile = 0;                
                int kc = 0;
                //start the write loop
                while (writtenChunkZeroBased < chunkCount && !ct.IsCancellationRequested && !ft.FailureDetected)
                {
                    ChunkedFilePart part;
                    while (writeQueue.TryDequeue(out part) && !ft.FailureDetected)
                    {
                        //retry?
                        logger(string.Format("[{1}] writing: {0}", writtenChunkZeroBased, parameters.Id));
                        stream.Position = part.FileOffset;
                        stream.Write(part.Content, 0, part.Length);
                        totalBytesWritten += part.Length;
                        bufferManager.FreeBuffer(part.Content);
                        if (progress != null)
                        {
                            var elapsed = watch.ElapsedMilliseconds;
                            var diff = elapsed - oldElapsedMilliSeconds;
                            if (diff > 2000)
                            {
                                long bytesDownloaded = (long) writtenChunkZeroBased * parameters.MaxChunkSize;
                                long interimReads = bytesDownloaded + part.Length - lastPointInFile;
                                byteWriteRate = (interimReads / (diff / (double)1000));                                

                                lastPointInFile += interimReads;                                
                                oldElapsedMilliSeconds = elapsed;
                                progress.Report(new LargeFileDownloadProgressChangedEventArgs(ComputeProgressIndicator(totalBytesWritten, parameters.FileSize),
                                                                                              byteWriteRate, byteWriteRate, totalBytesWritten, totalBytesWritten, "", "", null));
                            }
                        }
                        writtenChunkZeroBased++;
                    }

                    // kill hanged workers
                    var timedOutWorkers = downloadWorkers
                        .Where(w => w.Status == ThreadState.Running || w.Status == ThreadState.WaitSleepJoin)
                        .Where((w) =>
                           {
                               if (w.SimulateTimedOut)
                                   return true;
                               return w.HeartBeat.AddSeconds(expectedChunkDownloadTime) < DateTime.Now;
                           })
                   .ToList();

                    if (timedOutWorkers.Any())
                    {
                        foreach (var worker in timedOutWorkers)
                        {
                            try
                            {
                                worker.DownloadWorkerThread.Abort(); // this has a minute chance of throwing
                                logger(string.Format("[{1}] killing thread as it timed out {0}", kc++, parameters.Id));
                                if (worker.SimulateTimedOut)
                                    Thread.Sleep(3000); // introduce delay for unit test to pick-up the condition
                            }
                            catch (Exception ex)
                            { }
                        }
                    }

                    var activeWorkers = downloadWorkers.Where(x => x != null &&
                        (x.Status == ThreadState.Running
                        || x.Status == ThreadState.WaitSleepJoin)).ToList();
                    // respawn the missing workers if some had too many retries or were killed

                    //wait for something that was added
                    Thread.Sleep(100); 
                    if (activeWorkers.Count() < numberOfThreads)
                    {
                        for (int i = 0; i < numberOfThreads; i++)
                        {
                            if (downloadWorkers[i] == null)
                            {
                                logger(string.Format("[{0}]" + " reviving killed thread", parameters.Id));
                                downloadWorkers[i] = new Downloader(bufferManager, parameters, writeQueue, readStack,
                                downloadThrottle, expectedChunkDownloadTime, ft, logger, ct);
                                downloadWorkers[i].Start();
                                continue;
                            }

                            if (downloadWorkers[i].Status == ThreadState.Running
                                || downloadWorkers[i].Status == ThreadState.WaitSleepJoin
                                || downloadWorkers[i].Status == ThreadState.Background
                                || downloadWorkers[i].Status == ThreadState.Stopped) continue;

                            logger(string.Format("[{0}] reviving killed thread", parameters.Id));
                            downloadWorkers[i] = new Downloader(bufferManager, parameters, writeQueue, readStack,
                                                                downloadThrottle, expectedChunkDownloadTime, ft, logger, ct);
                            downloadWorkers[i].Start();
                        }
                    }

                }

                if (ft.FailureDetected)
                {
                    throw new Exception(String.Format("[{0}]A Non Retryable Failure was reported by one or more of the downloadworkers", parameters.Id));
                }
            }
            catch (Exception e)
            {
                // Report Failure
                isFailed = true;
                logger(string.Format("[{0}] Exception: TerminalVelocity Downloading failed", parameters.Id));
                logger(string.Format("[{0}] Message: {1} ", parameters.Id, e.Message));
                logger(string.Format("[{0}] StackTrace: {1}", parameters.Id, e.StackTrace));
                if (progress != null)
                {
                    progress.Report(new LargeFileDownloadProgressChangedEventArgs(ComputeProgressIndicator(totalBytesWritten, parameters.FileSize), 0, 0, totalBytesWritten, totalBytesWritten, "", "", null, isFailed, e.Message));
                }
            }
            finally
            {
                //kill all the tasks if exist
                if (downloadWorkers != null)
                {
                    downloadWorkers.ForEach(x =>
                    {
                        if (x == null) return;

                        ExecuteAndSquash(x.Dispose);
                    });
                }
                if (parameters.AutoCloseStream)
                {                    
                    if (progress != null)
                    {
                        progress.Report(new LargeFileDownloadProgressChangedEventArgs(ComputeProgressIndicator(totalBytesWritten, parameters.FileSize), byteWriteRate, byteWriteRate, totalBytesWritten, totalBytesWritten, "", "", null, isFailed));
                    }
                    logger(string.Format("[{0}] AutoClosing stream", parameters.Id));
                    stream.Close();
                }
            }
        }

        public void Start()
        {
            DownloadWorkerThread.Start();
        }
        public void Wait(int time)
        {
            DownloadWorkerThread.Join(time);
        }
        public void Wait(TimeSpan time)
        {
            DownloadWorkerThread.Join(time);
        }
        public void Dispose()
        {
            DownloadWorkerThread.Abort();
            DownloadWorkerThread = null;
        }
        public ThreadState Status
        {
            get
            {
                return DownloadWorkerThread.ThreadState;
            }
        }

        public static int ComputeProgressIndicator(long bytesWritten, long fileSize)
        {
            return (int)((fileSize!=0)?((bytesWritten / (double)fileSize) * 100.0):100);
            //if (chunkCount == 1)
            //{
            //    return 100;
            //}
            //return 1 + 99 * zeroBasedChunkNumber / (chunkCount - 1);

        }

        internal Downloader(BufferManager bufferManager, 
                            ILargeFileDownloadParameters parameters,
                            ConcurrentQueue<ChunkedFilePart> writeQueue,
                            ConcurrentStack<int> readStack,
                            Func<int, bool> downloadThrottle, 
                            int expectedChunkTimeInSeconds,
                            FailureToken failureToken,
                            Action<string> logger = null,
                            CancellationToken? cancellation = null,                             
                            Func<ILargeFileDownloadParameters, 
                            ISimpleHttpGetByRangeClient> clientFactory = null)
        {
            SimulateTimedOut = false;
            NonRetryableError = false;
            HeartBeat = DateTime.Now;
            cancellation = (cancellation != null) ? cancellation.Value : CancellationToken.None;

            DownloadWorkerThread = new Thread((() =>
                                 {
                                     try
                                     {
                                     clientFactory = clientFactory ?? ((p) => new SimpleHttpGetByRangeClient(p.Uri, bufferManager, expectedChunkTimeInSeconds * 1000 ));

                                     logger = logger ?? ((s) => { });

                                     ISimpleHttpGetByRangeClient client = clientFactory(parameters);
                                     int currentChunk;
                                     readStack.TryPop(out currentChunk);
                                     int delayThrottle = 0;

                                     try
                                         {

                                             while (currentChunk >= 0 && !cancellation.Value.IsCancellationRequested && !failureToken.FailureDetected) //-1 when we are done
                                             {
                                                 logger(string.Format("[{1}]downloading: {0}", currentChunk,parameters.Id));
                                                 SimpleHttpResponse response = null;
                                                  var part = new ChunkedFilePart();
                                                 part.FileOffset =  GetChunkStart(currentChunk, parameters.MaxChunkSize);
                                                 part.Length = GetChunkSizeForCurrentChunk(parameters.FileSize,
                                                                                           parameters.MaxChunkSize,
                                                                                           currentChunk);
                                                 try
                                                 {
                                                     response = client.Get(parameters.Uri, part.FileOffset, part.Length);
                                                 }
                                                 catch (Exception e)
                                                 {
                                                     logger(string.Format("[{0}] {1}", parameters.Id, (e.InnerException != null
                                                             ? e.InnerException.Message
                                                             : e.Message)));

                                                     ExecuteAndSquash(client.Dispose);
                                                     client = clientFactory(parameters);
                                                 }

                                                 if (response != null && response.WasSuccessful)
                                                 {

                                                     part.Content = response.Content;
                                                     writeQueue.Enqueue(part);

                                                     // reset the throttle when the part is finally successful
                                                     delayThrottle = 0;
                                                     logger(string.Format("[{1}] downloaded: {0}", currentChunk,parameters.Id));

                                                     HeartBeat = DateTime.Now;

                                                     if (!readStack.TryPop(out currentChunk))
                                                     {
                                                         currentChunk = -1;
                                                     }
                                                     while (downloadThrottle(currentChunk))
                                                     {
                                                         logger(string.Format("[{1}] throttling for chunk: {0}", currentChunk,parameters.Id));
                                                         if (!cancellation.Value.IsCancellationRequested && !failureToken.FailureDetected)
                                                         {
                                                             Thread.Sleep(500);
                                                         }
                                                     }
                                                 }
                                                 else if (response == null || response.IsStatusCodeRetryable)
                                                 {
                                                     int sleepSecs = Math.Min((int)Math.Pow(4.95, delayThrottle), 600);
                                                     logger(string.Format("[{2}] sleeping: {0}, {1}s", currentChunk, sleepSecs, parameters.Id));
                                                     if (!cancellation.Value.IsCancellationRequested && !failureToken.FailureDetected)
                                                     {
                                                         Thread.Sleep(sleepSecs * 1000); // 4s, 25s, 120s, 600s
                                                         delayThrottle++;
                                                     }
                                                 }
                                                 else
                                                 {
                                                     logger(String.Format("[{3}] parameters.Uri:{0}  part.FileOffset:{1} part.Length:{2}", parameters.Uri, part.FileOffset, part.Length, parameters.Id));
                                                     logger(string.Format("[{1}] ERROR!NonRetryableError! going to trigger download failure because got Response.StatusCode: {0}", response.StatusCode, parameters.Id));
                                                     NonRetryableError = true;
                                                     failureToken.TriggerFailure();
                                                     break;
                                                     //throw new SimpleHttpClientException(response);
                                                 }
                                             }
                                         }
                                         finally
                                         {
                                             if (currentChunk >= 0  /*&& !NonRetryableError*/)
                                             {
                                                 //put it back on the stack, if it's poison everyone else will die
                                                 readStack.Push(currentChunk);
                                             }
                                             if (client != null)
                                             {
                                                 ExecuteAndSquash(client.Dispose);
                                             }
                                         }
                                     logger(String.Format("[{1}] Thread {0} done", Thread.CurrentThread.ManagedThreadId, parameters.Id));
                                     }
                                     catch (ThreadAbortException exc)
                                     {
                                         Console.WriteLine(exc);
                                         Thread.Sleep(1000);
                                         throw;
                                     }
                                 }));

        }

        internal static void ExecuteAndSquash( Action a)
        {
            try
            {
                a();
            }
            catch 
            {
            }
        }
        
        internal static int ExpectedDownloadTimeInSeconds(int chunkSizeInBytes)
        {
            //so lets say 128Kbps per second with no overhead takes 62 seconds for 1MB 64kb = 16384bytes
            //20% latency
            //1024bytes in a KB
            //1048576 bytes in a MB

            int raw = chunkSizeInBytes / 16384; 
            return (int) Math.Max(raw * 1.25, 10);  //add 25% overhead, minimum 10 sec
        }


        internal static long GetChunkStart(int currentChunk, int maxChunkSize)
        {
            return currentChunk*(long)maxChunkSize;
        }

        internal static int GetChunkCount(long fileSize, long chunkSize)
        {
            if (chunkSize == 0)
                return 0;

            // from Euclid: length = chunkSize * chunkCount + remainder
            // where remainder = length % chunkSize
            // the trick is that from the remainder formula, we need length 
            // to be zero-based and chunkSize to be one-based
            // also the remainder formula returns a zero-based number.
            // from here you can infer that chunkCount must be zero-based.
            int chunkCount = (int) (((fileSize - 1) - (fileSize - 1) % chunkSize) / chunkSize);
            return 1 + chunkCount;
        }

        internal static int GetChunkSizeForCurrentChunk(long fileSize, int maxChunkSize, int zeroBasedChunkNumber)
        {
            int chunkCount = GetChunkCount(fileSize, maxChunkSize);

            if (zeroBasedChunkNumber + 1 < chunkCount)
            {
                return maxChunkSize;
            }

            if (zeroBasedChunkNumber >= chunkCount)
            {
                return 0;
            }

            var remainder = (int) (fileSize%maxChunkSize);
            return remainder > 0 ? remainder : maxChunkSize;
        }
    }
}