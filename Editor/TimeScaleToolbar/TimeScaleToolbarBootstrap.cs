using UnityEditor;

namespace ENP.UnityExtensions.Editor.TimeScaleToolbar
{
    [InitializeOnLoad]
    internal static class TimeScaleToolbarBootstrap
    {
        static TimeScaleToolbarBootstrap()
        {
            EditorApplication.playModeStateChanged += TimeScaleToolbarState.HandlePlayModeStateChanged;
            AssemblyReloadEvents.afterAssemblyReload += HandleAfterAssemblyReload;
        }

        private static void HandleAfterAssemblyReload()
        {
            if (EditorApplication.isPlaying)
            {
                TimeScaleToolbarState.HandlePlayModeStateChanged(PlayModeStateChange.EnteredPlayMode);
            }
            else
            {
                TimeScaleToolbarState.HandlePlayModeStateChanged(PlayModeStateChange.EnteredEditMode);
            }
        }
    }
}
