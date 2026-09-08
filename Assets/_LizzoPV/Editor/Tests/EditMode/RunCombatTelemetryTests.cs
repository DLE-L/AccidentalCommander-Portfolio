using System.IO;
using Lizzo.PV.Combat;
using Lizzo.PV.Flow;
using Lizzo.PV.Gameplay.Run;
using Lizzo.PV.Gameplay.Telemetry;
using Lizzo.PV.Legion.Combat;
using NUnit.Framework;
using UnityEngine;

namespace Lizzo.PV.EditorTests
{
    public sealed class RunCombatTelemetryTests
    {
        [Test]
        public void LogOnceEvents_AreWrittenAgainWhenANewRunBegins()
        {
            string firstPath = null;
            string secondPath = null;
            try
            {
                RunTelemetry.BeginRun();
                RunTelemetry.LogOnce(RunTelemetry.FirstRecruit, "unit_id=sword_soldier");
                RunTelemetry.FlushRunLog("first_run");
                firstPath = RunTelemetry.CurrentRunLogPath;

                RunTelemetry.BeginRun();
                RunTelemetry.LogOnce(RunTelemetry.FirstRecruit, "unit_id=shield_guard");
                RunTelemetry.FlushRunLog("second_run");
                secondPath = RunTelemetry.CurrentRunLogPath;

                Assert.That(CountOccurrences(File.ReadAllText(firstPath), " | first_recruit | "), Is.EqualTo(1));
                Assert.That(CountOccurrences(File.ReadAllText(secondPath), " | first_recruit | "), Is.EqualTo(1));
            }
            finally
            {
                if (!string.IsNullOrEmpty(firstPath) && File.Exists(firstPath))
                    File.Delete(firstPath);
                if (!string.IsNullOrEmpty(secondPath) && File.Exists(secondPath))
                    File.Delete(secondPath);
            }
        }

        [Test]
        public void BossOutcomeTelemetry_HasOneDamageOwnerAndRecordsBossDefeat()
        {
            string aoeSource = File.ReadAllText(
                "Assets/_LizzoPV/Gameplay/Enemies/Runtime/HungryGiantBehaviour.Aoe.cs");
            string damageSource = File.ReadAllText(
                "Assets/_LizzoPV/Gameplay/Commander/Runtime/CommanderDamageReceiver.cs");
            string deathSource = File.ReadAllText(
                "Assets/_LizzoPV/Gameplay/Enemies/Runtime/Actors/MonsterController_Feedback.cs");

            StringAssert.DoesNotContain("RunTelemetry.BossPatternHit", aoeSource);
            StringAssert.Contains("RunTelemetry.BossPatternHit", damageSource);
            StringAssert.Contains("RunTelemetry.FirstBossKill", deathSource);
        }

        [Test]
        public void CurrentRunTelemetry_RecordsAttackSynergyAndSummaries_WithoutLegacyOutputPath()
        {
            CanonicalCompanionCastStream casts = new CanonicalCompanionCastStream();
            RunCombatTelemetry combat = new RunCombatTelemetry(casts);
            GameObject targetObject = new GameObject("RunCombatTelemetryTarget");
            MonsterController target = targetObject.AddComponent<MonsterController>();
            string path = null;
            try
            {
                RunTelemetry.BeginRun(RunMode.Tutorial, "test", "test", string.Empty);
                CanonicalCompanionCastIdentity identity =
                    new CanonicalCompanionCastIdentity(11, "A1", "fire_mage", "magic");
                casts.TryEmit(identity, CanonicalCompanionActionKind.BasicAttack);
                casts.TryEmit(identity, CanonicalCompanionActionKind.ActiveSkill);
                casts.TryEmit(identity, CanonicalCompanionActionKind.ReturningAttackResolved);
                combat.RecordHit(target, "fire_mage", CombatKillSourceCategory.CompanionOwnedAction, 7, false);
                combat.RecordHit(target, "test_pair", CombatKillSourceCategory.SynergyAction, 9, true);
                combat.RecordHit(target, "commander", CombatKillSourceCategory.Commander, 5, false);
                combat.RecordSynergyExecutionStarted("test_pair", SynergyTier.Pair, 1L, "fire_mage", 2);
                combat.RecordSynergyExecutionCompleted("test_pair", SynergyTier.Pair, 1L, 2);
                combat.LogSummary("test_complete");
                RunTelemetry.Log(RunTelemetry.RunEnd, "result=clear", "boss_hp_percent=0");
                RunTelemetry.FlushRunLog("test_complete");

                path = RunTelemetry.CurrentRunLogPath;
                Assert.That(path, Does.Contain(Path.Combine("_Temp", "TelemetryRuns")));
                Assert.That(Path.GetFileName(path), Does.StartWith("run-"));
                string text = File.ReadAllText(path);
                Assert.That(text, Does.Contain("# Run Telemetry Log"));
                Assert.That(CountOccurrences(text, " | run_start | "), Is.EqualTo(1));
                Assert.That(text, Does.Contain("stage=stage1, run_mode=tutorial, build_version="));
                Assert.That(text, Does.Contain("companion_attack_completed"));
                Assert.That(text, Does.Contain("source_id=fire_mage"));
                Assert.That(text, Does.Contain("attacks=1, skills=1, resolutions=1, hits=1, damage=7, kills=0"));
                Assert.That(text, Does.Contain("synergy_hit_applied"));
                Assert.That(text, Does.Contain("synergy_execution_started"));
                Assert.That(text, Does.Contain("synergy_execution_completed"));
                Assert.That(text, Does.Contain("companion_combat_summary"));
                Assert.That(text, Does.Contain("synergy_combat_summary"));
                Assert.That(text, Does.Contain("activated=0, started=1, completed=1, hits=1, damage=9, kills=1"));
                Assert.That(text, Does.Not.Contain("source_id=commander"));
            }
            finally
            {
                combat.Dispose();
                casts.Dispose();
                Object.DestroyImmediate(targetObject);
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                    File.Delete(path);
            }
        }

        private static int CountOccurrences(string text, string value)
        {
            int count = 0;
            int startIndex = 0;
            while ((startIndex = text.IndexOf(value, startIndex, System.StringComparison.Ordinal)) >= 0)
            {
                count++;
                startIndex += value.Length;
            }
            return count;
        }
    }
}
