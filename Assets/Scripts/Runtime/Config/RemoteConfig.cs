namespace Lizzo.PV.P0.Config
{
    using Lizzo.PV.Data;

    public static class RemoteConfig
    {
        static IDataProvider s_data;

        public static void Configure(IDataProvider data)
        {
            s_data = data ?? throw new System.ArgumentNullException(nameof(data));
        }

        public static void ClearServices() => s_data = null;

        public const string FirstLevelExpKey = "rc_first_level_exp";
        public const string ShieldCardWeightKey = "rc_shield_card_weight";
        public const string SynergyTwoOfThreeWeightKey = "rc_synergy_two_of_three_weight";
        public const string GuardShieldPowerKey = "rc_guard_shield_power";
        public const string Boss1HpKey = "rc_boss1_hp";
        public const string Boss1AtkKey = "rc_boss1_atk";
        public const string Stage1SpawnBudgetKey = "rc_stage1_spawn_budget";
        public const string MaxEnemyStage1Key = "rc_max_enemy_stage1";
        public const string LowFxScaleKey = "rc_low_fx_scale";
        public const string RewardDoubleEnabledKey = "rc_reward_double_enabled";
        public const string ReviveAdEnabledKey = "rc_revive_ad_enabled";
        public const string RerollAdEnabledKey = "rc_reroll_ad_enabled";
        public const string FirstRunPaidPopupBlockKey = "rc_first_run_paid_popup_block";
        public const string StarterOfferMinRunCountKey = "rc_starter_offer_min_run_count";
        public const string CompanionDownDurationKey = "rc_companion_down_duration";
        public const string CompanionRecoverHpRatioKey = "rc_companion_recover_hp_ratio";
        public const string FormationSpacingKey = "rc_formation_spacing";
        public const string AttackLeashDistanceKey = "rc_attack_leash_distance";
        public const string FormationReturnSpeedKey = "rc_formation_return_speed";
        public const string CommanderVisibilityPushRadiusKey = "rc_commander_visibility_push_radius";
        public const string FormationVectorLockSecondsKey = "rc_formation_vector_lock_seconds";
        public const string ActiveCompanionVisualCapKey = "rc_active_companion_visual_cap";
        public const string ActiveCompanionSlotCapKey = "rc_active_companion_slot_cap";
        public const string FullSlotNewCompanionBlockKey = "rc_full_slot_new_companion_block";
        public const string FullSlotPromotionWeightKey = "rc_full_slot_promotion_weight";
        public const string ResultNextGoalEnabledKey = "rc_result_next_goal_enabled";
        public const string ResultNextGoalCopyKey = "rc_result_next_goal_copy";
        public const string SurroundedThreatRatioKey = "rc_surrounded_threat_ratio";
        public const string FirstRunFreeRerollCountKey = "rc_first_run_free_reroll_count";
        public const string Boss1WarningTimeKey = "rc_boss1_warning_time";
        public const string TutorialAssistEnabledKey = "rc_tutorial_assist_enabled";
        public const string CommanderHurtboxRadiusKey = "rc_commander_hurtbox_radius";
        public const string CommanderPostHitInvulnKey = "rc_commander_post_hit_invuln";
        public const string ContactDamageSourceCooldownKey = "rc_contact_damage_source_cd";
        public const string CompanionPostHitCooldownKey = "rc_companion_post_hit_cd";
        public const string CompanionHpScaleKey = "rc_companion_hp_scale";
        public const string CompanionSpawnProtectionKey = "rc_companion_spawn_protection";
        public const string ArcherSpawnProtectionKey = "rc_archer_spawn_protection";
        public const string GuardWallCooldownKey = "rc_guard_wall_cd";
        public const string GuardCompanionDamageReductionKey = "rc_guard_companion_dr";
        public const string GuardCompanionDamageReductionDurationKey = "rc_guard_companion_dr_duration";

        public static int FirstLevelExp => s_data?.RunTuning.FirstLevelExp ?? 8;
        public static float ShieldCardWeight => 3.0f;
        public static float SynergyTwoOfThreeWeight => 2.5f;
        public static int GuardShieldPower => s_data?.GetSynergy("guard_squad")?.ShieldDurability ?? 80;
        public static int Boss1Hp => s_data?.GetEnemy("boss_hungry_giant")?.Hp ?? 2500;
        public static int Boss1Atk => s_data?.GetEnemy("boss_hungry_giant")?.Attack ?? 25;
        public static float Stage1SpawnBudget => s_data?.GetStage1SpawnBudget(0.0f) ?? 1.0f;
        public static int MaxEnemyStage1 => s_data?.RunTuning.MaxEnemyStage1 ?? 50;
        public static float LowFxScale => s_data?.RunTuning.LowFxScale ?? 1.0f;
        public static bool RewardDoubleEnabled => true;
        public static bool ReviveAdEnabled => true;
        public static bool RerollAdEnabled => true;
        public static bool FirstRunPaidPopupBlock => true;
        public static int StarterOfferMinRunCount => 3;
        public static float CompanionDownDuration => 4.0f;
        public static float CompanionRecoverHpRatio => 0.3f;
        public static float FormationSpacing => 0.60f;
        public static float AttackLeashDistance => 2.2f;
        public static float FormationReturnSpeed => 4.8f;
        public static float CommanderVisibilityPushRadius => 0.7f;
        public static float FormationVectorLockSeconds => 1.7f;
        public static int ActiveCompanionVisualCap => 8;
        public static int ActiveCompanionSlotCap => 7;
        public static bool FullSlotNewCompanionBlock => true;
        public static float FullSlotPromotionWeight => 3.5f;
        public static bool ResultNextGoalEnabled => true;
        public static string ResultNextGoalCopy => "Red Charger 10초 안에 격파";
        public static float SurroundedThreatRatio => 0.45f;
        public static int FirstRunFreeRerollCount => 1;
        public static float Boss1WarningTime => 1.0f;
        public static bool TutorialAssistEnabled => true;
        public static float CommanderHurtboxRadius => 0.35f;
        public static float CommanderPostHitInvuln => 0.25f;
        public static float ContactDamageSourceCooldown => 0.25f;
        public static float CompanionPostHitCooldown => 0.6f;
        public static float CompanionHpScale => 1.25f;
        public static float CompanionSpawnProtection => 1.2f;
        public static float ArcherSpawnProtection => 1.5f;
        public static float BossSpawnSeconds => s_data?.RunTuning.BossSpawnSeconds ?? 300.0f;
        public static float GuardWallCooldown => s_data?.GetSynergy("guard_squad")?.Cooldown ?? 12.0f;
        public static float GuardCompanionDamageReduction => 0.25f;
        public static float GuardCompanionDamageReductionDuration => 6.0f;
    }
}