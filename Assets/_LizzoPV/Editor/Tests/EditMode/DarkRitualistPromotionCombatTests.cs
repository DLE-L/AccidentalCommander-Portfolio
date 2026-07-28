using Lizzo.PV.Data;
using Lizzo.PV.Legion;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class DarkRitualistPromotionSummonTests
    {
        [Test]
        public void PromotedCurse_ChangesOnlyRangeAfterOneGrowthApplication()
        {
            LocalDataProvider data = CreateProjectProvider();
            Assert.IsTrue(data.InitializeAsync().GetAwaiter().GetResult().Succeeded);
            CompanionProjectileCombatResolver resolver = new CompanionProjectileCombatResolver(data);

            Assert.IsTrue(resolver.TryResolve("necromancer", 1.0f, out CompanionProjectileCombatSetup baseSetup));
            CompanionProjectileCombatSetup promoted = baseSetup
                .WithPromotedDarkRitualistRange()
                .WithGrowthScale(new CompanionGrowthScale(1.75f, 2.20f, 1.05f, 3));

            Assert.AreEqual(5.0f, baseSetup.Range);
            Assert.AreEqual(5.3f, promoted.Range);
            Assert.AreEqual("necromancer", promoted.SourceId);
            Assert.AreEqual(AllyAttackStyle.TargetedProjectile, promoted.AttackStyle);
            Assert.AreEqual(1, promoted.MaxTargets);
            Assert.AreEqual(14, promoted.Damage);
            Assert.That(promoted.Period, Is.EqualTo(3.15f).Within(0.0001f));
            Assert.AreEqual(0.15f, promoted.NoTargetRetrySeconds);
        }

        [Test]
        public void PromotedPersonalSkeleton_UsesCapTwoAndOneRequestPerFifteenCountableKills()
        {
            LocalDataProvider data = CreateProjectProvider();
            Assert.IsTrue(data.InitializeAsync().GetAwaiter().GetResult().Succeeded);
            Assert.IsTrue(new CompanionPersonalSummonResolver(data).TryResolve("necromancer", out CompanionPersonalSummonSetup summon));
            Assert.AreEqual(1, summon.ResolveActiveCap(false));
            Assert.AreEqual(2, summon.ResolveActiveCap(true));

            CountableKillThresholdState state = new CountableKillThresholdState();
            state.Configure(summon.CountableKillThreshold, summon.ResolveActiveCap(true));
            Assert.IsFalse(state.RecordKill(false));
            Assert.IsTrue(RecordThreshold(state, 15));
            Assert.AreEqual(1, state.ActiveCount);
            Assert.IsTrue(RecordThreshold(state, 15));
            Assert.AreEqual(2, state.ActiveCount);
            Assert.IsFalse(state.RecordKill(true));
            Assert.AreEqual(0, state.PendingCountableKills);
        }

        [Test]
        public void PromotedPersonalSkeleton_ReleaseRearmsOnlyWithANewThresholdAndResetClearsState()
        {
            CountableKillThresholdState state = new CountableKillThresholdState();
            state.Configure(15, 2);
            Assert.IsTrue(RecordThreshold(state, 15));
            Assert.IsTrue(RecordThreshold(state, 15));
            Assert.IsTrue(state.ReleaseOne());
            Assert.AreEqual(1, state.ActiveCount);
            Assert.IsFalse(RecordThreshold(state, 14));
            Assert.IsTrue(state.RecordKill(true));
            Assert.AreEqual(2, state.ActiveCount);

            state.Reset();
            Assert.AreEqual(0, state.ActiveCount);
            Assert.AreEqual(0, state.PendingCountableKills);
        }

        [Test]
        public void PersonalSkeletonIdentity_RemainsSeparateFromSynergyAndHasNoCompanionSlotTags()
        {
            LocalDataProvider data = CreateProjectProvider();
            Assert.IsTrue(data.InitializeAsync().GetAwaiter().GetResult().Succeeded);
            Assert.IsTrue(new CompanionPersonalSummonResolver(data).TryResolve("necromancer", out CompanionPersonalSummonSetup summon));

            Assert.AreEqual("UNIT_PERSONAL_SKELETON_01", summon.SummonId);
            Assert.AreEqual("UNIT_SYNERGY_SKELETON_01", summon.DistinctFromSummonId);
            Assert.AreNotEqual(summon.SummonId, summon.DistinctFromSummonId);
            StringAssert.Contains("companion_tag=false", summon.Tags);
            StringAssert.Contains("no_family_tag", summon.Tags);
        }

        private static bool RecordThreshold(CountableKillThresholdState state, int count)
        {
            bool requested = false;
            for (int i = 0; i < count; i++)
                requested |= state.RecordKill(true);
            return requested;
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
