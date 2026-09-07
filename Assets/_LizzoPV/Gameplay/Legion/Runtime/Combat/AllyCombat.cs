using System.Collections.Generic;
using Lizzo.PV.P0.Visuals;
using UnityEngine;
using Lizzo.PV.P0.Units;
using Lizzo.PV.Combat;
using Lizzo.PV.Combat.Fields;
using Lizzo.PV.Data;
using Lizzo.PV.Legion.Combat.Attacks;
using Lizzo.PV.Legion.Combat;

namespace Lizzo.PV.Legion
{
    public enum AllyAttackStyle
    {
        SingleTarget,
        FarthestTarget,
        ForwardSlash,
        ForwardPush,
        AreaPulse,
        HealCommander,
        TargetedProjectile,
        TargetedArea,
        TargetedField,
        TargetedChain,
    }

    public sealed partial class AllyCombat : MonoBehaviour
    {

        internal const float MIN_ATTACK_RANGE = 0.1f;
        internal const float KNOCKBACK_INTERNAL_COOLDOWN = 0.2f;
        internal const float FORWARD_HITBOX_BACK_PADDING = 0.35f;
        internal const float FORWARD_HITBOX_RANGE_PADDING = 0.8f;
        internal const float FORWARD_HITBOX_HALF_WIDTH_MIN = 0.85f;
        internal const float FORWARD_HITBOX_HALF_WIDTH_FACTOR = 0.75f;
        internal const float NO_TARGET_RETRY_DELAY = 0.2f;
        internal const float KNOCKBACK_SLIDE_DURATION = 0.16f;
        internal const float SHIELD_PUSH_IMPACT_BACK_OFFSET = 0.12f;
        internal const float SHIELD_PUSH_IMPACT_SCALE = 0.9f;

        internal static readonly Dictionary<int, float> NextKnockbackAllowedTimeByTarget = new Dictionary<int, float>();

        internal readonly List<MonsterController> _forwardTargets = new List<MonsterController>(16);
        internal readonly List<MonsterController> _areaTargets = new List<MonsterController>(32);
        internal readonly List<TargetAreaImpactCandidate> _targetAreaCandidates = new List<TargetAreaImpactCandidate>(32);
        internal readonly List<TargetAreaImpactCandidate> _targetAreaImpactTargets = new List<TargetAreaImpactCandidate>(32);
        internal readonly List<TargetAreaImpactCandidate> _returningAttackCandidates = new List<TargetAreaImpactCandidate>(32);
        internal readonly List<TargetAreaImpactCandidate> _returningAttackTargets = new List<TargetAreaImpactCandidate>(8);
        internal readonly HashSet<int> _wolfChainVisitedTargets = new HashSet<int>();

        internal AllyAttackStyle _attackStyle;
        internal CompanionProjectileBounceSetup _promotedProjectileBounce;
        internal int _damage;
        internal float _period;
        internal float _range;
        internal float _knockback;
        internal float _angle;
        internal int _maxForwardTargetCount = int.MaxValue;
        internal int _maxProjectileTargetCount = 1;
        internal float _noTargetRetrySeconds = NO_TARGET_RETRY_DELAY;
        internal string _sourceIdOverride;
        internal float _projectileSpeedMultiplier = 1.0f;
        internal bool _usesStraightPiercingProjectile;
        internal float _projectileLifetime = 0.45f;
        internal CompanionEnemyStatusKind _projectileStatusKind;
        internal float _projectileStatusMagnitude;
        internal float _projectileStatusDuration;
        internal float _nextAttackTime;
        internal CombatAbilitySchedule _primaryAbilitySchedule;
        internal CombatAbilitySchedule _secondaryAbilitySchedule;
        internal TargetAreaCastState _targetAreaCastState;
        internal CombatAbilitySchedule _persistentFieldAbilitySchedule;
        internal CombatAbilitySchedule _chainAbilitySchedule;
        internal CombatAbilitySchedule _returningAttackSchedule;
        internal CompanionReturningAttackCombatSetup _returningAttackSetup;
        internal readonly ReturningAttackHitLedger _returningAttackHitLedger = new ReturningAttackHitLedger();
        internal bool _returningPassPending;
        internal float _returningPassDueTime;
        internal Vector3 _returningAttackStart;
        internal Vector3 _returningAttackEnd;
        internal CompanionChainCombatSetup _chainSetup;
        internal SuccessfulActionCounter _ownedProxyCounter;
        internal CompanionOwnedProxyCombatSetup _ownedProxySetup;
        internal readonly List<ChainTargetCandidate> _chainCandidates = new List<ChainTargetCandidate>(32);
        internal readonly List<ChainTargetCandidate> _chainTargets = new List<ChainTargetCandidate>(5);
        internal readonly List<ProjectileBounceTargetCandidate> _projectileBounceCandidates = new List<ProjectileBounceTargetCandidate>(32);
        internal CompanionPersistentFieldCombatSetup _persistentFieldSetup;
        internal CompanionWolfOwnedProxyCombatSetup _wolfSetup;
        internal WolfOwnedProxyState _wolfState;
        internal PersonalDamageMitigationState _personalMitigation;
        internal int _secondaryHealAmount;
        internal float _secondaryHealPeriod;
        internal float _secondaryHealRange;
        internal int _secondaryHealMaxTargets;
        internal float _secondaryHealSecondTargetRatio;
        internal bool _secondaryHealPeriodScalesWithGrowth = true;
        internal bool _healOnPrimaryReturn;
        internal bool _primaryReturnHealPending;
        internal float _primaryReturnHealDueTime;
        internal float _primaryReturnHealDelaySeconds;
        internal readonly List<ClericHealAttack.SupportHealTarget> _supportHealTargets = new List<ClericHealAttack.SupportHealTarget>(2);
        internal float _targetAreaRadius;
        internal int _targetAreaMaxTargets;
        internal float _targetAreaNormalPush;
        internal float _targetAreaEliteBossPush;
        internal CombatTargetRule _targetRule;
        internal CompanionEnemyStatusKind _meleeStatusKind;
        internal float _meleeStatusMagnitude;
        internal float _meleeStatusDuration;
        internal CompanionEnemyStatusKind _targetAreaStatusKind;
        internal float _targetAreaStatusMagnitude;
        internal float _targetAreaStatusDuration;
        internal bool _isDown;
        internal CommanderAllyVisual _visual;
        internal CompanionRuntime _runtime;
        internal PartyService _party;

        public void BindParty(PartyService party)
        {
            _party = party ?? throw new System.ArgumentNullException(nameof(party));
        }

        public AllyAttackStyle AttackStyle => _attackStyle;
        public int Damage => _damage;
        public float AttackPeriod => _period;
        public float AttackRange => _range;
        public float AttackAngle => _angle;
        public int MaxForwardTargetCount => _maxForwardTargetCount;
        public int MaxProjectileTargetCount => _maxProjectileTargetCount;
        public float NoTargetRetrySeconds => _noTargetRetrySeconds;
        public string CombatSourceId => GetSourceId();
        public int SecondaryHealAmount => _secondaryHealAmount;
        public float SecondaryHealRange => _secondaryHealRange;
        public float SecondaryHealPeriod => _secondaryHealPeriod;
        public int SecondaryHealMaxTargets => _secondaryHealMaxTargets;
        public float SecondaryHealSecondTargetRatio => _secondaryHealSecondTargetRatio;
        public float TargetAreaRadius => _targetAreaRadius;
        public int TargetAreaMaxTargets => _targetAreaMaxTargets;
        public float TargetAreaNormalPush => _targetAreaNormalPush;
        public float TargetAreaEliteBossPush => _targetAreaEliteBossPush;
        public CombatTargetRule TargetRule => _targetRule;
        public CompanionEnemyStatusKind MeleeStatusKind => _meleeStatusKind;
        public CompanionEnemyStatusKind TargetAreaStatusKind => _targetAreaStatusKind;
        public float TargetAreaStatusMagnitude => _targetAreaStatusMagnitude;
        public float TargetAreaStatusDuration => _targetAreaStatusDuration;
        public CompanionPersistentFieldCombatSetup PersistentFieldSetup => _persistentFieldSetup;
        public CompanionChainCombatSetup ChainSetup => _chainSetup;
        public bool HasOwnedProxyAssist => _ownedProxyCounter != null;
        public bool HasPromotedProjectileBounce => _promotedProjectileBounce.IsConfigured;
        public CompanionProjectileBounceSetup PromotedProjectileBounce => _promotedProjectileBounce;
        public WolfOwnedProxyPhase WolfPresentationPhase => _wolfState?.Phase ?? WolfOwnedProxyPhase.Inactive;
        public bool HasWolfOwnedProxy => _wolfState != null && _wolfState.IsActive;
        public CompanionWolfOwnedProxyCombatSetup WolfOwnedProxySetup => _wolfSetup;
        public Vector3 WolfPresentationPosition => _wolfState?.PresentationPosition ?? transform.position;
        public Vector3 WolfPresentationDirection => _wolfState?.PresentationDirection ?? transform.right;
        public bool HasPersonalMitigation => _personalMitigation != null;
        public float ProjectileSpeedMultiplier => _projectileSpeedMultiplier;
        public bool UsesStraightPiercingProjectile => _usesStraightPiercingProjectile;

        public bool CanAcceptForwardTarget(int acceptedTargetCount)
        {
            return acceptedTargetCount >= 0 && acceptedTargetCount < _maxForwardTargetCount;
        }

        internal string GetSourceId()
        {
            if (string.IsNullOrEmpty(_sourceIdOverride) == false)
                return _sourceIdOverride;

            CompanionRuntime runtime = GetRuntime();
            return runtime == null ? gameObject.name : runtime.UnitId;
        }

        internal bool IsRuntimeDown()
        {
            CompanionRuntime runtime = GetRuntime();
            return runtime != null && runtime.IsDown;
        }

        internal CompanionRuntime GetRuntime()
        {
            if (_runtime == null)
                _runtime = GetComponent<CompanionRuntime>();

            return _runtime;
        }

    }
}
