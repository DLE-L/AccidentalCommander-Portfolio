using System;
using UnityEngine;
using Lizzo.PV.Flow;

[DefaultExecutionOrder(-900)]
public sealed class RunBootstrap : MonoBehaviour
{
    [SerializeField] AppBootstrap appBootstrap;
    [SerializeField] GameScene gameScene;
    [SerializeField] Transform poolRoot;
    [SerializeField] GridController gridController;
    [SerializeField] Lizzo.PV.UI.GameplayUIController gameplayUiController;
    [SerializeField] RunPauseController runPauseController;

    public RunServices Services { get; private set; }
    public bool IsReady { get; private set; }

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
        if (gameplayUiController == null)
        {
            Debug.LogError("[RunBootstrap] Required GameplayUIController reference is missing.");
            return;
        }
        if (runPauseController == null)
        {
            Debug.LogError("[RunBootstrap] Required RunPauseController reference is missing.");
            return;
        }
        if (appBootstrap == null || !appBootstrap.IsReady)
        {
            Debug.LogError("[RunBootstrap] AppBootstrap must be authored and ready before RunBootstrap.");
            return;
        }

        try
        {
            ObjectPoolService pool = new ObjectPoolService(poolRoot);
            PrefabFactory factory = new PrefabFactory(appBootstrap.Services.Assets, pool);
            RuntimeObjectRegistry registry = new RuntimeObjectRegistry(factory, gridController);
            RunState runState = new RunState();
            Services = new RunServices(appBootstrap.Services, runState, registry, pool, factory);

            BindRuntimeServices();
            ResetRuntimeState();
            gameScene.Initialize(Services, gameplayUiController, runPauseController);
            IsReady = true;
        }
        catch (Exception exception)
        {
            IsReady = false;
            Debug.LogException(exception, this);
            try
            {
                Services?.Dispose();
            }
            finally
            {
                ClearRuntimeServices();
                Services = null;
            }
        }
    }

    void OnDestroy()
    {
        IsReady = false;
        try
        {
            ResetRuntimeState();
            Services?.Dispose();
        }
        finally
        {
            ClearRuntimeServices();
            Services = null;
        }
    }

    void BindRuntimeServices()
    {
        Lizzo.PV.P0.Config.RemoteConfig.Configure(Services.App.Data);
        Lizzo.PV.P0.Telemetry.P0PlaytestDiagnostics.ConfigureParty(Services.Party);
        Lizzo.PV.P0.Cards.FixedCardPool.Configure(Services.Registry, Services.Party);
        Lizzo.PV.P0.Cards.CardEffectRuntime.Configure(Services.Registry, Services.Party);
        Lizzo.PV.P0.Visuals.RetroSfx.Configure(Services.App.Assets);
        Lizzo.PV.Legion.RetroVfx.Configure(Services.App.Assets, Services.Factory);
        Lizzo.PV.Legion.AttackVisual.Configure(Services.Factory);
        Lizzo.PV.Legion.FloatingDamageText.Configure(Services.Factory);
        Lizzo.PV.Legion.ArcherProjectileVisual.Configure(Services.Factory, Services.Registry);
    }

    void ResetRuntimeState()
    {
        Lizzo.PV.P0.Cards.FixedCardPool.ResetRunState();
        Lizzo.PV.P0.Cards.CardEffectRuntime.ResetRunState();
        Services.Party.ResetRunState();
        Lizzo.PV.P0.Units.BossArena.Clear();
    }

    void ClearRuntimeServices()
    {
        Lizzo.PV.Legion.ArcherProjectileVisual.ClearServices();
        Lizzo.PV.Legion.FloatingDamageText.ClearServices();
        Lizzo.PV.Legion.AttackVisual.ClearServices();
        Lizzo.PV.Legion.RetroVfx.ClearServices();
        Lizzo.PV.P0.Visuals.RetroSfx.ClearServices();
        Lizzo.PV.P0.Cards.CardEffectRuntime.ClearServices();
        Lizzo.PV.P0.Cards.FixedCardPool.ClearServices();
Lizzo.PV.P0.Telemetry.P0PlaytestDiagnostics.ClearParty();
        Lizzo.PV.P0.Config.RemoteConfig.ClearServices();
    }
}
