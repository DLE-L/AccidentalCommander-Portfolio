using Lizzo.PV.Data;
using Lizzo.PV.Legion;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class BattleApothecaryPromotionHealTests
    {
        [Test]
        public void PromotedBattleApothecary_ChangesOnlySecondaryHealContract()
        {
            CompanionRangedSupportCombatSetup baseSetup = ResolveHerbalistSetup();
            CompanionRangedSupportCombatSetup promoted = baseSetup.WithPromotedBattleApothecaryHeal();

            Assert.AreEqual("field_herbalist", promoted.Primary.SourceId);
            Assert.AreEqual(8, promoted.Primary.Damage);
            Assert.AreEqual(1.4f, promoted.Primary.Period);
            Assert.AreEqual(5.0f, promoted.Primary.Range);
            Assert.AreEqual(1, promoted.Primary.MaxTargets);
            Assert.AreEqual(4, promoted.SecondaryHealAmount);
            Assert.AreEqual(5.0f, promoted.SecondaryPeriod);
            Assert.AreEqual(4.0f, promoted.SecondaryRange);
            Assert.AreEqual(2, promoted.SecondaryMaxTargets);
            Assert.AreEqual(0.60f, promoted.SecondarySecondTargetRatio);
            Assert.AreEqual(0.15f, promoted.SecondaryNoTargetRetrySeconds);
        }

        [Test]
        public void PromotedBattleApothecary_AppliesGrowthOnceWithoutChangingFinalHealCadence()
        {
            GameObject owner = new GameObject("BattleApothecaryCombat");
            try
            {
                AllyCombat combat = owner.AddComponent<AllyCombat>();
                combat.SetCanonicalRangedSupportInfo(ResolveHerbalistSetup().WithPromotedBattleApothecaryHeal());
                combat.ApplyGrowthScale(new CompanionGrowthScale(1.55f, 2.0f, 0.90f, 3));

                Assert.AreEqual(12, combat.Damage);
                Assert.That(combat.AttackPeriod, Is.EqualTo(1.26f).Within(0.0001f));
                Assert.AreEqual(6, combat.SecondaryHealAmount);
                Assert.AreEqual(2, combat.SecondaryHealMaxTargets);
                Assert.AreEqual(0.60f, combat.SecondaryHealSecondTargetRatio);
                Assert.AreEqual(5.0f, combat.SecondaryHealPeriod);
                Assert.AreEqual(4.0f, combat.SecondaryHealRange);
                Assert.AreEqual(0.15f, combat.NoTargetRetrySeconds);
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void BaseHerbalistAndLightGuideRemainDistinctPromotionContracts()
        {
            CompanionRangedSupportCombatSetup baseHerbalist = ResolveHerbalistSetup();
            CompanionRangedSupportCombatSetup baseCleric = new CompanionRangedSupportCombatSetup(
                new CompanionProjectileCombatSetup("cleric", AllyAttackStyle.TargetedProjectile, 5, 1.6f, 4.5f, 1, 0.15f),
                8,
                4.0f,
                4.0f,
                1,
                1.0f,
                0.15f);

            Assert.AreEqual(1, baseHerbalist.SecondaryMaxTargets);
            Assert.AreEqual(6.0f, baseHerbalist.SecondaryPeriod);
            Assert.AreEqual(2, baseCleric.WithPromotedLightGuideHeal().SecondaryMaxTargets);
            Assert.AreEqual(0.70f, baseCleric.WithPromotedLightGuideHeal().SecondarySecondTargetRatio);
        }

        private static CompanionRangedSupportCombatSetup ResolveHerbalistSetup()
        {
            TestAssetService assets = new TestAssetService();
            TextAsset gameData = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_LizzoPV/Data/Runtime/GameData.xml");
            Assert.IsNotNull(gameData);
            assets.Register("PlayerData.xml", gameData);
            LocalDataProvider provider = new LocalDataProvider(assets);
            Assert.IsTrue(provider.InitializeAsync().GetAwaiter().GetResult().Succeeded);

            CompanionRangedSupportCombatResolver resolver = new CompanionRangedSupportCombatResolver(provider);
            Assert.IsTrue(resolver.TryResolve("field_herbalist", 1.0f, out CompanionRangedSupportCombatSetup setup));
            return setup;
        }
    }
}
