using System;

namespace Lizzo.PV.Gameplay.Run
{
    public sealed class PairSynergySecondBalance
    {
        public float ThunderPullRadius { get; }
        public float ThunderPullDistance { get; }
        public float ThunderDamage { get; }
        public int ThunderChainLimit { get; }
        public float ConductiveDamage { get; }
        public int ConductiveHitLimit { get; }
        public float HuntingBiteDamage { get; }
        public float HuntingRange { get; }
        public float ArrowRainDamage { get; }
        public float ArrowRainRadius { get; }
        public float ArrowRainDelay { get; }
        public float PrecisionBombDamage { get; }
        public float PrecisionBombRadius { get; }
        public float PrecisionBombDelay { get; }
        public float CoverBombDamage { get; }
        public float CoverBombRadius { get; }
        public float CoverBombDelay { get; }
        public float CooldownSeconds { get; }

        public PairSynergySecondBalance(
            float thunderPullRadius,
            float thunderPullDistance,
            float thunderDamage,
            int thunderChainLimit,
            float conductiveDamage,
            int conductiveHitLimit,
            float huntingBiteDamage,
            float huntingRange,
            float arrowRainDamage,
            float arrowRainRadius,
            float arrowRainDelay,
            float precisionBombDamage,
            float precisionBombRadius,
            float precisionBombDelay,
            float coverBombDamage,
            float coverBombRadius,
            float coverBombDelay,
            float cooldownSeconds)
        {
            ValidatePositive(thunderPullRadius, nameof(thunderPullRadius));
            ValidatePositive(thunderPullDistance, nameof(thunderPullDistance));
            ValidatePositive(thunderDamage, nameof(thunderDamage));
            ValidatePositive(thunderChainLimit, nameof(thunderChainLimit));
            ValidatePositive(conductiveDamage, nameof(conductiveDamage));
            ValidatePositive(conductiveHitLimit, nameof(conductiveHitLimit));
            ValidatePositive(huntingBiteDamage, nameof(huntingBiteDamage));
            ValidatePositive(huntingRange, nameof(huntingRange));
            ValidatePositive(arrowRainDamage, nameof(arrowRainDamage));
            ValidatePositive(arrowRainRadius, nameof(arrowRainRadius));
            ValidatePositive(arrowRainDelay, nameof(arrowRainDelay));
            ValidatePositive(precisionBombDamage, nameof(precisionBombDamage));
            ValidatePositive(precisionBombRadius, nameof(precisionBombRadius));
            ValidatePositive(precisionBombDelay, nameof(precisionBombDelay));
            ValidatePositive(coverBombDamage, nameof(coverBombDamage));
            ValidatePositive(coverBombRadius, nameof(coverBombRadius));
            ValidatePositive(coverBombDelay, nameof(coverBombDelay));
            ValidatePositive(cooldownSeconds, nameof(cooldownSeconds));

            ThunderPullRadius = thunderPullRadius;
            ThunderPullDistance = thunderPullDistance;
            ThunderDamage = thunderDamage;
            ThunderChainLimit = thunderChainLimit;
            ConductiveDamage = conductiveDamage;
            ConductiveHitLimit = conductiveHitLimit;
            HuntingBiteDamage = huntingBiteDamage;
            HuntingRange = huntingRange;
            ArrowRainDamage = arrowRainDamage;
            ArrowRainRadius = arrowRainRadius;
            ArrowRainDelay = arrowRainDelay;
            PrecisionBombDamage = precisionBombDamage;
            PrecisionBombRadius = precisionBombRadius;
            PrecisionBombDelay = precisionBombDelay;
            CoverBombDamage = coverBombDamage;
            CoverBombRadius = coverBombRadius;
            CoverBombDelay = coverBombDelay;
            CooldownSeconds = cooldownSeconds;
        }

        private static void ValidatePositive(float value, string name)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0.0f)
                throw new ArgumentOutOfRangeException(name);
        }

        private static void ValidatePositive(int value, string name)
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(name);
        }
    }

    public static partial class PairSynergyCatalog
    {
        public static PairSynergyDefinitionSet CreateSecondSet(PairSynergySecondBalance balance)
        {
            if (balance == null)
                throw new ArgumentNullException(nameof(balance));
            return new PairSynergyDefinitionSet(new[]
            {
                Define(
                    PairSynergyId.ThunderRite,
                    "thunder-rite",
                    LegionIds.Necromancer,
                    LegionIds.LightningMage,
                    "storm_mage",
                    PairSynergyTriggerKind.CursedAndShockedEnemyKilled,
                    balance.CooldownSeconds,
                    new[]
                    {
                        new PairSynergyEffectStep(PairSynergyEffectKind.PullToCenter, balance.ThunderPullDistance, balance.ThunderPullRadius),
                        new PairSynergyEffectStep(PairSynergyEffectKind.AwaitMovementResolution),
                        new PairSynergyEffectStep(PairSynergyEffectKind.LightningStrike, balance.ThunderDamage),
                        new PairSynergyEffectStep(PairSynergyEffectKind.ChainLightning, balance.ThunderDamage, targetLimit: balance.ThunderChainLimit),
                    },
                    requiresBaseCurse: true,
                    requiresBaseShock: true),
                Define(
                    PairSynergyId.ConductiveHarvest,
                    "conductive-harvest",
                    LegionIds.LightningMage,
                    LegionIds.SkeletonScythe,
                    "skeleton_reaper",
                    PairSynergyTriggerKind.ScytheOutboundHitShocked,
                    balance.CooldownSeconds,
                    new[]
                    {
                        new PairSynergyEffectStep(
                            PairSynergyEffectKind.ElectrifiedReturnTrail,
                            balance.ConductiveDamage,
                            targetLimit: balance.ConductiveHitLimit),
                    },
                    requiresBaseShock: true,
                    requiresFirstActionOccurrence: true),
                Define(
                    PairSynergyId.HuntingHarvest,
                    "hunting-harvest",
                    LegionIds.SkeletonScythe,
                    LegionIds.WolfTamer,
                    "beast_commander",
                    PairSynergyTriggerKind.ScytheFlightReturned,
                    balance.CooldownSeconds,
                    new[]
                    {
                        new PairSynergyEffectStep(
                            PairSynergyEffectKind.WolfAfterimageBite,
                            balance.HuntingBiteDamage,
                            balance.HuntingRange),
                    },
                    requiresFirstActionOccurrence: true,
                    requiresSurvivingTarget: true,
                    minimumBasicHitCount: 2),
                Define(
                    PairSynergyId.TrackingHunt,
                    "tracking-hunt",
                    LegionIds.WolfTamer,
                    LegionIds.FalconArcher,
                    "falcon_captain",
                    PairSynergyTriggerKind.WolfBasicKillSelectedNextTarget,
                    balance.CooldownSeconds,
                    new[]
                    {
                        new PairSynergyEffectStep(PairSynergyEffectKind.DelayWolfChain, delaySeconds: balance.ArrowRainDelay),
                        new PairSynergyEffectStep(PairSynergyEffectKind.LocalArrowRain, balance.ArrowRainDamage, balance.ArrowRainRadius),
                        new PairSynergyEffectStep(PairSynergyEffectKind.ResumeWolfChain),
                    },
                    requiresFirstActionOccurrence: true,
                    requiresSurvivingTarget: true),
                Define(
                    PairSynergyId.TargetBombardment,
                    "target-bombardment",
                    LegionIds.FalconArcher,
                    LegionIds.Bombardier,
                    "powder_captain",
                    PairSynergyTriggerKind.ArcherBasicArrowCompleted,
                    balance.CooldownSeconds,
                    new[]
                    {
                        new PairSynergyEffectStep(PairSynergyEffectKind.AimingWarning, delaySeconds: balance.PrecisionBombDelay),
                        new PairSynergyEffectStep(PairSynergyEffectKind.PrecisionBomb, balance.PrecisionBombDamage, balance.PrecisionBombRadius),
                    },
                    requiresFirstActionOccurrence: true,
                    minimumBasicHitCount: 2),
                Define(
                    PairSynergyId.CoverBombardment,
                    "cover-bombardment",
                    LegionIds.Bombardier,
                    LegionIds.ShieldGuard,
                    "powder_captain",
                    PairSynergyTriggerKind.ShieldBasicReturnStarted,
                    balance.CooldownSeconds,
                    new[]
                    {
                        new PairSynergyEffectStep(
                            PairSynergyEffectKind.WideDelayedBomb,
                            balance.CoverBombDamage,
                            balance.CoverBombRadius,
                            delaySeconds: balance.CoverBombDelay),
                    },
                    requiresFirstActionOccurrence: true),
            });
        }
    }
}
