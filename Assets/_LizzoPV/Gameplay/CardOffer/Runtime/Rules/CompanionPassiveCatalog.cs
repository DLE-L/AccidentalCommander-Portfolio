using System;
using System.Collections.Generic;
using Lizzo.PV.Data;

namespace Lizzo.PV.Gameplay.CardOffer
{
    public readonly struct CompanionPassiveCatalogEntry
    {
        public CompanionPassiveCatalogEntry(
            CardKind cardKind,
            string id,
            string titleKo,
            string descriptionKo,
            string requiredLineageId,
            string effectId,
            float level1,
            float level2,
            float level3,
            int maxLevel)
        {
            CardKind = cardKind;
            Id = id;
            TitleKo = titleKo;
            DescriptionKo = descriptionKo;
            RequiredLineageId = requiredLineageId;
            EffectId = effectId;
            Level1 = level1;
            Level2 = level2;
            Level3 = level3;
            MaxLevel = maxLevel;
        }

        public CardKind CardKind { get; }
        public string Id { get; }
        public string TitleKo { get; }
        public string DescriptionKo { get; }
        public string RequiredLineageId { get; }
        public string EffectId { get; }
        public float Level1 { get; }
        public float Level2 { get; }
        public float Level3 { get; }
        public int MaxLevel { get; }
        public bool IsCommon => string.IsNullOrEmpty(RequiredLineageId);
    }

    public static class CompanionPassiveCatalog
    {
        private static readonly CompanionPassiveCatalogEntry[] Entries =
        {
            Common(CardKind.PassiveStandardBearer, "passive_standard_bearer", "군단 깃발", "모든 군단 피해가 증가합니다.", "legion_damage_multiplier", 1.10f, 1.20f, 1.30f),
            Common(CardKind.PassiveCommonWarDrum, "passive_war_drum", "전쟁 북", "모든 군단의 행동 주기가 빨라집니다.", "action_interval_multiplier", 0.94f, 0.88f, 0.82f),
            Common(CardKind.PassiveScoutingBanner, "passive_scouting_banner", "정찰 깃발", "군단의 표적 탐색 범위가 증가합니다.", "acquisition_range_multiplier", 1.08f, 1.16f, 1.24f),
            Common(CardKind.PassiveWideFormation, "passive_wide_formation", "넓은 진형", "범위 공격의 반경이 증가합니다.", "area_radius_multiplier", 1.08f, 1.16f, 1.24f),
            Common(CardKind.PassiveMarchingBoots, "passive_marching_boots", "행군 장화", "군단장의 이동속도가 증가합니다.", "commander_move_speed_multiplier", 1.08f, 1.16f, 1.24f),
            Common(CardKind.PassiveReinforcedArmor, "passive_reinforced_armor", "보강 갑옷", "군단장의 최대 체력이 증가합니다.", "commander_max_health_multiplier", 1.10f, 1.20f, 1.30f),
            Common(CardKind.PassiveCommonSupplyPouch, "passive_supply_pouch", "보급 주머니", "경험치 획득량이 증가합니다.", "experience_gain_multiplier", 1.10f, 1.20f, 1.30f),
            Common(CardKind.PassiveEliteDoctrine, "passive_elite_doctrine", "소수 정예", "활성 군단이 3종 이하일 때 군단 피해가 증가합니다.", "elite_roster_damage_multiplier", 1.10f, 1.20f, 1.30f),
            Common(CardKind.PassiveHeavyFormation, "passive_heavy_formation", "무거운 진형", "군단이 가하는 강제 이동 거리가 증가합니다.", "forced_movement_multiplier", 1.10f, 1.20f, 1.30f),
            Common(CardKind.PassiveLingeringTactics, "passive_lingering_tactics", "지속 전술", "군단이 부여하는 상태 효과가 오래 지속됩니다.", "status_duration_multiplier", 1.10f, 1.20f, 1.30f),
            Common(CardKind.PassiveSustainedSummons, "passive_sustained_summons", "지속 소환", "소환체와 장판의 지속시간이 증가합니다.", "owned_effect_duration_multiplier", 1.10f, 1.20f, 1.30f),
            Common(CardKind.PassiveVeteranCommand, "passive_veteran_command", "숙련 지휘", "진급체의 행동 주기가 빨라집니다.", "promoted_interval_multiplier", 0.94f, 0.88f, 0.82f),

            Role(CardKind.PassiveSwordGreatsword, "passive_sword_greatsword", "대검", "검병 공격 범위 +25%", "sword_soldier", "area_radius_multiplier", 1.25f),
            Role(CardKind.PassiveSwordFocusedStrike, "passive_sword_focused_strike", "집중 베기", "검병 피해 +25%", "sword_soldier", "damage_multiplier", 1.25f),
            Role(CardKind.PassiveSwordAfterimage, "passive_sword_afterimage", "검의 잔상", "검병 공격에 잔상 1회를 추가합니다.", "sword_soldier", "extra_action_count", 1.0f),
            Role(CardKind.PassiveSwordFootwork, "passive_sword_footwork", "검사의 발놀림", "검병 접근·복귀 속도 +25%", "sword_soldier", "excursion_speed_multiplier", 1.25f),
            Role(CardKind.PassiveShieldWideStrike, "passive_shield_wide_strike", "넓은 방패치기", "방패병 공격 범위 +25%", "shield_guard", "area_radius_multiplier", 1.25f),
            Role(CardKind.PassiveShieldStrongPush, "passive_shield_strong_push", "강한 밀치기", "방패병 밀치기 거리 +25%", "shield_guard", "forced_movement_multiplier", 1.25f),
            Role(CardKind.PassiveShieldReturnTrail, "passive_shield_return_trail", "방패의 귀환", "방패병 복귀 경로에 추가 타격을 남깁니다.", "shield_guard", "return_trail_count", 1.0f),
            Role(CardKind.PassiveShieldCloseIntercept, "passive_shield_close_intercept", "근접 차단", "군단장 근처의 적에게 방패병 피해 +25%", "shield_guard", "close_damage_multiplier", 1.25f),
            Role(CardKind.PassiveClericPiercingLight, "passive_cleric_piercing_light", "관통하는 빛", "성직자 투사체 관통 +1", "cleric", "projectile_pierce_bonus", 1.0f),
            Role(CardKind.PassiveClericSplitLight, "passive_cleric_split_light", "갈라지는 빛", "성직자 투사체가 2갈래로 발사됩니다.", "cleric", "projectile_count", 2.0f),
            Role(CardKind.PassiveClericSwiftReturn, "passive_cleric_swift_return", "빠른 귀환", "성직자 투사체 귀환 속도 +25%", "cleric", "return_speed_multiplier", 1.25f),
            Role(CardKind.PassiveClericFullPrayer, "passive_cleric_full_prayer", "온전한 기도", "성직자 회복량 +25%", "cleric", "healing_multiplier", 1.25f),

            Role(CardKind.PassiveArcherMultiShot, "passive_archer_multi_shot", "세 갈래 화살", "궁수 화살이 3갈래로 발사됩니다.", "falcon_archer", "projectile_count", 3.0f),
            Role(CardKind.PassiveArcherDoubleVolley, "passive_archer_double_volley", "연속 사격", "궁수가 2연발로 발사합니다.", "falcon_archer", "extra_action_count", 1.0f),
            Role(CardKind.PassiveArcherPiercingArrow, "passive_archer_piercing_arrow", "관통 화살", "궁수 화살 관통 +2", "falcon_archer", "projectile_pierce_bonus", 2.0f),
            Role(CardKind.PassiveArcherPenetrationAcceleration, "passive_archer_penetration_acceleration", "관통 가속", "관통할 때마다 화살 피해가 증가합니다.", "falcon_archer", "penetration_damage_step", 0.20f),
            Role(CardKind.PassiveBombDoubleThrow, "passive_bomb_double_throw", "두 폭탄", "폭탄을 2개 투척합니다.", "bombardier", "projectile_count", 2.0f),
            Role(CardKind.PassiveBombShortFuse, "passive_bomb_short_fuse", "짧은 도화선", "폭탄 폭발 대기시간 -35%", "bombardier", "delivery_delay_multiplier", 0.65f),
            Role(CardKind.PassiveBombFragments, "passive_bomb_fragments", "파편 폭탄", "폭발 후 파편 6개가 퍼집니다.", "bombardier", "fragment_count", 6.0f),
            Role(CardKind.PassiveBombCompressedPowder, "passive_bomb_compressed_powder", "압축 화약", "폭발 중심부 피해 +50%", "bombardier", "center_damage_multiplier", 1.50f),
            Role(CardKind.PassiveScytheGiantBlade, "passive_scythe_giant_blade", "거대 낫", "낫 공격 폭 +30%", "skeleton_scythe_thrower", "area_radius_multiplier", 1.30f),
            Role(CardKind.PassiveScytheSwiftReturn, "passive_scythe_swift_return", "신속 귀환", "낫 귀환 속도 +30%", "skeleton_scythe_thrower", "return_speed_multiplier", 1.30f),
            Role(CardKind.PassiveScytheDoubleDirection, "passive_scythe_double_direction", "양방향 낫", "낫을 양방향으로 발사합니다.", "skeleton_scythe_thrower", "projectile_count", 2.0f),
            Role(CardKind.PassiveScytheRoundTripHarvest, "passive_scythe_round_trip_harvest", "왕복 수확", "돌아오는 낫 피해 +35%", "skeleton_scythe_thrower", "return_damage_multiplier", 1.35f),

            Role(CardKind.PassiveHerbalistWideFlask, "passive_herbalist_wide_flask", "넓은 약병", "약병·취약 전파 범위 +30%", "field_herbalist", "area_radius_multiplier", 1.30f),
            Role(CardKind.PassiveHerbalistConcentratedMixture, "passive_herbalist_concentrated_mixture", "농축 혼합물", "취약의 받는 피해 증가 +20% → +35%", "field_herbalist", "status_magnitude_bonus", 0.15f),
            Role(CardKind.PassiveHerbalistLongReaction, "passive_herbalist_long_reaction", "긴 반응", "취약 지속시간 +50%", "field_herbalist", "status_duration_multiplier", 1.50f),
            Role(CardKind.PassiveHerbalistReactiveCompound, "passive_herbalist_reactive_compound", "반응성 화합물", "진급 취약 전파 대상 +2", "field_herbalist", "status_target_bonus", 2.0f),
            Role(CardKind.PassiveFireWideField, "passive_fire_wide_field", "넓은 화염진", "화염 장판 범위 +30%", "fire_mage", "area_radius_multiplier", 1.30f),
            Role(CardKind.PassiveFireLongBurn, "passive_fire_long_burn", "긴 연소", "화염 장판 지속시간 +50%", "fire_mage", "owned_effect_duration_multiplier", 1.50f),
            Role(CardKind.PassiveFireRapidCombustion, "passive_fire_rapid_combustion", "급속 연소", "화염 장판 피해 간격 -30%", "fire_mage", "field_tick_multiplier", 0.70f),
            Role(CardKind.PassiveFireAdditionalField, "passive_fire_additional_field", "추가 화염진", "동시에 유지하는 화염 장판 +1", "fire_mage", "field_capacity_bonus", 1.0f),
            Role(CardKind.PassiveLightningAdditionalChains, "passive_lightning_additional_chains", "연쇄 증폭", "번개 연쇄 대상 +2", "lightning_mage", "chain_target_bonus", 2.0f),
            Role(CardKind.PassiveLightningConductiveArc, "passive_lightning_conductive_arc", "전도 호", "연쇄 피해 감소를 완화합니다.", "lightning_mage", "chain_damage_retention", 0.10f),
            Role(CardKind.PassiveLightningLongShock, "passive_lightning_long_shock", "긴 감전", "감전 지속시간 +50%", "lightning_mage", "status_duration_multiplier", 1.50f),
            Role(CardKind.PassiveLightningWideOverload, "passive_lightning_wide_overload", "넓은 과부하", "과부하 범위 +35%", "lightning_mage", "area_radius_multiplier", 1.35f),

            Role(CardKind.PassiveWolfFang, "passive_wolf_fang", "늑대 송곳니", "늑대 피해 +25%", "wolf_tamer", "damage_multiplier", 1.25f),
            Role(CardKind.PassiveWolfRelentlessHunt, "passive_wolf_relentless_hunt", "끈질긴 사냥", "늑대 연쇄 공격 +1", "wolf_tamer", "chain_target_bonus", 1.0f),
            Role(CardKind.PassiveWolfExecutionSense, "passive_wolf_execution_sense", "처형 감각", "체력 20% 이하 적을 처형합니다.", "wolf_tamer", "execution_threshold", 0.20f),
            Role(CardKind.PassiveWolfPackFerocity, "passive_wolf_pack_ferocity", "무리의 흉포", "늑대 무리 피해 +30%", "wolf_tamer", "promoted_damage_multiplier", 1.30f),
            Role(CardKind.PassiveWraithWideSlash, "passive_wraith_wide_slash", "넓은 혼령 베기", "망령기사 공격 범위 +30%", "wraith_knight", "area_radius_multiplier", 1.30f),
            Role(CardKind.PassiveWraithDeepWeakening, "passive_wraith_deep_weakening", "깊은 약화", "약화 효과량 +10%", "wraith_knight", "status_magnitude_bonus", 0.10f),
            Role(CardKind.PassiveWraithLingeringWeakening, "passive_wraith_lingering_weakening", "남는 약화", "약화 지속시간 +50%", "wraith_knight", "status_duration_multiplier", 1.50f),
            Role(CardKind.PassiveWraithWidePatrol, "passive_wraith_wide_patrol", "넓은 순찰", "망령 수호장 순찰 반경 +35%", "wraith_knight", "orbit_radius_multiplier", 1.35f),
            Role(CardKind.PassiveNecromancerLongCurse, "passive_necromancer_long_curse", "긴 저주", "저주 지속시간 +50%", "necromancer", "status_duration_multiplier", 1.50f),
            Role(CardKind.PassiveNecromancerStrongPull, "passive_necromancer_strong_pull", "강한 인력", "저주 당김 범위 +40%", "necromancer", "curse_pull_radius_multiplier", 1.40f),
            Role(CardKind.PassiveNecromancerAdditionalSkeleton, "passive_necromancer_additional_skeleton", "추가 해골", "의식으로 생성하는 해골 +2", "necromancer", "owned_actor_count_bonus", 2.0f),
            Role(CardKind.PassiveNecromancerLongRitual, "passive_necromancer_long_ritual", "긴 의식", "의식 지속시간 +50%", "necromancer", "owned_effect_duration_multiplier", 1.50f),
        };

        private static readonly Dictionary<CardKind, CompanionPassiveCatalogEntry> ByKind = BuildByKind();
        private static readonly Dictionary<string, CompanionPassiveCatalogEntry> ById = BuildById();

        public static IReadOnlyList<CompanionPassiveCatalogEntry> All => Entries;

        public static bool TryGet(CardKind kind, out CompanionPassiveCatalogEntry entry) => ByKind.TryGetValue(kind, out entry);
        public static bool TryGet(string id, out CompanionPassiveCatalogEntry entry)
        {
            if (id != null && ById.TryGetValue(id, out entry)) return true;
            entry = default;
            return false;
        }
        public static int ResolveMaxLevel(string id) => TryGet(id, out CompanionPassiveCatalogEntry entry) ? entry.MaxLevel : PassiveRosterState.MaxLevel;

        public static PassiveData CreateData(in CompanionPassiveCatalogEntry entry)
        {
            return new PassiveData
            {
                Id = entry.Id,
                Category = entry.IsCommon ? "common" : "role",
                EligibleTarget = entry.IsCommon ? "all" : entry.RequiredLineageId,
                EffectId = entry.EffectId,
                ValueType = "multiplier_or_count",
                Level1Value = entry.Level1,
                Level2Value = entry.Level2,
                Level3Value = entry.Level3,
                StackRule = entry.MaxLevel == 1 ? "single" : "replace_by_level",
                TitleKo = entry.TitleKo,
                TitleEn = entry.Id,
                DescriptionTemplateKo = entry.DescriptionKo,
                OfferWeightRule = "equal",
                Prohibition = "none",
            };
        }

        private static CompanionPassiveCatalogEntry Common(CardKind kind, string id, string title, string description, string effect, float l1, float l2, float l3)
            => new CompanionPassiveCatalogEntry(kind, id, title, description, null, effect, l1, l2, l3, 3);

        private static CompanionPassiveCatalogEntry Role(CardKind kind, string id, string title, string description, string lineage, string effect, float value)
            => new CompanionPassiveCatalogEntry(kind, id, title, description, lineage, effect, value, value, value, 1);

        private static Dictionary<CardKind, CompanionPassiveCatalogEntry> BuildByKind()
        {
            Dictionary<CardKind, CompanionPassiveCatalogEntry> result = new Dictionary<CardKind, CompanionPassiveCatalogEntry>();
            for (int index = 0; index < Entries.Length; index++) result.Add(Entries[index].CardKind, Entries[index]);
            return result;
        }

        private static Dictionary<string, CompanionPassiveCatalogEntry> BuildById()
        {
            Dictionary<string, CompanionPassiveCatalogEntry> result = new Dictionary<string, CompanionPassiveCatalogEntry>(StringComparer.Ordinal);
            for (int index = 0; index < Entries.Length; index++) result.Add(Entries[index].Id, Entries[index]);
            return result;
        }
    }
}
