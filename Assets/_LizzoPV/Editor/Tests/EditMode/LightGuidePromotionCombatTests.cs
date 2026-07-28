using Lizzo.PV.Legion;
using NUnit.Framework;
using UnityEngine;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class LightGuidePromotionCombatTests
    {
        [Test]
        public void BaseAndPromotedHealSetup_KeepOneCadenceAndChangeOnlySecondTargetContract()
        {
            CompanionRangedSupportCombatSetup baseSetup = CreateBaseSetup();
            CompanionRangedSupportCombatSetup promoted = baseSetup.WithPromotedLightGuideHeal();

            Assert.AreEqual(1, baseSetup.SecondaryMaxTargets);
            Assert.AreEqual(1.0f, baseSetup.SecondarySecondTargetRatio);
            Assert.AreEqual(2, promoted.SecondaryMaxTargets);
            Assert.AreEqual(0.70f, promoted.SecondarySecondTargetRatio);
            Assert.AreEqual(baseSetup.SecondaryPeriod, promoted.SecondaryPeriod);
            Assert.AreEqual(baseSetup.SecondaryRange, promoted.SecondaryRange);
            Assert.AreEqual(baseSetup.SecondaryNoTargetRetrySeconds, promoted.SecondaryNoTargetRetrySeconds);
        }

        [Test]
        public void PromotedLightGuide_AppliesGrowthBeforeSecondTargetRatio()
        {
            GameObject owner = new GameObject("LightGuideCombat");
            try
            {
                AllyCombat combat = owner.AddComponent<AllyCombat>();
                combat.SetCanonicalRangedSupportInfo(CreateBaseSetup().WithPromotedLightGuideHeal());
                combat.ApplyGrowthScale(new CompanionGrowthScale(1.8f, 2.1f, 0.9f, 3));

                Assert.AreEqual(14, combat.SecondaryHealAmount);
                Assert.AreEqual(10, Mathf.RoundToInt(combat.SecondaryHealAmount * combat.SecondaryHealSecondTargetRatio));
                Assert.AreEqual(2, combat.SecondaryHealMaxTargets);
                Assert.AreEqual(0.15f, combat.NoTargetRetrySeconds);
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        private static CompanionRangedSupportCombatSetup CreateBaseSetup()
        {
            return new CompanionRangedSupportCombatSetup(
                new CompanionProjectileCombatSetup("cleric", AllyAttackStyle.TargetedProjectile, 5, 1.6f, 4.5f, 1, 0.15f),
                8,
                4.0f,
                4.0f,
                1,
                1.0f,
                0.15f);
        }
    }
}
