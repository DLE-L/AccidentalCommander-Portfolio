namespace Lizzo.PV.Data
{
    public sealed partial class LocalDataProvider
    {
        void SeedFallbackCompanionCombatProfiles()
        {
            AddFallbackCompanionCombatProfile("shield_guard", 80, 2.8f, "skill_shield_bash", "dmg_shield_bash_v1", null, null, "shield_captain", null);
            AddFallbackCompanionCombatProfile("sword_soldier", 65, 3.0f, "skill_sword_slash", "dmg_sword_slash_v1", null, null, "sword_captain", null);
            AddFallbackCompanionCombatProfile("cleric", 55, 2.7f, "skill_cleric_bolt", "dmg_cleric_bolt_v1", "skill_cleric_heal", "heal_cleric_v1", "light_guide", null);
            AddFallbackCompanionCombatProfile("falcon_archer", 45, 2.9f, "skill_falcon_arrow", "dmg_falcon_arrow_v1", "skill_falcon_assist", "dmg_falcon_assist_v1", "falcon_captain", "falcon_visual_proxy_non_squad");
            AddFallbackCompanionCombatProfile("field_herbalist", 50, 2.8f, "skill_herbal_dart", "dmg_herbal_dart_v1", null, null, "battle_apothecary", null);
            AddFallbackCompanionCombatProfile("bombardier", 50, 2.7f, "skill_bomb_throw", "dmg_bomb_explosion_v1", null, null, "powder_captain", null);
            AddFallbackCompanionCombatProfile("fire_mage", 45, 2.6f, "skill_fire_field", "dot_fire_field_v1", null, null, "fire_sage", null);
            AddFallbackCompanionCombatProfile("lightning_mage", 45, 2.7f, "skill_chain_lightning", "dmg_chain_lightning_v1", null, null, "storm_mage", null);
            AddFallbackCompanionCombatProfile("wolf_tamer", 55, 3.0f, "skill_wolf_assault", "dmg_wolf_assault_v1", null, null, "beast_commander", "wolf_proxy_non_squad_non_tag");
            AddFallbackCompanionCombatProfile("wraith_knight", 120, 2.6f, "skill_wraith_slash", "dmg_wraith_slash_v1", "skill_wraith_guard", "dr_wraith_guard_v1", "wraith_guardian", null);
            AddFallbackCompanionCombatProfile("necromancer", 50, 2.5f, "skill_curse_bolt", "dmg_curse_bolt_v1", null, null, "dark_ritualist", null);
            AddFallbackCompanionCombatProfile("skeleton_scythe_thrower", 40, 2.6f, "skill_skeleton_scythe_throw", "dmg_skeleton_scythe_throw_v1", null, null, "skeleton_reaper", null);
        }

        void SeedFallbackCombatEffects()
        {
            AddFallbackCombatEffect("dmg_shield_bash_v1", "shield_guard", "skill_shield_bash", CombatEffectKind.Damage, CombatDeliveryKind.Cone, 6.0f, 1.4f, 0.0f, 0.0f, 0.0f, 1.2f, 0.0f, 60.0f, 0.0f, 3, 0.0f, 0.5f, 0, 0, CombatTargetRule.CommanderThreat, "shield_bash");
            AddFallbackCombatEffect("dmg_sword_slash_v1", "sword_soldier", "skill_sword_slash", CombatEffectKind.Damage, CombatDeliveryKind.Cone, 12.0f, 1.0f, 0.0f, 0.0f, 0.0f, 1.6f, 0.0f, 60.0f, 0.0f, 5, 0.0f, 0.0f, 0, 0, CombatTargetRule.DensestCluster, "sword_slash");
            AddFallbackCombatEffect("dmg_cleric_bolt_v1", "cleric", "skill_cleric_bolt", CombatEffectKind.Damage, CombatDeliveryKind.Projectile, 5.0f, 1.6f, 0.0f, 0.0f, 0.0f, 4.5f, 0.0f, 0.0f, 0.0f, 1, 0.0f, 0.0f, 0, 0, CombatTargetRule.Targeted, "cleric_bolt");
            AddFallbackCombatEffect("heal_cleric_v1", "cleric", "skill_cleric_heal", CombatEffectKind.Heal, CombatDeliveryKind.Projectile, 8.0f, 4.0f, 0.0f, 0.0f, 0.0f, 4.0f, 0.0f, 0.0f, 0.0f, 1, 0.0f, 0.0f, 0, 0, CombatTargetRule.Self, "returning_light_commander_heal");
            AddFallbackCombatEffect("dmg_falcon_arrow_v1", "falcon_archer", "skill_falcon_arrow", CombatEffectKind.Damage, CombatDeliveryKind.Projectile, 9.0f, 0.9f, 0.0f, 0.0f, 0.8f, 5.5f, 0.0f, 0.0f, 0.0f, 3, 0.0f, 0.0f, 0, 0, CombatTargetRule.Nearest, "piercing_arrow");
            AddFallbackCombatEffect("dmg_falcon_assist_v1", "falcon_archer", "skill_falcon_assist", CombatEffectKind.Damage, CombatDeliveryKind.Proxy, 6.0f, 0.0f, 0.0f, 0.0f, 0.0f, 5.5f, 0.0f, 0.0f, 0.0f, 1, 0.0f, 0.0f, 4, 0, CombatTargetRule.Nearest, "falcon_visual_proxy_non_squad");
            AddFallbackCombatEffect("dmg_shield_captain_shockwave_v1", "shield_guard", "skill_shield_captain_shockwave", CombatEffectKind.Damage, CombatDeliveryKind.Circle, 1.0f, 6.0f, 0.0f, 0.0f, 0.0f, 0.0f, 2.5f, 0.0f, 0.0f, 8, 0.0f, 0.8f, 0, 0, CombatTargetRule.CommanderThreat, "shield_captain_shockwave");
            AddFallbackCombatEffect("dmg_sword_captain_crescent_v1", "sword_soldier", "skill_sword_captain_crescent", CombatEffectKind.Damage, CombatDeliveryKind.Projectile, 1.0f, 0.0f, 0.0f, 0.0f, 0.85f, 5.0f, 0.75f, 0.0f, 0.0f, 4, 0.0f, 0.0f, 3, 0, CombatTargetRule.Targeted, "sword_captain_crescent");
            AddFallbackCombatEffect("buff_light_guide_sanctuary_v1", "cleric", "skill_light_guide_sanctuary", CombatEffectKind.AttackSpeed, CombatDeliveryKind.Field, 1.25f, 0.0f, 0.0f, 4.0f, 0.0f, 0.0f, 2.5f, 0.0f, 0.0f, 8, 0.0f, 0.0f, 3, 0, CombatTargetRule.Self, "light_guide_sanctuary");
            AddFallbackCombatEffect("dmg_falcon_captain_dive_v1", "falcon_archer", "skill_falcon_captain_dive", CombatEffectKind.Damage, CombatDeliveryKind.Proxy, 1.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 1, 0.0f, 0.0f, 3, 0, CombatTargetRule.BossEliteHighestHealth, "falcon_captain_dive");
            AddFallbackCombatEffect("status_battle_apothecary_spread_v1", "field_herbalist", "skill_battle_apothecary_spread", CombatEffectKind.Status, CombatDeliveryKind.Circle, 1.2f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 2.0f, 0.0f, 0.0f, 4, 0.0f, 0.0f, 1, 1, CombatTargetRule.Targeted, "battle_apothecary_vulnerability_spread", statusKind: CompanionEnemyStatusKind.Vulnerable, statusMagnitude: 1.2f, statusDuration: 3.0f);
            AddFallbackCombatEffect("dmg_powder_captain_cluster_v1", "bombardier", "skill_powder_captain_cluster", CombatEffectKind.Damage, CombatDeliveryKind.Circle, 1.0f, 0.0f, 0.0f, 0.0f, 0.0f, 5.0f, 1.8f, 0.0f, 1.2f, 6, 0.0f, 0.0f, 3, 4, CombatTargetRule.DensestCluster, "powder_captain_cluster_bomb");
            AddFallbackCombatEffect("dmg_fire_sage_ignition_v1", "fire_mage", "skill_fire_sage_ignition", CombatEffectKind.Damage, CombatDeliveryKind.Field, 1.0f, 0.0f, 0.0f, 1.5f, 0.0f, 4.8f, 0.0f, 0.0f, 0.0f, 8, 0.0f, 0.0f, 3, 2, CombatTargetRule.Targeted, "fire_sage_active_field_ignition");
            AddFallbackCombatEffect("dmg_storm_mage_overload_v1", "lightning_mage", "skill_storm_mage_overload", CombatEffectKind.Damage, CombatDeliveryKind.Circle, 1.0f, 0.0f, 0.0f, 0.0f, 0.0f, 5.0f, 1.2f, 0.0f, 0.0f, 3, 0.0f, 0.0f, 3, 0, CombatTargetRule.Targeted, "storm_mage_shock_overload", statusKind: CompanionEnemyStatusKind.Shock, statusMagnitude: 0.75f, statusDuration: 2.0f);
            AddFallbackCombatEffect("dmg_beast_commander_pack_assault_v1", "wolf_tamer", "skill_beast_commander_pack_assault", CombatEffectKind.Damage, CombatDeliveryKind.Proxy, 1.0f, 0.0f, 0.0f, 0.0f, 0.0f, 4.0f, 0.0f, 0.0f, 0.0f, 1, 0.0f, 0.0f, 3, 3, CombatTargetRule.HighestHealth, "beast_commander_pack_assault");
            AddFallbackCombatEffect("dmg_wraith_guardian_patrol_v1", "wraith_knight", "skill_wraith_guardian_patrol", CombatEffectKind.Damage, CombatDeliveryKind.Circle, 1.0f, 0.0f, 0.0f, 0.0f, 0.0f, 3.0f, 0.6f, 0.0f, 0.0f, 6, 0.0f, 0.0f, 3, 0, CombatTargetRule.Self, "wraith_guardian_orbit_patrol", statusKind: CompanionEnemyStatusKind.Weakening, statusMagnitude: 0.7f, statusDuration: 3.0f);
            AddFallbackCombatEffect("summon_dark_ritualist_group_v1", "necromancer", "skill_dark_ritualist_ritual", CombatEffectKind.Damage, CombatDeliveryKind.Proxy, 1.0f, 0.0f, 0.0f, 6.0f, 0.0f, 5.0f, 0.0f, 0.0f, 0.0f, 3, 0.0f, 0.0f, 3, 1, CombatTargetRule.Self, "dark_ritualist_undead_ritual");
            AddFallbackCombatEffect("dmg_skeleton_reaper_orbit_v1", "skeleton_scythe_thrower", "skill_skeleton_reaper_orbit", CombatEffectKind.Damage, CombatDeliveryKind.Circle, 1.0f, 0.0f, 0.0f, 0.0f, 0.0f, 4.0f, 0.75f, 0.0f, 0.0f, 8, 0.0f, 0.0f, 3, 0, CombatTargetRule.Self, "skeleton_reaper_orbit_scythe");
            AddFallbackCombatEffect("dmg_herbal_dart_v1", "field_herbalist", "skill_herbal_dart", CombatEffectKind.Damage, CombatDeliveryKind.Circle, 8.0f, 1.4f, 0.0f, 0.0f, 0.0f, 5.0f, 1.2f, 0.0f, 0.0f, 4, 0.25f, 0.0f, 0, 0, CombatTargetRule.Targeted, "vulnerability_flask", statusKind: CompanionEnemyStatusKind.Vulnerable, statusMagnitude: 1.2f, statusDuration: 3.0f);
            AddFallbackCombatEffect("dmg_bomb_explosion_v1", "bombardier", "skill_bomb_throw", CombatEffectKind.Damage, CombatDeliveryKind.Circle, 16.0f, 2.2f, 0.0f, 0.0f, 0.0f, 5.0f, 1.6f, 0.0f, 0.0f, 6, 0.5f, 0.0f, 0, 0, CombatTargetRule.DensestCluster, "no_same_frame_recursion");
            AddFallbackCombatEffect("dot_fire_field_v1", "fire_mage", "skill_fire_field", CombatEffectKind.DamageOverTime, CombatDeliveryKind.Field, 5.0f, 3.2f, 1.0f, 3.0f, 0.0f, 4.8f, 1.6f, 0.0f, 0.0f, 8, 0.0f, 0.0f, 0, 2, CombatTargetRule.Targeted, "replace_oldest_field");
            AddFallbackCombatEffect("dmg_chain_lightning_v1", "lightning_mage", "skill_chain_lightning", CombatEffectKind.Damage, CombatDeliveryKind.Chain, 12.0f, 2.6f, 0.0f, 0.0f, 0.0f, 5.0f, 0.0f, 0.0f, 1.8f, 3, 0.0f, 0.0f, 0, 0, CombatTargetRule.Targeted, "first_target_shock", statusKind: CompanionEnemyStatusKind.Shock, statusMagnitude: 0.75f, statusDuration: 2.0f);
            AddFallbackCombatEffect("dmg_wolf_assault_v1", "wolf_tamer", "skill_wolf_assault", CombatEffectKind.Damage, CombatDeliveryKind.Proxy, 10.0f, 4.0f, 0.0f, 0.8f, 0.0f, 4.0f, 2.0f, 0.0f, 0.0f, 1, 0.0f, 0.0f, 3, 1, CombatTargetRule.LowestHealth, "lowest_hp_execution_chain");
            AddFallbackCombatEffect("dmg_wraith_slash_v1", "wraith_knight", "skill_wraith_slash", CombatEffectKind.Damage, CombatDeliveryKind.Cone, 14.0f, 1.4f, 0.0f, 0.0f, 0.0f, 1.2f, 0.0f, 60.0f, 0.0f, 3, 0.0f, 0.0f, 0, 0, CombatTargetRule.CommanderThreat, "commander_guard_weakening_slash", statusKind: CompanionEnemyStatusKind.Weakening, statusMagnitude: 0.7f, statusDuration: 3.0f);
            AddFallbackCombatEffect("dr_wraith_guard_v1", "wraith_knight", "skill_wraith_guard", CombatEffectKind.DamageReduction, CombatDeliveryKind.Self, 0.60f, 5.0f, 0.0f, 1.2f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 1, 0.0f, 0.0f, 0, 0, CombatTargetRule.Self, "self_damage_multiplier");
            AddFallbackCombatEffect("dmg_curse_bolt_v1", "necromancer", "skill_curse_bolt", CombatEffectKind.Damage, CombatDeliveryKind.Projectile, 8.0f, 3.0f, 0.0f, 0.0f, 0.0f, 5.0f, 2.0f, 0.0f, 0.0f, 1, 0.0f, 0.8f, 0, 0, CombatTargetRule.Nearest, "curse_death_single_pull", statusKind: CompanionEnemyStatusKind.Curse, statusMagnitude: 1.0f, statusDuration: 4.0f);
            AddFallbackCombatEffect("dmg_skeleton_scythe_throw_v1", "skeleton_scythe_thrower", "skill_skeleton_scythe_throw", CombatEffectKind.Damage, CombatDeliveryKind.ReturningProjectile, 15.0f, 2.4f, 0.0f, 1.0f, 0.0f, 4.8f, 0.75f, 0.0f, 0.0f, 4, 0.0f, 0.0f, 0, 0, CombatTargetRule.Targeted, "outbound_return_once_each");
        }

        void SeedFallbackCompanionSummons()
        {
            CompanionSummonData data = new CompanionSummonData
            {
                Id = "UNIT_PERSONAL_SKELETON_01",
                OwnerUnitId = "necromancer",
                SkillId = "skill_dark_ritualist_ritual",
                Hp = 18,
                Damage = 4,
                AttackInterval = 1.3f,
                Range = 1.0f,
                MoveSpeed = 2.7f,
                AiScanInterval = 0.2f,
                LifetimeRuleId = "timed_group_or_hp0",
                TargetRule = CombatTargetRule.Nearest,
                Tags = "summon_object,companion_tag=false,no_family_tag",
                BossRuleId = "normal_target",
                StackRuleId = "single_temporary_group",
                ResetRuleId = "battle_end",
            };
            _companionSummons.Add(data);
            _companionSummonsById.Add(data.Id, data);
        }

        void AddFallbackCompanionCombatProfile(string unitId, int baseHp, float moveSpeed, string basicSkillId, string basicEffectId, string secondarySkillId, string secondaryEffectId, string promotionProfileId, string secondaryRuleId)
        {
            CompanionCombatProfileData data = new CompanionCombatProfileData
            {
                UnitId = unitId,
                BaseHp = baseHp,
                MoveSpeed = moveSpeed,
                BasicSkillId = basicSkillId,
                BasicEffectId = basicEffectId,
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

        void AddFallbackCombatEffect(string id, string ownerUnitId, string skillId, CombatEffectKind effectKind, CombatDeliveryKind deliveryKind, float baseValue, float castInterval, float tickInterval, float duration, float projectileLifetime, float range, float radius, float angle, float chainDistance, int maxTargets, float castDelay, float push, int triggerCount, int maxActiveCount, CombatTargetRule targetRule, string ruleId, bool affectsAllTargetsInShape = false, CompanionEnemyStatusKind statusKind = CompanionEnemyStatusKind.None, float statusMagnitude = 0.0f, float statusDuration = 0.0f)
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
                Range = range,
                Radius = radius,
                Angle = angle,
                ChainDistance = chainDistance,
                MaxTargets = maxTargets,
                AffectsAllTargetsInShape = affectsAllTargetsInShape,
                CastDelay = castDelay,
                Push = push,
                TriggerCount = triggerCount,
                MaxActiveCount = maxActiveCount,
                TargetRule = targetRule,
                StatusKind = statusKind,
                StatusMagnitude = statusMagnitude,
                StatusDuration = statusDuration,
                RuleId = ruleId,
            };
            _combatEffects.Add(data);
            _combatEffectsById.Add(id, data);
        }
    }
}
