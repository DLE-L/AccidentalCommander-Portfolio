using System;
using System.Collections.Generic;
using Lizzo.PV.Flow;
using Lizzo.PV.Legion;
using Lizzo.PV.Gameplay.CardOffer;
using UnityEngine;

namespace Lizzo.PV.UI
{
    internal static class RunResultViewDataResolver
    {
        internal static RunResultViewData Resolve(
            RunResult result,
            RunServices services,
            RunRewardSettlement settlement)
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
                services.App.Data,
                companionPresentations,
                passivePresentations,
                synergyPresentations,
                7,
                5,
                null);

            string partySummary = BuildPartySummary(party);
            RunResultSnapshotSet resultSnapshots = RunResultSnapshotResolver.Capture(services);
            bool hasCompletedSynergy = synergyPresentations.Count > 0;
            string synergySectionLabel = result.Outcome == RunOutcome.Clear
                ? "이번 클리어 우수 시너지"
                : "이번 런에서 완성한 시너지";
            string synergyName = result.Outcome == RunOutcome.Clear
                ? JoinSynergyDisplayNames(synergyPresentations)
                : JoinSynergyDisplayNames(synergyPresentations);
            bool isTutorialClear = result.Outcome == RunOutcome.Clear
                && services.Context.IsTutorial;
            string stageGroupLabel = services.Context.IsTutorial
                ? "튜토리얼"
                : $"챕터 {(int)services.Context.StageId}";
            string stageNameLabel = services.Context.StageId == CampaignStageId.Stage1
                ? "경계선의 망꾼"
                : $"스테이지 {(int)services.Context.StageId}";
            IReadOnlyList<RunResultRewardPresentation> rewardPresentations =
                ResolveRewardPresentations(settlement);

            if (result.Outcome == RunOutcome.Abandoned)
            {
                return new RunResultViewData(
                    false,
                    "전투 종료",
                    "획득 보상",
                    "1-1",
                    string.Empty,
                    "메인으로",
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
                    null,
                    services.CardOffers.MaxBuildComplete,
                    resultSnapshots.FinalLegion,
                    resultSnapshots.CompletedSynergies,
                    resultSnapshots.SelectedTraits,
                    stageGroupLabel,
                    stageNameLabel,
                    rewardPresentations,
                    outcome: result.Outcome);
            }

            return result.Outcome == RunOutcome.Clear
                ? new RunResultViewData(
                    true,
                    isTutorialClear ? "튜토리얼 완료" : "승리",
                    string.Empty,
                    isTutorialClear ? "튜토리얼" : "1-1",
                    string.Empty,
                    "메인으로",
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
                    null,
                    services.CardOffers.MaxBuildComplete,
                    resultSnapshots.FinalLegion,
                    resultSnapshots.CompletedSynergies,
                    resultSnapshots.SelectedTraits,
                    stageGroupLabel,
                    stageNameLabel,
                    rewardPresentations,
                    outcome: result.Outcome)
                : new RunResultViewData(
                    false,
                    "패배",
                    string.Empty,
                    "1-1",
                    string.Empty,
                    "메인으로",
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
                    null,
                    services.CardOffers.MaxBuildComplete,
                    resultSnapshots.FinalLegion,
                    resultSnapshots.CompletedSynergies,
                    resultSnapshots.SelectedTraits,
                    stageGroupLabel,
                    stageNameLabel,
                    rewardPresentations,
                    outcome: result.Outcome);
        }

        private static string BuildPartySummary(PartyService party)
        {
            string formationSummary = party.BuildLegionSummary();
            return string.IsNullOrWhiteSpace(formationSummary) || formationSummary == "군단"
                ? string.Empty
                : $"편성 {formationSummary}";
        }

        private static IReadOnlyList<RunResultRewardPresentation> ResolveRewardPresentations(
            RunRewardSettlement settlement)
        {
            if (settlement?.Grants == null || settlement.Grants.Count == 0)
                return Array.Empty<RunResultRewardPresentation>();

            List<RunResultRewardPresentation> result = new List<RunResultRewardPresentation>(settlement.Grants.Count);
            for (int index = 0; index < settlement.Grants.Count; index++)
            {
                RunRewardGrant grant = settlement.Grants[index];
                string displayName = grant.Kind switch
                {
                    AccountResourceKind.Gold => "골드",
                    AccountResourceKind.LegionScroll => "군단 스크롤",
                    _ => grant.Kind.ToString(),
                };
                if (!Gameplay.GameplayContentSpriteProvider.TryRewardIcon(grant.Kind.ToString(), out Sprite icon))
                {
                    UnityEngine.Debug.LogError($"[RunResultViewDataResolver] Missing reward Sprite Asset binding: {grant.Kind}");
                    continue;
                }
                result.Add(new RunResultRewardPresentation(grant.Kind, displayName, grant.Amount, icon));
            }

            return result;
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
