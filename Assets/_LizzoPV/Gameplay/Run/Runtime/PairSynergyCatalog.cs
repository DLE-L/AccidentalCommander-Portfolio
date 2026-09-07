using System;

namespace Lizzo.PV.Gameplay.Run
{
    public static partial class PairSynergyCatalog
    {
        public static PairSynergyDefinitionSet CreateFirstSet(PairSynergyBalance balance)
        {
            if (balance == null)
                throw new ArgumentNullException(nameof(balance));
            return new PairSynergyDefinitionSet(new[]
            {
                Define(
                    PairSynergyId.ShieldBreakthrough,
                    "shield-breakthrough",
                    LegionIds.ShieldGuard,
                    LegionIds.SwordSoldier,
                    "sword_captain",
                    PairSynergyTriggerKind.ShieldBasicHit,
                    balance.CooldownSeconds,
                    new PairSynergyEffectStep(PairSynergyEffectKind.CrossSlash, balance.CrossSlashDamage, balance.CrossSlashRadius)),
                Define(
                    PairSynergyId.VulnerableCut,
                    "vulnerable-cut",
                    LegionIds.SwordSoldier,
                    LegionIds.FieldHerbalist,
                    "sword_captain",
                    PairSynergyTriggerKind.SwordBasicAreaHit,
                    balance.CooldownSeconds,
                    new[] { new PairSynergyEffectStep(PairSynergyEffectKind.CrossSlash, balance.VulnerableCutDamage, balance.VulnerableCutRadius, delaySeconds: balance.VulnerableCutDelay) },
                    requiresVulnerable: true),
                Define(
                    PairSynergyId.WeakeningBrew,
                    "weakening-brew",
                    LegionIds.FieldHerbalist,
                    LegionIds.WraithKnight,
                    "wraith_guardian",
                    PairSynergyTriggerKind.WraithBasicHit,
                    balance.CooldownSeconds,
                    new[]
                    {
                        new PairSynergyEffectStep(PairSynergyEffectKind.AlchemyMist, radius: balance.MistRadius, targetLimit: balance.MistTargetLimit),
                        new PairSynergyEffectStep(PairSynergyEffectKind.ApplyWeaken, balance.WeakenMagnitude, durationSeconds: balance.WeakenDuration, targetLimit: balance.MistTargetLimit),
                    },
                    requiresVulnerable: true),
                Define(
                    PairSynergyId.SoulGuard,
                    "soul-guard",
                    LegionIds.WraithKnight,
                    LegionIds.Cleric,
                    "light_guide",
                    PairSynergyTriggerKind.CommanderDamagedByWeakenedEnemy,
                    balance.CooldownSeconds,
                    new[]
                    {
                        new PairSynergyEffectStep(PairSynergyEffectKind.ConsumeBaseWeaken),
                        new PairSynergyEffectStep(PairSynergyEffectKind.SoulReturnHeal, balance.SoulHealing),
                    },
                    requiresBaseWeaken: true,
                    requiresAppliedCommanderDamage: true),
                Define(
                    PairSynergyId.CleansingFlame,
                    "cleansing-flame",
                    LegionIds.Cleric,
                    LegionIds.FireMage,
                    "light_guide",
                    PairSynergyTriggerKind.ClericBasicProjectileHit,
                    balance.CooldownSeconds,
                    new[] { new PairSynergyEffectStep(PairSynergyEffectKind.CleansingReturnTrail, balance.CleansingDamage, balance.CleansingWidth) },
                    requiresBaseFireField: true),
                Define(
                    PairSynergyId.CremationRite,
                    "cremation-rite",
                    LegionIds.FireMage,
                    LegionIds.Necromancer,
                    "dark_ritualist",
                    PairSynergyTriggerKind.CursedEnemyKilledInBasicFireField,
                    balance.CooldownSeconds,
                    new[]
                    {
                        new PairSynergyEffectStep(PairSynergyEffectKind.PullToCenter, balance.CremationPullDistance, balance.CremationPullRadius),
                        new PairSynergyEffectStep(PairSynergyEffectKind.AwaitMovementResolution),
                        new PairSynergyEffectStep(PairSynergyEffectKind.FireBurst, balance.CremationDamage, balance.CremationRadius),
                    },
                    requiresBaseCurse: true,
                    requiresBaseFireField: true),
            });
        }

        private static PairSynergyContentDefinition Define(
            PairSynergyId id,
            string runtimeId,
            string firstLegionId,
            string secondLegionId,
            string casterUnitId,
            PairSynergyTriggerKind triggerKind,
            float cooldownSeconds,
            PairSynergyEffectStep step)
        {
            return Define(
                id,
                runtimeId,
                firstLegionId,
                secondLegionId,
                casterUnitId,
                triggerKind,
                cooldownSeconds,
                new[] { step });
        }

        private static PairSynergyContentDefinition Define(
            PairSynergyId id,
            string runtimeId,
            string firstLegionId,
            string secondLegionId,
            string casterUnitId,
            PairSynergyTriggerKind triggerKind,
            float cooldownSeconds,
            PairSynergyEffectStep[] steps,
            bool requiresVulnerable = false,
            bool requiresBaseWeaken = false,
            bool requiresAppliedCommanderDamage = false,
            bool requiresBaseCurse = false,
            bool requiresBaseFireField = false,
            bool requiresBaseShock = false,
            bool requiresFirstActionOccurrence = false,
            bool requiresSurvivingTarget = false,
            int minimumBasicHitCount = 0)
        {
            return new PairSynergyContentDefinition(
                id,
                new SynergyDefinition(
                    runtimeId,
                    SynergyTier.Pair,
                    new[] { firstLegionId, secondLegionId },
                    casterUnitId,
                    SynergyCasterPresentation.Afterimage,
                    cooldownSeconds),
                triggerKind,
                steps,
                requiresVulnerable,
                requiresBaseWeaken,
                requiresAppliedCommanderDamage,
                requiresBaseCurse,
                requiresBaseFireField,
                requiresBaseShock,
                requiresFirstActionOccurrence,
                requiresSurvivingTarget,
                minimumBasicHitCount);
        }
    }
}
