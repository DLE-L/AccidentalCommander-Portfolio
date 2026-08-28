using System;
using System.Collections.Generic;
using Lizzo.PV.Data;
using Lizzo.PV.Legion.RunCore.Presentation;
using UnityEngine;

namespace Lizzo.PV.Legion.RunCore
{
    internal static class CompanionRecordingLineageIds
    {
        private static readonly string[] Canonical =
        {
            "shield_guard",
            "sword_soldier",
            "cleric",
            "falcon_archer",
            "bombardier",
            "fire_mage",
            "skeleton_bomber",
            "wolf_tamer",
        };

        internal static string[] CreateCopy()
        {
            return (string[])Canonical.Clone();
        }
    }

    internal readonly struct CompanionRecordingDefinitionInputs
    {
        internal CompanionRecordingDefinitionInputs(
            CombatEffectData primaryEffect,
            CombatEffectData secondaryEffect,
            CompanionPromotionData promotion)
        {
            PrimaryEffect = primaryEffect;
            SecondaryEffect = secondaryEffect;
            Promotion = promotion;
        }

        internal CombatEffectData PrimaryEffect { get; }
        internal CombatEffectData SecondaryEffect { get; }
        internal CompanionPromotionData Promotion { get; }
    }

    internal static class CompanionRecordingDefinitionInputsResolver
    {
        internal static CompanionRecordingDefinitionInputs Resolve(IDataProvider data, string companionId)
        {
            CompanionRosterData roster = data.GetCompanionRoster(companionId)
                ?? throw new InvalidOperationException("Recording companion roster is missing: " + companionId);
            CompanionCombatProfileData profile = data.GetCompanionCombatProfile(companionId);
            string effectId = string.IsNullOrWhiteSpace(profile?.BasicEffectId)
                ? roster.EffectRef
                : profile.BasicEffectId;
            CombatEffectData effect = data.GetCombatEffect(effectId)
                ?? throw new InvalidOperationException("Recording companion effect is missing: " + effectId);
            CombatEffectData secondaryCandidate = string.IsNullOrWhiteSpace(profile?.SecondaryEffectId)
                ? null
                : data.GetCombatEffect(profile.SecondaryEffectId);
            CombatEffectData secondaryEffect = secondaryCandidate != null
                && secondaryCandidate.EffectKind == CombatEffectKind.Heal
                && string.Equals(secondaryCandidate.OwnerUnitId, companionId, StringComparison.Ordinal)
                && secondaryCandidate.BaseValue > 0.0f
                    ? secondaryCandidate
                    : null;
            CompanionPromotionData promotion = data.GetCompanionPromotion(roster.PromotionProfileId)
                ?? throw new InvalidOperationException("Recording companion promotion is missing: " + roster.PromotionProfileId);

            if (!string.Equals(effect.OwnerUnitId, companionId, StringComparison.Ordinal)
                || effect.BaseValue <= 0.0f
                || effect.CastInterval <= 0.0f
                || promotion.RequiredUnitCount != 3
                || promotion.VisualUnitCount != 3
                || promotion.EffectMultiplier <= 0.0f
                || promotion.IntervalMultiplier <= 0.0f)
            {
                throw new InvalidOperationException("Recording companion data is invalid: " + companionId);
            }

            return new CompanionRecordingDefinitionInputs(effect, secondaryEffect, promotion);
        }
    }

    internal static class CompanionRecordingDeliveryResolver
    {
        internal static AttackDelivery Resolve(CombatDeliveryKind delivery, string companionId)
        {
            return delivery switch
            {
                CombatDeliveryKind.Cone => AttackDelivery.Direct,
                CombatDeliveryKind.Projectile => AttackDelivery.Projectile,
                CombatDeliveryKind.Circle => AttackDelivery.Area,
                CombatDeliveryKind.Field => AttackDelivery.SpawnedActor,
                CombatDeliveryKind.ReturningProjectile => AttackDelivery.ReturningProjectile,
                CombatDeliveryKind.Proxy => AttackDelivery.OwnedProxy,
                _ => throw new InvalidOperationException(
                    "Recording companion delivery is unsupported: " + companionId + ":" + delivery),
            };
        }
    }

    internal static class CompanionRecordingPresentationCueResolver
    {
        internal static string Resolve(
            string effectId,
            AttackDelivery delivery,
            bool promoted,
            string companionId)
        {
            if (promoted && string.Equals(companionId, "sword_soldier", StringComparison.Ordinal))
            {
                return CompanionPresentationCueIds.TravelingForward;
            }

            return delivery == AttackDelivery.Area
                ? CompanionPresentationCueIds.TravelingArea
                : effectId;
        }
    }

    internal static class CompanionRecordingActionStepFactory
    {
        private const float CommanderRelativeSlotRangeAllowance = 1.10f;
        private const float SwordExcursionActionDuration = 0.12f;
        private const float SwordExcursionSpeed = 7.5f;
        private const float SwordExcursionStandOff = 1.35f;
        private const float SwordExcursionLateral = 0.30f;

        internal static ActionStep CreateBase(
            CombatEffectData effect,
            AttackDelivery delivery,
            string companionId)
        {
            bool isSword = IsSword(companionId);
            CombatMotion motion = isSword ? CombatMotion.Excursion : CombatMotion.Stationary;
            return new ActionStep(
                motion,
                delivery,
                effect.Id,
                effect.BaseValue,
                CompanionRecordingPresentationCueResolver.Resolve(effect.Id, delivery, false, companionId),
                isSword ? SwordExcursionActionDuration : 0.0f,
                isSword ? SwordExcursionSpeed : 0.0f,
                Mathf.Max(0.0f, effect.CastDelay),
                isSword ? SwordExcursionStandOff : 0.0f,
                isSword ? SwordExcursionLateral : 0.0f,
                ResolveTargetAcquisitionRange(effect));
        }

        internal static ActionStep CreatePromoted(
            CombatEffectData effect,
            AttackDelivery delivery,
            string companionId,
            float magnitudeMultiplier)
        {
            bool isSword = IsSword(companionId);
            return new ActionStep(
                CombatMotion.Stationary,
                delivery,
                effect.Id,
                effect.BaseValue * magnitudeMultiplier,
                CompanionRecordingPresentationCueResolver.Resolve(effect.Id, delivery, true, companionId),
                isSword ? SwordExcursionActionDuration : 0.0f,
                isSword ? SwordExcursionSpeed : 0.0f,
                Mathf.Max(0.0f, effect.CastDelay),
                0.0f,
                0.0f,
                ResolveTargetAcquisitionRange(effect));
        }

        internal static ActionStep CreateSecondaryHeal(CombatEffectData effect, float magnitudeMultiplier)
        {
            return new ActionStep(
                CombatMotion.Stationary,
                CompanionRecordingDeliveryResolver.Resolve(effect.DeliveryKind, effect.OwnerUnitId),
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

        private static bool IsSword(string companionId)
        {
            return string.Equals(companionId, "sword_soldier", StringComparison.Ordinal);
        }
    }

    public sealed class CompanionRecordingDefinitionCatalog : ICompanionDefinitionCatalog
    {
        private readonly Dictionary<string, CompanionDefinition> _definitions =
            new Dictionary<string, CompanionDefinition>(StringComparer.Ordinal);
        private readonly IReadOnlyList<string> _lineageIds;

        public CompanionRecordingDefinitionCatalog(IDataProvider data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            string[] copiedIds = CompanionRecordingLineageIds.CreateCopy();
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
            CompanionRecordingDefinitionInputs inputs =
                CompanionRecordingDefinitionInputsResolver.Resolve(data, companionId);
            CombatEffectData effect = inputs.PrimaryEffect;
            CombatEffectData secondaryEffect = inputs.SecondaryEffect;
            CompanionPromotionData promotion = inputs.Promotion;
            AttackDelivery delivery = CompanionRecordingDeliveryResolver.Resolve(effect.DeliveryKind, companionId);
            ActionStep baseStep = CompanionRecordingActionStepFactory.CreateBase(effect, delivery, companionId);
            List<ActionStep> baseSteps = new List<ActionStep>(2) { baseStep };
            if (secondaryEffect != null)
            {
                baseSteps.Add(CompanionRecordingActionStepFactory.CreateSecondaryHeal(secondaryEffect, 1.0f));
            }

            ActionSet baseSet = new ActionSet(companionId + "-base", effect.CastInterval, baseSteps);
            float promotedCooldown = effect.CastInterval * promotion.IntervalMultiplier;
            ActionStep promotedStep = CompanionRecordingActionStepFactory.CreatePromoted(
                effect,
                delivery,
                companionId,
                promotion.EffectMultiplier);
            List<ActionStep> promotedSteps = new List<ActionStep>(2) { promotedStep };
            if (!string.Equals(companionId, "sword_soldier", StringComparison.Ordinal)
                && secondaryEffect != null)
            {
                promotedSteps.Add(CompanionRecordingActionStepFactory.CreateSecondaryHeal(
                    secondaryEffect,
                    promotion.EffectMultiplier));
            }

            ActionSet promotedSet = new ActionSet(companionId + "-promoted", promotedCooldown, promotedSteps);
            return new CompanionDefinition(companionId, baseSet, promotedSet);
        }
    }
}
