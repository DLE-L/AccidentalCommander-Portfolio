using System;
using System.Collections.Generic;
using Lizzo.PV.Combat;
using Lizzo.PV.Combat.Fields;
using Lizzo.PV.Combat.Projectiles;
using Lizzo.PV.Data;
using Lizzo.PV.Flow;
using Lizzo.PV.Legion.Presentation;
using Lizzo.PV.P0.Config;
using Lizzo.PV.P0.Cards;
using Lizzo.PV.P0.Presentation;
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
        private readonly CompanionReturningAttackCombatResolver _canonicalReturningAttackCombat;
        private readonly CompanionCurseDeathPullResolver _canonicalCurseDeathPull;
        private readonly CompanionGrowthScaleResolver _companionGrowthScale;
        private readonly IPartyRosterRuntimeView _legacyRosterView;
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
            : this(data, registry, factory, projectileModule, immediateHitModule, persistentFieldModule, null)
        {
        }

        public PartyService(
            IDataProvider data,
            RuntimeObjectRegistry registry,
            IPrefabFactory factory,
            ICombatProjectileModule projectileModule,
            ICombatImmediateHitModule immediateHitModule,
            ICombatPersistentFieldModule persistentFieldModule,
            RunState runState)
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
            _canonicalReturningAttackCombat = new CompanionReturningAttackCombatResolver(_data);
            _canonicalCurseDeathPull = new CompanionCurseDeathPullResolver(_data);
            _companionGrowthScale = new CompanionGrowthScaleResolver(_data);
            _incomingDamage = new CompanionIncomingDamageResolver();
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

        internal RuntimeObjectRegistry Registry => _registry;
        internal IDataProvider Data => _data;
        internal float RunElapsedSeconds => _runState == null ? 0.0f : _runState.ElapsedSeconds;
        internal IPrefabFactory Factory => _factory;
        internal ICombatProjectileModule ProjectileModule => _projectileModule;
        internal ICombatImmediateHitModule ImmediateHitModule => _immediateHitModule;
        internal ICombatPersistentFieldModule PersistentFieldModule => _persistentFieldModule;
        internal FormationService Formation => _formation;
        internal PartyRosterState Roster => _roster;

        internal CompanionMeleeCombatResolver CanonicalMeleeCombat => _canonicalMeleeCombat;
        internal CompanionProjectileCombatResolver CanonicalProjectileCombat => _canonicalProjectileCombat;
        internal CompanionOwnedProxyCombatResolver CanonicalOwnedProxyCombat => _canonicalOwnedProxyCombat;
        internal CompanionWolfOwnedProxyCombatResolver CanonicalWolfOwnedProxyCombat => _canonicalWolfOwnedProxyCombat;
        internal CompanionRangedSupportCombatResolver CanonicalRangedSupportCombat => _canonicalRangedSupportCombat;
        internal CompanionTargetAreaCombatResolver CanonicalTargetAreaCombat => _canonicalTargetAreaCombat;
        internal CompanionPersistentFieldCombatResolver CanonicalPersistentFieldCombat => _canonicalPersistentFieldCombat;
        internal CompanionChainCombatResolver CanonicalChainCombat => _canonicalChainCombat;
        internal CompanionReturningAttackCombatResolver CanonicalReturningAttackCombat => _canonicalReturningAttackCombat;
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

        public void IgnoreFriendlyBodyCollisionsWithEnemy(MonsterController monster) => CompanionCollisionPolicyModule.ApplyCollisionPolicyToEnemy(this, monster);

        internal T RequireComponent<T>(GameObject owner) where T : Component
        {
            T component = owner == null ? null : owner.GetComponent<T>();
            if (component == null)
                throw new InvalidOperationException($"Companion prefab is missing required component: {typeof(T).Name}");

            return component;
        }

    }
}
