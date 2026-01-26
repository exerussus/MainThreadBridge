
#if HAS_UNITASK

using Cysharp.Threading.Tasks;

namespace Exerussus.MainThreadBridge.Runtime.UnitaskExtension
{
    public static class HandleExtensions
    {
        public static async UniTask AsUniTask(this MainThreadBridgeFeature.MainThreadBridge.Handle handle, float checkInterval = 0.1f, float timeout = 0)
        {
            var millisecondsDelay = (int)(checkInterval * 1000);
            var timeoutMilliseconds = timeout <= 0 ? int.MaxValue : (int)(timeout * 1000);

            while (!handle.IsDone())
            {
                if (timeoutMilliseconds <= 0) return;

                await UniTask.Delay(millisecondsDelay);
                timeoutMilliseconds -= millisecondsDelay;
            }
        }
    }
}

#endif
