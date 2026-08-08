using System;
using System.Reflection;
using Lizzo.PV.Gameplay.RunTraits;
using Lizzo.PV.Legion;
using Lizzo.PV.Legion.Party.Roster;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using UnityEngine;

namespace Lizzo.PV.EditorTests
{
    public sealed class PromotionShoutRunModuleTests
    {
        [Test]
        public void UnselectedPromotion_ReturnsNeutralDivisor()
        {
            using RunTraitRunState traits = new RunTraitRunState();
            using RunTraitEffectCoordinator coordinator = new RunTraitEffectCoordinator(traits);

            coordinator.ReportPromotionCommitted(10.0f);

            Assert.That(coordinator.ContainsSelectedTrait(RunTraitIds.PromotionShout), Is.False);
            Assert.That(coordinator.GetCompanionAttackIntervalDivisor(10.0f), Is.EqualTo(1.0f));
        }

        [Test]
        public void SelectedPromotion_ReturnsTwentyPercentAttackSpeedDivisor()
        {
            using RunTraitRunState traits = CreatePromotionShoutTraits();
            using RunTraitEffectCoordinator coordinator = new RunTraitEffectCoordinator(traits);

            coordinator.ReportPromotionCommitted(10.0f);

            Assert.That(coordinator.ContainsSelectedTrait(RunTraitIds.PromotionShout), Is.True);
            Assert.That(coordinator.GetCompanionAttackIntervalDivisor(10.0f), Is.EqualTo(1.20f));
        }

        [Test]
        public void Retrigger_RefreshesExpiryWithoutStacking()
        {
            using RunTraitRunState traits = CreatePromotionShoutTraits();
            using RunTraitEffectCoordinator coordinator = new RunTraitEffectCoordinator(traits);

            coordinator.ReportPromotionCommitted(10.0f);
            coordinator.ReportPromotionCommitted(13.0f);

            Assert.That(coordinator.GetCompanionAttackIntervalDivisor(14.0f), Is.EqualTo(1.20f));
            Assert.That(coordinator.GetCompanionAttackIntervalDivisor(18.0f), Is.EqualTo(1.0f));
        }

        [Test]
        public void Expiry_ReturnsNeutralDivisor()
        {
            using RunTraitRunState traits = CreatePromotionShoutTraits();
            using RunTraitEffectCoordinator coordinator = new RunTraitEffectCoordinator(traits);

            coordinator.ReportPromotionCommitted(10.0f);

            Assert.That(coordinator.GetCompanionAttackIntervalDivisor(15.0f), Is.EqualTo(1.0f));
        }

        [Test]
        public void PartyPromotionSeam_ActivatesExpiresAndResets()
        {
            using var fixture = new ServiceTestFixture();
            var owner = new GameObject("PromotionShoutIntegrationCompanion");
            try
            {
                CompanionRuntime companion = owner.AddComponent<CompanionRuntime>();
                SetHp(companion, 1);
                Assert.That(fixture.Run.RunTraits.TrySelect(RunTraitIds.PromotionShout), Is.True);

                float now = Time.time;
                CommitPromotion(fixture.Run.Party, now);
                Assert.That(ResolveAttackIntervalDivisor(fixture.Run.Party, companion), Is.EqualTo(1.20f));

                CommitPromotion(fixture.Run.Party, now - 5.01f);
                Assert.That(ResolveAttackIntervalDivisor(fixture.Run.Party, companion), Is.EqualTo(1.0f));

                CommitPromotion(fixture.Run.Party, now);
                Assert.That(ResolveAttackIntervalDivisor(fixture.Run.Party, companion), Is.EqualTo(1.20f));
                fixture.Run.Party.ResetRunState();
                Assert.That(ResolveAttackIntervalDivisor(fixture.Run.Party, companion), Is.EqualTo(1.0f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

        static RunTraitRunState CreatePromotionShoutTraits()
        {
            var traits = new RunTraitRunState();
            Assert.That(traits.TrySelect(RunTraitIds.PromotionShout), Is.True);
            return traits;
        }

        static void CommitPromotion(PartyService party, float now)
        {
            InvokeParty(party, "HandlePromotionCommitted", PartyRosterChangeResult.Promote, now);
        }

        static float ResolveAttackIntervalDivisor(PartyService party, CompanionRuntime companion)
        {
            return (float)InvokeParty(party, "ResolveCompanionAttackIntervalDivisor", companion);
        }

        static object InvokeParty(PartyService party, string name, params object[] arguments)
        {
            MethodInfo method = typeof(PartyService).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, name);
            return method.Invoke(party, arguments);
        }

        static void SetHp(CompanionRuntime companion, int hp)
        {
            PropertyInfo property = typeof(CompanionRuntime).GetProperty("Hp", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(property, Is.Not.Null);
            property.GetSetMethod(true).Invoke(companion, new object[] { hp });
        }

    }
}
