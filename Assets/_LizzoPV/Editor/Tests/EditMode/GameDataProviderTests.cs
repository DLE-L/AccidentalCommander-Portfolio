using System;
using System.Reflection;
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
            Assert.That(provider.GetUnit("commander_01").SkillId, Is.Empty);
            Assert.That(provider.GetUnit("commander_01").Attack, Is.Zero);
            Assert.AreEqual(2500, provider.GetEnemy("boss_hungry_giant").Hp);
            Assert.AreEqual(16, provider.GetEnemy("red_charger").Attack);
            Assert.AreEqual(24, provider.GetEnemy("red_charger").ChargeAttack);
            Assert.AreEqual("normal", provider.GetEnemy("red_charger").Type);
            Assert.IsNull(provider.GetEnemy("elite_red_charger"));
            Assert.AreEqual(8, provider.GetLevelExp(1));
            Assert.AreEqual(5, provider.RunTuning.TimedElite.EnemyTemplateId);
            Assert.AreEqual(EnemyEncounterRank.Elite, provider.RunTuning.TimedElite.EncounterRank);
            Assert.AreEqual(5, provider.RunTuning.TutorialFinalThreat.EnemyTemplateId);
            Assert.AreEqual(EnemyEncounterRank.Boss, provider.RunTuning.TutorialFinalThreat.EncounterRank);
            Assert.AreEqual(3, provider.RunTuning.Stage1FinalThreat.EnemyTemplateId);
            Assert.AreEqual(EnemyEncounterRank.Boss, provider.RunTuning.Stage1FinalThreat.EncounterRank);
            Assert.AreEqual(3, provider.RunTuning.Stage2FinalThreat.EnemyTemplateId);
            Assert.AreEqual(3, provider.RunTuning.Stage3FinalThreat.EnemyTemplateId);
            Assert.That(provider.GetSynergy("pair-first-balance").Cooldown, Is.EqualTo(6.0f));
            Assert.That(provider.GetSynergy("pair-first-balance").BalanceParameters, Does.Contain("crossSlashDamage=15"));
            Assert.That(provider.GetSynergy("pair-second-balance").BalanceParameters, Does.Contain("coverBombDelay=0.4"));
            Assert.That(provider.GetSynergy("trio-first-balance").Cooldown, Is.EqualTo(10.0f));
            Assert.That(provider.GetSynergy("trio-second-balance").BalanceParameters, Does.Contain("sanctuaryRadius=2"));
            Assert.That(provider.GetSynergy("synergy-trigger-balance").Cooldown, Is.EqualTo(6.0f));
            Assert.That(provider.GetSynergy("synergy-trigger-balance").BalanceParameters, Does.Contain("counterThreshold=3"));
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
            Assert.AreEqual(24, provider.GetEnemy("red_charger").ChargeAttack);
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
            AssertCurrentEnemyAddresses(provider);
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
            Assert.AreEqual(24, provider.GetEnemy("red_charger").ChargeAttack);
            AssertCurrentEnemyAddresses(provider);
        }

        [Test]
        public void LocalMissingAssetFailsClosedWhenEmbeddedFallbackIsDisabled()
        {
            LogAssert.Expect(UnityEngine.LogType.Error, new Regex("\\[LocalDataProvider\\].*embeddedFallback=False"));
            LogAssert.Expect(UnityEngine.LogType.Error, new Regex("\\[LocalDataProvider\\] Required data missing:"));
            LocalDataProvider provider = CreateLocalProvider(
                new TestAssetService(),
                allowEmbeddedFallback: false);

            DataLoadResult result = provider.InitializeAsync().GetAwaiter().GetResult();

            Assert.IsFalse(result.Succeeded);
            Assert.IsFalse(result.UsedFallback);
            Assert.IsNotEmpty(result.ParseError);
            Assert.IsNotEmpty(result.MissingRequiredIds);
            Assert.IsNull(provider.GetEnemy("boss_hungry_giant"));
        }

        static LocalDataProvider CreateLocalProvider(IAssetService assets, bool allowEmbeddedFallback)
        {
            ConstructorInfo constructor = typeof(LocalDataProvider).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                new[] { typeof(IAssetService), typeof(bool) },
                modifiers: null);
            Assert.IsNotNull(constructor, "Missing LocalDataProvider fallback-policy test seam.");
            return (LocalDataProvider)constructor.Invoke(new object[] { assets, allowEmbeddedFallback });
        }

        static void AssertCurrentEnemyAddresses(IDataProvider provider)
        {
            Assert.AreEqual("Units/Enemies/SmallGoblin", provider.GetEnemy("small_goblin").Prefab);
            Assert.AreEqual("Units/Enemies/HungryWolf", provider.GetEnemy("hungry_wolf").Prefab);
            Assert.AreEqual("Units/Enemies/ShieldOrc", provider.GetEnemy("shield_orc").Prefab);
            Assert.AreEqual("Units/Enemies/RedCharger", provider.GetEnemy("red_charger").Prefab);
            Assert.AreEqual("Units/Enemies/HungryGiant", provider.GetEnemy("boss_hungry_giant").Prefab);
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
