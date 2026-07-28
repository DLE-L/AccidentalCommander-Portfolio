using Lizzo.PV.Legion;
using NUnit.Framework;
using UnityEngine;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class FalconCaptainPromotionBurstTests
    {
        [Test]
        public void PromotedBurst_UsesTwoShotsAtSixtyFivePercentOfAlreadyScaledDamage()
        {
            PromotedProjectileBurst burst = new PromotedProjectileBurst(2, 0.65f);

            Assert.AreEqual(2, burst.ShotCount);
            Assert.AreEqual(0.65f, burst.DamageRatio);
            Assert.AreEqual(11, burst.ResolveShotDamage(17));
        }

        [Test]
        public void BaseAndPromotedFalconRouting_KeepOneCadenceAndChangeBurstAndAssistThresholdOnly()
        {
            GameObject owner = new GameObject("FalconCaptainPromotion");
            try
            {
                AllyCombat combat = owner.AddComponent<AllyCombat>();
                CompanionProjectileCombatSetup primary = new CompanionProjectileCombatSetup(
                    "falcon_archer", AllyAttackStyle.TargetedProjectile, 9, 0.9f, 5.5f, 1, 0.15f);
                CompanionOwnedProxyCombatSetup baseAssist = new CompanionOwnedProxyCombatSetup(6, 5.5f, 1, 4);

                combat.SetCanonicalProjectileWithProxyInfo(primary, baseAssist);
                Assert.IsFalse(combat.HasPromotedProjectileBurst);
                Assert.AreEqual(4, combat.OwnedProxyTriggerCount);
                Assert.AreEqual(0.15f, combat.ResolveNextAttackDelay(false));

                combat.ApplyGrowthScale(new CompanionGrowthScale(1.65f, 2.0f, 0.9f, 3));
                combat.SetPromotedProjectileBurst(new PromotedProjectileBurst(2, 0.65f));
                combat.ConfigureOwnedProxyTriggerCount(3);

                Assert.IsTrue(combat.HasPromotedProjectileBurst);
                Assert.AreEqual(3, combat.OwnedProxyTriggerCount);
                Assert.AreEqual(15, combat.Damage);
                Assert.AreEqual(10, combat.PromotedProjectileBurst.ResolveShotDamage(combat.Damage));
                Assert.That(combat.ResolveNextAttackDelay(true), Is.EqualTo(0.81f).Within(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void AssistCounter_UsesOneSuccessfulBurstPerCycle_AndFailedBurstsDoNotAdvance()
        {
            SuccessfulActionCounter baseCounter = new SuccessfulActionCounter();
            baseCounter.Configure(4);
            SuccessfulActionCounter promotedCounter = new SuccessfulActionCounter();
            promotedCounter.Configure(3);

            Assert.IsFalse(baseCounter.RecordSuccess());
            Assert.IsFalse(baseCounter.RecordSuccess());
            Assert.IsFalse(baseCounter.RecordSuccess());
            Assert.IsTrue(baseCounter.RecordSuccess());

            Assert.IsFalse(promotedCounter.RecordSuccess());
            Assert.IsFalse(promotedCounter.RecordSuccess());
            Assert.AreEqual(2, promotedCounter.CurrentCount);
            Assert.IsTrue(promotedCounter.RecordSuccess());
            Assert.AreEqual(0, promotedCounter.CurrentCount);

            promotedCounter.Reset();
            Assert.AreEqual(0, promotedCounter.CurrentCount);
            Assert.IsFalse(promotedCounter.RecordSuccess());
        }
    }
}
