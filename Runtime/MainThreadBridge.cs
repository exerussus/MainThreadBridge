
using System;
using System.Threading;
using UnityEngine;

namespace Exerussus.MainThreadBridgeFeature
{
    public static partial class MainThreadBridge
    {
        internal static float Time = 0;
        private static CancellationTokenSource _cts = new();

#if UNITY_EDITOR
        private static Action EditorDispose;        
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            LoopHelper.OnUpdate -= Update;
            LoopHelper.OnUpdate += Update;
        }

        public static Builder CreateJob(Action action)
        {
            return Builder.Create(action);
        }

        private static void Update()
        {
            Time = UnityEngine.Time.time;
            UpdateActionBuilding();
        }
        
        private static void Dispose()
        {
            LoopHelper.OnUpdate -= Update;
            _cts.Cancel();
            _cts.Dispose();
            _cts = new();
        }
    }
}