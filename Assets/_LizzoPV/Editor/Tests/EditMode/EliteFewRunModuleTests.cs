using Lizzo.PV.Gameplay.RunTraits;
using NUnit.Framework;

namespace Lizzo.PV.EditorTests
{
    public sealed class EliteFewRunModuleTests
    {
        [Test]
        public void UnselectedTrait_ReturnsNeutralCommanderAttackIntervalMultiplier()
        {
            using RunTraitRunState traits = new RunTraitRunState();
            using RunTraitEffectCoordinator coordinator = new RunTraitEffectCoordinator(traits);

            Assert.That(coordinator.GetCommanderAttackIntervalMultiplier(0, 7), Is.EqualTo(1.00f));
        }

        [TestCase(0, 0.80f)]
        [TestCase(1, 0.80f)]
        [TestCase(2, 0.80f)]
        [TestCase(3, 0.80f)]
        [TestCase(4, 0.85f)]
        [TestCase(5, 0.90f)]
        [TestCase(6, 0.95f)]
        [TestCase(7, 1.00f)]
        public void SelectedTrait_ReturnsExpectedMultiplierForActiveSlotCount(int activeSlotCount, float expectedMultiplier)
        {
            using RunTraitRunState traits = new RunTraitRunState();
            Assert.That(traits.TrySelect(RunTraitIds.EliteFew), Is.True);
            using RunTraitEffectCoordinator coordinator = new RunTraitEffectCoordinator(traits);

            Assert.That(
                coordinator.GetCommanderAttackIntervalMultiplier(activeSlotCount, 7),
                Is.EqualTo(expectedMultiplier));
        }
    }
}
