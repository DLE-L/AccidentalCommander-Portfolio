using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.Profiling;

namespace Lizzo.PV.P0.Telemetry
{
    public static partial class P0Telemetry
    {
        private static int _performanceFrameSampleCount;
        private static double _performanceFrameSeconds;
        private static float _minimumFps;
        private static long _peakTotalAllocatedMemoryBytes;
        private static int _lastGen0CollectionCount;
        private static int _gcSpikeFrameCount;
        private static int _gcPeakCollectionsPerFrame;
        private static int _gcTotalCollectionDelta;

        private static void ResetPerformanceSamples()
        {
            _performanceFrameSampleCount = 0;
            _performanceFrameSeconds = 0.0;
            _minimumFps = float.PositiveInfinity;
            _peakTotalAllocatedMemoryBytes = 0L;
            _lastGen0CollectionCount = GC.CollectionCount(0);
            _gcSpikeFrameCount = 0;
            _gcPeakCollectionsPerFrame = 0;
            _gcTotalCollectionDelta = 0;
        }

        public static void SamplePerformance(float unscaledDeltaSeconds)
        {
            if (_runStarted == false || _runEnded || unscaledDeltaSeconds <= 0.0f || float.IsNaN(unscaledDeltaSeconds) || float.IsInfinity(unscaledDeltaSeconds))
                return;

            _performanceFrameSampleCount++;
            _performanceFrameSeconds += unscaledDeltaSeconds;
            _minimumFps = Mathf.Min(_minimumFps, 1.0f / unscaledDeltaSeconds);
            long totalAllocatedMemoryBytes = Profiler.GetTotalAllocatedMemoryLong();
            _peakTotalAllocatedMemoryBytes = Math.Max(_peakTotalAllocatedMemoryBytes, totalAllocatedMemoryBytes);

            int currentGen0CollectionCount = GC.CollectionCount(0);
            int collectionDelta = Math.Max(0, currentGen0CollectionCount - _lastGen0CollectionCount);
            if (collectionDelta > 0)
            {
                _gcSpikeFrameCount++;
                _gcPeakCollectionsPerFrame = Math.Max(_gcPeakCollectionsPerFrame, collectionDelta);
                _gcTotalCollectionDelta += collectionDelta;
                LogGcGen0Spike(unscaledDeltaSeconds, collectionDelta, totalAllocatedMemoryBytes);
            }

            _lastGen0CollectionCount = currentGen0CollectionCount;
        }

        private static void LogGcGen0Spike(float unscaledDeltaSeconds, int collectionDelta, long totalAllocatedMemoryBytes)
        {
            Log(
                GcGen0Spike,
                $"run_elapsed_seconds={RunElapsedSeconds.ToString("0.000", CultureInfo.InvariantCulture)}",
                $"frame_time_ms={(unscaledDeltaSeconds * 1000.0f).ToString("0.000", CultureInfo.InvariantCulture)}",
                "frame_time_unit=ms",
                $"gc_gen0_collection_delta={collectionDelta}",
                $"total_allocated_memory_bytes={totalAllocatedMemoryBytes}",
                "memory_unit=bytes");
        }

        private static void LogPerformanceSummary()
        {
            float averageFps = _performanceFrameSeconds <= 0.0
                ? 0.0f
                : (float)(_performanceFrameSampleCount / _performanceFrameSeconds);
            float minimumFps = float.IsPositiveInfinity(_minimumFps) ? 0.0f : _minimumFps;

            Log(
                PerformanceSummary,
                $"average_fps={averageFps.ToString("0.0", CultureInfo.InvariantCulture)}",
                $"minimum_fps={minimumFps.ToString("0.0", CultureInfo.InvariantCulture)}",
                $"frame_sample_count={_performanceFrameSampleCount}",
                $"memory_peak_total_allocated_bytes={_peakTotalAllocatedMemoryBytes}",
                "memory_unit=bytes",
                $"gc_gen0_spike_frame_count={_gcSpikeFrameCount}",
                $"gc_gen0_peak_collections_per_frame={_gcPeakCollectionsPerFrame}",
                $"gc_gen0_total_collection_delta={_gcTotalCollectionDelta}",
                "sample_definition=run_loaded_unscaled_frame_delta");
        }
    }
}
