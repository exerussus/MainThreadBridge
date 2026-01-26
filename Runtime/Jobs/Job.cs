using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Exerussus.MainThreadBridgeFeature
{
    public static partial class MainThreadBridge
    {
        private static readonly Dictionary<int, Job> ToCreate = new();
        private static readonly Dictionary<int, Job> ToWait = new();
        private static readonly ConcurrentQueue<Job> Jobs = new();
        private static readonly object JobsLock = new();

        internal class Job
        {
            private Job() { }

            public static Job Create(int id)
            {
                if (!Jobs.TryDequeue(out var job)) job = new();
                job.Id = id;
                return job;
            }

            public static void Release(Job job)
            {
                job.Id = 0;
                job.EndTime = 0;
                job.IsProtected = false;
                Jobs.Enqueue(job);
            }

            public int Id;
            public float EndTime;
            public bool IsProtected;
            public Action Action;
        }
    }
}