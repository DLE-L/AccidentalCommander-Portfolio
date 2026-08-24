using System;
using System.Collections.Generic;
using Lizzo.PV.Legion.Combat;
using Lizzo.PV.P0.Telemetry;
using Lizzo.PV.P0.Units;
using Lizzo.PV.P0.Visuals;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    public sealed class CombatAbilitySchedule
    {
        private float _period;
        private float _retrySeconds;
        private float _nextDueTime;

        public float NextDueTime => _nextDueTime;

        public void Configure(float period, float retrySeconds, float currentTime, float initialDelay)
        {
            _period = Mathf.Max(0.0f, period);
            _retrySeconds = Mathf.Max(0.0f, retrySeconds);
            _nextDueTime = currentTime + Mathf.Max(0.0f, initialDelay);
        }

        public bool IsDue(float currentTime)
        {
            return currentTime >= _nextDueTime;
        }

        public void RecordResolution(float currentTime, bool resolved)
        {
            RecordResolution(currentTime, resolved, 1.0f);
        }

        public void RecordResolution(float currentTime, bool resolved, float attackIntervalDivisor)
        {
            float divisor = attackIntervalDivisor > 0.0f ? attackIntervalDivisor : 1.0f;
            _nextDueTime = currentTime + (resolved ? _period / divisor : _retrySeconds);
        }

        public void Restart(float currentTime, float initialDelay)
        {
            _nextDueTime = currentTime + Mathf.Max(0.0f, initialDelay);
        }

        public void ApplyIntervalMultiplier(float multiplier)
        {
            _period = Mathf.Max(0.01f, _period * Mathf.Max(0.0f, multiplier));
        }
    }

    public sealed class TargetAreaCastState
    {
        private float _period;
        private float _retrySeconds;
        private float _castDelay;
        private float _nextTargetDueTime;
        private float _impactDueTime;
        private Vector3 _lockedImpactPoint;
        private int _primaryTargetInstanceId;
        private bool _hasPendingImpact;

        public float NextTargetDueTime => _nextTargetDueTime;
        public int PrimaryTargetInstanceId => _primaryTargetInstanceId;

        public void Configure(CompanionTargetAreaCombatSetup setup, float currentTime, float initialDelay)
        {
            _period = Mathf.Max(0.0f, setup.Period);
            _retrySeconds = Mathf.Max(0.0f, setup.NoTargetRetrySeconds);
            _castDelay = Mathf.Max(0.0f, setup.CastDelay);
            _nextTargetDueTime = currentTime + Mathf.Max(0.0f, initialDelay);
            _impactDueTime = 0.0f;
            _lockedImpactPoint = Vector3.zero;
            _primaryTargetInstanceId = 0;
            _hasPendingImpact = false;
        }

        public bool IsReadyForTarget(float currentTime)
        {
            return _hasPendingImpact == false && currentTime >= _nextTargetDueTime;
        }

        public void RecordNoTarget(float currentTime)
        {
            if (_hasPendingImpact == false)
                _nextTargetDueTime = currentTime + _retrySeconds;
        }

        public bool TryBeginCast(float currentTime, Vector3 impactPoint)
        {
            return TryBeginCast(currentTime, impactPoint, 0);
        }

        public bool TryBeginCast(float currentTime, Vector3 impactPoint, int primaryTargetInstanceId)
        {
            if (IsReadyForTarget(currentTime) == false)
                return false;

            _lockedImpactPoint = impactPoint;
            _primaryTargetInstanceId = primaryTargetInstanceId;
            _impactDueTime = currentTime + _castDelay;
            _hasPendingImpact = true;
            return true;
        }

        public bool TryConsumeImpact(float currentTime, out Vector3 impactPoint)
        {
            return TryConsumeImpact(currentTime, 1.0f, out impactPoint);
        }

        public bool TryConsumeImpact(float currentTime, float attackIntervalDivisor, out Vector3 impactPoint)
        {
            impactPoint = Vector3.zero;
            if (_hasPendingImpact == false || currentTime < _impactDueTime)
                return false;

            impactPoint = _lockedImpactPoint;
            _hasPendingImpact = false;
            float divisor = attackIntervalDivisor > 0.0f ? attackIntervalDivisor : 1.0f;
            _nextTargetDueTime = currentTime + _period / divisor;
            return true;
        }

        public void Restart(float currentTime, float initialDelay)
        {
            _hasPendingImpact = false;
            _nextTargetDueTime = currentTime + Mathf.Max(0.0f, initialDelay);
        }
    }

    public sealed partial class AllyCombat
    {
        internal bool UpdateCanonicalTargetArea(float currentTime)
        {
            TargetAreaCastState state = _targetAreaCastState;
            if (state == null)
                return false;

            if (state.TryConsumeImpact(currentTime, ResolveAttackIntervalDivisor(), out Vector3 impactPoint))
            {
                ResolveCanonicalTargetAreaImpact(impactPoint, state.PrimaryTargetInstanceId);
                return true;
            }

            if (state.IsReadyForTarget(currentTime) == false)
                return false;

            MonsterController castTarget = this.FindNearestTargetAreaCastTarget();
            if (castTarget == null)
            {
                state.RecordNoTarget(currentTime);
                return false;
            }

            this.FaceTarget(castTarget);
            Vector3 lockedImpactPoint = AllyTargeting.ResolveTargetPoint(castTarget, transform.position);
            if (state.TryBeginCast(currentTime, lockedImpactPoint, castTarget.GetInstanceID()) == false)
                return false;

            _party.ReportCanonicalCast(GetRuntime(), CanonicalCompanionActionKind.BasicAttack);

            if (state.TryConsumeImpact(currentTime, ResolveAttackIntervalDivisor(), out impactPoint))
                ResolveCanonicalTargetAreaImpact(impactPoint, state.PrimaryTargetInstanceId);

            return true;
        }

        private void ResolveCanonicalTargetAreaImpact(Vector3 impactPoint, int primaryTargetInstanceId)
        {
            List<TargetAreaImpactCandidate> targets = this.CollectTargetAreaImpactTargets(impactPoint);
            if (targets.Count == 0)
                return;

            P0BossDpsTracker.RecordAttackCast(GetSourceId(), targets[0].Target);
            this.SpawnCanonicalCompanionAttack(impactPoint, impactPoint - transform.position);
            for (int i = 0; i < targets.Count; i++)
            {
                MonsterController target = targets[i].Target;
                if (target == null || target.IsValid() == false)
                    continue;

                this.TryDamageTarget(target, _damage, AttackVisualKind.AreaHit, false, ResolveFuseLinkEffectId(GetSourceId()));
                TargetAreaPushRequest pushRequest = TargetAreaPushRequest.Create(
                    TargetAreaNormalPush,
                    TargetAreaEliteBossPush,
                    targets[i],
                    impactPoint);
                this.TryApplyTargetAreaPush(pushRequest);
            }

            if (HasPromotedTargetAreaFollowUp == false || _isDown)
                return;

            if (PromotedTargetAreaFollowUpSelector.TrySelect(
                    _targetAreaCandidates,
                    impactPoint,
                    primaryTargetInstanceId,
                    _promotedTargetAreaFollowUp.Radius,
                    out TargetAreaImpactCandidate followUp) == false)
            {
                return;
            }

            MonsterController followUpTarget = followUp.Target;
            if (followUpTarget == null || followUpTarget.IsValid() == false)
                return;

            int followUpDamage = _promotedTargetAreaFollowUp.ResolveDamage(_damage);
            this.TryDamageTarget(followUpTarget, followUpDamage, AttackVisualKind.SingleHit, spawnHitVisual: false);
        }

        private static string ResolveFuseLinkEffectId(string sourceId)
        {
            return sourceId == "bombardier" ? "dmg_bomb_explosion_v1"
                : sourceId == "skeleton_bomber" ? "dmg_skeleton_bomb_v1"
                : null;
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
            _targetAreaCastState.Configure(setup, Time.time, UnityEngine.Random.Range(0.1f, 0.35f));
            _nextAttackTime = float.PositiveInfinity;
        }

        public void SetPromotedTargetAreaFollowUp(PromotedTargetAreaFollowUpSetup setup)
        {
            if (_sourceIdOverride != setup.SourceId || setup.SourceId != "skeleton_bomber")
            {
                throw new InvalidOperationException(
                    "Bone Artillery follow-up requires the active skeleton_bomber target-area setup.");
            }

            _promotedTargetAreaFollowUp = setup;
            _hasPromotedTargetAreaFollowUp = true;
        }
    }
}
