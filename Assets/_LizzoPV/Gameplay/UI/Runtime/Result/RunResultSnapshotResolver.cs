using System;
using System.Collections.Generic;
using Lizzo.PV.Data;
using Lizzo.PV.Gameplay.RunTraits;
using Lizzo.PV.Legion;
using Lizzo.PV.Legion.Synergy;

namespace Lizzo.PV.UI
{
    internal sealed class RunResultSnapshotSet
    {
        internal RunResultSnapshotSet(
            IReadOnlyList<RunResultSquadSlotView> squadSlots,
            IReadOnlyList<RunResultCompanionSnapshot> finalLegion,
            IReadOnlyList<RunResultSynergySnapshot> completedSynergies,
            IReadOnlyList<RunResultTraitSnapshot> selectedTraits)
        {
            SquadSlots = squadSlots;
            FinalLegion = finalLegion;
            CompletedSynergies = completedSynergies;
            SelectedTraits = selectedTraits;
        }

        public IReadOnlyList<RunResultSquadSlotView> SquadSlots { get; }
        public IReadOnlyList<RunResultCompanionSnapshot> FinalLegion { get; }
        public IReadOnlyList<RunResultSynergySnapshot> CompletedSynergies { get; }
        public IReadOnlyList<RunResultTraitSnapshot> SelectedTraits { get; }
    }

    internal static class RunResultSnapshotResolver
    {
        internal static RunResultSnapshotSet Capture(RunServices services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            PartyService party = services.Party;
            return new RunResultSnapshotSet(
                BuildSquadSlots(party),
                BuildFinalLegion(party),
                BuildCompletedSynergies(services.Synergies, services.Build1SynergyProgression, services.App.Data),
                BuildSelectedTraits(services.RunTraits));
        }

        private static IReadOnlyList<RunResultSquadSlotView> BuildSquadSlots(PartyService party)
        {
            const int slotCount = 7;
            List<RunResultSquadSlotView> result = new List<RunResultSquadSlotView>(slotCount);
            IReadOnlyList<SquadSlotState> snapshot = party?.GetSquadSlotSnapshot();
            for (int index = 0; index < slotCount; index++)
            {
                SquadSlotState state = snapshot != null && index < snapshot.Count
                    ? snapshot[index]
                    : default;
                result.Add(new RunResultSquadSlotView(
                    string.IsNullOrWhiteSpace(state.SlotId) ? $"slot_{index:00}" : state.SlotId,
                    string.IsNullOrWhiteSpace(state.DisplayName) ? "빈 슬롯" : state.DisplayName,
                    -1,
                    state.CurrentCount,
                    false));
            }

            return result;
        }

        private static IReadOnlyList<RunResultCompanionSnapshot> BuildFinalLegion(PartyService party)
        {
            IReadOnlyList<SquadSlotState> slots = party?.GetSquadSlotSnapshot();
            if (slots == null || slots.Count == 0)
                return Array.Empty<RunResultCompanionSnapshot>();

            List<RunResultCompanionSnapshot> result = new List<RunResultCompanionSnapshot>(slots.Count);
            for (int index = 0; index < slots.Count; index++)
            {
                SquadSlotState slot = slots[index];
                if (slot.IsActive == false)
                    continue;

                result.Add(new RunResultCompanionSnapshot(
                    slot.BaseUnitId,
                    slot.DisplayName,
                    slot.CurrentCount,
                    slot.IsPromoted,
                    slot.IsPromoted ? slot.LeaderUnitId : string.Empty));
            }

            return result;
        }

        private static IReadOnlyList<RunResultSynergySnapshot> BuildCompletedSynergies(
            SynergyActivationState synergies,
            Build1SynergyProgression progression,
            IDataProvider data)
        {
            IReadOnlyList<SynergyActivationSnapshot> snapshots = synergies?.Snapshot;
            if (snapshots == null || snapshots.Count == 0)
                return Array.Empty<RunResultSynergySnapshot>();

            List<RunResultSynergySnapshot> result = new List<RunResultSynergySnapshot>(snapshots.Count);
            for (int index = 0; index < snapshots.Count; index++)
            {
                SynergyActivationSnapshot snapshot = snapshots[index];
                if (snapshot.IsActive == false)
                    continue;

                SynergyData synergy = data?.GetSynergy(snapshot.SynergyId);
                result.Add(new RunResultSynergySnapshot(
                    snapshot.SynergyId,
                    synergy?.DisplayName,
                    progression == null ? Build1SynergyStage.None.ToString() : progression.GetStage(snapshot.SynergyId).ToString(),
                    true));
            }

            return result;
        }

        private static IReadOnlyList<RunResultTraitSnapshot> BuildSelectedTraits(RunTraitRunState runTraits)
        {
            RunTraitRunStateSnapshot snapshot = runTraits?.CaptureSnapshot();
            if (snapshot == null || snapshot.SelectedTraitIds.Count == 0)
                return Array.Empty<RunResultTraitSnapshot>();

            List<RunResultTraitSnapshot> result = new List<RunResultTraitSnapshot>(snapshot.SelectedTraitIds.Count);
            for (int index = 0; index < snapshot.SelectedTraitIds.Count; index++)
            {
                string traitId = snapshot.SelectedTraitIds[index];
                string displayName = RunTraitCatalog.TryGet(traitId, out RunTraitDefinition definition)
                    ? definition.DisplayName
                    : string.Empty;
                result.Add(new RunResultTraitSnapshot(traitId, displayName, index + 1));
            }

            return result;
        }
    }
}
