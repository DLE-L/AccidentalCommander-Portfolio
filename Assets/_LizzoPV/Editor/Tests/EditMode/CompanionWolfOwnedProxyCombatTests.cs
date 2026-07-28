using System.Collections.Generic;
using Lizzo.PV.Data;
using Lizzo.PV.Legion;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class CompanionWolfOwnedProxyCombatTests
    {
        [Test]
        public void Resolver_MapsWolfAssaultToCanonicalProxySetup()
        {
            LocalDataProvider provider = CreateProjectProvider();
            Assert.IsTrue(provider.InitializeAsync().GetAwaiter().GetResult().Succeeded);
            CompanionWolfOwnedProxyCombatResolver resolver = new CompanionWolfOwnedProxyCombatResolver(provider);

            Assert.IsTrue(resolver.TryResolve("wolf_tamer", out CompanionWolfOwnedProxyCombatSetup setup));
            Assert.AreEqual("wolf_tamer", setup.SourceId);
            Assert.AreEqual(10, setup.Damage);
            Assert.AreEqual(4.0f, setup.Period);
            Assert.AreEqual(4.0f, setup.SearchRange);
            Assert.AreEqual(0.8f, setup.Duration);
            Assert.AreEqual(1, setup.MaxTargets);
            Assert.AreEqual(1, setup.MaxActive);
            Assert.AreEqual(0.15f, setup.NoTargetRetrySeconds);
            Assert.IsFalse(resolver.TryResolve("falcon_archer", out _));
        }

        [Test]
        public void TargetSelector_ChoosesNearestThenLowestInstanceIdWithinRange()
        {
            List<WolfOwnedProxyTargetCandidate> candidates = new List<WolfOwnedProxyTargetCandidate>
            {
                new WolfOwnedProxyTargetCandidate(30, new Vector3(3.0f, 0.0f), true),
                new WolfOwnedProxyTargetCandidate(20, new Vector3(2.0f, 0.0f), true),
                new WolfOwnedProxyTargetCandidate(10, new Vector3(2.0f, 0.0f), true),
                new WolfOwnedProxyTargetCandidate(1, new Vector3(1.0f, 0.0f), false),
            };

            Assert.IsTrue(WolfOwnedProxyTargetSelector.TrySelectNearest(Vector3.zero, 4.0f, candidates, out WolfOwnedProxyTargetCandidate target));
            Assert.AreEqual(10, target.InstanceId);
            Assert.IsFalse(WolfOwnedProxyTargetSelector.TrySelectNearest(Vector3.zero, 1.0f, candidates, out _));
        }

        [Test]
        public void State_EnforcesOneActiveProxy_AndTransitionsDashHitReturnTimeout()
        {
            WolfOwnedProxyState state = new WolfOwnedProxyState();
            Vector3 owner = Vector3.zero;
            Vector3 target = new Vector3(4.0f, 0.0f);

            Assert.IsTrue(state.TryBegin(owner, target, 0.0f, 0.8f));
            Assert.IsFalse(state.TryBegin(owner, target, 0.0f, 0.8f));
            Assert.AreEqual(WolfOwnedProxyPhase.Dash, state.Phase);

            Assert.IsTrue(state.Advance(0.2f, out Vector3 dashPosition, out bool shouldDamage));
            Assert.IsFalse(shouldDamage);
            Assert.AreEqual(2.0f, dashPosition.x);

            Assert.IsTrue(state.Advance(0.4f, out Vector3 impactPosition, out shouldDamage));
            Assert.IsTrue(shouldDamage);
            Assert.AreEqual(4.0f, impactPosition.x);
            Assert.AreEqual(WolfOwnedProxyPhase.Return, state.Phase);

            Assert.IsTrue(state.Advance(0.6f, out Vector3 returnPosition, out shouldDamage));
            Assert.IsFalse(shouldDamage);
            Assert.AreEqual(2.0f, returnPosition.x);

            Assert.IsFalse(state.Advance(0.8f, out Vector3 finishedPosition, out shouldDamage));
            Assert.AreEqual(owner, finishedPosition);
            Assert.AreEqual(WolfOwnedProxyPhase.Inactive, state.Phase);
        }

        [Test]
        public void State_ResetClearsOwnerDownOrRunState()
        {
            WolfOwnedProxyState state = new WolfOwnedProxyState();
            Assert.IsTrue(state.TryBegin(Vector3.zero, Vector3.right, 0.0f, 0.8f));

            state.Reset();
            Assert.IsFalse(state.IsActive);
            Assert.AreEqual(WolfOwnedProxyPhase.Inactive, state.Phase);
            Assert.IsTrue(state.TryBegin(Vector3.zero, Vector3.right, 1.0f, 0.8f));
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
