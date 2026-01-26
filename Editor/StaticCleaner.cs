
#if UNITY_EDITOR
namespace Exerussus.MainThreadBridgeFeature
{
    public static partial class MainThreadBridge
    {
        [UnityEditor.InitializeOnLoad]
        private static class StaticCleaner
        {
            static StaticCleaner()
            {
                UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            }

            private static void OnPlayModeStateChanged(UnityEditor.PlayModeStateChange state)
            {
                if (state == UnityEditor.PlayModeStateChange.ExitingPlayMode || state == UnityEditor.PlayModeStateChange.ExitingEditMode)
                {
                    ToWait.Clear();
                    ToRelease.Clear();
                    ActiveBuffers.Clear();
                    Time = 0;
                    _freeJobIndex = 1;
                    _freeBuilderIndex = 1;
                    Dispose();
                }
            }
        }
    }
}
#endif