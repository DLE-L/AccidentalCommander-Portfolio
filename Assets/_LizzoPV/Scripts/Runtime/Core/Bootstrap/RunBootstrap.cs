using System;
using UnityEngine;
using Lizzo.PV.Flow;
using Lizzo.PV.Combat;

[DefaultExecutionOrder(-900)]
public sealed class RunBootstrap : MonoBehaviour
{
    [SerializeField] AppBootstrap appBootstrap;
    [SerializeField] GameScene gameScene;
    [SerializeField] Transform poolRoot;
    [SerializeField] GridController gridController;
    [SerializeField] Lizzo.PV.UI.GameplayUIController gameplayUiController;
    [SerializeField] RunPauseController runPauseController;
    [SerializeField] SafeKnockbackWorld safeKnockbackWorld;

    public RunServices Services { get; private set; }
    public bool IsReady { get; private set; }
    bool _persistentFieldsResetForResult;
    bool _personalSummonsResetForResult;
    bool _passiveRosterResetForResult;
    bool _synergyTriggersResetForResult;

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

        try
        {
            ObjectPoolService pool = new ObjectPoolService(poolRoot);
            PrefabFactory factory = new PrefabFactory(appBootstrap.Services.Assets, pool);
            RuntimeObjectRegistry registry = new RuntimeObjectRegistry(factory, gridController);
            RunState runState = new RunState();
            RunContext context = appBootstrap.Services.LaunchState.ConsumeForLaunch();
            Services = new RunServices(appBootstrap.Services, runState, registry, pool, factory, context, safeKnockbackWorld);

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

    void Update()
    {
        if (IsReady == false || Services == null)
            return;

        if (RunPauseController.IsResultGameplayLocked)
        {
            if (_persistentFieldsResetForResult == false)
            {
                Services.PersistentFieldModule.Reset();
                _persistentFieldsResetForResult = true;
            }

            if (_personalSummonsResetForResult == false)
            {
                Services.PersonalSummonModule.Reset();
                _personalSummonsResetForResult = true;
            }

            if (_passiveRosterResetForResult == false)
            {
                Services.PassiveRoster.Reset();
                _passiveRosterResetForResult = true;
            }

            if (_synergyTriggersResetForResult == false)
            {
                Services.SynergyTriggers.Reset();
                Services.MixedCommand.Reset();
                Services.HealingBond.Reset();
                Services.ArcherRain.Reset();
                Services.MagicChain.Reset();
                Services.ExplosionChain.Reset();
                Services.BeastHunt.Reset();
                Services.UndeadSummon.ResetForResult();
                _synergyTriggersResetForResult = true;
            }

            return;
        }

        _persistentFieldsResetForResult = false;
        _personalSummonsResetForResult = false;
        _passiveRosterResetForResult = false;
        _synergyTriggersResetForResult = false;
        Services.SynergyTriggers.Tick(Time.deltaTime, Services.State.IsLoaded, runPauseController.IsPaused, Time.frameCount);
        Services.MixedCommand.TryResolvePending(Time.time);
        Services.MixedCommand.Tick(Time.time);
        Services.HealingBond.TryResolvePending(Time.time);
        Services.HealingBond.Tick(Time.time);
        Services.UndeadSummon.TryResolvePending(Time.time, Time.frameCount);
        Services.UndeadSummon.Tick(Time.time, Time.deltaTime);
        Services.GuardShockwave.TryResolvePending(Time.time);
        Services.ArcherRain.Tick(Time.time);
        Services.MagicChain.TryResolvePending();
        Services.ExplosionChain.TryResolvePending();
        Services.BeastHunt.TryResolvePending(Time.time);
        Services.BeastHunt.Tick(Time.time);
        Services.PersistentFieldModule.Tick(Time.time);
        Services.PersonalSummonModule.Tick(Time.time, Time.deltaTime);
    }

    void BindRuntimeServices()
    {
        Lizzo.PV.P0.Config.RemoteConfig.Configure(Services.App.Data);
        Lizzo.PV.P0.Telemetry.P0PlaytestDiagnostics.ConfigureParty(Services.Party);
        Lizzo.PV.P0.Cards.FixedCardPool.Configure(Services.Registry, Services.Party, Services.Context, Services.App.CompanionUnlockProgress, Services.PassiveRoster);
        Lizzo.PV.P0.Cards.CardEffectRuntime.Configure(Services.Registry, Services.Party);
        Lizzo.PV.P0.Visuals.RetroSfx.Configure(Services.App.Assets);
        Lizzo.PV.Legion.RetroVfx.Configure(Services.App.Assets, Services.Factory);
        Lizzo.PV.Legion.AttackVisual.Configure(Services.Factory);
        Lizzo.PV.Legion.FloatingDamageText.Configure(Services.Factory);
    }

    void ResetRuntimeState()
    {
        Lizzo.PV.P0.Cards.FixedCardPool.ResetRunState();
        Lizzo.PV.P0.Cards.CardEffectRuntime.ResetRunState();
        Services.PassiveRoster?.Reset();
        Services.SynergyTriggers?.Reset();
        Services.MixedCommand?.Reset();
        Services.HealingBond?.Reset();
        Services.ArcherRain?.Reset();
        Services.MagicChain?.Reset();
        Services.ExplosionChain?.Reset();
        Services.BeastHunt?.Reset();
        Services.UndeadSummon?.ResetForResult();
        Services.CanonicalCompanionCasts?.Reset();
        Services.Party.ResetRunState();
        Services.PersonalSummonModule?.Reset();
        Lizzo.PV.P0.Units.BossArena.Clear();
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
}
