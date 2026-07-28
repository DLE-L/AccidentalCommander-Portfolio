using Lizzo.PV.Data;
using Lizzo.PV.Legion;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class FireSagePromotionPersistentFieldTests
    {
        [Test]
        public void PromotedSetup_ChangesOnlyRadiusAndDuration_WithoutCompoundingGrowth()
        {
            LocalDataProvider provider = CreateProjectProvider();
            Assert.IsTrue(provider.InitializeAsync().GetAwaiter().GetResult().Succeeded);
            CompanionPersistentFieldCombatResolver resolver = new CompanionPersistentFieldCombatResolver(provider);
            Assert.IsTrue(resolver.TryResolve("fire_mage", 1.0f, out CompanionPersistentFieldCombatSetup baseSetup));

            CompanionPersistentFieldCombatSetup promoted = baseSetup.WithPromotedFireSageField();
            CompanionPersistentFieldCombatSetup promotedAgain = promoted.WithPromotedFireSageField();

            Assert.AreEqual(1.6f, baseSetup.Radius);
            Assert.AreEqual(3.0f, baseSetup.Duration);
            Assert.AreEqual(1.8f, promoted.Radius);
            Assert.AreEqual(4.0f, promoted.Duration);
            Assert.AreEqual(baseSetup.Damage, promoted.Damage);
            Assert.AreEqual(baseSetup.Period, promoted.Period);
            Assert.AreEqual(8, promoted.MaxTargets);
            Assert.AreEqual(1.0f, promoted.TickInterval);
            Assert.AreEqual(2, promoted.MaxActiveFields);
            Assert.AreEqual(promoted.Radius, promotedAgain.Radius);
            Assert.AreEqual(promoted.Duration, promotedAgain.Duration);
        }

        [Test]
        public void PersistentFieldGrowth_AppliesEffectAndCadenceExactlyOnce()
        {
            LocalDataProvider provider = CreateProjectProvider();
            Assert.IsTrue(provider.InitializeAsync().GetAwaiter().GetResult().Succeeded);
            CompanionPersistentFieldCombatResolver resolver = new CompanionPersistentFieldCombatResolver(provider);
            Assert.IsTrue(resolver.TryResolve("fire_mage", 1.0f, out CompanionPersistentFieldCombatSetup setup));

            GameObject owner = new GameObject("FireSageGrowthOnly");
            try
            {
                AllyCombat combat = owner.AddComponent<AllyCombat>();
                combat.SetCanonicalPersistentFieldInfo(setup.WithPromotedFireSageField());
                combat.ApplyGrowthScale(new CompanionGrowthScale(1.60f, 2.10f, 1.05f, 3));

                Assert.AreEqual(8, combat.PersistentFieldSetup.Damage);
                Assert.AreEqual(3.36f, combat.PersistentFieldSetup.Period, 0.0001f);
                Assert.AreEqual(1.8f, combat.PersistentFieldSetup.Radius);
                Assert.AreEqual(4.0f, combat.PersistentFieldSetup.Duration);
                Assert.AreEqual(8, combat.Damage);
                Assert.AreEqual(3.36f, combat.AttackPeriod, 0.0001f);
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
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
