using System;
using Lizzo.PV.Combat;
using Lizzo.PV.Combat.Fields;
using Lizzo.PV.Combat.Projectiles;
using Lizzo.PV.Combat.Summons;
using Lizzo.PV.Flow;
using Lizzo.PV.Legion;
using Lizzo.PV.Legion.Synergy;
using Lizzo.PV.Legion.Combat;
using Lizzo.PV.P0.Telemetry;
using Lizzo.PV.P0.Cards;
using Lizzo.PV.Gameplay.RunTraits;
using Lizzo.PV.Gameplay.Diagnostics;
using Lizzo.PV.P0.Presentation;
using Lizzo.PV.Legion.RunCore;

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
    public PartyService Party { get; }
    public PassiveRosterState PassiveRoster { get; }
    internal CompanionPassiveCombatResolver PassiveEffects { get; }
    public SynergyActivationState Synergies { get; }
    public DamageContributionLedger DamageContributions { get; }
    public SynergyTriggerState SynergyTriggers { get; }
    internal Build1SynergyProgression Build1SynergyProgression { get; }
    public UndeadSummonRunModule UndeadSummon { get; }
    public MagicChainSynergy MagicChain { get; }
    public CanonicalCompanionCastStream CanonicalCompanionCasts { get; }
    internal SafeKnockbackWorld SafeKnockbackWorld { get; }
    public RunContext Context { get; }
    public RunTraitRunState RunTraits { get; }
    internal RunTraitOfferCoordinator RunTraitOffers { get; }
    internal RunTraitEffectCoordinator RunTraitEffects { get; }
    public CompanionRecordingProductionHost RecordingCompanions { get; }

    readonly MixedCommandRunModule _mixedCommand;
    readonly HealingBondRunModule _healingBond;
    readonly GuardShockwaveSynergy _guardShockwave;
    readonly ArcherRainSynergy _archerRain;
    readonly ExplosionChainSynergy _explosionChain;
    readonly BeastHuntSynergy _beastHunt;
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
        CardPoolDefinition cardPoolDefinition = null)
    {
        App = app ?? throw new ArgumentNullException(nameof(app));
        State = state ?? throw new ArgumentNullException(nameof(state));
        Registry = registry ?? throw new ArgumentNullException(nameof(registry));
        Pool = pool ?? throw new ArgumentNullException(nameof(pool));
        Factory = factory ?? throw new ArgumentNullException(nameof(factory));
        Context = context;
        SafeKnockbackWorld = safeKnockbackWorld;
        RunTraits = new RunTraitRunState();
        ProjectilePresentationCatalog projectiles = PresentationCatalogProvider.TryGetCatalog(out PresentationCatalog catalog)
            ? catalog.Projectiles
            : null;
        ProjectileModule = new CombatProjectileModule(Factory, Registry, projectiles);
        ImmediateHitModule = new CombatImmediateHitModule();
        PersistentFieldModule = new CombatPersistentFieldModule(
            new RegistryPersistentFieldTargetSource(Registry),
            ImmediateHitModule);
        PersonalSummonModule = new CompanionPersonalSummonModule(
            Factory,
            Registry.Grid,
            new RegistryPersonalSummonTargetSource(Registry),
            ImmediateHitModule);
        Party = new PartyService(App.Data, Registry, Factory, ProjectileModule, ImmediateHitModule, PersistentFieldModule, State, PersonalSummonModule);
        CanonicalCompanionCasts = new CanonicalCompanionCastStream();
        Party.BindCanonicalCompanionCastStream(CanonicalCompanionCasts);
        PassiveRoster = new PassiveRosterState();
        PassiveEffects = new CompanionPassiveCombatResolver(App.Data, PassiveRoster);
        Party.BindPassiveRoster(PassiveRoster, PassiveEffects);
        Synergies = new SynergyActivationState(App.Data);
        Party.BindSynergyActivationState(Synergies);
        Build1SynergyProgression = new Build1SynergyProgression(App.Data, Synergies, State, Party, Registry, ImmediateHitModule);
        Party.BindBuild1SynergyProgression(Build1SynergyProgression);
        RunTraitOffers = new RunTraitOfferCoordinator(RunTraits);
        RunTraitEffects = new RunTraitEffectCoordinator(RunTraits, App.Data, Registry, ImmediateHitModule);
        if (CompanionRecordingProductionHost.IsRecordingProfile(cardPoolDefinition))
        {
            CompanionRuntimePresentationSet presentationSet = catalog?.CompanionRuntime
                ?? throw new InvalidOperationException(
                    "[RunServices] Recording companion presentation set is missing.");
            RecordingCompanions = new CompanionRecordingProductionHost(
                App.Data,
                Registry,
                ProjectileModule,
                ImmediateHitModule,
                PersistentFieldModule,
                presentationSet);
            Party.BindCompanionRuntimeCompatibility(RecordingCompanions.Adapter);
            RecordingCompanions.Adapter.RosterChanged += OnRecordingCompanionRosterChanged;
        }
        SynergyTriggers = new SynergyTriggerState(Synergies);
        Party.BindRunTraitEffectCoordinator(RunTraitEffects, SynergyTriggers);
        DamageContributions = new DamageContributionLedger(App.Data);
        Party.BindDamageContributionLedger(DamageContributions);
        _mixedCommand = new MixedCommandRunModule(App.Data, SynergyTriggers, Party);
        Party.BindMixedCommandRunModule(_mixedCommand);
        _healingBond = new HealingBondRunModule(App.Data, SynergyTriggers, Party, Registry);
        Party.BindHealingBondRunModule(_healingBond);
        UndeadSummon = new UndeadSummonRunModule(App.Data, SynergyTriggers, Registry, Party, Factory, ImmediateHitModule, Registry.Grid, SafeKnockbackWorld);
        _guardShockwave = new GuardShockwaveSynergy(App.Data, Synergies, SynergyTriggers, Party, Registry, ImmediateHitModule);
        _archerRain = new ArcherRainSynergy(App.Data, Synergies, SynergyTriggers, Party, Registry, ImmediateHitModule);
        MagicChain = new MagicChainSynergy(App.Data, Synergies, SynergyTriggers, Party, Registry, ProjectileModule, CanonicalCompanionCasts);
        _explosionChain = new ExplosionChainSynergy(App.Data, Synergies, SynergyTriggers, State, Registry, ImmediateHitModule);
        _beastHunt = new BeastHuntSynergy(App.Data, Synergies, SynergyTriggers, Party, Registry, ImmediateHitModule, SafeKnockbackWorld);
        _companionUnlockProgressBinder = new CompanionUnlockProgressRunBinder(App.CompanionUnlockProgress, State);
        Spawner = new RuntimeObjectSpawner(this);
    }

    internal void ResetRunState()
    {
        FixedCardPool.ResetRunState();
        CardEffectRuntime.ResetRunState();
        PassiveRoster?.Reset();
        ResetSynergyRuntimeForResult();
        CanonicalCompanionCasts?.Reset();
        RecordingCompanions?.Reset();
        Party.ResetRunState();
        PersonalSummonModule?.Reset();
        Lizzo.PV.P0.Units.BossArena.Clear();
    }

    private void ResetSynergyRuntimeForResult()
    {
        SynergyTriggers?.Reset();
        Build1SynergyProgression?.Reset();
        _mixedCommand?.Reset();
        _healingBond?.Reset();
        _archerRain?.Reset();
        MagicChain?.Reset();
        _explosionChain?.Reset();
        _beastHunt?.Reset();
        UndeadSummon?.ResetForResult();
    }

    internal void ResetRuntimeForResult()
    {
        PersistentFieldModule.Reset();
        PersonalSummonModule.Reset();
        PassiveRoster.Reset();
        ResetSynergyRuntimeForResult();
        RecordingCompanions?.StopForResult();
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
            RecordingCompanions?.Advance(
                deltaTime,
                isPaused || isHitStopActive,
                commander.transform);
        }

        TickSynergyRuntime(deltaTime, time, frameCount, isPaused);
        PersistentFieldModule.Tick(time);
        PersonalSummonModule.Tick(time, deltaTime);
    }

    private void TickSynergyRuntime(float deltaTime, float time, int frameCount, bool isPaused)
    {
        SynergyTriggers.Tick(deltaTime, State.IsLoaded, isPaused, frameCount);
        Build1SynergyProgression.Tick(deltaTime, State.IsLoaded, isPaused);
        _mixedCommand.TryResolvePending(time);
        _mixedCommand.Tick(time);
        _healingBond.TryResolvePending(time);
        _healingBond.Tick(time);
        UndeadSummon.TryResolvePending(time, frameCount);
        UndeadSummon.Tick(time, deltaTime);
        _guardShockwave.TryResolvePending(time);
        _archerRain.Tick(time);
        MagicChain.TryResolvePending();
        _explosionChain.TryResolvePending();
        _beastHunt.TryResolvePending(time);
        _beastHunt.Tick(time);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        Build1RuntimeDiagnostics.Log("runtime_reset",
            Build1RuntimeDiagnostics.Text("reason", "run_services_dispose"),
            Build1RuntimeDiagnostics.Int("selected_trait_count", RunTraits.SelectionCount),
            Build1RuntimeDiagnostics.Text("guard_stage", Build1SynergyProgression.GetStage(SynergyActivationIds.GuardShockwave).ToString()),
            Build1RuntimeDiagnostics.Text("explosive_stage", Build1SynergyProgression.GetStage(SynergyActivationIds.ExplosionChain).ToString()),
            Build1RuntimeDiagnostics.Text("mixed_stage", Build1SynergyProgression.GetStage(SynergyActivationIds.MixedCommand).ToString()));
        _companionUnlockProgressBinder.Dispose();
        if (RecordingCompanions != null)
        {
            RecordingCompanions.Adapter.RosterChanged -= OnRecordingCompanionRosterChanged;
            Party.UnbindCompanionRuntimeCompatibility(RecordingCompanions.Adapter);
            RecordingCompanions.Dispose();
        }
        UndeadSummon.Dispose();
        Party.UnbindHealingBondRunModule(_healingBond);
        _healingBond.Dispose();
        Party.UnbindMixedCommandRunModule(_mixedCommand);
        _mixedCommand.Dispose();
        Party.UnbindRunTraitEffectCoordinator(RunTraitEffects);
        RunTraitEffects.Dispose();
        Party.UnbindDamageContributionLedger(DamageContributions);
        Party.UnbindBuild1SynergyProgression(Build1SynergyProgression);
        Build1SynergyProgression.Dispose();
        Party.Dispose();
        SynergyTriggers.Dispose();
        _guardShockwave.Dispose();
        _archerRain.Dispose();
        MagicChain.Dispose();
        _explosionChain.Dispose();
        _beastHunt.Dispose();
        CanonicalCompanionCasts.Dispose();
        Synergies.Dispose();
        DamageContributions.Dispose();
        PersonalSummonModule.Dispose();
        PersistentFieldModule.Dispose();
        Registry.Clear();
        Factory.Clear();
        LogRestartResetPostcondition();
        RunTraitOffers.Dispose();
        RunTraits.Dispose();
        State.Dispose();
    }

    internal void BindVisibilityQuery(IWorldVisibilityQuery visibilityQuery)
    {
        _archerRain.BindVisibilityQuery(visibilityQuery);
    }

    private void OnRecordingCompanionRosterChanged(CompanionRosterCommandKind commandKind)
    {
        var slots = RecordingCompanions.Adapter.GetSquadSlotSnapshot();
        Synergies.Refresh(slots);
        Build1SynergyProgression.Refresh(slots);
        if (commandKind == CompanionRosterCommandKind.Promote)
            RunTraitEffects.ReportPromotionCommitted(UnityEngine.Time.time);
    }

    private void LogRestartResetPostcondition()
    {
        int enemyResidualCount = Registry.EnemyResidualCount;
        int expResidualCount = Registry.ExpResidualCount;
        int poolActiveResidualCount = Pool.ActiveCount;
        bool isClean = enemyResidualCount == 0 && expResidualCount == 0 && poolActiveResidualCount == 0;
        string[] parameters =
        {
            "reason=run_services_dispose",
            $"enemy_residual_count={enemyResidualCount}",
            $"exp_residual_count={expResidualCount}",
            $"pool_active_residual_count={poolActiveResidualCount}",
            $"invariant_zero={isClean.ToString().ToLowerInvariant()}",
        };

        P0Telemetry.Log(P0Telemetry.RestartResetPostcondition, parameters);
        if (isClean == false)
        {
            P0Telemetry.Log(P0Telemetry.RestartResetResidualViolation, parameters);
            UnityEngine.Debug.LogError($"[RunServices] Restart/reset residual invariant failed. enemy={enemyResidualCount}, exp={expResidualCount}, pool_active={poolActiveResidualCount}.");
        }

        P0Telemetry.FlushRunLog("restart_reset_postcondition");
    }
}
