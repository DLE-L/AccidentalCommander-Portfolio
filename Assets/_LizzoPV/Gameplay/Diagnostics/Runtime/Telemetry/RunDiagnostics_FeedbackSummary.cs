namespace Lizzo.PV.Gameplay.Telemetry
{
    public static partial class RunDiagnostics
    {
        private static void LogEnemyFeedbackSummaries(string reason)
        {
            string normalizedReason = NormalizeReason(reason);
            RunTelemetry.Log(
                RunTelemetry.EnemyDeathFeedbackShow,
                $"reason={normalizedReason}",
                $"feedback_by_enemy={FormatCounts(EnemyDeathFeedbackCounts)}");

            RunTelemetry.Log(
                RunTelemetry.EnemyRewardDrop,
                $"reason={normalizedReason}",
                $"orb_cues={FormatCounts(ExpOrbAbsorbCueCounts)}");
        }

        private static void LogFxSfxSummaries(string reason)
        {
            string normalizedReason = NormalizeReason(reason);
            RunTelemetry.Log(
                RunTelemetry.HitFeedbackShow,
                $"reason={normalizedReason}",
                $"feedback_counts={FormatCounts(HitFeedbackCounts)}",
                $"missing_counts={FormatCounts(HitFeedbackMissingCounts)}");

            RunTelemetry.Log(
                RunTelemetry.HitstopApply,
                $"reason={normalizedReason}",
                $"hitstop_counts={FormatCounts(HitStopCounts)}");

            RunTelemetry.Log(
                RunTelemetry.SfxPlay,
                $"reason={normalizedReason}",
                $"played={FormatCounts(SfxPlayCounts)}",
                $"missing={FormatCounts(SfxMissingCounts)}");

            RunTelemetry.Log(
                RunTelemetry.SfxCooldownSkip,
                $"reason={normalizedReason}",
                $"skipped={FormatCounts(SfxCooldownSkipCounts)}",
                "cooldown_seconds=0.08");
        }

        private static void LogShieldOrcFeedbackSummary(string reason)
        {
            RunTelemetry.Log(
                RunTelemetry.ShieldOrcFeedbackCheck,
                "stage=summary",
                $"reason={NormalizeReason(reason)}",
                "requires_video=true",
                $"hit_feedback_count={_shieldOrcHitFeedbackCount}",
                $"crack_feedback_count={_shieldOrcCrackFeedbackCount}",
                $"death_feedback_count={_shieldOrcDeathFeedbackCount}",
                $"spawned={GetCount(SpawnCounts, SHIELD_ORC_ID)}",
                $"killed={GetCount(DeathCounts, SHIELD_ORC_ID)}");
        }

        private static bool ShouldLogShieldOrcFeedbackStage(string normalizedStage)
        {
            if (normalizedStage == "hit")
                return _shieldOrcHitFeedbackCount == 1;

            if (normalizedStage == "crack")
                return _shieldOrcCrackFeedbackCount == 1;

            if (normalizedStage == "death")
                return _shieldOrcDeathFeedbackCount == 1;

            return true;
        }
    }
}
