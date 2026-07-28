using System.Collections.Generic;
using Lizzo.PV.P0.Debugging;
using Lizzo.PV.P0.Visuals;
using UnityEngine;
using Lizzo.PV.Flow;
using Lizzo.PV.P0.Telemetry;
using Lizzo.PV.P0.Combat;
using Lizzo.PV.P0.Units;
using Lizzo.PV.Combat;
using Lizzo.PV.Combat.Fields;
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

    public sealed class AllyCombat : MonoBehaviour
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

        internal AllyAttackStyle _attackStyle;
        internal PromotedMultiHitSequence _promotedMultiHitSequence;
        internal PromotedProjectileBurst _promotedProjectileBurst;
        internal CompanionProjectileBounceSetup _promotedProjectileBounce;
        internal bool _returnToPreferredSlotRequested;
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
        internal float _nextAttackTime;
        internal CombatAbilitySchedule _primaryAbilitySchedule;
        internal CombatAbilitySchedule _secondaryAbilitySchedule;
        internal TargetAreaCastState _targetAreaCastState;
        internal CombatAbilitySchedule _persistentFieldAbilitySchedule;
        internal CombatAbilitySchedule _chainAbilitySchedule;
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
        internal readonly List<ClericHealAttack.SupportHealTarget> _supportHealTargets = new List<ClericHealAttack.SupportHealTarget>(2);
        internal float _targetAreaRadius;
        internal int _targetAreaMaxTargets;
        internal float _targetAreaNormalPush;
        internal float _targetAreaEliteBossPush;
        internal PromotedTargetAreaFollowUpSetup _promotedTargetAreaFollowUp;
        internal bool _hasPromotedTargetAreaFollowUp;
        internal bool _isDown;
        internal CommanderAllyVisual _visual;
        internal CompanionRuntime _runtime;
        internal PartyService _party;

        public void BindParty(PartyService party)
        {
            _party = party ?? throw new System.ArgumentNullException(nameof(party));
        }

        public void SetInfo(AllyAttackStyle attackStyle, int damage, float period, float range, float knockback, float angle = 60.0f)
        {
            ClearCanonicalAbilitySchedules();
            _attackStyle = attackStyle;
            _damage = damage;
            _period = period;
            _range = Mathf.Max(range, MIN_ATTACK_RANGE);
            _knockback = knockback;
            _angle = angle;
            _maxForwardTargetCount = int.MaxValue;
            _maxProjectileTargetCount = 1;
            _noTargetRetrySeconds = NO_TARGET_RETRY_DELAY;
            _sourceIdOverride = null;
            _projectileSpeedMultiplier = 1.0f;
            _nextAttackTime = Time.time + Random.Range(0.1f, 0.35f);
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
        public bool HasPromotedTargetAreaFollowUp => _hasPromotedTargetAreaFollowUp;
        public CompanionPersistentFieldCombatSetup PersistentFieldSetup => _persistentFieldSetup;
        public CompanionChainCombatSetup ChainSetup => _chainSetup;
        public bool HasOwnedProxyAssist => _ownedProxyCounter != null;
        public int OwnedProxyTriggerCount => _ownedProxyCounter?.TriggerCount ?? 0;
        public bool HasPromotedProjectileBurst => _promotedProjectileBurst != null;
        public PromotedProjectileBurst PromotedProjectileBurst => _promotedProjectileBurst;
        public bool HasPromotedProjectileBounce => _promotedProjectileBounce.IsConfigured;
        public CompanionProjectileBounceSetup PromotedProjectileBounce => _promotedProjectileBounce;
        public bool ReturnToPreferredSlotRequested => _returnToPreferredSlotRequested;
        public WolfOwnedProxyPhase WolfPresentationPhase => _wolfState?.Phase ?? WolfOwnedProxyPhase.Inactive;
        public bool HasWolfOwnedProxy => _wolfState != null && _wolfState.IsActive;
        public CompanionWolfOwnedProxyCombatSetup WolfOwnedProxySetup => _wolfSetup;
        public Vector3 WolfPresentationPosition => _wolfState?.PresentationPosition ?? transform.position;
        public Vector3 WolfPresentationDirection => _wolfState?.PresentationDirection ?? transform.right;
        public bool HasPersonalMitigation => _personalMitigation != null;
        public float PersonalIncomingDamageMultiplier => _personalMitigation?.IncomingDamageMultiplier ?? 1.0f;
        public float ProjectileSpeedMultiplier => _projectileSpeedMultiplier;

        public void SetCanonicalWolfOwnedProxyInfo(CompanionWolfOwnedProxyCombatSetup setup)
        {
            ClearCanonicalAbilitySchedules(); _wolfSetup = setup; _wolfState = new WolfOwnedProxyState();
            _attackStyle = AllyAttackStyle.SingleTarget; _damage = setup.Damage; _period = setup.Period; _range = setup.SearchRange; _sourceIdOverride = setup.SourceId;
            _noTargetRetrySeconds = setup.NoTargetRetrySeconds; _nextAttackTime = Time.time;
        }

        public void SetCanonicalWraithMeleeDefenseInfo(CompanionWraithMeleeDefenseSetup setup)
        {
            SetCanonicalMeleeInfo(setup.Melee); _personalMitigation = new PersonalDamageMitigationState(); _personalMitigation.Configure(setup.PersonalDefense, Time.time);
        }

        public void SetPromotedMultiHitSequence(PromotedMultiHitSequence sequence)
        {
            _promotedMultiHitSequence = sequence;
            _returnToPreferredSlotRequested = false;
        }

        public void SetPromotedProjectileBurst(PromotedProjectileBurst burst)
        {
            _promotedProjectileBurst = burst ?? throw new System.ArgumentNullException(nameof(burst));
        }

        public void SetPromotedProjectileBounce(CompanionProjectileBounceSetup bounce)
        {
            if (bounce.IsConfigured == false || _sourceIdOverride != bounce.SourceId)
                throw new System.InvalidOperationException("Projectile bounce requires the active canonical projectile source.");

            _promotedProjectileBounce = bounce;
        }

        public void ConfigureOwnedProxyTriggerCount(int triggerCount)
        {
            if (_ownedProxyCounter == null)
                throw new System.InvalidOperationException("Owned proxy assist is not configured.");

            _ownedProxySetup = _ownedProxySetup.WithTriggerCount(triggerCount);
            _ownedProxyCounter.Configure(triggerCount);
        }

        public void ApplyGrowthScale(CompanionGrowthScale scale)
        {
            if (_chainAbilitySchedule != null)
            {
                _chainSetup = _chainSetup.WithGrowthScale(scale);
                _damage = _chainSetup.Damage;
                _period = _chainSetup.Period;
                _chainAbilitySchedule.ApplyIntervalMultiplier(scale.IntervalMultiplier);
                return;
            }

            if (_persistentFieldAbilitySchedule != null)
            {
                _persistentFieldSetup = _persistentFieldSetup.WithGrowthScale(scale);
                _damage = _persistentFieldSetup.Damage;
                _period = _persistentFieldSetup.Period;
                _persistentFieldAbilitySchedule.ApplyIntervalMultiplier(scale.IntervalMultiplier);
                return;
            }

            _damage = Mathf.Max(1, Mathf.RoundToInt(_damage * scale.EffectMultiplier));
            _period = Mathf.Max(0.01f, _period * scale.IntervalMultiplier);
            _secondaryHealAmount = _secondaryHealAmount > 0 ? Mathf.Max(1, Mathf.RoundToInt(_secondaryHealAmount * scale.EffectMultiplier)) : 0;
            if (_secondaryHealPeriodScalesWithGrowth)
            {
                _secondaryHealPeriod = Mathf.Max(0.01f, _secondaryHealPeriod * scale.IntervalMultiplier);
                _secondaryAbilitySchedule?.ApplyIntervalMultiplier(scale.IntervalMultiplier);
            }
        }

        public void SetCanonicalMeleeInfo(CompanionMeleeCombatSetup setup)
        {
            ClearCanonicalAbilitySchedules();
            _attackStyle = setup.AttackStyle;
            _damage = setup.Damage;
            _period = setup.Period;
            _range = Mathf.Max(setup.Range, MIN_ATTACK_RANGE);
            _knockback = setup.Knockback;
            _angle = setup.Angle;
            _maxForwardTargetCount = Mathf.Max(1, setup.MaxTargets);
            _maxProjectileTargetCount = 1;
            _noTargetRetrySeconds = Mathf.Max(0.0f, setup.NoTargetRetrySeconds);
            _sourceIdOverride = null;
            _projectileSpeedMultiplier = 1.0f;
            _nextAttackTime = Time.time + Random.Range(0.1f, 0.35f);
        }

        public void SetCanonicalProjectileInfo(CompanionProjectileCombatSetup setup)
        {
            ClearCanonicalAbilitySchedules();
            _attackStyle = setup.AttackStyle;
            _damage = setup.Damage;
            _period = setup.Period;
            _range = Mathf.Max(setup.Range, MIN_ATTACK_RANGE);
            _knockback = 0.0f;
            _angle = 0.0f;
            _maxForwardTargetCount = int.MaxValue;
            _maxProjectileTargetCount = Mathf.Max(1, setup.MaxTargets);
            _noTargetRetrySeconds = Mathf.Max(0.0f, setup.NoTargetRetrySeconds);
            _sourceIdOverride = setup.SourceId;
            _projectileSpeedMultiplier = setup.ProjectileSpeedMultiplier;
            _nextAttackTime = Time.time + Random.Range(0.1f, 0.35f);
        }
        public void SetCanonicalProjectileWithProxyInfo(CompanionProjectileCombatSetup setup, CompanionOwnedProxyCombatSetup proxy)
        {
            SetCanonicalProjectileInfo(setup);
            _ownedProxySetup = proxy;
            _ownedProxyCounter = new SuccessfulActionCounter();
            _ownedProxyCounter.Configure(proxy.TriggerCount);
        }

        internal bool TryRecordOwnedProxyBasicCast()
        {
            return _ownedProxyCounter != null && _ownedProxyCounter.RecordSuccess();
        }

        public void SetCanonicalRangedSupportInfo(CompanionRangedSupportCombatSetup setup)
        {
            _promotedProjectileBounce = default;
            _attackStyle = setup.Primary.AttackStyle;
            _damage = setup.Primary.Damage;
            _period = setup.Primary.Period;
            _range = Mathf.Max(setup.Primary.Range, MIN_ATTACK_RANGE);
            _knockback = 0.0f;
            _angle = 0.0f;
            _maxForwardTargetCount = int.MaxValue;
            _maxProjectileTargetCount = Mathf.Max(1, setup.Primary.MaxTargets);
            _noTargetRetrySeconds = Mathf.Max(0.0f, setup.Primary.NoTargetRetrySeconds);
            _sourceIdOverride = setup.Primary.SourceId;
            _projectileSpeedMultiplier = setup.Primary.ProjectileSpeedMultiplier;
            _secondaryHealAmount = setup.SecondaryHealAmount;
            _secondaryHealPeriod = setup.SecondaryPeriod;
            _secondaryHealRange = Mathf.Max(setup.SecondaryRange, MIN_ATTACK_RANGE);
            _secondaryHealMaxTargets = Mathf.Max(1, setup.SecondaryMaxTargets);
            _secondaryHealSecondTargetRatio = Mathf.Clamp01(setup.SecondarySecondTargetRatio);
            _secondaryHealPeriodScalesWithGrowth = setup.SecondaryPeriodScalesWithGrowth;
            float now = Time.time;
            _primaryAbilitySchedule = new CombatAbilitySchedule();
            _secondaryAbilitySchedule = new CombatAbilitySchedule();
            _primaryAbilitySchedule.Configure(_period, _noTargetRetrySeconds, now, Random.Range(0.1f, 0.35f));
            _secondaryAbilitySchedule.Configure(setup.SecondaryPeriod, setup.SecondaryNoTargetRetrySeconds, now, Random.Range(0.1f, 0.35f));
            _nextAttackTime = float.PositiveInfinity;
        }

        public void SetCanonicalTargetAreaInfo(CompanionTargetAreaCombatSetup setup)
        {
            ClearCanonicalAbilitySchedules();
            _attackStyle = AllyAttackStyle.TargetedArea;
            _damage = setup.Damage;
            _period = setup.Period;
            _range = Mathf.Max(setup.Range, MIN_ATTACK_RANGE);
            _knockback = 0.0f;
            _angle = 0.0f;
            _maxForwardTargetCount = int.MaxValue;
            _maxProjectileTargetCount = 1;
            _noTargetRetrySeconds = Mathf.Max(0.0f, setup.NoTargetRetrySeconds);
            _sourceIdOverride = setup.SourceId;
            _projectileSpeedMultiplier = 1.0f;
            _targetAreaRadius = Mathf.Max(setup.Radius, MIN_ATTACK_RANGE);
            _targetAreaMaxTargets = Mathf.Max(1, setup.MaxTargets);
            _targetAreaNormalPush = setup.NormalPush;
            _targetAreaEliteBossPush = setup.EliteBossPush;
            _hasPromotedTargetAreaFollowUp = false;
            _targetAreaCastState = new TargetAreaCastState();
            _targetAreaCastState.Configure(setup, Time.time, Random.Range(0.1f, 0.35f));
            _nextAttackTime = float.PositiveInfinity;
        }

        public void SetPromotedTargetAreaFollowUp(PromotedTargetAreaFollowUpSetup setup)
        {
            if (_sourceIdOverride != setup.SourceId || setup.SourceId != "skeleton_bomber")
                throw new System.InvalidOperationException("Bone Artillery follow-up requires the active skeleton_bomber target-area setup.");

            _promotedTargetAreaFollowUp = setup;
            _hasPromotedTargetAreaFollowUp = true;
        }

        public void SetCanonicalPersistentFieldInfo(CompanionPersistentFieldCombatSetup setup)
        {
            ClearCanonicalAbilitySchedules();
            _attackStyle = AllyAttackStyle.TargetedField;
            _damage = setup.Damage;
            _period = setup.Period;
            _range = Mathf.Max(setup.Range, MIN_ATTACK_RANGE);
            _knockback = 0.0f;
            _angle = 0.0f;
            _maxForwardTargetCount = int.MaxValue;
            _maxProjectileTargetCount = 1;
            _noTargetRetrySeconds = Mathf.Max(0.0f, setup.NoTargetRetrySeconds);
            _sourceIdOverride = setup.SourceId;
            _projectileSpeedMultiplier = 1.0f;
            _persistentFieldSetup = setup;
            _persistentFieldAbilitySchedule = new CombatAbilitySchedule();
            _persistentFieldAbilitySchedule.Configure(_period, _noTargetRetrySeconds, Time.time, Random.Range(0.1f, 0.35f));
            _nextAttackTime = float.PositiveInfinity;
        }

        public void SetCanonicalChainInfo(CompanionChainCombatSetup setup)
        {
            ClearCanonicalAbilitySchedules();
            _attackStyle = AllyAttackStyle.TargetedChain; _damage = setup.Damage; _period = setup.Period;
            _range = Mathf.Max(setup.InitialRange, MIN_ATTACK_RANGE); _knockback = 0; _angle = 0;
            _maxForwardTargetCount = int.MaxValue; _maxProjectileTargetCount = 1;
            _noTargetRetrySeconds = setup.NoTargetRetrySeconds; _sourceIdOverride = setup.SourceId; _chainSetup = setup;
            _projectileSpeedMultiplier = 1.0f;
            _chainAbilitySchedule = new CombatAbilitySchedule();
            _chainAbilitySchedule.Configure(setup.Period, setup.NoTargetRetrySeconds, Time.time, Random.Range(0.1f, 0.35f));
            _nextAttackTime = float.PositiveInfinity;
        }

        public bool CanAcceptForwardTarget(int acceptedTargetCount)
        {
            return acceptedTargetCount >= 0 && acceptedTargetCount < _maxForwardTargetCount;
        }

        public float ResolveNextAttackDelay(bool didAttack)
        {
            return didAttack ? _period / ResolveAttackIntervalDivisor() : _noTargetRetrySeconds;
        }

        internal float ResolveAttackIntervalDivisor()
        {
            float divisor = _party == null ? 1.0f : _party.ResolveCompanionAttackIntervalDivisor(GetRuntime());
            return divisor > 0.0f ? divisor : 1.0f;
        }

        public void SetDown(bool isDown)
        {
            _isDown = isDown;

            if (isDown)
            {
                _ownedProxyCounter?.Reset();
                _wolfState?.Reset();
                _personalMitigation?.ResetForOwnerDown(Time.time);
                if (_runtime != null) _runtime.IncomingDamageMultiplier = 1.0f;
                _nextAttackTime = float.PositiveInfinity;
                return;
            }

            if (_targetAreaCastState != null)
            {
                _targetAreaCastState.Restart(Time.time, Random.Range(0.15f, 0.35f));
                return;
            }

            if (_persistentFieldAbilitySchedule != null)
            {
                _persistentFieldAbilitySchedule.Restart(Time.time, Random.Range(0.15f, 0.35f));
                return;
            }

            if (_chainAbilitySchedule != null) { _chainAbilitySchedule.Restart(Time.time, Random.Range(0.15f, 0.35f)); return; }

            if (_primaryAbilitySchedule != null)
            {
                float restartDelay = Random.Range(0.15f, 0.35f);
                _primaryAbilitySchedule.Restart(Time.time, restartDelay);
                _secondaryAbilitySchedule.Restart(Time.time, restartDelay);
                return;
            }

            _nextAttackTime = Time.time + Random.Range(0.15f, 0.35f);
        }

        private void Update()
        {
            AdvanceCanonicalCombat(Time.time);
        }

#if UNITY_EDITOR
        public void TryAdvanceCanonicalCastForTests(float currentTime)
#else
        internal void TryAdvanceCanonicalCastForTests(float currentTime)
#endif
        {
            AdvanceCanonicalCombat(currentTime);
        }

        void AdvanceCanonicalCombat(float currentTime)
        {
            if (RunPauseController.IsResultGameplayLocked)
                return;

            if (_isDown || IsRuntimeDown())
                return;

            if (_personalMitigation != null)
            {
                _personalMitigation.Advance(currentTime);
                GetRuntime().IncomingDamageMultiplier = _personalMitigation.IncomingDamageMultiplier;
            }

            if (_wolfState != null)
            {
                this.UpdateCanonicalWolfOwnedProxy(currentTime);
                return;
            }

            if (_targetAreaCastState != null)
            {
                this.UpdateCanonicalTargetArea(currentTime);
                return;
            }

            if (_persistentFieldAbilitySchedule != null)
            {
                if (_persistentFieldAbilitySchedule.IsDue(currentTime))
                {
                    bool resolved = this.SpawnCanonicalPersistentField(currentTime);
                    _persistentFieldAbilitySchedule.RecordResolution(currentTime, resolved, resolved ? ResolveAttackIntervalDivisor() : 1.0f);
                    if (resolved) _party.ReportCanonicalCast(GetRuntime(), CanonicalCompanionActionKind.BasicAttack);
                }
                return;
            }

            if (_chainAbilitySchedule != null) { if (_chainAbilitySchedule.IsDue(currentTime)) { bool resolved=this.AttackCanonicalChain(); _chainAbilitySchedule.RecordResolution(currentTime,resolved,resolved ? ResolveAttackIntervalDivisor() : 1.0f); if(resolved)_party.ReportCanonicalCast(GetRuntime(), CanonicalCompanionActionKind.BasicAttack); } return; }

            if (_primaryAbilitySchedule != null)
            {
                if (_primaryAbilitySchedule.IsDue(currentTime))
                {
                    bool resolved=this.AttackTargetedProjectile(); _primaryAbilitySchedule.RecordResolution(currentTime,resolved,resolved ? ResolveAttackIntervalDivisor() : 1.0f); if(resolved)_party.ReportCanonicalCast(GetRuntime(), CanonicalCompanionActionKind.BasicAttack);
                }

                if (_secondaryAbilitySchedule.IsDue(currentTime))
                {
                    bool resolved=this.AttackCanonicalRangedSupportHeal(); _secondaryAbilitySchedule.RecordResolution(currentTime,resolved,resolved ? ResolveAttackIntervalDivisor() : 1.0f); if(resolved)_party.ReportCanonicalCast(GetRuntime(), CanonicalCompanionActionKind.ActiveSkill);
                }

                return;
            }

            if (currentTime < _nextAttackTime)
                return;

            bool didAttack = _attackStyle switch
            {
                AllyAttackStyle.SingleTarget => this.AttackNearest(),
                AllyAttackStyle.FarthestTarget => this.AttackFarthest(),
                AllyAttackStyle.TargetedProjectile => this.AttackTargetedProjectile(),
                AllyAttackStyle.ForwardSlash => this.AttackForwardSlash(),
                AllyAttackStyle.ForwardPush => this.AttackForwardPush(),
                AllyAttackStyle.AreaPulse => this.AttackArea(),
                AllyAttackStyle.HealCommander => this.HealCommander(),
                _ => false,
            };

            _nextAttackTime = currentTime + ResolveNextAttackDelay(didAttack);
            if (didAttack) _party.ReportCanonicalCast(GetRuntime(), CanonicalCompanionActionKind.BasicAttack);
        }

        private void ClearCanonicalAbilitySchedules()
        {
            _primaryAbilitySchedule = null;
            _secondaryAbilitySchedule = null;
            _targetAreaCastState = null;
            _persistentFieldAbilitySchedule = null;
            _chainAbilitySchedule = null;
            _persistentFieldSetup = default;
            _chainSetup = default;
            _ownedProxyCounter = null;
            _ownedProxySetup = default;
            _secondaryHealAmount = 0;
            _secondaryHealPeriod = 0.0f;
            _secondaryHealRange = 0.0f;
            _secondaryHealMaxTargets = 0;
            _secondaryHealSecondTargetRatio = 0.0f;
            _secondaryHealPeriodScalesWithGrowth = true;
            _supportHealTargets.Clear();
            _targetAreaRadius = 0.0f;
            _targetAreaMaxTargets = 0;
            _targetAreaNormalPush = 0.0f;
            _targetAreaEliteBossPush = 0.0f;
            _promotedMultiHitSequence = null;
            _promotedProjectileBurst = null;
            _promotedProjectileBounce = default;
            _returnToPreferredSlotRequested = false;
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

        internal void FaceTarget(MonsterController target)
        {
            if (target == null)
                return;

            FaceDirection(this.GetFacingDeltaToTarget(target));
        }

        internal void FaceDirection(Vector3 direction)
        {
            if (direction.sqrMagnitude <= 0.0001f)
                return;

            float attackHoldSeconds = AttackAnimationTiming.ResolveHoldSeconds(ResolveNextAttackDelay(true));
            if (_visual == null)
                _visual = GetComponent<CommanderAllyVisual>();

            if (_visual == null)
            {
                Debug.LogError($"Companion prefab is missing required CommanderAllyVisual: {gameObject.name}", this);
                return;
            }

            _visual.PlayAttack(direction, attackHoldSeconds);
        }


        internal void DamageTarget(MonsterController target, AttackVisualKind visualKind)
        {
            DamageTarget(target, visualKind, spawnHitVisual: true);
        }

        internal void DamageTarget(MonsterController target, AttackVisualKind visualKind, bool spawnHitVisual)
        {
            TryDamageTarget(target, _damage, visualKind, spawnHitVisual);
        }

        internal bool TryDamageTarget(MonsterController target, int damage, AttackVisualKind visualKind, bool spawnHitVisual)
        {
            ICombatImmediateHitModule module = _party?.ImmediateHitModule;
            if (module == null)
            {
                Debug.LogError("[AllyCombat] Required CombatImmediateHitModule runtime wiring is missing.", this);
                return false;
            }

            string sourceId = GetSourceId();
            Vector3 sourcePosition = transform.position;
            Vector3 feedbackPosition = AllyTargeting.ResolveTargetPoint(target, sourcePosition);
            CombatImmediateHitRequest request = CombatImmediateHitRequest.CreateAllyDirectTarget(
                sourceId,
                target,
                sourcePosition,
                feedbackPosition,
                damage,
                visualKind,
                spawnHitVisual);
            return module.TryApply(request);
        }

        internal static void ApplyDamageToTarget(
            MonsterController target,
            Vector3 sourcePosition,
            int damage,
            AttackVisualKind visualKind,
            bool spawnHitVisual,
            string sourceId = null)
        {
            if (RunPauseController.IsResultGameplayLocked)
                return;

            if (target == null || target.IsValid() == false || damage <= 0)
                return;

            Vector3 hitPosition = AllyTargeting.ResolveTargetPoint(target, sourcePosition);
            P0BossDpsTracker.RecordBossDamage(sourceId, target, damage);
            target.OnDamagedFromPosition(sourcePosition, damage, CombatIds.Normalize(sourceId));
            if (spawnHitVisual)
                AttackVisual.Spawn(hitPosition, visualKind);

            if (target.IsValid() == false)
                return;

            HitFlash flash = target.HitFlash;
            if (flash == null)
            {
                Debug.LogError($"Enemy prefab is missing required HitFlash: {target.gameObject.name}", target);
                return;
            }
            flash.Play();

            EnemyRuntimeStats stats = target.RuntimeStats;
            if (stats?.Data == null || stats.Data.Type == "boss")
            {
                EnemyHealthBar.RemoveFrom(target.transform);
            }
            else
            {
                EnemyHealthBar healthBar = target.HealthBar;
                if (healthBar == null)
                {
                    Debug.LogError($"Enemy prefab is missing required EnemyHealthBar: {target.gameObject.name}", target);
                    return;
                }

                bool alwaysVisible = stats.Data.Id == CombatIds.ShieldOrc || stats.Data.Id == CombatIds.EliteRedCharger;
                healthBar.Refresh(target, alwaysVisible, EnemyHealthBar.HIT_REVEAL_SECONDS);
            }
        }
    }
}
