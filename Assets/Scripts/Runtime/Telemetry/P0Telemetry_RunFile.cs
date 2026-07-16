using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
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
        private static string _lastLogcatExportRawSha256 = string.Empty;
        private static int _logcatExportSequence;

        private const string LogcatExportSchemaVersion = "p0_run_logcat_export_v1";
        private const int MaxLogcatPayloadChunkCharacters = 2800;

        private readonly struct LogcatRunExport
        {
            public readonly string RawSha256;
            public readonly int RawByteCount;
            public readonly string[] Chunks;

            public LogcatRunExport(string rawSha256, int rawByteCount, string[] chunks)
            {
                RawSha256 = rawSha256;
                RawByteCount = rawByteCount;
                Chunks = chunks;
            }
        }

        public static string CurrentRunLogPath => _runLogPath;

        private static void BeginRunLog()
        {
            RunLogEntries.Clear();
            _runLogStartedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            _runLogId = DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
            _runLogPath = Path.Combine(ResolveRunLogDirectory(), $"p0-run-{_runLogId}.txt");
            _lastFlushedRunLogCount = -1;
            _lastLogcatExportRawSha256 = string.Empty;
            _logcatExportSequence = 0;
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

                string rawPayload = BuildRunLogText(reason);
                byte[] rawPayloadBytes = CreateRunLogRawBytes(rawPayload);
                File.WriteAllBytes(_runLogPath, rawPayloadBytes);
                _lastFlushedRunLogCount = RunLogEntries.Count;
#if UNITY_ANDROID && !UNITY_EDITOR
                TryExportRunLogToLogcat(rawPayloadBytes, reason);
#endif
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

        private static void TryExportRunLogToLogcat(byte[] rawPayloadBytes, string flushReason)
        {
            try
            {
                LogcatRunExport export = CreateLogcatRunExport(rawPayloadBytes);
                if (string.Equals(_lastLogcatExportRawSha256, export.RawSha256, StringComparison.Ordinal))
                {
                    Debug.Log($"P0RUN_EXPORT_DEDUP schema_version={LogcatExportSchemaVersion} run_id={_runLogId} raw_sha256={export.RawSha256}");
                    return;
                }

                _logcatExportSequence++;
                int eventCount = RunLogEntries.Count;
                int chunkCount = export.Chunks.Length;
                Debug.Log($"P0RUN_EXPORT_BEGIN schema_version={LogcatExportSchemaVersion} run_id={_runLogId} export_sequence={_logcatExportSequence} flush_reason={flushReason} event_count={eventCount} chunk_count={chunkCount} raw_byte_count={export.RawByteCount} raw_sha256={export.RawSha256}");

                for (int chunkIndex = 0; chunkIndex < chunkCount; chunkIndex++)
                {
                    Debug.Log($"P0RUN_EXPORT_CHUNK schema_version={LogcatExportSchemaVersion} run_id={_runLogId} export_sequence={_logcatExportSequence} chunk_index={chunkIndex} chunk_count={chunkCount} raw_sha256={export.RawSha256} payload_base64={export.Chunks[chunkIndex]}");
                }

                Debug.Log($"P0RUN_EXPORT_END schema_version={LogcatExportSchemaVersion} run_id={_runLogId} export_sequence={_logcatExportSequence} flush_reason={flushReason} event_count={eventCount} chunk_count={chunkCount} raw_byte_count={export.RawByteCount} raw_sha256={export.RawSha256}");
                _lastLogcatExportRawSha256 = export.RawSha256;
            }
            catch (Exception ex)
            {
                Debug.LogError($"P0 telemetry logcat export failed: {ex.Message}");
            }
        }

        private static byte[] CreateRunLogRawBytes(string rawPayload)
        {
            byte[] payloadBytes = Encoding.UTF8.GetBytes(rawPayload ?? string.Empty);
            byte[] preamble = Encoding.UTF8.GetPreamble();
            byte[] rawBytes = new byte[preamble.Length + payloadBytes.Length];
            Buffer.BlockCopy(preamble, 0, rawBytes, 0, preamble.Length);
            Buffer.BlockCopy(payloadBytes, 0, rawBytes, preamble.Length, payloadBytes.Length);
            return rawBytes;
        }

        private static LogcatRunExport CreateLogcatRunExport(byte[] rawBytes)
        {
            rawBytes = rawBytes ?? Array.Empty<byte>();
            string rawSha256 = ComputeRawSha256(rawBytes);
            string payloadBase64 = Convert.ToBase64String(rawBytes);
            int chunkCount = Math.Max(1, (payloadBase64.Length + MaxLogcatPayloadChunkCharacters - 1) / MaxLogcatPayloadChunkCharacters);
            string[] chunks = new string[chunkCount];

            for (int chunkIndex = 0; chunkIndex < chunkCount; chunkIndex++)
            {
                int offset = chunkIndex * MaxLogcatPayloadChunkCharacters;
                int length = Math.Min(MaxLogcatPayloadChunkCharacters, payloadBase64.Length - offset);
                chunks[chunkIndex] = payloadBase64.Substring(offset, length);
            }

            return new LogcatRunExport(rawSha256, rawBytes.Length, chunks);
        }

        private static string ComputeRawSha256(byte[] rawBytes)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                return BitConverter.ToString(sha256.ComputeHash(rawBytes)).Replace("-", string.Empty).ToLowerInvariant();
            }
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
