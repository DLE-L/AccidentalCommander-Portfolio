using System;
using Lizzo.PV.Data;
using Lizzo.PV.P0.Cards;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    public readonly struct CompanionPassiveCombatModifiers
    {
        public readonly float DamageMultiplier;
        public readonly float PeriodMultiplier;
        public readonly float RangeMultiplier;
        public readonly float ProjectileSpeedMultiplier;
        public readonly float HealMultiplier;
        public readonly float HealPeriodMultiplier;

        public CompanionPassiveCombatModifiers(float damageMultiplier, float periodMultiplier, float rangeMultiplier, float projectileSpeedMultiplier, float healMultiplier, float healPeriodMultiplier)
        {
            DamageMultiplier = damageMultiplier;
            PeriodMultiplier = periodMultiplier;
            RangeMultiplier = rangeMultiplier;
            ProjectileSpeedMultiplier = projectileSpeedMultiplier;
            HealMultiplier = healMultiplier;
            HealPeriodMultiplier = healPeriodMultiplier;
        }

        public static CompanionPassiveCombatModifiers Identity => new CompanionPassiveCombatModifiers(1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f);
    }

    public readonly struct CommanderPassiveModifiers
    {
        public readonly int BasicDamageBonus;
        public readonly float MoveSpeedBonus;
        public readonly float AbsorbRadiusBonus;
        public readonly int MaxHpBonus;
        public readonly float ExperienceMultiplier;
        public readonly float GuardShockwaveRadiusMultiplier;
        public readonly float GuardCompanionDurationBonus;

        public CommanderPassiveModifiers(int basicDamageBonus, float moveSpeedBonus, float absorbRadiusBonus, int maxHpBonus, float experienceMultiplier, float guardShockwaveRadiusMultiplier, float guardCompanionDurationBonus)
        {
            BasicDamageBonus = basicDamageBonus;
            MoveSpeedBonus = moveSpeedBonus;
            AbsorbRadiusBonus = absorbRadiusBonus;
            MaxHpBonus = maxHpBonus;
            ExperienceMultiplier = experienceMultiplier;
            GuardShockwaveRadiusMultiplier = guardShockwaveRadiusMultiplier;
            GuardCompanionDurationBonus = guardCompanionDurationBonus;
        }

        public static CommanderPassiveModifiers Identity => new CommanderPassiveModifiers(0, 0.0f, 0.0f, 0, 1.0f, 1.0f, 0.0f);
    }

    public static class CommanderPassiveHealth
    {
        public static int ResolveCurrentHp(int currentHp, int currentMaxHp, int nextMaxHp)
        {
            int normalizedNextMax = Mathf.Max(1, nextMaxHp);
            int normalizedCurrent = Mathf.Clamp(currentHp, 0, Mathf.Max(1, currentMaxHp));
            if (normalizedNextMax > currentMaxHp)
                normalizedCurrent += normalizedNextMax - currentMaxHp;
            return Mathf.Min(normalizedCurrent, normalizedNextMax);
        }
    }

    public sealed class CompanionPassiveCombatResolver
    {
        private const string MeleeTraining = "passive_melee_training";
        private const string FrontlineTempo = "passive_frontline_tempo";
        private const string RangedTraining = "passive_ranged_training";
        private const string ProjectileSpeed = "passive_projectile_speed";
        private const string LongRange = "passive_long_range";
        private const string HealingPrayer = "passive_healing_prayer";
        private const string SwiftPrayer = "passive_swift_prayer";
        private const string OldFlag = "passive_old_flag";
        private const string WarDrum = "passive_war_drum";
        private const string BlueShieldCrest = "passive_blue_shield_crest";
        private const string HoldFormation = "passive_hold_formation";
        private const string BattleCommand = "passive_battle_command";
        private const string MarchSpeed = "passive_march_speed";
        private const string CommandRadius = "passive_command_radius";
        private const string SurvivalInstinct = "passive_survival_instinct";
        private const string SupplyPouch = "passive_supply_pouch";

        private readonly IDataProvider _data;
        private readonly PassiveRosterState _roster;

        public CompanionPassiveCombatResolver(IDataProvider data, PassiveRosterState roster)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _roster = roster ?? throw new ArgumentNullException(nameof(roster));
        }

        public CompanionPassiveCombatModifiers Resolve(string baseUnitId)
        {
            CompanionRosterData rosterData = _data.GetCompanionRoster(baseUnitId);
            CompanionCombatProfileData profile = _data.GetCompanionCombatProfile(baseUnitId);
            if (rosterData == null || profile == null)
                return CompanionPassiveCombatModifiers.Identity;

            CombatEffectData basic = _data.GetCombatEffect(profile.BasicEffectId);
            CombatEffectData secondary = _data.GetCombatEffect(profile.SecondaryEffectId);
            bool isMelee = HasExactTag(rosterData.FamilyTags, "melee_family") || HasExactTag(rosterData.FamilyTags, "sword_family");
            bool isRanged = HasExactTag(rosterData.FamilyTags, "ranged_family");
            bool isProjectile = basic != null && basic.DeliveryKind == CombatDeliveryKind.Projectile;
            bool hasHealingSkill = secondary != null && secondary.EffectKind == CombatEffectKind.Heal;

            float damage = Value(OldFlag);
            if (isMelee) damage *= Value(MeleeTraining);
            if (isRanged) damage *= Value(RangedTraining);

            float period = Value(WarDrum);
            if (isMelee) period *= Value(FrontlineTempo);

            float range = isRanged ? Value(LongRange) : 1.0f;
            float projectileSpeed = isProjectile ? Value(ProjectileSpeed) : 1.0f;
            float heal = hasHealingSkill ? Value(HealingPrayer) : 1.0f;
            float healPeriod = hasHealingSkill ? Value(SwiftPrayer) * Value(WarDrum) : 1.0f;
            return new CompanionPassiveCombatModifiers(damage, period, range, projectileSpeed, heal, healPeriod);
        }

        public CommanderPassiveModifiers ResolveCommander()
        {
            return new CommanderPassiveModifiers(
                Mathf.RoundToInt(ValueOrDefault(BattleCommand, 0.0f)),
                ValueOrDefault(MarchSpeed, 0.0f),
                ValueOrDefault(CommandRadius, 0.0f),
                Mathf.RoundToInt(ValueOrDefault(SurvivalInstinct, 0.0f)),
                ValueOrDefault(SupplyPouch, 1.0f),
                ValueOrDefault(BlueShieldCrest, 1.0f),
                ValueOrDefault(HoldFormation, 0.0f));
        }

        private float Value(string passiveId)
        {
            int level = _roster.GetLevel(passiveId);
            if (level <= 0) return 1.0f;
            return ValueAtLevel(passiveId, level);
        }

        private float ValueOrDefault(string passiveId, float defaultValue)
        {
            int level = _roster.GetLevel(passiveId);
            return level <= 0 ? defaultValue : ValueAtLevel(passiveId, level);
        }

        private float ValueAtLevel(string passiveId, int level)
        {
            PassiveData data = _data.GetPassive(passiveId);
            if (data == null) throw new InvalidOperationException($"Canonical passive data is missing: {passiveId}");
            return level == 1 ? data.Level1Value : level == 2 ? data.Level2Value : data.Level3Value;
        }

        private static bool HasExactTag(string tags, string expected)
        {
            if (string.IsNullOrEmpty(tags) || string.IsNullOrEmpty(expected)) return false;
            int start = 0;
            while (start < tags.Length)
            {
                int end = tags.IndexOf(',', start);
                if (end < 0) end = tags.Length;
                if (end - start == expected.Length && string.CompareOrdinal(tags, start, expected, 0, expected.Length) == 0) return true;
                start = end + 1;
            }
            return false;
        }
    }
}
