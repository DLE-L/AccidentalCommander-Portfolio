using Lizzo.PV.Data;
using Lizzo.PV.Legion;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class CompanionOwnedProxyCombatTests
    {
        [Test]
        public void Resolver_MapsFalconAssistToCanonicalProxySetup()
        {
            LocalDataProvider provider = CreateProjectProvider();
            Assert.IsTrue(provider.InitializeAsync().GetAwaiter().GetResult().Succeeded);
            CompanionOwnedProxyCombatResolver resolver = new CompanionOwnedProxyCombatResolver(provider);

            Assert.IsTrue(resolver.TryResolve("falcon_archer", out CompanionOwnedProxyCombatSetup setup));
            Assert.AreEqual(6, setup.Damage);
            Assert.AreEqual(5.5f, setup.Range);
            Assert.AreEqual(1, setup.MaxTargets);
            Assert.AreEqual(4, setup.TriggerCount);
            Assert.IsFalse(resolver.TryResolve("archer", out _));
            Assert.IsFalse(resolver.TryResolve("cleric", out _));
        }

        [Test]
        public void SuccessfulActionCounter_TriggersEveryFourthSuccess_AndResets()
        {
            SuccessfulActionCounter counter = new SuccessfulActionCounter();
            counter.Configure(4);

            Assert.IsFalse(counter.RecordSuccess());
            Assert.IsFalse(counter.RecordSuccess());
            Assert.IsFalse(counter.RecordSuccess());
            Assert.AreEqual(3, counter.CurrentCount);
            Assert.IsTrue(counter.RecordSuccess());
            Assert.AreEqual(0, counter.CurrentCount);

            Assert.IsFalse(counter.RecordSuccess());
            counter.Reset();
            Assert.AreEqual(0, counter.CurrentCount);
            Assert.IsFalse(counter.RecordSuccess());
        }

        [Test]
        public void CanonicalFalconSetup_EnablesOwnedProxyWithoutChangingPrimaryBridge()
        {
            LocalDataProvider provider = CreateProjectProvider();
            Assert.IsTrue(provider.InitializeAsync().GetAwaiter().GetResult().Succeeded);
            CompanionProjectileCombatResolver projectileResolver = new CompanionProjectileCombatResolver(provider);
            CompanionOwnedProxyCombatResolver proxyResolver = new CompanionOwnedProxyCombatResolver(provider);
            Assert.IsTrue(projectileResolver.TryResolve("falcon_archer", 1.0f, out CompanionProjectileCombatSetup projectile));
            Assert.IsTrue(proxyResolver.TryResolve("falcon_archer", out CompanionOwnedProxyCombatSetup proxy));

            GameObject owner = new GameObject("FalconCanonicalProxyBridge");
            try
            {
                AllyCombat combat = owner.AddComponent<AllyCombat>();
                combat.SetCanonicalProjectileWithProxyInfo(projectile, proxy);

                Assert.IsTrue(combat.HasOwnedProxyAssist);
                Assert.AreEqual(AllyAttackStyle.TargetedProjectile, combat.AttackStyle);
                Assert.AreEqual("falcon_archer", combat.CombatSourceId);
                Assert.AreEqual(9, combat.Damage);
                Assert.AreEqual(5.5f, combat.AttackRange);
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