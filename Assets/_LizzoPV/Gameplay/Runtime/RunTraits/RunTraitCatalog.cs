using System;
using System.Collections.Generic;

namespace Lizzo.PV.Gameplay.RunTraits
{
    public static class RunTraitCatalog
    {
        static readonly RunTraitDefinition[] DefinitionsArray =
        {
            new RunTraitDefinition(RunTraitIds.FuseLink, "도화선 연결", RunTraitCategories.BuildRelated, "폭발 공격이 적에게 3초 도화선을 남기고 다른 폭발이 적중하면 작은 2차 폭발(원본 60%)이 발생한다.", "폭발단"),
            new RunTraitDefinition(RunTraitIds.MomentOfCompletion, "완성의 순간", RunTraitCategories.BuildRelated, "이번 출정에서 시너지가 처음 완성될 때 해당 시너지 쿨다운을 1회 초기화하고 즉시 완성 효과를 추가 발동한다.", "8개 시너지"),
            new RunTraitDefinition(RunTraitIds.PromotionShout, "진급의 함성", RunTraitCategories.General, "런 중 군단 진급 완료 시 생존 부대 전원이 5초간 공격속도+20%를 얻고 군단장 주위로 짧게 정렬한다.", "전체"),
            new RunTraitDefinition(RunTraitIds.EmergencyRally, "응급 집결", RunTraitCategories.General, "모든 생존 부대가 군단장 주변으로 재집결하고 4초간 이동속도+25%와 작은 보호막을 얻는다.", "전체"),
            new RunTraitDefinition(RunTraitIds.DangerousMarch, "위험한 행군", RunTraitCategories.Variant, "일반 적 밀도 +20%. 대신 처치 기반 시너지 카운터와 경험 획득이 +25% 빠르게 누적된다.", "폭발단·망자단"),
            new RunTraitDefinition(RunTraitIds.EliteFew, "소수 정예", RunTraitCategories.Variant, "빈 부대 슬롯 1칸마다 군단장 무기 재사용 대기시간 -5%. 새 군단 소집 시 보너스가 즉시 줄어든다.", "무기 중심 빌드"),
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
