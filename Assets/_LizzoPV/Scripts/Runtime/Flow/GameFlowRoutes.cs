using UnityEngine;
using UnityEngine.SceneManagement;

namespace Lizzo.PV.Flow
{
    public static class GameFlowRoutes
    {
        public const string LoadingScenePath = "Assets/_LizzoPV/Scenes/Loading.unity";
        public const string LobbyScenePath = "Assets/_LizzoPV/Scenes/Lobby_Clean.unity";
        public const string GameplayScenePath = "Assets/_LizzoPV/Scenes/Gameplay_Clean.unity";

        public static RunMode ResolveNextBattleMode()
        {
            return FirstRunProgress.ResolveNextBattleMode(forceNormal: false);
        }

        public static void LoadLobby()
        {
            Load(LobbyScenePath);
        }

        public static void LoadGameplay()
        {
            PrepareRun(ResolveNextBattleMode());
            Load(GameplayScenePath);
        }

        public static void ReloadBattleScene(Scene battleScene)
        {
            if (string.IsNullOrEmpty(battleScene.path))
            {
                Debug.LogError("[GameFlowRoutes] Battle scene path is required for retry.");
                return;
            }

            if (battleScene.path != GameplayScenePath)
            {
                Debug.LogError("[GameFlowRoutes] Retry is only supported for the active Gameplay route.");
                return;
            }

            PrepareRetry();
            Load(battleScene.path);
        }

        static void PrepareRun(RunMode mode)
        {
            AppBootstrap.Instance?.Services?.LaunchState?.Prepare(new RunContext(mode));
        }

        static void PrepareRetry()
        {
            AppBootstrap.Instance?.Services?.LaunchState?.PrepareRetry();
        }

        static void Load(string scenePath)
        {
            SceneTransitionOverlay.Show();
            SceneManager.LoadScene(scenePath, LoadSceneMode.Single);
        }
    }
}
