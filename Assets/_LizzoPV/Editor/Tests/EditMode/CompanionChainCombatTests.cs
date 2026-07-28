using System.Collections.Generic;
using Lizzo.PV.Data;
using Lizzo.PV.Legion;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class CompanionChainCombatTests
    {
        [Test]
        public void Resolver_MapsLightningMageExactCanonicalValues()
        {
            TestAssetService assets = new TestAssetService(); assets.Register("PlayerData.xml", AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_LizzoPV/Data/Runtime/GameData.xml"));
            LocalDataProvider data = new LocalDataProvider(assets); Assert.IsTrue(data.InitializeAsync().GetAwaiter().GetResult().Succeeded);
            Assert.IsTrue(new CompanionChainCombatResolver(data).TryResolve("lightning_mage", 1.0f, out CompanionChainCombatSetup setup));
            Assert.AreEqual(12, setup.Damage); Assert.AreEqual(2.6f, setup.Period); Assert.AreEqual(5f, setup.InitialRange); Assert.AreEqual(1.8f, setup.ChainDistance); Assert.AreEqual(3, setup.MaxTargets); Assert.AreEqual(0.15f, setup.NoTargetRetrySeconds);
        }

        [Test]
        public void Selector_UsesNearestThenNearestUnhitWithDeterministicCap()
        {
            List<ChainTargetCandidate> source = new List<ChainTargetCandidate> { new ChainTargetCandidate(null, new Vector3(1,0), 30), new ChainTargetCandidate(null, new Vector3(1,0), 10), new ChainTargetCandidate(null, new Vector3(2.5f,0), 20), new ChainTargetCandidate(null, new Vector3(4.2f,0), 40) };
            List<ChainTargetCandidate> results = new List<ChainTargetCandidate>();
            ChainTargetSelector.Collect(source, Vector3.zero, 5.0f, 1.8f, 3, results);
            Assert.AreEqual(3, results.Count);
            Assert.AreEqual(10, results[0].InstanceId);
            Assert.AreEqual(30, results[1].InstanceId);
            Assert.AreEqual(20, results[2].InstanceId);
        }
    }
}
