using System;
using System.Collections.Generic;
using Lizzo.PV.Data;
using Lizzo.PV.Legion.RunCore.Presentation;
using UnityEngine;
using Lizzo.PV.Flow;

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
            CompanionPromotionData promotion,
            float moveSpeed)
        {
            PrimaryEffect = primaryEffect;
            SecondaryEffect = secondaryEffect;
            Promotion = promotion;
            MoveSpeed = moveSpeed;
        }

        internal CombatEffectData PrimaryEffect { get; }
        internal CombatEffectData SecondaryEffect { get; }
        internal CompanionPromotionData Promotion { get; }
        internal float MoveSpeed { get; }
    }

    internal static class CompanionRecordingDefinitionInputsResolver
    {
        internal static CompanionRecordingDefinitionInputs Resolve(
            IDataProvider data,
            string companionId,
            bool isTutorial)
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

            if (isTutorial)
            {
                effect = TutorialCompanionCombatBaseline.ResolvePrimary(effect, companionId);
                if (secondaryEffect != null)
                    secondaryEffect = TutorialCompanionCombatBaseline.ResolveSecondary(secondaryEffect, companionId);
            }

            return new CompanionRecordingDefinitionInputs(
                effect,
                secondaryEffect,
                promotion,
                Mathf.Max(0.0f, profile?.MoveSpeed ?? 0.0f));
        }
    }

    internal static class TutorialCompanionCombatBaseline
    {
        internal static CombatEffectData ResolvePrimary(CombatEffectData source, string companionId)
        {
            CombatEffectData effect = Clone(source);
            switch (companionId)
            {
                case "shield_guard":
                    Set(effect, 10.0f, 2.5f, 1.25f);
                    effect.Push = 1.0f;
                    break;
                case "sword_soldier":
                    Set(effect, 34.0f, 0.7f, 0.75f);
                    break;
                case "cleric":
                    Set(effect, 25.0f, 2.0f, 4.5f);
                    break;
                case "falcon_archer":
                    Set(effect, 25.0f, 1.0f, 5.0f);
                    effect.MaxTargets = 3;
                    break;
                case "bombardier":
                    Set(effect, 60.0f, 2.5f, 4.5f);
                    effect.CastDelay = 0.5f;
                    effect.Radius = 1.25f;
                    break;
                case "skeleton_bomber":
                    Set(effect, 30.0f, 3.0f, 4.5f);
                    effect.Radius = 0.45f;
                    effect.MaxTargets = int.MaxValue;
                    effect.Duration = 1.5f;
                    break;
                case "wolf_tamer":
                    Set(effect, 100.0f, 3.0f, 4.0f);
                    effect.Radius = 1.5f;
                    effect.TriggerCount = 3;
                    break;
            }
            return effect;
        }

        internal static CombatEffectData ResolveSecondary(CombatEffectData source, string companionId)
        {
            CombatEffectData effect = Clone(source);
            if (string.Equals(companionId, "cleric", StringComparison.Ordinal))
            {
                effect.BaseValue = 5.0f;
                effect.CastInterval = 2.0f;
                effect.Range = 4.5f;
            }
            return effect;
        }

        internal static float ResolveAcquisitionRange(string companionId, CombatEffectData effect)
        {
            return companionId switch
            {
                "shield_guard" => 3.0f,
                "sword_soldier" => 3.0f,
                "cleric" => 4.5f,
                "falcon_archer" => 5.0f,
                "bombardier" => 4.5f,
                "skeleton_bomber" => 4.5f,
                "wolf_tamer" => 4.0f,
                _ => Mathf.Max(0.0f, effect.Range),
            };
        }

        private static void Set(CombatEffectData effect, float damage, float interval, float range)
        {
            effect.BaseValue = damage;
            effect.CastInterval = interval;
            effect.Range = range;
        }

        private static CombatEffectData Clone(CombatEffectData source)
        {
            return new CombatEffectData
            {
                Id = source.Id,
                OwnerUnitId = source.OwnerUnitId,
                SkillId = source.SkillId,
                EffectKind = source.EffectKind,
                DeliveryKind = source.DeliveryKind,
                BaseValue = source.BaseValue,
                CastInterval = source.CastInterval,
                TickInterval = source.TickInterval,
                Duration = source.Duration,
                ProjectileLifetime = source.ProjectileLifetime,
                Range = source.Range,
                Radius = source.Radius,
                Angle = source.Angle,
                ChainDistance = source.ChainDistance,
                MaxTargets = source.MaxTargets,
                AffectsAllTargetsInShape = source.AffectsAllTargetsInShape,
                CastDelay = source.CastDelay,
                Push = source.Push,
                TriggerCount = source.TriggerCount,
                MaxActiveCount = source.MaxActiveCount,
                TargetRule = source.TargetRule,
                StatusKind = source.StatusKind,
                StatusMagnitude = source.StatusMagnitude,
                StatusDuration = source.StatusDuration,
                RuleId = source.RuleId,
            };
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
        private const float ShieldTutorialMaxDeparture = 2.0f;

        internal static ActionStep CreateBase(
            CombatEffectData effect,
            AttackDelivery delivery,
            string companionId,
            bool isTutorial,
            float moveSpeed)
        {
            bool isSword = IsSword(companionId);
            bool isWolf = string.Equals(companionId, "wolf_tamer", StringComparison.Ordinal);
            bool isShield = string.Equals(companionId, "shield_guard", StringComparison.Ordinal);
            bool usesTutorialExcursion = isTutorial && (isSword || isWolf || isShield);
            CombatMotion motion = isSword || usesTutorialExcursion
                ? CombatMotion.Excursion
                : CombatMotion.Stationary;
            float excursionSpeed = isSword
                ? (isTutorial ? 1.2f : SwordExcursionSpeed)
                : isShield && isTutorial ? moveSpeed
                : usesTutorialExcursion ? 1.4f : 0.0f;
            float returnSpeed = isSword && isTutorial
                ? 1.5f
                : isWolf && isTutorial ? 1.8f
                : isShield && isTutorial ? moveSpeed : 0.0f;
            return new ActionStep(
                motion,
                delivery,
                effect.Id,
                effect.BaseValue,
                CompanionRecordingPresentationCueResolver.Resolve(effect.Id, delivery, false, companionId),
                isSword ? SwordExcursionActionDuration : 0.0f,
                excursionSpeed,
                Mathf.Max(0.0f, effect.CastDelay),
                isSword ? (isTutorial ? 0.5f : SwordExcursionStandOff) : 0.0f,
                isSword ? SwordExcursionLateral : 0.0f,
                isTutorial
                    ? TutorialCompanionCombatBaseline.ResolveAcquisitionRange(companionId, effect)
                    : ResolveTargetAcquisitionRange(effect),
                returnSpeed,
                isShield && isTutorial ? ShieldTutorialMaxDeparture : 0.0f,
                isShield && isTutorial);
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

        public CompanionRecordingDefinitionCatalog(IDataProvider data, RunContext context = default)
            : this(data, RunDefinitionResolver.Resolve(context, data))
        {
        }

        public CompanionRecordingDefinitionCatalog(IDataProvider data, RunDefinition definition)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            string[] copiedIds = CompanionRecordingLineageIds.CreateCopy();
            _lineageIds = Array.AsReadOnly(copiedIds);
            for (int index = 0; index < copiedIds.Length; index += 1)
            {
                string companionId = copiedIds[index];
                _definitions.Add(companionId, CreateDefinition(
                    data,
                    companionId,
                    definition.UsesBaselineCombatProfile));
            }
        }

        public IReadOnlyList<string> LineageIds => _lineageIds;

        public bool TryGetDefinition(string companionId, out CompanionDefinition definition)
        {
            return _definitions.TryGetValue(companionId ?? string.Empty, out definition);
        }

        private static CompanionDefinition CreateDefinition(IDataProvider data, string companionId, bool isTutorial)
        {
            CompanionRecordingDefinitionInputs inputs =
                CompanionRecordingDefinitionInputsResolver.Resolve(data, companionId, isTutorial);
            CombatEffectData effect = inputs.PrimaryEffect;
            CombatEffectData secondaryEffect = inputs.SecondaryEffect;
            CompanionPromotionData promotion = inputs.Promotion;
            AttackDelivery delivery = CompanionRecordingDeliveryResolver.Resolve(effect.DeliveryKind, companionId);
            ActionStep baseStep = CompanionRecordingActionStepFactory.CreateBase(
                effect,
                delivery,
                companionId,
                isTutorial,
                inputs.MoveSpeed);
            List<ActionStep> baseSteps = new List<ActionStep>(2) { baseStep };
            if (secondaryEffect != null)
            {
                baseSteps.Add(CompanionRecordingActionStepFactory.CreateSecondaryHeal(secondaryEffect, 1.0f));
            }

            ActionSet baseSet = new ActionSet(companionId + "-base", effect.CastInterval, baseSteps);
            float promotedCooldown = isTutorial
                ? effect.CastInterval
                : effect.CastInterval * promotion.IntervalMultiplier;
            ActionStep promotedStep = isTutorial
                ? baseStep
                : CompanionRecordingActionStepFactory.CreatePromoted(
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
                    isTutorial ? 1.0f : promotion.EffectMultiplier));
            }

            ActionSet promotedSet = new ActionSet(companionId + "-promoted", promotedCooldown, promotedSteps);
            return new CompanionDefinition(companionId, baseSet, promotedSet);
        }
    }
}
