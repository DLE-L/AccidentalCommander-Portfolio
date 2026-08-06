using System;
using System.Collections.Generic;

namespace Lizzo.PV.Gameplay.RunTraits
{
    public static class RunTraitCatalog
    {
        static readonly RunTraitDefinition[] DefinitionsArray =
        {
            new RunTraitDefinition(RunTraitIds.FuseLink, "도화선 연결", RunTraitCategories.BuildRelated),
            new RunTraitDefinition(RunTraitIds.MomentOfCompletion, "완성의 순간", RunTraitCategories.BuildRelated),
            new RunTraitDefinition(RunTraitIds.PromotionShout, "진급의 함성", RunTraitCategories.General),
            new RunTraitDefinition(RunTraitIds.EmergencyRally, "응급 집결", RunTraitCategories.General),
            new RunTraitDefinition(RunTraitIds.DangerousMarch, "위험한 행군", RunTraitCategories.Variant),
            new RunTraitDefinition(RunTraitIds.EliteFew, "소수 정예", RunTraitCategories.Variant),
        };

        static readonly IReadOnlyList<RunTraitDefinition> DefinitionView = Array.AsReadOnly(DefinitionsArray);
        static readonly Dictionary<string, RunTraitDefinition> DefinitionsById = CreateDefinitionsById();

        public static IReadOnlyList<RunTraitDefinition> Definitions => DefinitionView;

        public static bool TryGet(string traitId, out RunTraitDefinition definition)
        {
            if (string.IsNullOrWhiteSpace(traitId))
            {
                definition = null;
                return false;
            }

            return DefinitionsById.TryGetValue(traitId, out definition);
        }

        public static bool Contains(string traitId)
        {
            return TryGet(traitId, out _);
        }

        static Dictionary<string, RunTraitDefinition> CreateDefinitionsById()
        {
            var definitions = new Dictionary<string, RunTraitDefinition>(DefinitionsArray.Length, StringComparer.Ordinal);
            for (int i = 0; i < DefinitionsArray.Length; i++)
            {
                RunTraitDefinition definition = DefinitionsArray[i];
                definitions.Add(definition.Id, definition);
            }

            return definitions;
        }
    }
}
