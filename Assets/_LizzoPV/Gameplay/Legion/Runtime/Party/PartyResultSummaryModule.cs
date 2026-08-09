using System;
using System.Collections.Generic;
using Lizzo.PV.Data;

namespace Lizzo.PV.Legion
{
    internal sealed class PartyResultSummaryModule
    {
        private readonly PartyService _party;

        internal PartyResultSummaryModule(PartyService party)
        {
            _party = party ?? throw new ArgumentNullException(nameof(party));
        }

        internal string BuildLegionSummary()
        {
            string summary = "군단";
            summary = AppendUnitSummary(summary, "방패대장", _party.ShieldCaptainCountState);
            summary = AppendUnitSummary(summary, "방패병", _party.ShieldSoldierCountState);
            summary = AppendUnitSummary(summary, "검병", _party.SwordsmanCountState);
            summary = AppendUnitSummary(summary, "성직자", _party.ClericCountState);
            summary = AppendUnitSummary(summary, "궁수", _party.ArcherCountState);
            return summary;
        }

        internal string GetCompletedSynergySummary() => _party.GuardSquadActivatedState ? "근위대" : "없음";

        internal void FillCompletedSynergyIds(List<string> destination)
        {
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));

            destination.Clear();
            if (_party.GuardSquadActivatedState)
                destination.Add("guard_squad");
        }

        internal bool TryGetSynergyDisplayName(string synergyId, out string displayName)
        {
            displayName = string.Empty;
            if (string.IsNullOrWhiteSpace(synergyId))
                return false;

            SynergyData synergy = _party.Data.GetSynergy(synergyId);
            if (synergy == null || string.IsNullOrWhiteSpace(synergy.DisplayName))
                return false;

            displayName = synergy.DisplayName;
            return true;
        }

        internal string GetMvpCompanionSummary()
        {
            if (_party.ShieldCaptainCountState > 0)
                return "방패대장";
            if (_party.ClericCountState > 0)
                return "성직자";
            if (_party.SwordsmanCountState > 0)
                return "검병";
            if (_party.ShieldSoldierCountState > 0)
                return "방패병";
            if (_party.ArcherCountState > 0)
                return "궁수";

            return "군단장";
        }

        private static string AppendUnitSummary(string summary, string label, int count)
        {
            return count <= 0 ? summary : $"{summary} / {label} x{count}";
        }
    }
}
