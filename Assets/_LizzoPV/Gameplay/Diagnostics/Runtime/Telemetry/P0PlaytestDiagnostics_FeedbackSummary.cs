namespace Lizzo.PV.P0.Telemetry
{
    public static partial class P0PlaytestDiagnostics
    {
        private static void LogEnemyFeedbackSummaries(string reason)
        {
            string normalizedReason = NormalizeReason(reason);
            P0Telemetry.Log(
                P0Telemetry.EnemyDeathFeedbackShow,
                $"reason={normalizedReason}",
                $"feedback_by_enemy={FormatCounts(EnemyDeathFeedbackCounts)}");

            P0Telemetry.Log(
                P0Telemetry.EnemyRewardDrop,
                $"reason={normalizedReason}",
                $"orb_cues={FormatCounts(ExpOrbAbsorbCueCounts)}");
        }

        private static void LogFxSfxSummaries(string reason)
        {
            string normalizedReason = NormalizeReason(reason);
            P0Telemetry.Log(
                P0Telemetry.HitFeedbackShow,
                $"reason={normalizedReason}",
                $"feedback_counts={FormatCounts(HitFeedbackCounts)}",
                $"missing_counts={FormatCounts(HitFeedbackMissingCounts)}");

            P0Telemetry.Log(
                P0Telemetry.HitstopApply,
                $"reason={normalizedReason}",
                $"hitstop_counts={FormatCounts(HitStopCounts)}");

            P0Telemetry.Log(
                P0Telemetry.SfxPlay,
                $"reason={normalizedReason}",
                $"played={FormatCounts(SfxPlayCounts)}",
                $"missing={FormatCounts(SfxMissingCounts)}");

            P0Telemetry.Log(
                P0Telemetry.SfxCooldownSkip,
                $"reason={normalizedReason}",
                $"skipped={FormatCounts(SfxCooldownSkipCounts)}",
                "cooldown_seconds=0.08");
        }

        private static void LogShieldOrcFeedbackSummary(string reason)
        {
            P0Telemetry.Log(
                P0Telemetry.ShieldOrcFeedbackCheck,
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
