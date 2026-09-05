using System;

namespace Lizzo.PV.Gameplay.Run
{
    public sealed class TrioSynergySecondBalance
    {
        private readonly float[] _values;
        public float this[int index] => _values[index];
        public float CooldownSeconds { get; }

        public TrioSynergySecondBalance(
            float undeadMarchDamage,
            float undeadWeaken,
            float undeadWeakenDuration,
            float undeadScytheDamage,
            float alchemyVulnerability,
            float alchemyVulnerabilityDuration,
            float alchemyBombDamage,
            float alchemyExplosionDamage,
            float alchemyFieldDamage,
            float alchemyRadius,
            float assaultShieldDamage,
            float assaultSwordDamage,
            float assaultBiteDamage,
            float assaultPathWidth,
            float sanctuaryBindDuration,
            float sanctuaryDamage,
            float sanctuaryRadius,
            float cooldownSeconds)
        {
            _values = new[]
            {
                undeadMarchDamage, undeadWeaken, undeadWeakenDuration, undeadScytheDamage,
                alchemyVulnerability, alchemyVulnerabilityDuration, alchemyBombDamage,
                alchemyExplosionDamage, alchemyFieldDamage, alchemyRadius,
                assaultShieldDamage, assaultSwordDamage, assaultBiteDamage, assaultPathWidth,
                sanctuaryBindDuration, sanctuaryDamage, sanctuaryRadius,
            };
            for (int index = 0; index < _values.Length; index++)
            {
                if (float.IsNaN(_values[index]) || float.IsInfinity(_values[index]) || _values[index] <= 0.0f)
                    throw new ArgumentOutOfRangeException(nameof(undeadMarchDamage));
            }
            if (float.IsNaN(cooldownSeconds) || float.IsInfinity(cooldownSeconds) || cooldownSeconds <= 0.0f)
                throw new ArgumentOutOfRangeException(nameof(cooldownSeconds));
            CooldownSeconds = cooldownSeconds;
        }
    }

    public static partial class TrioSynergyCatalog
    {
        public static TrioSynergyDefinitionSet CreateSecondSet(TrioSynergySecondBalance balance)
        {
            if (balance == null)
                throw new ArgumentNullException(nameof(balance));
            return new TrioSynergyDefinitionSet(new[]
            {
                Define(TrioSynergyId.UndeadMarch, "undead-march", new[] { LegionIds.WraithKnight, LegionIds.Necromancer, LegionIds.SkeletonScythe }, "wraith_guardian", TrioSynergyTriggerKind.UndeadCountersReady, SynergyTriggerPriority.Cumulative, balance.CooldownSeconds, new[]
                {
                    new TrioSynergyEffectStep(TrioSynergyEffectKind.TombPath),
                    new TrioSynergyEffectStep(TrioSynergyEffectKind.WraithMarchDamageWeaken, balance[0], durationSeconds: balance[2], statusMagnitude: balance[1]),
                    new TrioSynergyEffectStep(TrioSynergyEffectKind.GiantScytheReturn, balance[3]),
                }, requiresSeparateCounters: true),
                Define(TrioSynergyId.AlchemyBombardment, "alchemy-bombardment", new[] { LegionIds.FieldHerbalist, LegionIds.Bombardier, LegionIds.FireMage }, "battle_apothecary", TrioSynergyTriggerKind.AlchemyBombHit, SynergyTriggerPriority.ConditionReactive, balance.CooldownSeconds, new[]
                {
                    new TrioSynergyEffectStep(TrioSynergyEffectKind.VolatilePotion),
                    new TrioSynergyEffectStep(TrioSynergyEffectKind.ApplySynergyVulnerability, balance[4], balance[9], balance[5]),
                    new TrioSynergyEffectStep(TrioSynergyEffectKind.OuterBombRing, balance[6], balance[9]),
                    new TrioSynergyEffectStep(TrioSynergyEffectKind.InwardChainExplosion, balance[7], balance[9]),
                    new TrioSynergyEffectStep(TrioSynergyEffectKind.CenterExplosion, balance[7], balance[9]),
                    new TrioSynergyEffectStep(TrioSynergyEffectKind.AlchemyFireField, balance[8], balance[9]),
                }, requiresBaseFire: true, requiresFirstReady: true),
                Define(TrioSynergyId.AssaultCorps, "assault-corps", new[] { LegionIds.ShieldGuard, LegionIds.SwordSoldier, LegionIds.WolfTamer }, "beast_commander", TrioSynergyTriggerKind.AssaultLinkedKill, SynergyTriggerPriority.ConditionReactive, balance.CooldownSeconds, new[]
                {
                    new TrioSynergyEffectStep(TrioSynergyEffectKind.BeastHowl),
                    new TrioSynergyEffectStep(TrioSynergyEffectKind.FixedChargePath, radius: balance[13]),
                    new TrioSynergyEffectStep(TrioSynergyEffectKind.ShieldAfterimageCharge, balance[10], balance[13]),
                    new TrioSynergyEffectStep(TrioSynergyEffectKind.SwordAfterimageSlashes, balance[11], balance[13]),
                    new TrioSynergyEffectStep(TrioSynergyEffectKind.WolfAfterimageRoutes, targetCount: 3),
                    new TrioSynergyEffectStep(TrioSynergyEffectKind.PackBiteHighestHealth, balance[12]),
                }, requiresSeparateCounters: true),
                Define(TrioSynergyId.SanctuaryGuard, "sanctuary-guard", new[] { LegionIds.ShieldGuard, LegionIds.Cleric, LegionIds.WraithKnight }, "light_guide", TrioSynergyTriggerKind.SanctuaryCounterattack, SynergyTriggerPriority.Periodic, balance.CooldownSeconds, new[]
                {
                    new TrioSynergyEffectStep(TrioSynergyEffectKind.SpiritShieldBlock),
                    new TrioSynergyEffectStep(TrioSynergyEffectKind.WraithBind, durationSeconds: balance[14]),
                    new TrioSynergyEffectStep(TrioSynergyEffectKind.HolyPillar, balance[15], balance[16]),
                }),
            });
        }
    }
}
