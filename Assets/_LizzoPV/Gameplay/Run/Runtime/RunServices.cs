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
    public CompanionPassiveCombatResolver PassiveEffects { get; }
    public SynergyActivationState Synergies { get; }
    public DamageContributionLedger DamageContributions { get; }
    public SynergyTriggerState SynergyTriggers { get; }
    public Build1SynergyProgression Build1SynergyProgression { get; }
    public MixedCommandRunModule MixedCommand { get; }
    public HealingBondRunModule HealingBond { get; }
    public UndeadSummonRunModule UndeadSummon { get; }
    public GuardShockwaveSynergy GuardShockwave { get; }
    public ArcherRainSynergy ArcherRain { get; }
    public MagicChainSynergy MagicChain { get; }
    public ExplosionChainSynergy ExplosionChain { get; }
    public BeastHuntSynergy BeastHunt { get; }
    public CanonicalCompanionCastStream CanonicalCompanionCasts { get; }
    public SafeKnockbackWorld SafeKnockbackWorld { get; }
    public RunContext Context { get; }
    public RunTraitRunState RunTraits { get; }
    public RunTraitOfferCoordinator RunTraitOffers { get; }
    public RunTraitEffectCoordinator RunTraitEffects { get; }

    readonly CompanionUnlockProgressRunBinder _companionUnlockProgressBinder;

    bool _disposed;

    public RunServices(AppServices app, RunState state, RuntimeObjectRegistry registry, ObjectPoolService pool, IPrefabFactory factory)
        : this(app, state, registry, pool, factory, RunContext.Normal)
    {
    }

    public RunServices(AppServices app, RunState state, RuntimeObjectRegistry registry, ObjectPoolService pool, IPrefabFactory factory, RunContext context, SafeKnockbackWorld safeKnockbackWorld = null)
    {
        App = app ?? throw new ArgumentNullException(nameof(app));
        State = state ?? throw new ArgumentNullException(nameof(state));
        Registry = registry ?? throw new ArgumentNullException(nameof(registry));
        Pool = pool ?? throw new ArgumentNullException(nameof(pool));
        Factory = factory ?? throw new ArgumentNullException(nameof(factory));
        Context = context;
        SafeKnockbackWorld = safeKnockbackWorld;
        RunTraits = new RunTraitRunState();
        FeedbackPresentationSet feedback = PresentationCatalogProvider.TryGetCatalog(out PresentationCatalog catalog)
            ? catalog.Feedback
            : null;
        ProjectileModule = new CombatProjectileModule(Factory, Registry, feedback);
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
        SynergyTriggers = new SynergyTriggerState(Synergies);
        Party.BindRunTraitEffectCoordinator(RunTraitEffects, SynergyTriggers);
        DamageContributions = new DamageContributionLedger(App.Data);
        Party.BindDamageContributionLedger(DamageContributions);
        MixedCommand = new MixedCommandRunModule(App.Data, SynergyTriggers, Party);
        Party.BindMixedCommandRunModule(MixedCommand);
        HealingBond = new HealingBondRunModule(App.Data, SynergyTriggers, Party, Registry);
        Party.BindHealingBondRunModule(HealingBond);
        UndeadSummon = new UndeadSummonRunModule(App.Data, SynergyTriggers, Registry, Party, Factory, ImmediateHitModule, Registry.Grid, SafeKnockbackWorld);
        GuardShockwave = new GuardShockwaveSynergy(App.Data, Synergies, SynergyTriggers, Party, Registry, ImmediateHitModule);
        ArcherRain = new ArcherRainSynergy(App.Data, Synergies, SynergyTriggers, Party, Registry, ImmediateHitModule);
        MagicChain = new MagicChainSynergy(App.Data, Synergies, SynergyTriggers, Party, Registry, ProjectileModule, CanonicalCompanionCasts);
        ExplosionChain = new ExplosionChainSynergy(App.Data, Synergies, SynergyTriggers, State, Registry, ImmediateHitModule);
        BeastHunt = new BeastHuntSynergy(App.Data, Synergies, SynergyTriggers, Party, Registry, ImmediateHitModule, SafeKnockbackWorld);
        _companionUnlockProgressBinder = new CompanionUnlockProgressRunBinder(App.CompanionUnlockProgress, State);
        Spawner = new RuntimeObjectSpawner(this);
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
        UndeadSummon.Dispose();
        Party.UnbindHealingBondRunModule(HealingBond);
        HealingBond.Dispose();
        Party.UnbindMixedCommandRunModule(MixedCommand);
        MixedCommand.Dispose();
        Party.UnbindRunTraitEffectCoordinator(RunTraitEffects);
        RunTraitEffects.Dispose();
        Party.UnbindDamageContributionLedger(DamageContributions);
        Party.UnbindBuild1SynergyProgression(Build1SynergyProgression);
        Build1SynergyProgression.Dispose();
        Party.Dispose();
        SynergyTriggers.Dispose();
        GuardShockwave.Dispose();
        ArcherRain.Dispose();
        MagicChain.Dispose();
        ExplosionChain.Dispose();
        BeastHunt.Dispose();
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

    public void BindVisibilityQuery(IWorldVisibilityQuery visibilityQuery)
    {
        ArcherRain.BindVisibilityQuery(visibilityQuery);
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
