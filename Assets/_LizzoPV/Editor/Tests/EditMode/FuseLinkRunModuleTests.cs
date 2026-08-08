using Lizzo.PV.Gameplay.RunTraits;
using NUnit.Framework;

namespace Lizzo.PV.EditorTests
{
    public sealed class FuseLinkRunModuleTests
    {
        [Test]
        public void FirstPrimaryHit_SetsFuseWithoutSecondary()
        {
            using FuseLinkRunModule module = new FuseLinkRunModule(3.0f, 0.60f);

            bool resolved = module.TryProcess(new FuseLinkPrimaryHit(11L, 10, true, false), 4.0f, out _);

            Assert.That(resolved, Is.False);
        }

        [Test]
        public void LaterPrimaryHit_ConsumesFuseAndRoundsConfiguredDamage()
        {
            using FuseLinkRunModule module = new FuseLinkRunModule(3.0f, 0.60f);
            module.TryProcess(new FuseLinkPrimaryHit(11L, 10, true, false), 4.0f, out _);

            bool resolved = module.TryProcess(new FuseLinkPrimaryHit(11L, 11, true, false), 5.0f, out FuseLinkSecondaryPlan plan);

            Assert.That(resolved, Is.True);
            Assert.That(plan.TriggerSpawnSequence, Is.EqualTo(11L));
            Assert.That(plan.Damage, Is.EqualTo(7));
        }

        [Test]
        public void ExpiredOrSecondaryHit_DoesNotConsumeOrCreateAChain()
        {
            using FuseLinkRunModule module = new FuseLinkRunModule(3.0f, 0.60f);
            module.TryProcess(new FuseLinkPrimaryHit(11L, 10, true, false), 4.0f, out _);

            Assert.That(module.TryProcess(new FuseLinkPrimaryHit(11L, 10, true, true), 5.0f, out _), Is.False);
            Assert.That(module.TryProcess(new FuseLinkPrimaryHit(11L, 10, true, false), 7.1f, out _), Is.False);
        }
    }
}
