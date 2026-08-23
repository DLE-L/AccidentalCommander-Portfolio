using System;

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

        private static string AppendUnitSummary(string summary, string label, int count)
        {
            return count <= 0 ? summary : $"{summary} / {label} x{count}";
        }
    }
}
