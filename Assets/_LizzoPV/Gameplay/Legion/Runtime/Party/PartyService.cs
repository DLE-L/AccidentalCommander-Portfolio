using System;
using System.Collections.Generic;
using Lizzo.PV.Combat;
using Lizzo.PV.Combat.Fields;
using Lizzo.PV.Combat.Projectiles;
using Lizzo.PV.Combat.Summons;
using Lizzo.PV.Data;
using Lizzo.PV.Flow;
using Lizzo.PV.Legion.Presentation;
using Lizzo.PV.P0.Config;
using Lizzo.PV.P0.Cards;
using Lizzo.PV.P0.Presentation;
using Lizzo.PV.P0.Skills.Guard;
using Lizzo.PV.P0.Telemetry;
using Lizzo.PV.P0.Units;
using Lizzo.PV.P0.Visuals;
using Lizzo.PV.Legion.Combat.Attacks;
using Lizzo.PV.Legion.Combat;
using Lizzo.PV.Legion.Party.Roster;
using Lizzo.PV.Legion.Synergy;
using Lizzo.PV.Gameplay.RunTraits;
using Lizzo.PV.Gameplay.World;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    public enum CompanionKind
    {
        ShieldSoldier,
        ShieldCaptain,
        Swordsman,
        Cleric,
        Archer,
    }

    public sealed partial class PartyService : IDisposable, ICanonicalCompanionRosterView, ICanonicalCompanionCardProgressView
    {
        internal const string SHIELD_FAMILY_TAG = "shield_family";
        internal const string SWORD_FAMILY_TAG = "sword_family";
        internal const string CLERIC_FAMILY_TAG = "cleric_family";
        internal const string RANGED_FAMILY_TAG = "ranged_family";
        internal const string SHIELD_SOLDIER_PREFAB_KEY = "P0/Units/Companions/ShieldSoldier.prefab";
        internal const string SHIELD_CAPTAIN_PREFAB_KEY = "P0/Units/Companions/ShieldCaptain.prefab";
        internal const string SWORDSMAN_PREFAB_KEY = "P0/Units/Companions/Swordsman.prefab";
        internal const string CLERIC_PREFAB_KEY = "P0/Units/Companions/Cleric.prefab";
        internal const string ARCHER_PREFAB_KEY = "P0/Units/Companions/Archer.prefab";

        private readonly IDataProvider _data;
        private readonly RuntimeObjectRegistry _registry;
        private readonly IPrefabFactory _factory;
        private readonly ICombatProjectileModule _projectileModule;
        private readonly ICombatImmediateHitModule _immediateHitModule;
        private readonly ICombatPersistentFieldModule _persistentFieldModule;
        private readonly RunState _runState;
        private readonly FormationService _formation;
        private readonly PartyRosterState _roster;
        private readonly CompanionMeleeCombatResolver _canonicalMeleeCombat;
        private readonly CompanionProjectileCombatResolver _canonicalProjectileCombat;
        private readonly CompanionOwnedProxyCombatResolver _canonicalOwnedProxyCombat;
        private readonly CompanionWolfOwnedProxyCombatResolver _canonicalWolfOwnedProxyCombat;
        private readonly CompanionRangedSupportCombatResolver _canonicalRangedSupportCombat;
        private readonly CompanionTargetAreaCombatResolver _canonicalTargetAreaCombat;
        private readonly CompanionPersistentFieldCombatResolver _canonicalPersistentFieldCombat;
        private readonly CompanionChainCombatResolver _canonicalChainCombat;
        private readonly CompanionGrowthScaleResolver _companionGrowthScale;
        private readonly CompanionPersonalSummonKillCoordinator _personalSummonKillCoordinator;
        private readonly CompanionIncomingDamageResolver _incomingDamage;
        private readonly PartyResultSummaryModule _resultSummary;
        private readonly IPartyRosterRuntimeView _legacyRosterView;
        private PassiveRosterState _passiveRoster;
        private CompanionPassiveCombatResolver _passiveCombat;
        private SynergyActivationState _synergies;
        private HealingBondRunModule _healingBondRunModule;
        private MixedCommandRunModule _mixedCommandRunModule;
        private RunTraitEffectCoordinator _runTraitEffects;
        private SynergyTriggerState _synergyTriggers;
        private DamageContributionLedger _damageContributions;
        private IPartyRosterRuntimeView _rosterView;
        internal readonly List<AllyFollower> Allies = new List<AllyFollower>();
        internal readonly List<AllyFollower> ShieldSoldiers = new List<AllyFollower>();
        internal readonly List<CompanionRuntime> Companions = new List<CompanionRuntime>();

        internal int ShieldSoldierCountState;
        internal int ShieldCaptainCountState;
        internal int SwordsmanCountState;
        internal int ClericCountState;
        internal int ArcherCountState;
        internal bool GuardSquadActivatedState;
        internal bool WasSlotFullState;
        internal float AllyAttackMultiplierState = 1.0f;
        internal float GuardWallBonusMultiplierState = 1.0f;

        public PartyService(
            IDataProvider data,
            RuntimeObjectRegistry registry,
            IPrefabFactory factory,
            ICombatProjectileModule projectileModule,
            ICombatImmediateHitModule immediateHitModule,
            ICombatPersistentFieldModule persistentFieldModule)
            : this(data, registry, factory, projectileModule, immediateHitModule, persistentFieldModule, null, null)
        {
        }

        public PartyService(
            IDataProvider data,
            RuntimeObjectRegistry registry,
            IPrefabFactory factory,
            ICombatProjectileModule projectileModule,
            ICombatImmediateHitModule immediateHitModule,
            ICombatPersistentFieldModule persistentFieldModule,
            RunState runState,
            ICompanionPersonalSummonModule personalSummonModule)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _projectileModule = projectileModule ?? throw new ArgumentNullException(nameof(projectileModule));
            _immediateHitModule = immediateHitModule ?? throw new ArgumentNullException(nameof(immediateHitModule));
            _persistentFieldModule = persistentFieldModule ?? throw new ArgumentNullException(nameof(persistentFieldModule));
            _runState = runState;
            _formation = new FormationService(_registry, this);
            _roster = new PartyRosterState(_data);
            _legacyRosterView = new LegacyPartyRosterRuntimeView(_roster, () => Companions.Count);
            _rosterView = _legacyRosterView;
            _canonicalMeleeCombat = new CompanionMeleeCombatResolver(_data);
            _canonicalProjectileCombat = new CompanionProjectileCombatResolver(_data);
            _canonicalOwnedProxyCombat = new CompanionOwnedProxyCombatResolver(_data);
            _canonicalWolfOwnedProxyCombat = new CompanionWolfOwnedProxyCombatResolver(_data);
            _canonicalRangedSupportCombat = new CompanionRangedSupportCombatResolver(_data);
            _canonicalTargetAreaCombat = new CompanionTargetAreaCombatResolver(_data);
            _canonicalPersistentFieldCombat = new CompanionPersistentFieldCombatResolver(_data);
            _canonicalChainCombat = new CompanionChainCombatResolver(_data);
            _companionGrowthScale = new CompanionGrowthScaleResolver(_data);
            _personalSummonKillCoordinator = new CompanionPersonalSummonKillCoordinator(
                _data,
                _runState,
                personalSummonModule,
                Companions);
            _incomingDamage = new CompanionIncomingDamageResolver();
            _resultSummary = new PartyResultSummaryModule(this);
        }

        internal void BindPassiveRoster(PassiveRosterState passiveRoster, CompanionPassiveCombatResolver passiveEffects = null)
        {
            if (ReferenceEquals(_passiveRoster, passiveRoster)) return;
            if (_passiveRoster != null) _passiveRoster.Changed -= RefreshAllCompanionCombat;
            _passiveRoster = passiveRoster ?? throw new ArgumentNullException(nameof(passiveRoster));
            _passiveCombat = passiveEffects ?? new CompanionPassiveCombatResolver(_data, _passiveRoster);
            _passiveRoster.Changed += RefreshAllCompanionCombat;
            RefreshAllCompanionCombat();
        }

        internal void BindSynergyActivationState(SynergyActivationState synergies)
        {
            _synergies = synergies ?? throw new ArgumentNullException(nameof(synergies));
            RefreshSynergyActivations();
        }

        internal void BindCompanionRuntimeCompatibility(IPartyRosterRuntimeView compatibility)
        {
            _rosterView = compatibility
                ?? throw new ArgumentNullException(nameof(compatibility));
        }

        internal void UnbindCompanionRuntimeCompatibility(IPartyRosterRuntimeView compatibility)
        {
            if (ReferenceEquals(_rosterView, compatibility))
                _rosterView = _legacyRosterView;
        }

        Build1SynergyProgression _build1SynergyProgression;

        internal void BindBuild1SynergyProgression(Build1SynergyProgression progression)
        {
            _build1SynergyProgression = progression ?? throw new ArgumentNullException(nameof(progression));
            RefreshSynergyActivations();
        }

        internal void UnbindBuild1SynergyProgression(Build1SynergyProgression progression)
        {
            if (ReferenceEquals(_build1SynergyProgression, progression))
                _build1SynergyProgression = null;
        }

        internal void BindHealingBondRunModule(HealingBondRunModule module)
        {
            _healingBondRunModule = module ?? throw new ArgumentNullException(nameof(module));
        }

        internal void UnbindHealingBondRunModule(HealingBondRunModule module)
        {
            if (ReferenceEquals(_healingBondRunModule, module))
                _healingBondRunModule = null;
        }

        internal void BindMixedCommandRunModule(MixedCommandRunModule module)
        {
            _mixedCommandRunModule = module ?? throw new ArgumentNullException(nameof(module));
        }

        internal void UnbindMixedCommandRunModule(MixedCommandRunModule module)
        {
            if (ReferenceEquals(_mixedCommandRunModule, module))
                _mixedCommandRunModule = null;
        }

        internal void BindRunTraitEffectCoordinator(
            RunTraitEffectCoordinator coordinator,
            SynergyTriggerState synergyTriggers)
        {
            _runTraitEffects = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
            _synergyTriggers = synergyTriggers ?? throw new ArgumentNullException(nameof(synergyTriggers));
            _synergyTriggers.BindRunTraitEffectCoordinator(_runTraitEffects);
        }

        internal void UnbindRunTraitEffectCoordinator(RunTraitEffectCoordinator coordinator)
        {
            if (ReferenceEquals(_runTraitEffects, coordinator))
            {
                _synergyTriggers?.UnbindRunTraitEffectCoordinator(coordinator);
                _synergyTriggers = null;
                _runTraitEffects = null;
            }
        }

        internal void HandlePromotionCommitted(PartyRosterChangeResult rosterCommit, float now)
        {
            if (rosterCommit == PartyRosterChangeResult.Promote)
                _runTraitEffects?.ReportPromotionCommitted(now);
        }

        internal bool ReportHealingBond(CompanionRuntime companion, in SynergyHealingEvent healingEvent)
        {
            return _healingBondRunModule != null && _healingBondRunModule.ReportHealing(companion, healingEvent);
        }

        internal bool ReportHealingBond(PlayerController player, in SynergyHealingEvent healingEvent)
        {
            return _healingBondRunModule != null && _healingBondRunModule.ReportHealing(player, healingEvent);
        }

        internal CompanionPassiveCombatModifiers ResolvePassiveCombatModifiers(string baseUnitId)
        {
            return _passiveCombat == null ? CompanionPassiveCombatModifiers.Identity : _passiveCombat.Resolve(baseUnitId);
        }

        internal CommanderPassiveModifiers ResolveCommanderPassiveModifiers()
        {
            return _passiveCombat == null ? CommanderPassiveModifiers.Identity : _passiveCombat.ResolveCommander();
        }

        internal RuntimeObjectRegistry Registry => _registry;
        internal IDataProvider Data => _data;
        internal float RunElapsedSeconds => _runState == null ? 0.0f : _runState.ElapsedSeconds;
        internal IPrefabFactory Factory => _factory;
        internal ICombatProjectileModule ProjectileModule => _projectileModule;
        internal ICombatImmediateHitModule ImmediateHitModule => _immediateHitModule;
        internal ICombatPersistentFieldModule PersistentFieldModule => _persistentFieldModule;
        internal FormationService Formation => _formation;
        internal PartyRosterState Roster => _roster;

        internal void BindArenaBounds(ArenaBounds arenaBounds)
        {
            _formation.BindArenaBounds(arenaBounds);
        }
        internal CompanionMeleeCombatResolver CanonicalMeleeCombat => _canonicalMeleeCombat;
        internal CompanionProjectileCombatResolver CanonicalProjectileCombat => _canonicalProjectileCombat;
        internal CompanionOwnedProxyCombatResolver CanonicalOwnedProxyCombat => _canonicalOwnedProxyCombat;
        internal CompanionWolfOwnedProxyCombatResolver CanonicalWolfOwnedProxyCombat => _canonicalWolfOwnedProxyCombat;
        internal CompanionRangedSupportCombatResolver CanonicalRangedSupportCombat => _canonicalRangedSupportCombat;
        internal CompanionTargetAreaCombatResolver CanonicalTargetAreaCombat => _canonicalTargetAreaCombat;
        internal CompanionPersistentFieldCombatResolver CanonicalPersistentFieldCombat => _canonicalPersistentFieldCombat;
        internal CompanionChainCombatResolver CanonicalChainCombat => _canonicalChainCombat;
        internal CompanionGrowthScale ResolveGrowthScale(string baseUnitId)
        {
            return _roster.TryGetSlot(baseUnitId, out SquadSlotState slot)
                ? _companionGrowthScale.Resolve(slot)
                : new CompanionGrowthScale(1.0f, 1.0f, 1.0f, 1);
        }

        internal bool TryActivateShieldCaptainPromotionProtection(
            PartyRosterChangeResult rosterCommit,
            string baseUnitId,
            float currentTime)
        {
            return _incomingDamage.TryActivateShieldCaptainPromotionProtection(
                rosterCommit,
                baseUnitId,
                currentTime);
        }

        internal bool TryResolveFormationAnchor(string rosterSlotId, out Vector3 anchor)
        {
            return _formation.TryResolveFormationAnchor(rosterSlotId, out anchor);
        }

        internal bool TryResolveSynergyAnchorAndRange(string rosterSlotId, out Vector3 anchor, out float attackRange)
        {
            return _formation.TryResolveSynergyAnchorAndRange(rosterSlotId, out anchor, out attackRange);
        }

        CanonicalCompanionCastStream _canonicalCompanionCasts;
        internal void BindCanonicalCompanionCastStream(CanonicalCompanionCastStream stream) => _canonicalCompanionCasts = stream;
        internal void ReportCanonicalCast(CompanionRuntime runtime, CanonicalCompanionActionKind actionKind)
        {
            if (runtime == null || runtime.IsDown) return;
            CanonicalCompanionCastIdentity identity = new CanonicalCompanionCastIdentity(runtime.GetInstanceID(), runtime.RosterSlotId, runtime.BaseUnitId, runtime.FamilyTags);
            _canonicalCompanionCasts?.TryEmit(identity, actionKind);
        }

        internal void ApplyGuardShockwaveProtection(float duration, float currentTime)
        {
            _incomingDamage.ApplyGuardShockwaveProtection(Companions, duration, currentTime);
        }

        internal void BindDamageContributionLedger(DamageContributionLedger ledger)
        {
            _damageContributions = ledger ?? throw new System.ArgumentNullException(nameof(ledger));
        }

        internal void UnbindDamageContributionLedger(DamageContributionLedger ledger)
        {
            if (ReferenceEquals(_damageContributions, ledger))
                _damageContributions = null;
        }

        internal void RecordCompanionDamagePrevention(in CompanionIncomingDamageResolution resolution)
        {
            _damageContributions?.RecordPreventedDamage(SynergyActivationIds.GuardShockwave, resolution.GuardShockwavePreventedDamage);
            _damageContributions?.RecordPreventedDamage(SynergyActivationIds.HealingBond, resolution.HealingBondPreventedDamage);
        }

        internal float ResolveCompanionIncomingDamageMultiplier(CompanionRuntime companion, float currentTime)
        {
            return _incomingDamage.ResolveIncomingDamageMultiplier(
                companion,
                currentTime,
                _healingBondRunModule);
        }

        internal CompanionIncomingDamageResolution ResolveCompanionIncomingDamage(
            CompanionRuntime companion,
            int originalDamage,
            int currentHp,
            float currentTime)
        {
            return _incomingDamage.Resolve(
                companion,
                originalDamage,
                currentHp,
                currentTime,
                _healingBondRunModule,
                _runTraitEffects);
        }

        internal float ResolveCompanionAttackIntervalDivisorForSource(string sourceId)
        {
            if (TryResolveCompanionSource(sourceId, out CompanionRuntime companion) == false)
                return 1.0f;

            return ResolveCompanionAttackIntervalDivisor(companion);
        }

        internal bool TryResolveCompanionSource(string sourceId, out CompanionRuntime result)
        {
            result = null;
            if (string.IsNullOrEmpty(sourceId))
                return false;

            for (int index = 0; index < Companions.Count; index++)
            {
                CompanionRuntime companion = Companions[index];
                if (companion != null
                    && companion.IsDown == false
                    && (companion.UnitId == sourceId || companion.BaseUnitId == sourceId))
                {
                    result = companion;
                    return true;
                }
            }

            return false;
        }

        internal bool TryResolveActiveCompanionWithFamilyTag(string familyTag, out CompanionRuntime companion)
        {
            for (int index = 0; index < Companions.Count; index++)
            {
                CompanionRuntime candidate = Companions[index];
                if (candidate != null && candidate.IsDown == false && HasFamilyTag(candidate.FamilyTags, familyTag))
                {
                    companion = candidate;
                    return true;
                }
            }

            companion = null;
            return false;
        }

        internal bool HasGuardShockwaveProtection(CompanionRuntime companion, float currentTime)
        {
            return _incomingDamage.HasGuardShockwaveProtection(companion, currentTime);
        }

        internal bool HasHealingBondKnockdownImmunity(CompanionRuntime companion)
        {
            return _incomingDamage.HasHealingBondKnockdownImmunity(
                companion,
                _healingBondRunModule);
        }

        internal float ResolveCompanionAttackIntervalDivisor(CompanionRuntime companion)
        {
            float mixedCommandDivisor = _mixedCommandRunModule?.GetAttackIntervalDivisor(companion) ?? 1.0f;
            float promotionShoutDivisor = companion == null || companion.IsDown
                ? 1.0f
                : _runTraitEffects?.GetCompanionAttackIntervalDivisor(Time.time) ?? 1.0f;
            return mixedCommandDivisor * promotionShoutDivisor;
        }

        internal float ResolveCompanionMoveSpeedMultiplier(CompanionRuntime companion)
        {
            float mixedCommandMultiplier = _mixedCommandRunModule?.GetMoveSpeedMultiplier(companion) ?? 1.0f;
            float build1ReadyMultiplier = _build1SynergyProgression?.GetMoveSpeedMultiplier(companion) ?? 1.0f;
            float emergencyRallyMultiplier = companion == null || companion.IsDown
                ? 1.0f
                : _runTraitEffects?.GetEmergencyRallyMoveSpeedMultiplier(companion.RosterSlotId, Time.time) ?? 1.0f;
            return mixedCommandMultiplier * build1ReadyMultiplier * emergencyRallyMultiplier;
        }

        public int ShieldSoldierCount => ShieldSoldierCountState;
        public int ShieldCaptainCount => ShieldCaptainCountState;
        public int SwordsmanCount => SwordsmanCountState;
        public int ClericCount => ClericCountState;
        public int ArcherCount => ArcherCountState;
        public bool IsGuardSquadActivated => GuardSquadActivatedState;
        public float GuardWallBonusMultiplier => GuardWallBonusMultiplierState;

        internal IReadOnlyList<AllyFollower> ActiveAllies => Allies;
        internal IReadOnlyList<CompanionRuntime> ActiveCompanions => Companions;
        internal int ActiveAllyCount => Allies.Count;

        /// <summary>Collects exactly one living canonical Beast actor per immutable roster slot.
        /// The formation SlotId ordering is the approved reinforced-squad representative rule.</summary>
        internal void CollectLivingBeastRepresentatives(List<CompanionRuntime> results)
        {
            if (results == null) throw new ArgumentNullException(nameof(results));
            results.Clear();
            for (int i = 0; i < Companions.Count; i++)
            {
                CompanionRuntime candidate = Companions[i];
                if (candidate == null || candidate.IsDown || string.IsNullOrEmpty(candidate.RosterSlotId)
                    || string.IsNullOrEmpty(candidate.SlotId) || HasFamilyTag(candidate.FamilyTags, "beast_family") == false)
                    continue;

                int existingIndex = -1;
                for (int resultIndex = 0; resultIndex < results.Count; resultIndex++)
                {
                    if (results[resultIndex].RosterSlotId == candidate.RosterSlotId)
                    {
                        existingIndex = resultIndex;
                        break;
                    }
                }

                if (existingIndex < 0)
                    results.Add(candidate);
                else if (string.CompareOrdinal(candidate.SlotId, results[existingIndex].SlotId) < 0)
                    results[existingIndex] = candidate;
            }
        }

        static bool HasFamilyTag(string values, string required)
        {
            if (string.IsNullOrEmpty(values) || string.IsNullOrEmpty(required)) return false;
            int start = 0;
            for (int index = 0; index <= values.Length; index++)
            {
                if (index != values.Length && values[index] != ',') continue;
                int length = index - start;
                if (length == required.Length && string.CompareOrdinal(values, start, required, 0, length) == 0)
                    return true;
                start = index + 1;
            }
            return false;
        }

        public void Dispose()
        {
            if (_passiveRoster != null)
                _passiveRoster.Changed -= RefreshAllCompanionCombat;
            _personalSummonKillCoordinator.Dispose();
            this.ResetRunState();
            _rosterView = _legacyRosterView;
        }

        public bool TryGetNecromancerKillState(string slotId, out CountableKillThresholdState state)
        {
            return _personalSummonKillCoordinator.TryGetState(slotId, out state);
        }

        public bool TryAdvanceNecromancerPersonalSummon(
            in CountableKillAttribution attribution,
            string rosterSlotId,
            bool isPromoted,
            Transform spawnOrigin)
        {
            return _personalSummonKillCoordinator.TryAdvance(
                attribution,
                rosterSlotId,
                isPromoted,
                spawnOrigin);
        }

        public void IgnoreFriendlyBodyCollisionsWithEnemy(MonsterController monster) => CompanionCollisionPolicyModule.ApplyCollisionPolicyToEnemy(this, monster);

        public void ResetRunState()
        {
            for (int i = Allies.Count - 1; i >= 0; i--)
            {
                if (Allies[i] != null)
                    _factory.Release(Allies[i].gameObject);
            }

            Allies.Clear();
            ShieldSoldiers.Clear();
            Companions.Clear();
            ShieldSoldierCountState = 0;
            ShieldCaptainCountState = 0;
            SwordsmanCountState = 0;
            ClericCountState = 0;
            ArcherCountState = 0;
            GuardSquadActivatedState = false;
            WasSlotFullState = false;
            _roster.Reset();
            _synergies?.Reset();
            _incomingDamage.Reset();
            _runTraitEffects?.ResetRunState();
            _personalSummonKillCoordinator.Reset();
            ResetCardModifiers();
            Formation.ResetRunState();
            GuardSquadSkillBehaviour.StopActive();
        }

        public float AddAllyAttackBonus(float bonusRatio)
        {
            AllyAttackMultiplierState = Mathf.Max(1.0f, AllyAttackMultiplierState + Mathf.Max(0.0f, bonusRatio));
            RefreshAllCompanionCombat();
            return AllyAttackMultiplierState;
        }

        public float AddGuardWallBonus(float bonusRatio)
        {
            GuardWallBonusMultiplierState = Mathf.Max(1.0f, GuardWallBonusMultiplierState + Mathf.Max(0.0f, bonusRatio));
            return GuardWallBonusMultiplierState;
        }

        internal T RequireComponent<T>(GameObject owner) where T : Component
        {
            T component = owner == null ? null : owner.GetComponent<T>();
            if (component == null)
                throw new InvalidOperationException($"Companion prefab is missing required component: {typeof(T).Name}");

            return component;
        }

        internal void ResetCardModifiers()
        {
            AllyAttackMultiplierState = 1.0f;
            GuardWallBonusMultiplierState = 1.0f;
        }

        internal void RefreshSynergyActivations()
        {
            _synergies?.Refresh(_roster.Snapshot);
            _build1SynergyProgression?.Refresh(_roster.Snapshot);
        }

        internal void RefreshAllCompanionCombat() => CompanionCombatSetupModule.RefreshAllCompanionCombat(this);

        internal readonly struct FormationSlot
        {
            public readonly string Id;
            public readonly Vector3 Offset;

            public FormationSlot(string id, Vector3 offset)
            {
                Id = id;
                Offset = offset;
            }
        }
    }
}
