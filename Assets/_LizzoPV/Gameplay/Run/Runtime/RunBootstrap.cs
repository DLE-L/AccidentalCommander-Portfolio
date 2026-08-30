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
    public enum InitializationStage
    {
        None,
        DataInitialization,
        RuntimeInfrastructureCreation,
        LaunchRequestConsumption,
        CardCatalogValidation,
        RunServicesCreation,
        RuntimeServiceBinding,
        RunStateReset,
        SceneInitialization,
        RunStartDispatch,
        Complete,
    }

    public sealed class InitializationTrace
    {
        public InitializationStage Stage { get; private set; }

        public void Enter(InitializationStage stage)
        {
            Stage = stage;
        }

        public Exception CreateFailure(Exception cause)
        {
            if (cause == null)
                throw new ArgumentNullException(nameof(cause));

            return new InvalidOperationException(
                $"[RunBootstrap] entry=run_initialization stage={ToDiagnosticName(Stage)} outcome=failure; run services were not created.",
                cause);
        }

        public static string ToDiagnosticName(InitializationStage stage)
        {
            return stage switch
            {
                InitializationStage.DataInitialization => "data_initialization",
                InitializationStage.RuntimeInfrastructureCreation => "runtime_infrastructure_creation",
                InitializationStage.LaunchRequestConsumption => "launch_request_consumption",
                InitializationStage.CardCatalogValidation => "card_catalog_validation",
                InitializationStage.RunServicesCreation => "run_services_creation",
                InitializationStage.RuntimeServiceBinding => "runtime_service_binding",
                InitializationStage.RunStateReset => "run_state_reset",
                InitializationStage.SceneInitialization => "scene_initialization",
                InitializationStage.RunStartDispatch => "run_start_dispatch",
                InitializationStage.Complete => "complete",
                _ => "none",
            };
        }
    }

    [SerializeField] AppBootstrap appBootstrap;
    [SerializeField] GameScene gameScene;
    [SerializeField] Transform poolRoot;
    [SerializeField] GridController gridController;
    [SerializeField] GameplayRunUiController gameplayRunUiController;
    [SerializeField] RunPauseController runPauseController;
    [SerializeField] SafeKnockbackWorld safeKnockbackWorld;
    [SerializeField] AudioSource retroSfxSource;

    public RunServices Services { get; private set; }
    public bool IsReady { get; private set; }
    public InitializationTrace Trace { get; } = new InitializationTrace();
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
        if (retroSfxSource == null)
        {
            Debug.LogError("[RunBootstrap] Required RetroSfx AudioSource reference is missing.");
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
            Trace.Enter(InitializationStage.DataInitialization);
            Debug.Log("[RunBootstrap] entry=run_initialization stage=data_initialization outcome=started", this);
            if (!await InitializeDataBeforeRunAsync(
                    appBootstrap.Services.Data,
                    null,
                    this.GetCancellationTokenOnDestroy()))
                return;

            Trace.Enter(InitializationStage.RuntimeInfrastructureCreation);
            ObjectPoolService pool = new ObjectPoolService(poolRoot);
            PrefabFactory factory = new PrefabFactory(appBootstrap.Services.Assets, pool);
            RuntimeObjectRegistry registry = new RuntimeObjectRegistry(factory, gridController);
            RunState runState = new RunState();
            Trace.Enter(InitializationStage.LaunchRequestConsumption);
            RunStartRequest startRequest = appBootstrap.Services.LaunchState
                .ConsumeForLaunch();
            Trace.Enter(InitializationStage.CardCatalogValidation);
            if (!CardCatalogProvider.TryGetPool(
                    startRequest.Definition.CardPoolProfileId,
                    out CardPoolDefinition cardPoolDefinition))
            {
                throw new InvalidOperationException(
                    $"[RunBootstrap] Required card pool profile '{startRequest.Definition.CardPoolProfileId}' is not registered in the authored catalog.");
            }
            Trace.Enter(InitializationStage.RunServicesCreation);
            Services = new RunServices(
                appBootstrap.Services,
                runState,
                registry,
                pool,
                factory,
                startRequest,
                safeKnockbackWorld,
                cardPoolDefinition);

            Trace.Enter(InitializationStage.RuntimeServiceBinding);
            _runtimeUpdate = new RunRuntimeUpdateCoordinator(Services);
            BindRuntimeServices();
            Trace.Enter(InitializationStage.RunStateReset);
            Services.ResetRunState();
            Trace.Enter(InitializationStage.SceneInitialization);
            gameScene.Initialize(Services, ResolveGameplayUiRoute(), runPauseController);
            IsReady = true;
            Trace.Enter(InitializationStage.RunStartDispatch);
            gameScene.BeginRunFromRoute();
            Trace.Enter(InitializationStage.Complete);
            Debug.Log("[RunBootstrap] entry=run_initialization stage=complete outcome=ready", this);
        }
        catch (Exception exception)
        {
            IsReady = false;
            Debug.LogException(Trace.CreateFailure(exception), this);
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
        Lizzo.PV.P0.Visuals.RetroSfx.Configure(Services.App.Assets, retroSfxSource);
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
