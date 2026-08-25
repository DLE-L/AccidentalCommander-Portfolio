using Lizzo.PV.P0.Telemetry;

namespace Lizzo.PV.Legion
{
    public sealed partial class PartyService
    {
        public void LogActiveSlotState(string reason)
        {
            P0Telemetry.Log(P0Telemetry.ActiveSlotStateUpdate, BuildSlotStateParameters(reason));

            bool isFull = IsCompanionSlotFull;
            if (isFull && WasSlotFullState == false)
                P0Telemetry.Log(P0Telemetry.CompanionSlotFull, BuildSlotStateParameters(reason));

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
                $"synergy_ready_count={SynergyReadyCount}",
            };
        }

        public void LogActiveSquadSlotState(string reason) => PartySquadSlots.LogActiveSquadSlotState(this, reason);

        public string[] BuildSquadSlotStateParameters(string reason) => PartySquadSlots.BuildSquadSlotStateParameters(this, reason);

        public string BuildLegionSummary()
        {
            string summary = "군단";
            summary = AppendUnitSummary(summary, "방패대장", ShieldCaptainCountState);
            summary = AppendUnitSummary(summary, "방패병", ShieldSoldierCountState);
            summary = AppendUnitSummary(summary, "검병", SwordsmanCountState);
            summary = AppendUnitSummary(summary, "성직자", ClericCountState);
            summary = AppendUnitSummary(summary, "궁수", ArcherCountState);
            return summary;
        }

        private static string AppendUnitSummary(string summary, string label, int count)
        {
            return count <= 0 ? summary : $"{summary} / {label} x{count}";
        }
    }
}
