using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Lizzo.PV.Flow;

namespace Lizzo.PV.EditorTools
{
    public static class FtueHomeTestActions
    {
        internal const string TutorialCompletionKey = "lizzo.ftue.tutorial_completed.v1";

        internal static bool IsTutorialCompleted => PlayerPrefs.GetInt(TutorialCompletionKey, 0) != 0;

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
            ResetFirstRunState(new CompanionUnlockProgress(new EditorPlayerPrefsCompanionUnlockProgressStore()));
        }

        internal static void ResetFirstRunState(CompanionUnlockProgress progress)
        {
            if (progress == null)
                throw new ArgumentNullException(nameof(progress));

            PlayerPrefs.DeleteKey(TutorialCompletionKey);
            TutorialCheckpointProgress.Reset();
            progress.ResetAccountProgress();
            PlayerPrefs.Save();
        }

        sealed class EditorPlayerPrefsCompanionUnlockProgressStore : ICompanionUnlockProgressStore
        {
            public int GetInt(string key, int defaultValue)
            {
                return PlayerPrefs.GetInt(key, defaultValue);
            }

            public void SetInt(string key, int value)
            {
                PlayerPrefs.SetInt(key, value);
            }

            public void Save()
            {
                PlayerPrefs.Save();
            }
        }

        internal static void SetReturningState()
        {
            PlayerPrefs.SetInt(TutorialCompletionKey, 1);
            PlayerPrefs.Save();
        }

        internal static void LaunchFromLoading()
        {
            if (EditorApplication.isPlaying)
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(GameFlowRoutes.LoadingScenePath);
                return;
            }

            EditorSceneManager.OpenScene(GameFlowRoutes.LoadingScenePath, OpenSceneMode.Single);
            EditorApplication.isPlaying = true;
        }
    }
}
