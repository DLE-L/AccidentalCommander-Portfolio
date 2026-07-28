using Lizzo.PV.Legion;
using NUnit.Framework;
using UnityEngine;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class BeastCommanderPromotionWolfTests
    {
        [Test]
        public void PromotedWolf_UsesTwoSeventyPercentHitsAfterOneGrowthApplication()
        {
            CompanionWolfOwnedProxyCombatSetup baseSetup = new CompanionWolfOwnedProxyCombatSetup(
                "wolf_tamer", 10, 4.0f, 4.0f, 0.8f, 1, 1, 0.15f);

            CompanionWolfOwnedProxyCombatSetup promoted = baseSetup
                .WithPromotedBeastCommanderHits()
                .WithGrowthScale(new CompanionGrowthScale(1.65f, 2.15f, 1.10f, 3));

            Assert.AreEqual(1, baseSetup.HitCount);
            Assert.AreEqual(1.0f, baseSetup.PerHitDamageRatio);
            Assert.AreEqual(2, promoted.HitCount);
            Assert.AreEqual(0.70f, promoted.PerHitDamageRatio);
            Assert.AreEqual(Mathf.RoundToInt(10 * 1.65f), promoted.Damage);
            Assert.AreEqual(Mathf.RoundToInt(promoted.Damage * 0.70f), promoted.ResolvePerHitDamage());
            Assert.That(promoted.Period, Is.EqualTo(4.4f).Within(0.0001f));
        }

        [Test]
        public void PromotedWolf_LocksOneTargetForTwoHitsAndRejectsSecondActiveWolf()
        {
            WolfOwnedProxyState state = new WolfOwnedProxyState();
            Vector3 owner = Vector3.zero;
            Vector3 target = new Vector3(4.0f, 0.0f);

            Assert.IsTrue(state.TryBegin(owner, target, 42, 0.0f, 0.8f, 2));
            Assert.IsFalse(state.TryBegin(owner, target, 43, 0.0f, 0.8f, 2));
            Assert.IsTrue(state.Advance(0.4f, out Vector3 impactPosition));
            Assert.AreEqual(target, impactPosition);
            Assert.AreEqual(WolfOwnedProxyPhase.Impact, state.Phase);
            Assert.AreEqual(42, state.LockedTargetInstanceId);

            Assert.IsTrue(state.TryConsumeLockedTargetHit(true));
            Assert.AreEqual(1, state.PendingHitCount);
            Assert.AreEqual(42, state.LockedTargetInstanceId);
            Assert.IsTrue(state.TryConsumeLockedTargetHit(true));
            Assert.AreEqual(0, state.PendingHitCount);
            Assert.AreEqual(WolfOwnedProxyPhase.Return, state.Phase);
        }

        [Test]
        public void PromotedWolf_TargetDeathBeforeSecondHitCancelsOnlySecondWithoutRetarget()
        {
            WolfOwnedProxyState state = new WolfOwnedProxyState();
            Vector3 lockedTarget = new Vector3(3.0f, 0.0f);
            Assert.IsTrue(state.TryBegin(Vector3.zero, lockedTarget, 77, 0.0f, 0.8f, 2));
            Assert.IsTrue(state.Advance(0.4f, out _));

            Assert.IsTrue(state.TryConsumeLockedTargetHit(true));
            Assert.IsFalse(state.TryConsumeLockedTargetHit(false));
            Assert.AreEqual(WolfOwnedProxyPhase.Return, state.Phase);
            Assert.AreEqual(0, state.PendingHitCount);
            Assert.AreEqual(77, state.LockedTargetInstanceId);
            Assert.IsTrue(state.Advance(0.6f, out Vector3 returnPosition));
            Assert.AreEqual(lockedTarget.x * 0.5f, returnPosition.x);
        }

        [Test]
        public void WolfCadence_UsesRetryForNoTargetAndStateTimeoutOrResetClearsTheChild()
        {
            CompanionWolfOwnedProxyCombatSetup promoted = new CompanionWolfOwnedProxyCombatSetup(
                    "wolf_tamer", 10, 4.0f, 4.0f, 0.8f, 1, 1, 0.15f)
                .WithPromotedBeastCommanderHits();
            CombatAbilitySchedule schedule = new CombatAbilitySchedule();
            schedule.Configure(promoted.Period, promoted.NoTargetRetrySeconds, 0.0f, 0.0f);
            schedule.RecordResolution(0.0f, false);
            Assert.IsFalse(schedule.IsDue(0.149f));
            Assert.IsTrue(schedule.IsDue(0.15f));
            schedule.RecordResolution(0.15f, true);
            Assert.IsFalse(schedule.IsDue(4.149f));
            Assert.IsTrue(schedule.IsDue(4.15f));

            WolfOwnedProxyState state = new WolfOwnedProxyState();
            Assert.IsTrue(state.TryBegin(Vector3.zero, Vector3.right, 1, 0.0f, 0.8f, promoted.HitCount));
            Assert.IsFalse(state.Advance(0.8f, out _));
            Assert.IsFalse(state.IsActive);
            Assert.IsTrue(state.TryBegin(Vector3.zero, Vector3.right, 1, 1.0f, 0.8f, promoted.HitCount));
            state.Reset();
            Assert.IsFalse(state.IsActive);
        }
    }
}
