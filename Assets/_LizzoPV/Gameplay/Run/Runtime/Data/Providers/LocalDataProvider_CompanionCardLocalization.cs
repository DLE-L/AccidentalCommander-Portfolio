using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Lizzo.PV.Data
{
    public sealed partial class LocalDataProvider
    {
        readonly List<CompanionCardLocalizationData> _companionCardLocalizations = new List<CompanionCardLocalizationData>();
        readonly Dictionary<string, CompanionCardLocalizationData> _companionCardLocalizationsByUnitId = new Dictionary<string, CompanionCardLocalizationData>();
        readonly IReadOnlyList<CompanionCardLocalizationData> _companionCardLocalizationView;

        public IReadOnlyList<CompanionCardLocalizationData> CompanionCardLocalizations
        {
            get
            {
                EnsureInitialized();
                return _companionCardLocalizationView;
            }
        }

        public CompanionCardLocalizationData GetCompanionCardLocalization(string unitId)
        {
            EnsureInitialized();
            return _companionCardLocalizationsByUnitId.TryGetValue(unitId, out CompanionCardLocalizationData data) ? data : null;
        }

        void LoadCompanionCardLocalizations(XElement parent)
        {
            if (parent == null)
            {
                AddCompanionCatalogValidationError("companion_card_localization:missing_table");
                return;
            }

            foreach (XElement element in parent.Elements("CompanionCardLocalizationData"))
            {
                CompanionCardLocalizationData data = new CompanionCardLocalizationData
                {
                    UnitId = StringAttr(element, "unitId", string.Empty),
                    RecruitTitleKey = StringAttr(element, "recruitTitleKey", string.Empty),
                    RecruitTitleKo = StringAttr(element, "recruitTitleKo", string.Empty),
                    RecruitTitleEn = StringAttr(element, "recruitTitleEn", string.Empty),
                    RecruitDescKey = StringAttr(element, "recruitDescKey", string.Empty),
                    RecruitDescKo = StringAttr(element, "recruitDescKo", string.Empty),
                    RecruitDescEn = StringAttr(element, "recruitDescEn", string.Empty),
                    ReinforceTitleKo = StringAttr(element, "reinforceTitleKo", string.Empty),
                    ReinforceTitleEn = StringAttr(element, "reinforceTitleEn", string.Empty),
                    ReinforceDescKo = StringAttr(element, "reinforceDescKo", string.Empty),
                    ReinforceDescEn = StringAttr(element, "reinforceDescEn", string.Empty),
                    PromotionTitleKo = StringAttr(element, "promotionTitleKo", string.Empty),
                    PromotionTitleEn = StringAttr(element, "promotionTitleEn", string.Empty),
                    RoleBadgeKo = StringAttr(element, "roleBadgeKo", string.Empty),
                    RoleBadgeEn = StringAttr(element, "roleBadgeEn", string.Empty),
                    SynergyHintKo = StringAttr(element, "synergyHintKo", string.Empty),
                    SynergyHintEn = StringAttr(element, "synergyHintEn", string.Empty),
                    RecruitBadgeKey = StringAttr(element, "recruitBadgeKey", string.Empty),
                    ReinforceBadgeKey = StringAttr(element, "reinforceBadgeKey", string.Empty),
                    PromoteBadgeKey = StringAttr(element, "promoteBadgeKey", string.Empty),
                };

                if (string.IsNullOrEmpty(data.UnitId))
                {
                    AddCompanionCatalogValidationError("companion_card_localization:missing_unit_id");
                    continue;
                }

                if (_companionCardLocalizationsByUnitId.ContainsKey(data.UnitId))
                {
                    AddCompanionCatalogValidationError($"companion_card_localization:duplicate:{data.UnitId}");
                    continue;
                }

                _companionCardLocalizations.Add(data);
                _companionCardLocalizationsByUnitId.Add(data.UnitId, data);
            }
        }

        void AddFallbackCompanionCardLocalization(
            string unitId, string recruitTitleKey, string recruitTitleKo, string recruitTitleEn,
            string recruitDescKey, string recruitDescKo, string recruitDescEn,
            string reinforceTitleKo, string reinforceTitleEn, string reinforceDescKo, string reinforceDescEn,
            string promotionTitleKo, string promotionTitleEn, string roleBadgeKo, string roleBadgeEn,
            string synergyHintKo, string synergyHintEn)
        {
            CompanionCardLocalizationData data = new CompanionCardLocalizationData
            {
                UnitId = unitId,
                RecruitTitleKey = recruitTitleKey,
                RecruitTitleKo = recruitTitleKo,
                RecruitTitleEn = recruitTitleEn,
                RecruitDescKey = recruitDescKey,
                RecruitDescKo = recruitDescKo,
                RecruitDescEn = recruitDescEn,
                ReinforceTitleKo = reinforceTitleKo,
                ReinforceTitleEn = reinforceTitleEn,
                ReinforceDescKo = reinforceDescKo,
                ReinforceDescEn = reinforceDescEn,
                PromotionTitleKo = promotionTitleKo,
                PromotionTitleEn = promotionTitleEn,
                RoleBadgeKo = roleBadgeKo,
                RoleBadgeEn = roleBadgeEn,
                SynergyHintKo = synergyHintKo,
                SynergyHintEn = synergyHintEn,
                RecruitBadgeKey = "ui.card.badge.recruit",
                ReinforceBadgeKey = "ui.card.badge.reinforce",
                PromoteBadgeKey = "ui.card.badge.promote",
            };
            _companionCardLocalizations.Add(data);
            _companionCardLocalizationsByUnitId.Add(unitId, data);
        }
    }
}
