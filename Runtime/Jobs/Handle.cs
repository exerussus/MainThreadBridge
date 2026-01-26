using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Exerussus.MainThreadBridgeFeature
{
    public static partial class MainThreadBridge
    {
        public readonly struct Handle
        {
            internal Handle(int id)
            {
                Id = id;
            }

            private int Id { get; }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool TryCancel()
            {
                return MainThreadBridge.TryCancel(Id);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Cancel()
            {
                MainThreadBridge.Cancel(Id);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool IsValid()
            {
                return MainThreadBridge.IsValid(Id);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool IsDone()
            {
                return MainThreadBridge.IsDone(Id);
            }

            public async Task AsTask(float checkInterval = 0.1f, float timeout = 0)
            {
                var id = Id;
                var millisecondsDelay = (int)(checkInterval * 1000);
                var timeoutMilliseconds = timeout <= 0 ? int.MaxValue : (int)(timeout * 1000);

                while (!MainThreadBridge.IsDone(id))
                {
                    if (timeoutMilliseconds <= 0) return;

                    await Task.Delay(millisecondsDelay);
                    timeoutMilliseconds -= millisecondsDelay;
                }
            }
        }
    }
}