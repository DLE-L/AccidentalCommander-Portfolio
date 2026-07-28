using System;
using System.Collections.Generic;

namespace Lizzo.PV.UI
{
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
        public int BattleGold { get; }
        public bool HasCompletedSynergy { get; }
        public string SynergySectionLabel { get; }
        public string SynergyName { get; }
        public string SynergyMembers { get; }
        public string SynergyEffect { get; }
        public IReadOnlyList<int> SynergyIconIndices { get; }
        public IReadOnlyList<RunResultSquadSlotView> SquadSlots { get; }

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
                isClear,
                title,
                headline,
                stageLabel,
                body,
                primaryButtonLabel,
                optionalButtonVisible,
                optionalButtonLabel,
                elapsedSeconds,
                killCount,
                level,
                partySummary,
                failureCause,
                recommendation,
                0,
                false,
                string.Empty,
                "완성한 시너지 없음",
                string.Empty,
                string.Empty,
                Array.Empty<int>(),
                Array.Empty<RunResultSquadSlotView>())
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
            BattleGold = Math.Max(0, battleGold);
            HasCompletedSynergy = hasCompletedSynergy;
            SynergySectionLabel = synergySectionLabel ?? string.Empty;
            SynergyName = synergyName ?? string.Empty;
            SynergyMembers = synergyMembers ?? string.Empty;
            SynergyEffect = synergyEffect ?? string.Empty;
            SynergyIconIndices = Copy(synergyIconIndices);
            SquadSlots = Copy(squadSlots);
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
    }
}
