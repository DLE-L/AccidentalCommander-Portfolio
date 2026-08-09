using UnityEngine;
using UnityEngine.SceneManagement;

namespace Lizzo.PV.Flow
{
    public static class GameFlowRoutes
    {
        public const string LoadingScenePath = "Assets/_LizzoPV/Scenes/Loading.unity";
        public const string LobbyScenePath = "Assets/_LizzoPV/Scenes/Lobby.unity";
        public const string GameplayScenePath = "Assets/_LizzoPV/Scenes/Gameplay.unity";

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
            PrepareRun(ResolveNextBattleMode(), CommanderWeaponId.None);
            Load(GameplayScenePath);
        }

        public static void LoadGameplay(CommanderWeaponId commanderWeapon)
        {
            if (CommanderWeaponCatalog.IsSelectable(commanderWeapon) == false)
            {
                Debug.LogError("[GameFlowRoutes] A selected commander weapon is required for the Lobby sortie.");
                return;
            }

            if (PrepareRun(ResolveNextBattleMode(), commanderWeapon) == false)
                return;

            CommanderWeaponPreferenceStore.Save(commanderWeapon);
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

        static bool PrepareRun(RunMode mode, CommanderWeaponId commanderWeapon)
        {
            RunLaunchState launchState = AppBootstrap.Instance?.Services?.LaunchState;
            if (launchState == null)
            {
                Debug.LogError("[GameFlowRoutes] AppBootstrap Services and LaunchState must be ready before preparing a run.");
                return false;
            }

            launchState.Prepare(new RunContext(mode, commanderWeapon));
            return true;
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
