using Lizzo.PV.Gameplay.Run.M2;
using NUnit.Framework;

namespace Lizzo.PV.EditorTests
{
    public sealed class M2CombatEffectsTests
    {
        [Test]
        public void DamagePipelineRoundsOnceAfterAllMultipliersAndKeepsPositiveMinimum()
        {
            CombatResolver resolver = new CombatResolver();
            resolver.Register(new CombatEntityDefinition(1, CombatEntityKind.NormalEnemy, 100, RunPoint.Zero));
            resolver.Register(new CombatEntityDefinition(
                2,
                CombatEntityKind.Commander,
                100,
                RunPoint.Zero,
                receivedDamageMultiplier: 0.80f,
                commanderDamageReduction: 0.25f));
            resolver.ApplyStatus(new StatusRequest(1, 2, CombatStatusKind.Vulnerable, 1.50f, 5.0f));

            CombatResolution result = resolver.ApplyDamage(new DamageRequest(
                1,
                2,
                11.0f,
                CombatDamageKind.Contact,
                attackerDamageMultiplier: 1.20f,
                receivedDamageMultiplier: 1.10f));

            Assert.That(result.AppliedAmount, Is.EqualTo(13));
            Assert.That(resolver.CreateSnapshot().GetEntity(2).Health, Is.EqualTo(87));

            CombatResolution minimum = resolver.ApplyDamage(new DamageRequest(
                1,
                2,
                0.01f,
                CombatDamageKind.Periodic,
                attackerDamageMultiplier: 0.01f,
                receivedDamageMultiplier: 0.01f));
            Assert.That(minimum.AppliedAmount, Is.EqualTo(1));
        }

        [Test]
        public void DirectGuardBlocksOneEligibleHitButPeriodicDamageDoesNotConsumeIt()
        {
            CombatResolver resolver = BuildEnemyAndCommander(commanderGuardCharges: 1);

            CombatResolution periodic = resolver.ApplyDamage(new DamageRequest(
                1,
                2,
                5.0f,
                CombatDamageKind.Periodic));
            CombatResolution guarded = resolver.ApplyDamage(new DamageRequest(
                1,
                2,
                20.0f,
                CombatDamageKind.Projectile));
            CombatResolution next = resolver.ApplyDamage(new DamageRequest(
                1,
                2,
                20.0f,
                CombatDamageKind.Projectile));

            Assert.That(periodic.AppliedAmount, Is.EqualTo(5));
            Assert.That(guarded.WasGuarded, Is.True);
            Assert.That(guarded.AppliedAmount, Is.Zero);
            Assert.That(next.AppliedAmount, Is.EqualTo(20));
            Assert.That(resolver.CreateSnapshot().GetEntity(2).Health, Is.EqualTo(75));
        }

        [Test]
        public void WeakenReducesAndConsumesOnlyTheNextActualCommanderHit()
        {
            CombatResolver resolver = BuildEnemyAndCommander();
            resolver.ApplyStatus(new StatusRequest(2, 1, CombatStatusKind.Weakened, 0.40f, 5.0f, charges: 1));

            CombatResolution invalid = resolver.ApplyDamage(new DamageRequest(
                1,
                99,
                10.0f,
                CombatDamageKind.Direct));
            CombatResolution first = resolver.ApplyDamage(new DamageRequest(
                1,
                2,
                10.0f,
                CombatDamageKind.Direct));
            CombatResolution second = resolver.ApplyDamage(new DamageRequest(
                1,
                2,
                10.0f,
                CombatDamageKind.Direct));

            Assert.That(invalid.AppliedAmount, Is.Zero);
            Assert.That(first.AppliedAmount, Is.EqualTo(6));
            Assert.That(second.AppliedAmount, Is.EqualTo(10));
            Assert.That(resolver.CreateSnapshot().GetEntity(1).GetStatusCount(CombatStatusKind.Weakened), Is.Zero);
        }

        [Test]
        public void StatusReapplicationRefreshesOneSourceEntryWithoutStackingMagnitude()
        {
            CombatResolver resolver = BuildEnemyAndCommander();
            resolver.ApplyStatus(new StatusRequest(2, 1, CombatStatusKind.Curse, 1.20f, 2.0f));
            resolver.ApplyStatus(new StatusRequest(2, 1, CombatStatusKind.Curse, 1.20f, 6.0f));

            CombatEntitySnapshot enemy = resolver.CreateSnapshot().GetEntity(1);
            Assert.That(enemy.GetStatusCount(CombatStatusKind.Curse), Is.EqualTo(1));
            Assert.That(enemy.GetStatusRemaining(CombatStatusKind.Curse, 2), Is.EqualTo(6.0f));

            resolver.Advance(5.0f);
            Assert.That(
                resolver.CreateSnapshot().GetEntity(1).GetStatusRemaining(CombatStatusKind.Curse, 2),
                Is.EqualTo(1.0f).Within(0.001f));
        }

        [Test]
        public void HealingCapsAtMaximumAndNeverRevivesADeadEntity()
        {
            CombatResolver resolver = BuildEnemyAndCommander();
            resolver.ApplyDamage(new DamageRequest(1, 2, 30.0f, CombatDamageKind.Direct));
            CombatResolution healed = resolver.ApplyHealing(new HealingRequest(2, 2, 50.0f, 1.20f));
            resolver.ApplyDamage(new DamageRequest(1, 2, 500.0f, CombatDamageKind.Direct));
            CombatResolution deadHeal = resolver.ApplyHealing(new HealingRequest(2, 2, 100.0f, 1.0f));

            Assert.That(healed.AppliedAmount, Is.EqualTo(30));
            Assert.That(deadHeal.AppliedAmount, Is.Zero);
            Assert.That(resolver.CreateSnapshot().GetEntity(2).IsDead, Is.True);
        }

        [Test]
        public void LethalDamageCommitsDeathContextAndReactionsExactlyOnce()
        {
            CombatResolver resolver = BuildEnemyAndCommander();

            CombatResolution lethal = resolver.ApplyDamage(new DamageRequest(
                2,
                1,
                150.0f,
                CombatDamageKind.Area,
                attackId: "sword-slash"));
            CombatResolution duplicate = resolver.ApplyDamage(new DamageRequest(
                2,
                1,
                150.0f,
                CombatDamageKind.Area,
                attackId: "second-hit"));
            CombatEffectsSnapshot snapshot = resolver.CreateSnapshot();

            Assert.That(lethal.DidKill, Is.True);
            Assert.That(duplicate.AppliedAmount, Is.Zero);
            Assert.That(snapshot.DeathCount, Is.EqualTo(1));
            Assert.That(snapshot.LastDeath.EntityId, Is.EqualTo(1));
            Assert.That(snapshot.LastDeath.KillerEntityId, Is.EqualTo(2));
            Assert.That(snapshot.LastDeath.AttackId, Is.EqualTo("sword-slash"));
            Assert.That(snapshot.GetEntity(1).DeathReactionCount, Is.EqualTo(1));
        }

        [Test]
        public void ForcedMovementSelectsPriorityThenStrengthAndLocksUntilExplicitEnd()
        {
            CombatResolver resolver = BuildEnemyAndCommander();
            resolver.BeginAction(1, pendingUnspawnedAttackCount: 2);
            ForcedMovementResolution applied = resolver.ResolveForcedMovement(new[]
            {
                new ForcedMovementRequest(7, 1, ForcedMovementKind.Pull, new RunPoint(-1.0f, 0.0f), 2.0f, 1, 8.0f, 1.0f),
                new ForcedMovementRequest(8, 1, ForcedMovementKind.Push, new RunPoint(1.0f, 0.0f), 3.0f, 2, 5.0f, 1.0f),
                new ForcedMovementRequest(9, 1, ForcedMovementKind.Push, new RunPoint(1.0f, 0.0f), 4.0f, 2, 9.0f, 1.0f),
            });
            ForcedMovementResolution locked = resolver.ResolveForcedMovement(new[]
            {
                new ForcedMovementRequest(10, 1, ForcedMovementKind.Pull, new RunPoint(-1.0f, 0.0f), 5.0f, 99, 99.0f, 1.0f),
            });

            CombatEntitySnapshot moving = resolver.CreateSnapshot().GetEntity(1);
            Assert.That(applied.Outcome, Is.EqualTo(ForcedMovementOutcome.Applied));
            Assert.That(applied.SourceEntityId, Is.EqualTo(9));
            Assert.That(applied.Destination.X, Is.EqualTo(4.0f).Within(0.001f));
            Assert.That(locked.Outcome, Is.EqualTo(ForcedMovementOutcome.Locked));
            Assert.That(moving.IsForcedMovementLocked, Is.True);
            Assert.That(moving.IsActionActive, Is.False);
            Assert.That(moving.PendingUnspawnedAttackCount, Is.Zero);
            Assert.That(moving.ActionCancelCount, Is.EqualTo(1));

            Assert.That(
                resolver.EndForcedMovement(1, ForcedMovementEndReason.Collision, new RunPoint(2.5f, 0.0f)),
                Is.True);
            Assert.That(resolver.CreateSnapshot().GetEntity(1).IsForcedMovementLocked, Is.False);
        }

        [Test]
        public void BossRejectsForcedMovementAsImmuneWithoutStartingOrCancellingAction()
        {
            CombatResolver resolver = new CombatResolver();
            resolver.Register(new CombatEntityDefinition(3, CombatEntityKind.BossEnemy, 500, new RunPoint(1.0f, 1.0f)));
            resolver.BeginAction(3, pendingUnspawnedAttackCount: 1);

            ForcedMovementResolution result = resolver.ResolveForcedMovement(new[]
            {
                new ForcedMovementRequest(2, 3, ForcedMovementKind.Push, new RunPoint(1.0f, 0.0f), 5.0f, 1, 5.0f, 1.0f),
            });
            CombatEffectsSnapshot snapshot = resolver.CreateSnapshot();

            Assert.That(result.Outcome, Is.EqualTo(ForcedMovementOutcome.Immune));
            Assert.That(result.FollowUpPoint, Is.EqualTo(new RunPoint(1.0f, 1.0f)));
            Assert.That(snapshot.ForcedMovementStartCount, Is.Zero);
            Assert.That(snapshot.GetEntity(3).IsActionActive, Is.True);
            Assert.That(snapshot.GetEntity(3).ActionCancelCount, Is.Zero);
        }

        [Test]
        public void DeathEndsActiveForcedMovementLock()
        {
            CombatResolver resolver = BuildEnemyAndCommander();
            resolver.ResolveForcedMovement(new[]
            {
                new ForcedMovementRequest(2, 1, ForcedMovementKind.Push, new RunPoint(1.0f, 0.0f), 2.0f, 1, 1.0f, 1.0f),
            });

            resolver.ApplyDamage(new DamageRequest(2, 1, 500.0f, CombatDamageKind.Direct));

            CombatEntitySnapshot enemy = resolver.CreateSnapshot().GetEntity(1);
            Assert.That(enemy.IsDead, Is.True);
            Assert.That(enemy.IsForcedMovementLocked, Is.False);
            Assert.That(enemy.LastForcedMovementEndReason, Is.EqualTo(ForcedMovementEndReason.Death));
        }

        private static CombatResolver BuildEnemyAndCommander(int commanderGuardCharges = 0)
        {
            CombatResolver resolver = new CombatResolver();
            resolver.Register(new CombatEntityDefinition(1, CombatEntityKind.NormalEnemy, 100, RunPoint.Zero));
            resolver.Register(new CombatEntityDefinition(
                2,
                CombatEntityKind.Commander,
                100,
                RunPoint.Zero,
                directGuardCharges: commanderGuardCharges));
            return resolver;
        }
    }
}
