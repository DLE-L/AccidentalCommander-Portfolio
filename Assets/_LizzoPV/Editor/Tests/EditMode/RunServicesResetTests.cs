using System.Collections.Generic;
using System.Reflection;
using Lizzo.PV.Data;
using Lizzo.PV.Legion.Combat;
using Lizzo.PV.P0.Cards;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class RunServicesResetTests
    {
        [Test]
        public void ResetRunState_ClearsOwnedProgressAndRestartsCastIdentity()
        {
            using ServiceTestFixture fixture = new ServiceTestFixture();
            PassiveData passive = CreatePassive();
            List<long> castIds = new List<long>();
            fixture.Run.CanonicalCompanionCasts.Completed += cast => castIds.Add(cast.CastId);
            CanonicalCompanionCastIdentity identity = new CanonicalCompanionCastIdentity(
                1,
                "slot_00",
                "melee_guard",
                "melee");

            Assert.That(fixture.Run.PassiveRoster.TryApply(passive, out _), Is.True);
            Assert.That(fixture.Run.CanonicalCompanionCasts.TryEmit(
                identity,
                CanonicalCompanionActionKind.BasicAttack), Is.True);
            Assert.That(fixture.Run.CanonicalCompanionCasts.TryEmit(
                identity,
                CanonicalCompanionActionKind.ActiveSkill), Is.True);

            ResetRunState(fixture.Run);

            Assert.That(fixture.Run.PassiveRoster.ActiveSlotCount, Is.Zero);
            Assert.That(fixture.Run.CanonicalCompanionCasts.TryEmit(
                identity,
                CanonicalCompanionActionKind.BasicAttack), Is.True);
            Assert.That(castIds, Is.EqualTo(new long[] { 1L, 2L, 1L }));
        }

        private static void ResetRunState(RunServices services)
        {
            MethodInfo method = typeof(RunServices).GetMethod(
                "ResetRunState",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, "Missing RunServices reset contract.");
            method.Invoke(services, null);
        }

        private static PassiveData CreatePassive()
        {
            return new PassiveData
            {
                Id = "passive_run_services_reset_test",
                Category = "general",
                EligibleTarget = "commander",
                EffectId = "effect",
                ValueType = "flat_damage_add",
                Level1Value = 1.0f,
                Level2Value = 2.0f,
                Level3Value = 3.0f,
                StackRule = "replace",
                TitleKo = "테스트",
                TitleEn = "Test",
                DescriptionTemplateKo = "+{value}",
                OfferWeightRule = "base x1.0",
                Prohibition = "none",
            };
        }
    }
}
