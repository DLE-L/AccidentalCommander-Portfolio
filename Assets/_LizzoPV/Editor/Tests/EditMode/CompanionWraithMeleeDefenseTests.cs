using Lizzo.PV.Data;
using Lizzo.PV.Legion;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class CompanionWraithMeleeDefenseTests
    {
        [Test]
        public void Resolver_MapsWraithPrimaryAndPersonalDefenseToTypedSetup()
        {
            LocalDataProvider provider = CreateProjectProvider();
            Assert.IsTrue(provider.InitializeAsync().GetAwaiter().GetResult().Succeeded);
            CompanionMeleeCombatResolver resolver = new CompanionMeleeCombatResolver(provider);

            Assert.IsTrue(resolver.TryResolveWraithMeleeDefense(1.0f, out CompanionWraithMeleeDefenseSetup setup));
            Assert.AreEqual(AllyAttackStyle.ForwardSlash, setup.Melee.AttackStyle);
            Assert.AreEqual(14, setup.Melee.Damage);
            Assert.AreEqual(1.4f, setup.Melee.Period);
            Assert.AreEqual(1.2f, setup.Melee.Range);
            Assert.AreEqual(60.0f, setup.Melee.Angle);
            Assert.AreEqual(3, setup.Melee.MaxTargets);
            Assert.AreEqual(0.15f, setup.Melee.NoTargetRetrySeconds);
            Assert.AreEqual(0.60f, setup.PersonalDefense.IncomingDamageMultiplier);
            Assert.AreEqual(5.0f, setup.PersonalDefense.Period);
            Assert.AreEqual(1.2f, setup.PersonalDefense.Duration);
        }

        [Test]
        public void PersonalMitigation_ActivatesExpiresOnCadenceAndResetsOnOwnerDown()
        {
            PersonalDamageMitigationState state = new PersonalDamageMitigationState();
            state.Configure(new PersonalDamageMitigationSetup(0.60f, 5.0f, 1.2f), 0.0f);

            Assert.IsTrue(state.Advance(0.0f));
            Assert.IsTrue(state.IsActive);
            Assert.AreEqual(6, state.ApplyToSelf(10));
            Assert.IsFalse(state.Advance(1.2f));
            Assert.IsFalse(state.IsActive);
            Assert.AreEqual(10, state.ApplyToSelf(10));

            Assert.IsTrue(state.Advance(5.0f));
            Assert.IsTrue(state.IsActive);
            state.ResetForOwnerDown(6.0f);
            Assert.IsFalse(state.IsActive);
            Assert.IsTrue(state.Advance(6.0f));
            Assert.AreEqual(6, state.ApplyToSelf(10));
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
