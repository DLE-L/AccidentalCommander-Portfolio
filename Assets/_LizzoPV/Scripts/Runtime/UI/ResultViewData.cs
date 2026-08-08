using System;
using System.Collections.Generic;

namespace Lizzo.PV.UI
{
    public sealed class RunResultBestSynergyPresentation
    {
        public RunResultBestSynergyPresentation(string id, string displayName, int totalScore)
        {
            Id = id ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            TotalScore = Math.Max(0, totalScore);
        }

        public string Id { get; }
        public string DisplayName { get; }
        public int TotalScore { get; }
        public string SummaryText => string.IsNullOrWhiteSpace(DisplayName)
            ? "우수 시너지 없음"
            : $"{DisplayName} · 기여 {TotalScore}";
    }

    public sealed class RunResultSquadSlotView
    {
        public RunResultSquadSlotView(
            string slotId,
            string displayName,
            int iconIndex,
            int currentCount,
            bool isHighlighted)
        {
            SlotId = slotId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            IconIndex = iconIndex;
            CurrentCount = Math.Max(0, Math.Min(3, currentCount));
            IsHighlighted = isHighlighted;
        }

        public string SlotId { get; }
        public string DisplayName { get; }
        public int IconIndex { get; }
        public int CurrentCount { get; }
        public bool IsActive => CurrentCount > 0;
        public bool IsHighlighted { get; }
    }

    public sealed class RunResultViewData
    {
        public bool IsClear { get; }
        public string Title { get; }
        public string Headline { get; }
        public string StageLabel { get; }
        public string Body { get; }
        public string PrimaryButtonLabel { get; }
        public bool OptionalButtonVisible { get; }
        public string OptionalButtonLabel { get; }
        public float ElapsedSeconds { get; }
        public int KillCount { get; }
        public int Level { get; }
        public string PartySummary { get; }
        public string FailureCause { get; }
        public string Recommendation { get; }
        public bool HasCompletedSynergy { get; }
        public string SynergySectionLabel { get; }
        public string SynergyName { get; }
        public string SynergyMembers { get; }
        public string SynergyEffect { get; }
        public IReadOnlyList<int> SynergyIconIndices { get; }
        public IReadOnlyList<RunResultSquadSlotView> SquadSlots { get; }
        public IReadOnlyList<PauseCompanionPresentation> CompanionPresentations { get; }
        public IReadOnlyList<PausePassivePresentation> PassivePresentations { get; }
        public IReadOnlyList<PauseSynergyPresentation> SynergyPresentations { get; }
        public RunResultBestSynergyPresentation BestActiveSynergy { get; }
        public bool IsFinalBuildComplete { get; }

        public RunResultViewData(
            bool isClear,
            string title,
            string headline,
            string stageLabel,
            string body,
            string primaryButtonLabel,
            bool optionalButtonVisible,
            string optionalButtonLabel,
            float elapsedSeconds,
            int killCount,
            int level,
            string partySummary,
            string failureCause,
            string recommendation)
            : this(
                isClear, title, headline, stageLabel, body, primaryButtonLabel,
                optionalButtonVisible, optionalButtonLabel, elapsedSeconds, killCount,
                level, partySummary, failureCause, recommendation, false,
                string.Empty, "완성한 시너지 없음", string.Empty, string.Empty,
                Array.Empty<int>(), Array.Empty<RunResultSquadSlotView>(),
                Array.Empty<PauseCompanionPresentation>(),
                Array.Empty<PausePassivePresentation>(),
                Array.Empty<PauseSynergyPresentation>())
        {
        }

        public RunResultViewData(
            bool isClear,
            string title,
            string headline,
            string stageLabel,
            string body,
            string primaryButtonLabel,
            bool optionalButtonVisible,
            string optionalButtonLabel,
            float elapsedSeconds,
            int killCount,
            int level,
            string partySummary,
            string failureCause,
            string recommendation,
            int battleGold,
            bool hasCompletedSynergy,
            string synergySectionLabel,
            string synergyName,
            string synergyMembers,
            string synergyEffect,
            IReadOnlyList<int> synergyIconIndices,
            IReadOnlyList<RunResultSquadSlotView> squadSlots)
            : this(
                isClear, title, headline, stageLabel, body, primaryButtonLabel,
                optionalButtonVisible, optionalButtonLabel, elapsedSeconds, killCount,
                level, partySummary, failureCause, recommendation,
                hasCompletedSynergy, synergySectionLabel, synergyName, synergyMembers,
                synergyEffect, synergyIconIndices, squadSlots,
                Array.Empty<PauseCompanionPresentation>(),
                Array.Empty<PausePassivePresentation>(),
                Array.Empty<PauseSynergyPresentation>())
        {
        }

        public RunResultViewData(
            bool isClear,
            string title,
            string headline,
            string stageLabel,
            string body,
            string primaryButtonLabel,
            bool optionalButtonVisible,
            string optionalButtonLabel,
            float elapsedSeconds,
            int killCount,
            int level,
            string partySummary,
            string failureCause,
            string recommendation,
            bool hasCompletedSynergy,
            string synergySectionLabel,
            string synergyName,
            string synergyMembers,
            string synergyEffect,
            IReadOnlyList<int> synergyIconIndices,
            IReadOnlyList<RunResultSquadSlotView> squadSlots,
            IReadOnlyList<PauseCompanionPresentation> companionPresentations,
            IReadOnlyList<PausePassivePresentation> passivePresentations,
            IReadOnlyList<PauseSynergyPresentation> synergyPresentations,
            RunResultBestSynergyPresentation bestActiveSynergy = null,
            bool isFinalBuildComplete = false)
        {
            IsClear = isClear;
            Title = title ?? string.Empty;
            Headline = headline ?? string.Empty;
            StageLabel = stageLabel ?? string.Empty;
            Body = body ?? string.Empty;
            PrimaryButtonLabel = primaryButtonLabel ?? string.Empty;
            OptionalButtonVisible = optionalButtonVisible;
            OptionalButtonLabel = optionalButtonLabel ?? string.Empty;
            ElapsedSeconds = elapsedSeconds;
            KillCount = killCount;
            Level = level;
            PartySummary = partySummary ?? string.Empty;
            FailureCause = failureCause ?? string.Empty;
            Recommendation = recommendation ?? string.Empty;
            HasCompletedSynergy = hasCompletedSynergy;
            SynergySectionLabel = synergySectionLabel ?? string.Empty;
            SynergyName = synergyName ?? string.Empty;
            SynergyMembers = synergyMembers ?? string.Empty;
            SynergyEffect = synergyEffect ?? string.Empty;
            SynergyIconIndices = Copy(synergyIconIndices);
            SquadSlots = Copy(squadSlots);
            CompanionPresentations = Copy(companionPresentations);
            PassivePresentations = Copy(passivePresentations);
            SynergyPresentations = Copy(synergyPresentations);
            BestActiveSynergy = bestActiveSynergy;
            IsFinalBuildComplete = isFinalBuildComplete;
        }

        public RunResultViewData(
            bool isClear,
            string title,
            string headline,
            string stageLabel,
            string body,
            string primaryButtonLabel,
            bool optionalButtonVisible,
            string optionalButtonLabel,
            float elapsedSeconds,
            int killCount,
            int level,
            string partySummary,
            string failureCause,
            string recommendation,
            int _,
            bool hasCompletedSynergy,
            string synergySectionLabel,
            string synergyName,
            string synergyMembers,
            string synergyEffect,
            IReadOnlyList<int> synergyIconIndices,
            IReadOnlyList<RunResultSquadSlotView> squadSlots,
            IReadOnlyList<PauseCompanionPresentation> companionPresentations,
            IReadOnlyList<PausePassivePresentation> passivePresentations,
            IReadOnlyList<PauseSynergyPresentation> synergyPresentations,
            RunResultBestSynergyPresentation bestActiveSynergy = null,
            bool isFinalBuildComplete = false)
            : this(
                isClear, title, headline, stageLabel, body, primaryButtonLabel,
                optionalButtonVisible, optionalButtonLabel, elapsedSeconds, killCount,
                level, partySummary, failureCause, recommendation,
                hasCompletedSynergy, synergySectionLabel, synergyName, synergyMembers,
                synergyEffect, synergyIconIndices, squadSlots, companionPresentations,
                passivePresentations, synergyPresentations, bestActiveSynergy,
                isFinalBuildComplete)
        {
        }

        private static IReadOnlyList<int> Copy(IReadOnlyList<int> values)
        {
            if (values == null || values.Count == 0)
                return Array.Empty<int>();
            int[] copy = new int[values.Count];
            for (int i = 0; i < values.Count; i++)
                copy[i] = values[i];
            return copy;
        }

        private static IReadOnlyList<RunResultSquadSlotView> Copy(IReadOnlyList<RunResultSquadSlotView> values)
        {
            if (values == null || values.Count == 0)
                return Array.Empty<RunResultSquadSlotView>();
            RunResultSquadSlotView[] copy = new RunResultSquadSlotView[values.Count];
            for (int i = 0; i < values.Count; i++)
                copy[i] = values[i];
            return copy;
        }

        private static IReadOnlyList<PauseCompanionPresentation> Copy(IReadOnlyList<PauseCompanionPresentation> values)
        {
            if (values == null || values.Count == 0)
                return Array.Empty<PauseCompanionPresentation>();
            PauseCompanionPresentation[] copy = new PauseCompanionPresentation[values.Count];
            for (int i = 0; i < values.Count; i++)
                copy[i] = values[i];
            return copy;
        }

        private static IReadOnlyList<PausePassivePresentation> Copy(IReadOnlyList<PausePassivePresentation> values)
        {
            if (values == null || values.Count == 0)
                return Array.Empty<PausePassivePresentation>();
            PausePassivePresentation[] copy = new PausePassivePresentation[values.Count];
            for (int i = 0; i < values.Count; i++)
                copy[i] = values[i];
            return copy;
        }

        private static IReadOnlyList<PauseSynergyPresentation> Copy(IReadOnlyList<PauseSynergyPresentation> values)
        {
            if (values == null || values.Count == 0)
                return Array.Empty<PauseSynergyPresentation>();
            PauseSynergyPresentation[] copy = new PauseSynergyPresentation[values.Count];
            for (int i = 0; i < values.Count; i++)
                copy[i] = values[i];
            return copy;
        }
    }
}
