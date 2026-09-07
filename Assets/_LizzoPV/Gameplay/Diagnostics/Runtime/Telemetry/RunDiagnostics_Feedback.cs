using UnityEngine;

namespace Lizzo.PV.Gameplay.Telemetry
{
    public static partial class RunDiagnostics
    {
        public static bool IsHitFeedbackPresentation(string presentationId)
        {
            return presentationId == "healing_received"
                || presentationId == "buff_applied"
                || presentationId == "player_damaged"
                || presentationId == "shield_orc_crack"
                || presentationId == "boss_aoe_impact"
                || presentationId == "guard_shockwave"
                || presentationId == "guard_radial_shield"
                || presentationId == "exp_absorb";
        }

        public static bool HasRewardCuePresentation(string presentationId)
        {
            return presentationId == "level_up"
                || presentationId == "result_clear"
                || presentationId == "exp_absorb";
        }

        public static void RecordEnemyDeathFeedback(string enemyId, string feedbackId)
        {
            Increment(EnemyDeathFeedbackCounts, $"{NormalizeKey(enemyId)}:{NormalizeKey(feedbackId)}");
        }

        public static void RecordExpOrbAbsorbCue(string enemyId, int expReward, int orbCount, string cueScale, bool visualOnly)
        {
            Increment(
                ExpOrbAbsorbCueCounts,
                $"{NormalizeKey(enemyId)}:exp={Mathf.Max(0, expReward)}:orbs={Mathf.Max(0, orbCount)}:cue={NormalizeKey(cueScale)}:visual_only={visualOnly.ToString().ToLowerInvariant()}");
        }

        public static void RecordHitFeedback(string slotId, bool hasFx, bool hasSfx, bool hasHitStop, bool hasRewardCue)
        {
            slotId = NormalizeKey(slotId);
            Increment(HitFeedbackCounts, slotId);
            bool complete = hasFx && hasSfx;
            if (complete == false)
                Increment(HitFeedbackMissingCounts, slotId);

            if (GetCount(HitFeedbackCounts, slotId) > 1 && complete)
                return;

            RunTelemetry.Log(
                RunTelemetry.HitFeedbackShow,
                $"slot={slotId}",
                $"has_fx={hasFx.ToString().ToLowerInvariant()}",
                $"has_sfx={hasSfx.ToString().ToLowerInvariant()}",
                $"has_hitstop={hasHitStop.ToString().ToLowerInvariant()}",
                $"has_reward_cue={hasRewardCue.ToString().ToLowerInvariant()}",
                $"status={(complete ? "ok" : "missing_element")}");
        }

        public static void RecordHitStop(float seconds, string reason)
        {
            string normalizedReason = NormalizeReason(reason);
            Increment(HitStopCounts, normalizedReason);
            RunTelemetry.Log(
                RunTelemetry.HitstopApply,
                $"reason={normalizedReason}",
                $"seconds={Mathf.Max(0.0f, seconds):0.###}");
        }

        public static void RecordSfxPlay(string sfxId, float volume, bool played)
        {
            sfxId = NormalizeKey(sfxId);
            if (played)
                Increment(SfxPlayCounts, sfxId);
            else
                Increment(SfxMissingCounts, sfxId);

            int count = played ? GetCount(SfxPlayCounts, sfxId) : GetCount(SfxMissingCounts, sfxId);
            if (played && count > 1)
                return;

            RunTelemetry.Log(
                RunTelemetry.SfxPlay,
                $"sfx_id={sfxId}",
                $"volume_percent={Mathf.RoundToInt(Mathf.Clamp01(volume) * 100.0f)}",
                $"played={played.ToString().ToLowerInvariant()}");
        }

        public static void RecordSfxCooldownSkip(string sfxId, float cooldownSeconds)
        {
            sfxId = NormalizeKey(sfxId);
            Increment(SfxCooldownSkipCounts, sfxId);
            if (GetCount(SfxCooldownSkipCounts, sfxId) > 1)
                return;

            RunTelemetry.Log(
                RunTelemetry.SfxCooldownSkip,
                $"sfx_id={sfxId}",
                $"cooldown_seconds={Mathf.Max(0.0f, cooldownSeconds):0.###}");
        }

        public static void LogShieldOrcFeedbackCheck(string stage, global::MonsterController monster)
        {
            if (monster == null)
                return;

            int hpPercent = monster.MaxHp <= 0
                ? 0
                : Mathf.Clamp(Mathf.CeilToInt((float)monster.Hp / monster.MaxHp * 100.0f), 0, 100);
            string normalizedStage = NormalizeReason(stage);
            if (normalizedStage == "hit")
                _shieldOrcHitFeedbackCount++;
            else if (normalizedStage == "crack")
                _shieldOrcCrackFeedbackCount++;
            else if (normalizedStage == "death")
                _shieldOrcDeathFeedbackCount++;

            if (ShouldLogShieldOrcFeedbackStage(normalizedStage) == false)
                return;

            RunTelemetry.Log(
                RunTelemetry.ShieldOrcFeedbackCheck,
                $"stage={normalizedStage}",
                "requires_video=true",
                $"hp_percent={hpPercent}",
                $"hit_feedback_count={_shieldOrcHitFeedbackCount}",
                $"crack_feedback_count={_shieldOrcCrackFeedbackCount}",
                $"death_feedback_count={_shieldOrcDeathFeedbackCount}",
                $"spawned={GetCount(SpawnCounts, SHIELD_ORC_ID)}",
                $"killed={GetCount(DeathCounts, SHIELD_ORC_ID)}");
        }

    }
}
