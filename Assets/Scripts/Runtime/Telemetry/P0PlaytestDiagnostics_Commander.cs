using Lizzo.PV.P0.Combat;
using UnityEngine;

namespace Lizzo.PV.P0.Telemetry
{
    public static partial class P0PlaytestDiagnostics
    {
        public static void LogPlayerMovementSample(global::PlayerController player)
        {
            if (player == null)
                return;

            Vector3 position = player.transform.position;
            float now = Time.time;
            float sampleSeconds = _hasMovementSample ? Mathf.Max(0.001f, now - _lastMovementSampleTime) : 0.0f;
            float movedDistance = _hasMovementSample ? Vector3.Distance(position, _lastMovementPosition) : 0.0f;
            float speed = sampleSeconds <= 0.0f ? 0.0f : movedDistance / sampleSeconds;
            float nearestEnemyDistance = ResolveNearestEnemyDistance(position);
            Vector2 moveDir = player.Services?.Registry?.Player?.MoveDirection ?? Vector2.zero;

            P0Telemetry.Log(
                P0Telemetry.PlayerMovementSample,
                $"x={position.x:0.00}",
                $"y={position.y:0.00}",
                $"moved={movedDistance:0.00}",
                $"speed={speed:0.00}",
                $"input={moveDir.magnitude:0.00}",
                $"nearest_enemy={nearestEnemyDistance:0.00}",
                $"alive_total={ResolveAliveCount()}");

            _lastMovementPosition = position;
            _lastMovementSampleTime = now;
            _hasMovementSample = true;
        }

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
