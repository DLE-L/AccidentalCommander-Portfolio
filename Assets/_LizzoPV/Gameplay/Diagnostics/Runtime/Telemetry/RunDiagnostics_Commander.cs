using Lizzo.PV.Gameplay.Combat;
using UnityEngine;

namespace Lizzo.PV.Gameplay.Telemetry
{
    public static partial class RunDiagnostics
    {
        public static void RecordCommanderHurtboxContact(string enemyId, string patternId)
        {
            enemyId = NormalizeKey(enemyId);
            patternId = NormalizeKey(patternId);
            Increment(CommanderHurtboxContacts, $"{enemyId}:{patternId}");
        }

        public static void RecordCommanderDamage(string enemyId, string patternId, int damage, int hpPercent)
        {
            if (damage <= 0)
                return;

            enemyId = NormalizeKey(enemyId);
            patternId = NormalizeKey(patternId);
            AddValue(CommanderDamageBySource, enemyId, damage);
            Increment(CommanderHitsBySource, enemyId);
            AddValue(CommanderDamageByPattern, patternId, damage);
            _commanderDamageTotal += damage;
            _commanderHitCount++;
            _commanderLastHpPercent = Mathf.Clamp(hpPercent, 0, 100);

            if (patternId == CombatIds.ContactAttack && IsEliteOrBossEnemy(enemyId) == false)
                Increment(NormalEnemyContactDamageCounts, enemyId);

            if (enemyId == RED_CHARGER_ID && patternId == CombatIds.RedChargerImpactGrace)
                Increment(RedChargerImpactGraceHitCounts, patternId);
            else if (enemyId == RED_CHARGER_ID && patternId == CombatIds.RedChargerDash)
                Increment(RedChargerImpactHitCounts, patternId);
        }

        public static void LogCommanderDamageSummary(string reason)
        {
            string normalizedReason = NormalizeReason(reason);
            RunTelemetry.Log(
                RunTelemetry.CommanderDamageSummary,
                $"reason={normalizedReason}",
                $"total_damage={_commanderDamageTotal}",
                $"hit_count={_commanderHitCount}",
                $"last_hp_percent={_commanderLastHpPercent}",
                $"damage_by_source={FormatCounts(CommanderDamageBySource)}",
                $"hits_by_source={FormatCounts(CommanderHitsBySource)}");

            RunTelemetry.Log(
                RunTelemetry.PlayerDamageBySource,
                $"reason={normalizedReason}",
                $"damage_by_source={FormatCounts(CommanderDamageBySource)}",
                $"damage_by_pattern={FormatCounts(CommanderDamageByPattern)}");

            RunTelemetry.Log(
                RunTelemetry.HurtboxContactCommander,
                $"reason={normalizedReason}",
                $"contacts={FormatCounts(CommanderHurtboxContacts)}");

            RunTelemetry.Log(
                RunTelemetry.NormalEnemyContactDamage,
                $"reason={normalizedReason}",
                $"hits={FormatCounts(NormalEnemyContactDamageCounts)}");

            RunTelemetry.Log(
                RunTelemetry.RedChargerImpactHit,
                $"reason={normalizedReason}",
                $"hits_by_pattern={FormatCounts(RedChargerImpactHitCounts)}");

            RunTelemetry.Log(
                RunTelemetry.RedChargerImpactGraceHit,
                $"reason={normalizedReason}",
                $"hits_by_pattern={FormatCounts(RedChargerImpactGraceHitCounts)}");
        }
    }
}
