using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Lizzo.PV.Flow;

namespace Lizzo.PV.EditorTools
{
    [InitializeOnLoad]
    public static class FtueHomeTestActions
    {
        internal const string TutorialCompletionKey = "lizzo.ftue.tutorial_completed.v1";

        internal static bool IsTutorialCompleted => PlayerPrefs.GetInt(TutorialCompletionKey, 0) != 0;

        static FtueHomeTestActions()
        {
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        }

        public static bool IsValidBattleRun(bool runLoaded, string scenePath)
        {
            return runLoaded && scenePath == GameFlowRoutes.GameplayScenePath;
        }

        public static string GetBattleControlDisabledReason(bool isPlaying, bool runLoaded, string scenePath)
        {
            if (!isPlaying)
                return "Disabled: enter Play Mode through Loading.";
            if (!runLoaded)
                return "Disabled: wait for Gameplay to finish loading.";
            if (scenePath == GameFlowRoutes.GameplayScenePath)
                return string.Empty;

            return "Disabled: runtime controls are available only in the loaded Gameplay scene.";
        }

        internal static void ResetFirstRunState()
        {
            RestoreTimeScale();
            PlayerPrefs.DeleteKey(TutorialCompletionKey);
            PlayerPrefs.Save();
        }

        internal static void SetReturningState()
        {
            RestoreTimeScale();
            PlayerPrefs.SetInt(TutorialCompletionKey, 1);
            PlayerPrefs.Save();
        }

        internal static void LaunchFromLoading()
        {
            RestoreTimeScale();
            if (EditorApplication.isPlaying)
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(GameFlowRoutes.LoadingScenePath);
                return;
            }

            EditorSceneManager.OpenScene(GameFlowRoutes.LoadingScenePath, OpenSceneMode.Single);
            EditorApplication.isPlaying = true;
        }

        internal static void RestoreTimeScale()
        {
            Time.timeScale = 1.0f;
        }

        static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
                RestoreTimeScale();
        }
    }
}
