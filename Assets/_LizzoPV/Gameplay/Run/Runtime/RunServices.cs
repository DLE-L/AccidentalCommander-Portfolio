using Lizzo.PV.Gameplay.Units;
using System;
using Lizzo.PV.Combat;
using Lizzo.PV.Combat.Fields;
using Lizzo.PV.Combat.Projectiles;
using Lizzo.PV.Legion.Summons;
using Lizzo.PV.Flow;
using Lizzo.PV.Legion;
using Lizzo.PV.Legion.Combat;
using Lizzo.PV.Gameplay.Telemetry;
using Lizzo.PV.Gameplay.CardOffer;
using Lizzo.PV.Gameplay.CardOffer;
using Lizzo.PV.Gameplay.Run;
using Lizzo.PV.Gameplay.Diagnostics;
using Lizzo.PV.Gameplay.Presentation;
using Lizzo.PV.Legion.RunCore;
using Lizzo.PV.Presentation;

public sealed class RunServices
{
    public Lizzo.PV.Gameplay.Commander.CommanderStatusState CommanderStatuses { get; } = new Lizzo.PV.Gameplay.Commander.CommanderStatusState();
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
    public Lizzo.PV.Legion.Presentation.CompanionConditionPresentation ConditionPresentation { get; }
    private readonly Lizzo.PV.Legion.Presentation.PersonalSummonAudioPlayer _summonAudio;
    private readonly Lizzo.PV.Legion.Presentation.SwordCombatAudio _swordAudio;
    private readonly Lizzo.PV.Legion.Presentation.ClericSanctuaryVisual _sanctuaryVisual;
    private readonly Lizzo.PV.Legion.Presentation.ClericCombatAudio _clericAudio;
    private readonly CombatHitVisualPlayer _hitVisuals;
    private readonly Lizzo.PV.Legion.Presentation.FalconCombatAudio _falconAudio;
    private readonly Lizzo.PV.Legion.Presentation.BombFragmentAudio _bombFragmentAudio;
    private readonly Lizzo.PV.Legion.Presentation.FireFieldVisual _fireFieldVisual;
    private readonly Lizzo.PV.Legion.Presentation.FireFieldAudio _fireFieldAudio;
    private readonly Lizzo.PV.Legion.Presentation.LightningChainVisual _lightningVisual;
    private readonly Lizzo.PV.Legion.Presentation.LightningCombatAudio _lightningAudio;
    private readonly Lizzo.PV.Legion.Presentation.WolfAttackVisual _wolfVisual;
    private readonly Lizzo.PV.Legion.Presentation.WolfAttackAudio _wolfAudio;
    private readonly Lizzo.PV.Legion.Presentation.CompanionOrbitVisual _wraithVisual;
    private readonly Lizzo.PV.Legion.Presentation.WraithCombatAudio _wraithAudio;
    private readonly Lizzo.PV.Legion.Presentation.CompanionOrbitVisual _reaperVisual;
    private readonly Lizzo.PV.Legion.Presentation.ScytheOrbitAudio _reaperAudio;
    public CompanionEnemyDeathCombatEffects CompanionEnemyDeathEffects { get; }
    internal SafeKnockbackWorld SafeKnockbackWorld { get; }
    public RunContext Context { get; }
    public CompanionRuntimeProductionHost CompanionRuntimeHost { get; }
    public CompanionSynergyProductionHost ProductionSynergies { get; }
    public WorldFeedbackRuntime WorldFeedback { get; }
    internal EnemyDeathResolver EnemyDeaths { get; }
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

        CompanionAttackPowers = new CompanionAttackPowerState(App.Data);
        var immediateHitModule = new CombatImmediateHitModule(CompanionAttackPowers.ResolveDamage);
        ImmediateHitModule = immediateHitModule;
        _hitVisuals = new CombatHitVisualPlayer(immediateHitModule, Factory, catalog?.HitVisuals);
        ProjectileModule = new CombatProjectileModule(Factory, Registry, ImmediateHitModule, projectiles);
        if (worldFeedbackProfiles != null)
            WorldFeedback = new WorldFeedbackRuntime(worldFeedbackProfiles, immediateHitModule, State, () => CompanionEnemyDeathEffects.CursePullRadius);
        if (runRewardDefinition != null)
            ResultRewards = new RunRewardSettlementService(App.AccountWallet, runRewardDefinition);
        PersistentFieldModule = new CombatPersistentFieldModule(
            new RegistryPersistentFieldTargetSource(Registry),
            ImmediateHitModule);
        PersonalSummonModule = new CompanionPersonalSummonModule(
            Factory,
            new RegistryPersonalSummonTargetSource(Registry),
            ImmediateHitModule);
        Party = new PartyService(App.Data, Registry, State);
        CanonicalCompanionCasts = new CanonicalCompanionCastStream();
        CombatTelemetry = new RunCombatTelemetry(CanonicalCompanionCasts);
        WorldFeedback?.BindCanonicalCompanionCasts(CanonicalCompanionCasts);
        PassiveRoster = new PassiveRosterState();
        PassiveEffects = new CompanionPassiveCombatResolver(
            App.Data,
            PassiveRoster,
            () => CompanionRuntimeHost == null
                ? 0
                : CompanionRuntimeHost.Adapter.ActiveCompanionSlotCount);
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
            Factory,
            CanonicalCompanionCasts,
            PassiveEffects,
            PassiveRoster,
            position => FirstPromotionCombat?.GetSanctuaryAttackIntervalDivisor(
                new UnityEngine.Vector3(position.X, position.Y, 0.0f), UnityEngine.Time.time) ?? 1.0f);
        Party.BindCompanionRuntime(CompanionRuntimeHost.Adapter);
        CompanionPromotionCombatContext promotionCombatContext =
            new CompanionPromotionCombatContext(Registry, CompanionRuntimeHost);
        FirstPromotionCombat = new CompanionFirstPromotionCombatRunModule(
            App.Data,
            promotionCombatContext,
            ProjectileModule,
            ImmediateHitModule,
            CanonicalCompanionCasts, PassiveEffects.Resolve, CompanionRuntimeHost.CombatEvents);
        _lightningVisual = new Lizzo.PV.Legion.Presentation.LightningChainVisual(CompanionRuntimeHost.CombatEvents, Factory, presentationSet.LightningChainPrefab);
        _wolfVisual = new Lizzo.PV.Legion.Presentation.WolfAttackVisual(CompanionRuntimeHost.WolfAttack, Factory, presentationSet.WolfPackPrefab);
        _wolfAudio = new Lizzo.PV.Legion.Presentation.WolfAttackAudio(CompanionRuntimeHost.WolfAttack, immediateHitModule);
        _lightningAudio = new Lizzo.PV.Legion.Presentation.LightningCombatAudio(CompanionRuntimeHost.CombatEvents);
        _falconAudio = new Lizzo.PV.Legion.Presentation.FalconCombatAudio(immediateHitModule);
        _bombFragmentAudio = new Lizzo.PV.Legion.Presentation.BombFragmentAudio(immediateHitModule);
        _clericAudio = new Lizzo.PV.Legion.Presentation.ClericCombatAudio(FirstPromotionCombat, CanonicalCompanionCasts, immediateHitModule, Registry);
        _fireFieldVisual = new Lizzo.PV.Legion.Presentation.FireFieldVisual((CombatPersistentFieldModule)PersistentFieldModule, Factory, presentationSet.FireFieldPrefab);
        _fireFieldAudio = new Lizzo.PV.Legion.Presentation.FireFieldAudio((CombatPersistentFieldModule)PersistentFieldModule, Factory, presentationSet.FireFieldLoopPrefab);
        _sanctuaryVisual = new Lizzo.PV.Legion.Presentation.ClericSanctuaryVisual(FirstPromotionCombat, Factory, presentationSet.SanctuaryPrefab);
        _swordAudio = new Lizzo.PV.Legion.Presentation.SwordCombatAudio(FirstPromotionCombat, immediateHitModule, CompanionRuntimeHost.CombatEvents);
        SecondPromotionCombat = new CompanionSecondPromotionCombatRunModule(
            App.Data,
            promotionCombatContext,
            ImmediateHitModule,
            PersistentFieldModule,
            CanonicalCompanionCasts, PassiveEffects.Resolve, CompanionRuntimeHost.CombatEvents);
        ThirdPromotionCombat = new CompanionThirdPromotionCombatRunModule(
            App.Data,
            promotionCombatContext,
            ImmediateHitModule,
            PersonalSummonModule,
            CanonicalCompanionCasts,
            State,
            PassiveEffects.Resolve, CompanionRuntimeHost.WolfAttack);
        _wraithVisual = new Lizzo.PV.Legion.Presentation.CompanionOrbitVisual(ThirdPromotionCombat.WraithOrbit, Factory, presentationSet.WraithOrbitPrefab, presentationSet.WraithOrbitBoundaryPrefab);
        _wraithAudio = new Lizzo.PV.Legion.Presentation.WraithCombatAudio(ThirdPromotionCombat.WraithOrbit, CompanionRuntimeHost.CombatEvents, immediateHitModule);
        _reaperVisual = new Lizzo.PV.Legion.Presentation.CompanionOrbitVisual(ThirdPromotionCombat.ReaperOrbit, Factory, presentationSet.ReaperOrbitPrefab, presentationSet.ReaperOrbitBoundaryPrefab);
        _reaperAudio = new Lizzo.PV.Legion.Presentation.ScytheOrbitAudio(ThirdPromotionCombat.ReaperOrbit, immediateHitModule);
        ConditionPresentation = new Lizzo.PV.Legion.Presentation.CompanionConditionPresentation(
            Factory, CompanionRuntimeHost, new ICompanionConditionSource[] { FirstPromotionCombat, SecondPromotionCombat.ConditionSource, ThirdPromotionCombat.ConditionSource }, presentationSet.ConditionVisuals);
        if (presentationSet.SummonAudioPrefab != null)
        {
            var audioObject = Factory.Rent(presentationSet.SummonAudioPrefab.gameObject, "PersonalSummonAudio");
            if (audioObject != null)
            {
                _summonAudio = audioObject.GetComponent<Lizzo.PV.Legion.Presentation.PersonalSummonAudioPlayer>();
                _summonAudio.Bind((CompanionPersonalSummonModule)PersonalSummonModule);
            }
        }
        CompanionEnemyDeathEffects = new CompanionEnemyDeathCombatEffects(
            App.Data,
            Registry,
            SecondPromotionCombat,
            ThirdPromotionCombat, PassiveEffects.ResolveCursePullRadiusMultiplier);
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
        WorldFeedback?.BindSpawner(Spawner);
        EnemyDeaths = new EnemyDeathResolver(Registry, State, Spawner, App.Data, Context,
            ProductionSynergies, CompanionEnemyDeathEffects);
        WorldFeedback?.BindEnemyDeaths(EnemyDeaths);
        WorldFeedback?.BindCardSelections(CardOffers, Registry);
    }

    public void BindEnemy(EnemyActor enemy)
    {
        if (enemy == null) throw new ArgumentNullException(nameof(enemy));
        enemy.BindRuntime(Registry, App.Data, Tuning, ImmediateHitModule, CombatTelemetry,
            SafeKnockbackWorld, EnemyDeaths);
        WorldFeedback?.ObserveEnemy(enemy);
    }

    public void BindCommander(CommanderActor commander)
    {
        if (commander == null) throw new ArgumentNullException(nameof(commander));
        var collector = new Lizzo.PV.Gameplay.Commander.CommanderGemCollector(State, Registry);
        WorldFeedback?.BindCommanderExperience(collector);
        commander.BindRuntime(App.Data, collector, PassiveRoster, PassiveEffects,
            ProductionSynergies, Context, Tuning);
    }

    public CompanionAttackPowerState CompanionAttackPowers { get; private set; }

    internal void ResetRunState()
    {
        CompanionAttackPowers.Reset();
        _hitVisuals.Clear();
        CommanderStatuses.Clear();
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
        ConditionPresentation.Clear();
        _lightningVisual.Clear();
        _summonAudio?.StopPlayback();
        Lizzo.PV.Gameplay.Units.BossArena.Clear();
    }

    internal void ResetRuntimeForResult()
    {
        _hitVisuals.Clear();
        CommanderStatuses.Clear();
        PersistentFieldModule.Reset();
        PersonalSummonModule.Reset();
        FirstPromotionCombat.Reset();
        SecondPromotionCombat.Reset();
        ThirdPromotionCombat.Reset();
        PassiveRoster.Reset();
        CompanionRuntimeHost?.StopForResult();
        ProductionSynergies?.Reset();
        ConditionPresentation.Clear();
        _lightningVisual.Clear();
        _summonAudio?.StopPlayback();
    }

    internal void TickRuntime(
        float deltaTime,
        float time,
        int frameCount,
        bool isPaused,
        bool isHitStopActive)
    {
        try { _fireFieldAudio.SetPaused(isPaused || isHitStopActive); }
        catch (Exception exception) { UnityEngine.Debug.LogException(exception); }
        CommanderActor commander = Registry.Player;
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
        RefreshCommanderStatuses(time);
        try { ConditionPresentation.Tick(); }
        catch (Exception exception) { UnityEngine.Debug.LogException(exception); }
    }

    private void RefreshCommanderStatuses(float time)
    {
        var player = Registry.Player;
        var active = Lizzo.PV.Gameplay.Commander.CommanderStatusKind.None;
        if (!_disposed && State.IsLoaded && player != null && player.isActiveAndEnabled && player.Hp > 0)
        {
            if (FirstPromotionCombat.GetSanctuaryAttackIntervalDivisor(player.transform.position, time) > 1f)
                active |= Lizzo.PV.Gameplay.Commander.CommanderStatusKind.SanctuaryHaste;
            if (ProductionSynergies.HasSanctuaryCharge)
                active |= Lizzo.PV.Gameplay.Commander.CommanderStatusKind.SanctuaryGuard;
            if (player.IsPostHitInvulnerable(time))
                active |= Lizzo.PV.Gameplay.Commander.CommanderStatusKind.PostHitInvulnerability;
        }
        CommanderStatuses.Publish(active);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        CommanderStatuses.Clear();
        _hitVisuals.Dispose();
        _falconAudio.Dispose();
        _bombFragmentAudio.Dispose();
        _fireFieldVisual.Dispose();
        _fireFieldAudio.Dispose();
        _lightningVisual.Dispose();
        _lightningAudio.Dispose();
        _wolfVisual.Dispose();
        _wolfAudio.Dispose();
        _wraithVisual.Dispose();
        _wraithAudio.Dispose();
        _reaperVisual.Dispose();
        _reaperAudio.Dispose();
        _clericAudio.Dispose();
        _sanctuaryVisual.Dispose();
        _swordAudio.Dispose();
        ConditionPresentation.Dispose();
        if (_summonAudio != null) Factory.Release(_summonAudio.gameObject);
        _companionUnlockProgressBinder.Dispose();
        CombatTelemetry?.LogSummary("run_services_dispose");
        ProductionSynergies?.Dispose();
        if (CompanionRuntimeHost != null)
        {
            CompanionRuntimeHost.Adapter.RosterChanged -= OnCompanionRosterChanged;
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
        ConditionPresentation?.RequestRefresh();
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
