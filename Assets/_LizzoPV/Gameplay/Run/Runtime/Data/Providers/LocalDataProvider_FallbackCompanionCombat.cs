namespace Lizzo.PV.Data
{
    public sealed partial class LocalDataProvider
    {
        void SeedFallbackCompanionCombatProfiles()
        {
            AddFallbackCompanionCombatProfile("shield_guard", 80, 2.8f, "skill_shield_bash", "dmg_shield_bash_v1", null, null, "shield_captain", null);
            AddFallbackCompanionCombatProfile("sword_soldier", 65, 3.0f, "skill_sword_slash", "dmg_sword_slash_v1", null, null, "sword_captain", null, promotedActionEffectIds: "dmg_sword_captain_turn_slash_v1", promotionOnMemberTurn: true);
            AddFallbackCompanionCombatProfile("cleric", 55, 2.7f, "skill_cleric_bolt", "dmg_cleric_bolt_v1", "skill_cleric_heal", "heal_cleric_v1", "light_guide", null);
            AddFallbackCompanionCombatProfile("falcon_archer", 45, 2.9f, "skill_falcon_arrow", "dmg_falcon_arrow_v1", null, null, "falcon_captain", null);
            AddFallbackCompanionCombatProfile("field_herbalist", 50, 2.8f, "skill_herbal_dart", "dmg_herbal_dart_v1", null, null, "battle_apothecary", null);
            AddFallbackCompanionCombatProfile("bombardier", 50, 2.7f, "skill_bomb_throw", "dmg_bomb_explosion_v1", null, null, "powder_captain", null);
            AddFallbackCompanionCombatProfile("fire_mage", 45, 2.6f, "skill_fire_field", "dot_fire_field_v1", null, null, "fire_sage", null);
            AddFallbackCompanionCombatProfile("lightning_mage", 45, 2.7f, "skill_chain_lightning", "dmg_chain_lightning_v1", null, null, "storm_mage", null);
            AddFallbackCompanionCombatProfile("wolf_tamer", 55, 3.0f, "skill_wolf_assault", "dmg_wolf_assault_v1", null, null, "beast_commander", "wolf_proxy_non_squad_non_tag");
            AddFallbackCompanionCombatProfile("wraith_knight", 120, 2.6f, "skill_wraith_slash", "dmg_wraith_slash_v1", null, null, "wraith_guardian", null);
            AddFallbackCompanionCombatProfile("necromancer", 50, 2.5f, "skill_curse_bolt", "dmg_curse_bolt_v1", null, null, "dark_ritualist", null);
            AddFallbackCompanionCombatProfile("skeleton_scythe_thrower", 40, 2.6f, "skill_skeleton_scythe_throw", "dmg_skeleton_scythe_throw_v1", null, null, "skeleton_reaper", null);
        }

        void SeedFallbackCombatEffects()
        {
            AddFallbackCombatEffect("dmg_shield_bash_v1", "shield_guard", "skill_shield_bash", CombatEffectKind.Damage, CombatDeliveryKind.Cone, 6.0f, 1.4f, 0.0f, 0.0f, 0.0f, 1.2f, 0.0f, 60.0f, 0.0f, 3, 0.0f, 0.35f, 0, 0, CombatTargetRule.CommanderThreat, "shield_bash", pushNormalEnemiesOnly: true, closeDamageRadius: 2.0f, baseMotion: CompanionSourceMotionKind.Excursion, promotedMotion: CompanionSourceMotionKind.Excursion, actionDurationSeconds: 0.12f, motionSpeed: 7.5f, excursionStandOffDistance: 0.85f, excursionLateralOffset: 0.22f);
            AddFallbackCombatEffect("dmg_sword_captain_turn_slash_v1", "sword_soldier", "skill_sword_captain_turn_slash", CombatEffectKind.Damage, CombatDeliveryKind.Cone, 12f, 1f, 0f, 0f, 0f, 2.1f, 0f, 75f, 0f, 0, 0f, 0f, 0, 0, CombatTargetRule.DensestCluster, "member_turn_power_slash", affectsAllTargetsInShape: true, baseMotion: CompanionSourceMotionKind.Excursion, promotedMotion: CompanionSourceMotionKind.Excursion, actionDurationSeconds: .22f, recoverySeconds: .24f, motionSpeed: 7.5f, excursionStandOffDistance: 1.35f, excursionLateralOffset: .30f, basePresentationCueId: "sword_captain_turn_slash", promotedPresentationCueId: "sword_captain_turn_slash");
            AddFallbackCombatEffect("dmg_sword_slash_v1", "sword_soldier", "skill_sword_slash", CombatEffectKind.Damage, CombatDeliveryKind.Cone, 12.0f, 1.0f, 0.0f, 0.0f, 0.0f, 1.6f, 0.0f, 60.0f, 0.0f, 5, 0.0f, 0.0f, 0, 0, CombatTargetRule.DensestCluster, "sword_slash", baseMotion: CompanionSourceMotionKind.Excursion, actionDurationSeconds: 0.18f, recoverySeconds: .18f, motionSpeed: 7.5f, excursionStandOffDistance: 1.35f, excursionLateralOffset: 0.30f, promotedPresentationCueId: "traveling-forward", omitPromotedSecondaryEffect: true);
            AddFallbackCombatEffect("dmg_cleric_bolt_v1", "cleric", "skill_cleric_bolt", CombatEffectKind.Damage, CombatDeliveryKind.Projectile, 5.0f, 1.6f, 0.0f, 0.0f, 0.0f, 4.5f, 0.22f, 0.0f, 0.0f, 1, 0.0f, 0.0f, 0, 0, CombatTargetRule.Targeted, "cleric_bolt", motionSpeed: 7f);
            AddFallbackCombatEffect("heal_cleric_v1", "cleric", "skill_cleric_heal", CombatEffectKind.Heal, CombatDeliveryKind.Projectile, 8.0f, 4.0f, 0.0f, 0.0f, 0.0f, 4.0f, 0.0f, 0.0f, 0.0f, 1, 0.0f, 0.0f, 0, 0, CombatTargetRule.Self, "returning_light_commander_heal");
            AddFallbackCombatEffect("dmg_falcon_arrow_v1", "falcon_archer", "skill_falcon_arrow", CombatEffectKind.Damage, CombatDeliveryKind.Projectile, 9.0f, 0.9f, 0.0f, 0.0f, 0.8f, 5.5f, 0.0f, 0.0f, 0.0f, 3, 0.0f, 0.0f, 0, 0, CombatTargetRule.Nearest, "piercing_arrow", repeatInterval: .12f);
            AddFallbackCombatEffect("dmg_shield_captain_shockwave_v1", "shield_guard", "skill_shield_captain_shockwave", CombatEffectKind.Damage, CombatDeliveryKind.Circle, 1.0f, 6.0f, 0.0f, 0.0f, 0.0f, 0.0f, 1.8f, 0.0f, 0.0f, 8, 0.0f, 0.8f, 0, 0, CombatTargetRule.CommanderThreat, "shield_captain_shockwave");
            AddFallbackCombatEffect("dmg_sword_captain_crescent_v1", "sword_soldier", "skill_sword_captain_crescent", CombatEffectKind.Damage, CombatDeliveryKind.Projectile, 1.0f, 0.0f, 0.0f, 0.0f, 0.85f, 5.0f, 0.75f, 0.0f, 0.0f, 0, 0.0f, 0.0f, 3, 0, CombatTargetRule.Targeted, "sword_captain_crescent", promotionEvent: CompanionPromotionEvent.BasicAttack);
            AddFallbackCombatEffect("buff_light_guide_sanctuary_v1", "cleric", "skill_light_guide_sanctuary", CombatEffectKind.AttackSpeed, CombatDeliveryKind.Field, 1.25f, 0.0f, 0.0f, 4.0f, 0.0f, 0.0f, 2.5f, 0.0f, 0.0f, 8, 0.0f, 0.0f, 6, 0, CombatTargetRule.Self, "light_guide_sanctuary", promotionEvent: CompanionPromotionEvent.ReturningLightResolved);
            AddFallbackCombatEffect("dmg_falcon_captain_dive_v1", "falcon_archer", "skill_falcon_captain_dive", CombatEffectKind.Damage, CombatDeliveryKind.Proxy, 3.0f, 0.0f, 0.0f, 0.0f, 1.0f, 5.5f, 0.0f, 0.0f, 0.0f, 1, 0.0f, 0.0f, 3, 0, CombatTargetRule.BossEliteHighestHealth, "falcon_captain_dive", promotionEvent: CompanionPromotionEvent.BasicAttack, motionSpeed: 18f);
            AddFallbackCombatEffect("status_battle_apothecary_spread_v1", "field_herbalist", "skill_battle_apothecary_spread", CombatEffectKind.Status, CombatDeliveryKind.Circle, 1.2f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 2.0f, 0.0f, 0.0f, 4, 0.0f, 0.0f, 1, 1, CombatTargetRule.Targeted, "battle_apothecary_vulnerability_spread", statusKind: CompanionEnemyStatusKind.Vulnerable, statusMagnitude: 1.2f, statusDuration: 3.0f);
            AddFallbackCombatEffect("dmg_powder_captain_cluster_v1", "bombardier", "skill_powder_captain_cluster", CombatEffectKind.Damage, CombatDeliveryKind.Circle, 1.0f, 0.0f, 0.0f, 0.0f, 0.0f, 5.0f, 0.9f, 0.0f, 0.6f, 6, 0.0f, 0.0f, 9, 4, CombatTargetRule.DensestCluster, "powder_captain_cluster_bomb", promotionEvent: CompanionPromotionEvent.BasicAttack, centerDamageRadiusRatio: .5f, secondaryDamageMultiplier: .5f);
            AddFallbackCombatEffect("dmg_fire_sage_ignition_v1", "fire_mage", "skill_fire_sage_ignition", CombatEffectKind.Damage, CombatDeliveryKind.Field, 1.0f, 0.0f, 0.0f, 1.5f, 0.0f, 4.8f, 0.0f, 0.0f, 0.0f, 8, 0.0f, 0.0f, 3, 2, CombatTargetRule.Targeted, "fire_sage_active_field_ignition", promotionEvent: CompanionPromotionEvent.BasicAttack);
            AddFallbackCombatEffect("dmg_storm_mage_overload_v1", "lightning_mage", "skill_storm_mage_overload", CombatEffectKind.Damage, CombatDeliveryKind.Circle, 1.0f, 0.0f, 0.0f, 0.0f, 0.0f, 5.0f, 0.6f, 0.0f, 0.0f, 3, 0.0f, 0.0f, 3, 0, CombatTargetRule.Targeted, "storm_mage_shock_overload", statusKind: CompanionEnemyStatusKind.Shock, statusMagnitude: 0.75f, statusDuration: 2.0f, statusTargetLimit: 1, promotionEvent: CompanionPromotionEvent.BasicAttack);
            AddFallbackCombatEffect("dmg_beast_commander_pack_assault_v1", "wolf_tamer", "skill_beast_commander_pack_assault", CombatEffectKind.Damage, CombatDeliveryKind.Proxy, 1.0f, 0.0f, 0.0f, .12f, 0.0f, 4.0f, .45f, 0.0f, 0.0f, 1, 0.0f, 0.0f, 3, 3, CombatTargetRule.HighestHealth, "beast_commander_pack_assault", promotionEvent: CompanionPromotionEvent.CountableKill, repeatInterval: .12f);
            AddFallbackCombatEffect("dmg_wraith_guardian_patrol_v1", "wraith_knight", "skill_wraith_guardian_patrol", CombatEffectKind.Damage, CombatDeliveryKind.Circle, 1.0f, 0.0f, 0.0f, 1.2f, 0.0f, 1.5f, 0.3f, 0.0f, 0.0f, 6, 0.0f, 0.0f, 3, 0, CombatTargetRule.Self, "wraith_guardian_orbit_patrol", statusKind: CompanionEnemyStatusKind.Weakening, statusMagnitude: 0.7f, statusDuration: 3.0f, promotionEvent: CompanionPromotionEvent.BasicAttack);
            AddFallbackCombatEffect("summon_dark_ritualist_group_v1", "necromancer", "skill_dark_ritualist_ritual", CombatEffectKind.Damage, CombatDeliveryKind.Proxy, 1.0f, 0.0f, 0.0f, 6.0f, 0.0f, 5.0f, 0.0f, 0.0f, 0.0f, 3, 0.0f, 0.0f, 3, 1, CombatTargetRule.Self, "dark_ritualist_undead_ritual", promotionEvent: CompanionPromotionEvent.CursedDeath);
            AddFallbackCombatEffect("dmg_skeleton_reaper_orbit_v1", "skeleton_scythe_thrower", "skill_skeleton_reaper_orbit", CombatEffectKind.Damage, CombatDeliveryKind.Circle, 1.0f, 0.0f, 0.0f, 1.2f, 0.0f, 2.0f, 0.375f, 0.0f, 0.0f, 8, 0.0f, 0.0f, 3, 0, CombatTargetRule.Self, "skeleton_reaper_orbit_scythe", promotionEvent: CompanionPromotionEvent.ReturningAttackResolved);
            AddFallbackCombatEffect("dmg_herbal_dart_v1", "field_herbalist", "skill_herbal_dart", CombatEffectKind.Damage, CombatDeliveryKind.Circle, 8.0f, 1.4f, 0.0f, 0.0f, 0.0f, 5.0f, 1.2f, 0.0f, 0.0f, 4, 0.25f, 0.0f, 0, 0, CombatTargetRule.Targeted, "vulnerability_flask", statusKind: CompanionEnemyStatusKind.Vulnerable, statusMagnitude: 1.2f, statusDuration: 3.0f, basePresentationCueId: "dmg_herbal_dart_v1", promotedPresentationCueId: "dmg_herbal_dart_v1");
            AddFallbackCombatEffect("dmg_bomb_explosion_v1", "bombardier", "skill_bomb_throw", CombatEffectKind.Damage, CombatDeliveryKind.Circle, 16.0f, 2.2f, 0.0f, 0.0f, 0.0f, 5.0f, 0.8f, 0.0f, 0.0f, 6, 0.5f, 0.0f, 0, 0, CombatTargetRule.DensestCluster, "no_same_frame_recursion", basePresentationCueId: "bombardier_payload", promotedPresentationCueId: "bombardier_payload", repeatInterval: .12f, centerDamageRadiusRatio: .5f, fragmentPresentationId: "bomb_fragment", fragmentDamageMultiplier: 1f/3f, fragmentSpeed: 7f, fragmentLifetime: .225f);
            AddFallbackCombatEffect("dot_fire_field_v1", "fire_mage", "skill_fire_field", CombatEffectKind.DamageOverTime, CombatDeliveryKind.Field, 5.0f, 3.2f, 1.0f, 3.0f, 0.0f, 4.8f, 0.8f, 0.0f, 0.0f, 8, 0.0f, 0.0f, 0, 2, CombatTargetRule.Targeted, "replace_oldest_field");
            AddFallbackCombatEffect("dmg_chain_lightning_v1", "lightning_mage", "skill_chain_lightning", CombatEffectKind.Damage, CombatDeliveryKind.Chain, 12.0f, 2.6f, 0.0f, 0.0f, 0.0f, 5.0f, 0.0f, 0.0f, 1.8f, 3, 0.0f, 0.0f, 0, 0, CombatTargetRule.Targeted, "first_target_shock", statusKind: CompanionEnemyStatusKind.Shock, statusMagnitude: 0.75f, statusDuration: 2.0f, damageRetentionPerTarget: 0.75f, statusTargetLimit: 1);
            AddFallbackCombatEffect("dmg_wolf_assault_v1", "wolf_tamer", "skill_wolf_assault", CombatEffectKind.Damage, CombatDeliveryKind.Proxy, 10.0f, 4.0f, 0.0f, 0.8f, 1.2f, 4.0f, 2.0f, 0.0f, 0.0f, 1, 0.0f, 0.0f, 3, 1, CombatTargetRule.LowestHealth, "lowest_hp_execution_chain", motionSpeed: 10f);
            AddFallbackCombatEffect("dmg_wraith_slash_v1", "wraith_knight", "skill_wraith_slash", CombatEffectKind.Damage, CombatDeliveryKind.Cone, 14.0f, 1.4f, 0.0f, 0.0f, 0.0f, 0.9f, 0.0f, 60.0f, 0.0f, 3, 0.0f, 0.0f, 0, 0, CombatTargetRule.CommanderThreat, "commander_guard_weakening_slash", statusKind: CompanionEnemyStatusKind.Weakening, statusMagnitude: 0.7f, statusDuration: 3.0f, baseMotion: CompanionSourceMotionKind.Excursion, promotedMotion: CompanionSourceMotionKind.Excursion, actionDurationSeconds: 0.25f, recoverySeconds: .2f, motionSpeed: 4.5f, excursionStandOffDistance: 0.65f, excursionLateralOffset: 0.15f);
            AddFallbackCombatEffect("dmg_curse_bolt_v1", "necromancer", "skill_curse_bolt", CombatEffectKind.Damage, CombatDeliveryKind.Projectile, 8.0f, 3.0f, 0.0f, 0.0f, 0.0f, 5.0f, 1.0f, 0.0f, 0.0f, 1, 0.0f, 0.8f, 0, 0, CombatTargetRule.Nearest, "curse_death_single_pull", statusKind: CompanionEnemyStatusKind.Curse, statusMagnitude: 1.0f, statusDuration: 4.0f);
            AddFallbackCombatEffect("dmg_skeleton_scythe_throw_v1", "skeleton_scythe_thrower", "skill_skeleton_scythe_throw", CombatEffectKind.Damage, CombatDeliveryKind.ReturningProjectile, 15.0f, 2.4f, 0.0f, 1.0f, 0.0f, 4.8f, 0.75f, 0.0f, 0.0f, 0, 0.35f, 0.0f, 0, 0, CombatTargetRule.Targeted, "outbound_return_once_each", affectsAllTargetsInShape: true, fixedTravelDistance: 4.8f);
        }

        void SeedFallbackCompanionSummons()
        {
            CompanionSummonData data = new CompanionSummonData
            {
                Id = "UNIT_PERSONAL_SKELETON_01",
                OwnerUnitId = "necromancer",
                SkillId = "skill_dark_ritualist_ritual",
                Damage = 4,
                AttackInterval = 1.3f,
                Range = 1.0f,
                MoveSpeed = 2.7f,
                AiScanInterval = 0.2f,
                LifetimeRuleId = "timed_group",
                TargetRule = CombatTargetRule.Nearest,
                Tags = "summon_object,companion_tag=false,no_family_tag",
                BossRuleId = "normal_target",
                StackRuleId = "single_temporary_group",
                ResetRuleId = "battle_end",
            };
            _companionSummons.Add(data);
            _companionSummonsById.Add(data.Id, data);
        }

        void AddFallbackCompanionCombatProfile(string unitId, int baseHp, float moveSpeed, string basicSkillId, string basicEffectId, string secondarySkillId, string secondaryEffectId, string promotionProfileId, string secondaryRuleId, string promotedActionEffectIds = null, bool promotionOnMemberTurn = false)
        {
            CompanionCombatProfileData data = new CompanionCombatProfileData
            {
                UnitId = unitId,
                PromotionOnMemberTurn = promotionOnMemberTurn,
                BaseHp = baseHp,
                MoveSpeed = moveSpeed,
                BasicSkillId = basicSkillId,
                BasicEffectId = basicEffectId,
                BaseActionEffectIds = basicEffectId,
                PromotedActionEffectIds = promotedActionEffectIds ?? basicEffectId,
                SecondarySkillId = secondarySkillId ?? string.Empty,
                SecondaryEffectId = secondaryEffectId ?? string.Empty,
                PromotionProfileId = promotionProfileId,
                DownDurationSeconds = 4.0f,
                RecoverHpPercent = 0.30f,
                Count2EffectMultiplier = 1.60f,
                Count2HpMultiplier = 1.45f,
                NoTargetRetrySeconds = 0.15f,
                Count3RuleId = "promotion_profile_only",
                SecondaryRuleId = secondaryRuleId ?? string.Empty,
            };
            _companionCombatProfiles.Add(data);
            _companionCombatProfilesByUnitId.Add(unitId, data);
        }

        void AddFallbackCombatEffect(string id, string ownerUnitId, string skillId, CombatEffectKind effectKind, CombatDeliveryKind deliveryKind, float baseValue, float castInterval, float tickInterval, float duration, float projectileLifetime, float range, float radius, float angle, float chainDistance, int maxTargets, float castDelay, float push, int triggerCount, int maxActiveCount, CombatTargetRule targetRule, string ruleId, bool affectsAllTargetsInShape = false, CompanionEnemyStatusKind statusKind = CompanionEnemyStatusKind.None, float statusMagnitude = 0.0f, float statusDuration = 0.0f, CompanionSourceMotionKind baseMotion = CompanionSourceMotionKind.Stationary, CompanionSourceMotionKind promotedMotion = CompanionSourceMotionKind.Stationary, float actionDurationSeconds = 0.0f, float motionSpeed = 0.0f, float excursionStandOffDistance = 0.0f, float excursionLateralOffset = 0.0f, string basePresentationCueId = "", string promotedPresentationCueId = "", bool omitPromotedSecondaryEffect = false, float closeDamageRadius = 0.0f, float damageRetentionPerTarget = 1.0f, int statusTargetLimit = 0, float minimumTravelDistance = 0.0f, CompanionPromotionEvent promotionEvent = CompanionPromotionEvent.None, bool pushNormalEnemiesOnly = false, float repeatInterval = 0f, float centerDamageRadiusRatio = 0f, float secondaryDamageMultiplier = 1f, string fragmentPresentationId = "", float fragmentDamageMultiplier = 0f, float fragmentSpeed = 0f, float fragmentLifetime = 0f, float recoverySeconds = 0f, float fixedTravelDistance = 0f)
        {
            CombatEffectData data = new CombatEffectData
            {
                Id = id,
                OwnerUnitId = ownerUnitId,
                SkillId = skillId,
                EffectKind = effectKind,
                DeliveryKind = deliveryKind,
                BaseValue = baseValue,
                CastInterval = castInterval,
                TickInterval = tickInterval,
                Duration = duration,
                ProjectileLifetime = projectileLifetime,
                RepeatInterval = repeatInterval,
                CenterDamageRadiusRatio = centerDamageRadiusRatio, SecondaryDamageMultiplier = secondaryDamageMultiplier,
                FragmentPresentationId = fragmentPresentationId, FragmentDamageMultiplier = fragmentDamageMultiplier,
                FragmentSpeed = fragmentSpeed, FragmentLifetime = fragmentLifetime,
                MinimumTravelDistance = minimumTravelDistance,
                FixedTravelDistance = fixedTravelDistance,
                Range = range,
                Radius = radius,
                Angle = angle,
                ChainDistance = chainDistance,
                MaxTargets = maxTargets,
                AffectsAllTargetsInShape = affectsAllTargetsInShape,
                CastDelay = castDelay,
                Push = push,
                PushNormalEnemiesOnly = pushNormalEnemiesOnly,
                TriggerCount = triggerCount,
                PromotionEvent = promotionEvent,
                MaxActiveCount = maxActiveCount,
                TargetRule = targetRule,
                StatusKind = statusKind,
                StatusMagnitude = statusMagnitude,
                StatusDuration = statusDuration,
                BaseMotion = baseMotion,
                PromotedMotion = promotedMotion,
                ActionDurationSeconds = actionDurationSeconds,
                RecoverySeconds = recoverySeconds,
                MotionSpeed = motionSpeed,
                ExcursionStandOffDistance = excursionStandOffDistance,
                ExcursionLateralOffset = excursionLateralOffset,
                BasePresentationCueId = basePresentationCueId,
                PromotedPresentationCueId = promotedPresentationCueId,
                OmitPromotedSecondaryEffect = omitPromotedSecondaryEffect,
                CloseDamageRadius = closeDamageRadius,
                DamageRetentionPerTarget = damageRetentionPerTarget,
                StatusTargetLimit = statusTargetLimit,
                RuleId = ruleId,
            };
            _combatEffects.Add(data);
            _combatEffectsById.Add(id, data);
        }
    }
}
