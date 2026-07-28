using Lizzo.PV.Data;
using Lizzo.PV.Legion;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class CompanionProjectileCombatResolverTests
    {
        [Test]
        public void Resolver_MapsFalconProfileToCanonicalNearestProjectileSetup()
        {
            LocalDataProvider provider = CreateProjectProvider();
            Assert.IsTrue(provider.InitializeAsync().GetAwaiter().GetResult().Succeeded);
            CompanionProjectileCombatResolver resolver = new CompanionProjectileCombatResolver(provider);

            Assert.IsTrue(resolver.TryResolve("falcon_archer", 1.0f, out CompanionProjectileCombatSetup setup));
            Assert.AreEqual("falcon_archer", setup.SourceId);
            Assert.AreEqual(AllyAttackStyle.TargetedProjectile, setup.AttackStyle);
            Assert.AreEqual(9, setup.Damage);
            Assert.AreEqual(0.9f, setup.Period);
            Assert.AreEqual(5.5f, setup.Range);
            Assert.AreEqual(1, setup.MaxTargets);
            Assert.AreEqual(0.15f, setup.NoTargetRetrySeconds);

            Assert.IsFalse(resolver.TryResolve("archer", 1.0f, out _));
            Assert.IsFalse(resolver.TryResolve("cleric", 1.0f, out _));
        }

        [Test]
        public void CanonicalProjectileSetup_UsesFalconSourceIdentityWithoutChangingRuntimeBridge()
        {
            LocalDataProvider provider = CreateProjectProvider();
            Assert.IsTrue(provider.InitializeAsync().GetAwaiter().GetResult().Succeeded);
            CompanionProjectileCombatResolver resolver = new CompanionProjectileCombatResolver(provider);
            Assert.IsTrue(resolver.TryResolve("falcon_archer", 1.0f, out CompanionProjectileCombatSetup setup));

            GameObject owner = new GameObject("LegacyArcherRuntimeBridge");
            try
            {
                AllyCombat combat = owner.AddComponent<AllyCombat>();
                combat.SetCanonicalProjectileInfo(setup);

                Assert.AreEqual(AllyAttackStyle.TargetedProjectile, combat.AttackStyle);
                Assert.AreEqual(9, combat.Damage);
                Assert.AreEqual(0.9f, combat.AttackPeriod);
                Assert.AreEqual(5.5f, combat.AttackRange);
                Assert.AreEqual(1, combat.MaxProjectileTargetCount);
                Assert.AreEqual(0.15f, combat.NoTargetRetrySeconds);
                Assert.AreEqual("falcon_archer", combat.CombatSourceId);
                Assert.AreEqual(0.15f, combat.ResolveNextAttackDelay(false));
                Assert.AreEqual(0.9f, combat.ResolveNextAttackDelay(true));
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
