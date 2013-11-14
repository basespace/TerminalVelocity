using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Illumina.TerminalVelocity
{
    public class BufferQueueSetting
    {
        public BufferQueueSetting(uint chunkSize, uint poolSize = 10)
        {
            ChunkSize = chunkSize;
            InitialPoolSize = poolSize;
        }
        public uint ChunkSize { get;private set; }
        public uint InitialPoolSize { get; private set; }
    }

    public class BufferQueue : ConcurrentQueue<byte[]>
    {
        
    }

    public class BufferManager : IDisposable
    {   
        private readonly ConcurrentDictionary<uint, BufferQueue> queues = new ConcurrentDictionary<uint, BufferQueue>();
        public static readonly byte[] EmptyBuffer = new byte[0];

        /// <summary>
        /// The class that controls the allocating and deallocating of all byte[] buffers used in the engine.
        /// </summary>
        public BufferManager(IEnumerable<BufferQueueSetting> buffers)
        {
            
            this.AllocateBuffers(buffers);
        }

        /// <summary>
        /// Allocates an existing buffer from the pool
        /// </summary>
        /// <param name="buffer">The byte[]you want the buffer to be assigned to</param>
        /// <param name="type">The type of buffer that is needed</param>
        private void GetBuffer(ref byte[] buffer, uint chunkSize)
        {
            // We check to see if the buffer already there is the empty buffer. If it isn't, then we have
            // a buffer leak somewhere and the buffers aren't being freed properly.
            if (buffer != EmptyBuffer)
                throw new Exception("The old Buffer should have been recovered before getting a new buffer");

            // If we're getting a small buffer and there are none in the pool, just return a new one.
            // Otherwise return one from the pool.
            BufferQueue queue;
            if (queues.TryGetValue(chunkSize, out queue))
            {
                if (queue.Count == 0)
                {
                    AllocateBuffers(new []{new BufferQueueSetting(chunkSize, 5) });
                }
               bool success = queues[chunkSize].TryDequeue(out buffer);
                if (success)
                    return;
            }
            buffer = new byte[chunkSize];
            
        }


        public byte[] GetBuffer(uint minCapacity)
        {
            var buffer = EmptyBuffer;
            GetBuffer(ref buffer, minCapacity);
            return buffer;
        }
        

        public void FreeBuffer(byte[] buffer, bool clearContent = false)
        {
            FreeBuffer(ref buffer);
        }

        public void FreeBuffer(ref byte[] buffer, bool clearContent = false)
        {
            if (buffer == EmptyBuffer)
                return;

             BufferQueue queue;
             if (queues.TryGetValue((uint)buffer.Length, out queue))
            {
                if (clearContent)
                {
                    Array.Clear(buffer, 0, buffer.Length);
                }
                queue.Enqueue(buffer);
            }
            
            buffer = EmptyBuffer; // After recovering the buffer, we send the "EmptyBuffer" back as a placeholder
        }


        private void AllocateBuffers(IEnumerable<BufferQueueSetting> settings)
        {
            foreach (var setting in settings)
            {
                BufferQueue queue;
                if (queues.ContainsKey(setting.ChunkSize))
                {
                    queue = queues[setting.ChunkSize];
                }
                else
                {
                    queue = new BufferQueue();
                    queues.AddOrUpdate(setting.ChunkSize, queue, (i, bufferQueue) => bufferQueue);
                }
                for (int i = 0; i < setting.InitialPoolSize; i++)
                {
                    queue.Enqueue(new byte[setting.ChunkSize]);
                }
               
            }
        }

        public void Dispose()
        {
            //let's null out the bytearrays 
            foreach (var queue in queues)
            {
                for (int i = 0; i < queue.Value.Count; i++)
                {
                    byte[] x;
                    if (queue.Value.TryDequeue(out x))
                    {
                        x = null;
                    }
                }
            }
        }
    }
}
