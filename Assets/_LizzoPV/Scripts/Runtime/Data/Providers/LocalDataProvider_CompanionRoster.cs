using System;
using System.Xml.Linq;

namespace Lizzo.PV.Data
{
    public sealed partial class LocalDataProvider
    {
        const int RequiredCompanionRosterCount = 12;
        const int RequiredPromotionUnitCount = 3;

        void ResetCompanionCatalog()
        {
            _companionRoster.Clear();
            _companionRosterByUnitId.Clear();
            _companionPromotionsByProfileId.Clear();
            _companionCardLocalizations.Clear();
            _companionCardLocalizationsByUnitId.Clear();
            _passives.Clear();
            _passivesById.Clear();
            ResetCompanionCombatCatalog();
            _companionCatalogValidationErrors.Clear();
        }

        void LoadCompanionRoster(XElement parent)
        {
            if (parent == null)
            {
                AddCompanionCatalogValidationError("companion_roster:missing_table");
                return;
            }

            foreach (XElement element in parent.Elements("CompanionRosterData"))
            {
                CompanionRosterData data = new CompanionRosterData
                {
                    UnitId = StringAttr(element, "unitId", string.Empty),
                    FamilyTags = StringAttr(element, "familyTags", string.Empty),
                    SkillId = StringAttr(element, "skillId", string.Empty),
                    EffectRef = StringAttr(element, "effectRef", string.Empty),
                    PromotionProfileId = StringAttr(element, "promotionProfileId", string.Empty),
                    RecruitTitleKey = StringAttr(element, "recruitTitleKey", string.Empty),
                    RecruitDescKey = StringAttr(element, "recruitDescKey", string.Empty),
                };

                if (string.IsNullOrEmpty(data.UnitId))
                {
                    AddCompanionCatalogValidationError("companion_roster:missing_unit_id");
                    continue;
                }

                if (_companionRosterByUnitId.ContainsKey(data.UnitId))
                {
                    AddCompanionCatalogValidationError($"companion_roster:duplicate:{data.UnitId}");
                    continue;
                }

                _companionRoster.Add(data);
                _companionRosterByUnitId.Add(data.UnitId, data);
            }
        }

        void LoadCompanionPromotions(XElement parent)
        {
            if (parent == null)
            {
                AddCompanionCatalogValidationError("companion_promotion:missing_table");
                return;
            }

            foreach (XElement element in parent.Elements("CompanionPromotionData"))
            {
                CompanionPromotionData data = new CompanionPromotionData
                {
                    ProfileId = StringAttr(element, "profileId", string.Empty),
                    BaseUnitId = StringAttr(element, "baseUnitId", string.Empty),
                    PromotedUnitId = StringAttr(element, "promotedUnitId", string.Empty),
                    DisplayName = StringAttr(element, "displayName", string.Empty),
                    HpMultiplier = FloatAttr(element, "hpMultiplier", 0.0f),
                    EffectMultiplier = FloatAttr(element, "effectMultiplier", 0.0f),
                    IntervalMultiplier = FloatAttr(element, "intervalMultiplier", 0.0f),
                    PrefabId = StringAttr(element, "prefabId", string.Empty),
                    CardKey = StringAttr(element, "cardKey", string.Empty),
                    RequiredUnitCount = IntAttr(element, "requiredUnitCount", 0),
                    VisualUnitCount = IntAttr(element, "visualUnitCount", 0),
                };

                if (string.IsNullOrEmpty(data.ProfileId))
                {
                    AddCompanionCatalogValidationError("companion_promotion:missing_profile_id");
                    continue;
                }

                if (_companionPromotionsByProfileId.ContainsKey(data.ProfileId))
                {
                    AddCompanionCatalogValidationError($"companion_promotion:duplicate:{data.ProfileId}");
                    continue;
                }

                _companionPromotionsByProfileId.Add(data.ProfileId, data);
            }
        }

        void ValidateCompanionCatalog(DataLoadResult result)
        {
            for (int i = 0; i < _companionCatalogValidationErrors.Count; i++)
                AddMissingRequiredId(result, _companionCatalogValidationErrors[i]);

            if (_companionRoster.Count != RequiredCompanionRosterCount)
                AddMissingRequiredId(result, $"companion_roster:count:{_companionRoster.Count}");
            if (_companionPromotionsByProfileId.Count != RequiredCompanionRosterCount)
                AddMissingRequiredId(result, $"companion_promotion:count:{_companionPromotionsByProfileId.Count}");
            if (_companionCardLocalizations.Count != RequiredCompanionRosterCount)
                AddMissingRequiredId(result, $"companion_card_localization:count:{_companionCardLocalizations.Count}");

            for (int i = 0; i < _companionRoster.Count; i++)
            {
                CompanionRosterData roster = _companionRoster[i];
                if (string.IsNullOrEmpty(roster.FamilyTags)
                    || string.IsNullOrEmpty(roster.SkillId)
                    || string.IsNullOrEmpty(roster.EffectRef)
                    || string.IsNullOrEmpty(roster.PromotionProfileId)
                    || string.IsNullOrEmpty(roster.RecruitTitleKey)
                    || string.IsNullOrEmpty(roster.RecruitDescKey))
                {
                    AddMissingRequiredId(result, $"companion_roster:invalid:{roster.UnitId}");
                }

                if (_companionPromotionsByProfileId.TryGetValue(roster.PromotionProfileId, out CompanionPromotionData promotion) == false)
                {
                    AddMissingRequiredId(result, $"companion_roster:missing_promotion:{roster.UnitId}");
                    continue;
                }

                if (promotion.BaseUnitId != roster.UnitId)
                    AddMissingRequiredId(result, $"companion_roster:promotion_base_mismatch:{roster.UnitId}");

                if (_companionCardLocalizationsByUnitId.TryGetValue(roster.UnitId, out CompanionCardLocalizationData localization) == false
                    || IsValidCardLocalization(localization) == false)
                {
                    AddMissingRequiredId(result, $"companion_card_localization:invalid:{roster.UnitId}");
                }
            }

            foreach (CompanionPromotionData promotion in _companionPromotionsByProfileId.Values)
            {
                if (_companionRosterByUnitId.ContainsKey(promotion.BaseUnitId) == false)
                    AddMissingRequiredId(result, $"companion_promotion:orphan_base:{promotion.BaseUnitId}");

                if (string.IsNullOrEmpty(promotion.BaseUnitId)
                    || string.IsNullOrEmpty(promotion.PromotedUnitId)
                    || string.IsNullOrEmpty(promotion.DisplayName)
                    || string.IsNullOrEmpty(promotion.PrefabId)
                    || string.IsNullOrEmpty(promotion.CardKey)
                    || promotion.HpMultiplier <= 0.0f
                    || promotion.EffectMultiplier <= 0.0f
                    || promotion.IntervalMultiplier <= 0.0f
                    || promotion.RequiredUnitCount != RequiredPromotionUnitCount
                    || promotion.VisualUnitCount != RequiredPromotionUnitCount)
                {
                    AddMissingRequiredId(result, $"companion_promotion:invalid:{promotion.ProfileId}");
                }
            }

            ValidateCompanionCombatCatalog(result);
        }

        static bool IsValidCardLocalization(CompanionCardLocalizationData data)
        {
            return data != null
                && string.IsNullOrEmpty(data.RecruitTitleKey) == false
                && string.IsNullOrEmpty(data.RecruitTitleKo) == false
                && string.IsNullOrEmpty(data.RecruitTitleEn) == false
                && string.IsNullOrEmpty(data.RecruitDescKey) == false
                && string.IsNullOrEmpty(data.RecruitDescKo) == false
                && string.IsNullOrEmpty(data.RecruitDescEn) == false
                && string.IsNullOrEmpty(data.ReinforceTitleKo) == false
                && string.IsNullOrEmpty(data.ReinforceTitleEn) == false
                && string.IsNullOrEmpty(data.ReinforceDescKo) == false
                && string.IsNullOrEmpty(data.ReinforceDescEn) == false
                && string.IsNullOrEmpty(data.PromotionTitleKo) == false
                && string.IsNullOrEmpty(data.PromotionTitleEn) == false
                && string.IsNullOrEmpty(data.RoleBadgeKo) == false
                && string.IsNullOrEmpty(data.RoleBadgeEn) == false
                && string.IsNullOrEmpty(data.SynergyHintKo) == false
                && string.IsNullOrEmpty(data.SynergyHintEn) == false
                && string.IsNullOrEmpty(data.RecruitBadgeKey) == false
                && string.IsNullOrEmpty(data.ReinforceBadgeKey) == false
                && string.IsNullOrEmpty(data.PromoteBadgeKey) == false;
        }

        void AddCompanionCatalogValidationError(string value)
        {
            if (_companionCatalogValidationErrors.Contains(value) == false)
                _companionCatalogValidationErrors.Add(value);
        }

        static void AddMissingRequiredId(DataLoadResult result, string value)
        {
            if (result.MissingRequiredIds.Contains(value) == false)
                result.MissingRequiredIds.Add(value);
        }

        void AddFallbackCompanionRoster(
            string unitId,
            string familyTags,
            string skillId,
            string effectRef,
            string promotionProfileId,
            string recruitTitleKey,
            string recruitDescKey)
        {
            CompanionRosterData data = new CompanionRosterData
            {
                UnitId = unitId,
                FamilyTags = familyTags,
                SkillId = skillId,
                EffectRef = effectRef,
                PromotionProfileId = promotionProfileId,
                RecruitTitleKey = recruitTitleKey,
                RecruitDescKey = recruitDescKey,
            };
            _companionRoster.Add(data);
            _companionRosterByUnitId.Add(unitId, data);
        }

        void AddFallbackCompanionPromotion(
            string profileId,
            string baseUnitId,
            string promotedUnitId,
            string displayName,
            float hpMultiplier,
            float effectMultiplier,
            float intervalMultiplier,
            string prefabId,
            string cardKey)
        {
            _companionPromotionsByProfileId.Add(profileId, new CompanionPromotionData
            {
                ProfileId = profileId,
                BaseUnitId = baseUnitId,
                PromotedUnitId = promotedUnitId,
                DisplayName = displayName,
                HpMultiplier = hpMultiplier,
                EffectMultiplier = effectMultiplier,
                IntervalMultiplier = intervalMultiplier,
                PrefabId = prefabId,
                CardKey = cardKey,
                RequiredUnitCount = RequiredPromotionUnitCount,
                VisualUnitCount = RequiredPromotionUnitCount,
            });
        }
    }
}
