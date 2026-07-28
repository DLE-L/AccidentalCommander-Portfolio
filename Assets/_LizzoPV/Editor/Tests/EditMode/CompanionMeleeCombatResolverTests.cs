using Lizzo.PV.Data;
using Lizzo.PV.Legion;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class CompanionMeleeCombatResolverTests
    {
        [Test]
        public void Resolver_MapsShieldAndSwordProfilesToTypedForwardSetups()
        {
            LocalDataProvider provider = CreateProjectProvider();
            Assert.IsTrue(provider.InitializeAsync().GetAwaiter().GetResult().Succeeded);
            CompanionMeleeCombatResolver resolver = new CompanionMeleeCombatResolver(provider);

            Assert.IsTrue(resolver.TryResolve("shield_guard", 1.0f, out CompanionMeleeCombatSetup shield));
            Assert.AreEqual(AllyAttackStyle.ForwardPush, shield.AttackStyle);
            Assert.AreEqual(6, shield.Damage);
            Assert.AreEqual(1.4f, shield.Period);
            Assert.AreEqual(1.2f, shield.Range);
            Assert.AreEqual(60.0f, shield.Angle);
            Assert.AreEqual(0.5f, shield.Knockback);
            Assert.AreEqual(3, shield.MaxTargets);
            Assert.AreEqual(0.15f, shield.NoTargetRetrySeconds);

            Assert.IsTrue(resolver.TryResolve("sword_soldier", 1.0f, out CompanionMeleeCombatSetup sword));
            Assert.AreEqual(AllyAttackStyle.ForwardSlash, sword.AttackStyle);
            Assert.AreEqual(12, sword.Damage);
            Assert.AreEqual(1.0f, sword.Period);
            Assert.AreEqual(1.1f, sword.Range);
            Assert.AreEqual(60.0f, sword.Angle);
            Assert.AreEqual(0.0f, sword.Knockback);
            Assert.AreEqual(3, sword.MaxTargets);
            Assert.AreEqual(0.15f, sword.NoTargetRetrySeconds);

            Assert.IsFalse(resolver.TryResolve("cleric", 1.0f, out _));
            Assert.IsFalse(resolver.TryResolve("archer", 1.0f, out _));
        }

        [Test]
        public void CanonicalMeleeSetup_LimitsForwardTargetsAndUsesProfileRetry()
        {
            LocalDataProvider provider = CreateProjectProvider();
            Assert.IsTrue(provider.InitializeAsync().GetAwaiter().GetResult().Succeeded);
            CompanionMeleeCombatResolver resolver = new CompanionMeleeCombatResolver(provider);
            Assert.IsTrue(resolver.TryResolve("shield_guard", 1.0f, out CompanionMeleeCombatSetup shield));

            GameObject owner = new GameObject("CanonicalMeleeCombat");
            try
            {
                AllyCombat combat = owner.AddComponent<AllyCombat>();
                combat.SetCanonicalMeleeInfo(shield);

                Assert.AreEqual(3, combat.MaxForwardTargetCount);
                Assert.AreEqual(0.15f, combat.NoTargetRetrySeconds);
                Assert.IsTrue(combat.CanAcceptForwardTarget(2));
                Assert.IsFalse(combat.CanAcceptForwardTarget(3));
                Assert.AreEqual(0.15f, combat.ResolveNextAttackDelay(false));
                Assert.AreEqual(1.4f, combat.ResolveNextAttackDelay(true));
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
