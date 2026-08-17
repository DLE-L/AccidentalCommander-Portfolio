namespace Lizzo.PV.Data
{
    public sealed partial class LocalDataProvider
    {
        void SeedFallbackSynergyCombatCatalog()
        {
            AddSynergyDamage(new SynergyDamageData("DMG_SYNERGY_GUARD_01", "synergy_guard_shockwave", 18.0f, 12.0f, 0.0f, 0.0f, 0.0f, 3.5f, 120.0f, 6, 0, 0, 0, 0, 0.01f, 0.0f, 0.0f, 0.0f, false, false, false, false, false, false, false, false, "sector", "nearest", "per_cast_max_hp_cap", string.Empty, "battle_end", "rc_synergy_guard_damage"));
            AddSynergyDamage(new SynergyDamageData("DMG_BUILD1_GUARD_READY_01", "synergy_guard_shockwave", 0.0f, 15.0f, 0.0f, 0.0f, 0.0f, 2.35f, 85.0f, 3, 0, 0, 0, 0, 0.0f, 0.5f, 0.0f, 0.0f, false, false, false, false, false, false, false, false, "cone", "frontal", "no_damage", string.Empty, "battle_end", "rc_build1_guard_ready"));
            AddSynergyDamage(new SynergyDamageData("DMG_SYNERGY_ARCHER_01", "synergy_archer_rain", 14.0f, 8.0f, 0.0f, 0.0f, 0.0f, 2.0f, 0.0f, 8, 0, 0, 0, 0, 0.0f, 0.0f, 0.8f, 2.0f, false, false, false, false, false, false, false, false, "circle", "strongest_only", "normal", string.Empty, string.Empty, "rc_synergy_archer_damage"));
            AddSynergyDamage(new SynergyDamageData("DMG_SYNERGY_MAGIC_01", "synergy_magic_chain", 10.0f, 0.0f, 0.0f, 0.0f, 2.0f, 0.0f, 0.0f, 5, 5, 3, 0, 0, 0.0025f, 0.0f, 0.0f, 0.0f, true, true, false, false, false, false, false, false, "projectile", "same_target_duplicates_allowed", "per_projectile_max_hp_cap", string.Empty, string.Empty, "rc_synergy_magic_damage"));
            AddSynergyDamage(new SynergyDamageData("DMG_SYNERGY_EXPLOSION_01", "synergy_explosion_chain", 20.0f, 0.0f, 0.0f, 0.0f, 0.0f, 1.8f, 0.0f, 8, 0, 0, 8, 1, 0.008f, 0.0f, 0.0f, 0.0f, false, false, true, true, false, false, false, false, "circle", "death_origin", "per_cast_max_hp_cap", string.Empty, string.Empty, "rc_synergy_explosion_damage"));
            AddSynergyDamage(new SynergyDamageData("DMG_BUILD1_EXPLOSIVE_READY_01", "synergy_explosion_chain", 10.0f, 0.0f, 0.0f, 0.0f, 0.0f, 1.6f, 0.0f, 6, 0, 0, 12, 0, 0.008f, 0.0f, 0.0f, 0.0f, false, false, true, false, false, false, false, false, "circle", "lethal_origin", "per_cast_max_hp_cap", string.Empty, "battle_end", "rc_build1_explosive_ready"));
            AddSynergyDamage(new SynergyDamageData("DMG_SYNERGY_BEAST_01", "synergy_beast_hunt", 8.0f, 10.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 1, 0, 0, 0, 0, 0.004f, 0.0f, 0.0f, 0.0f, false, false, false, false, true, true, true, false, "direct", "common_target", "per_caster_max_hp_cap", string.Empty, string.Empty, "rc_synergy_beast_damage"));
            AddSynergyDamage(new SynergyDamageData("DOT_SYNERGY_BEAST_01", "synergy_beast_hunt", 3.0f, 0.0f, 1.0f, 5.0f, 0.0f, 0.0f, 0.0f, 1, 0, 0, 0, 0, 0.001f, 0.0f, 0.0f, 0.0f, false, false, false, false, false, false, false, true, "dot", "boss_target", "per_tick_max_hp_cap", "max_one_refresh_duration", string.Empty, "rc_synergy_beast_bleed"));

            AddSynergyEffect(new SynergyEffectData("EFFECT_SYNERGY_GUARD_DR", "synergy_guard_shockwave", 0.75f, 0.25f, 0.0f, 6.0f, 0.0f, 0.0f, 0.0f, 0.60f, false, true, true, true, false, true, false, false, false, false, "on_cast", "same_source_refresh_no_numeric_stack", "rc_guard_companion_dr"));
            AddSynergyEffect(new SynergyEffectData("EFFECT_HEALING_BOND_DR", "synergy_healing_bond", 0.8f, 0.2f, 0.0f, 3.0f, 2.5f, 0.0f, 0.0f, 0.0f, true, false, true, true, false, false, false, true, true, true, "zone_membership", "new_replaces_old_no_stack", "rc_healing_bond_dr"));
            AddSynergyEffect(new SynergyEffectData("EFFECT_MIXED_COMMAND", "synergy_mixed_command", 0.0f, 0.0f, 15.0f, 5.0f, 0.0f, 1.15f, 1.15f, 0.0f, false, true, true, false, true, true, false, false, false, false, "all_alive_companions", "same_source_refresh_no_multiplier_stack", "rc_mixed_command_multiplier"));
            AddSynergyEffect(new SynergyEffectData("EFFECT_BUILD1_MIXED_READY_01", "synergy_mixed_command", 0.0f, 0.0f, 18.0f, 3.0f, 0.0f, 1.0f, 1.12f, 0.0f, false, true, true, true, true, true, false, false, false, false, "all_living_companions", "same_source_refresh_no_multiplier_stack", "rc_build1_mixed_ready"));

            AddSynergySummon(new SynergySummonData("UNIT_SYNERGY_SKELETON_01", "synergy_undead_summon", 22, 5, 1.2f, 1.0f, 2.8f, 0.2f, "battle_end_or_hp0", CombatTargetRule.Nearest, 5, 3, 1, "summon_object,companion_tag=false,no_family_tag", "battle_end", "rc_synergy_skeleton_stats", "UNIT_PERSONAL_SKELETON_01"));
        }
    }
}
