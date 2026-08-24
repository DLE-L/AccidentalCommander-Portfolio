using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Lizzo.PV.Data
{
    public sealed partial class LocalDataProvider
    {
        readonly List<SynergyDamageData> _synergyDamages = new List<SynergyDamageData>();
        readonly Dictionary<string, SynergyDamageData> _synergyDamagesById = new Dictionary<string, SynergyDamageData>();
        readonly List<SynergyEffectData> _synergyEffects = new List<SynergyEffectData>();
        readonly Dictionary<string, SynergyEffectData> _synergyEffectsById = new Dictionary<string, SynergyEffectData>();
        readonly List<SynergySummonData> _synergySummons = new List<SynergySummonData>();
        readonly Dictionary<string, SynergySummonData> _synergySummonsById = new Dictionary<string, SynergySummonData>();
        readonly IReadOnlyList<SynergyDamageData> _synergyDamageView;
        readonly IReadOnlyList<SynergyEffectData> _synergyEffectView;
        readonly IReadOnlyList<SynergySummonData> _synergySummonView;

        const int RequiredSynergyDamageCount = 8;
        const int RequiredSynergyEffectCount = 4;
        const int RequiredSynergySummonCount = 1;

        public IReadOnlyList<SynergyDamageData> SynergyDamages
        {
            get { EnsureInitialized(); return _synergyDamageView; }
        }

        public IReadOnlyList<SynergyEffectData> SynergyEffects
        {
            get { EnsureInitialized(); return _synergyEffectView; }
        }

        public IReadOnlyList<SynergySummonData> SynergySummons
        {
            get { EnsureInitialized(); return _synergySummonView; }
        }

        public SynergyDamageData GetSynergyDamage(string damageId)
        {
            EnsureInitialized();
            return _synergyDamagesById.TryGetValue(damageId, out SynergyDamageData data) ? data : null;
        }

        public SynergyEffectData GetSynergyEffect(string effectId)
        {
            EnsureInitialized();
            return _synergyEffectsById.TryGetValue(effectId, out SynergyEffectData data) ? data : null;
        }

        public SynergySummonData GetSynergySummon(string summonId)
        {
            EnsureInitialized();
            return _synergySummonsById.TryGetValue(summonId, out SynergySummonData data) ? data : null;
        }

        void ResetSynergyCombatCatalog()
        {
            _synergyDamages.Clear();
            _synergyDamagesById.Clear();
            _synergyEffects.Clear();
            _synergyEffectsById.Clear();
            _synergySummons.Clear();
            _synergySummonsById.Clear();
        }

        void LoadSynergyCombatCatalog(XElement root)
        {
            LoadSynergyDamages(root.Element("SynergyDamageDatas"));
            LoadSynergyEffects(root.Element("SynergyEffectDatas"));
            LoadSynergySummons(root.Element("SynergySummonDatas"));
        }

        void LoadSynergyDamages(XElement parent)
        {
            if (parent == null)
            {
                AddCompanionCatalogValidationError("synergy_damage:missing_table");
                return;
            }

            foreach (XElement element in parent.Elements("SynergyDamageData"))
            {
                AddSynergyDamage(new SynergyDamageData(
                    StringAttr(element, "id", string.Empty), StringAttr(element, "synergyId", string.Empty),
                    FloatAttr(element, "baseValue", 0.0f), FloatAttr(element, "cadenceSeconds", 0.0f),
                    FloatAttr(element, "tickIntervalSeconds", 0.0f), FloatAttr(element, "durationSeconds", 0.0f),
                    FloatAttr(element, "projectileLifetimeSeconds", 0.0f), FloatAttr(element, "radius", 0.0f),
                    FloatAttr(element, "angle", 0.0f), IntAttr(element, "maxTargets", 0),
                    IntAttr(element, "projectileCount", 0), IntAttr(element, "actionCount", 0),
                    IntAttr(element, "triggerThreshold", 0), IntAttr(element, "frameCap", 0),
                    FloatAttr(element, "bossMaxHpPercent", 0.0f), FloatAttr(element, "push", 0.0f),
                    FloatAttr(element, "slowMultiplier", 0.0f), FloatAttr(element, "slowDurationSeconds", 0.0f),
                    BoolAttr(element, "sameTargetDuplicatesAllowed"), BoolAttr(element, "excludesSelfCounter"),
                    BoolAttr(element, "sameScopeRecursionBlocked"), BoolAttr(element, "overflowCarries"),
                    BoolAttr(element, "eachAliveParticipantOneHit"), BoolAttr(element, "targetDeathCancelsRemaining"),
                    BoolAttr(element, "bossNoStagger"), BoolAttr(element, "bleedImmuneExcluded"),
                    StringAttr(element, "deliveryRuleId", string.Empty), StringAttr(element, "targetRuleId", string.Empty),
                    StringAttr(element, "bossRuleId", string.Empty), StringAttr(element, "stackRuleId", string.Empty),
                    StringAttr(element, "resetRuleId", string.Empty), StringAttr(element, "remoteConfigKey", string.Empty)));
            }
        }

        void LoadSynergyEffects(XElement parent)
        {
            if (parent == null)
            {
                AddCompanionCatalogValidationError("synergy_effect:missing_table");
                return;
            }

            foreach (XElement element in parent.Elements("SynergyEffectData"))
            {
                AddSynergyEffect(new SynergyEffectData(
                    StringAttr(element, "id", string.Empty), StringAttr(element, "synergyId", string.Empty),
                    FloatAttr(element, "damageTakenMultiplier", 0.0f), FloatAttr(element, "damageReduction", 0.0f),
                    FloatAttr(element, "cadenceSeconds", 0.0f), FloatAttr(element, "durationSeconds", 0.0f),
                    FloatAttr(element, "radius", 0.0f), FloatAttr(element, "attackIntervalDivisor", 0.0f),
                    FloatAttr(element, "moveSpeedMultiplier", 0.0f), FloatAttr(element, "totalDamageReductionCap", 0.0f),
                    BoolAttr(element, "knockdownImmunity"), BoolAttr(element, "allAliveCompanions"),
                    BoolAttr(element, "companionsOnly"), BoolAttr(element, "commanderExcluded"),
                    BoolAttr(element, "excludesCompanionTagFalseSummons"), BoolAttr(element, "sameSourceRefresh"),
                    BoolAttr(element, "numericStackingAllowed"), BoolAttr(element, "zoneMembership"),
                    BoolAttr(element, "leaveRemoves"), BoolAttr(element, "newReplacesOld"),
                    StringAttr(element, "activationRuleId", string.Empty), StringAttr(element, "stackRuleId", string.Empty),
                    StringAttr(element, "remoteConfigKey", string.Empty)));
            }
        }

        void LoadSynergySummons(XElement parent)
        {
            if (parent == null)
            {
                AddCompanionCatalogValidationError("synergy_summon:missing_table");
                return;
            }

            foreach (XElement element in parent.Elements("SynergySummonData"))
            {
                AddSynergySummon(new SynergySummonData(
                    StringAttr(element, "id", string.Empty), StringAttr(element, "synergyId", string.Empty),
                    IntAttr(element, "hp", 0), IntAttr(element, "damage", 0), FloatAttr(element, "attackInterval", 0.0f),
                    FloatAttr(element, "range", 0.0f), FloatAttr(element, "moveSpeed", 0.0f), FloatAttr(element, "aiScanInterval", 0.0f),
                    StringAttr(element, "lifetimeRuleId", string.Empty), EnumAttr<CombatTargetRule>(element, "targetRule"),
                    IntAttr(element, "activeCap", 0), IntAttr(element, "bossLockCount", 0), IntAttr(element, "frameSpawnCap", 0),
                    StringAttr(element, "tags", string.Empty), StringAttr(element, "resetRuleId", string.Empty),
                    StringAttr(element, "remoteConfigKey", string.Empty), StringAttr(element, "distinctFromSummonId", string.Empty)));
            }
        }

        void AddSynergyDamage(SynergyDamageData data)
        {
            if (data == null || string.IsNullOrEmpty(data.Id))
            {
                AddCompanionCatalogValidationError("synergy_damage:missing_id");
                return;
            }
            if (_synergyDamagesById.ContainsKey(data.Id))
            {
                AddCompanionCatalogValidationError($"synergy_damage:duplicate:{data.Id}");
                return;
            }
            _synergyDamages.Add(data);
            _synergyDamagesById.Add(data.Id, data);
        }

        void AddSynergyEffect(SynergyEffectData data)
        {
            if (data == null || string.IsNullOrEmpty(data.Id))
            {
                AddCompanionCatalogValidationError("synergy_effect:missing_id");
                return;
            }
            if (_synergyEffectsById.ContainsKey(data.Id))
            {
                AddCompanionCatalogValidationError($"synergy_effect:duplicate:{data.Id}");
                return;
            }
            _synergyEffects.Add(data);
            _synergyEffectsById.Add(data.Id, data);
        }

        void AddSynergySummon(SynergySummonData data)
        {
            if (data == null || string.IsNullOrEmpty(data.Id))
            {
                AddCompanionCatalogValidationError("synergy_summon:missing_id");
                return;
            }
            if (_synergySummonsById.ContainsKey(data.Id))
            {
                AddCompanionCatalogValidationError($"synergy_summon:duplicate:{data.Id}");
                return;
            }
            _synergySummons.Add(data);
            _synergySummonsById.Add(data.Id, data);
        }

        void ValidateSynergyCombatCatalog(DataLoadResult result)
        {
            if (_synergyDamages.Count != RequiredSynergyDamageCount)
                AddMissingRequiredId(result, $"synergy_damage:count:{_synergyDamages.Count}");
            if (_synergyEffects.Count != RequiredSynergyEffectCount)
                AddMissingRequiredId(result, $"synergy_effect:count:{_synergyEffects.Count}");
            if (_synergySummons.Count != RequiredSynergySummonCount)
                AddMissingRequiredId(result, $"synergy_summon:count:{_synergySummons.Count}");

            ValidateSynergyDamage(result, "DMG_SYNERGY_GUARD_01", "synergy_guard_shockwave", 18.0f, 12.0f, 3.5f, 120.0f, 6, 0.01f, "sector", "per_cast_max_hp_cap", "battle_end", "rc_synergy_guard_damage");
            ValidateSynergyDamage(result, "DMG_BUILD1_GUARD_READY_01", "synergy_guard_shockwave", 0.0f, 15.0f, 1.2f, 60.0f, 3, 0.0f, "cone", "no_damage", "battle_end", "rc_build1_guard_ready");
            ValidateSynergyDamage(result, "DMG_SYNERGY_ARCHER_01", "synergy_archer_rain", 14.0f, 8.0f, 2.0f, 0.0f, 8, 0.0f, "circle", "normal", string.Empty, "rc_synergy_archer_damage");
            ValidateSynergyDamage(result, "DMG_SYNERGY_MAGIC_01", "synergy_magic_chain", 10.0f, 0.0f, 0.0f, 0.0f, 5, 0.0025f, "projectile", "per_projectile_max_hp_cap", string.Empty, "rc_synergy_magic_damage");
            ValidateSynergyDamage(result, "DMG_SYNERGY_EXPLOSION_01", "synergy_explosion_chain", 20.0f, 0.0f, 1.8f, 0.0f, 8, 0.008f, "circle", "per_cast_max_hp_cap", string.Empty, "rc_synergy_explosion_damage");
            ValidateSynergyDamage(result, "DMG_BUILD1_EXPLOSIVE_READY_01", "synergy_explosion_chain", 10.0f, 0.0f, 1.6f, 0.0f, 6, 0.008f, "circle", "per_cast_max_hp_cap", "battle_end", "rc_build1_explosive_ready");
            ValidateSynergyDamage(result, "DMG_SYNERGY_BEAST_01", "synergy_beast_hunt", 8.0f, 10.0f, 0.0f, 0.0f, 1, 0.004f, "direct", "per_caster_max_hp_cap", string.Empty, "rc_synergy_beast_damage");
            ValidateSynergyDamage(result, "DOT_SYNERGY_BEAST_01", "synergy_beast_hunt", 3.0f, 0.0f, 0.0f, 0.0f, 1, 0.001f, "dot", "per_tick_max_hp_cap", string.Empty, "rc_synergy_beast_bleed");

            if (_synergyDamagesById.TryGetValue("DMG_SYNERGY_ARCHER_01", out SynergyDamageData archer) == false
                || archer.SlowMultiplier != 0.8f || archer.SlowDurationSeconds != 2.0f || archer.TargetRuleId != "strongest_only")
                AddMissingRequiredId(result, "synergy_damage:invalid:DMG_SYNERGY_ARCHER_01");
            if (_synergyDamagesById.TryGetValue("DMG_SYNERGY_MAGIC_01", out SynergyDamageData magic) == false
                || magic.ActionCount != 3 || magic.ProjectileLifetimeSeconds != 2.0f || magic.ProjectileCount != 5
                || !magic.SameTargetDuplicatesAllowed || !magic.ExcludesSelfCounter)
                AddMissingRequiredId(result, "synergy_damage:invalid:DMG_SYNERGY_MAGIC_01");
            if (_synergyDamagesById.TryGetValue("DMG_SYNERGY_EXPLOSION_01", out SynergyDamageData explosion) == false
                || explosion.TriggerThreshold != 8 || !explosion.SameScopeRecursionBlocked || explosion.FrameCap != 1 || !explosion.OverflowCarries)
                AddMissingRequiredId(result, "synergy_damage:invalid:DMG_SYNERGY_EXPLOSION_01");
            if (_synergyDamagesById.TryGetValue("DMG_SYNERGY_BEAST_01", out SynergyDamageData beast) == false
                || !beast.EachAliveParticipantOneHit || !beast.TargetDeathCancelsRemaining || !beast.BossNoStagger || beast.TargetRuleId != "common_target")
                AddMissingRequiredId(result, "synergy_damage:invalid:DMG_SYNERGY_BEAST_01");
            if (_synergyDamagesById.TryGetValue("DOT_SYNERGY_BEAST_01", out SynergyDamageData bleed) == false
                || bleed.TickIntervalSeconds != 1.0f || bleed.DurationSeconds != 5.0f || bleed.StackRuleId != "max_one_refresh_duration"
                || !bleed.BleedImmuneExcluded || bleed.TargetRuleId != "boss_target")
                AddMissingRequiredId(result, "synergy_damage:invalid:DOT_SYNERGY_BEAST_01");

            ValidateSynergyEffect(result, "EFFECT_SYNERGY_GUARD_DR", "synergy_guard_shockwave", 0.75f, 0.25f, 0.0f, 6.0f, 0.0f, 0.0f, 0.0f, 0.60f, "on_cast", "same_source_refresh_no_numeric_stack", "rc_guard_companion_dr");
            ValidateSynergyEffect(result, "EFFECT_HEALING_BOND_DR", "synergy_healing_bond", 0.8f, 0.2f, 0.0f, 3.0f, 2.5f, 0.0f, 0.0f, 0.0f, "zone_membership", "new_replaces_old_no_stack", "rc_healing_bond_dr");
            ValidateSynergyEffect(result, "EFFECT_MIXED_COMMAND", "synergy_mixed_command", 0.0f, 0.0f, 15.0f, 5.0f, 0.0f, 1.15f, 1.15f, 0.0f, "all_alive_companions", "same_source_refresh_no_multiplier_stack", "rc_mixed_command_multiplier");
            ValidateSynergyEffect(result, "EFFECT_BUILD1_MIXED_READY_01", "synergy_mixed_command", 0.0f, 0.0f, 18.0f, 3.0f, 0.0f, 1.0f, 1.12f, 0.0f, "all_living_companions", "same_source_refresh_no_multiplier_stack", "rc_build1_mixed_ready");

            if (_synergyEffectsById.TryGetValue("EFFECT_SYNERGY_GUARD_DR", out SynergyEffectData guard) == false
                || !guard.AllAliveCompanions || !guard.CommanderExcluded || !guard.SameSourceRefresh || guard.NumericStackingAllowed)
                AddMissingRequiredId(result, "synergy_effect:invalid:EFFECT_SYNERGY_GUARD_DR");
            if (_synergyEffectsById.TryGetValue("EFFECT_HEALING_BOND_DR", out SynergyEffectData healing) == false
                || !healing.KnockdownImmunity || !healing.ZoneMembership || !healing.LeaveRemoves || !healing.NewReplacesOld
                || !healing.CompanionsOnly || !healing.CommanderExcluded)
                AddMissingRequiredId(result, "synergy_effect:invalid:EFFECT_HEALING_BOND_DR");
            if (_synergyEffectsById.TryGetValue("EFFECT_MIXED_COMMAND", out SynergyEffectData mixed) == false
                || !mixed.AllAliveCompanions || !mixed.ExcludesCompanionTagFalseSummons || !mixed.SameSourceRefresh || mixed.NumericStackingAllowed)
                AddMissingRequiredId(result, "synergy_effect:invalid:EFFECT_MIXED_COMMAND");

            if (_synergySummonsById.TryGetValue("UNIT_SYNERGY_SKELETON_01", out SynergySummonData summon) == false)
            {
                AddMissingRequiredId(result, "synergy_summon:missing:UNIT_SYNERGY_SKELETON_01");
            }
            else if (summon.SynergyId != "synergy_undead_summon" || summon.Hp != 22 || summon.Damage != 5
                || summon.AttackInterval != 1.2f || summon.Range != 1.0f || summon.MoveSpeed != 2.8f || summon.AiScanInterval != 0.2f
                || summon.LifetimeRuleId != "battle_end_or_hp0" || summon.TargetRule != CombatTargetRule.Nearest || summon.ActiveCap != 5
                || summon.BossLockCount != 3 || summon.FrameSpawnCap != 1 || summon.Tags != "summon_object,companion_tag=false,no_family_tag"
                || summon.ResetRuleId != "battle_end" || summon.RemoteConfigKey != "rc_synergy_skeleton_stats"
                || summon.DistinctFromSummonId != "UNIT_PERSONAL_SKELETON_01")
            {
                AddMissingRequiredId(result, "synergy_summon:invalid:UNIT_SYNERGY_SKELETON_01");
            }

            if (_companionSummonsById.TryGetValue("UNIT_PERSONAL_SKELETON_01", out CompanionSummonData personal) == false
                || personal.DistinctFromSummonId != "UNIT_SYNERGY_SKELETON_01")
                AddMissingRequiredId(result, "synergy_summon:personal_identity_mismatch");
        }

        void ValidateSynergyDamage(
            DataLoadResult result, string id, string synergyId, float baseValue, float cadenceSeconds, float radius, float angle,
            int maxTargets, float bossMaxHpPercent, string deliveryRuleId, string bossRuleId, string resetRuleId, string remoteConfigKey)
        {
            if (_synergyDamagesById.TryGetValue(id, out SynergyDamageData data) == false)
            {
                AddMissingRequiredId(result, $"synergy_damage:missing:{id}");
                return;
            }
            if (data.SynergyId != synergyId || data.BaseValue != baseValue || data.CadenceSeconds != cadenceSeconds
                || data.Radius != radius || data.Angle != angle || data.MaxTargets != maxTargets
                || data.BossMaxHpPercent != bossMaxHpPercent || data.DeliveryRuleId != deliveryRuleId
                || data.BossRuleId != bossRuleId || data.ResetRuleId != resetRuleId || data.RemoteConfigKey != remoteConfigKey)
            {
                AddMissingRequiredId(result, $"synergy_damage:invalid:{id}");
            }
        }

        void ValidateSynergyEffect(
            DataLoadResult result, string id, string synergyId, float damageTakenMultiplier, float damageReduction, float cadenceSeconds,
            float durationSeconds, float radius, float attackIntervalDivisor, float moveSpeedMultiplier, float totalDamageReductionCap,
            string activationRuleId, string stackRuleId, string remoteConfigKey)
        {
            if (_synergyEffectsById.TryGetValue(id, out SynergyEffectData data) == false)
            {
                AddMissingRequiredId(result, $"synergy_effect:missing:{id}");
                return;
            }
            if (data.SynergyId != synergyId || data.DamageTakenMultiplier != damageTakenMultiplier || data.DamageReduction != damageReduction
                || data.CadenceSeconds != cadenceSeconds || data.DurationSeconds != durationSeconds || data.Radius != radius
                || data.AttackIntervalDivisor != attackIntervalDivisor || data.MoveSpeedMultiplier != moveSpeedMultiplier
                || data.TotalDamageReductionCap != totalDamageReductionCap || data.ActivationRuleId != activationRuleId
                || data.StackRuleId != stackRuleId || data.RemoteConfigKey != remoteConfigKey)
            {
                AddMissingRequiredId(result, $"synergy_effect:invalid:{id}");
            }
        }

        static bool BoolAttr(XElement element, string name)
        {
            return string.Equals(element.Attribute(name)?.Value, "true", StringComparison.OrdinalIgnoreCase);
        }
    }
}
