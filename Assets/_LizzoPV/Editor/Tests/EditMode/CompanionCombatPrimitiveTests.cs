using Lizzo.PV.Legion;
using NUnit.Framework;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class CompanionCombatPrimitiveTests
    {
        [Test]
        public void EnemyStatusState_RefreshesWithoutStackingAndConsumesWeakeningOnce()
        {
            CompanionEnemyStatusState state = new CompanionEnemyStatusState();
            CompanionStatusSource herbalist = new CompanionStatusSource("field_herbalist", 10);
            CompanionStatusSource lightningMage = new CompanionStatusSource("lightning_mage", 20);
            CompanionStatusSource wraithKnight = new CompanionStatusSource("wraith_knight", 30);

            Assert.That(state.ApplyVulnerable(herbalist, 1.20f, 2.0f, 1.0f), Is.True);
            Assert.That(state.ApplyVulnerable(herbalist, 1.30f, 2.0f, 2.0f), Is.True);
            Assert.That(state.ResolveIncomingDamageMultiplier(3.9f), Is.EqualTo(1.30f));
            Assert.That(state.ResolveIncomingDamageMultiplier(4.0f), Is.EqualTo(1.0f));

            Assert.That(state.ApplyShock(lightningMage, 0.75f, 3.0f, 1.0f), Is.True);
            Assert.That(state.ResolveMovementSpeedMultiplier(3.9f), Is.EqualTo(0.75f));
            Assert.That(state.TryConsumeShock(3.9f, out CompanionStatusSource shockSource), Is.True);
            Assert.That(shockSource.OwnerInstanceId, Is.EqualTo(20));
            Assert.That(state.ResolveMovementSpeedMultiplier(3.9f), Is.EqualTo(1.0f));

            Assert.That(state.ApplyWeakening(wraithKnight, 0.60f, 4.0f, 1.0f), Is.True);
            Assert.That(state.TryConsumeWeakeningMultiplier(2.0f, out float multiplier), Is.True);
            Assert.That(multiplier, Is.EqualTo(0.60f));
            Assert.That(state.TryConsumeWeakeningMultiplier(2.0f, out multiplier), Is.False);
            Assert.That(multiplier, Is.EqualTo(1.0f));
        }

        [Test]
        public void EnemyStatusState_CapturesVulnerableAndCurseDeathExactlyOnce()
        {
            CompanionEnemyStatusState state = new CompanionEnemyStatusState();
            CompanionStatusSource herbalist = new CompanionStatusSource("field_herbalist", 10);
            CompanionStatusSource necromancer = new CompanionStatusSource("necromancer", 40);

            Assert.That(state.ApplyVulnerable(herbalist, 1.20f, 3.0f, 1.0f), Is.True);
            Assert.That(state.ApplyCurse(necromancer, 3.0f, 1.0f), Is.True);
            Assert.That(state.TryCaptureDeath(2.0f, out CompanionEnemyDeathStatusSnapshot snapshot), Is.True);
            Assert.That(snapshot.WasVulnerable, Is.True);
            Assert.That(snapshot.VulnerableSource.OwnerInstanceId, Is.EqualTo(10));
            Assert.That(snapshot.WasCursed, Is.True);
            Assert.That(snapshot.CurseSource.OwnerInstanceId, Is.EqualTo(40));
            Assert.That(state.TryCaptureDeath(2.0f, out _), Is.False);
            Assert.That(state.ApplyCurse(necromancer, 3.0f, 2.0f), Is.False);

            state.Reset();
            Assert.That(state.ApplyCurse(necromancer, 1.0f, 5.0f), Is.True);
            Assert.That(state.TryCaptureDeath(6.0f, out snapshot), Is.True);
            Assert.That(snapshot.WasCursed, Is.False);
        }

        [Test]
        public void LineageCounter_FiltersEventKindAndRetainsOverflow()
        {
            CompanionLineageTriggerCounter counter = new CompanionLineageTriggerCounter();
            counter.Configure(CompanionLineageEventKind.Hit, 3);

            Assert.That(counter.Record(CompanionLineageEventKind.Action, 2), Is.EqualTo(0));
            Assert.That(counter.Record(CompanionLineageEventKind.Hit, 2), Is.EqualTo(0));
            Assert.That(counter.Record(CompanionLineageEventKind.Hit, 5), Is.EqualTo(2));
            Assert.That(counter.CurrentCount, Is.EqualTo(1));
            Assert.That(counter.TotalTriggerCount, Is.EqualTo(2));

            counter.Reset();
            Assert.That(counter.CurrentCount, Is.EqualTo(0));
            Assert.That(counter.TotalTriggerCount, Is.EqualTo(0));
        }

        [Test]
        public void SuccessfulActionCounter_PreservesExistingContractThroughSharedCounter()
        {
            SuccessfulActionCounter counter = new SuccessfulActionCounter();
            counter.Configure(2);

            Assert.That(counter.RecordSuccess(), Is.False);
            Assert.That(counter.RecordSuccess(), Is.True);
            Assert.That(counter.RecordSuccess(), Is.False);
            Assert.That(counter.CurrentCount, Is.EqualTo(1));
            Assert.That(counter.TriggerCount, Is.EqualTo(2));
        }

        [Test]
        public void ReturningAttackLedger_AllowsEachTargetOncePerPassAndResetsPerCast()
        {
            ReturningAttackHitLedger ledger = new ReturningAttackHitLedger();

            Assert.That(ledger.TryRecord(100, ReturningAttackPass.Outbound), Is.True);
            Assert.That(ledger.TryRecord(100, ReturningAttackPass.Outbound), Is.False);
            Assert.That(ledger.TryRecord(100, ReturningAttackPass.Return), Is.True);
            Assert.That(ledger.TryRecord(100, ReturningAttackPass.Return), Is.False);
            Assert.That(ledger.TryRecord(200, ReturningAttackPass.Outbound), Is.True);
            Assert.That(ledger.OutboundHitCount, Is.EqualTo(2));
            Assert.That(ledger.ReturnHitCount, Is.EqualTo(1));

            ledger.Reset();
            Assert.That(ledger.TryRecord(100, ReturningAttackPass.Outbound), Is.True);
            Assert.That(ledger.OutboundHitCount, Is.EqualTo(1));
            Assert.That(ledger.ReturnHitCount, Is.EqualTo(0));
        }
    }
}
