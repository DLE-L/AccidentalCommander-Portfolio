using System.Collections.Generic;
using Lizzo.PV.Legion;
using NUnit.Framework;
using UnityEngine;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class PowderCaptainPromotionTargetAreaTests
    {
        [Test]
        public void PromotedBombardier_UsesAuthoritativeImpactGeometryAndPushRules()
        {
            CompanionTargetAreaCombatSetup baseSetup = new CompanionTargetAreaCombatSetup(
                "bombardier", 28, 2.42f, 5.0f, 1.6f, 6, 0.5f, 0.15f);

            CompanionTargetAreaCombatSetup promoted = baseSetup.WithPromotedPowderCaptainImpact();

            Assert.AreEqual("bombardier", promoted.SourceId);
            Assert.AreEqual(28, promoted.Damage);
            Assert.AreEqual(2.42f, promoted.Period);
            Assert.AreEqual(5.0f, promoted.Range);
            Assert.AreEqual(2.0f, promoted.Radius);
            Assert.AreEqual(8, promoted.MaxTargets);
            Assert.AreEqual(0.5f, promoted.CastDelay);
            Assert.AreEqual(0.15f, promoted.NoTargetRetrySeconds);
            Assert.AreEqual(0.4f, promoted.NormalPush);
            Assert.AreEqual(0.0f, promoted.EliteBossPush);
        }

        [Test]
        public void ImpactCollector_PreservesDeterministicCapAndReturnsPerTargetPushRequests()
        {
            CompanionTargetAreaCombatSetup promoted = new CompanionTargetAreaCombatSetup(
                "bombardier", 28, 2.42f, 5.0f, 1.6f, 6, 0.5f, 0.15f)
                .WithPromotedPowderCaptainImpact();
            List<TargetAreaImpactCandidate> source = new List<TargetAreaImpactCandidate>
            {
                new TargetAreaImpactCandidate(null, new Vector3(1.0f, 0.0f), 30, TargetAreaImpactTargetClass.Normal),
                new TargetAreaImpactCandidate(null, new Vector3(-1.0f, 0.0f), 10, TargetAreaImpactTargetClass.Elite),
                new TargetAreaImpactCandidate(null, new Vector3(1.5f, 0.0f), 20, TargetAreaImpactTargetClass.Boss),
                new TargetAreaImpactCandidate(null, new Vector3(2.1f, 0.0f), 40, TargetAreaImpactTargetClass.Normal),
            };
            List<TargetAreaImpactCandidate> results = new List<TargetAreaImpactCandidate>();

            TargetAreaImpactCollector.Collect(source, Vector3.zero, promoted.Radius, promoted.MaxTargets, results);

            Assert.AreEqual(3, results.Count);
            Assert.AreEqual(10, results[0].InstanceId);
            Assert.AreEqual(30, results[1].InstanceId);
            Assert.AreEqual(20, results[2].InstanceId);
            Assert.AreEqual(0.0f, TargetAreaPushRequest.Create(promoted, results[0], Vector3.zero).Distance);
            Assert.AreEqual(0.4f, TargetAreaPushRequest.Create(promoted, results[1], Vector3.zero).Distance);
            Assert.AreEqual(0.0f, TargetAreaPushRequest.Create(promoted, results[2], Vector3.zero).Distance);
        }

        [Test]
        public void CastState_RetriesThenResolvesOneDelayedImpactAndResetCancelsPendingImpact()
        {
            CompanionTargetAreaCombatSetup setup = new CompanionTargetAreaCombatSetup(
                "bombardier", 28, 2.42f, 5.0f, 1.6f, 6, 0.5f, 0.15f)
                .WithPromotedPowderCaptainImpact();
            TargetAreaCastState state = new TargetAreaCastState();
            state.Configure(setup, 0.0f, 0.0f);

            state.RecordNoTarget(0.0f);
            Assert.AreEqual(0.15f, state.NextTargetDueTime);
            Assert.IsTrue(state.TryBeginCast(0.15f, Vector3.one));
            Assert.IsFalse(state.TryConsumeImpact(0.64f, out _));
            state.Restart(0.64f, 0.0f);
            Assert.IsFalse(state.TryConsumeImpact(0.65f, out _));
            Assert.IsTrue(state.TryBeginCast(0.64f, Vector3.one));
            Assert.IsTrue(state.TryConsumeImpact(1.14f, out _));
            Assert.IsFalse(state.TryConsumeImpact(1.14f, out _));
        }
    }
}
