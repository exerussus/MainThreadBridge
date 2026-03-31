using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Exerussus.MainThreadBridgeFeature
{
    public static partial class MainThreadBridge
    {
        private static int _freeJobIndex = 1;
        private static int _freeBuilderIndex = 1;
        private static readonly object JobIndexLock = new();
        private static readonly object BuilderIndexLock = new();

        public readonly struct Builder
        {
            private Builder(int id)
            {
                _id = id;
            }

            public static Builder Create()
            {
                int id = 0;

                lock (BuilderIndexLock)
                {
                    id = _freeBuilderIndex++;
                }

                Buffer.Create(id);
                return new Builder(id);
            }

            private readonly int _id;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Builder WithDelay(float delay)
            {
                SetDelay(_id, delay);
                return this;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Builder WithProtection()
            {
                SetProtection(_id);
                return this;
            }

            // [MethodImpl(MethodImplOptions.AggressiveInlining)]
            // public Builder Preserve(Action action)
            // {
            //     SetPreserve(_id);
            //     return this;
            // }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Builder Break()
            {
                MainThreadBridge.Break(_id);
                return this;
            }

            // public Handle Run()
            // {
            //     if (!GetIsValid(_id)) return default;
            //     if (!IsPreserved(_id))
            //     {
            //         Debug.LogError($"Only preserved can run without action.");
            //         return default;
            //     }
            //     
            // }

            public Handle Run(Action action)
            {
                if (!GetIsValid(_id)) return default;

                int jobId;
                lock (JobIndexLock)
                {
                    jobId = _freeJobIndex++;
                }

                var handle = new Handle(jobId);

                BakeJob(_id, jobId, action);

                return handle;
            }

            public Handle Run<T>(T context, Action<T> action)
            {
                if (!GetIsValid(_id)) return default;

                int jobId;
                lock (JobIndexLock)
                {
                    jobId = _freeJobIndex++;
                }

                var handle = new Handle(jobId);

                BakeJob<T>(_id, jobId, context, action);

                return handle;
            }
        }
    }
}