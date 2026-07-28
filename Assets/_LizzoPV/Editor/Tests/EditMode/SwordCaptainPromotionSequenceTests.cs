using Lizzo.PV.Legion;
using NUnit.Framework;
using UnityEngine;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class SwordCaptainPromotionSequenceTests
    {
        [Test]
        public void PromotedSequence_UsesTwoPassesWithExactAlreadyScaledDamageRatio()
        {
            PromotedMultiHitSequence promoted = new PromotedMultiHitSequence(2, 0.70f);

            Assert.AreEqual(2, promoted.PassCount);
            Assert.AreEqual(0.70f, promoted.DamageRatio);
            Assert.AreEqual(14, UnityEngine.Mathf.RoundToInt(20 * promoted.DamageRatio));
        }

        [Test]
        public void PromotedSequence_CompletesTwoIndependentPassesWithoutTargetSuppression()
        {
            PromotedMultiHitSequence sequence = new PromotedMultiHitSequence(2, 0.70f);

            sequence.BeginCast();
            Assert.IsTrue(sequence.TryRecordResolvedPass());
            Assert.AreEqual(1, sequence.CompletedPassCount);
            Assert.IsFalse(sequence.IsComplete);

            Assert.IsTrue(sequence.TryRecordResolvedPass());
            Assert.AreEqual(2, sequence.CompletedPassCount);
            Assert.IsTrue(sequence.IsComplete);
            Assert.IsFalse(sequence.TryRecordResolvedPass());
        }

        [Test]
        public void PromotedSequence_FirstPassMissKeepsReturnSignalIncompleteAndSupportsRetry()
        {
            PromotedMultiHitSequence sequence = new PromotedMultiHitSequence(2, 0.70f);

            sequence.BeginCast();
            Assert.AreEqual(0, sequence.CompletedPassCount);
            Assert.IsFalse(sequence.IsComplete);

            sequence.BeginCast();
            Assert.IsTrue(sequence.TryRecordResolvedPass());
            Assert.IsFalse(sequence.IsComplete);
            sequence.Reset();
            Assert.AreEqual(0, sequence.CompletedPassCount);
            Assert.IsFalse(sequence.IsComplete);
        }

        [Test]
        public void BaseMeleeRouting_RetainsOnePassTargetCapAndNoTargetRetryContract()
        {
            GameObject owner = new GameObject("SwordCaptainBaseMelee");
            try
            {
                AllyCombat combat = owner.AddComponent<AllyCombat>();
                combat.SetCanonicalMeleeInfo(new CompanionMeleeCombatSetup(
                    AllyAttackStyle.ForwardSlash,
                    12,
                    1.0f,
                    1.1f,
                    60.0f,
                    0.0f,
                    3,
                    0.15f));

                Assert.AreEqual(AllyAttackStyle.ForwardSlash, combat.AttackStyle);
                Assert.AreEqual(3, combat.MaxForwardTargetCount);
                Assert.IsTrue(combat.CanAcceptForwardTarget(2));
                Assert.IsFalse(combat.CanAcceptForwardTarget(3));
                Assert.AreEqual(0.15f, combat.ResolveNextAttackDelay(false));
                Assert.IsFalse(combat.ReturnToPreferredSlotRequested);
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }
    }
}
