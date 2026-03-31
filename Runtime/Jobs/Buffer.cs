using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Exerussus.MainThreadBridgeFeature
{
    public static partial class MainThreadBridge
    {
        private static readonly ConcurrentQueue<Buffer> Buffers = new();
        private static readonly Dictionary<int, Buffer> ActiveBuffers = new();
        private static readonly object ActiveBuffersLock = new();

        private static void SetDelay(int builderId, float delay)
        {
            lock (ActiveBuffersLock)
            {
                if (!ActiveBuffers.TryGetValue(builderId, out var buffer)) return;
                buffer.Delay = delay;
            }
        }

        private static void SetProtection(int builderId)
        {
            lock (ActiveBuffersLock)
            {
                if (!ActiveBuffers.TryGetValue(builderId, out var buffer)) return;
                buffer.IsProtected = true;
            }
        }

        private static void SetPreserve(int builderId)
        {
            lock (ActiveBuffersLock)
            {
                if (!ActiveBuffers.TryGetValue(builderId, out var buffer)) return;
                buffer.IsPreserved = true;
            }
        }

        private static bool IsPreserved(int builderId)
        {
            lock (ActiveBuffersLock)
            {
                if (!ActiveBuffers.TryGetValue(builderId, out var buffer)) return false;
                return buffer.IsPreserved;
            }
        }

        private static bool GetIsValid(int builderId)
        {
            lock (ActiveBuffersLock)
            {
                return ActiveBuffers.ContainsKey(builderId);
            }
        }

        private static void BakeJob(int builderId, int jobId, Action action)
        {
            Buffer buffer;

            lock (ActiveBuffersLock)
            {
                if (!ActiveBuffers.TryGetValue(builderId, out buffer)) return;
                
                CreateJob(buffer, jobId, action);

                if (buffer.IsPreserved) return;

                ActiveBuffers.Remove(builderId);
            }

            buffer.Release();
        }

        private static void BakeJob<T>(int builderId, int jobId, T context, Action<T> action)
        {
            Buffer buffer;

            lock (ActiveBuffersLock)
            {
                if (!ActiveBuffers.TryGetValue(builderId, out buffer)) return;
                
                CreateJob(buffer, jobId, context, action);

                if (buffer.IsPreserved) return;

                ActiveBuffers.Remove(builderId);
            }

            buffer.Release();
        }

        private static void Break(int builderId)
        {
            Buffer buffer;

            lock (ActiveBuffersLock)
            {
                if (!ActiveBuffers.Remove(builderId, out buffer)) return;
            }

            buffer.Release();
        }
        
        internal class Buffer
        {
            private Buffer()
            {
            }

            public static void Create(int id)
            {
                lock (ActiveBuffersLock)
                {
                    if (!Buffers.TryDequeue(out var buffer)) buffer = new Buffer();
                    buffer.Id = id;
                    ActiveBuffers[id] = buffer;
                }
            }
            
            public int Id { get; set; }
            public float Delay { get; set; }
            public bool IsProtected { get; set; }
            public bool IsPreserved { get; set; }

            public void Release()
            {
                Id = 0;
                Delay = 0;
                IsProtected = false;
                IsPreserved = false;
                Buffers.Enqueue(this);
            }
        }
    }
}