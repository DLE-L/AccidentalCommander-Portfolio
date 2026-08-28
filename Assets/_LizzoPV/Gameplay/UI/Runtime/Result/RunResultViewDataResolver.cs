using System;
using System.Collections.Generic;
using Lizzo.PV.Combat;
using Lizzo.PV.Data;
using Lizzo.PV.Flow;
using Lizzo.PV.Legion;
using Lizzo.PV.P0.Cards;
using UnityEngine;

namespace Lizzo.PV.UI
{
    internal static class RunResultViewDataResolver
    {
        internal static RunResultViewData Resolve(
            RunResult result,
            RunServices services,
            DamageContributionSnapshot contributionSnapshot,
            UnityEngine.Object context)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            PartyService party = services.Party;
            List<PauseCompanionPresentation> companionPresentations = new List<PauseCompanionPresentation>(7);
            List<PausePassivePresentation> passivePresentations = new List<PausePassivePresentation>(5);
            List<PauseSynergyPresentation> synergyPresentations = new List<PauseSynergyPresentation>(8);
            PauseBuildSummaryPresentationResolver.Fill(
                party.GetSquadSlotSnapshot(),
                services.PassiveRoster,
                services.Synergies,
                services.App.Data,
                companionPresentations,
                passivePresentations,
                synergyPresentations,
                7,
                5,
                context);

            string partySummary = BuildPartySummary(party);
            RunResultSnapshotSet resultSnapshots = RunResultSnapshotResolver.Capture(services);
            RunResultBestSynergyPresentation bestActiveSynergy = result.Outcome == RunOutcome.Clear
                ? ResolveBestActiveSynergy(contributionSnapshot, services.App.Data, context)
                : null;
            bool hasCompletedSynergy = synergyPresentations.Count > 0;
            string synergySectionLabel = result.Outcome == RunOutcome.Clear
                ? "이번 클리어 우수 시너지"
                : "이번 런에서 완성한 시너지";
            string synergyName = result.Outcome == RunOutcome.Clear
                ? (bestActiveSynergy?.SummaryText ?? "우수 시너지 없음")
                : JoinSynergyDisplayNames(synergyPresentations);
            RunResultPresentation presentation = services.Definition.ResultPresentation;

            return result.Outcome == RunOutcome.Clear
                ? new RunResultViewData(
                    true,
                    presentation.ClearTitle,
                    string.Empty,
                    presentation.StageLabel,
                    string.Empty,
                    presentation.ClearPrimaryLabel,
                    false,
                    string.Empty,
                    result.ElapsedSeconds,
                    result.KillCount,
                    services.State.Level,
                    partySummary,
                    string.Empty,
                    string.Empty,
                    hasCompletedSynergy,
                    synergySectionLabel,
                    synergyName,
                    string.Empty,
                    string.Empty,
                    Array.Empty<int>(),
                    resultSnapshots.SquadSlots,
                    companionPresentations,
                    passivePresentations,
                    synergyPresentations,
                    bestActiveSynergy,
                    FixedCardPool.MaxBuildComplete,
                    resultSnapshots.FinalLegion,
                    resultSnapshots.CompletedSynergies,
                    resultSnapshots.SelectedTraits)
                : new RunResultViewData(
                    false,
                    "쓰러졌습니다",
                    "이번 전투 기록",
                    "1-1",
                    "다시 전장에 들어가 준비를 이어가세요.",
                    "다시 도전",
                    false,
                    string.Empty,
                    result.ElapsedSeconds,
                    result.KillCount,
                    services.State.Level,
                    partySummary,
                    "사령관이 전투 중 쓰러졌습니다.",
                    "동료를 모아 강화하세요.",
                    hasCompletedSynergy,
                    synergySectionLabel,
                    synergyName,
                    string.Empty,
                    string.Empty,
                    Array.Empty<int>(),
                    resultSnapshots.SquadSlots,
                    companionPresentations,
                    passivePresentations,
                    synergyPresentations,
                    null,
                    FixedCardPool.MaxBuildComplete,
                    resultSnapshots.FinalLegion,
                    resultSnapshots.CompletedSynergies,
                    resultSnapshots.SelectedTraits);
        }

        private static string BuildPartySummary(PartyService party)
        {
            string formationSummary = party.BuildLegionSummary();
            return string.IsNullOrWhiteSpace(formationSummary) || formationSummary == "군단"
                ? string.Empty
                : $"편성 {formationSummary}";
        }

        private static RunResultBestSynergyPresentation ResolveBestActiveSynergy(
            DamageContributionSnapshot snapshot,
            IDataProvider data,
            UnityEngine.Object context)
        {
            DamageContributionEntry? best = snapshot?.BestActiveSynergy;
            if (best.HasValue == false)
                return null;

            SynergyData synergy = data?.GetSynergy(best.Value.Id);
            if (synergy == null || string.IsNullOrWhiteSpace(synergy.DisplayName))
            {
                Debug.LogError($"[GameScene] Missing canonical synergy display data: {best.Value.Id}", context);
                return null;
            }

            return new RunResultBestSynergyPresentation(best.Value.Id, synergy.DisplayName, best.Value.TotalScore);
        }

        private static string JoinSynergyDisplayNames(IReadOnlyList<PauseSynergyPresentation> synergies)
        {
            if (synergies == null || synergies.Count == 0)
                return "활성 시너지 없음";

            System.Text.StringBuilder result = new System.Text.StringBuilder(64);
            for (int index = 0; index < synergies.Count; index++)
            {
                if (string.IsNullOrWhiteSpace(synergies[index].DisplayName))
                    continue;
                if (result.Length > 0)
                    result.Append(" · ");
                result.Append(synergies[index].DisplayName);
            }

            return result.Length == 0 ? "활성 시너지 없음" : result.ToString();
        }
    }
}
