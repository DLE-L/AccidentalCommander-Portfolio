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
