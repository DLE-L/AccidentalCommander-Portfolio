using Lizzo.PV.Gameplay.Units;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Lizzo.PV.Data;
using UnityEngine;
using Lizzo.PV.Flow;
using Lizzo.PV.Combat;
using Lizzo.PV.Gameplay.Route;
using Lizzo.PV.Gameplay.Run;
using Lizzo.PV.Gameplay.CardOffer;
using Lizzo.PV.Gameplay.Visuals;
using Lizzo.PV.Presentation;

[DefaultExecutionOrder(-900)]
public sealed class RunBootstrap : MonoBehaviour
{
    [SerializeField] AppBootstrap appBootstrap;
    [SerializeField] GameScene gameScene;
    [SerializeField] Transform poolRoot;
    [SerializeField] GameplayRunUiController gameplayRunUiController;
    [SerializeField] RunPauseController runPauseController;
    [SerializeField] SafeKnockbackWorld safeKnockbackWorld;
    [SerializeField] WorldFeedbackProfileSetSO worldFeedbackProfiles;
    [SerializeField] RunRewardDefinitionSO runRewardDefinition;

    public RunServices Services { get; private set; }
    public bool IsReady { get; private set; }
    RunRuntimeUpdateCoordinator _runtimeUpdate;

    void Awake()
    {
        appBootstrap = AppBootstrap.Instance ?? appBootstrap ?? FindFirstObjectByType<AppBootstrap>();
        gameScene ??= GetComponent<GameScene>();
        poolRoot ??= transform.Find("PoolRoot");
        runPauseController ??= GetComponent<RunPauseController>();

        if (gameScene == null)
        {
            Debug.LogError("[RunBootstrap] Required GameScene reference is missing.");
            return;
        }
        if (poolRoot == null)
        {
            Debug.LogError("[RunBootstrap] Required PoolRoot reference is missing.");
            return;
        }
        if (ResolveGameplayUiRoute() == null)
            return;
        if (runPauseController == null)
        {
            Debug.LogError("[RunBootstrap] Required RunPauseController reference is missing.");
            return;
        }
        if (safeKnockbackWorld == null)
        {
            Debug.LogError("[RunBootstrap] Required SafeKnockbackWorld reference is missing.");
            return;
        }
        if (runRewardDefinition == null)
        {
            Debug.LogError("[RunBootstrap] Required RunRewardDefinition reference is missing.");
            return;
        }
        if (appBootstrap == null || !appBootstrap.IsReady)
        {
            Debug.LogError("[RunBootstrap] AppBootstrap must be authored and ready before RunBootstrap.");
            return;
        }

        InitializeAsync().Forget();
    }

    async UniTaskVoid InitializeAsync()
    {
        try
        {
            if (!await InitializeDataBeforeRunAsync(
                    appBootstrap.Services.Data,
                    null,
                    this.GetCancellationTokenOnDestroy()))
            {
                SceneTransitionCoordinatorHost.ReportTargetFailure(
                    gameObject.scene.path,
                    "Gameplay data initialization failed.");
                return;
            }

            ObjectPoolService pool = new ObjectPoolService(poolRoot);
            PrefabFactory factory = new PrefabFactory(appBootstrap.Services.Assets, pool);
            RuntimeObjectRegistry registry = new RuntimeObjectRegistry(factory);
            RunState runState = new RunState();
            RunContext context = appBootstrap.Services.LaunchState.ConsumeForLaunch();
            Services = new RunServices(
                appBootstrap.Services,
                runState,
                registry,
                pool,
                factory,
                context,
                safeKnockbackWorld,
                worldFeedbackProfiles,
                runRewardDefinition);

            _runtimeUpdate = new RunRuntimeUpdateCoordinator(Services);
            BindRuntimeServices();
            Services.ResetRunState();
            gameScene.Initialize(Services, ResolveGameplayUiRoute(), runPauseController);
            IsReady = true;
            gameScene.BeginRunFromRoute();
        }
        catch (Exception exception)
        {
            IsReady = false;
            Debug.LogException(exception, this);
            Debug.LogError("[RunBootstrap] Run initialization failed; run services were not created.", this);
            SceneTransitionCoordinatorHost.ReportTargetFailure(
                gameObject.scene.path,
                "Gameplay bootstrap failed.");
            DisposeRuntimeServices(resetRunState: false);
        }
    }

    public static async UniTask<bool> InitializeDataBeforeRunAsync(
        IDataProvider data,
        Action afterInitialization,
        CancellationToken cancellationToken)
    {
        if (data == null)
        {
            Debug.LogError("[RunBootstrap] Data provider initialization failed; run services were not created.");
            return false;
        }

        try
        {
            if (!data.IsInitialized)
            {
                DataLoadResult result = await data.InitializeAsync(cancellationToken);
                if (result == null || !result.Succeeded)
                {
                    Debug.LogError("[RunBootstrap] Data provider initialization failed; run services were not created.");
                    return false;
                }
            }

            afterInitialization?.Invoke();
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return false;
        }
        catch (Exception)
        {
            Debug.LogError("[RunBootstrap] Data provider initialization failed; run services were not created.");
            return false;
        }
    }

    void OnDestroy()
    {
        IsReady = false;
        DisposeRuntimeServices(resetRunState: true);
    }

    void DisposeRuntimeServices(bool resetRunState)
    {
        try
        {
            if (resetRunState && Services != null)
                Services.ResetRunState();
            Services?.Dispose();
        }
        finally
        {
            ClearRuntimeOwnership();
        }
    }

    void Update()
    {
        if (IsReady == false || Services == null || _runtimeUpdate == null)
            return;

        _runtimeUpdate.Tick(
            Time.deltaTime,
            Time.time,
            Time.frameCount,
            runPauseController.IsPaused,
            HitStop.IsActive,
            RunPauseController.IsResultGameplayLocked);
    }

    void BindRuntimeServices()
    {
        Lizzo.PV.Gameplay.Telemetry.RunDiagnostics.ConfigureParty(Services.Party);
        Lizzo.PV.Gameplay.Visuals.RetroVfx.Configure(Services.App.Assets, Services.Factory);
        Lizzo.PV.Gameplay.Visuals.FloatingDamageText.Configure(
            Services.Factory,
            worldFeedbackProfiles.WorldUiProfile.ConsecutiveDamageMergeWindowSeconds);
    }

    void ClearRuntimeServices()
    {
        Lizzo.PV.Gameplay.Visuals.FloatingDamageText.ClearServices();
        Lizzo.PV.Gameplay.Visuals.RetroVfx.ClearServices();
        Lizzo.PV.Gameplay.Visuals.RetroSfx.StopAndReset();
Lizzo.PV.Gameplay.Telemetry.RunDiagnostics.ClearParty();
    }

    void ClearRuntimeOwnership()
    {
        ClearRuntimeServices();
        _runtimeUpdate = null;
        Services = null;
    }

    IGameplayRunUi ResolveGameplayUiRoute()
    {
        if (gameplayRunUiController != null)
            return gameplayRunUiController;

        Debug.LogError("[RunBootstrap] Required GameplayRunUiController authoring is missing.", this);
        return null;
    }
}
