using System;
using Lizzo.PV.Data;
using Lizzo.PV.Gameplay.CardOffer;
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
        public readonly float AreaRadiusMultiplier;
        public readonly float AcquisitionRangeMultiplier;
        public readonly float ForcedMovementMultiplier;
        public readonly float StatusMagnitudeBonus;
        public readonly float StatusDurationMultiplier;
        public readonly float OwnedEffectDurationMultiplier;
        public readonly float FieldTickIntervalMultiplier;
        public readonly int FieldCapacityBonus;
        public readonly int ProjectileCount;
        public readonly int ProjectilePierceBonus;
        public readonly int ExtraHitCount;
        public readonly float ReturnSpeedMultiplier;
        public readonly float ReturnDamageMultiplier;
        public readonly float CenterDamageMultiplier;
        public readonly int FragmentCount;
        public readonly int ChainTargetBonus;
        public readonly float ExecutionThreshold;
        public readonly float PromotedDamageMultiplier;
        public readonly float PromotedPeriodMultiplier;
        public readonly float ExcursionSpeedMultiplier;
        public readonly float CloseDamageMultiplier;
        public readonly int OwnedActorCountBonus;
        public readonly float DeliveryDelayMultiplier;
        public readonly float ChainDamageRetentionBonus;
        public readonly float PenetrationDamageStep;

        public CompanionPassiveCombatModifiers(float damageMultiplier, float periodMultiplier, float rangeMultiplier, float projectileSpeedMultiplier, float healMultiplier, float healPeriodMultiplier)
            : this(
                damageMultiplier, periodMultiplier, rangeMultiplier, projectileSpeedMultiplier, healMultiplier, healPeriodMultiplier,
                rangeMultiplier, rangeMultiplier, 1.0f, 0.0f, 1.0f, 1.0f, 1.0f, 0, 1, 0, 0,
                1.0f, 1.0f, 1.0f, 0, 0, 0.0f, 1.0f, 1.0f, 1.0f, 1.0f, 0, 1.0f, 0.0f, 0.0f)
        {
        }

        public CompanionPassiveCombatModifiers(
            float damageMultiplier,
            float periodMultiplier,
            float rangeMultiplier,
            float projectileSpeedMultiplier,
            float healMultiplier,
            float healPeriodMultiplier,
            float areaRadiusMultiplier,
            float acquisitionRangeMultiplier,
            float forcedMovementMultiplier,
            float statusMagnitudeBonus,
            float statusDurationMultiplier,
            float ownedEffectDurationMultiplier,
            float fieldTickIntervalMultiplier,
            int fieldCapacityBonus,
            int projectileCount,
            int projectilePierceBonus,
            int extraHitCount,
            float returnSpeedMultiplier,
            float returnDamageMultiplier,
            float centerDamageMultiplier,
            int fragmentCount,
            int chainTargetBonus,
            float executionThreshold,
            float promotedDamageMultiplier,
            float promotedPeriodMultiplier,
            float excursionSpeedMultiplier,
            float closeDamageMultiplier,
            int ownedActorCountBonus,
            float deliveryDelayMultiplier,
            float chainDamageRetentionBonus,
            float penetrationDamageStep)
        {
            DamageMultiplier = damageMultiplier;
            PeriodMultiplier = periodMultiplier;
            RangeMultiplier = rangeMultiplier;
            ProjectileSpeedMultiplier = projectileSpeedMultiplier;
            HealMultiplier = healMultiplier;
            HealPeriodMultiplier = healPeriodMultiplier;
            AreaRadiusMultiplier = areaRadiusMultiplier;
            AcquisitionRangeMultiplier = acquisitionRangeMultiplier;
            ForcedMovementMultiplier = forcedMovementMultiplier;
            StatusMagnitudeBonus = statusMagnitudeBonus;
            StatusDurationMultiplier = statusDurationMultiplier;
            OwnedEffectDurationMultiplier = ownedEffectDurationMultiplier;
            FieldTickIntervalMultiplier = fieldTickIntervalMultiplier;
            FieldCapacityBonus = fieldCapacityBonus;
            ProjectileCount = projectileCount;
            ProjectilePierceBonus = projectilePierceBonus;
            ExtraHitCount = extraHitCount;
            ReturnSpeedMultiplier = returnSpeedMultiplier;
            ReturnDamageMultiplier = returnDamageMultiplier;
            CenterDamageMultiplier = centerDamageMultiplier;
            FragmentCount = fragmentCount;
            ChainTargetBonus = chainTargetBonus;
            ExecutionThreshold = executionThreshold;
            PromotedDamageMultiplier = promotedDamageMultiplier;
            PromotedPeriodMultiplier = promotedPeriodMultiplier;
            ExcursionSpeedMultiplier = excursionSpeedMultiplier;
            CloseDamageMultiplier = closeDamageMultiplier;
            OwnedActorCountBonus = ownedActorCountBonus;
            DeliveryDelayMultiplier = deliveryDelayMultiplier;
            ChainDamageRetentionBonus = chainDamageRetentionBonus;
            PenetrationDamageStep = penetrationDamageStep;
        }

        public static CompanionPassiveCombatModifiers Identity => new CompanionPassiveCombatModifiers(1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f);
    }

    public readonly struct CommanderPassiveModifiers
    {
        public readonly float ExperienceMultiplier;
        public readonly float MoveSpeedMultiplier;
        public readonly float MaxHpMultiplier;

        public CommanderPassiveModifiers(float experienceMultiplier, float moveSpeedMultiplier, float maxHpMultiplier)
        {
            ExperienceMultiplier = experienceMultiplier;
            MoveSpeedMultiplier = moveSpeedMultiplier;
            MaxHpMultiplier = maxHpMultiplier;
        }

        public static CommanderPassiveModifiers Identity => new CommanderPassiveModifiers(1.0f, 1.0f, 1.0f);
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
        private const string WarDrum = "passive_war_drum";
        private const string SupplyPouch = "passive_supply_pouch";

        private readonly IDataProvider _data;
        private readonly PassiveRosterState _roster;
        private readonly Func<int> _activeLegionCount;
        public CompanionPassiveCombatResolver(IDataProvider data, PassiveRosterState roster)
            : this(data, roster, null)
        {
        }

        public CompanionPassiveCombatResolver(
            IDataProvider data,
            PassiveRosterState roster,
            Func<int> activeLegionCount)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _roster = roster ?? throw new ArgumentNullException(nameof(roster));
            _activeLegionCount = activeLegionCount;
        }

        public CompanionPassiveCombatModifiers Resolve(string baseUnitId)
        {
            CompanionRosterData rosterData = _data.GetCompanionRoster(baseUnitId);
            CompanionCombatProfileData profile = _data.GetCompanionCombatProfile(baseUnitId);
            if (rosterData == null || profile == null)
                return CompanionPassiveCombatModifiers.Identity;

            CombatEffectData secondary = _data.GetCombatEffect(profile.SecondaryEffectId);
            bool hasHealingSkill = secondary != null && secondary.EffectKind == CombatEffectKind.Heal;

            float damage = Value("passive_standard_bearer");
            int activeCount = _activeLegionCount == null ? 0 : _activeLegionCount();
            if (activeCount > 0 && activeCount <= 3)
                damage *= Value("passive_elite_doctrine");

            damage *= Value(RolePassive(baseUnitId, "focused_strike", "fang"));

            float period = Value("passive_war_drum");

            float acquisitionRange = Value("passive_scouting_banner")
                * Value(RolePassive(baseUnitId, "wide_patrol"));
            float areaRadius = Value("passive_wide_formation")
                * Value(RolePassive(baseUnitId, "greatsword", "wide_strike", "giant_blade", "wide_flask", "wide_field", "wide_overload", "wide_slash"));
            float range = acquisitionRange;
            float projectileSpeed = 1.0f;
            float heal = hasHealingSkill
                ? Value(RolePassive(baseUnitId, "full_prayer"))
                : 1.0f;
            float healPeriod = hasHealingSkill
                ? Value(WarDrum)
                : 1.0f;
            return new CompanionPassiveCombatModifiers(
                damage,
                period,
                range,
                projectileSpeed,
                heal,
                healPeriod,
                areaRadius,
                acquisitionRange,
                Value("passive_heavy_formation") * Value(RolePassive(baseUnitId, "strong_push", "strong_pull")),
                ValueOrDefault(RolePassive(baseUnitId, "concentrated_mixture", "deep_weakening"), 0.0f),
                Value("passive_lingering_tactics") * Value(RolePassive(baseUnitId, "long_reaction", "long_shock", "lingering_weakening", "long_curse")),
                Value("passive_sustained_summons") * Value(RolePassive(baseUnitId, "long_burn", "long_ritual")),
                Value(RolePassive(baseUnitId, "rapid_combustion")),
                Mathf.RoundToInt(ValueOrDefault(RolePassive(baseUnitId, "additional_field"), 0.0f)),
                Mathf.Max(1, Mathf.RoundToInt(ValueOrDefault(RolePassive(baseUnitId, "split_light", "multi_shot", "double_throw", "double_direction"), 1.0f))),
                Mathf.RoundToInt(ValueOrDefault(RolePassive(baseUnitId, "piercing_light", "piercing_arrow"), 0.0f)),
                Mathf.RoundToInt(ValueOrDefault(RolePassive(baseUnitId, "afterimage", "double_volley", "return_trail"), 0.0f)),
                Value(RolePassive(baseUnitId, "swift_return")),
                Value(RolePassive(baseUnitId, "round_trip_harvest")),
                Value(RolePassive(baseUnitId, "compressed_powder")),
                Mathf.RoundToInt(ValueOrDefault(RolePassive(baseUnitId, "fragments"), 0.0f)),
                Mathf.RoundToInt(ValueOrDefault(RolePassive(baseUnitId, "additional_chains", "relentless_hunt", "reactive_compound"), 0.0f)),
                ValueOrDefault(RolePassive(baseUnitId, "execution_sense"), 0.0f),
                Value(RolePassive(baseUnitId, "pack_ferocity")),
                Value("passive_veteran_command"),
                Value(RolePassive(baseUnitId, "footwork")),
                Value(RolePassive(baseUnitId, "close_intercept")),
                Mathf.RoundToInt(ValueOrDefault(RolePassive(baseUnitId, "additional_skeleton"), 0.0f)),
                Value(RolePassive(baseUnitId, "short_fuse")),
                ValueOrDefault(RolePassive(baseUnitId, "conductive_arc"), 0.0f),
                ValueOrDefault(RolePassive(baseUnitId, "penetration_acceleration"), 0.0f));
        }

        public CommanderPassiveModifiers ResolveCommander()
        {
            return new CommanderPassiveModifiers(
                ValueOrDefault(SupplyPouch, 1.0f),
                Value("passive_marching_boots"),
                Value("passive_reinforced_armor"));
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
            PassiveData data = CompanionPassiveCatalog.TryGet(passiveId, out CompanionPassiveCatalogEntry entry)
                ? CompanionPassiveCatalog.CreateData(in entry)
                : null;
            if (data == null) throw new InvalidOperationException($"Canonical passive data is missing: {passiveId}");
            return level == 1 ? data.Level1Value : level == 2 ? data.Level2Value : data.Level3Value;
        }

        private static string RolePassive(string baseUnitId, params string[] suffixes)
        {
            if (string.IsNullOrEmpty(baseUnitId) || suffixes == null)
                return null;
            string prefix = baseUnitId switch
            {
                "sword_soldier" => "passive_sword_",
                "shield_guard" => "passive_shield_",
                "cleric" => "passive_cleric_",
                "falcon_archer" => "passive_archer_",
                "bombardier" => "passive_bomb_",
                "skeleton_scythe_thrower" => "passive_scythe_",
                "field_herbalist" => "passive_herbalist_",
                "fire_mage" => "passive_fire_",
                "lightning_mage" => "passive_lightning_",
                "wolf_tamer" => "passive_wolf_",
                "wraith_knight" => "passive_wraith_",
                "necromancer" => "passive_necromancer_",
                _ => null,
            };
            if (prefix == null)
                return null;
            for (int index = 0; index < suffixes.Length; index++)
            {
                string candidate = prefix + suffixes[index];
                if (CompanionPassiveCatalog.TryGet(candidate, out _))
                    return candidate;
            }
            return null;
        }

    }
}
