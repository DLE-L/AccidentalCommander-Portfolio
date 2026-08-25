using Lizzo.PV.P0.Config;
using Lizzo.PV.P0.Telemetry;
using Lizzo.PV.P0.Units;

namespace Lizzo.PV.Legion
{
    internal static class CompanionBossRepeatBlockTelemetry
    {
        internal static void Log(CompanionRuntime owner, MonsterController monster, string patternId, float remaining)
        {
            EnemyRuntimeStats stats = monster.RuntimeStats;
            string sourceId = stats?.Data?.Id ?? monster.gameObject.name;
            P0Telemetry.Log(P0Telemetry.BossPatternRepeatBlock,
                $"target={owner.UnitId}", $"enemy_id={sourceId}", $"pattern_id={patternId}", $"remaining={remaining:0.##}");
        }
    }
    internal static class CompanionDamageTelemetry
    {
        internal static void Record(CompanionRuntime owner, int damage, int originalDamage, string source)
        {
            int hpPercent = CompanionSurvivalHealthMath.HpPercent(owner);
            P0Telemetry.Log(P0Telemetry.HurtboxContact, $"target={owner.UnitId}", $"source={source}");
            P0Telemetry.Log(P0Telemetry.DamageApply, $"target={owner.UnitId}", $"damage={damage}", $"source={source}", $"original_damage={originalDamage}");
            P0Telemetry.Log(P0Telemetry.CompanionDamage, $"unit_id={owner.UnitId}", $"damage={damage}", $"hp_percent={hpPercent}", $"source={source}");
            P0PlaytestDiagnostics.RecordCompanionDamage(owner.UnitId, damage, hpPercent, source);
            P0PlaytestDiagnostics.RecordEnemyContactDamage(source);
        }
    }
    internal static class CompanionStateTransitionTelemetry
    {
        internal static void Down(CompanionRuntime owner, string source)
        {
            P0Telemetry.Log(P0Telemetry.CompanionDown, $"unit_id={owner.UnitId}", $"family_tags_snapshot={owner.FamilyTags}",
                $"promoted_state={owner.Promoted}", $"down_duration={RemoteConfig.CompanionDownDuration:0.##}", $"source={source}", $"slot_id={owner.SlotId}");
            P0PlaytestDiagnostics.RecordCompanionDown(owner.UnitId, source);
        }
        internal static void Recover(CompanionRuntime owner, string source, string priorityReason)
        {
            P0Telemetry.Log(P0Telemetry.CompanionRecover, $"unit_id={owner.UnitId}", $"family_tags_snapshot={owner.FamilyTags}",
                $"promoted_state={owner.Promoted}", $"hp_percent={CompanionSurvivalHealthMath.HpPercent(owner)}",
                $"source={source}", $"priority_reason={priorityReason}", $"slot_id={owner.SlotId}");
        }
    }

}
