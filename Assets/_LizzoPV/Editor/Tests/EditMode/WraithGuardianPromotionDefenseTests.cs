using Lizzo.PV.Data;
using Lizzo.PV.Legion;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class WraithGuardianPromotionDefenseTests
    {
        [Test]
        public void PromotedGuardian_UsesOnlyApprovedGeometryAndPersonalDefenseSpecializations()
        {
            CompanionWraithMeleeDefenseSetup baseSetup = ResolveBaseSetup();
            CompanionWraithMeleeDefenseSetup promoted = baseSetup
                .WithPromotedWraithGuardianGeometry()
                .WithPromotedWraithGuardianDefense();

            Assert.AreEqual(14, baseSetup.Melee.Damage);
            Assert.AreEqual(1.4f, baseSetup.Melee.Period);
            Assert.AreEqual(1.2f, baseSetup.Melee.Range);
            Assert.AreEqual(60.0f, baseSetup.Melee.Angle);
            Assert.AreEqual(3, baseSetup.Melee.MaxTargets);
            Assert.AreEqual(0.15f, baseSetup.Melee.NoTargetRetrySeconds);
            Assert.AreEqual(baseSetup.Melee.AttackStyle, promoted.Melee.AttackStyle);
            Assert.AreEqual(14, promoted.Melee.Damage);
            Assert.AreEqual(1.4f, promoted.Melee.Period);
            Assert.AreEqual(1.4f, promoted.Melee.Range);
            Assert.AreEqual(75.0f, promoted.Melee.Angle);
            Assert.AreEqual(3, promoted.Melee.MaxTargets);
            Assert.AreEqual(0.15f, promoted.Melee.NoTargetRetrySeconds);
            Assert.AreEqual(0.60f, baseSetup.PersonalDefense.IncomingDamageMultiplier);
            Assert.AreEqual(1.2f, baseSetup.PersonalDefense.Duration);
            Assert.AreEqual(5.0f, baseSetup.PersonalDefense.Period);
            Assert.AreEqual(0.50f, promoted.PersonalDefense.IncomingDamageMultiplier);
            Assert.AreEqual(1.5f, promoted.PersonalDefense.Duration);
            Assert.AreEqual(5.0f, promoted.PersonalDefense.Period);
        }

        [Test]
        public void PromotedGuardian_AppliesGrowthExactlyOnceWithoutChangingSpecializedGeometry()
        {
            CompanionWraithMeleeDefenseSetup promoted = ResolveBaseSetup()
                .WithPromotedWraithGuardianGeometry()
                .WithPromotedWraithGuardianDefense();
            CompanionMeleeCombatSetup scaled = promoted.Melee.WithGrowthScale(new CompanionGrowthScale(1.70f, 2.20f, 1.05f, 3));

            Assert.AreEqual(24, scaled.Damage);
            Assert.That(scaled.Period, Is.EqualTo(1.47f).Within(0.0001f));
            Assert.AreEqual(1.4f, scaled.Range);
            Assert.AreEqual(75.0f, scaled.Angle);
            Assert.AreEqual(3, scaled.MaxTargets);
            Assert.AreEqual(0.50f, promoted.PersonalDefense.IncomingDamageMultiplier);
        }

        [Test]
        public void PromotedGuardian_MitigationActivatesExpiresOnCadenceAndResetsForOwnerDown()
        {
            PersonalDamageMitigationState state = new PersonalDamageMitigationState();
            state.Configure(ResolveBaseSetup().WithPromotedWraithGuardianDefense().PersonalDefense, 0.0f);

            Assert.IsTrue(state.Advance(0.0f));
            Assert.IsTrue(state.IsActive);
            Assert.AreEqual(5, state.ApplyToSelf(10));
            Assert.IsFalse(state.Advance(1.5f));
            Assert.IsFalse(state.IsActive);
            Assert.AreEqual(10, state.ApplyToSelf(10));
            Assert.IsTrue(state.Advance(5.0f));
            state.ResetForOwnerDown(5.2f);
            Assert.IsFalse(state.IsActive);
            Assert.IsTrue(state.Advance(5.2f));
            Assert.AreEqual(5, state.ApplyToSelf(10));
        }

        private static CompanionWraithMeleeDefenseSetup ResolveBaseSetup()
        {
            TestAssetService assets = new TestAssetService();
            TextAsset gameData = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_LizzoPV/Data/Runtime/GameData.xml");
            Assert.IsNotNull(gameData);
            assets.Register("PlayerData.xml", gameData);
            LocalDataProvider provider = new LocalDataProvider(assets);
            Assert.IsTrue(provider.InitializeAsync().GetAwaiter().GetResult().Succeeded);

            CompanionMeleeCombatResolver resolver = new CompanionMeleeCombatResolver(provider);
            Assert.IsTrue(resolver.TryResolveWraithMeleeDefense(1.0f, out CompanionWraithMeleeDefenseSetup setup));
            return setup;
        }
    }
}
