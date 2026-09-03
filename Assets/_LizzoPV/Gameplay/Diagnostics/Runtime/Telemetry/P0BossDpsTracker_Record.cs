using Lizzo.PV.P0.Combat;
using Lizzo.PV.P0.Units;
using UnityEngine;

namespace Lizzo.PV.P0.Telemetry
{
    public static partial class P0BossDpsTracker
    {
        public static void RecordAttackCast(string sourceId, MonsterController target)
        {
            P0PlaytestDiagnostics.RecordEnemyTargeted(target, sourceId);

            if (_isActive == false)
                return;

            Tick();
            string safeSourceId = NormalizeSourceId(sourceId);
            string targetId = ResolveTargetId(target);
            bool isBossTarget = IsBossTarget(target);

            _attackCastCount++;
            RecordTargetCast(isBossTarget);

            P0Telemetry.Log(
                P0Telemetry.AttackCast,
                "phase=boss",
                $"source_id={safeSourceId}",
                $"target_id={targetId}",
                $"is_boss={isBossTarget.ToString().ToLowerInvariant()}");

            if (LastTargetBySource.TryGetValue(safeSourceId, out string lastTargetId) && lastTargetId == targetId)
                return;

            LastTargetBySource[safeSourceId] = targetId;
            P0Telemetry.Log(
                P0Telemetry.TargetAcquired,
                "phase=boss",
                $"source_id={safeSourceId}",
                $"target_id={targetId}",
                $"is_boss={isBossTarget.ToString().ToLowerInvariant()}");
        }

        public static void RecordSkillCast(string sourceId, string skillId, int targetCount, bool hitBoss)
        {
            if (_isActive == false)
                return;

            Tick();
            string safeSourceId = NormalizeSourceId(sourceId);
            _skillCastCount++;
            RecordTargetCast(hitBoss);

            P0Telemetry.Log(
                P0Telemetry.SkillCast,
                "phase=boss",
                $"source_id={safeSourceId}",
                $"skill_id={skillId}",
                $"target_count={Mathf.Max(0, targetCount)}",
                $"hit_boss={hitBoss.ToString().ToLowerInvariant()}");
        }

        public static void RecordBossDamage(string sourceId, MonsterController target, int damage)
        {
            if (_isActive == false || damage <= 0 || IsBossTarget(target) == false)
                return;

            Tick();
            string safeSourceId = NormalizeSourceId(sourceId);
            int resolvedDamage = HungryGiantBehaviour.ResolveIncomingDamageForBossTarget(target, damage);
            int actualDamage = Mathf.Min(Mathf.Max(0, target.Hp), resolvedDamage);
            if (actualDamage <= 0)
                return;

            _totalBossDamage += actualDamage;
            if (BossDamageBySource.ContainsKey(safeSourceId))
                BossDamageBySource[safeSourceId] += actualDamage;
            else
                BossDamageBySource[safeSourceId] = actualDamage;
        }

        public static bool IsBossTarget(MonsterController target)
        {
            if (target == null)
                return false;

            return target.IsBoss;
        }

        private static void RecordTargetCast(bool isBossTarget)
        {
            _targetCastCount++;
            if (isBossTarget)
                _bossTargetCastCount++;
        }

        private static string ResolveTargetId(MonsterController target)
        {
            return target == null ? NONE_TARGET_ID : target.GetDamageEnemyId();
        }

        private static string NormalizeSourceId(string sourceId)
        {
            return string.IsNullOrEmpty(sourceId) ? "unknown" : sourceId;
        }
    }
}
