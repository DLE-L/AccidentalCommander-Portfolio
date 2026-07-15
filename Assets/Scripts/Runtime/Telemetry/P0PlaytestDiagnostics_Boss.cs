using UnityEngine;

namespace Lizzo.PV.P0.Telemetry
{
    public static partial class P0PlaytestDiagnostics
    {
        public static void LogBossHpSample(int hp, int maxHp, float uiFillRatio, string reason)
        {
            float runSeconds = P0Telemetry.RunElapsedSeconds;
            if (runSeconds < _nextBossHpSampleRunSeconds)
                return;

            _nextBossHpSampleRunSeconds = runSeconds + 5.0f;
            float hpRatio = maxHp <= 0 ? 0.0f : Mathf.Clamp01((float)hp / maxHp);
            string normalizedReason = NormalizeReason(reason);
            if (normalizedReason != "ui_update")
            {
                P0Telemetry.Log(
                    P0Telemetry.BossHpSample,
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

            P0Telemetry.Log(
                P0Telemetry.BossHpbarSyncCheck,
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
            P0Telemetry.Log(
                P0Telemetry.BossHpbarSyncCheck,
                $"reason={NormalizeReason(reason)}",
                $"samples={_bossHpbarSyncSamples}",
                $"mismatch_count={_bossHpbarSyncMismatchCount}",
                $"sync_ok={(_bossHpbarSyncMismatchCount == 0).ToString().ToLowerInvariant()}");
        }

    }
}
