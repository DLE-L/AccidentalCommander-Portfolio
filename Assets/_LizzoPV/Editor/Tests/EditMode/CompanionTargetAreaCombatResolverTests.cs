using System.Collections.Generic;
using Lizzo.PV.Data;
using Lizzo.PV.Legion;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class CompanionTargetAreaCombatResolverTests
    {
        [Test]
        public void Resolver_MapsBombardierAndSkeletonBomberToCanonicalTargetAreaSetups()
        {
            LocalDataProvider provider = CreateProjectProvider();
            Assert.IsTrue(provider.InitializeAsync().GetAwaiter().GetResult().Succeeded);
            CompanionTargetAreaCombatResolver resolver = new CompanionTargetAreaCombatResolver(provider);

            Assert.IsTrue(resolver.TryResolve("bombardier", 1.0f, out CompanionTargetAreaCombatSetup bombardier));
            AssertSetup(bombardier, "bombardier", 16, 2.2f, 5.0f, 1.6f, 6, 0.5f, 0.15f);

            Assert.IsTrue(resolver.TryResolve("skeleton_bomber", 1.0f, out CompanionTargetAreaCombatSetup skeletonBomber));
            AssertSetup(skeletonBomber, "skeleton_bomber", 15, 2.4f, 4.8f, 1.5f, 6, 0.0f, 0.15f);

            Assert.IsFalse(resolver.TryResolve("fire_mage", 1.0f, out _));
        }

        [Test]
        public void CastState_UsesRetryDelayAndConsumesOneLockedImpactExactlyOnce()
        {
            CompanionTargetAreaCombatSetup setup = new CompanionTargetAreaCombatSetup(
                "bombardier", 16, 2.2f, 5.0f, 1.6f, 6, 0.5f, 0.15f);
            TargetAreaCastState state = new TargetAreaCastState();
            state.Configure(setup, 0.0f, 0.0f);

            Assert.IsTrue(state.IsReadyForTarget(0.0f));
            state.RecordNoTarget(0.0f);
            Assert.AreEqual(0.15f, state.NextTargetDueTime);
            Assert.IsFalse(state.IsReadyForTarget(0.14f));
            Assert.IsTrue(state.IsReadyForTarget(0.15f));

            Vector3 impactPoint = new Vector3(2.0f, 3.0f, 0.0f);
            Assert.IsTrue(state.TryBeginCast(0.15f, impactPoint));
            Assert.IsFalse(state.TryConsumeImpact(0.64f, out _));
            Assert.IsTrue(state.TryConsumeImpact(0.65f, out Vector3 consumedPoint));
            Assert.AreEqual(impactPoint, consumedPoint);
            Assert.IsFalse(state.TryConsumeImpact(0.65f, out _));
            Assert.AreEqual(2.85f, state.NextTargetDueTime);
        }

        [Test]
        public void ImpactCollector_OrdersByDistanceThenInstanceIdAndCapsTargets()
        {
            List<TargetAreaImpactCandidate> source = new List<TargetAreaImpactCandidate>
            {
                new TargetAreaImpactCandidate(null, new Vector3(1.0f, 0.0f), 30),
                new TargetAreaImpactCandidate(null, new Vector3(-1.0f, 0.0f), 10),
                new TargetAreaImpactCandidate(null, new Vector3(2.0f, 0.0f), 20),
                new TargetAreaImpactCandidate(null, new Vector3(4.0f, 0.0f), 40),
            };
            List<TargetAreaImpactCandidate> results = new List<TargetAreaImpactCandidate>();

            TargetAreaImpactCollector.Collect(source, Vector3.zero, 2.0f, 3, results);

            Assert.AreEqual(3, results.Count);
            Assert.AreEqual(10, results[0].InstanceId);
            Assert.AreEqual(30, results[1].InstanceId);
            Assert.AreEqual(20, results[2].InstanceId);
        }

        private static void AssertSetup(
            CompanionTargetAreaCombatSetup setup,
            string sourceId,
            int damage,
            float period,
            float range,
            float radius,
            int maxTargets,
            float castDelay,
            float retry)
        {
            Assert.AreEqual(sourceId, setup.SourceId);
            Assert.AreEqual(damage, setup.Damage);
            Assert.AreEqual(period, setup.Period);
            Assert.AreEqual(range, setup.Range);
            Assert.AreEqual(radius, setup.Radius);
            Assert.AreEqual(maxTargets, setup.MaxTargets);
            Assert.AreEqual(castDelay, setup.CastDelay);
            Assert.AreEqual(retry, setup.NoTargetRetrySeconds);
        }

        private static LocalDataProvider CreateProjectProvider()
        {
            TestAssetService assets = new TestAssetService();
            TextAsset gameData = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_LizzoPV/Data/Runtime/GameData.xml");
            Assert.IsNotNull(gameData);
            assets.Register("PlayerData.xml", gameData);
            return new LocalDataProvider(assets);
        }
    }
}
