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

        private static bool GetIsValid(int builderId)
        {
            lock (ActiveBuffersLock)
            {
                return ActiveBuffers.ContainsKey(builderId);
            }
        }

        private static void BakeJob(int builderId, int jobId)
        {
            Buffer buffer;

            lock (ActiveBuffersLock)
            {
                if (!ActiveBuffers.TryGetValue(builderId, out buffer)) return;

                CreateJob(buffer, jobId);

                if (buffer.IsPreserved) return;

                ActiveBuffers.Remove(builderId);
            }

            buffer.Delay = 0;
            buffer.IsPreserved = false;
            buffer.Action = null;
            Buffers.Enqueue(buffer);
        }

        private static void Break(int builderId)
        {
            Buffer buffer;

            lock (ActiveBuffersLock)
            {
                if (!ActiveBuffers.Remove(builderId, out buffer)) return;
            }

            buffer.Delay = 0;
            buffer.IsPreserved = false;
            buffer.Action = null;
            Buffers.Enqueue(buffer);
        }

        public class Buffer
        {
            private Buffer()
            {
            }

            public static void Create(int id, Action action)
            {
                lock (ActiveBuffersLock)
                {
                    if (!Buffers.TryDequeue(out var buffer)) buffer = new Buffer();
                    ActiveBuffers[id] = buffer;
                    buffer.Action = action;
                }
            }
            
            public float Delay;
            public bool IsProtected;
            public bool IsPreserved;
            public Action Action;
        }
    }
}