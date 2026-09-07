using System;

namespace Lizzo.PV.Gameplay.Run
{
    public static partial class TrioSynergyCatalog
    {
        public static TrioSynergyDefinitionSet CreateFirstSet(TrioSynergyBalance balance)
        {
            if (balance == null)
                throw new ArgumentNullException(nameof(balance));
            return new TrioSynergyDefinitionSet(new[]
            {
                Define(TrioSynergyId.GuardCorps, "guard-corps", new[] { LegionIds.ShieldGuard, LegionIds.SwordSoldier, LegionIds.Cleric }, "shield_captain", TrioSynergyTriggerKind.GuardPeriodReady, SynergyTriggerPriority.Periodic, balance.CooldownSeconds, new[]
                {
                    new TrioSynergyEffectStep(TrioSynergyEffectKind.DirectionalShieldWave, balance[0], balance[1], distance: balance[2]),
                    new TrioSynergyEffectStep(TrioSynergyEffectKind.AwaitMovementResolution),
                    new TrioSynergyEffectStep(TrioSynergyEffectKind.LocalSwordHits, balance[3]),
                    new TrioSynergyEffectStep(TrioSynergyEffectKind.HolyReturnHeal, balance[4]),
                }),
                Define(TrioSynergyId.RangedBarrage, "ranged-barrage", new[] { LegionIds.FalconArcher, LegionIds.Bombardier, LegionIds.SkeletonScythe }, "falcon_captain", TrioSynergyTriggerKind.RangedCountersReady, SynergyTriggerPriority.Cumulative, balance.CooldownSeconds, new[]
                {
                    new TrioSynergyEffectStep(TrioSynergyEffectKind.PiercingVolley, balance[5], balance[8]),
                    new TrioSynergyEffectStep(TrioSynergyEffectKind.GiantScytheRoundTrip, balance[6], balance[8]),
                    new TrioSynergyEffectStep(TrioSynergyEffectKind.SequentialBombardment, balance[7], balance[8]),
                }, requiresSeparateCounters: true),
                Define(TrioSynergyId.MagicRite, "magic-rite", new[] { LegionIds.FireMage, LegionIds.LightningMage, LegionIds.Necromancer }, "dark_ritualist", TrioSynergyTriggerKind.MagicCompoundDeath, SynergyTriggerPriority.ConditionReactive, balance.CooldownSeconds, new[]
                {
                    new TrioSynergyEffectStep(TrioSynergyEffectKind.RitualPull, balance[9], balance[10]),
                    new TrioSynergyEffectStep(TrioSynergyEffectKind.AwaitMovementResolution),
                    new TrioSynergyEffectStep(TrioSynergyEffectKind.RitualFireRing, balance[11], balance[10]),
                    new TrioSynergyEffectStep(TrioSynergyEffectKind.RitualLightning, balance[12], balance[10]),
                    new TrioSynergyEffectStep(TrioSynergyEffectKind.RitualExplosion, balance[13], balance[10]),
                }, requiresBaseCurse: true, requiresBaseShock: true, requiresBaseFire: true),
                Define(TrioSynergyId.TrackingParty, "tracking-party", new[] { LegionIds.FalconArcher, LegionIds.FieldHerbalist, LegionIds.WolfTamer }, "battle_apothecary", TrioSynergyTriggerKind.TrackingCountersReady, SynergyTriggerPriority.Cumulative, balance.CooldownSeconds, new[]
                {
                    new TrioSynergyEffectStep(TrioSynergyEffectKind.LurePotion, radius: balance[18]),
                    new TrioSynergyEffectStep(TrioSynergyEffectKind.ApplySynergyVulnerability, balance[14], balance[18], balance[15]),
                    new TrioSynergyEffectStep(TrioSynergyEffectKind.LocalArrowRain, balance[16], balance[18]),
                    new TrioSynergyEffectStep(TrioSynergyEffectKind.WolfAfterimageRoutes, targetCount: 3),
                    new TrioSynergyEffectStep(TrioSynergyEffectKind.PackBiteHighestHealth, balance[17]),
                }, requiresSeparateCounters: true),
            });
        }

        private static TrioSynergyContentDefinition Define(
            TrioSynergyId id,
            string runtimeId,
            string[] requiredLegions,
            string casterUnitId,
            TrioSynergyTriggerKind triggerKind,
            SynergyTriggerPriority priority,
            float cooldownSeconds,
            TrioSynergyEffectStep[] steps,
            bool requiresSeparateCounters = false,
            bool requiresBaseCurse = false,
            bool requiresBaseShock = false,
            bool requiresBaseFire = false,
            bool requiresFirstReady = false)
        {
            return new TrioSynergyContentDefinition(
                id,
                new SynergyDefinition(runtimeId, SynergyTier.Trio, requiredLegions, casterUnitId, SynergyCasterPresentation.Representative, cooldownSeconds, priority),
                triggerKind,
                steps,
                requiresSeparateCounters,
                requiresBaseCurse,
                requiresBaseShock,
                requiresBaseFire,
                requiresFirstReady);
        }
    }
}
