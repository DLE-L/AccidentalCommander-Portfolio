using System;
using System.Text.RegularExpressions;
using System.Threading;
using Lizzo.PV.Data;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.TestTools;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class GameDataProviderTests
    {
        [TestCase("fake")]
        [TestCase("local")]
        public void ProvidersRequireInitializationBeforeLookup(string adapter)
        {
            IDataProvider provider = CreateProvider(adapter);

            Assert.Throws<InvalidOperationException>(() => provider.GetEnemy("boss_hungry_giant"));
        }

        [TestCase("fake")]
        [TestCase("local")]
        public void ProvidersLoadTheCanonicalLookupContract(string adapter)
        {
            IDataProvider provider = CreateProvider(adapter);
            DataLoadResult result = provider.InitializeAsync().GetAwaiter().GetResult();

            Assert.IsTrue(result.Succeeded);
            Assert.IsTrue(provider.IsInitialized);
            Assert.IsFalse(result.UsedFallback);
            Assert.AreEqual("commander_01", provider.GetUnit("commander_01").Id);
            Assert.AreEqual("commander_basic", provider.GetSkill("commander_basic").Id);
            Assert.AreEqual(2500, provider.GetEnemy("boss_hungry_giant").Hp);
            Assert.AreEqual(8, provider.GetLevelExp(1));
            Assert.AreEqual(5, provider.RunTuning.TimedElite.EnemyTemplateId);
            Assert.AreEqual(EnemyEncounterRank.Elite, provider.RunTuning.TimedElite.EncounterRank);
            Assert.AreEqual(5, provider.RunTuning.TutorialFinalThreat.EnemyTemplateId);
            Assert.AreEqual(EnemyEncounterRank.Elite, provider.RunTuning.TutorialFinalThreat.EncounterRank);
            Assert.AreEqual(3, provider.RunTuning.Stage1FinalThreat.EnemyTemplateId);
            Assert.AreEqual(EnemyEncounterRank.Boss, provider.RunTuning.Stage1FinalThreat.EncounterRank);
            Assert.AreEqual(3, provider.RunTuning.Stage2FinalThreat.EnemyTemplateId);
            Assert.AreEqual(3, provider.RunTuning.Stage3FinalThreat.EnemyTemplateId);
        }

        [TestCase("fake")]
        [TestCase("local")]
        public void ProvidersReturnNullForMissingCanonicalLookups(string adapter)
        {
            IDataProvider provider = CreateInitializedProvider(adapter);

            Assert.IsNull(provider.GetUnit("missing_unit"));
            Assert.IsNull(provider.GetSkill("missing_skill"));
            Assert.IsNull(provider.GetEnemy("missing_enemy"));
            Assert.IsNull(provider.GetEnemyByTemplateId(999));
        }

        [Test]
        public void FakeBaselineAndOverridesRemainIsolated()
        {
            FakeDataProvider provider = new FakeDataProvider()
                .SetEnemy(new EnemyData { Id = "test_enemy", TemplateId = 99, Hp = 321 })
                .SetLevelExp(1, 11);

            DataLoadResult result = provider.InitializeAsync().GetAwaiter().GetResult();

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(321, provider.GetEnemyByTemplateId(99).Hp);
            Assert.AreEqual(11, provider.GetLevelExp(1));
            Assert.AreEqual(2500, provider.GetEnemy("boss_hungry_giant").Hp);
        }

        [Test]
        public void FakeFailedInitializationReportsRequiredIds()
        {
            FakeDataProvider provider = new FakeDataProvider().SetInitializationFailure("enemy:test");
            DataLoadResult result = provider.InitializeAsync().GetAwaiter().GetResult();

            Assert.IsFalse(result.Succeeded);
            CollectionAssert.Contains(result.MissingRequiredIds, "enemy:test");
        }

        [Test]
        public void FakeInitializationHonorsCancellation()
        {
            FakeDataProvider provider = new FakeDataProvider();
            using CancellationTokenSource source = new CancellationTokenSource();
            source.Cancel();

            Assert.Throws<OperationCanceledException>(() => provider.InitializeAsync(source.Token).GetAwaiter().GetResult());
        }

        [Test]
        public void LocalProviderLoadsProjectDataWithoutAddressables()
        {
            TestAssetService assets = new TestAssetService();
            UnityEngine.TextAsset gameData = AssetDatabase.LoadAssetAtPath<UnityEngine.TextAsset>("Assets/_LizzoPV/Gameplay/Run/Data/GameData.xml");
            Assert.IsNotNull(gameData);
            assets.Register("PlayerData.xml", gameData);

            LocalDataProvider provider = new LocalDataProvider(assets);
            DataLoadResult result = provider.InitializeAsync().GetAwaiter().GetResult();

            Assert.IsTrue(result.Succeeded);
            Assert.IsFalse(result.UsedFallback);
            Assert.AreEqual(8, provider.GetLevelExp(1));
            Assert.AreEqual(2500, provider.GetEnemy("boss_hungry_giant").Hp);
        }

        [Test]
        public void LocalMissingAssetUsesFallbackAndReportsParseError()
        {
            LogAssert.Expect(UnityEngine.LogType.Error, new Regex("\\[LocalDataProvider\\].*"));
            LocalDataProvider provider = new LocalDataProvider(new TestAssetService());
            DataLoadResult result = provider.InitializeAsync().GetAwaiter().GetResult();

            Assert.IsTrue(result.Succeeded);
            Assert.IsTrue(result.UsedFallback);
            Assert.IsNotEmpty(result.ParseError);
            Assert.AreEqual(2500, provider.GetEnemy("boss_hungry_giant").Hp);
        }

        static IDataProvider CreateInitializedProvider(string adapter)
        {
            IDataProvider provider = CreateProvider(adapter);
            DataLoadResult result = provider.InitializeAsync().GetAwaiter().GetResult();
            Assert.IsTrue(result.Succeeded);
            return provider;
        }

        static IDataProvider CreateProvider(string adapter)
        {
            switch (adapter)
            {
                case "fake":
                    return new FakeDataProvider();
                case "local":
                    TestAssetService assets = new TestAssetService();
                    UnityEngine.TextAsset gameData = AssetDatabase.LoadAssetAtPath<UnityEngine.TextAsset>("Assets/_LizzoPV/Gameplay/Run/Data/GameData.xml");
                    Assert.IsNotNull(gameData);
                    assets.Register("PlayerData.xml", gameData);
                    return new LocalDataProvider(assets);
                default:
                    Assert.Fail("Unknown provider adapter: " + adapter);
                    return null;
            }
        }
    }
}
