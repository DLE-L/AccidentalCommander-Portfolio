using System;
using System.Collections.Generic;
using Lizzo.PV.Legion.Combat;
using Lizzo.PV.P0.Combat;
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
        internal MonsterController FindNearestTargetAreaCastTarget()
        {
            MonsterController nearest = null;
            float nearestSqrDistance = _range * _range;
            int nearestId = int.MaxValue;

            foreach (MonsterController monster in _party.Registry.Enemies)
            {
                if (monster.IsValid() == false || monster.gameObject == gameObject)
                    continue;

                float sqrDistance = this.GetSqrDistanceToTarget(monster);
                int instanceId = monster.GetInstanceID();
                if (sqrDistance > nearestSqrDistance
                    || (Mathf.Approximately(sqrDistance, nearestSqrDistance) && instanceId >= nearestId))
                {
                    continue;
                }

                nearest = monster;
                nearestSqrDistance = sqrDistance;
                nearestId = instanceId;
            }

            return nearest;
        }

        internal List<TargetAreaImpactCandidate> CollectTargetAreaImpactTargets(Vector3 impactPoint)
        {
            List<TargetAreaImpactCandidate> candidates = _targetAreaCandidates;
            candidates.Clear();
            foreach (MonsterController monster in _party.Registry.Enemies)
            {
                if (monster.IsValid() == false || monster.gameObject == gameObject)
                    continue;

                candidates.Add(new TargetAreaImpactCandidate(
                    monster,
                    AllyTargeting.ResolveTargetPoint(monster, impactPoint),
                    monster.GetInstanceID()));
            }

            TargetAreaImpactCollector.Collect(
                candidates,
                impactPoint,
                TargetAreaRadius,
                TargetAreaMaxTargets,
                _targetAreaImpactTargets);
            return _targetAreaImpactTargets;
        }

        internal bool TryApplyTargetAreaPush(TargetAreaPushRequest request)
        {
            if (request.IsRequested == false
                || request.TargetClass != TargetAreaImpactTargetClass.Normal
                || request.Target.IsValid() == false)
                return false;

            int targetId = request.Target.GetInstanceID();
            if (NextKnockbackAllowedTimeByTarget.TryGetValue(targetId, out float nextAllowedTime)
                && Time.time < nextAllowedTime)
                return false;

            NextKnockbackAllowedTimeByTarget[targetId] = Time.time + KNOCKBACK_INTERNAL_COOLDOWN;
            request.Target.ApplySmoothKnockback(request.Direction, request.Distance, KNOCKBACK_SLIDE_DURATION);
            return true;
        }

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

    public static class PromotedTargetAreaFollowUpSelector
    {
        public static bool TrySelect(
            List<TargetAreaImpactCandidate> source,
            Vector3 impactPoint,
            int excludedPrimaryTargetInstanceId,
            float radius,
            out TargetAreaImpactCandidate target)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            float sqrRadius = Mathf.Max(0.0f, radius);
            sqrRadius *= sqrRadius;
            float bestDistance = float.PositiveInfinity;
            int bestInstanceId = int.MaxValue;
            int bestIndex = -1;
            for (int i = 0; i < source.Count; i++)
            {
                TargetAreaImpactCandidate candidate = source[i];
                if (candidate.InstanceId == excludedPrimaryTargetInstanceId)
                    continue;

                float distance = (candidate.Point - impactPoint).sqrMagnitude;
                if (distance > sqrRadius
                    || (Mathf.Approximately(distance, bestDistance) && candidate.InstanceId >= bestInstanceId)
                    || distance > bestDistance)
                {
                    continue;
                }

                bestDistance = distance;
                bestInstanceId = candidate.InstanceId;
                bestIndex = i;
            }

            if (bestIndex < 0)
            {
                target = default;
                return false;
            }

            target = source[bestIndex];
            return true;
        }
    }

    public enum TargetAreaImpactTargetClass
    {
        Normal,
        Elite,
        Boss,
    }

    public readonly struct TargetAreaImpactCandidate
    {
        public readonly MonsterController Target;
        public readonly Vector3 Point;
        public readonly int InstanceId;
        public readonly TargetAreaImpactTargetClass TargetClass;

        public TargetAreaImpactCandidate(MonsterController target, Vector3 point, int instanceId)
            : this(target, point, instanceId, TargetAreaImpactTargetClassifier.Resolve(target))
        {
        }

        public TargetAreaImpactCandidate(
            MonsterController target,
            Vector3 point,
            int instanceId,
            TargetAreaImpactTargetClass targetClass)
        {
            Target = target;
            Point = point;
            InstanceId = instanceId;
            TargetClass = targetClass;
        }
    }

    public static class TargetAreaImpactTargetClassifier
    {
        public static TargetAreaImpactTargetClass Resolve(MonsterController target)
        {
            if (target == null)
                return TargetAreaImpactTargetClass.Normal;

            if (target.IsBoss || target.EnemyType == "boss")
                return TargetAreaImpactTargetClass.Boss;

            return target.EnemyId == CombatIds.EliteRedCharger
                ? TargetAreaImpactTargetClass.Elite
                : TargetAreaImpactTargetClass.Normal;
        }
    }

    public readonly struct TargetAreaPushRequest
    {
        public readonly MonsterController Target;
        public readonly TargetAreaImpactTargetClass TargetClass;
        public readonly Vector3 Direction;
        public readonly float Distance;

        private TargetAreaPushRequest(
            MonsterController target,
            TargetAreaImpactTargetClass targetClass,
            Vector3 direction,
            float distance)
        {
            Target = target;
            TargetClass = targetClass;
            Direction = direction;
            Distance = Mathf.Max(0.0f, distance);
        }

        public bool IsRequested => Target != null && Distance > 0.0f && Direction.sqrMagnitude > 0.0001f;

        public static TargetAreaPushRequest Create(
            CompanionTargetAreaCombatSetup setup,
            TargetAreaImpactCandidate candidate,
            Vector3 impactPoint)
        {
            return Create(setup.NormalPush, setup.EliteBossPush, candidate, impactPoint);
        }

        public static TargetAreaPushRequest Create(
            float normalPush,
            float eliteBossPush,
            TargetAreaImpactCandidate candidate,
            Vector3 impactPoint)
        {
            float distance = candidate.TargetClass == TargetAreaImpactTargetClass.Normal
                ? normalPush
                : eliteBossPush;
            Vector3 direction = candidate.Point - impactPoint;
            if (direction.sqrMagnitude <= 0.0001f && candidate.Target != null)
                direction = candidate.Target.transform.position - impactPoint;

            return new TargetAreaPushRequest(candidate.Target, candidate.TargetClass, direction, distance);
        }
    }

    public static class TargetAreaImpactCollector
    {
        public static void Collect(
            List<TargetAreaImpactCandidate> source,
            Vector3 impactPoint,
            float radius,
            int maxTargets,
            List<TargetAreaImpactCandidate> results)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (results == null)
                throw new ArgumentNullException(nameof(results));

            results.Clear();
            float sqrRadius = Mathf.Max(0.0f, radius) * Mathf.Max(0.0f, radius);
            int limit = Mathf.Max(0, maxTargets);
            if (limit == 0)
                return;

            for (int i = 0; i < source.Count; i++)
            {
                TargetAreaImpactCandidate candidate = source[i];
                float candidateDistance = (candidate.Point - impactPoint).sqrMagnitude;
                if (candidateDistance > sqrRadius)
                    continue;

                int insertIndex = 0;
                while (insertIndex < results.Count)
                {
                    TargetAreaImpactCandidate existing = results[insertIndex];
                    float existingDistance = (existing.Point - impactPoint).sqrMagnitude;
                    if (candidateDistance < existingDistance
                        || (Mathf.Approximately(candidateDistance, existingDistance)
                            && candidate.InstanceId < existing.InstanceId))
                    {
                        break;
                    }

                    insertIndex++;
                }

                if (insertIndex >= limit)
                    continue;

                results.Insert(insertIndex, candidate);
                if (results.Count > limit)
                    results.RemoveAt(limit);
            }
        }
    }
}
