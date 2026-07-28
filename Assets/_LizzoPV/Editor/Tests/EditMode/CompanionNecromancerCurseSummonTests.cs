using Lizzo.PV.Data;
using Lizzo.PV.Legion;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class CompanionNecromancerCurseSummonTests
    {
        [Test]
        public void Resolver_MapsCanonicalCurseProjectileAndPersonalSkeleton()
        {
            LocalDataProvider data = CreateProjectProvider();
            Assert.IsTrue(data.InitializeAsync().GetAwaiter().GetResult().Succeeded);

            CompanionProjectileCombatResolver projectileResolver = new CompanionProjectileCombatResolver(data);
            Assert.IsTrue(projectileResolver.TryResolve("necromancer", 1.0f, out CompanionProjectileCombatSetup projectile));
            Assert.AreEqual("necromancer", projectile.SourceId);
            Assert.AreEqual(AllyAttackStyle.TargetedProjectile, projectile.AttackStyle);
            Assert.AreEqual(8, projectile.Damage);
            Assert.AreEqual(3.0f, projectile.Period);
            Assert.AreEqual(5.0f, projectile.Range);
            Assert.AreEqual(1, projectile.MaxTargets);
            Assert.AreEqual(0.15f, projectile.NoTargetRetrySeconds);

            CompanionPersonalSummonResolver summonResolver = new CompanionPersonalSummonResolver(data);
            Assert.IsTrue(summonResolver.TryResolve("necromancer", out CompanionPersonalSummonSetup summon));
            Assert.AreEqual("UNIT_PERSONAL_SKELETON_01", summon.SummonId);
            Assert.AreEqual("necromancer", summon.OwnerUnitId);
            Assert.AreEqual(15, summon.CountableKillThreshold);
            Assert.AreEqual(1, summon.BaseActiveCap);
            Assert.AreEqual(2, summon.PromotedActiveCap);
            Assert.AreEqual(18, summon.Hp);
            Assert.AreEqual(4, summon.Damage);
            Assert.AreEqual(1.3f, summon.AttackInterval);
            Assert.AreEqual(1.0f, summon.Range);
            Assert.AreEqual(2.7f, summon.MoveSpeed);
            Assert.AreEqual(0.2f, summon.AiScanInterval);
            Assert.AreEqual(CombatTargetRule.Nearest, summon.TargetRule);
            Assert.AreEqual("UNIT_SYNERGY_SKELETON_01", summon.DistinctFromSummonId);
            StringAssert.Contains("companion_tag=false", summon.Tags);
            StringAssert.Contains("no_family_tag", summon.Tags);
        }

        [Test]
        public void CountableKillThreshold_RequestsOnePersonalSummonThenRequiresReleaseAndNewKills()
        {
            CountableKillThresholdState state = new CountableKillThresholdState();
            state.Configure(15, 1);

            Assert.IsFalse(state.RecordKill(false));
            for (int i = 0; i < 14; i++)
                Assert.IsFalse(state.RecordKill(true));

            Assert.IsTrue(state.RecordKill(true));
            Assert.AreEqual(1, state.ActiveCount);
            Assert.IsFalse(state.RecordKill(true));

            Assert.IsTrue(state.ReleaseOne());
            for (int i = 0; i < 14; i++)
                Assert.IsFalse(state.RecordKill(true));
            Assert.IsTrue(state.RecordKill(true));

            state.Reset();
            Assert.AreEqual(0, state.ActiveCount);
            Assert.AreEqual(0, state.PendingCountableKills);
        }

        [Test]
        public void Resolver_RejectsNonNecromancerAndPersonalIdentityIsSeparateFromSynergy()
        {
            LocalDataProvider data = CreateProjectProvider();
            Assert.IsTrue(data.InitializeAsync().GetAwaiter().GetResult().Succeeded);

            CompanionPersonalSummonResolver resolver = new CompanionPersonalSummonResolver(data);
            Assert.IsFalse(resolver.TryResolve("skeleton_bomber", out _));
            Assert.IsTrue(resolver.TryResolve("necromancer", out CompanionPersonalSummonSetup summon));
            Assert.AreNotEqual(summon.SummonId, summon.DistinctFromSummonId);
        }

        [Test]
        public void FallbackCatalog_PreservesCanonicalPersonalSkeletonData()
        {
            LogAssert.Expect(LogType.Error, "[LocalDataProvider] Local data asset was not available. address=PlayerData.xml");
            LocalDataProvider data = new LocalDataProvider(new TestAssetService());
            Assert.IsTrue(data.InitializeAsync().GetAwaiter().GetResult().Succeeded);
            Assert.AreSame(data.CompanionSummons, data.CompanionSummons);
            Assert.AreEqual(1, data.CompanionSummons.Count);
            Assert.AreEqual("UNIT_PERSONAL_SKELETON_01", data.GetCompanionSummon("UNIT_PERSONAL_SKELETON_01").Id);
            Assert.IsTrue(new CompanionPersonalSummonResolver(data).TryResolve("necromancer", out _));
        }

        static LocalDataProvider CreateProjectProvider()
        {
            TestAssetService assets = new TestAssetService();
            TextAsset gameData = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_LizzoPV/Data/Runtime/GameData.xml");
            Assert.IsNotNull(gameData);
            assets.Register("PlayerData.xml", gameData);
            return new LocalDataProvider(assets);
        }
    }
}
