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
        private static readonly Dictionary<Type, object> GenericJobs = new();
        private static readonly object JobsLock = new();
        
        private static ConcurrentQueue<Job<T>> GetGenericPool<T>()
        {
            ConcurrentQueue<Job<T>> pool;
                
            var type = typeof(T);
            if (!GenericJobs.TryGetValue(type, out var concurrentQueue))
            {
                pool = new ConcurrentQueue<Job<T>>();
                GenericJobs[type] = pool;
            }
            else
            {
                pool = concurrentQueue as ConcurrentQueue<Job<T>>;
            }

            return pool;
        }
        
        private static void Release<T>(Job<T> job)
        {
            var pool = GenericJobs[typeof(T)] as ConcurrentQueue<Job<T>>;
            pool?.Enqueue(job);
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
                Jobs.Enqueue(this);
            }
        }
        
        internal class Job<T> : IJob
        {
            private Job() { }

            public static Job<T> Create(int id, T context, Action<T> action)
            {
                var pool = GetGenericPool<T>();
                
                if (!pool.TryDequeue(out var job)) job = new();
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
                Release<T>(this);
            }
        }
    }
}