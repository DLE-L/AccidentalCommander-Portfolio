using System.Collections.Generic;
using Lizzo.PV.Legion.Party.Roster;
using Lizzo.PV.P0.Telemetry;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    public static class PartySquadSlots
    {
        internal const int SQUAD_FAMILY_SLOT_CAP = PartyRosterState.SlotCap;

        internal static int GetActiveSquadFamilySlotCount(this PartyService party) => party.ActiveCompanionSlotCount;

        public static IReadOnlyList<SquadSlotState> GetSquadSlotSnapshot(this PartyService party) => party.GetSquadSlotSnapshot();

        public static bool TryGetSquadSlotForCompanion(this PartyService party, CompanionKind kind, out SquadSlotState state)
        {
            return party.TryGetSquadSlotForCompanion(kind, out state);
        }

        public static int PreviewSquadSlotCountAfterRecruit(this PartyService party, CompanionKind kind)
        {
            return party.PreviewSquadSlotCountAfterRecruit(kind);
        }

        public static void LogActiveSquadSlotState(this PartyService party, string reason)
        {
            P0Telemetry.Log(P0Telemetry.ActiveSquadSlotStateUpdate, party.BuildSquadSlotStateParameters(reason));
        }

        public static string[] BuildSquadSlotStateParameters(this PartyService party, string reason)
        {
            IReadOnlyList<SquadSlotState> snapshot = party.GetSquadSlotSnapshot();
            string[] parameters = new string[snapshot.Count + 4];
            parameters[0] = $"reason={reason}";
            parameters[1] = "slot_model=base_unit";
            parameters[2] = $"squad_slots_used={party.ActiveCompanionSlotCount}";
            parameters[3] = $"squad_slots_cap={party.ActiveCompanionSlotCap}";
            for (int i = 0; i < snapshot.Count; i++)
                parameters[i + 4] = BuildSquadSlotParameter(snapshot[i]);

            return parameters;
        }

        static string BuildSquadSlotParameter(SquadSlotState state)
        {
            string promoted = state.IsPromoted ? ":promoted" : string.Empty;
            return $"{state.SlotId}:{state.BaseUnitId}={state.CurrentCount}/{state.MaxCount}{promoted}";
        }
    }
}
