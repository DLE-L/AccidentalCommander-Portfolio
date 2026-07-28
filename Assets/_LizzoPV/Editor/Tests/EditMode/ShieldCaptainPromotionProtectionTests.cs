using Lizzo.PV.Legion;
using NUnit.Framework;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class ShieldCaptainPromotionProtectionTests
    {
        [Test]
        public void PromotedShieldSetup_UsesAuthoritativePushGeometryWithoutChangingTargetCapOrRetry()
        {
            CompanionMeleeCombatSetup baseSetup = new CompanionMeleeCombatSetup(
                AllyAttackStyle.ForwardPush,
                12,
                1.14f,
                1.2f,
                60.0f,
                0.5f,
                3,
                0.15f);

            CompanionMeleeCombatSetup promoted = baseSetup.WithPromotedShieldCaptainGeometry();

            Assert.AreEqual(AllyAttackStyle.ForwardPush, promoted.AttackStyle);
            Assert.AreEqual(1.8f, promoted.Range);
            Assert.AreEqual(90.0f, promoted.Angle);
            Assert.AreEqual(0.9f, promoted.Knockback);
            Assert.AreEqual(3, promoted.MaxTargets);
            Assert.AreEqual(0.15f, promoted.NoTargetRetrySeconds);
        }

        [Test]
        public void ProtectionWindow_ActivatesOnceAndAppliesOnlyDuringItsDuration()
        {
            CompanionProtectionWindow window = new CompanionProtectionWindow(
                new CompanionProtectionWindowSetup("shield_captain_promotion_protection", 0.90f, 1.5f));

            Assert.AreEqual(10, window.ApplyToCompanionDamage(10, 0.0f));
            Assert.IsTrue(window.TryActivateOnce(2.0f));
            Assert.IsTrue(window.IsActive(3.49f));
            Assert.AreEqual(9, window.ApplyToCompanionDamage(10, 3.49f));
            Assert.IsFalse(window.IsActive(3.5f));
            Assert.AreEqual(10, window.ApplyToCompanionDamage(10, 3.5f));
            Assert.IsFalse(window.TryActivateOnce(4.0f));
        }

        [Test]
        public void ProtectionWindow_ResetAllowsOnlyTheNextRunActivationAndKeepsSourceDistinct()
        {
            CompanionProtectionWindow window = new CompanionProtectionWindow(
                new CompanionProtectionWindowSetup("shield_captain_promotion_protection", 0.90f, 1.5f));

            Assert.IsTrue(window.TryActivateOnce(0.0f));
            window.Reset();

            Assert.IsFalse(window.IsActive(0.0f));
            Assert.IsTrue(window.TryActivateOnce(0.0f));
            Assert.AreNotEqual("guard_squad", window.SourceKey);
        }
    }
}
