using System;
using System.Collections.Generic;
using Lizzo.PV.Combat;
using Lizzo.PV.Data;
using Lizzo.PV.P0.Combat;
using Lizzo.PV.P0.Units;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    public readonly struct CompanionTargetAreaCombatSetup
    {
        public readonly string SourceId;
        public readonly int Damage;
        public readonly float Period;
        public readonly float Range;
        public readonly float Radius;
        public readonly int MaxTargets;
        public readonly float CastDelay;
        public readonly float NoTargetRetrySeconds;
        public readonly float NormalPush;
        public readonly float EliteBossPush;

        public CompanionTargetAreaCombatSetup(
            string sourceId,
            int damage,
            float period,
            float range,
            float radius,
            int maxTargets,
            float castDelay,
            float noTargetRetrySeconds,
            float normalPush = 0.0f,
            float eliteBossPush = 0.0f)
        {
            SourceId = sourceId;
            Damage = damage;
            Period = period;
            Range = range;
            Radius = radius;
            MaxTargets = maxTargets;
            CastDelay = castDelay;
            NoTargetRetrySeconds = noTargetRetrySeconds;
            NormalPush = Mathf.Max(0.0f, normalPush);
            EliteBossPush = Mathf.Max(0.0f, eliteBossPush);
        }

        public CompanionTargetAreaCombatSetup WithPromotedPowderCaptainImpact()
        {
            if (SourceId != "bombardier")
                throw new InvalidOperationException("Powder Captain promotion requires the bombardier base setup.");

            return new CompanionTargetAreaCombatSetup(
                SourceId,
                Damage,
                Period,
                Range,
                2.0f,
                8,
                CastDelay,
                NoTargetRetrySeconds,
                0.4f,
                0.0f);
        }

        public CompanionTargetAreaCombatSetup WithGrowthScale(CompanionGrowthScale scale)
        {
            return new CompanionTargetAreaCombatSetup(
                SourceId,
                Mathf.Max(1, Mathf.RoundToInt(Damage * Mathf.Max(0.0f, scale.EffectMultiplier))),
                Mathf.Max(0.01f, Period * Mathf.Max(0.0f, scale.IntervalMultiplier)),
                Range,
                Radius,
                MaxTargets,
                CastDelay,
                NoTargetRetrySeconds,
                NormalPush,
                EliteBossPush);
        }

        public CompanionTargetAreaCombatSetup WithPassiveModifiers(CompanionPassiveCombatModifiers modifiers)
        {
            return new CompanionTargetAreaCombatSetup(SourceId, Mathf.Max(1, Mathf.RoundToInt(Damage * modifiers.DamageMultiplier)), Mathf.Max(0.01f, Period * modifiers.PeriodMultiplier), Range * modifiers.RangeMultiplier, Radius, MaxTargets, CastDelay, NoTargetRetrySeconds, NormalPush, EliteBossPush);
        }

        public PromotedTargetAreaFollowUpSetup CreatePromotedBoneArtilleryFollowUp()
        {
            if (SourceId != "skeleton_bomber")
                throw new InvalidOperationException("Bone Artillery follow-up requires the skeleton_bomber base setup.");

            return new PromotedTargetAreaFollowUpSetup(SourceId, 2.0f, 1, 0.60f);
        }
    }

    public readonly struct PromotedTargetAreaFollowUpSetup
    {
        public readonly string SourceId;
        public readonly float Radius;
        public readonly int MaxTargets;
        public readonly float DamageRatio;

        public PromotedTargetAreaFollowUpSetup(string sourceId, float radius, int maxTargets, float damageRatio)
        {
            SourceId = sourceId;
            Radius = Mathf.Max(0.0f, radius);
            MaxTargets = Mathf.Max(1, maxTargets);
            DamageRatio = Mathf.Clamp01(damageRatio);
        }

        public int ResolveDamage(int alreadyScaledDamage)
        {
            return Mathf.Max(1, Mathf.RoundToInt(alreadyScaledDamage * DamageRatio));
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

    public sealed class CompanionTargetAreaCombatResolver
    {
        private const string BombardierId = "bombardier";
        private const string SkeletonBomberId = "skeleton_bomber";

        private readonly IDataProvider _data;

        public CompanionTargetAreaCombatResolver(IDataProvider data)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
        }

        public bool TryResolve(string baseUnitId, float attackMultiplier, out CompanionTargetAreaCombatSetup setup)
        {
            if (IsSupportedBaseUnit(baseUnitId) == false)
            {
                setup = default;
                return false;
            }

            CompanionCombatProfileData profile = _data.GetCompanionCombatProfile(baseUnitId);
            if (profile == null)
                throw new InvalidOperationException($"Canonical target-area profile is missing: {baseUnitId}");

            CombatEffectData effect = _data.GetCombatEffect(profile.BasicEffectId);
            if (effect == null)
                throw new InvalidOperationException($"Canonical target-area effect is missing: {profile.BasicEffectId}");

            if (IsValid(profile, effect) == false)
                throw new InvalidOperationException($"Canonical target-area data is invalid: {baseUnitId}");

            int damage = Mathf.Max(1, Mathf.RoundToInt(effect.BaseValue * Mathf.Max(1.0f, attackMultiplier)));
            setup = new CompanionTargetAreaCombatSetup(
                baseUnitId,
                damage,
                effect.CastInterval,
                effect.Range,
                effect.Radius,
                effect.MaxTargets,
                effect.CastDelay,
                profile.NoTargetRetrySeconds);
            return true;
        }

        private static bool IsSupportedBaseUnit(string baseUnitId)
        {
            return baseUnitId == BombardierId || baseUnitId == SkeletonBomberId;
        }

        private static bool IsValid(CompanionCombatProfileData profile, CombatEffectData effect)
        {
            return effect.OwnerUnitId == profile.UnitId
                && effect.SkillId == profile.BasicSkillId
                && effect.EffectKind == CombatEffectKind.Damage
                && effect.DeliveryKind == CombatDeliveryKind.Circle
                && effect.TargetRule == CombatTargetRule.Targeted
                && effect.BaseValue > 0.0f
                && effect.CastInterval > 0.0f
                && effect.Range > 0.0f
                && effect.Radius > 0.0f
                && effect.MaxTargets > 0
                && effect.CastDelay >= 0.0f
                && profile.NoTargetRetrySeconds > 0.0f;
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
