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

        public static FirstRunEntryRoute ResolveInitialEntryRoute()
        {
            return FirstRunProgress.ResolveEntryRoute(startNormalGameplay: false);
        }

        public static void LoadInitialRoute()
        {
            FirstRunEntryRoute route = ResolveInitialEntryRoute();
            if (route == FirstRunEntryRoute.Tutorial)
            {
                if (PrepareRun(ResolveNextBattleMode(), CampaignStageId.Stage1))
                    RequestTransition(GameplayScenePath, SceneTransitionKind.Start, canReturnToSource: false);
                return;
            }

            if (route == FirstRunEntryRoute.Home)
            {
                RequestTransition(LobbyScenePath, SceneTransitionKind.Start, canReturnToSource: false);
                return;
            }

            Debug.LogError($"[GameFlowRoutes] Unsupported initial entry route: {route}.");
        }

        public static void LoadLobby()
        {
            RequestTransition(LobbyScenePath, SceneTransitionKind.Standard, canReturnToSource: true);
        }

        public static void LoadGameplay()
        {
            TryLoadGameplay(CampaignStageId.Stage1);
        }

        public static void LoadGameplay(CampaignStageId stageId)
        {
            TryLoadGameplay(stageId);
        }

        public static bool TryLoadGameplay()
        {
            return TryLoadGameplay(CampaignStageId.Stage1);
        }

        public static bool TryLoadGameplay(CampaignStageId stageId)
        {
            if (!SceneTransitionCoordinatorHost.CanAcceptRequest)
            {
                Debug.LogError("[GameFlowRoutes] Scene transition is unavailable or already running.");
                return false;
            }

            if (PrepareRun(ResolveNextBattleMode(), stageId) == false)
                return false;

            return RequestTransition(GameplayScenePath, SceneTransitionKind.Standard, canReturnToSource: true);
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
            RequestTransition(battleScene.path, SceneTransitionKind.Standard, canReturnToSource: true);
        }

        static bool PrepareRun(RunMode mode, CampaignStageId stageId)
        {
            if (stageId < CampaignStageId.Stage1 || stageId > CampaignStageId.Stage3)
            {
                Debug.LogError($"[GameFlowRoutes] Unsupported campaign Stage: {stageId}.");
                return false;
            }

            if (mode == RunMode.Tutorial && stageId != CampaignStageId.Stage1)
            {
                Debug.LogError("[GameFlowRoutes] Tutorial runs support only Stage 1.");
                return false;
            }

            AppServices services = AppBootstrap.Instance?.Services;
            RunLaunchState launchState = services?.LaunchState;
            CompanionUnlockProgress progress = services?.CompanionUnlockProgress;
            if (launchState == null || progress == null)
            {
                Debug.LogError("[GameFlowRoutes] AppBootstrap run launch services must be ready before preparing a run.");
                return false;
            }

            RunContext context = new RunContext(mode, stageId);
            if (launchState.TryPrepare(context, progress) == false)
            {
                Debug.LogError($"[GameFlowRoutes] Campaign Stage is locked: {stageId}.");
                return false;
            }

            return true;
        }

        static void PrepareRetry()
        {
            AppBootstrap.Instance?.Services?.LaunchState?.PrepareRetry();
        }

        static bool RequestTransition(string scenePath, SceneTransitionKind kind, bool canReturnToSource)
        {
            if (!SceneTransitionCoordinatorHost.IsAvailable)
            {
                Debug.LogError("[GameFlowRoutes] SceneTransitionCoordinatorHost is unavailable.");
                return false;
            }

            return SceneTransitionCoordinatorHost.TryRequest(
                new SceneTransitionRequest(scenePath, kind, canReturnToSource));
        }
    }
}
