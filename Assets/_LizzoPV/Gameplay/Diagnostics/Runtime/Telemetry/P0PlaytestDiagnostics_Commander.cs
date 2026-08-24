using Lizzo.PV.P0.Combat;
using UnityEngine;

namespace Lizzo.PV.P0.Telemetry
{
    public static partial class P0PlaytestDiagnostics
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
            P0Telemetry.Log(
                P0Telemetry.CommanderDamageSummary,
                $"reason={normalizedReason}",
                $"total_damage={_commanderDamageTotal}",
                $"hit_count={_commanderHitCount}",
                $"last_hp_percent={_commanderLastHpPercent}",
                $"damage_by_source={FormatCounts(CommanderDamageBySource)}",
                $"hits_by_source={FormatCounts(CommanderHitsBySource)}");

            P0Telemetry.Log(
                P0Telemetry.PlayerDamageBySource,
                $"reason={normalizedReason}",
                $"damage_by_source={FormatCounts(CommanderDamageBySource)}",
                $"damage_by_pattern={FormatCounts(CommanderDamageByPattern)}");

            P0Telemetry.Log(
                P0Telemetry.HurtboxContactCommander,
                $"reason={normalizedReason}",
                $"contacts={FormatCounts(CommanderHurtboxContacts)}");

            P0Telemetry.Log(
                P0Telemetry.NormalEnemyContactDamage,
                $"reason={normalizedReason}",
                $"hits={FormatCounts(NormalEnemyContactDamageCounts)}");

            P0Telemetry.Log(
                P0Telemetry.RedChargerImpactHit,
                $"reason={normalizedReason}",
                $"hits_by_pattern={FormatCounts(RedChargerImpactHitCounts)}");

            P0Telemetry.Log(
                P0Telemetry.RedChargerImpactGraceHit,
                $"reason={normalizedReason}",
                $"hits_by_pattern={FormatCounts(RedChargerImpactGraceHitCounts)}");
        }
    }
}
