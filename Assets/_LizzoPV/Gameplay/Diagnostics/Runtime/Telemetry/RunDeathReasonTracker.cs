using Lizzo.PV.Gameplay.Combat;
using Lizzo.PV.P0.Units;

namespace Lizzo.PV.Gameplay.Telemetry
{
    public static class RunDeathReasonTracker
    {
        private const string FALLBACK_REASON = "overrun";

        public static string LastReasonCode { get; private set; } = FALLBACK_REASON;
        public static string LastEnemyId { get; private set; } = string.Empty;
        public static string LastEnemyType { get; private set; } = string.Empty;
        public static string LastEnemyName { get; private set; } = string.Empty;
        public static string LastPatternId { get; private set; } = string.Empty;

        public static void Reset()
        {
            LastReasonCode = FALLBACK_REASON;
            LastEnemyId = string.Empty;
            LastEnemyType = string.Empty;
            LastEnemyName = string.Empty;
            LastPatternId = string.Empty;
        }

        public static void RecordEnemyDamage(MonsterController attacker, string patternId)
        {
            LastReasonCode = FALLBACK_REASON;
            LastPatternId = string.IsNullOrEmpty(patternId) ? CombatIds.ContactAttack : patternId;

            if (attacker == null)
            {
                LastEnemyId = string.Empty;
                LastEnemyType = string.Empty;
                LastEnemyName = string.Empty;
                return;
            }

            EnemyRuntimeStats stats = attacker.GetComponent<EnemyRuntimeStats>();
            LastEnemyId = stats?.Data?.Id ?? attacker.name;
            LastEnemyType = string.IsNullOrWhiteSpace(attacker.EnemyType)
                ? CombatIds.Unknown
                : attacker.EnemyType;
            LastEnemyName = stats?.Data?.DisplayName ?? attacker.name;
        }

    }
}
