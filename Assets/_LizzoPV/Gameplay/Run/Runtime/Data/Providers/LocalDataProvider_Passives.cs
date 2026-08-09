using System;
using System.Xml.Linq;

namespace Lizzo.PV.Data
{
    public sealed partial class LocalDataProvider
    {
        const int RequiredPassiveCount = 16;

        void LoadPassives(XElement parent)
        {
            if (parent == null)
            {
                AddCompanionCatalogValidationError("passive:missing_table");
                return;
            }

            foreach (XElement element in parent.Elements("PassiveData"))
            {
                PassiveData data = new PassiveData
                {
                    Id = StringAttr(element, "id", string.Empty),
                    Category = StringAttr(element, "category", string.Empty),
                    EligibleTarget = StringAttr(element, "eligibleTarget", string.Empty),
                    EffectId = StringAttr(element, "effectId", string.Empty),
                    ValueType = StringAttr(element, "valueType", string.Empty),
                    Level1Value = FloatAttr(element, "level1Value", 0.0f),
                    Level2Value = FloatAttr(element, "level2Value", 0.0f),
                    Level3Value = FloatAttr(element, "level3Value", 0.0f),
                    StackRule = StringAttr(element, "stackRule", string.Empty),
                    TitleKo = StringAttr(element, "titleKo", string.Empty),
                    TitleEn = StringAttr(element, "titleEn", string.Empty),
                    DescriptionTemplateKo = StringAttr(element, "descriptionTemplateKo", string.Empty),
                    OfferWeightRule = StringAttr(element, "offerWeightRule", string.Empty),
                    Prohibition = StringAttr(element, "prohibition", string.Empty),
                };
                AddPassive(data);
            }
        }

        void AddPassive(PassiveData data)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.Id))
            {
                AddCompanionCatalogValidationError("passive:missing_id");
                return;
            }
            if (_passivesById.ContainsKey(data.Id))
            {
                AddCompanionCatalogValidationError($"passive:duplicate:{data.Id}");
                return;
            }
            _passives.Add(data);
            _passivesById.Add(data.Id, data);
        }

        void ValidatePassives(DataLoadResult result)
        {
            if (_passives.Count != RequiredPassiveCount)
                AddMissingRequiredId(result, $"passive:count:{_passives.Count}");

            for (int i = 0; i < _passives.Count; i++)
            {
                PassiveData data = _passives[i];
                if (string.IsNullOrWhiteSpace(data.Id)
                    || string.IsNullOrWhiteSpace(data.Category)
                    || string.IsNullOrWhiteSpace(data.EligibleTarget)
                    || string.IsNullOrWhiteSpace(data.EffectId)
                    || string.IsNullOrWhiteSpace(data.ValueType)
                    || string.IsNullOrWhiteSpace(data.StackRule)
                    || string.IsNullOrWhiteSpace(data.TitleKo)
                    || string.IsNullOrWhiteSpace(data.TitleEn)
                    || string.IsNullOrWhiteSpace(data.DescriptionTemplateKo)
                    || string.IsNullOrWhiteSpace(data.OfferWeightRule)
                    || string.IsNullOrWhiteSpace(data.Prohibition)
                    || data.Level1Value <= 0.0f
                    || data.Level2Value <= 0.0f
                    || data.Level3Value <= 0.0f)
                {
                    AddMissingRequiredId(result, $"passive:invalid:{data.Id}");
                }
            }
        }

        void AddFallbackPassive(
            string id, string category, string eligibleTarget, string effectId, string valueType,
            float level1Value, float level2Value, float level3Value, string stackRule,
            string titleKo, string titleEn, string descriptionTemplateKo, string offerWeightRule, string prohibition)
        {
            AddPassive(new PassiveData
            {
                Id = id, Category = category, EligibleTarget = eligibleTarget, EffectId = effectId, ValueType = valueType,
                Level1Value = level1Value, Level2Value = level2Value, Level3Value = level3Value, StackRule = stackRule,
                TitleKo = titleKo, TitleEn = titleEn, DescriptionTemplateKo = descriptionTemplateKo,
                OfferWeightRule = offerWeightRule, Prohibition = prohibition,
            });
        }
    }
}
