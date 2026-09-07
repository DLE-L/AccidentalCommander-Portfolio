using UnityEngine;

namespace Lizzo.PV.Gameplay.Telemetry
{
    public static partial class RunDiagnostics
    {
        public static void LogBossHpSample(int hp, int maxHp, float uiFillRatio, string reason)
        {
            float runSeconds = RunTelemetry.RunElapsedSeconds;
            if (runSeconds < _nextBossHpSampleRunSeconds)
                return;

            _nextBossHpSampleRunSeconds = runSeconds + 5.0f;
            float hpRatio = maxHp <= 0 ? 0.0f : Mathf.Clamp01((float)hp / maxHp);
            string normalizedReason = NormalizeReason(reason);
            if (normalizedReason != "ui_update")
            {
                RunTelemetry.Log(
                    RunTelemetry.BossHpSample,
                    $"reason={normalizedReason}",
                    $"hp={Mathf.Max(0, hp)}",
                    $"max_hp={Mathf.Max(0, maxHp)}",
                    $"hp_percent={Mathf.CeilToInt(hpRatio * 100.0f)}",
                    $"ui_fill_percent={Mathf.RoundToInt(Mathf.Clamp01(uiFillRatio) * 100.0f)}");
            }

            LogBossHpbarSyncCheck(hp, maxHp, uiFillRatio, normalizedReason);
        }

        private static void LogBossHpbarSyncCheck(int hp, int maxHp, float uiFillRatio, string reason)
        {
            float hpRatio = maxHp <= 0 ? 0.0f : Mathf.Clamp01((float)hp / maxHp);
            int hpPercent = Mathf.CeilToInt(hpRatio * 100.0f);
            int uiFillPercent = Mathf.RoundToInt(Mathf.Clamp01(uiFillRatio) * 100.0f);
            int delta = Mathf.Abs(hpPercent - uiFillPercent);
            bool inSync = delta <= 1;

            _bossHpbarSyncSamples++;
            if (inSync == false)
                _bossHpbarSyncMismatchCount++;

            if (inSync && NormalizeReason(reason) == "ui_update")
                return;

            RunTelemetry.Log(
                RunTelemetry.BossHpbarSyncCheck,
                $"reason={NormalizeReason(reason)}",
                $"hp={Mathf.Max(0, hp)}",
                $"max_hp={Mathf.Max(0, maxHp)}",
                $"hp_percent={hpPercent}",
                $"ui_fill_percent={uiFillPercent}",
                $"delta_percent={delta}",
                $"sync_ok={inSync.ToString().ToLowerInvariant()}");
        }

        private static void LogBossHpbarSyncSummary(string reason)
        {
            RunTelemetry.Log(
                RunTelemetry.BossHpbarSyncCheck,
                $"reason={NormalizeReason(reason)}",
                $"samples={_bossHpbarSyncSamples}",
                $"mismatch_count={_bossHpbarSyncMismatchCount}",
                $"sync_ok={(_bossHpbarSyncMismatchCount == 0).ToString().ToLowerInvariant()}");
        }

    }
}
