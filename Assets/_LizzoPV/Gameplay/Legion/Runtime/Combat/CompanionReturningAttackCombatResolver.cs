using System;
using System.Collections.Generic;
using Lizzo.PV.Data;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    public readonly struct CompanionReturningAttackCombatSetup
    {
        public CompanionReturningAttackCombatSetup(
            string sourceId,
            int damage,
            float period,
            float range,
            float width,
            float travelDuration,
            int maxTargetsPerPass,
            float noTargetRetrySeconds)
        {
            SourceId = sourceId;
            Damage = damage;
            Period = period;
            Range = range;
            Width = width;
            TravelDuration = travelDuration;
            MaxTargetsPerPass = maxTargetsPerPass;
            NoTargetRetrySeconds = noTargetRetrySeconds;
        }

        public string SourceId { get; }
        public int Damage { get; }
        public float Period { get; }
        public float Range { get; }
        public float Width { get; }
        public float TravelDuration { get; }
        public int MaxTargetsPerPass { get; }
        public float NoTargetRetrySeconds { get; }

        public CompanionReturningAttackCombatSetup WithGrowthScale(CompanionGrowthScale scale)
        {
            return new CompanionReturningAttackCombatSetup(
                SourceId,
                Mathf.Max(1, Mathf.RoundToInt(Damage * scale.EffectMultiplier)),
                Mathf.Max(0.01f, Period * scale.IntervalMultiplier),
                Range,
                Width,
                TravelDuration,
                MaxTargetsPerPass,
                NoTargetRetrySeconds);
        }

        public CompanionReturningAttackCombatSetup WithPassiveModifiers(CompanionPassiveCombatModifiers modifiers)
        {
            return new CompanionReturningAttackCombatSetup(
                SourceId,
                Mathf.Max(1, Mathf.RoundToInt(Damage * modifiers.DamageMultiplier)),
                Mathf.Max(0.01f, Period * modifiers.PeriodMultiplier),
                Range * modifiers.RangeMultiplier,
                Width,
                TravelDuration,
                MaxTargetsPerPass,
                NoTargetRetrySeconds);
        }
    }

    public sealed class CompanionReturningAttackCombatResolver
    {
        private const string SkeletonBomberId = "skeleton_bomber";
        private readonly IDataProvider _data;

        public CompanionReturningAttackCombatResolver(IDataProvider data)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
        }

        public bool TryResolve(string baseUnitId, float attackMultiplier, out CompanionReturningAttackCombatSetup setup)
        {
            if (baseUnitId != SkeletonBomberId)
            {
                setup = default;
                return false;
            }

            CompanionCombatProfileData profile = _data.GetCompanionCombatProfile(baseUnitId)
                ?? throw new InvalidOperationException("Canonical returning attack profile is missing: skeleton_bomber");
            CombatEffectData effect = _data.GetCombatEffect(profile.BasicEffectId)
                ?? throw new InvalidOperationException("Canonical returning attack effect is missing: skeleton_bomber");
            if (effect.OwnerUnitId != baseUnitId
                || effect.SkillId != profile.BasicSkillId
                || effect.EffectKind != CombatEffectKind.Damage
                || effect.DeliveryKind != CombatDeliveryKind.ReturningProjectile
                || effect.TargetRule != CombatTargetRule.Targeted
                || effect.StatusKind != CompanionEnemyStatusKind.None
                || effect.BaseValue <= 0.0f
                || effect.CastInterval <= 0.0f
                || effect.Duration <= 0.0f
                || effect.Range <= 0.0f
                || effect.Radius <= 0.0f
                || effect.MaxTargets <= 1
                || effect.MaxTargets > 8
                || profile.NoTargetRetrySeconds <= 0.0f)
            {
                throw new InvalidOperationException("Canonical returning attack data is invalid: skeleton_bomber");
            }

            setup = new CompanionReturningAttackCombatSetup(
                baseUnitId,
                Mathf.Max(1, Mathf.RoundToInt(effect.BaseValue * Mathf.Max(1.0f, attackMultiplier))),
                effect.CastInterval,
                effect.Range,
                effect.Radius,
                effect.Duration,
                effect.MaxTargets,
                profile.NoTargetRetrySeconds);
            return true;
        }
    }

    public static class CompanionReturningAttackTargetSelector
    {
        public static void Collect(
            IReadOnlyList<TargetAreaImpactCandidate> source,
            Vector3 start,
            Vector3 end,
            float width,
            int maxTargets,
            ReturningAttackPass pass,
            List<TargetAreaImpactCandidate> results)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (results == null)
                throw new ArgumentNullException(nameof(results));

            results.Clear();
            Vector3 segment = end - start;
            float segmentLengthSquared = segment.sqrMagnitude;
            float widthSquared = Mathf.Max(0.0f, width);
            widthSquared *= widthSquared;
            int limit = Mathf.Max(0, maxTargets);
            if (segmentLengthSquared <= 0.0001f || limit == 0)
                return;

            for (int index = 0; index < source.Count; index += 1)
            {
                TargetAreaImpactCandidate candidate = source[index];
                float progress = Mathf.Clamp01(Vector3.Dot(candidate.Point - start, segment) / segmentLengthSquared);
                Vector3 closest = start + segment * progress;
                if ((candidate.Point - closest).sqrMagnitude > widthSquared)
                    continue;

                int insertIndex = 0;
                while (insertIndex < results.Count)
                {
                    TargetAreaImpactCandidate existing = results[insertIndex];
                    float existingProgress = Mathf.Clamp01(Vector3.Dot(existing.Point - start, segment) / segmentLengthSquared);
                    bool comesFirst = pass == ReturningAttackPass.Outbound
                        ? progress < existingProgress
                            || (Mathf.Approximately(progress, existingProgress) && candidate.InstanceId < existing.InstanceId)
                        : progress > existingProgress
                            || (Mathf.Approximately(progress, existingProgress) && candidate.InstanceId < existing.InstanceId);
                    if (comesFirst)
                        break;
                    insertIndex += 1;
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
