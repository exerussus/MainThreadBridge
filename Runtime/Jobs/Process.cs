using System;
using System.Collections.Generic;
using UnityEngine;

namespace Exerussus.MainThreadBridgeFeature
{
    public static partial class MainThreadBridge
    {
        private static readonly HashSet<int> ToRelease = new();

        internal static void UpdateActionBuilding()
        {
            Time = UnityEngine.Time.time;
            UpdateReleasing();
            UpdateCreating();
            UpdateWaiting();
        }

        internal static void UpdateCreating()
        {
            lock (JobsLock)
            {
                foreach (var job in ToCreate.Values)
                {
                    ToWait[job.Id] = job;
                }

                ToCreate.Clear();
            }
        }

        internal static void UpdateWaiting()
        {
            lock (JobsLock)
            {
                foreach (var job in ToWait.Values)
                {
                    if (ToRelease.Contains(job.Id)) continue;

                    if (job.EndTime < Time)
                    {
                        ToRelease.Add(job.Id);
                        ExecuteJob(job);
                    }
                }
            }
        }

        internal static void UpdateReleasing()
        {
            lock (JobsLock)
            {
                foreach (var jobId in ToRelease)
                {
                    IJob job;
                    if (ToWait.TryGetValue(jobId, out job)) ToWait.Remove(jobId);
                    else if (ToCreate.TryGetValue(jobId, out job)) ToCreate.Remove(jobId);
                    else continue;
                    job.Release();
                }

                ToRelease.Clear();
            }
        }

        internal static void ExecuteJob(IJob job)
        {
            if (job.IsProtected)
            {
                try
                {
                    job.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            }
            else
            {
                job.Invoke();
            }
        }

        internal static void CreateJob(Buffer buffer, int jobId, Action action)
        {
            IJob job = Job.Create(jobId, action);
            job.EndTime = Time + buffer.Delay;
            job.IsProtected = buffer.IsProtected;
            lock (JobsLock) ToCreate.Add(job.Id, job);
        }

        internal static void CreateJob<T>(Buffer buffer, int jobId, T context, Action<T> action)
        {
            IJob job = Job<T>.Create(jobId, context, action);
            job.EndTime = Time + buffer.Delay;
            job.IsProtected = buffer.IsProtected;
            lock (JobsLock) ToCreate.Add(job.Id, job);
        }

        internal static bool TryCancel(int jobId)
        {
            lock (JobsLock)
            {
                if (!ToWait.ContainsKey(jobId) && !ToCreate.ContainsKey(jobId)) return false;

                ToRelease.Add(jobId);
                return true;
            }
        }

        internal static void Cancel(int jobId)
        {
            lock (JobsLock)
            {
                if (ToWait.ContainsKey(jobId) || ToCreate.ContainsKey(jobId)) ToRelease.Add(jobId);
            }
        }

        internal static bool IsValid(int jobId)
        {
            if (jobId == 0) return false;

            lock (JobsLock)
            {
                return !ToRelease.Contains(jobId) && (ToWait.ContainsKey(jobId) || ToCreate.ContainsKey(jobId));
            }
        }

        internal static bool IsDone(int jobId)
        {
            if (jobId == 0) return false;

            lock (JobsLock)
            {
                var exists = ToWait.ContainsKey(jobId) || ToCreate.ContainsKey(jobId);
                return !exists && jobId < _freeJobIndex;
            }
        }
    }
}