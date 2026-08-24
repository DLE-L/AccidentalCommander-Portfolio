namespace Lizzo.PV.P0.Telemetry
{
    public static partial class P0PlaytestDiagnostics
    {
        private readonly struct EnemySpawnRecord
        {
            public EnemySpawnRecord(string enemyId, float time)
            {
                EnemyId = enemyId;
                Time = time;
            }

            public string EnemyId { get; }
            public float Time { get; }
        }

        private sealed class EnemyAnalysisRecord
        {
            public EnemyAnalysisRecord(string enemyId, float spawnTime)
            {
                EnemyId = NormalizeKey(enemyId);
                SpawnTime = spawnTime;
            }

            public string EnemyId { get; }
            public float SpawnTime { get; }
            public float FirstDamagedTime { get; set; } = -1.0f;
            public float LastHitTime { get; set; } = -1.0f;
            public float FirstTargetedTime { get; set; } = -1.0f;
            public float LastTargetedTime { get; set; } = -1.0f;
            public int TotalDamageTaken { get; set; }
            public int ContactDamageCount { get; set; }
            public string KilledBy { get; set; } = "unknown";
        }
    }
}
