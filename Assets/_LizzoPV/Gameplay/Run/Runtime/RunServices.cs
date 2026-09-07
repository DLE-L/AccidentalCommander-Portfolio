using System;
using Lizzo.PV.Combat;
using Lizzo.PV.Combat.Fields;
using Lizzo.PV.Combat.Projectiles;
using Lizzo.PV.Combat.Summons;
using Lizzo.PV.Flow;
using Lizzo.PV.Legion;
using Lizzo.PV.Legion.Combat;
using Lizzo.PV.Gameplay.Telemetry;
using Lizzo.PV.Gameplay.CardOffer;
using Lizzo.PV.P0.Cards;
using Lizzo.PV.Gameplay.Run;
using Lizzo.PV.Gameplay.Diagnostics;
using Lizzo.PV.P0.Presentation;
using Lizzo.PV.Legion.RunCore;
using Lizzo.PV.Presentation;

public sealed class RunServices
{
    public AppServices App { get; }
    public RunState State { get; }
    public RuntimeObjectRegistry Registry { get; }
    public RuntimeObjectSpawner Spawner { get; }
    public ObjectPoolService Pool { get; }
    public IPrefabFactory Factory { get; }
    public ICombatProjectileModule ProjectileModule { get; }
    public ICombatImmediateHitModule ImmediateHitModule { get; }
    public ICombatPersistentFieldModule PersistentFieldModule { get; }
    public ICompanionPersonalSummonModule PersonalSummonModule { get; }
    public RunGameplayTuning Tuning { get; }
    public PartyService Party { get; }
    public CardOfferRuntime CardOffers { get; }
    public PassiveRosterState PassiveRoster { get; }
    internal CompanionPassiveCombatResolver PassiveEffects { get; }
    public CanonicalCompanionCastStream CanonicalCompanionCasts { get; }
    public RunCombatTelemetry CombatTelemetry { get; }
    public CompanionFirstPromotionCombatRunModule FirstPromotionCombat { get; }
    public CompanionSecondPromotionCombatRunModule SecondPromotionCombat { get; }
    public CompanionThirdPromotionCombatRunModule ThirdPromotionCombat { get; }
    internal SafeKnockbackWorld SafeKnockbackWorld { get; }
    public RunContext Context { get; }
    public CompanionRuntimeProductionHost CompanionRuntimeHost { get; }
    public CompanionSynergyProductionHost ProductionSynergies { get; }
    public WorldFeedbackRuntime WorldFeedback { get; }
    public RunRewardSettlementService ResultRewards { get; }

    readonly CompanionUnlockProgressRunBinder _companionUnlockProgressBinder;

    bool _disposed;

    public RunServices(AppServices app, RunState state, RuntimeObjectRegistry registry, ObjectPoolService pool, IPrefabFactory factory)
        : this(app, state, registry, pool, factory, RunContext.Normal)
    {
    }

    public RunServices(
        AppServices app,
        RunState state,
        RuntimeObjectRegistry registry,
        ObjectPoolService pool,
        IPrefabFactory factory,
        RunContext context,
        SafeKnockbackWorld safeKnockbackWorld = null,
        WorldFeedbackProfileSetSO worldFeedbackProfiles = null,
        RunRewardDefinitionSO runRewardDefinition = null,
        CompanionRuntimePresentationSet companionRuntimePresentationSet = null)
    {
        App = app ?? throw new ArgumentNullException(nameof(app));
        State = state ?? throw new ArgumentNullException(nameof(state));
        Registry = registry ?? throw new ArgumentNullException(nameof(registry));
        Pool = pool ?? throw new ArgumentNullException(nameof(pool));
        Factory = factory ?? throw new ArgumentNullException(nameof(factory));
        Context = context;
        Tuning = new RunGameplayTuning(App.Data);
        SafeKnockbackWorld = safeKnockbackWorld;
        ProjectilePresentationCatalog projectiles = PresentationCatalogProvider.TryGetCatalog(out PresentationCatalog catalog)
            ? catalog.Projectiles
            : null;
        ProjectileModule = new CombatProjectileModule(Factory, Registry, projectiles);
        var immediateHitModule = new CombatImmediateHitModule();
        ImmediateHitModule = immediateHitModule;
        if (worldFeedbackProfiles != null)
            WorldFeedback = new WorldFeedbackRuntime(worldFeedbackProfiles, immediateHitModule, State);
        if (runRewardDefinition != null)
            ResultRewards = new RunRewardSettlementService(App.AccountWallet, runRewardDefinition);
        PersistentFieldModule = new CombatPersistentFieldModule(
            new RegistryPersistentFieldTargetSource(Registry),
            ImmediateHitModule);
        PersonalSummonModule = new CompanionPersonalSummonModule(
            Factory,
            new RegistryPersonalSummonTargetSource(Registry),
            ImmediateHitModule);
        Party = new PartyService(App.Data, Registry, Factory, ProjectileModule, ImmediateHitModule, PersistentFieldModule, State, Tuning);
        CanonicalCompanionCasts = new CanonicalCompanionCastStream();
        CombatTelemetry = new RunCombatTelemetry(CanonicalCompanionCasts);
        WorldFeedback?.BindCanonicalCompanionCasts(CanonicalCompanionCasts);
        Party.BindCanonicalCompanionCastStream(CanonicalCompanionCasts);
        FirstPromotionCombat = new CompanionFirstPromotionCombatRunModule(
            App.Data,
            Party,
            Registry,
            ProjectileModule,
            ImmediateHitModule,
            CanonicalCompanionCasts);
        SecondPromotionCombat = new CompanionSecondPromotionCombatRunModule(
            App.Data,
            Party,
            Registry,
            ImmediateHitModule,
            PersistentFieldModule,
            CanonicalCompanionCasts);
        ThirdPromotionCombat = new CompanionThirdPromotionCombatRunModule(
            App.Data,
            Party,
            Registry,
            ImmediateHitModule,
            PersonalSummonModule,
            CanonicalCompanionCasts,
            State);
        PassiveRoster = new PassiveRosterState();
        PassiveEffects = new CompanionPassiveCombatResolver(
            App.Data,
            PassiveRoster,
            () => CompanionRuntimeHost == null
                ? 0
                : CompanionRuntimeHost.Adapter.ActiveCompanionSlotCount);
        Party.BindPassiveRoster(PassiveRoster, PassiveEffects);
        CompanionRuntimePresentationSet presentationSet = companionRuntimePresentationSet ?? catalog?.CompanionRuntime
            ?? throw new InvalidOperationException(
                "[RunServices] Companion runtime presentation set is missing.");
        CompanionRuntimeHost = new CompanionRuntimeProductionHost(
            App.Data,
            Registry,
            ProjectileModule,
            ImmediateHitModule,
            PersistentFieldModule,
            presentationSet,
            CanonicalCompanionCasts,
            PassiveEffects,
            PassiveRoster);
        Party.BindCompanionRuntime(CompanionRuntimeHost.Adapter);
        Party.BindCompanionCombatAnchorSource(CompanionRuntimeHost);
        FirstPromotionCombat.BindRepresentativeSource(CompanionRuntimeHost);
        SecondPromotionCombat.BindRepresentativeSource(CompanionRuntimeHost);
        ThirdPromotionCombat.BindRepresentativeSource(CompanionRuntimeHost);
        CompanionRuntimeHost.Adapter.RosterChanged += OnCompanionRosterChanged;
        ProductionSynergies = new CompanionSynergyProductionHost(
            App.Data,
            Registry,
            immediateHitModule,
            CompanionRuntimeHost,
            CanonicalCompanionCasts,
            CombatTelemetry);
        CardOffers = new CardOfferRuntime();
        CardOffers.Configure(
            Registry,
            Party,
            Context,
            App.CompanionUnlockProgress,
            PassiveRoster,
            CompanionRuntimeHost?.CardInput,
            CompanionRuntimeHost?.Adapter);
        _companionUnlockProgressBinder = new CompanionUnlockProgressRunBinder(
            App.CompanionUnlockProgress,
            State,
            Context);
        Spawner = new RuntimeObjectSpawner(this);
    }

    internal void ResetRunState()
    {
        CardOffers.ResetRunState();
        PassiveRoster?.Reset();
        CanonicalCompanionCasts?.Reset();
        CombatTelemetry?.Reset();
        FirstPromotionCombat?.Reset();
        SecondPromotionCombat?.Reset();
        ThirdPromotionCombat?.Reset();
        CompanionRuntimeHost?.Reset();
        ProductionSynergies?.Reset();
        Party.ResetRunState();
        PersonalSummonModule?.Reset();
        Lizzo.PV.P0.Units.BossArena.Clear();
    }

    internal void ResetRuntimeForResult()
    {
        PersistentFieldModule.Reset();
        PersonalSummonModule.Reset();
        FirstPromotionCombat.Reset();
        SecondPromotionCombat.Reset();
        ThirdPromotionCombat.Reset();
        PassiveRoster.Reset();
        CompanionRuntimeHost?.StopForResult();
        ProductionSynergies?.Reset();
    }

    internal void TickRuntime(
        float deltaTime,
        float time,
        int frameCount,
        bool isPaused,
        bool isHitStopActive)
    {
        PlayerController commander = Registry.Player;
        if (commander != null)
        {
            CompanionRuntimeHost?.Advance(
                deltaTime,
                isPaused || isHitStopActive,
                commander.transform);
            ProductionSynergies?.Advance(
                deltaTime,
                time,
                isPaused || isHitStopActive,
                commander.transform);
        }

        if (isPaused == false)
        {
            FirstPromotionCombat.Tick(time);
            SecondPromotionCombat.Tick(time);
            ThirdPromotionCombat.Tick(time);
        }
        PersistentFieldModule.Tick(time);
        PersonalSummonModule.Tick(time, deltaTime);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _companionUnlockProgressBinder.Dispose();
        CombatTelemetry?.LogSummary("run_services_dispose");
        ProductionSynergies?.Dispose();
        if (CompanionRuntimeHost != null)
        {
            CompanionRuntimeHost.Adapter.RosterChanged -= OnCompanionRosterChanged;
            Party.UnbindCompanionCombatAnchorSource(CompanionRuntimeHost);
            CompanionRuntimeHost.Dispose();
        }
        FirstPromotionCombat.Dispose();
        SecondPromotionCombat.Dispose();
        ThirdPromotionCombat.Dispose();
        CardOffers.Dispose();
        Party.Dispose();
        CombatTelemetry?.Dispose();
        CanonicalCompanionCasts.Dispose();
        PersonalSummonModule.Dispose();
        PersistentFieldModule.Dispose();
        int enemyCountBeforeClear = Registry.EnemyResidualCount;
        int expCountBeforeClear = Registry.ExpResidualCount;
        int poolActiveCountBeforeClear = Pool.ActiveCount;
        Registry.Clear();
        Factory.Clear();
        LogRestartResetPostcondition(enemyCountBeforeClear, expCountBeforeClear, poolActiveCountBeforeClear);
        WorldFeedback?.Dispose();
        State.Dispose();
    }

    private void OnCompanionRosterChanged(CompanionRosterCommandKind commandKind)
    {
        ProductionSynergies?.RefreshProgression();
    }

    private void LogRestartResetPostcondition(
        int enemyCountBeforeClear,
        int expCountBeforeClear,
        int poolActiveCountBeforeClear)
    {
        int enemyResidualCount = Registry.EnemyResidualCount;
        int expResidualCount = Registry.ExpResidualCount;
        int poolActiveResidualCount = Pool.ActiveCount;
        bool isClean = enemyResidualCount == 0 && expResidualCount == 0 && poolActiveResidualCount == 0;
        string[] parameters =
        {
            "reason=run_services_dispose",
            $"enemy_count_before_clear={enemyCountBeforeClear}",
            $"exp_count_before_clear={expCountBeforeClear}",
            $"pool_active_count_before_clear={poolActiveCountBeforeClear}",
            $"enemy_residual_count={enemyResidualCount}",
            $"exp_residual_count={expResidualCount}",
            $"pool_active_residual_count={poolActiveResidualCount}",
            $"invariant_zero={isClean.ToString().ToLowerInvariant()}",
        };

        RunTelemetry.Log(RunTelemetry.RestartResetPostcondition, parameters);
        if (isClean == false)
        {
            RunTelemetry.Log(RunTelemetry.RestartResetResidualViolation, parameters);
            UnityEngine.Debug.LogError($"[RunServices] Restart/reset residual invariant failed. enemy={enemyResidualCount}, exp={expResidualCount}, pool_active={poolActiveResidualCount}.");
        }

        RunTelemetry.FlushRunLog("restart_reset_postcondition");
    }
}
