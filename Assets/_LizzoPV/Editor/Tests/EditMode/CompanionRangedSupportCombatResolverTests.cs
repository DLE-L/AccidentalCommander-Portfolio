using Lizzo.PV.Data;
using Lizzo.PV.Legion;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class CompanionRangedSupportCombatResolverTests
    {
        [Test]
        public void Resolver_MapsClericAndHerbalistToCanonicalRangedSupportSetups()
        {
            LocalDataProvider provider = CreateProjectProvider();
            Assert.IsTrue(provider.InitializeAsync().GetAwaiter().GetResult().Succeeded);
            CompanionRangedSupportCombatResolver resolver = new CompanionRangedSupportCombatResolver(provider);

            Assert.IsTrue(resolver.TryResolve("cleric", 1.0f, out CompanionRangedSupportCombatSetup cleric));
            AssertSetup(cleric, "cleric", 5, 1.6f, 4.5f, 8, 4.0f, 4.0f);

            Assert.IsTrue(resolver.TryResolve("field_herbalist", 1.0f, out CompanionRangedSupportCombatSetup herbalist));
            AssertSetup(herbalist, "field_herbalist", 8, 1.4f, 5.0f, 4, 6.0f, 4.0f);
            Assert.IsFalse(resolver.TryResolve("falcon_archer", 1.0f, out _));
        }

        [Test]
        public void HerbalistSetup_AppliesThroughGenericAllyCombatSeamWithoutSpawnBinding()
        {
            LocalDataProvider provider = CreateProjectProvider();
            Assert.IsTrue(provider.InitializeAsync().GetAwaiter().GetResult().Succeeded);
            CompanionRangedSupportCombatResolver resolver = new CompanionRangedSupportCombatResolver(provider);
            Assert.IsTrue(resolver.TryResolve("field_herbalist", 1.0f, out CompanionRangedSupportCombatSetup setup));

            GameObject owner = new GameObject("FieldHerbalistSetupOnly");
            try
            {
                AllyCombat combat = owner.AddComponent<AllyCombat>();
                combat.SetCanonicalRangedSupportInfo(setup);

                Assert.AreEqual(AllyAttackStyle.TargetedProjectile, combat.AttackStyle);
                Assert.AreEqual(8, combat.Damage);
                Assert.AreEqual(1.4f, combat.AttackPeriod);
                Assert.AreEqual(5.0f, combat.AttackRange);
                Assert.AreEqual("field_herbalist", combat.CombatSourceId);
                Assert.AreEqual(4, combat.SecondaryHealAmount);
                Assert.AreEqual(4.0f, combat.SecondaryHealRange);
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void AbilitySchedules_AdvancePrimaryAndSecondaryIndependently()
        {
            CombatAbilitySchedule primary = new CombatAbilitySchedule();
            CombatAbilitySchedule secondary = new CombatAbilitySchedule();
            primary.Configure(1.4f, 0.15f, 0.0f, 0.0f);
            secondary.Configure(6.0f, 0.15f, 0.0f, 0.0f);

            primary.RecordResolution(0.0f, true);
            secondary.RecordResolution(0.0f, false);

            Assert.AreEqual(1.4f, primary.NextDueTime);
            Assert.AreEqual(0.15f, secondary.NextDueTime);
            Assert.IsFalse(primary.IsDue(0.15f));
            Assert.IsTrue(secondary.IsDue(0.15f));
        }

        private static void AssertSetup(
            CompanionRangedSupportCombatSetup setup,
            string sourceId,
            int primaryDamage,
            float primaryPeriod,
            float primaryRange,
            int secondaryHeal,
            float secondaryPeriod,
            float secondaryRange)
        {
            Assert.AreEqual(sourceId, setup.Primary.SourceId);
            Assert.AreEqual(AllyAttackStyle.TargetedProjectile, setup.Primary.AttackStyle);
            Assert.AreEqual(primaryDamage, setup.Primary.Damage);
            Assert.AreEqual(primaryPeriod, setup.Primary.Period);
            Assert.AreEqual(primaryRange, setup.Primary.Range);
            Assert.AreEqual(1, setup.Primary.MaxTargets);
            Assert.AreEqual(0.15f, setup.Primary.NoTargetRetrySeconds);
            Assert.AreEqual(secondaryHeal, setup.SecondaryHealAmount);
            Assert.AreEqual(secondaryPeriod, setup.SecondaryPeriod);
            Assert.AreEqual(secondaryRange, setup.SecondaryRange);
            Assert.AreEqual(1, setup.SecondaryMaxTargets);
            Assert.AreEqual(0.15f, setup.SecondaryNoTargetRetrySeconds);
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
