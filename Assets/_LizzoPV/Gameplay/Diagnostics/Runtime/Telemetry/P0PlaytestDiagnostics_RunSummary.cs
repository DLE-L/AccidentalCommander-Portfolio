using Lizzo.PV.Legion;

namespace Lizzo.PV.P0.Telemetry
{
    public static partial class P0PlaytestDiagnostics
    {
        public static void LogCombatReadabilityCheck(string reason)
        {
            P0Telemetry.Log(
                P0Telemetry.CombatReadabilityCheck,
                $"reason={NormalizeReason(reason)}",
                "requires_video=true",
                "unit_scale_hierarchy=sheet_70",
                "commander_hpbar=always",
                "companion_hpbar_threshold=0.6",
                $"enemy_hpbar_hit_seconds={EnemyHealthBar.HIT_REVEAL_SECONDS:0.##}",
                "boss_hpbar=hud_only");
        }

        public static void LogRunEndSummaries(string result)
        {
            string reason = string.IsNullOrEmpty(result) ? "run_end" : $"run_end_{result}";
            LogEnemyTtkSummary(reason);
            LogEnemyCauseSummaries(reason);
            LogEnemyFeedbackSummaries(reason);
            LogFxSfxSummaries(reason);
            LogShieldOrcTtk(reason);
            LogRedChargerTtk(reason);
            LogShieldOrcFeedbackSummary(reason);
            LogCommanderDamageSummary(reason);
            LogCompanionDamageSummary(reason);
            LogBossCompanionDamageSummary(reason);
            LogBossHpbarSyncSummary(reason);
            LogBossBodyVisibilitySummary(reason);
        }
    }
}
