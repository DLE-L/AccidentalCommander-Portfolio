using Lizzo.PV.Gameplay.Run;
using NUnit.Framework;

namespace Lizzo.PV.EditorTests
{
    public sealed class SwordVerticalTests
    {
        [Test]
        public void InitialCardRecruitsSwordAndUnblocksTheRun()
        {
            using RunRuntimeHost host = BuildHost();

            host.Start();
            host.Submit(RunCommand.ChooseSwordGrowthCard());
            host.Advance(0.0f);

            SwordVerticalSnapshot sword = host.CurrentSnapshot.SwordVertical;
            Assert.That(sword.Progression, Is.EqualTo(1));
            Assert.That(sword.HasBaseMember, Is.True);
            Assert.That(sword.HasPromotedMember, Is.False);
            Assert.That(host.CurrentSnapshot.ActiveBlockers, Is.EqualTo(SimulationBlocker.None));
        }

        [Test]
        public void MeleeLocksEnemyPositionThenReturnsToLatestSlotBeforeReacquiring()
        {
            using RunRuntimeHost host = BuildStartedSwordHost();
            host.Submit(RunCommand.SpawnVerticalEnemy(10, new RunPoint(3.0f, 0.0f), 20, 7, 10));
            host.Advance(0.0f);

            Assert.That(host.CurrentSnapshot.SwordVertical.Phase, Is.EqualTo(SwordActionPhase.Approaching));
            Assert.That(host.CurrentSnapshot.SwordVertical.LockedTargetId, Is.EqualTo(10));
            Assert.That(host.CurrentSnapshot.SwordVertical.LockedTargetPoint, Is.EqualTo(new RunPoint(3.0f, 0.0f)));

            host.Submit(RunCommand.MoveVerticalEnemy(10, new RunPoint(8.0f, 0.0f)));
            host.Submit(RunCommand.SetSwordSlot(new RunPoint(-2.0f, 0.0f)));
            host.Submit(RunCommand.SwordReachedActionPoint());
            host.Advance(0.0f);

            Assert.That(host.CurrentSnapshot.SwordVertical.Phase, Is.EqualTo(SwordActionPhase.Returning));
            Assert.That(host.CurrentSnapshot.SwordVertical.GetEnemyHealth(10), Is.EqualTo(20));
            Assert.That(host.CurrentSnapshot.SwordVertical.LastSlashPoint, Is.EqualTo(new RunPoint(3.0f, 0.0f)));

            host.Submit(RunCommand.SwordReachedSlot());
            host.Advance(0.0f);

            Assert.That(host.CurrentSnapshot.SwordVertical.Phase, Is.EqualTo(SwordActionPhase.Idle));
            Assert.That(host.CurrentSnapshot.SwordVertical.MemberPosition, Is.EqualTo(new RunPoint(-2.0f, 0.0f)));
            Assert.That(host.CurrentSnapshot.SwordVertical.CompletedBaseActions, Is.EqualTo(1));
        }

        [Test]
        public void EnemyContactDamagesCommanderAndCanCommitDefeatOnce()
        {
            using RunRuntimeHost host = BuildStartedSwordHost();
            host.Submit(RunCommand.SpawnVerticalEnemy(20, RunPoint.Zero, 1000, 45, 10));
            host.Submit(RunCommand.VerticalEnemyContact(20));
            host.Advance(0.0f);

            Assert.That(host.CurrentSnapshot.SwordVertical.CommanderHealth, Is.EqualTo(55));
            Assert.That(host.CurrentSnapshot.Outcome, Is.EqualTo(RunSessionOutcome.None));

            host.Submit(RunCommand.VerticalEnemyContact(20));
            host.Advance(0.0f);
            Assert.That(host.CurrentSnapshot.SwordVertical.CommanderHealth, Is.EqualTo(55));

            host.Advance(0.50f);
            host.Submit(RunCommand.VerticalEnemyContact(20));
            host.Advance(0.0f);
            Assert.That(host.CurrentSnapshot.SwordVertical.CommanderHealth, Is.EqualTo(10));

            host.Advance(0.50f);
            host.Submit(RunCommand.VerticalEnemyContact(20));
            host.Advance(0.0f);

            Assert.That(host.CurrentSnapshot.SwordVertical.CommanderHealth, Is.Zero);
            Assert.That(host.CurrentSnapshot.Outcome, Is.EqualTo(RunSessionOutcome.Defeat));
            Assert.That(host.CurrentSnapshot.ResultCommitCount, Is.EqualTo(1));

            host.Submit(RunCommand.VerticalEnemyContact(20));
            host.Advance(1.0f);
            Assert.That(host.CurrentSnapshot.ResultCommitCount, Is.EqualTo(1));
        }

        [Test]
        public void KillCreatesImmediateFlightThenAbsorbsExperienceAndOffersGrowth()
        {
            using RunRuntimeHost host = BuildStartedSwordHost();

            KillEnemyAndStartReturn(host, 30);
            Assert.That(host.CurrentSnapshot.SwordVertical.PendingExperienceFlights, Is.EqualTo(1));
            Assert.That(host.CurrentSnapshot.SwordVertical.AbsorbedExperience, Is.Zero);

            host.Advance(0.35f);

            Assert.That(host.CurrentSnapshot.SwordVertical.PendingExperienceFlights, Is.Zero);
            Assert.That(host.CurrentSnapshot.SwordVertical.AbsorbedExperience, Is.EqualTo(10));
            Assert.That(
                (host.CurrentSnapshot.ActiveBlockers & SimulationBlocker.GrowthSelection) != 0,
                Is.True);

            host.Submit(RunCommand.ChooseSwordGrowthCard());
            host.Advance(0.0f);
            Assert.That(host.CurrentSnapshot.SwordVertical.Progression, Is.EqualTo(2));
            Assert.That(host.CurrentSnapshot.ActiveBlockers, Is.EqualTo(SimulationBlocker.None));
        }

        [Test]
        public void PromotedCrescentCountsOnlyPostPromotionActionsAndStartsAfterReturn()
        {
            using RunRuntimeHost host = BuildStartedSwordHost();

            KillEnemyAbsorbAndGrow(host, 40);
            KillEnemyAbsorbAndGrow(host, 41);
            Assert.That(host.CurrentSnapshot.SwordVertical.Progression, Is.EqualTo(3));
            Assert.That(host.CurrentSnapshot.SwordVertical.HasPromotedMember, Is.True);
            Assert.That(host.CurrentSnapshot.SwordVertical.PromotionActionProgress, Is.Zero);

            CompleteSurvivingAttack(host, 50);
            CompleteSurvivingAttack(host, 51);

            host.Submit(RunCommand.SpawnVerticalEnemy(52, new RunPoint(1.0f, 0.0f), 100, 1, 0));
            host.Advance(0.02f);
            host.Submit(RunCommand.SwordReachedActionPoint());
            host.Advance(0.0f);
            Assert.That(host.CurrentSnapshot.SwordVertical.Phase, Is.EqualTo(SwordActionPhase.Returning));
            Assert.That(host.CurrentSnapshot.SwordVertical.PendingCrescentCount, Is.EqualTo(1));
            Assert.That(host.CurrentSnapshot.SwordVertical.CrescentCastCount, Is.Zero);

            host.Submit(RunCommand.SwordReachedSlot());
            host.Advance(0.0f);

            Assert.That(host.CurrentSnapshot.SwordVertical.Phase, Is.EqualTo(SwordActionPhase.Idle));
            Assert.That(host.CurrentSnapshot.SwordVertical.PendingCrescentCount, Is.Zero);
            Assert.That(host.CurrentSnapshot.SwordVertical.CrescentCastCount, Is.EqualTo(1));
        }

        [Test]
        public void StateDigestSeparatesDifferentCommanderEnemyAndFlightState()
        {
            using RunRuntimeHost first = BuildStartedSwordHost();
            using RunRuntimeHost second = BuildStartedSwordHost();

            first.Submit(RunCommand.MoveCommander(new RunPoint(0.5f, 0.0f)));
            first.Submit(RunCommand.SpawnVerticalEnemy(70, new RunPoint(1.0f, 0.0f), 10, 1, 10));
            second.Submit(RunCommand.SpawnVerticalEnemy(70, new RunPoint(2.0f, 0.0f), 10, 1, 10));
            first.Advance(0.0f);
            second.Advance(0.0f);

            Assert.That(first.CurrentSnapshot.StateDigest, Is.Not.EqualTo(second.CurrentSnapshot.StateDigest));

            first.Submit(RunCommand.SwordReachedActionPoint());
            first.Advance(0.0f);
            Assert.That(first.CurrentSnapshot.SwordVertical.PendingExperienceFlights, Is.EqualTo(1));
            Assert.That(first.CurrentSnapshot.StateDigest, Is.Not.EqualTo(second.CurrentSnapshot.StateDigest));
        }

        [Test]
        public void AttackPeriodStartsWithActionAndDoesNotAddAnotherPeriodAfterReturn()
        {
            using RunRuntimeHost host = BuildStartedSwordHost();
            host.Submit(RunCommand.SpawnVerticalEnemy(80, new RunPoint(1.0f, 0.0f), 1000, 1, 0));
            host.Advance(0.0f);
            Assert.That(host.CurrentSnapshot.SwordVertical.Phase, Is.EqualTo(SwordActionPhase.Approaching));

            host.Advance(0.02f);
            host.Submit(RunCommand.SwordReachedActionPoint());
            host.Advance(0.0f);
            host.Submit(RunCommand.SwordReachedSlot());
            host.Advance(0.0f);
            Assert.That(host.CurrentSnapshot.SwordVertical.Phase, Is.EqualTo(SwordActionPhase.Idle));

            host.Advance(0.0f);
            Assert.That(host.CurrentSnapshot.SwordVertical.Phase, Is.EqualTo(SwordActionPhase.Approaching));
        }

        private static RunRuntimeHost BuildHost()
        {
            SwordVerticalDefinition vertical = new SwordVerticalDefinition(
                commanderHealth: 100,
                commanderPosition: RunPoint.Zero,
                initialSwordSlot: new RunPoint(-1.0f, 0.0f),
                commanderTargetRange: 5.0f,
                meleeMoveSpeed: 20.0f,
                slashRadius: 0.6f,
                slashDamage: 10,
                attackPeriod: 0.01f,
                contactPeriod: 0.50f,
                experiencePerLevel: 10,
                experienceFlightSeconds: 0.30f,
                promotedTriggerCount: 3,
                crescentDamage: 5);
            return RunCompositionRoot.Build(new RunDefinitionSnapshot(1847, vertical));
        }

        private static RunRuntimeHost BuildStartedSwordHost()
        {
            RunRuntimeHost host = BuildHost();
            host.Start();
            host.Submit(RunCommand.ChooseSwordGrowthCard());
            host.Advance(0.0f);
            return host;
        }

        private static void KillEnemyAndStartReturn(RunRuntimeHost host, int enemyId)
        {
            host.Submit(RunCommand.SpawnVerticalEnemy(enemyId, new RunPoint(1.0f, 0.0f), 10, 1, 10));
            host.Advance(0.02f);
            host.Submit(RunCommand.SwordReachedActionPoint());
            host.Advance(0.0f);
            Assert.That(host.CurrentSnapshot.SwordVertical.GetEnemyHealth(enemyId), Is.Zero);
        }

        private static void KillEnemyAbsorbAndGrow(RunRuntimeHost host, int enemyId)
        {
            KillEnemyAndStartReturn(host, enemyId);
            host.Submit(RunCommand.SwordReachedSlot());
            host.Advance(0.0f);
            host.Advance(0.35f);
            host.Submit(RunCommand.ChooseSwordGrowthCard());
            host.Advance(0.0f);
        }

        private static void CompleteSurvivingAttack(RunRuntimeHost host, int enemyId)
        {
            host.Submit(RunCommand.SpawnVerticalEnemy(enemyId, new RunPoint(1.0f, 0.0f), 100, 1, 0));
            host.Advance(0.02f);
            host.Submit(RunCommand.SwordReachedActionPoint());
            host.Advance(0.0f);
            host.Submit(RunCommand.SwordReachedSlot());
            host.Advance(0.0f);
        }
    }
}
