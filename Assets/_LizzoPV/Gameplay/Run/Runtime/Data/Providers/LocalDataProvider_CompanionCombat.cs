using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Lizzo.PV.Data
{
    public sealed partial class LocalDataProvider
    {
        readonly List<CompanionCombatProfileData> _companionCombatProfiles = new List<CompanionCombatProfileData>();
        readonly Dictionary<string, CompanionCombatProfileData> _companionCombatProfilesByUnitId = new Dictionary<string, CompanionCombatProfileData>();
        readonly List<CombatEffectData> _combatEffects = new List<CombatEffectData>();
        readonly Dictionary<string, CombatEffectData> _combatEffectsById = new Dictionary<string, CombatEffectData>();
        readonly List<CompanionSummonData> _companionSummons = new List<CompanionSummonData>();
        readonly Dictionary<string, CompanionSummonData> _companionSummonsById = new Dictionary<string, CompanionSummonData>();
        readonly IReadOnlyList<CompanionCombatProfileData> _companionCombatProfileView;
        readonly IReadOnlyList<CombatEffectData> _combatEffectView;
        readonly IReadOnlyList<CompanionSummonData> _companionSummonView;

        const int RequiredCompanionCombatProfileCount = 12;
        const int RequiredCombatEffectCount = 27;
        const int RequiredCompanionSummonCount = 1;

        public IReadOnlyList<CompanionCombatProfileData> CompanionCombatProfiles
        {
            get
            {
                EnsureInitialized();
                return _companionCombatProfileView;
            }
        }

        public IReadOnlyList<CombatEffectData> CombatEffects
        {
            get
            {
                EnsureInitialized();
                return _combatEffectView;
            }
        }

        public IReadOnlyList<CompanionSummonData> CompanionSummons
        {
            get
            {
                EnsureInitialized();
                return _companionSummonView;
            }
        }

        public CompanionCombatProfileData GetCompanionCombatProfile(string unitId)
        {
            EnsureInitialized();
            return _companionCombatProfilesByUnitId.TryGetValue(unitId, out CompanionCombatProfileData data) ? data : null;
        }

        public CombatEffectData GetCombatEffect(string effectId)
        {
            EnsureInitialized();
            return _combatEffectsById.TryGetValue(effectId, out CombatEffectData data) ? data : null;
        }

        public CompanionSummonData GetCompanionSummon(string summonId)
        {
            EnsureInitialized();
            return _companionSummonsById.TryGetValue(summonId, out CompanionSummonData data) ? data : null;
        }

        void ResetCompanionCombatCatalog()
        {
            _companionCombatProfiles.Clear();
            _companionCombatProfilesByUnitId.Clear();
            _combatEffects.Clear();
            _combatEffectsById.Clear();
            _companionSummons.Clear();
            _companionSummonsById.Clear();
        }

        void LoadCompanionCombatProfiles(XElement parent)
        {
            if (parent == null)
            {
                AddCompanionCatalogValidationError("companion_combat_profile:missing_table");
                return;
            }

            foreach (XElement element in parent.Elements("CompanionCombatProfileData"))
            {
                CompanionCombatProfileData data = new CompanionCombatProfileData
                {
                    UnitId = StringAttr(element, "unitId", string.Empty),
                    BaseHp = IntAttr(element, "baseHp", 0),
                    MoveSpeed = FloatAttr(element, "moveSpeed", 0.0f),
                    BasicSkillId = StringAttr(element, "basicSkillId", string.Empty),
                    BasicEffectId = StringAttr(element, "basicEffectId", string.Empty),
                    SecondarySkillId = StringAttr(element, "secondarySkillId", string.Empty),
                    SecondaryEffectId = StringAttr(element, "secondaryEffectId", string.Empty),
                    PromotionProfileId = StringAttr(element, "promotionProfileId", string.Empty),
                    DownDurationSeconds = FloatAttr(element, "downDurationSeconds", 0.0f),
                    RecoverHpPercent = FloatAttr(element, "recoverHpPercent", 0.0f),
                    Count2EffectMultiplier = FloatAttr(element, "count2EffectMultiplier", 0.0f),
                    Count2HpMultiplier = FloatAttr(element, "count2HpMultiplier", 0.0f),
                    NoTargetRetrySeconds = FloatAttr(element, "noTargetRetrySeconds", 0.0f),
                    Count3RuleId = StringAttr(element, "count3RuleId", string.Empty),
                    SecondaryRuleId = StringAttr(element, "secondaryRuleId", string.Empty),
                };

                if (string.IsNullOrEmpty(data.UnitId))
                {
                    AddCompanionCatalogValidationError("companion_combat_profile:missing_unit_id");
                    continue;
                }

                if (_companionCombatProfilesByUnitId.ContainsKey(data.UnitId))
                {
                    AddCompanionCatalogValidationError($"companion_combat_profile:duplicate:{data.UnitId}");
                    continue;
                }

                _companionCombatProfiles.Add(data);
                _companionCombatProfilesByUnitId.Add(data.UnitId, data);
            }
        }

        void LoadCombatEffects(XElement parent)
        {
            if (parent == null)
            {
                AddCompanionCatalogValidationError("combat_effect:missing_table");
                return;
            }

            foreach (XElement element in parent.Elements("CombatEffectData"))
            {
                CombatEffectData data = new CombatEffectData
                {
                    Id = StringAttr(element, "id", string.Empty),
                    OwnerUnitId = StringAttr(element, "ownerUnitId", string.Empty),
                    SkillId = StringAttr(element, "skillId", string.Empty),
                    EffectKind = EnumAttr<CombatEffectKind>(element, "effectKind"),
                    DeliveryKind = EnumAttr<CombatDeliveryKind>(element, "deliveryKind"),
                    BaseValue = FloatAttr(element, "baseValue", 0.0f),
                    CastInterval = FloatAttr(element, "castInterval", 0.0f),
                    TickInterval = FloatAttr(element, "tickInterval", 0.0f),
                    Duration = FloatAttr(element, "duration", 0.0f),
                    ProjectileLifetime = FloatAttr(element, "projectileLifetime", 0.0f),
                    Range = FloatAttr(element, "range", 0.0f),
                    Radius = FloatAttr(element, "radius", 0.0f),
                    Angle = FloatAttr(element, "angle", 0.0f),
                    ChainDistance = FloatAttr(element, "chainDistance", 0.0f),
                    MaxTargets = IntAttr(element, "maxTargets", 0),
                    AffectsAllTargetsInShape = BoolAttr(element, "affectsAllTargetsInShape"),
                    CastDelay = FloatAttr(element, "castDelay", 0.0f),
                    Push = FloatAttr(element, "push", 0.0f),
                    TriggerCount = IntAttr(element, "triggerCount", 0),
                    MaxActiveCount = IntAttr(element, "maxActiveCount", 0),
                    TargetRule = EnumAttr<CombatTargetRule>(element, "targetRule"),
                    StatusKind = EnumAttr<CompanionEnemyStatusKind>(element, "statusKind"),
                    StatusMagnitude = FloatAttr(element, "statusMagnitude", 0.0f),
                    StatusDuration = FloatAttr(element, "statusDuration", 0.0f),
                    RuleId = StringAttr(element, "ruleId", string.Empty),
                };

                if (string.IsNullOrEmpty(data.Id))
                {
                    AddCompanionCatalogValidationError("combat_effect:missing_id");
                    continue;
                }

                if (_combatEffectsById.ContainsKey(data.Id))
                {
                    AddCompanionCatalogValidationError($"combat_effect:duplicate:{data.Id}");
                    continue;
                }

                _combatEffects.Add(data);
                _combatEffectsById.Add(data.Id, data);
            }
        }

        void LoadCompanionSummons(XElement parent)
        {
            if (parent == null)
            {
                AddCompanionCatalogValidationError("companion_summon:missing_table");
                return;
            }

            foreach (XElement element in parent.Elements("CompanionSummonData"))
            {
                CompanionSummonData data = new CompanionSummonData
                {
                    Id = StringAttr(element, "id", string.Empty),
                    OwnerUnitId = StringAttr(element, "ownerUnitId", string.Empty),
                    SkillId = StringAttr(element, "skillId", string.Empty),
                    Hp = IntAttr(element, "hp", 0),
                    Damage = IntAttr(element, "damage", 0),
                    AttackInterval = FloatAttr(element, "attackInterval", 0.0f),
                    Range = FloatAttr(element, "range", 0.0f),
                    MoveSpeed = FloatAttr(element, "moveSpeed", 0.0f),
                    AiScanInterval = FloatAttr(element, "aiScanInterval", 0.0f),
                    LifetimeRuleId = StringAttr(element, "lifetimeRuleId", string.Empty),
                    TargetRule = EnumAttr<CombatTargetRule>(element, "targetRule"),
                    Tags = StringAttr(element, "tags", string.Empty),
                    BossRuleId = StringAttr(element, "bossRuleId", string.Empty),
                    StackRuleId = StringAttr(element, "stackRuleId", string.Empty),
                    ResetRuleId = StringAttr(element, "resetRuleId", string.Empty),
                };

                if (string.IsNullOrEmpty(data.Id))
                {
                    AddCompanionCatalogValidationError("companion_summon:missing_id");
                    continue;
                }

                if (_companionSummonsById.ContainsKey(data.Id))
                {
                    AddCompanionCatalogValidationError($"companion_summon:duplicate:{data.Id}");
                    continue;
                }

                _companionSummons.Add(data);
                _companionSummonsById.Add(data.Id, data);
            }
        }

        void ValidateCompanionCombatCatalog(DataLoadResult result)
        {
            if (_companionCombatProfiles.Count != RequiredCompanionCombatProfileCount)
                AddMissingRequiredId(result, $"companion_combat_profile:count:{_companionCombatProfiles.Count}");
            if (_combatEffects.Count != RequiredCombatEffectCount)
                AddMissingRequiredId(result, $"combat_effect:count:{_combatEffects.Count}");
            if (_companionSummons.Count != RequiredCompanionSummonCount)
                AddMissingRequiredId(result, $"companion_summon:count:{_companionSummons.Count}");

            for (int i = 0; i < _companionCombatProfiles.Count; i++)
            {
                CompanionCombatProfileData profile = _companionCombatProfiles[i];
                if (i >= _companionRoster.Count || _companionRoster[i].UnitId != profile.UnitId)
                    AddMissingRequiredId(result, $"companion_combat_profile:order:{profile.UnitId}");

                if (_companionRosterByUnitId.TryGetValue(profile.UnitId, out CompanionRosterData roster) == false)
                {
                    AddMissingRequiredId(result, $"companion_combat_profile:orphan_base:{profile.UnitId}");
                    continue;
                }

                if (profile.UnitId == "archer" || profile.BaseHp <= 0 || profile.MoveSpeed <= 0.0f
                    || string.IsNullOrEmpty(profile.BasicSkillId) || string.IsNullOrEmpty(profile.BasicEffectId)
                    || string.IsNullOrEmpty(profile.PromotionProfileId) || profile.DownDurationSeconds <= 0.0f
                    || profile.RecoverHpPercent <= 0.0f || profile.Count2EffectMultiplier <= 0.0f
                    || profile.Count2HpMultiplier <= 0.0f || profile.NoTargetRetrySeconds <= 0.0f
                    || string.IsNullOrEmpty(profile.Count3RuleId))
                {
                    AddMissingRequiredId(result, $"companion_combat_profile:invalid:{profile.UnitId}");
                }

                if (roster.SkillId != profile.BasicSkillId || roster.EffectRef != profile.BasicEffectId || roster.PromotionProfileId != profile.PromotionProfileId)
                    AddMissingRequiredId(result, $"companion_combat_profile:roster_mismatch:{profile.UnitId}");

                if (_companionPromotionsByProfileId.TryGetValue(profile.PromotionProfileId, out CompanionPromotionData promotion) == false
                    || promotion.BaseUnitId != profile.UnitId)
                {
                    AddMissingRequiredId(result, $"companion_combat_profile:promotion_mismatch:{profile.UnitId}");
                }

                ValidateProfileEffect(result, profile.UnitId, profile.BasicSkillId, profile.BasicEffectId, false);
                if (!string.IsNullOrEmpty(profile.SecondaryEffectId))
                    ValidateProfileEffect(result, profile.UnitId, profile.SecondarySkillId, profile.SecondaryEffectId, true);
                else if (!string.IsNullOrEmpty(profile.SecondarySkillId) && string.IsNullOrEmpty(profile.SecondaryRuleId))
                    AddMissingRequiredId(result, $"companion_combat_profile:secondary_rule_missing:{profile.UnitId}");

                if (roster.PromotionContractStage == CompanionCombatContractStage.RuntimeConnected)
                    ValidatePromotionEffect(result, roster);
            }

            for (int i = 0; i < _combatEffects.Count; i++)
            {
                CombatEffectData effect = _combatEffects[i];
                if (string.IsNullOrEmpty(effect.OwnerUnitId) || string.IsNullOrEmpty(effect.SkillId)
                    || effect.EffectKind == CombatEffectKind.Invalid || effect.DeliveryKind == CombatDeliveryKind.Invalid
                    || effect.TargetRule == CombatTargetRule.Invalid || effect.BaseValue <= 0.0f
                    || (effect.StatusKind != CompanionEnemyStatusKind.None
                        && (effect.StatusMagnitude <= 0.0f || effect.StatusDuration <= 0.0f))
                    || (effect.CastInterval <= 0.0f && effect.TriggerCount <= 0) || effect.MaxTargets <= 0
                    || string.IsNullOrEmpty(effect.RuleId))
                {
                    AddMissingRequiredId(result, $"combat_effect:invalid:{effect.Id}");
                }

                bool profileReferenced = _companionCombatProfilesByUnitId.TryGetValue(effect.OwnerUnitId, out CompanionCombatProfileData profile)
                    && (profile.BasicEffectId == effect.Id || profile.SecondaryEffectId == effect.Id);
                bool promotionReferenced = _companionRosterByUnitId.TryGetValue(effect.OwnerUnitId, out CompanionRosterData roster)
                    && roster.PromotionEffectRef == effect.Id;
                if (profileReferenced == false && promotionReferenced == false)
                {
                    AddMissingRequiredId(result, $"combat_effect:orphan:{effect.Id}");
                }
            }

            for (int i = 0; i < _companionSummons.Count; i++)
            {
                CompanionSummonData summon = _companionSummons[i];
                if (summon.Id != "UNIT_PERSONAL_SKELETON_01"
                    || summon.OwnerUnitId != "necromancer"
                    || summon.SkillId != "skill_dark_ritualist_ritual"
                    || summon.Hp != 18
                    || summon.Damage != 4
                    || summon.AttackInterval != 1.3f
                    || summon.Range != 1.0f
                    || summon.MoveSpeed != 2.7f
                    || summon.AiScanInterval != 0.2f
                    || summon.LifetimeRuleId != "timed_group_or_hp0"
                    || summon.TargetRule != CombatTargetRule.Nearest
                    || summon.Tags != "summon_object,companion_tag=false,no_family_tag"
                    || summon.BossRuleId != "normal_target"
                    || summon.StackRuleId != "single_temporary_group"
                    || summon.ResetRuleId != "battle_end")
                {
                    AddMissingRequiredId(result, $"companion_summon:invalid:{summon.Id}");
                }

                CompanionRosterData roster = _companionRosterByUnitId.TryGetValue(summon.OwnerUnitId, out CompanionRosterData resolved) ? resolved : null;
                if (roster == null
                    || roster.PromotionAction != CompanionPromotionActionKind.CursedDeathUndeadRitual
                    || roster.PromotionContractStage != CompanionCombatContractStage.RuntimeConnected)
                    AddMissingRequiredId(result, $"companion_summon:orphan:{summon.Id}");
            }
        }

        void ValidateProfileEffect(DataLoadResult result, string unitId, string skillId, string effectId, bool secondary)
        {
            if (_combatEffectsById.TryGetValue(effectId, out CombatEffectData effect) == false)
            {
                AddMissingRequiredId(result, $"companion_combat_profile:missing_effect:{unitId}:{effectId}");
                return;
            }

            if (effect.OwnerUnitId != unitId || effect.SkillId != skillId)
                AddMissingRequiredId(result, $"companion_combat_profile:{(secondary ? "secondary" : "basic")}_effect_mismatch:{unitId}:{effectId}");
        }

        void ValidatePromotionEffect(DataLoadResult result, CompanionRosterData roster)
        {
            if (_combatEffectsById.TryGetValue(roster.PromotionEffectRef, out CombatEffectData effect) == false)
            {
                AddMissingRequiredId(result, $"companion_promotion_effect:missing:{roster.UnitId}:{roster.PromotionEffectRef}");
                return;
            }

            if (effect.OwnerUnitId != roster.UnitId)
                AddMissingRequiredId(result, $"companion_promotion_effect:owner_mismatch:{roster.UnitId}:{roster.PromotionEffectRef}");
        }

        static T EnumAttr<T>(XElement element, string name) where T : struct
        {
            string value = element.Attribute(name)?.Value;
            return Enum.TryParse(value, true, out T parsed) ? parsed : default;
        }

        static bool BoolAttr(XElement element, string name)
        {
            return string.Equals(element.Attribute(name)?.Value, "true", StringComparison.OrdinalIgnoreCase);
        }
    }
}
