using Lizzo.PV.Gameplay.Units;
using Lizzo.PV.Legion;
using UnityEngine;

namespace Lizzo.PV.Gameplay.Telemetry
{
    public static partial class RunDiagnostics
    {
        public static void LogEnemyAliveSnapshot(string reason)
        {
            ScratchCounts.Clear();

            int totalAlive = 0;
            if (_party.Registry != null)
            {
                foreach (global::Lizzo.PV.Gameplay.Units.EnemyActor monster in _party.Registry.Enemies)
                {
                    if (monster == null)
                        continue;

                    totalAlive++;
                    Increment(ScratchCounts, ResolveEnemyId(monster));
                }
            }

            RunTelemetry.Log(
                RunTelemetry.EnemyAliveSnapshot,
                $"reason={NormalizeReason(reason)}",
                $"total_alive={totalAlive}",
                $"alive={FormatCounts(ScratchCounts)}",
                $"spawned={FormatCounts(SpawnCounts)}",
                $"killed={FormatCounts(DeathCounts)}",
                $"avg_life={FormatAverageLifetimes()}");

            LogEnemyTtkSummary(reason);
            LogShieldOrcTtk(reason);
            LogRedChargerTtk(reason);
        }

        private static void LogEnemyTtkSummary(string reason)
        {
            RunTelemetry.Log(
                RunTelemetry.EnemyTtkSummary,
                $"reason={NormalizeReason(reason)}",
                $"spawned={FormatCounts(SpawnCounts)}",
                $"killed={FormatCounts(DeathCounts)}",
                $"avg_life={FormatAverageLifetimes()}");
        }

        private static void LogEnemyCauseSummaries(string reason)
        {
            string normalizedReason = NormalizeReason(reason);
            RunTelemetry.Log(
                RunTelemetry.EnemyAliveTime,
                $"reason={normalizedReason}",
                $"avg_seconds={FormatAverageLifetimes()}",
                $"kills={FormatCounts(LifetimeCounts)}");

            RunTelemetry.Log(
                RunTelemetry.EnemyDamagedTime,
                $"reason={normalizedReason}",
                $"avg_first_damage_seconds={FormatFloatAverages(FirstDamageTimeSums, FirstDamageTimeCounts)}",
                $"avg_damage_window_seconds={FormatFloatAverages(DamageWindowSums, DamageWindowCounts)}");

            RunTelemetry.Log(
                RunTelemetry.EnemyTargetedTime,
                $"reason={normalizedReason}",
                $"avg_first_target_seconds={FormatFloatAverages(FirstTargetTimeSums, FirstTargetTimeCounts)}",
                $"avg_target_window_seconds={FormatFloatAverages(TargetWindowSums, TargetWindowCounts)}");

            RunTelemetry.Log(
                RunTelemetry.EnemyLastHitTime,
                $"reason={normalizedReason}",
                $"avg_last_hit_seconds={FormatFloatAverages(LastHitTimeSums, LastHitTimeCounts)}");

            RunTelemetry.Log(
                RunTelemetry.EnemyTotalDamageTaken,
                $"reason={normalizedReason}",
                $"damage_by_enemy={FormatCounts(EnemyDamageTaken)}");

            RunTelemetry.Log(
                RunTelemetry.EnemyKilledBy,
                $"reason={normalizedReason}",
                $"killed_by={FormatCounts(EnemyKilledByCounts)}");

            RunTelemetry.Log(
                RunTelemetry.EnemyContactDamageCount,
                $"reason={normalizedReason}",
                $"contact_damage_by_enemy={FormatCounts(EnemyContactDamageCounts)}");
        }

        private static void LogShieldOrcTtk(string reason)
        {
            int kills = GetCount(LifetimeCounts, SHIELD_ORC_ID);
            float average = GetAverageLifetime(SHIELD_ORC_ID);
            string status = average < 0.0f ? "missing" : average >= 5.0f && average <= 8.0f ? "target" : "out_of_target";
            RunTelemetry.Log(
                RunTelemetry.ShieldOrcTtk,
                $"reason={NormalizeReason(reason)}",
                $"kills={kills}",
                $"avg_seconds={(average < 0.0f ? "none" : average.ToString("0.0"))}",
                "target_seconds=5-8",
                $"status={status}");
        }

        private static void LogRedChargerTtk(string reason)
        {
            int kills = GetCount(LifetimeCounts, RED_CHARGER_ID);
            float average = GetAverageLifetime(RED_CHARGER_ID);
            string status = average < 0.0f ? "missing" : average >= 8.0f && average <= 12.0f ? "target" : "out_of_target";
            RunTelemetry.Log(
                RunTelemetry.RedChargerTtk,
                $"reason={NormalizeReason(reason)}",
                $"kills={kills}",
                $"avg_seconds={(average < 0.0f ? "none" : average.ToString("0.0"))}",
                "target_seconds=8-12",
                $"status={status}");
        }
    }
}
