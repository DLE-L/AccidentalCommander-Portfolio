using System.Collections.Generic;
using Lizzo.PV.P0.Debugging;
using Lizzo.PV.P0.Visuals;
using UnityEngine;
using Lizzo.PV.P0.Telemetry;
using Lizzo.PV.P0.Combat;
using Lizzo.PV.P0.Units;

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

        internal AllyAttackStyle _attackStyle;
        internal int _damage;
        internal float _period;
        internal float _range;
        internal float _knockback;
        internal float _angle;
        internal float _nextAttackTime;
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
            _attackStyle = attackStyle;
            _damage = damage;
            _period = period;
            _range = Mathf.Max(range, MIN_ATTACK_RANGE);
            _knockback = knockback;
            _angle = angle;
            _nextAttackTime = Time.time + Random.Range(0.1f, 0.35f);
        }

        public void SetDown(bool isDown)
        {
            _isDown = isDown;

            if (isDown)
            {
                _nextAttackTime = float.PositiveInfinity;
                return;
            }

            _nextAttackTime = Time.time + Random.Range(0.15f, 0.35f);
        }

        private void Update()
        {
            if (_isDown || IsRuntimeDown())
                return;

            if (Time.time < _nextAttackTime)
                return;

            bool didAttack = _attackStyle switch
            {
                AllyAttackStyle.SingleTarget => this.AttackNearest(),
                AllyAttackStyle.FarthestTarget => this.AttackFarthest(),
                AllyAttackStyle.ForwardSlash => this.AttackForwardSlash(),
                AllyAttackStyle.ForwardPush => this.AttackForwardPush(),
                AllyAttackStyle.AreaPulse => this.AttackArea(),
                AllyAttackStyle.HealCommander => this.HealCommander(),
                _ => false,
            };

            _nextAttackTime = Time.time + (didAttack ? _period : NO_TARGET_RETRY_DELAY);
        }

        internal string GetSourceId()
        {
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

            float attackHoldSeconds = AttackAnimationTiming.ResolveHoldSeconds(_period);
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
            ApplyDamageToTarget(target, transform.position, _damage, visualKind, spawnHitVisual, GetSourceId());
        }

        internal static void ApplyDamageToTarget(
            MonsterController target,
            Vector3 sourcePosition,
            int damage,
            AttackVisualKind visualKind,
            bool spawnHitVisual,
            string sourceId = null)
        {
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
