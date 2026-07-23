using UnityEngine;
using UnityEngine.SceneManagement;

namespace Lizzo.PV.Flow
{
    public static class GameFlowRoutes
    {
        public const string LoadingScenePath = "Assets/_LizzoPV/Scenes/Loading.unity";
        public const string TutorialScenePath = "Assets/_LizzoPV/Scenes/Tutorial.unity";
        public const string LobbyScenePath = "Assets/_LizzoPV/Scenes/Lobby.unity";
        public const string GameplayScenePath = "Assets/_LizzoPV/Scenes/Gameplay.unity";

        public static bool IsTutorialScene(Scene scene)
        {
            return scene.path == TutorialScenePath;
        }

        public static void LoadTutorial()
        {
            Load(TutorialScenePath);
        }

        public static void LoadLobby()
        {
            Load(LobbyScenePath);
        }

        public static void LoadGameplay()
        {
            Load(GameplayScenePath);
        }

        public static void ReloadBattleScene(Scene battleScene)
        {
            if (string.IsNullOrEmpty(battleScene.path))
            {
                Debug.LogError("[GameFlowRoutes] Battle scene path is required for retry.");
                return;
            }

            Load(battleScene.path);
        }

        static void Load(string scenePath)
        {
            SceneTransitionOverlay.Show();
            SceneManager.LoadScene(scenePath, LoadSceneMode.Single);
        }
    }
}
