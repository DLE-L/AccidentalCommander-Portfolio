using System.Collections.Generic;

namespace Lizzo.PV.Gameplay.Telemetry
{
    public static partial class RunDiagnostics
    {
        private static void Increment(Dictionary<string, int> counts, string key)
        {
            key = NormalizeKey(key);
            counts[key] = counts.TryGetValue(key, out int count) ? count + 1 : 1;
        }

        private static void AddValue(Dictionary<string, int> counts, string key, int amount)
        {
            key = NormalizeKey(key);
            counts[key] = counts.TryGetValue(key, out int count) ? count + amount : amount;
        }

        private static void AddLifetime(string enemyId, float lifetime)
        {
            enemyId = string.IsNullOrEmpty(enemyId) ? UNKNOWN_ENEMY_ID : enemyId;
            LifetimeSums[enemyId] = LifetimeSums.TryGetValue(enemyId, out float sum) ? sum + lifetime : lifetime;
            LifetimeCounts[enemyId] = LifetimeCounts.TryGetValue(enemyId, out int count) ? count + 1 : 1;
        }

        private static void AddFloatValue(Dictionary<string, float> sums, Dictionary<string, int> counts, string key, float amount)
        {
            key = NormalizeKey(key);
            sums[key] = sums.TryGetValue(key, out float sum) ? sum + amount : amount;
            counts[key] = counts.TryGetValue(key, out int count) ? count + 1 : 1;
        }

        private static int GetCount(Dictionary<string, int> counts, string key)
        {
            key = NormalizeKey(key);
            return counts.TryGetValue(key, out int count) ? count : 0;
        }

        private static float GetAverageLifetime(string enemyId)
        {
            enemyId = NormalizeKey(enemyId);
            if (LifetimeCounts.TryGetValue(enemyId, out int count) == false || count <= 0)
                return -1.0f;

            return LifetimeSums.TryGetValue(enemyId, out float sum) ? sum / count : -1.0f;
        }
    }
}
