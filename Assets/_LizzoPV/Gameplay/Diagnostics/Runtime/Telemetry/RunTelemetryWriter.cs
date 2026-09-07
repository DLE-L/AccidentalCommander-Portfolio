using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Lizzo.PV.Flow;
using UnityEngine;

namespace Lizzo.PV.Gameplay.Telemetry
{
    internal static class RunTelemetryWriter
    {
        private readonly struct Entry
        {
            public Entry(int index, float runSeconds, string eventName, string parametersText)
            {
                Index = index;
                RunSeconds = runSeconds;
                EventName = eventName;
                ParametersText = parametersText;
            }

            public int Index { get; }
            public float RunSeconds { get; }
            public string EventName { get; }
            public string ParametersText { get; }
        }

        private static readonly HashSet<string> ConsoleEvents = new HashSet<string>(StringComparer.Ordinal)
        {
            "run_start", "run_end", RunTelemetry.CompanionCombatSummary, RunTelemetry.SynergyActivated,
            RunTelemetry.SynergyExecutionCompleted, RunTelemetry.SynergyCombatSummary,
        };
        private static readonly List<Entry> Entries = new List<Entry>(4096);
        private static readonly StringBuilder Builder = new StringBuilder(256 * 1024);
        private static string _runId = string.Empty;
        private static string _startedAt = string.Empty;
        private static string _path = string.Empty;
        private static float _startedAtTime;
        private static float _endedAtSeconds = -1.0f;
        private static int _lastFlushedCount = -1;
        private static bool _started;
        private static bool _ended;

        public static string CurrentRunLogPath => _path;
        public static string CurrentRunId => _runId;
        public static float ElapsedSeconds => !_started ? 0.0f : _ended
            ? Mathf.Max(0.0f, _endedAtSeconds)
            : Mathf.Max(0.0f, Time.time - _startedAtTime);

        public static void BeginRun(RunMode mode, string configVersion, string configAssignmentHash, string selectedWeaponId)
        {
            Entries.Clear();
            _runId = DateTime.Now.ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture)
                + "-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            _startedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            _path = Path.Combine(ResolveDirectory(), $"run-{_runId}.txt");
            _startedAtTime = Time.time;
            _endedAtSeconds = -1.0f;
            _lastFlushedCount = -1;
            _ended = false;
            _started = true;
            Log(
                "run_start",
                "stage=stage1",
                $"run_mode={mode.ToString().ToLowerInvariant()}",
                $"build_version={Application.version}",
                $"config_version={configVersion ?? string.Empty}",
                $"config_assignment_hash={configAssignmentHash ?? string.Empty}",
                $"selected_weapon_id={selectedWeaponId ?? string.Empty}");
        }

        public static void EndRun()
        {
            if (!_started || _ended) return;
            _endedAtSeconds = ElapsedSeconds;
            _ended = true;
            Flush("run_end");
        }

        public static void Log(string eventName, params string[] parameters)
        {
            if (!_started || string.IsNullOrWhiteSpace(eventName)) return;
            string parametersText = parameters == null || parameters.Length == 0
                ? string.Empty : string.Join(", ", parameters);
            Entries.Add(new Entry(Entries.Count + 1, ElapsedSeconds, eventName, parametersText));
            if (!ConsoleEvents.Contains(eventName)) return;
            Debug.Log(string.IsNullOrEmpty(parametersText)
                ? $"[RUN_TELEMETRY] {eventName}"
                : $"[RUN_TELEMETRY] {eventName} | {parametersText}");
        }

        public static void Flush(string reason)
        {
            if (!_started || Entries.Count == 0 || string.IsNullOrEmpty(_path) || _lastFlushedCount == Entries.Count) return;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_path));
                File.WriteAllBytes(_path, WithUtf8Preamble(BuildText(reason)));
                _lastFlushedCount = Entries.Count;
                Debug.Log($"[RUN_TELEMETRY] run log saved: {_path}");
            }
            catch (Exception exception)
            {
                Debug.LogError($"[RUN_TELEMETRY] run log save failed: {exception.Message}");
            }
        }

        private static string BuildText(string reason)
        {
            Builder.Clear();
            Builder.AppendLine("# Run Telemetry Log");
            Builder.AppendLine($"run_id={_runId}");
            Builder.AppendLine($"started_at_local={_startedAt}");
            Builder.AppendLine($"flush_reason={reason}");
            Builder.AppendLine($"event_count={Entries.Count.ToString(CultureInfo.InvariantCulture)}");
            Builder.AppendLine($"run_elapsed_seconds={ElapsedSeconds.ToString("0.0", CultureInfo.InvariantCulture)}");
            Builder.AppendLine($"run_ended={_ended.ToString().ToLowerInvariant()}");
            Builder.AppendLine($"log_path={_path}");
            Builder.AppendLine();
            Builder.AppendLine("[events]");
            for (int index = 0; index < Entries.Count; index++)
            {
                Entry entry = Entries[index];
                Builder.Append(entry.Index.ToString("0000", CultureInfo.InvariantCulture));
                Builder.Append(" | ");
                Builder.Append(entry.RunSeconds.ToString("0.0", CultureInfo.InvariantCulture));
                Builder.Append(" | ");
                Builder.Append(entry.EventName);
                if (!string.IsNullOrEmpty(entry.ParametersText))
                {
                    Builder.Append(" | ");
                    Builder.Append(entry.ParametersText);
                }
                Builder.AppendLine();
            }
            return Builder.ToString();
        }

        private static byte[] WithUtf8Preamble(string text)
        {
            byte[] payload = Encoding.UTF8.GetBytes(text ?? string.Empty);
            byte[] preamble = Encoding.UTF8.GetPreamble();
            byte[] result = new byte[preamble.Length + payload.Length];
            Buffer.BlockCopy(preamble, 0, result, 0, preamble.Length);
            Buffer.BlockCopy(payload, 0, result, preamble.Length, payload.Length);
            return result;
        }

        private static string ResolveDirectory()
        {
#if UNITY_EDITOR
            string projectRoot = Directory.GetCurrentDirectory();
            if (string.IsNullOrEmpty(projectRoot) || !Directory.Exists(Path.Combine(projectRoot, "Assets")))
                projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.Combine(projectRoot, "_Temp", "TelemetryRuns");
#else
            return Path.Combine(Application.persistentDataPath, "TelemetryRuns");
#endif
        }
    }
}
