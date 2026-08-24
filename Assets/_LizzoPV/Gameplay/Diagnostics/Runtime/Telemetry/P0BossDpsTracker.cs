using System.Collections.Generic;
using Lizzo.PV.P0.Units;
using UnityEngine;

namespace Lizzo.PV.P0.Telemetry
{
    public static partial class P0BossDpsTracker
    {
        private const float FIRST_PROBE_SECONDS = 30.0f;
        private const string NONE_TARGET_ID = "none";

        private static readonly Dictionary<string, int> BossDamageBySource = new Dictionary<string, int>();
        private static readonly Dictionary<string, string> LastTargetBySource = new Dictionary<string, string>();

        private static bool _isActive;
        private static bool _hasLoggedFirstProbe;
        private static float _bossStartTime;
        private static int _targetCastCount;
        private static int _bossTargetCastCount;
        private static int _attackCastCount;
        private static int _skillCastCount;
        private static int _totalBossDamage;

        public static void Reset()
        {
            _isActive = false;
            _hasLoggedFirstProbe = false;
            _bossStartTime = 0.0f;
            _targetCastCount = 0;
            _bossTargetCastCount = 0;
            _attackCastCount = 0;
            _skillCastCount = 0;
            _totalBossDamage = 0;
            BossDamageBySource.Clear();
            LastTargetBySource.Clear();
        }

        public static void BeginBossFight(MonsterController boss)
        {
            Reset();
            if (boss == null || IsBossTarget(boss) == false)
                return;

            _isActive = true;
            _bossStartTime = Time.time;
        }

        public static void Tick()
        {
            if (_isActive == false || _hasLoggedFirstProbe)
                return;

            if (GetBossElapsedSeconds() < FIRST_PROBE_SECONDS)
                return;

            _hasLoggedFirstProbe = true;
            LogSummaries("probe_30s");
        }

        private static float GetBossElapsedSeconds()
        {
            return Mathf.Max(0.0f, Time.time - _bossStartTime);
        }
    }
}
