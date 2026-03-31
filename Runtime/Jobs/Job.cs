using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Exerussus.MainThreadBridgeFeature
{
    public static partial class MainThreadBridge
    {
        private static readonly Dictionary<int, IJob> ToCreate = new();
        private static readonly Dictionary<int, IJob> ToWait = new();
        private static readonly ConcurrentQueue<Job> Jobs = new();
        private static readonly object JobsLock = new();

        private static class JobPool<T>
        {
            public static readonly ConcurrentQueue<Job<T>> Queue = new();
        }

        internal interface IJob
        {
            public int Id { get; set; }
            public bool IsProtected { get; set; }
            public float EndTime { get; set; }
            public void Invoke();
            public void Release();
        }

        internal class Job : IJob
        {
            private Job() { }

            public static Job Create(int id, Action action)
            {
                if (!Jobs.TryDequeue(out var job)) job = new();
                job.Id = id;
                job._action = action;
                return job;
            }

            private Action _action;

            public int Id { get; set; }
            public float EndTime { get; set; }
            public bool IsProtected { get; set; }
            public void Invoke() => _action.Invoke();

            public void Release()
            {
                Id = 0;
                EndTime = 0;
                IsProtected = false;
                _action = null;
                Jobs.Enqueue(this);
            }
        }

        internal class Job<T> : IJob
        {
            private Job() { }

            public static Job<T> Create(int id, T context, Action<T> action)
            {
                if (!JobPool<T>.Queue.TryDequeue(out var job)) job = new();
                job.Id = id;
                job._action = action;
                job.Context = context;
                return job;
            }

            private Action<T> _action;

            public int Id { get; set; }
            public float EndTime { get; set; }
            public bool IsProtected { get; set; }
            public T Context;
            public void Invoke() => _action.Invoke(Context);

            public void Release()
            {
                Id = 0;
                EndTime = 0;
                IsProtected = false;
                Context = default;
                _action = null;
                JobPool<T>.Queue.Enqueue(this);
            }
        }
    }
}