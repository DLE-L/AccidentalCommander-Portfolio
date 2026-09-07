using System;
using System.Collections.Generic;
using Lizzo.PV.Gameplay.Run;
using Lizzo.PV.Legion;

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
                BuildCompletedSynergies(services.ProductionSynergies),
                Array.Empty<RunResultTraitSnapshot>());
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
            CompanionSynergyProductionHost synergies)
        {
            if (synergies == null)
                return Array.Empty<RunResultSynergySnapshot>();

            SynergyRuntimeSnapshot snapshot = synergies.CurrentSnapshot;
            List<RunResultSynergySnapshot> result = new List<RunResultSynergySnapshot>(snapshot.SynergyCount);
            for (int index = 0; index < snapshot.SynergyCount; index++)
            {
                SynergyStateSnapshot state = snapshot.GetSynergyAt(index);
                if (state.IsActive == false)
                    continue;

                result.Add(new RunResultSynergySnapshot(
                    state.SynergyId,
                    string.Empty,
                    state.Tier.ToString(),
                    true));
            }

            return result;
        }

    }
}
