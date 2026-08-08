using System;
using System.Collections.Generic;
using System.Reflection;
using Lizzo.PV.Gameplay.RunTraits;
using NUnit.Framework;

namespace Lizzo.PV.EditorTests
{
    public sealed class EmergencyRallyRunModuleTests
    {
        [Test]
        public void SelectedTrait_StrictLowHpSnapshotsRecipientsAndAppliesSpeedForFourSeconds()
        {
            using RunTraitRunState traits = CreateTraits();
            using RunTraitEffectCoordinator coordinator = new RunTraitEffectCoordinator(traits);
            string[] recipients = { "slot_sword", "slot_cleric" };

            Assert.That(TryActivate(coordinator, 35, 100, recipients, 10.0f), Is.False);
            Assert.That(TryActivate(coordinator, 34, 100, recipients, 10.0f), Is.True);
            Assert.That(GetMoveSpeedMultiplier(coordinator, "slot_sword", 13.99f), Is.EqualTo(1.25f));
            Assert.That(GetMoveSpeedMultiplier(coordinator, "slot_cleric", 14.0f), Is.EqualTo(1.0f));
            Assert.That(GetMoveSpeedMultiplier(coordinator, "slot_recruited_later", 11.0f), Is.EqualTo(1.0f));
        }

        [Test]
        public void ActiveRecipient_AbsorbsPostMitigationDamageWithoutRefillOrRetrigger()
        {
            using RunTraitRunState traits = CreateTraits();
            using RunTraitEffectCoordinator coordinator = new RunTraitEffectCoordinator(traits);
            string[] recipients = { "slot_sword" };

            Assert.That(TryActivate(coordinator, 34, 100, recipients, 10.0f), Is.True);
            Assert.That(ResolvePostMitigationDamage(coordinator, "slot_sword", 12, 11.0f, out int firstAbsorbed), Is.EqualTo(0));
            Assert.That(firstAbsorbed, Is.EqualTo(12));
            Assert.That(ResolvePostMitigationDamage(coordinator, "slot_sword", 12, 11.0f, out int secondAbsorbed), Is.EqualTo(4));
            Assert.That(secondAbsorbed, Is.EqualTo(8));
            Assert.That(GetMoveSpeedMultiplier(coordinator, "slot_sword", 12.0f), Is.EqualTo(1.25f));

            Assert.That(TryActivate(coordinator, 1, 100, recipients, 12.0f), Is.False);
            Assert.That(ResolvePostMitigationDamage(coordinator, "slot_sword", 2, 12.0f, out int thirdAbsorbed), Is.EqualTo(2));
            Assert.That(thirdAbsorbed, Is.EqualTo(0));
        }

        [Test]
        public void RecipientDownResetAndDispose_RemoveTheOriginalSlotPool()
        {
            using RunTraitRunState traits = CreateTraits();
            using RunTraitEffectCoordinator coordinator = new RunTraitEffectCoordinator(traits);
            string[] recipients = { "slot_sword" };

            Assert.That(TryActivate(coordinator, 34, 100, recipients, 10.0f), Is.True);
            NotifyRecipientDown(coordinator, "slot_sword");
            Assert.That(ResolvePostMitigationDamage(coordinator, "slot_sword", 5, 11.0f, out int afterDownAbsorbed), Is.EqualTo(5));
            Assert.That(afterDownAbsorbed, Is.EqualTo(0));

            coordinator.ResetRunState();
            Assert.That(GetMoveSpeedMultiplier(coordinator, "slot_sword", 11.0f), Is.EqualTo(1.0f));
        }

        static RunTraitRunState CreateTraits()
        {
            var traits = new RunTraitRunState();
            Assert.That(traits.TrySelect(RunTraitIds.EmergencyRally), Is.True);
            return traits;
        }

        static bool TryActivate(RunTraitEffectCoordinator coordinator, int currentHp, int maxHp, IReadOnlyList<string> recipientSlots, float now)
        {
            return (bool)Invoke(coordinator, "TryActivateEmergencyRally", currentHp, maxHp, recipientSlots, now);
        }

        static float GetMoveSpeedMultiplier(RunTraitEffectCoordinator coordinator, string rosterSlotId, float now)
        {
            return (float)Invoke(coordinator, "GetEmergencyRallyMoveSpeedMultiplier", rosterSlotId, now);
        }

        static int ResolvePostMitigationDamage(RunTraitEffectCoordinator coordinator, string rosterSlotId, int damage, float now, out int absorbedDamage)
        {
            object[] arguments = { rosterSlotId, damage, now, 0 };
            int residual = (int)Invoke(coordinator, "ResolveEmergencyRallyPostMitigationDamage", arguments);
            absorbedDamage = (int)arguments[3];
            return residual;
        }

        static void NotifyRecipientDown(RunTraitEffectCoordinator coordinator, string rosterSlotId)
        {
            Invoke(coordinator, "NotifyEmergencyRallyRecipientDown", rosterSlotId);
        }

        static object Invoke(RunTraitEffectCoordinator coordinator, string methodName, params object[] arguments)
        {
            MethodInfo method = typeof(RunTraitEffectCoordinator).GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
            Assert.That(method, Is.Not.Null, $"Missing approved coordinator seam: {methodName}");
            return method.Invoke(coordinator, arguments);
        }
    }
}
