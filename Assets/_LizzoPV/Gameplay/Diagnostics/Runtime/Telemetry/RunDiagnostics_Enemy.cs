using Lizzo.PV.Legion;
using Lizzo.PV.Gameplay.Units;
using UnityEngine;

namespace Lizzo.PV.Gameplay.Telemetry
{
    public static partial class RunDiagnostics
    {
        public static void RegisterEnemySpawn(global::MonsterController monster)
        {
            if (monster == null)
                return;

            string enemyId = ResolveEnemyId(monster);
            int instanceId = monster.GetInstanceID();
            SpawnRecords[instanceId] = new EnemySpawnRecord(enemyId, Time.time);
            EnemyRecords[instanceId] = new EnemyAnalysisRecord(enemyId, Time.time);
            Increment(SpawnCounts, enemyId);
        }

        public static void RegisterEnemyDeath(global::MonsterController monster)
        {
            if (monster == null)
                return;

            string enemyId = ResolveEnemyId(monster);
            Increment(DeathCounts, enemyId);

            int instanceId = monster.GetInstanceID();
            if (SpawnRecords.TryGetValue(instanceId, out EnemySpawnRecord record))
            {
                float lifetime = Mathf.Max(0.0f, Time.time - record.Time);
                AddLifetime(record.EnemyId, lifetime);
                if (EnemyRecords.TryGetValue(instanceId, out EnemyAnalysisRecord analysisRecord))
                {
                    FinalizeEnemyRecord(analysisRecord);
                    EnemyRecords.Remove(instanceId);
                }

                SpawnRecords.Remove(instanceId);
            }
            else if (EnemyRecords.TryGetValue(instanceId, out EnemyAnalysisRecord analysisRecord))
            {
                FinalizeEnemyRecord(analysisRecord);
                EnemyRecords.Remove(instanceId);
            }
        }

        public static void RecordEnemyTargeted(global::MonsterController monster, string sourceId)
        {
            if (monster == null)
                return;

            EnemyAnalysisRecord record = GetOrCreateEnemyRecord(monster);
            float now = Time.time;
            if (record.FirstTargetedTime < 0.0f)
                record.FirstTargetedTime = now;

            record.LastTargetedTime = now;
        }

        public static void RecordEnemyDamage(global::MonsterController monster, string sourceId, int damageTaken, bool willKill)
        {
            if (monster == null || damageTaken <= 0)
                return;

            EnemyAnalysisRecord record = GetOrCreateEnemyRecord(monster);
            string enemyId = record.EnemyId;
            float now = Time.time;
            if (record.FirstDamagedTime < 0.0f)
                record.FirstDamagedTime = now;

            record.LastHitTime = now;
            record.TotalDamageTaken += damageTaken;
            AddValue(EnemyDamageTaken, enemyId, damageTaken);

            if (willKill)
                record.KilledBy = NormalizeKey(sourceId);
        }

        public static void RecordEnemyContactDamage(global::MonsterController monster)
        {
            if (monster == null)
                return;

            EnemyAnalysisRecord record = GetOrCreateEnemyRecord(monster);
            record.ContactDamageCount++;
            Increment(EnemyContactDamageCounts, record.EnemyId);
        }

        public static void RecordEnemyContactDamage(string source)
        {
            string enemyId = ResolveEnemyIdFromDamageSource(source);
            Increment(EnemyContactDamageCounts, enemyId);
        }

        private static string ResolveEnemyId(global::MonsterController monster)
        {
            if (monster == null)
                return UNKNOWN_ENEMY_ID;

            EnemyRuntimeStats stats = monster.RuntimeStats;
            if (stats != null && stats.Data != null && string.IsNullOrEmpty(stats.Data.Id) == false)
                return stats.Data.Id;

            return UNKNOWN_ENEMY_ID;
        }

    }
}
