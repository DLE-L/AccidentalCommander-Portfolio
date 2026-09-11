using System.Collections.Generic;
using Lizzo.PV.Gameplay.Telemetry;

namespace Lizzo.PV.Legion
{
    public sealed partial class PartyService
    {
        public void LogActiveSlotState(string reason)
        {
            RunTelemetry.Log(RunTelemetry.ActiveSlotStateUpdate, BuildSlotStateParameters(reason));

            bool isFull = IsCompanionSlotFull;
            if (isFull && WasSlotFullState == false)
                RunTelemetry.Log(RunTelemetry.CompanionSlotFull, BuildSlotStateParameters(reason));

            WasSlotFullState = isFull;
        }

        public string[] BuildSlotStateParameters(string reason)
        {
            return new[]
            {
                $"reason={reason}",
                $"slot_used={ActiveCompanionSlotCount}",
                $"slot_cap={ActiveCompanionSlotCap}",
                $"free_slots={FreeCompanionSlots}",
                $"promotion_ready_count={PromotionReadyCount}",
            };
        }

        public void LogActiveSquadSlotState(string reason)
        {
            RunTelemetry.Log(RunTelemetry.ActiveSquadSlotStateUpdate, BuildSquadSlotStateParameters(reason));
        }

        public string[] BuildSquadSlotStateParameters(string reason)
        {
            IReadOnlyList<SquadSlotState> snapshot = GetSquadSlotSnapshot();
            string[] parameters = new string[snapshot.Count + 4];
            parameters[0] = $"reason={reason}";
            parameters[1] = "slot_model=base_unit";
            parameters[2] = $"squad_slots_used={ActiveCompanionSlotCount}";
            parameters[3] = $"squad_slots_cap={ActiveCompanionSlotCap}";
            for (int i = 0; i < snapshot.Count; i++)
                parameters[i + 4] = BuildSquadSlotParameter(snapshot[i]);

            return parameters;
        }

        public string BuildLegionSummary()
        {
            string summary = "군단";
            IReadOnlyList<SquadSlotState> snapshot = GetSquadSlotSnapshot();
            for (int index = 0; index < snapshot.Count; index++)
            {
                SquadSlotState state = snapshot[index];
                if (state.IsActive)
                    summary = AppendUnitSummary(summary, state.DisplayName, state.CurrentCount);
            }
            return summary;
        }

        private static string AppendUnitSummary(string summary, string label, int count)
        {
            return count <= 0 ? summary : $"{summary} / {label} x{count}";
        }

        private static string BuildSquadSlotParameter(SquadSlotState state)
        {
            string promoted = state.IsPromoted ? ":promoted" : string.Empty;
            return $"{state.SlotId}:{state.BaseUnitId}={state.CurrentCount}/{state.MaxCount}{promoted}";
        }
    }
}
