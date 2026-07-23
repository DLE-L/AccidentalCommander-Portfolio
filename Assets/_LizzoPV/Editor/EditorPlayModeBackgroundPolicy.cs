using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.EditorTools
{
    internal static class EditorPlayModeBackgroundPolicy
    {
        internal static bool ShouldEnableRunInBackground(PlayModeStateChange change)
        {
            return change == PlayModeStateChange.EnteredPlayMode;
        }
    }

    [InitializeOnLoad]
    internal static class EditorPlayModeBackgroundRunner
    {
        static EditorPlayModeBackgroundRunner()
        {
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
            if (EditorApplication.isPlaying)
                EnableRunInBackground();
        }

        static void HandlePlayModeStateChanged(PlayModeStateChange change)
        {
            if (EditorPlayModeBackgroundPolicy.ShouldEnableRunInBackground(change))
                EnableRunInBackground();
        }

        static void EnableRunInBackground()
        {
            Application.runInBackground = true;
        }
    }
}
