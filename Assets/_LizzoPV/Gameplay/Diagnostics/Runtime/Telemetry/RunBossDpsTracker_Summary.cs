using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Lizzo.PV.Gameplay.Telemetry
{
    public static partial class RunBossDpsTracker
    {
        private static readonly StringBuilder SummaryBuilder = new StringBuilder(160);

        private static void LogSummaries(string reason)
        {
            float elapsed = GetBossElapsedSeconds();
            int bossTargetRatio = _targetCastCount <= 0
                ? 0
                : Mathf.RoundToInt((float)_bossTargetCastCount / _targetCastCount * 100.0f);

            RunTelemetry.Log(
                RunTelemetry.BossTargetingSummary,
                $"reason={reason}",
                $"boss_elapsed_seconds={Mathf.RoundToInt(elapsed)}",
                $"target_casts={_targetCastCount}",
                $"boss_target_casts={_bossTargetCastCount}",
                $"boss_target_ratio={bossTargetRatio}",
                $"attack_casts={_attackCastCount}",
                $"skill_casts={_skillCastCount}");

            string topSourceId = NONE_TARGET_ID;
            int topDamage = 0;
            foreach (KeyValuePair<string, int> pair in BossDamageBySource)
            {
                if (pair.Value <= topDamage)
                    continue;

                topSourceId = pair.Key;
                topDamage = pair.Value;
            }

            RunTelemetry.Log(
                RunTelemetry.BossDamageSummary,
                $"reason={reason}",
                $"boss_elapsed_seconds={Mathf.RoundToInt(elapsed)}",
                $"total_boss_damage={_totalBossDamage}",
                $"top_source={topSourceId}",
                $"top_damage={topDamage}",
                $"source_breakdown={BuildDamageBreakdown()}");
        }

        private static string BuildDamageBreakdown()
        {
            if (BossDamageBySource.Count == 0)
                return NONE_TARGET_ID;

            SummaryBuilder.Clear();
            foreach (KeyValuePair<string, int> pair in BossDamageBySource)
            {
                if (SummaryBuilder.Length > 0)
                    SummaryBuilder.Append(';');

                SummaryBuilder.Append(pair.Key);
                SummaryBuilder.Append('=');
                SummaryBuilder.Append(pair.Value);
            }

            return SummaryBuilder.ToString();
        }
    }
}
