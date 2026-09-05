using Lizzo.PV.Gameplay.Run;
using NUnit.Framework;

namespace Lizzo.PV.EditorTests
{
    public sealed class BossOutcomeTests
    {
        [Test]
        public void BossSpawnDoesNotStopEncounterSpawns()
        {
            BossOutcomeRuntime runtime = new BossOutcomeRuntime();
            Assert.That(runtime.RegisterBoss(100), Is.True);
            Assert.That(runtime.CreateSnapshot().ShouldStopNewSpawns, Is.False);
        }

        [Test]
        public void ActualRegisteredBossDeathWinsButSystemRemovalDoesNot()
        {
            BossOutcomeRuntime runtime = new BossOutcomeRuntime();
            runtime.RegisterBoss(100);
            Assert.That(runtime.ResolveSystemRemoval(100), Is.False);
            Assert.That(runtime.CreateSnapshot().Outcome, Is.EqualTo(CombatRunOutcome.InProgress));
            Assert.That(runtime.ResolveCombatDeath(100, CombatEntityKind.BossEnemy), Is.True);
            Assert.That(runtime.CreateSnapshot().Outcome, Is.EqualTo(CombatRunOutcome.Victory));
        }

        [Test]
        public void CommanderDeathDefeatsAndTerminalOutcomeIsImmutable()
        {
            BossOutcomeRuntime runtime = new BossOutcomeRuntime();
            runtime.RegisterBoss(100);
            Assert.That(runtime.ResolveCombatDeath(1, CombatEntityKind.Commander), Is.True);
            Assert.That(runtime.ResolveCombatDeath(100, CombatEntityKind.BossEnemy), Is.False);
            Assert.That(runtime.CreateSnapshot().Outcome, Is.EqualTo(CombatRunOutcome.Defeat));
        }

        [Test]
        public void TerminalCleanupNeverGrantsExperience()
        {
            BossOutcomeRuntime runtime = new BossOutcomeRuntime();
            runtime.RegisterBoss(100);
            runtime.ResolveCombatDeath(100, CombatEntityKind.BossEnemy);
            BossOutcomeSnapshot snapshot = runtime.CreateSnapshot();
            Assert.That(snapshot.ShouldRemoveRemainingEnemies, Is.True);
            Assert.That(snapshot.RemovedEnemiesGrantExperience, Is.False);
        }
    }
}
