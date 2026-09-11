using Lizzo.PV.Legion.RunCore;
using System;
using System.Collections.Generic;
using Lizzo.PV.Data;
using Lizzo.PV.Legion.Combat;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    public readonly struct CompanionPackAssaultSetup
    {
        public CompanionPackAssaultSetup(string sourceId, float damage, int triggerCount, float range, int wolfHitCount, CombatTargetRule targetRule)
        {
            SourceId = sourceId;
            Damage = Mathf.Max(1, damage);
            TriggerCount = Mathf.Max(1, triggerCount);
            Range = Mathf.Max(0.0f, range);
            WolfHitCount = Mathf.Max(1, wolfHitCount);
            TargetRule = targetRule;
        }

        public string SourceId { get; }
        public float Damage { get; }
        public int TriggerCount { get; }
        public float Range { get; }
        public int WolfHitCount { get; }
        public CombatTargetRule TargetRule { get; }
    }

    public readonly struct CompanionOrbitPatrolSetup
    {
        public CompanionOrbitPatrolSetup(string sourceId, float damage, int triggerCount, float orbitRadius, float pathHalfWidth, int maxTargets, float statusMagnitude, float statusDuration)
        {
            SourceId = sourceId;
            Damage = Mathf.Max(1, damage);
            TriggerCount = Mathf.Max(1, triggerCount);
            OrbitRadius = Mathf.Max(0.0f, orbitRadius);
            PathHalfWidth = Mathf.Max(0.0f, pathHalfWidth);
            MaxTargets = Mathf.Max(1, maxTargets);
            StatusMagnitude = Mathf.Max(0.0f, statusMagnitude);
            StatusDuration = Mathf.Max(0.0f, statusDuration);
        }

        public string SourceId { get; }
        public float Damage { get; }
        public int TriggerCount { get; }
        public float OrbitRadius { get; }
        public float PathHalfWidth { get; }
        public int MaxTargets { get; }
        public float StatusMagnitude { get; }
        public float StatusDuration { get; }
        public CompanionEnemyStatusKind StatusKind => CompanionEnemyStatusKind.Weakening;
    }

    public readonly struct CompanionUndeadRitualSetup
    {
        public CompanionUndeadRitualSetup(string sourceId, int triggerCount, float range, int groupSize, float duration, int maxGroups)
        {
            SourceId = sourceId;
            TriggerCount = Mathf.Max(1, triggerCount);
            Range = Mathf.Max(0.0f, range);
            GroupSize = Mathf.Max(1, groupSize);
            Duration = Mathf.Max(0.0f, duration);
            MaxGroups = Mathf.Max(1, maxGroups);
        }

        public string SourceId { get; }
        public int TriggerCount { get; }
        public float Range { get; }
        public int GroupSize { get; }
        public float Duration { get; }
        public int MaxGroups { get; }
    }

    public readonly struct CompanionReaperOrbitSetup
    {
        public CompanionReaperOrbitSetup(string sourceId, float damage, int triggerCount, float orbitRadius, float pathHalfWidth, int maxTargets)
        {
            SourceId = sourceId;
            Damage = Mathf.Max(1, damage);
            TriggerCount = Mathf.Max(1, triggerCount);
            OrbitRadius = Mathf.Max(0.0f, orbitRadius);
            PathHalfWidth = Mathf.Max(0.0f, pathHalfWidth);
            MaxTargets = Mathf.Max(1, maxTargets);
        }

        public string SourceId { get; }
        public float Damage { get; }
        public int TriggerCount { get; }
        public float OrbitRadius { get; }
        public float PathHalfWidth { get; }
        public int MaxTargets { get; }
    }

    public readonly struct CompanionThirdPromotionCombatSetup
    {
        public CompanionThirdPromotionCombatSetup(CompanionPackAssaultSetup beast, CompanionOrbitPatrolSetup wraith, CompanionUndeadRitualSetup ritual, CompanionReaperOrbitSetup reaper, CompanionPromotionTriggerBinding[] triggers)
        {
            Beast = beast;
            Wraith = wraith;
            Ritual = ritual;
            Reaper = reaper;
            _triggers = (CompanionPromotionTriggerBinding[])triggers.Clone();
        }

        public CompanionPackAssaultSetup Beast { get; }
        public CompanionOrbitPatrolSetup Wraith { get; }
        public CompanionUndeadRitualSetup Ritual { get; }
        public CompanionReaperOrbitSetup Reaper { get; }
        private readonly CompanionPromotionTriggerBinding[] _triggers;
        public CompanionPromotionTriggerBinding[] CreateTriggers() => (CompanionPromotionTriggerBinding[])_triggers.Clone();
    }

    public sealed class CompanionThirdPromotionCombatResolver
    {
        private readonly IDataProvider _data;

        public CompanionThirdPromotionCombatResolver(IDataProvider data)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
        }

        public bool TryResolve(out CompanionThirdPromotionCombatSetup setup)
        {
            CombatEffectData beast = CompanionRuntimeDefinitionInputsResolver.ResolvePromotionEffect(_data, "wolf_tamer");
            CombatEffectData wraith = CompanionRuntimeDefinitionInputsResolver.ResolvePromotionEffect(_data, "wraith_knight");
            CombatEffectData ritual = CompanionRuntimeDefinitionInputsResolver.ResolvePromotionEffect(_data, "necromancer");
            CombatEffectData reaper = CompanionRuntimeDefinitionInputsResolver.ResolvePromotionEffect(_data, "skeleton_scythe_thrower");
            if (beast == null || wraith == null || ritual == null || reaper == null)
            {
                setup = default;
                return false;
            }

            ValidateBeast(beast);
            ValidateWraith(wraith);
            ValidateRitual(ritual);
            ValidateReaper(reaper);
            setup = new CompanionThirdPromotionCombatSetup(
                new CompanionPackAssaultSetup(beast.RuleId, _data.GetCombatEffect(_data.GetCompanionCombatProfile("wolf_tamer").BasicEffectId).BaseValue
                    * _data.GetCompanionPromotion(_data.GetCompanionCombatProfile("wolf_tamer").PromotionProfileId).EffectMultiplier * beast.BaseValue, beast.TriggerCount, beast.Range, beast.MaxActiveCount, beast.TargetRule),
                new CompanionOrbitPatrolSetup(wraith.RuleId, _data.GetCombatEffect(_data.GetCompanionCombatProfile("wraith_knight").BasicEffectId).BaseValue
                    * _data.GetCompanionPromotion(_data.GetCompanionCombatProfile("wraith_knight").PromotionProfileId).EffectMultiplier * wraith.BaseValue, wraith.TriggerCount, wraith.Range, wraith.Radius, wraith.MaxTargets, wraith.StatusMagnitude, wraith.StatusDuration),
                new CompanionUndeadRitualSetup(ritual.RuleId, ritual.TriggerCount, ritual.Range, ritual.MaxTargets, ritual.Duration, ritual.MaxActiveCount),
                new CompanionReaperOrbitSetup(reaper.RuleId, _data.GetCombatEffect(_data.GetCompanionCombatProfile("skeleton_scythe_thrower").BasicEffectId).BaseValue
                    * _data.GetCompanionPromotion(_data.GetCompanionCombatProfile("skeleton_scythe_thrower").PromotionProfileId).EffectMultiplier * reaper.BaseValue, reaper.TriggerCount, reaper.Range, reaper.Radius, reaper.MaxTargets),
                new[] { CompanionPromotionTriggerBinding.FromEffect(beast), CompanionPromotionTriggerBinding.FromEffect(wraith), CompanionPromotionTriggerBinding.FromEffect(ritual), CompanionPromotionTriggerBinding.FromEffect(reaper) });
            return true;
        }



        private static void ValidateBeast(CombatEffectData effect)
        {
            if (effect.OwnerUnitId != "wolf_tamer" || effect.EffectKind != CombatEffectKind.Damage
                || effect.DeliveryKind != CombatDeliveryKind.Proxy || effect.TargetRule != CombatTargetRule.HighestHealth
                || effect.BaseValue <= 0.0f || effect.TriggerCount <= 0 || effect.Range <= 0.0f
                || effect.MaxTargets != 1 || effect.MaxActiveCount != 3)
                throw new InvalidOperationException("Beast Commander promotion data is invalid.");
        }

        private static void ValidateWraith(CombatEffectData effect)
        {
            if (effect.OwnerUnitId != "wraith_knight" || effect.EffectKind != CombatEffectKind.Damage
                || effect.DeliveryKind != CombatDeliveryKind.Circle || effect.TargetRule != CombatTargetRule.Self
                || effect.StatusKind != CompanionEnemyStatusKind.Weakening || effect.StatusMagnitude <= 0.0f
                || effect.Duration <= 0.0f || effect.StatusDuration <= 0.0f || effect.BaseValue <= 0.0f || effect.TriggerCount <= 0
                || effect.Range <= 0.0f || effect.Radius <= 0.0f || effect.MaxTargets <= 0)
                throw new InvalidOperationException("Wraith Guardian promotion data is invalid.");
        }

        private static void ValidateRitual(CombatEffectData effect)
        {
            if (effect.OwnerUnitId != "necromancer" || effect.EffectKind != CombatEffectKind.Damage
                || effect.DeliveryKind != CombatDeliveryKind.Proxy || effect.TargetRule != CombatTargetRule.Self
                || effect.TriggerCount <= 0 || effect.Range <= 0.0f || effect.Duration <= 0.0f
                || effect.MaxTargets <= 1 || effect.MaxActiveCount != 1)
                throw new InvalidOperationException("Dark Ritualist promotion data is invalid.");
        }

        private static void ValidateReaper(CombatEffectData effect)
        {
            if (effect.OwnerUnitId != "skeleton_scythe_thrower" || effect.EffectKind != CombatEffectKind.Damage
                || effect.DeliveryKind != CombatDeliveryKind.Circle || effect.TargetRule != CombatTargetRule.Self
                || effect.StatusKind != CompanionEnemyStatusKind.None || effect.Push > 0.0f
                || effect.BaseValue <= 0.0f || effect.TriggerCount <= 0 || effect.Range <= effect.Radius
                || effect.Radius <= 0.0f || effect.MaxTargets <= 0)
                throw new InvalidOperationException("Skeleton Reaper promotion data is invalid.");
        }
    }


    public static class CompanionOrbitPathTargetSelector
    {
        public static void Collect(IReadOnlyList<CompanionPromotionTargetCandidate> source, Vector3 center, float orbitRadius, float pathHalfWidth, int maxTargets, List<CompanionPromotionTargetCandidate> results)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (results == null) throw new ArgumentNullException(nameof(results));
            results.Clear();
            float radius = Mathf.Max(0.0f, orbitRadius);
            float width = Mathf.Max(0.0f, pathHalfWidth);
            int limit = source.Count;
            for (int index = 0; index < source.Count; index++)
            {
                CompanionPromotionTargetCandidate candidate = source[index];
                Vector3 delta = candidate.Point - center;
                if (Mathf.Abs(delta.magnitude - radius) > width)
                    continue;
                float angle = Mathf.Atan2(delta.y, delta.x);
                if (angle < 0.0f) angle += Mathf.PI * 2.0f;
                int insertion = 0;
                while (insertion < results.Count)
                {
                    Vector3 currentDelta = results[insertion].Point - center;
                    float currentAngle = Mathf.Atan2(currentDelta.y, currentDelta.x);
                    if (currentAngle < 0.0f) currentAngle += Mathf.PI * 2.0f;
                    if (angle < currentAngle || (Mathf.Approximately(angle, currentAngle) && candidate.InstanceId < results[insertion].InstanceId))
                        break;
                    insertion++;
                }
                if (insertion >= limit)
                    continue;
                results.Insert(insertion, candidate);
                if (results.Count > limit)
                    results.RemoveAt(limit);
            }
        }
    }
}
