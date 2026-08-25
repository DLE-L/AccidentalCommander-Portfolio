using System.Collections.Generic;
using System.Text;
using Lizzo.PV.P0.Combat;
using Lizzo.PV.P0.Config;
using UnityEngine;

namespace Lizzo.PV.P0.Skills.Guard
{
    internal sealed class GuardSquadEnemyCountFormatter
    {
        readonly StringBuilder _builder = new StringBuilder(120);

        internal string Format(Dictionary<string, int> counts)
        {
            if (counts.Count == 0)
                return "none";

            _builder.Clear();
            foreach (KeyValuePair<string, int> pair in counts)
            {
                if (_builder.Length > 0)
                    _builder.Append(';');
                _builder.Append(pair.Key);
                _builder.Append('=');
                _builder.Append(pair.Value);
            }
            return _builder.ToString();
        }
    }

    internal sealed class GuardSquadCastTargetLedger
    {
        readonly HashSet<int> _countedTargets = new HashSet<int>();

        internal int TargetCount => _countedTargets.Count;
        internal bool HitBoss { get; private set; }

        internal void Record(int targetKey, bool isBoss)
        {
            _countedTargets.Add(targetKey);
            HitBoss |= isBoss;
        }
    }

    internal sealed class GuardSquadDamageLedger
    {
        const int DamageTargetCap = 3;
        const int FirstCastMaxKillTargets = 2;

        readonly HashSet<int> _damagedTargets = new HashSet<int>();
        readonly Dictionary<string, int> _damagedEnemyCounts = new Dictionary<string, int>();
        readonly Dictionary<string, int> _killedEnemyCounts = new Dictionary<string, int>();

        internal int DamagedTargetCount { get; private set; }
        internal int KillCount { get; private set; }
        internal int TotalDamageApplied { get; private set; }
        internal Dictionary<string, int> DamagedEnemyCounts => _damagedEnemyCounts;
        internal Dictionary<string, int> KilledEnemyCounts => _killedEnemyCounts;

        internal bool CanApply(int targetKey)
        {
            return _damagedTargets.Contains(targetKey) == false && DamagedTargetCount < DamageTargetCap;
        }

        internal void MarkAttempt(int targetKey)
        {
            _damagedTargets.Add(targetKey);
        }

        internal int LimitDamage(bool isFirstActivationCast, int damage, int hpBefore)
        {
            if (isFirstActivationCast && KillCount >= FirstCastMaxKillTargets && damage >= hpBefore)
                return hpBefore > 1 ? hpBefore - 1 : 0;
            return damage;
        }

        internal void RecordResult(string enemyId, int hpBefore, int hpAfter)
        {
            int appliedDamage = Mathf.Max(0, hpBefore - hpAfter);
            if (appliedDamage > 0)
            {
                DamagedTargetCount++;
                TotalDamageApplied += appliedDamage;
                Increment(_damagedEnemyCounts, enemyId);
            }
            if (hpBefore > 0 && hpAfter <= 0)
            {
                KillCount++;
                Increment(_killedEnemyCounts, enemyId);
            }
        }

        static void Increment(Dictionary<string, int> counts, string key)
        {
            key = CombatIds.Normalize(key);
            counts[key] = counts.TryGetValue(key, out int count) ? count + 1 : 1;
        }
    }

    internal sealed class GuardSquadPushLedger
    {
        const int RadialPushTargetCap = 8;

        readonly HashSet<int> _pushedTargets = new HashSet<int>();
        readonly Dictionary<string, int> _pushedEnemyCounts = new Dictionary<string, int>();

        internal int PushCount { get; private set; }
        internal int TargetCap => RadialPushTargetCap;
        internal Dictionary<string, int> PushedEnemyCounts => _pushedEnemyCounts;

        internal bool CanApply(int targetKey)
        {
            return _pushedTargets.Contains(targetKey) || PushCount < RadialPushTargetCap;
        }

        internal void Record(int targetKey, string enemyId)
        {
            if (_pushedTargets.Add(targetKey) == false)
                return;

            PushCount++;
            enemyId = CombatIds.Normalize(enemyId);
            _pushedEnemyCounts[enemyId] = _pushedEnemyCounts.TryGetValue(enemyId, out int count)
                ? count + 1
                : 1;
        }
    }

}
