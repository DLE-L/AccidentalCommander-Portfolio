using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Lizzo.PV.Combat;

namespace Lizzo.PV.P0.Telemetry
{
    public static class DamageContributionSummaryTelemetry
    {
        public static void Emit(
            string result,
            DamageContributionSnapshot snapshot,
            Action<string, string> emit)
        {
            if (emit == null)
                throw new ArgumentNullException(nameof(emit));

            emit(P0Telemetry.SynergyContributionSummary, FormatSynergySummary(result, snapshot));
            emit(P0Telemetry.CompanionDamageContributionSummary, FormatCompanionSummary(result, snapshot));
        }

        public static string FormatSynergySummary(string result, DamageContributionSnapshot snapshot)
        {
            StringBuilder text = new StringBuilder(1024);
            text.Append("{\"result\":");
            AppendJsonString(text, result);
            text.Append(",\"winner_id\":");
            AppendJsonString(text, GetWinnerId(snapshot));
            text.Append(",\"synergies\":[");

            for (int index = 0; index < DamageContributionLedger.CanonicalSynergyIds.Count; index++)
            {
                if (index > 0)
                    text.Append(',');

                string id = DamageContributionLedger.CanonicalSynergyIds[index];
                DamageContributionEntry entry = FindEntry(snapshot?.SynergyEntries, id);
                text.Append("{\"id\":");
                AppendJsonString(text, id);
                text.Append(",\"direct_damage\":");
                AppendInteger(text, entry.DirectDamage);
                text.Append(",\"prevented_damage\":");
                AppendInteger(text, entry.PreventedDamage);
                text.Append(",\"attributed_mixed_command_bonus_damage\":");
                AppendInteger(text, entry.AttributedBonusDamage);
                text.Append(",\"total_score\":");
                AppendInteger(text, entry.TotalScore);
                text.Append(",\"active\":");
                text.Append(entry.IsActive ? "true" : "false");
                text.Append('}');
            }

            text.Append("]}");
            return text.ToString();
        }

        public static string FormatCompanionSummary(string result, DamageContributionSnapshot snapshot)
        {
            StringBuilder text = new StringBuilder(512);
            text.Append("{\"result\":");
            AppendJsonString(text, result);
            text.Append(",\"companions\":[");

            IReadOnlyList<DamageContributionEntry> entries = snapshot?.CompanionEntries;
            if (entries != null)
            {
                for (int index = 0; index < entries.Count; index++)
                {
                    if (index > 0)
                        text.Append(',');

                    DamageContributionEntry entry = entries[index];
                    text.Append("{\"id\":");
                    AppendJsonString(text, entry.Id);
                    text.Append(",\"applied_hp_damage\":");
                    AppendInteger(text, entry.Damage);
                    text.Append('}');
                }
            }

            text.Append("]}");
            return text.ToString();
        }

        private static string GetWinnerId(DamageContributionSnapshot snapshot)
        {
            return snapshot?.BestActiveSynergy.HasValue == true
                ? snapshot.BestActiveSynergy.Value.Id
                : "none";
        }

        private static DamageContributionEntry FindEntry(
            IReadOnlyList<DamageContributionEntry> entries,
            string id)
        {
            if (entries != null)
            {
                for (int index = 0; index < entries.Count; index++)
                    if (entries[index].Id == id)
                        return entries[index];
            }

            return new DamageContributionEntry(id, 0);
        }

        private static void AppendInteger(StringBuilder text, int value)
        {
            text.Append(value.ToString(CultureInfo.InvariantCulture));
        }

        private static void AppendJsonString(StringBuilder text, string value)
        {
            text.Append('"');
            if (value != null)
            {
                for (int index = 0; index < value.Length; index++)
                {
                    char character = value[index];
                    switch (character)
                    {
                        case '\\':
                            text.Append("\\\\");
                            break;
                        case '"':
                            text.Append("\\\"");
                            break;
                        case '\n':
                            text.Append("\\n");
                            break;
                        case '\r':
                            text.Append("\\r");
                            break;
                        case '\t':
                            text.Append("\\t");
                            break;
                        default:
                            text.Append(character);
                            break;
                    }
                }
            }

            text.Append('"');
        }
    }
}
