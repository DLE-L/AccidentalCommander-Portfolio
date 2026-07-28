using Lizzo.PV.Legion;
using NUnit.Framework;
using UnityEngine;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class WolfWraithLiveCombatSeamTests
    {
        [Test]
        public void WolfState_LocksOneTarget_AllowsPromotedTwoHits_AndCancelsInvalidSecond()
        {
            WolfOwnedProxyState state = new WolfOwnedProxyState();
            Assert.IsTrue(state.TryBegin(Vector3.zero, Vector3.right, 42, 0.0f, 0.8f, 2));
            Assert.AreEqual(WolfOwnedProxyPhase.Dash, state.Phase);
            state.Advance(0.5f, out _, out bool firstHit);
            Assert.IsTrue(firstHit); Assert.AreEqual(42, state.LockedTargetInstanceId); Assert.AreEqual(1, state.PendingHitCount);
            Assert.IsFalse(state.TryConsumeLockedTargetHit(false));
            Assert.AreEqual(WolfOwnedProxyPhase.Return, state.Phase);
            state.Reset(); Assert.AreEqual(WolfOwnedProxyPhase.Inactive, state.Phase); Assert.AreEqual(0, state.PendingHitCount);
        }

        [Test]
        public void WolfState_BaseConsumesExactlyOneLockedHit()
        {
            WolfOwnedProxyState state = new WolfOwnedProxyState();
            Assert.IsTrue(state.TryBegin(Vector3.zero, Vector3.right, 7, 0.0f, 0.8f, 1));
            state.Advance(0.5f, out _, out bool hit);
            Assert.IsTrue(hit); Assert.AreEqual(0, state.PendingHitCount); Assert.AreEqual(WolfOwnedProxyPhase.Return, state.Phase);
        }

        [Test]
        public void PersonalMitigation_ActivatesExpiresAndResetsOwnerLocally()
        {
            PersonalDamageMitigationState baseState = new PersonalDamageMitigationState();
            baseState.Configure(new PersonalDamageMitigationSetup(0.60f, 5.0f, 1.2f), 0.0f);
            Assert.IsTrue(baseState.Advance(0.0f)); Assert.AreEqual(6, baseState.ApplyToSelf(10));
            Assert.IsFalse(baseState.Advance(1.2f)); Assert.AreEqual(10, baseState.ApplyToSelf(10));
            PersonalDamageMitigationState promoted = new PersonalDamageMitigationState();
            promoted.Configure(new PersonalDamageMitigationSetup(0.50f, 5.0f, 1.5f), 0.0f);
            Assert.IsTrue(promoted.Advance(0.0f)); Assert.AreEqual(5, promoted.ApplyToSelf(10));
            baseState.ResetForOwnerDown(2.0f); Assert.IsFalse(baseState.IsActive); Assert.AreEqual(10, baseState.ApplyToSelf(10));
        }
    }
}
