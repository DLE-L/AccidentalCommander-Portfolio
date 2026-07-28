using Lizzo.PV.Data;
using Lizzo.PV.Legion;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class CompanionPersistentFieldCombatResolverTests
    {
        [Test]
        public void Resolver_MapsFireMageToCanonicalPersistentFieldSetup()
        {
            LocalDataProvider provider = CreateProjectProvider();
            Assert.IsTrue(provider.InitializeAsync().GetAwaiter().GetResult().Succeeded);
            CompanionPersistentFieldCombatResolver resolver = new CompanionPersistentFieldCombatResolver(provider);

            Assert.IsTrue(resolver.TryResolve("fire_mage", 1.0f, out CompanionPersistentFieldCombatSetup setup));
            Assert.AreEqual("fire_mage", setup.SourceId);
            Assert.AreEqual("dot_fire_field_v1", setup.EffectId);
            Assert.AreEqual(5, setup.Damage);
            Assert.AreEqual(3.2f, setup.Period);
            Assert.AreEqual(4.8f, setup.Range);
            Assert.AreEqual(1.6f, setup.Radius);
            Assert.AreEqual(1.0f, setup.TickInterval);
            Assert.AreEqual(3.0f, setup.Duration);
            Assert.AreEqual(8, setup.MaxTargets);
            Assert.AreEqual(2, setup.MaxActiveFields);
            Assert.AreEqual(0.15f, setup.NoTargetRetrySeconds);
            Assert.IsFalse(resolver.TryResolve("bombardier", 1.0f, out _));
        }

        [Test]
        public void FireMageSetup_AppliesThroughInactiveAllyCombatSeam()
        {
            LocalDataProvider provider = CreateProjectProvider();
            Assert.IsTrue(provider.InitializeAsync().GetAwaiter().GetResult().Succeeded);
            CompanionPersistentFieldCombatResolver resolver = new CompanionPersistentFieldCombatResolver(provider);
            Assert.IsTrue(resolver.TryResolve("fire_mage", 1.0f, out CompanionPersistentFieldCombatSetup setup));

            GameObject owner = new GameObject("FireMageSetupOnly");
            try
            {
                AllyCombat combat = owner.AddComponent<AllyCombat>();
                combat.SetCanonicalPersistentFieldInfo(setup);

                Assert.AreEqual(AllyAttackStyle.TargetedField, combat.AttackStyle);
                Assert.AreEqual(5, combat.Damage);
                Assert.AreEqual(3.2f, combat.AttackPeriod);
                Assert.AreEqual(4.8f, combat.AttackRange);
                Assert.AreEqual("fire_mage", combat.CombatSourceId);
                Assert.AreEqual("dot_fire_field_v1", combat.PersistentFieldSetup.EffectId);
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
