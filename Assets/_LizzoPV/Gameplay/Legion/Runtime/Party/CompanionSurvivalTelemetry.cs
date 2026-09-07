using Lizzo.PV.Gameplay.Telemetry;
using Lizzo.PV.P0.Units;

namespace Lizzo.PV.Legion
{
    internal static class CompanionBossRepeatBlockTelemetry
    {
        internal static void Log(CompanionRuntime owner, MonsterController monster, string patternId, float remaining)
        {
            EnemyRuntimeStats stats = monster.RuntimeStats;
            string sourceId = stats?.Data?.Id ?? monster.gameObject.name;
            RunTelemetry.Log(RunTelemetry.BossPatternRepeatBlock,
                $"target={owner.UnitId}", $"enemy_id={sourceId}", $"pattern_id={patternId}", $"remaining={remaining:0.##}");
        }
    }
    internal static class CompanionDamageTelemetry
    {
        internal static void Record(CompanionRuntime owner, int damage, int originalDamage, string source)
        {
            int hpPercent = CompanionSurvivalHealthMath.HpPercent(owner);
            RunTelemetry.Log(RunTelemetry.HurtboxContact, $"target={owner.UnitId}", $"source={source}");
            RunTelemetry.Log(RunTelemetry.DamageApply, $"target={owner.UnitId}", $"damage={damage}", $"source={source}", $"original_damage={originalDamage}");
            RunTelemetry.Log(RunTelemetry.CompanionDamage, $"unit_id={owner.UnitId}", $"damage={damage}", $"hp_percent={hpPercent}", $"source={source}");
            RunDiagnostics.RecordCompanionDamage(owner.UnitId, damage, hpPercent, source);
            RunDiagnostics.RecordEnemyContactDamage(source);
        }
    }
    internal static class CompanionStateTransitionTelemetry
    {
        internal static void Down(CompanionRuntime owner, string source)
        {
            RunTelemetry.Log(RunTelemetry.CompanionDown, $"unit_id={owner.UnitId}", $"family_tags_snapshot={owner.FamilyTags}",
                $"promoted_state={owner.Promoted}", $"down_duration={owner.Party.Tuning.CompanionDownDuration:0.##}", $"source={source}", $"slot_id={owner.SlotId}");
            RunDiagnostics.RecordCompanionDown(owner.UnitId, source);
        }
        internal static void Recover(CompanionRuntime owner, string source, string priorityReason)
        {
            RunTelemetry.Log(RunTelemetry.CompanionRecover, $"unit_id={owner.UnitId}", $"family_tags_snapshot={owner.FamilyTags}",
                $"promoted_state={owner.Promoted}", $"hp_percent={CompanionSurvivalHealthMath.HpPercent(owner)}",
                $"source={source}", $"priority_reason={priorityReason}", $"slot_id={owner.SlotId}");
        }
    }

}
