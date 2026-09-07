using System.Collections.Generic;
using Lizzo.PV.Legion;
using UnityEngine;

namespace Lizzo.PV.Gameplay.Telemetry
{
    public static partial class RunDiagnostics
    {
        public static void RecordCompanionDamage(string unitId, int damage, int hpPercent, string source)
        {
            if (damage <= 0)
                return;

            unitId = NormalizeKey(unitId);
            source = NormalizeKey(source);
            AddValue(CompanionDamageByUnit, unitId, damage);
            Increment(CompanionHitsByUnit, unitId);
            AddValue(CompanionDamageBySource, source, damage);
            CompanionLastHpPercent[unitId] = Mathf.Clamp(hpPercent, 0, 100);
        }

        public static void RecordCompanionDown(string unitId, string source)
        {
            unitId = NormalizeKey(unitId);
            source = NormalizeKey(source);
            Increment(CompanionDownByUnit, unitId);
            Increment(CompanionDownBySource, source);

            if (RunTelemetry.HasLogged(RunTelemetry.FirstBossSeen))
            {
                _bossPhaseCompanionDownCount++;
                Increment(BossPhaseCompanionDownBySource, source);
                return;
            }

            _preBossCompanionDownCount++;
            Increment(PreBossCompanionDownByUnit, unitId);
            Increment(PreBossCompanionDownBySource, source);
        }

        public static void LogCompanionDamageSummary(string reason)
        {
            if (CompanionDamageByUnit.Count > 0 || CompanionDownByUnit.Count > 0)
            {
                RunTelemetry.Log(
                    RunTelemetry.CompanionDamageSummary,
                    $"reason={NormalizeReason(reason)}",
                    $"damage_by_unit={FormatCounts(CompanionDamageByUnit)}",
                    $"hits_by_unit={FormatCounts(CompanionHitsByUnit)}",
                    $"last_hp={FormatCounts(CompanionLastHpPercent)}",
                    $"damage_by_source={FormatCounts(CompanionDamageBySource)}",
                    $"down_by_unit={FormatCounts(CompanionDownByUnit)}",
                    $"down_by_source={FormatCounts(CompanionDownBySource)}");
            }

            LogCompanionDownSummaries(reason);
        }

        public static void LogBossCompanionDamageSummary(string reason)
        {
            ScratchCounts.Clear();
            foreach (KeyValuePair<string, int> pair in CompanionDamageBySource)
            {
                if (IsBossDamageSource(pair.Key) == false)
                    continue;

                AddValue(ScratchCounts, pair.Key, pair.Value);
            }

            RunTelemetry.Log(
                RunTelemetry.BossCompanionDamageSummary,
                $"reason={NormalizeReason(reason)}",
                $"damage_by_source={FormatCounts(ScratchCounts)}",
                $"boss_phase_down_by_source={FormatCounts(BossPhaseCompanionDownBySource)}");
        }

        private static void LogCompanionDownSummaries(string reason)
        {
            RunTelemetry.Log(
                RunTelemetry.CompanionDownCountPreBoss,
                $"reason={NormalizeReason(reason)}",
                $"down_count={_preBossCompanionDownCount}",
                $"down_by_unit={FormatCounts(PreBossCompanionDownByUnit)}",
                $"down_by_source={FormatCounts(PreBossCompanionDownBySource)}",
                $"has_cleric={HasCanonicalCleric().ToString().ToLowerInvariant()}");

            RunTelemetry.Log(
                RunTelemetry.CompanionDownReasonSummary,
                $"reason={NormalizeReason(reason)}",
                $"pre_boss_down={_preBossCompanionDownCount}",
                $"boss_phase_down={_bossPhaseCompanionDownCount}",
                $"all_down_by_source={FormatCounts(CompanionDownBySource)}",
                $"pre_boss_down_by_source={FormatCounts(PreBossCompanionDownBySource)}",
                $"boss_phase_down_by_source={FormatCounts(BossPhaseCompanionDownBySource)}");
        }

        private static bool HasCanonicalCleric()
        {
            return _party != null
                && _party.TryGetCanonicalCompanionProgress("cleric", out int ownedCount, out _)
                && ownedCount > 0;
        }
    }
}
