using System.Collections.Generic;
using System.Globalization;
using Lizzo.PV.Build;
using UnityEngine;

namespace Lizzo.PV.P0.Telemetry
{
    public static partial class P0Telemetry
    {
        public readonly struct EventSnapshot
        {
            public readonly string EventName;
            public readonly int Count;
            public readonly float FirstRunSeconds;
            public readonly float LastRunSeconds;
            public readonly string LastParametersText;

            public EventSnapshot(string eventName, int count, float firstRunSeconds, float lastRunSeconds, string lastParametersText)
            {
                EventName = eventName;
                Count = count;
                FirstRunSeconds = firstRunSeconds;
                LastRunSeconds = lastRunSeconds;
                LastParametersText = lastParametersText;
            }
        }

        private sealed class EventState
        {
            public int Count;
            public float FirstRunSeconds = -1.0f;
            public float LastRunSeconds = -1.0f;
            public string LastParametersText = string.Empty;
        }

        private static readonly HashSet<string> LoggedOnceEvents = new HashSet<string>();
        private static readonly Dictionary<string, EventState> EventStates = new Dictionary<string, EventState>();
        private static float _runStartTime;
        private static float _lastRecordedRunSeconds;
        private static float _runEndSeconds = -1.0f;
        private static bool _runEnded;
        private static bool _runStarted;

        public static float RunElapsedSeconds => !_runStarted
            ? 0.0f
            : _runEnded && _runEndSeconds >= 0.0f
                ? _runEndSeconds
                : Mathf.Max(0.0f, Time.time - _runStartTime);
        public static string RunTimeSecondsParameter => $"run_time_seconds={RunElapsedSeconds.ToString("0.0", CultureInfo.InvariantCulture)}";
        public static bool IsRunEnded => _runEnded;

        public static void BeginRun(
            Lizzo.PV.Flow.RunMode mode = Lizzo.PV.Flow.RunMode.Normal,
            string configVersion = null,
            string configAssignmentHash = null,
            string selectedWeaponId = null)
        {
            EventStates.Clear();
            P0DeathReasonTracker.Reset();
            P0BossDpsTracker.Reset();
            P0PlaytestDiagnostics.Reset();
            _runStartTime = Time.time;
            _lastRecordedRunSeconds = 0.0f;
            _runEndSeconds = -1.0f;
            _runEnded = false;
            _runStarted = true;
            BeginRunLog();
            ResetPerformanceSamples();
            if (InternalBuildInfo.TryLoadRuntime(out InternalBuildInfo buildInfo))
                LogOnce(BuildIdentity, buildInfo.ToTelemetryParameters());
            else
                LogOnce(BuildIdentityMissing, "reason=runtime_payload_unavailable");
            LogOnce(AnalyticsReady, "provider=local_console");
            LogOnce(AppsFlyerInstallReady, "provider=local_console_placeholder");
            LogOnce(Install, "source=prototype_session");
            if (mode == Lizzo.PV.Flow.RunMode.Tutorial)
                LogOnce(TutorialStart, "stage=stage1");
            Log(
                RunStart,
                "stage=stage1",
                $"run_mode={mode.ToString().ToLowerInvariant()}",
                $"build_version={Application.version}",
                $"config_version={configVersion ?? string.Empty}",
                $"config_assignment_hash={configAssignmentHash ?? string.Empty}",
                $"selected_weapon_id={selectedWeaponId ?? "unconfigured"}");
            P0PlaytestDiagnostics.LogCombatReadabilityCheck("run_start");
        }

        public static void EndRun(string result, int bossHpPercent)
        {
            if (_runEnded)
                return;

            _runEndSeconds = Mathf.Max(_lastRecordedRunSeconds, Mathf.Max(0.0f, Time.time - _runStartTime));
            _runEnded = true;
            int durationSeconds = Mathf.Max(0, Mathf.RoundToInt(_runEndSeconds));
            LogPerformanceSummary();
            P0PlaytestDiagnostics.LogRunEndSummaries(result);
            Log(RunEnd, $"result={result}", $"duration_seconds={durationSeconds}", $"boss_hp_percent={bossHpPercent}");
            FlushRunLog("run_end");
        }

        public static bool HasLogged(string eventName)
        {
            return EventStates.ContainsKey(eventName);
        }

        public static int GetCount(string eventName)
        {
            return EventStates.TryGetValue(eventName, out EventState state) ? state.Count : 0;
        }

        public static bool TryGetEventSnapshot(string eventName, out EventSnapshot snapshot)
        {
            if (EventStates.TryGetValue(eventName, out EventState state))
            {
                snapshot = new EventSnapshot(
                    eventName,
                    state.Count,
                    state.FirstRunSeconds,
                    state.LastRunSeconds,
                    state.LastParametersText);
                return true;
            }

            snapshot = default;
            return false;
        }
    }
}
