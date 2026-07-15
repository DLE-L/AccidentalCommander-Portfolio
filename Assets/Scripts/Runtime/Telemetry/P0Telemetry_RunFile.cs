using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace Lizzo.PV.P0.Telemetry
{
    public static partial class P0Telemetry
    {
        private readonly struct RunLogEntry
        {
            public readonly int Index;
            public readonly float RunSeconds;
            public readonly string EventName;
            public readonly string ParametersText;

            public RunLogEntry(int index, float runSeconds, string eventName, string parametersText)
            {
                Index = index;
                RunSeconds = runSeconds;
                EventName = eventName;
                ParametersText = parametersText;
            }
        }

        private static readonly List<RunLogEntry> RunLogEntries = new List<RunLogEntry>(4096);
        private static readonly StringBuilder RunLogBuilder = new StringBuilder(256 * 1024);

        private static string _runLogId = string.Empty;
        private static string _runLogStartedAt = string.Empty;
        private static string _runLogPath = string.Empty;
        private static int _lastFlushedRunLogCount = -1;

        public static string CurrentRunLogPath => _runLogPath;

        private static void BeginRunLog()
        {
            RunLogEntries.Clear();
            _runLogStartedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            _runLogId = DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
            _runLogPath = Path.Combine(ResolveRunLogDirectory(), $"p0-run-{_runLogId}.txt");
            _lastFlushedRunLogCount = -1;
        }

        private static void RecordRunLogEntry(string eventName, string parametersText)
        {
            if (_runStarted == false)
                return;

            float runSeconds = RunElapsedSeconds;
            _lastRecordedRunSeconds = Mathf.Max(_lastRecordedRunSeconds, runSeconds);
            RunLogEntries.Add(new RunLogEntry(
                RunLogEntries.Count + 1,
                runSeconds,
                eventName,
                parametersText ?? string.Empty));
        }

        public static void FlushRunLog(string reason)
        {
            if (_runStarted == false || RunLogEntries.Count == 0 || string.IsNullOrEmpty(_runLogPath))
                return;

            if (_lastFlushedRunLogCount == RunLogEntries.Count)
                return;

            try
            {
                string directory = Path.GetDirectoryName(_runLogPath);
                if (string.IsNullOrEmpty(directory) == false)
                    Directory.CreateDirectory(directory);

                File.WriteAllText(_runLogPath, BuildRunLogText(reason), Encoding.UTF8);
                _lastFlushedRunLogCount = RunLogEntries.Count;
                Debug.Log($"P0 telemetry run log saved: {_runLogPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"P0 telemetry run log save failed: {ex.Message}");
            }
        }

        private static string BuildRunLogText(string reason)
        {
            float elapsedSeconds = _runEnded && _runEndSeconds >= 0.0f
                ? _runEndSeconds
                : Mathf.Max(_lastRecordedRunSeconds, RunElapsedSeconds);

            RunLogBuilder.Clear();
            RunLogBuilder.AppendLine("# P0 Telemetry Run Log");
            RunLogBuilder.AppendLine($"run_id={_runLogId}");
            RunLogBuilder.AppendLine($"started_at_local={_runLogStartedAt}");
            RunLogBuilder.AppendLine($"flush_reason={reason}");
            RunLogBuilder.AppendLine($"event_count={RunLogEntries.Count.ToString(CultureInfo.InvariantCulture)}");
            RunLogBuilder.AppendLine($"run_elapsed_seconds={elapsedSeconds.ToString("0.0", CultureInfo.InvariantCulture)}");
            RunLogBuilder.AppendLine($"run_ended={_runEnded.ToString().ToLowerInvariant()}");
            RunLogBuilder.AppendLine($"log_path={_runLogPath}");
            RunLogBuilder.AppendLine();
            RunLogBuilder.AppendLine("[events]");

            for (int i = 0; i < RunLogEntries.Count; i++)
            {
                RunLogEntry entry = RunLogEntries[i];
                RunLogBuilder.Append(entry.Index.ToString("0000", CultureInfo.InvariantCulture));
                RunLogBuilder.Append(" | ");
                RunLogBuilder.Append(entry.RunSeconds.ToString("0.0", CultureInfo.InvariantCulture));
                RunLogBuilder.Append(" | ");
                RunLogBuilder.Append(entry.EventName);

                if (string.IsNullOrEmpty(entry.ParametersText) == false)
                {
                    RunLogBuilder.Append(" | ");
                    RunLogBuilder.Append(entry.ParametersText);
                }

                RunLogBuilder.AppendLine();
            }

            return RunLogBuilder.ToString();
        }

        private static string ResolveRunLogDirectory()
        {
#if UNITY_EDITOR
            string projectRoot = Directory.GetCurrentDirectory();
            if (string.IsNullOrEmpty(projectRoot) || Directory.Exists(Path.Combine(projectRoot, "Assets")) == false)
                projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

            return Path.Combine(projectRoot, "_Temp", "P0TelemetryRuns");
#else
            return Path.Combine(Application.persistentDataPath, "P0TelemetryRuns");
#endif
        }
    }
}
