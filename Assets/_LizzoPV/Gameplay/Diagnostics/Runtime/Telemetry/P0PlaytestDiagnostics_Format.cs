using System.Collections.Generic;
using Lizzo.PV.P0.Combat;
using UnityEngine;

namespace Lizzo.PV.P0.Telemetry
{
    public static partial class P0PlaytestDiagnostics
    {
        private static string FormatCounts(Dictionary<string, int> counts)
        {
            if (counts.Count == 0)
                return "none";

            Builder.Clear();
            foreach (KeyValuePair<string, int> pair in counts)
            {
                if (Builder.Length > 0)
                    Builder.Append(';');

                Builder.Append(pair.Key);
                Builder.Append('=');
                Builder.Append(pair.Value);
            }

            return Builder.ToString();
        }

        private static string FormatAverageLifetimes()
        {
            if (LifetimeCounts.Count == 0)
                return "none";

            Builder.Clear();
            foreach (KeyValuePair<string, int> pair in LifetimeCounts)
            {
                if (pair.Value <= 0 || LifetimeSums.TryGetValue(pair.Key, out float sum) == false)
                    continue;

                if (Builder.Length > 0)
                    Builder.Append(';');

                Builder.Append(pair.Key);
                Builder.Append('=');
                Builder.Append((sum / pair.Value).ToString("0.0"));
            }

            return Builder.Length == 0 ? "none" : Builder.ToString();
        }

        private static string FormatFloatAverages(Dictionary<string, float> sums, Dictionary<string, int> counts)
        {
            if (counts.Count == 0)
                return "none";

            Builder.Clear();
            foreach (KeyValuePair<string, int> pair in counts)
            {
                if (pair.Value <= 0 || sums.TryGetValue(pair.Key, out float sum) == false)
                    continue;

                if (Builder.Length > 0)
                    Builder.Append(';');

                Builder.Append(pair.Key);
                Builder.Append('=');
                Builder.Append((sum / pair.Value).ToString("0.0"));
            }

            return Builder.Length == 0 ? "none" : Builder.ToString();
        }

        private static EnemyAnalysisRecord GetOrCreateEnemyRecord(global::MonsterController monster)
        {
            int instanceId = monster.GetInstanceID();
            if (EnemyRecords.TryGetValue(instanceId, out EnemyAnalysisRecord record))
                return record;

            record = new EnemyAnalysisRecord(ResolveEnemyId(monster), Time.time);
            EnemyRecords[instanceId] = record;
            return record;
        }

        private static void FinalizeEnemyRecord(EnemyAnalysisRecord record)
        {
            if (record == null)
                return;

            string enemyId = NormalizeKey(record.EnemyId);
            if (record.FirstDamagedTime >= 0.0f)
            {
                AddFloatValue(FirstDamageTimeSums, FirstDamageTimeCounts, enemyId, Mathf.Max(0.0f, record.FirstDamagedTime - record.SpawnTime));
                AddFloatValue(DamageWindowSums, DamageWindowCounts, enemyId, Mathf.Max(0.0f, record.LastHitTime - record.FirstDamagedTime));
                AddFloatValue(LastHitTimeSums, LastHitTimeCounts, enemyId, Mathf.Max(0.0f, record.LastHitTime - record.SpawnTime));
            }

            if (record.FirstTargetedTime >= 0.0f)
            {
                AddFloatValue(FirstTargetTimeSums, FirstTargetTimeCounts, enemyId, Mathf.Max(0.0f, record.FirstTargetedTime - record.SpawnTime));
                AddFloatValue(TargetWindowSums, TargetWindowCounts, enemyId, Mathf.Max(0.0f, record.LastTargetedTime - record.FirstTargetedTime));
            }

            Increment(EnemyKilledByCounts, $"{enemyId}:{NormalizeKey(record.KilledBy)}");
        }

        private static string ResolveEnemyIdFromDamageSource(string source)
        {
            if (string.IsNullOrEmpty(source))
                return UNKNOWN_ENEMY_ID;

            int separatorIndex = source.IndexOf(':');
            return separatorIndex <= 0 ? NormalizeKey(source) : NormalizeKey(source.Substring(0, separatorIndex));
        }

        private static bool IsEliteOrBossEnemy(string enemyId)
        {
            enemyId = NormalizeKey(enemyId);
            return enemyId == RED_CHARGER_ID || enemyId.StartsWith("boss_");
        }

        private static bool IsBossDamageSource(string source)
        {
            return CombatIds.IsBossPattern(NormalizeKey(source));
        }

        private static string NormalizeReason(string reason)
        {
            return string.IsNullOrEmpty(reason) ? "unknown" : reason;
        }

        private static string NormalizeKey(string key)
        {
            return string.IsNullOrEmpty(key) ? UNKNOWN_ENEMY_ID : key;
        }
    }
}
