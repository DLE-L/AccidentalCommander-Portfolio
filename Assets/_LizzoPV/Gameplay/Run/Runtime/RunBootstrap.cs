using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Lizzo.PV.Data;
using UnityEngine;
using Lizzo.PV.Flow;
using Lizzo.PV.Combat;
using Lizzo.PV.Gameplay.Route;
using Lizzo.PV.Gameplay.Run;
using Lizzo.PV.P0.Cards;
using Lizzo.PV.P0.Visuals;

[DefaultExecutionOrder(-900)]
public sealed class RunBootstrap : MonoBehaviour
{
    [SerializeField] AppBootstrap appBootstrap;
    [SerializeField] GameScene gameScene;
    [SerializeField] Transform poolRoot;
    [SerializeField] GridController gridController;
    [SerializeField] GameplayRunUiController gameplayRunUiController;
    [SerializeField] RunPauseController runPauseController;
    [SerializeField] SafeKnockbackWorld safeKnockbackWorld;

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
        if (gridController == null)
        {
            Debug.LogError("[RunBootstrap] Required GridController reference is missing.");
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
                return;

            ObjectPoolService pool = new ObjectPoolService(poolRoot);
            PrefabFactory factory = new PrefabFactory(appBootstrap.Services.Assets, pool);
            RuntimeObjectRegistry registry = new RuntimeObjectRegistry(factory, gridController);
            RunState runState = new RunState();
            RunStartRequest startRequest = appBootstrap.Services.LaunchState
                .ConsumeForLaunch();
            if (!CardCatalogProvider.TryGetPool(out CardPoolDefinition cardPoolDefinition))
                throw new InvalidOperationException("[RunBootstrap] Required card pool definition is missing.");
            if (!string.Equals(
                    cardPoolDefinition.ProfileId,
                    startRequest.Definition.CardPoolProfileId,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"[RunBootstrap] Authored card pool profile '{cardPoolDefinition.ProfileId}' does not match injected profile '{startRequest.Definition.CardPoolProfileId}'.");
            }
            Services = new RunServices(
                appBootstrap.Services,
                runState,
                registry,
                pool,
                factory,
                startRequest,
                safeKnockbackWorld,
                cardPoolDefinition);

            _runtimeUpdate = new RunRuntimeUpdateCoordinator(Services);
            BindRuntimeServices();
            Services.ResetRunState();
            gameScene.Initialize(Services, ResolveGameplayUiRoute(), runPauseController);
            IsReady = true;
            gameScene.BeginRunFromRoute();
        }
        catch (Exception)
        {
            IsReady = false;
            Debug.LogError("[RunBootstrap] Run initialization failed; run services were not created.", this);
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
        Lizzo.PV.P0.Config.RemoteConfig.Configure(Services.App.Data);
        Lizzo.PV.P0.Telemetry.P0PlaytestDiagnostics.ConfigureParty(Services.Party);
        Lizzo.PV.P0.Cards.FixedCardPool.Configure(
            Services.Registry,
            Services.Party,
            Services.Context,
            Services.App.CompanionUnlockProgress,
            Services.PassiveRoster,
            Services.RecordingCompanions?.CardInput,
            Services.RecordingCompanions?.Adapter,
            Services.Definition,
            Services.CardPoolDefinition);
        Lizzo.PV.P0.Cards.CardEffectRuntime.Configure(Services.Registry, Services.Party);
        Lizzo.PV.P0.Visuals.RetroSfx.Configure(Services.App.Assets);
        Lizzo.PV.Legion.RetroVfx.Configure(Services.App.Assets, Services.Factory);
        Lizzo.PV.Legion.AttackVisual.Configure(Services.Factory);
        Lizzo.PV.Legion.FloatingDamageText.Configure(Services.Factory);
    }

    void ClearRuntimeServices()
    {
        Lizzo.PV.Legion.FloatingDamageText.ClearServices();
        Lizzo.PV.Legion.AttackVisual.ClearServices();
        Lizzo.PV.Legion.RetroVfx.ClearServices();
        Lizzo.PV.P0.Visuals.RetroSfx.ClearServices();
        Lizzo.PV.P0.Cards.CardEffectRuntime.ClearServices();
        Lizzo.PV.P0.Cards.FixedCardPool.ClearServices();
Lizzo.PV.P0.Telemetry.P0PlaytestDiagnostics.ClearParty();
        Lizzo.PV.P0.Config.RemoteConfig.ClearServices();
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
