using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.EditorTools
{
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
            if (change == PlayModeStateChange.EnteredPlayMode)
                EnableRunInBackground();
        }

        static void EnableRunInBackground()
        {
            Application.runInBackground = true;
        }
    }
}
