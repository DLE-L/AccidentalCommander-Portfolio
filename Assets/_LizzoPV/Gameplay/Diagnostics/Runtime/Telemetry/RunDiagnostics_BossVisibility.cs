using Lizzo.PV.P0.Units;
using UnityEngine;

namespace Lizzo.PV.Gameplay.Telemetry
{
    public static partial class RunDiagnostics
    {
        public static void SampleBossBodyVisibility(bool directionIndicatorVisible)
        {
            if (HungryGiantBehaviour.Current == null)
                return;

            float runSeconds = RunTelemetry.RunElapsedSeconds;
            if (runSeconds < _nextBossVisibilitySampleRunSeconds)
                return;

            _nextBossVisibilitySampleRunSeconds = runSeconds + BOSS_VISIBILITY_SAMPLE_INTERVAL_SECONDS;
            float visibleRatio = CalculateBossBodyVisibleRatio(HungryGiantBehaviour.Current);
            int visiblePercent = Mathf.RoundToInt(visibleRatio * 100.0f);
            _bossVisibilitySamples++;
            _bossVisibilityRatioSum += visibleRatio;
            _bossMinVisibilityRatio = Mathf.Min(_bossMinVisibilityRatio, visibleRatio);

            if (visibleRatio < BOSS_LOW_VISIBILITY_THRESHOLD)
                _bossLowVisibilitySamples++;
            if (directionIndicatorVisible)
                _bossIndicatorVisibleSamples++;

            if (_hasLoggedBossLowVisibility || visibleRatio >= BOSS_LOW_VISIBILITY_THRESHOLD)
                return;

            _hasLoggedBossLowVisibility = true;
            RunTelemetry.Log(
                RunTelemetry.BossBodyVisibleRatio,
                "reason=first_low_visibility",
                $"visible_percent={visiblePercent}",
                $"indicator_visible={directionIndicatorVisible.ToString().ToLowerInvariant()}");
        }

        private static void LogBossBodyVisibilitySummary(string reason)
        {
            if (_bossVisibilitySamples <= 0)
                return;

            int averagePercent = Mathf.RoundToInt(_bossVisibilityRatioSum / _bossVisibilitySamples * 100.0f);
            int minPercent = Mathf.RoundToInt(_bossMinVisibilityRatio * 100.0f);
            RunTelemetry.Log(
                RunTelemetry.BossBodyVisibleRatio,
                $"reason={NormalizeReason(reason)}",
                $"samples={_bossVisibilitySamples}",
                $"avg_visible_percent={averagePercent}",
                $"min_visible_percent={minPercent}",
                $"low_visibility_samples={_bossLowVisibilitySamples}",
                $"indicator_visible_samples={_bossIndicatorVisibleSamples}");
        }

        private static float CalculateBossBodyVisibleRatio(HungryGiantBehaviour boss)
        {
            Camera camera = Camera.main;
            if (camera == null || boss == null)
                return 0.0f;

            Renderer renderer = boss.GetComponentInChildren<Renderer>();
            if (renderer == null)
            {
                Vector3 viewportPosition = camera.WorldToViewportPoint(boss.transform.position);
                bool isVisible = viewportPosition.z > 0.0f
                    && viewportPosition.x >= 0.0f
                    && viewportPosition.x <= 1.0f
                    && viewportPosition.y >= 0.0f
                    && viewportPosition.y <= 1.0f;
                return isVisible ? 1.0f : 0.0f;
            }

            Bounds bounds = renderer.bounds;
            Vector3 center = bounds.center;
            Vector3 extents = bounds.extents;
            float minX = float.PositiveInfinity;
            float minY = float.PositiveInfinity;
            float maxX = float.NegativeInfinity;
            float maxY = float.NegativeInfinity;
            bool hasFrontCorner = false;

            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 worldCorner = center + Vector3.Scale(extents, new Vector3(x, y, z));
                        Vector3 viewportCorner = camera.WorldToViewportPoint(worldCorner);
                        if (viewportCorner.z <= 0.0f)
                            continue;

                        hasFrontCorner = true;
                        minX = Mathf.Min(minX, viewportCorner.x);
                        minY = Mathf.Min(minY, viewportCorner.y);
                        maxX = Mathf.Max(maxX, viewportCorner.x);
                        maxY = Mathf.Max(maxY, viewportCorner.y);
                    }
                }
            }

            if (hasFrontCorner == false)
                return 0.0f;

            float width = Mathf.Max(0.0001f, maxX - minX);
            float height = Mathf.Max(0.0001f, maxY - minY);
            float visibleWidth = Mathf.Max(0.0f, Mathf.Min(maxX, 1.0f) - Mathf.Max(minX, 0.0f));
            float visibleHeight = Mathf.Max(0.0f, Mathf.Min(maxY, 1.0f) - Mathf.Max(minY, 0.0f));
            return Mathf.Clamp01((visibleWidth * visibleHeight) / (width * height));
        }
    }
}
