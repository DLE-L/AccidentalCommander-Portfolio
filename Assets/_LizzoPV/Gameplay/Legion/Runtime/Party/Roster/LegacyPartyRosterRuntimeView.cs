using System;
using System.Collections.Generic;

namespace Lizzo.PV.Legion.Party.Roster
{
    internal sealed class LegacyPartyRosterRuntimeView : IPartyRosterRuntimeView
    {
        private readonly PartyRosterState _roster;
        private readonly Func<int> _activeCompanionCount;

        internal LegacyPartyRosterRuntimeView(
            PartyRosterState roster,
            Func<int> activeCompanionCount)
        {
            _roster = roster ?? throw new ArgumentNullException(nameof(roster));
            _activeCompanionCount = activeCompanionCount
                ?? throw new ArgumentNullException(nameof(activeCompanionCount));
        }

        public int ActiveCompanionSlotCount => _roster.ActiveSquadCount;

        public int ActiveCompanionSlotCap => PartyRosterState.SlotCap;

        public int ActiveCompanionCount => Math.Max(0, _activeCompanionCount());

        public int PromotionReadyCount
        {
            get
            {
                IReadOnlyList<SquadSlotState> slots = _roster.Snapshot;
                int count = 0;
                for (int index = 0; index < slots.Count; index += 1)
                {
                    SquadSlotState slot = slots[index];
                    if (slot.IsActive
                        && !slot.IsPromoted
                        && slot.CurrentCount == slot.MaxCount - 1)
                    {
                        count += 1;
                    }
                }

                return count;
            }
        }

        public PartyRosterChangeResult PreviewCanonicalRecruit(string baseUnitId) =>
            _roster.PreviewAdd(baseUnitId);

        public IReadOnlyList<SquadSlotState> GetSquadSlotSnapshot() => _roster.Snapshot;

        public bool TryGetSlot(string baseUnitId, out SquadSlotState state) =>
            _roster.TryGetSlot(baseUnitId, out state);

        public bool TryGetCanonicalCompanionProgress(
            string baseUnitId,
            out int currentCount,
            out int previewCount)
        {
            currentCount = 0;
            previewCount = 0;
            PartyRosterChangeResult preview = _roster.PreviewAdd(baseUnitId);
            if (_roster.TryGetSlot(baseUnitId, out SquadSlotState state))
            {
                currentCount = state.CurrentCount;
                previewCount = preview == PartyRosterChangeResult.Reinforce
                    || preview == PartyRosterChangeResult.Promote
                        ? Math.Min(state.CurrentCount + 1, state.MaxCount)
                        : state.CurrentCount;
                return true;
            }

            if (preview != PartyRosterChangeResult.Recruit)
                return false;

            previewCount = 1;
            return true;
        }
    }
}
