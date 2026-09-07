using System.Collections.Generic;

namespace Lizzo.PV.Legion.Party.Roster
{
    public enum PartyRosterChangeResult
    {
        Recruit,
        Reinforce,
        Promote,
        RejectedUnknown,
        RejectedFull,
        RejectedMaxed,
    }

    public interface IPartyRosterRuntimeView : ICanonicalCompanionRosterView
    {
        int ActiveCompanionCount { get; }

        int PromotionReadyCount { get; }

        IReadOnlyList<SquadSlotState> GetSquadSlotSnapshot();

        bool TryGetSlot(string baseUnitId, out SquadSlotState state);

        bool TryGetCanonicalCompanionProgress(
            string baseUnitId,
            out int currentCount,
            out int previewCount);
    }
}
