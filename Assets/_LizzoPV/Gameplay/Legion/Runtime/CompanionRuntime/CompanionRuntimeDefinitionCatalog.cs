using System;
using System.Collections.Generic;
using Lizzo.PV.Data;
using Lizzo.PV.Gameplay.Run;
using Lizzo.PV.Legion.RunCore.Presentation;
using UnityEngine;

namespace Lizzo.PV.Legion.RunCore
{
    internal static class CompanionRuntimeLineageIds
    {
        private static readonly string[] Canonical =
        {
            "shield_guard",
            "sword_soldier",
            "cleric",
            "falcon_archer",
            "field_herbalist",
            "bombardier",
            "fire_mage",
            "lightning_mage",
            "wraith_knight",
            "necromancer",
            "skeleton_scythe_thrower",
            "wolf_tamer",
        };

        internal static string[] CreateCopy()
        {
            return (string[])Canonical.Clone();
        }
    }

    internal readonly struct CompanionRuntimeDefinitionInputs
    {
        internal CompanionRuntimeDefinitionInputs(
            CombatEffectData primaryEffect,
            CompanionPromotionData promotion)
        {
            PrimaryEffect = primaryEffect;
            Promotion = promotion;
        }

        internal CombatEffectData PrimaryEffect { get; }
        internal CompanionPromotionData Promotion { get; }
    }

    internal static class CompanionRuntimeDefinitionInputsResolver
    {
        internal static CombatEffectData ResolvePromotionEffect(IDataProvider data, string companionId)
        {
            CompanionRosterData roster = data.GetCompanionRoster(companionId);
            return roster == null
                || roster.PromotionContractStage != CompanionCombatContractStage.RuntimeConnected
                || string.IsNullOrEmpty(roster.PromotionEffectRef)
                ? null
                : data.GetCombatEffect(roster.PromotionEffectRef);
        }

        internal static CompanionRuntimeDefinitionInputs Resolve(IDataProvider data, string companionId)
        {
            CompanionRosterData roster = data.GetCompanionRoster(companionId)
                ?? throw new InvalidOperationException("Companion runtime roster is missing: " + companionId);
            CompanionCombatProfileData profile = data.GetCompanionCombatProfile(companionId)
                ?? throw new InvalidOperationException("Companion runtime profile is missing: " + companionId);
            string effectId = profile.BasicEffectId;
            CombatEffectData effect = data.GetCombatEffect(effectId)
                ?? throw new InvalidOperationException("Companion runtime effect is missing: " + effectId);
            CompanionPromotionData promotion = data.GetCompanionPromotion(roster.PromotionProfileId)
                ?? throw new InvalidOperationException("Companion runtime promotion is missing: " + roster.PromotionProfileId);

            if (!string.Equals(effect.OwnerUnitId, companionId, StringComparison.Ordinal)
                || effect.BaseValue <= 0.0f
                || effect.CastInterval <= 0.0f
                || promotion.RequiredUnitCount != 3
                || promotion.VisualUnitCount != 3
                || promotion.EffectMultiplier <= 0.0f
                || promotion.IntervalMultiplier <= 0.0f)
            {
                throw new InvalidOperationException("Companion runtime data is invalid: " + companionId);
            }

            return new CompanionRuntimeDefinitionInputs(effect, promotion);
        }
    }

    internal static class CompanionRuntimeDeliveryResolver
    {
        internal static AttackDelivery Resolve(CombatDeliveryKind delivery, string companionId)
        {
            return delivery switch
            {
                CombatDeliveryKind.Cone => AttackDelivery.Direct,
                CombatDeliveryKind.Projectile => AttackDelivery.Projectile,
                CombatDeliveryKind.Circle => AttackDelivery.Area,
                CombatDeliveryKind.Field => AttackDelivery.SpawnedActor,
                CombatDeliveryKind.Chain => AttackDelivery.Chain,
                CombatDeliveryKind.ReturningProjectile => AttackDelivery.ReturningProjectile,
                CombatDeliveryKind.Proxy => AttackDelivery.OwnedProxy,
                _ => throw new InvalidOperationException(
                    "Companion runtime delivery is unsupported: " + companionId + ":" + delivery),
            };
        }
    }

    internal static class CompanionRuntimePresentationCueResolver
    {
        internal static string Resolve(CombatEffectData effect, AttackDelivery delivery, bool promoted)
        {
            string authoredCue = promoted ? effect.PromotedPresentationCueId : effect.BasePresentationCueId;
            if (!string.IsNullOrWhiteSpace(authoredCue))
                return authoredCue;

            return delivery == AttackDelivery.Area
                ? CompanionPresentationCueIds.TravelingArea
                : effect.Id;
        }
    }

    internal static class CompanionRuntimeActionStepFactory
    {
        private const float CommanderRelativeSlotRangeAllowance = 1.10f;
        internal static ActionStep CreateBase(
            CombatEffectData effect,
            AttackDelivery delivery)
        {
            CombatMotion motion = ResolveMotion(effect.BaseMotion);
            return new ActionStep(
                motion,
                delivery,
                effect.Id,
                effect.BaseValue,
                CompanionRuntimePresentationCueResolver.Resolve(effect, delivery, false),
                effect.ActionDurationSeconds,
                effect.MotionSpeed,
                Mathf.Max(0.0f, effect.CastDelay),
                effect.ExcursionStandOffDistance,
                effect.ExcursionLateralOffset,
                ResolveTargetAcquisitionRange(effect), effect.RecoverySeconds);
        }

        internal static ActionStep CreatePromoted(
            CombatEffectData effect,
            AttackDelivery delivery,
            float magnitudeMultiplier)
        {
            return new ActionStep(
                ResolveMotion(effect.PromotedMotion),
                delivery,
                effect.Id,
                effect.BaseValue * magnitudeMultiplier,
                CompanionRuntimePresentationCueResolver.Resolve(effect, delivery, true),
                effect.ActionDurationSeconds,
                effect.MotionSpeed,
                Mathf.Max(0.0f, effect.CastDelay),
                effect.PromotedMotion == CompanionSourceMotionKind.Excursion ? effect.ExcursionStandOffDistance : 0.0f,
                effect.PromotedMotion == CompanionSourceMotionKind.Excursion ? effect.ExcursionLateralOffset : 0.0f,
                ResolveTargetAcquisitionRange(effect), effect.RecoverySeconds);
        }

        internal static ActionStep CreateSecondaryHeal(CombatEffectData effect, float magnitudeMultiplier)
        {
            return new ActionStep(
                CombatMotion.Stationary,
                CompanionRuntimeDeliveryResolver.Resolve(effect.DeliveryKind, effect.OwnerUnitId),
                effect.Id,
                effect.BaseValue * magnitudeMultiplier,
                effect.Id,
                0.0f,
                0.0f,
                Mathf.Max(0.0f, effect.CastDelay));
        }

        private static float ResolveTargetAcquisitionRange(CombatEffectData effect)
        {
            return Mathf.Max(0.0f, effect.Range) + CommanderRelativeSlotRangeAllowance;
        }

        private static CombatMotion ResolveMotion(CompanionSourceMotionKind motion)
        {
            return motion == CompanionSourceMotionKind.Excursion
                ? CombatMotion.Excursion
                : CombatMotion.Stationary;
        }
    }

    public sealed class CompanionRuntimeDefinitionCatalog : ICompanionDefinitionCatalog
    {
        private readonly Dictionary<string, CompanionDefinition> _definitions =
            new Dictionary<string, CompanionDefinition>(StringComparer.Ordinal);
        private readonly IReadOnlyList<string> _lineageIds;

        public CompanionRuntimeDefinitionCatalog(IDataProvider data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            string[] copiedIds = CompanionRuntimeLineageIds.CreateCopy();
            _lineageIds = Array.AsReadOnly(copiedIds);
            for (int index = 0; index < copiedIds.Length; index += 1)
            {
                string companionId = copiedIds[index];
                _definitions.Add(companionId, CreateDefinition(data, companionId));
            }
        }

        public IReadOnlyList<string> LineageIds => _lineageIds;

        public bool TryGetDefinition(string companionId, out CompanionDefinition definition)
        {
            return _definitions.TryGetValue(companionId ?? string.Empty, out definition);
        }

        private static CompanionDefinition CreateDefinition(IDataProvider data, string companionId)
        {
            CompanionRuntimeDefinitionInputs inputs =
                CompanionRuntimeDefinitionInputsResolver.Resolve(data, companionId);
            CombatEffectData effect = inputs.PrimaryEffect;
            CompanionPromotionData promotion = inputs.Promotion;
            CompanionCombatProfileData profile = data.GetCompanionCombatProfile(companionId);
            ActionSet baseSet = new ActionSet(companionId + "-base", effect.CastInterval,
                ResolveActionSteps(data, companionId, profile.BaseActionEffectIds, false, 1.0f));
            float promotedCooldown = effect.CastInterval * promotion.IntervalMultiplier;
            ActionSet promotedSet = new ActionSet(companionId + "-promoted", promotedCooldown,
                ResolveActionSteps(data, companionId, profile.PromotedActionEffectIds, true, promotion.EffectMultiplier));
            return new CompanionDefinition(companionId, baseSet, promotedSet);
        }

        private static IReadOnlyList<ActionStep> ResolveActionSteps(IDataProvider data, string companionId,
            string authoredIds, bool promoted, float multiplier)
        {
            if (string.IsNullOrWhiteSpace(authoredIds))
                throw new InvalidOperationException("Companion action list is missing: " + companionId + (promoted ? ":promoted" : ":base"));
            string[] ids = authoredIds.Split(',');
            var steps = new ActionStep[ids.Length];
            for (int i = 0; i < ids.Length; i++)
            {
                string id = ids[i].Trim();
                CombatEffectData effect = string.IsNullOrEmpty(id) ? null : data.GetCombatEffect(id);
                if (effect == null || !string.Equals(effect.OwnerUnitId, companionId, StringComparison.Ordinal)
                    || effect.BaseValue <= 0.0f
                    || (effect.EffectKind != CombatEffectKind.Damage && effect.EffectKind != CombatEffectKind.DamageOverTime
                        && effect.EffectKind != CombatEffectKind.Heal))
                    throw new InvalidOperationException("Invalid companion action-list effect: " + companionId + ":" + id);
                AttackDelivery delivery = CompanionRuntimeDeliveryResolver.Resolve(effect.DeliveryKind, companionId);
                steps[i] = effect.EffectKind == CombatEffectKind.Heal
                    ? CompanionRuntimeActionStepFactory.CreateSecondaryHeal(effect, multiplier)
                    : promoted ? CompanionRuntimeActionStepFactory.CreatePromoted(effect, delivery, multiplier)
                    : CompanionRuntimeActionStepFactory.CreateBase(effect, delivery);
            }
            return steps;
        }
    }
}
