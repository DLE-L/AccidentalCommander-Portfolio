using System;
using System.Reflection;
using Lizzo.PV.Data;
using Lizzo.PV.P0.Cards;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class RunRuntimeUpdateCoordinatorTests
    {
        [Test]
        public void Tick_ResultLockResetsOnceAndRearmsAfterLiveTick()
        {
            using ServiceTestFixture fixture = new ServiceTestFixture();
            fixture.Run.State.Reset(fixture.Data.GetLevelExp(1));
            fixture.Run.State.MarkLoaded();
            PassiveData passive = CreatePassive();
            object coordinator = CreateCoordinator(fixture.Run);

            Assert.That(fixture.Run.PassiveRoster.TryApply(passive, out _), Is.True);
            Tick(coordinator, isResultGameplayLocked: true);
            Assert.That(fixture.Run.PassiveRoster.ActiveSlotCount, Is.Zero);

            Assert.That(fixture.Run.PassiveRoster.TryApply(passive, out _), Is.True);
            Tick(coordinator, isResultGameplayLocked: true);
            Assert.That(fixture.Run.PassiveRoster.ActiveSlotCount, Is.EqualTo(1));

            Tick(coordinator, isResultGameplayLocked: false);
            Assert.That(fixture.Run.PassiveRoster.ActiveSlotCount, Is.EqualTo(1));

            Tick(coordinator, isResultGameplayLocked: true);
            Assert.That(fixture.Run.PassiveRoster.ActiveSlotCount, Is.Zero);
        }

        private static object CreateCoordinator(RunServices services)
        {
            Type type = typeof(RunServices).Assembly.GetType(
                "Lizzo.PV.Gameplay.Run.RunRuntimeUpdateCoordinator");
            Assert.IsNotNull(type, "Missing run runtime update coordinator test type.");
            ConstructorInfo constructor = type.GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[] { typeof(RunServices) },
                null);
            Assert.IsNotNull(constructor, "Missing run runtime update coordinator constructor.");
            return constructor.Invoke(new object[] { services });
        }

        private static void Tick(object coordinator, bool isResultGameplayLocked)
        {
            MethodInfo method = coordinator.GetType().GetMethod(
                "Tick",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, "Missing run runtime update tick method.");
            method.Invoke(
                coordinator,
                new object[]
                {
                    0.016f,
                    10.0f,
                    10,
                    false,
                    false,
                    isResultGameplayLocked,
                });
        }

        private static PassiveData CreatePassive()
        {
            return new PassiveData
            {
                Id = "passive_runtime_loop_test",
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
