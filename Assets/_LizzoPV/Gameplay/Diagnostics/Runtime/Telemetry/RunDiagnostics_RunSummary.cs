using Lizzo.PV.Combat;
using Lizzo.PV.Gameplay.Units;
using Lizzo.PV.Gameplay.Visuals;
using Lizzo.PV.Legion;

namespace Lizzo.PV.Gameplay.Telemetry
{
    public static partial class RunDiagnostics
    {
        public static void LogCombatReadabilityCheck(string reason)
        {
            RunTelemetry.Log(
                RunTelemetry.CombatReadabilityCheck,
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
