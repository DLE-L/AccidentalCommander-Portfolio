using System.Collections.Generic;
using Lizzo.PV.Data;
using Lizzo.PV.Legion;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class StormMagePromotionChainTests
    {
        [Test]
        public void PromotedSetup_ChangesOnlyMaxTargetsFromThreeToFive()
        {
            CompanionChainCombatSetup baseSetup = ResolveBaseSetup();

            CompanionChainCombatSetup promoted = baseSetup.WithPromotedStormMageChain();
            CompanionChainCombatSetup promotedAgain = promoted.WithPromotedStormMageChain();

            Assert.AreEqual(3, baseSetup.MaxTargets);
            Assert.AreEqual(5, promoted.MaxTargets);
            Assert.AreEqual(5, promotedAgain.MaxTargets);
            Assert.AreEqual(baseSetup.Damage, promoted.Damage);
            Assert.AreEqual(baseSetup.Period, promoted.Period);
            Assert.AreEqual(baseSetup.InitialRange, promoted.InitialRange);
            Assert.AreEqual(baseSetup.ChainDistance, promoted.ChainDistance);
            Assert.AreEqual(baseSetup.NoTargetRetrySeconds, promoted.NoTargetRetrySeconds);
        }

        [Test]
        public void PromotedSelector_UsesFiveNearestUnhitTargetsWithDeterministicTies()
        {
            List<ChainTargetCandidate> source = new List<ChainTargetCandidate>
            {
                new ChainTargetCandidate(null, new Vector3(1.0f, 0.0f), 20),
                new ChainTargetCandidate(null, new Vector3(1.0f, 0.0f), 10),
                new ChainTargetCandidate(null, new Vector3(2.0f, 0.0f), 30),
                new ChainTargetCandidate(null, new Vector3(3.0f, 0.0f), 40),
                new ChainTargetCandidate(null, new Vector3(4.0f, 0.0f), 50),
                new ChainTargetCandidate(null, new Vector3(5.0f, 0.0f), 60),
            };
            List<ChainTargetCandidate> results = new List<ChainTargetCandidate>(5);

            ChainTargetSelector.Collect(source, Vector3.zero, 5.0f, 1.8f, 5, results);

            Assert.AreEqual(5, results.Count);
            CollectionAssert.AreEqual(new[] { 10, 20, 30, 40, 50 }, new[]
            {
                results[0].InstanceId, results[1].InstanceId, results[2].InstanceId, results[3].InstanceId, results[4].InstanceId,
            });
        }

        [Test]
        public void PromotedChainGrowth_AppliesDamageAndCadenceExactlyOnce()
        {
            GameObject owner = new GameObject("StormMageGrowthOnly");
            try
            {
                AllyCombat combat = owner.AddComponent<AllyCombat>();
                combat.SetCanonicalChainInfo(ResolveBaseSetup().WithPromotedStormMageChain());
                combat.ApplyGrowthScale(new CompanionGrowthScale(1.50f, 2.10f, 1.05f, 3));

                Assert.AreEqual(18, combat.Damage);
                Assert.AreEqual(2.73f, combat.AttackPeriod, 0.0001f);
                Assert.AreEqual(5, combat.ChainSetup.MaxTargets);
                Assert.AreEqual(18, combat.ChainSetup.Damage);
                Assert.AreEqual(2.73f, combat.ChainSetup.Period, 0.0001f);
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void ChainSchedule_UsesNoTargetRetryAndRestartWithoutConsumingCastPeriod()
        {
            CompanionChainCombatSetup setup = ResolveBaseSetup().WithPromotedStormMageChain();
            CombatAbilitySchedule schedule = new CombatAbilitySchedule();
            schedule.Configure(setup.Period, setup.NoTargetRetrySeconds, 0.0f, 0.0f);

            Assert.IsTrue(schedule.IsDue(0.0f));
            schedule.RecordResolution(0.0f, resolved: false);
            Assert.IsFalse(schedule.IsDue(0.149f));
            Assert.IsTrue(schedule.IsDue(0.15f));

            schedule.Restart(2.0f, 0.25f);
            Assert.IsFalse(schedule.IsDue(2.249f));
            Assert.IsTrue(schedule.IsDue(2.25f));
        }

        private static CompanionChainCombatSetup ResolveBaseSetup()
        {
            TestAssetService assets = new TestAssetService();
            TextAsset gameData = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_LizzoPV/Data/Runtime/GameData.xml");
            Assert.IsNotNull(gameData);
            assets.Register("PlayerData.xml", gameData);
            LocalDataProvider provider = new LocalDataProvider(assets);
            Assert.IsTrue(provider.InitializeAsync().GetAwaiter().GetResult().Succeeded);
            Assert.IsTrue(new CompanionChainCombatResolver(provider).TryResolve("lightning_mage", 1.0f, out CompanionChainCombatSetup setup));
            return setup;
        }
    }
}
