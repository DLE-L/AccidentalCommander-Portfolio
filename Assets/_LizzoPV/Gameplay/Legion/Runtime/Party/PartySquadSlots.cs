using System.Collections.Generic;
using Lizzo.PV.Legion.Party.Roster;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    public sealed partial class PartyService
    {
        public int ActiveCompanionSlotCount => RosterView.ActiveCompanionSlotCount;
        public int ActiveCompanionSlotCap => RosterView.ActiveCompanionSlotCap;
        public int FreeCompanionSlots => Mathf.Max(0, ActiveCompanionSlotCap - ActiveCompanionSlotCount);
        public bool IsCompanionSlotFull => ActiveCompanionSlotCount >= ActiveCompanionSlotCap;
        public int ActiveCompanionCount => RosterView.ActiveCompanionCount;
        public int PromotionReadyCount => RosterView.PromotionReadyCount;
        public int ActiveSquadFamilySlotCount => ActiveCompanionSlotCount;

        public PartyRosterChangeResult PreviewCanonicalRecruit(string baseUnitId) =>
            RosterView.PreviewCanonicalRecruit(baseUnitId);

        public IReadOnlyList<SquadSlotState> GetSquadSlotSnapshot() =>
            RosterView.GetSquadSlotSnapshot();

        public bool TryGetCanonicalCompanionProgress(string baseUnitId, out int ownedCount, out int previewCount) =>
            RosterView.TryGetCanonicalCompanionProgress(baseUnitId, out ownedCount, out previewCount);
    }

}
