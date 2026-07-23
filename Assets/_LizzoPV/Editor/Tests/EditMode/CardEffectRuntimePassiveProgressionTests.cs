using NUnit.Framework;
using Lizzo.PV.P0.Cards;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class CardEffectRuntimePassiveProgressionTests
    {
        [Test]
        public void FiveDistinctPassiveTypesAreAcceptedAndSixthNewTypeIsRejected()
        {
            CardEffectRuntime.PassiveProgression progression = new CardEffectRuntime.PassiveProgression();

            for (int i = 0; i < CardEffectRuntime.MaxDistinctPassiveTypes; i++)
                Assert.IsTrue(progression.TryRecordSuccess($"passive_{i}"));

            Assert.AreEqual(5, progression.DistinctCount);
            Assert.IsFalse(progression.TryRecordSuccess("passive_5"));
        }

        [Test]
        public void OwnedPassiveCanUpgradeWhileFiveDistinctSlotsAreFull()
        {
            CardEffectRuntime.PassiveProgression progression = new CardEffectRuntime.PassiveProgression();

            for (int i = 0; i < CardEffectRuntime.MaxDistinctPassiveTypes; i++)
                Assert.IsTrue(progression.TryRecordSuccess($"passive_{i}"));

            Assert.IsTrue(progression.TryRecordSuccess("passive_0"));
            Assert.AreEqual(2, progression.GetCount("passive_0"));
            Assert.AreEqual(5, progression.DistinctCount);
        }

        [Test]
        public void ExactlyThreeSuccessesReachMaxLevelAndFourthIsRejected()
        {
            CardEffectRuntime.PassiveProgression progression = new CardEffectRuntime.PassiveProgression();

            Assert.IsTrue(progression.TryRecordSuccess("passive"));
            Assert.IsTrue(progression.TryRecordSuccess("passive"));
            Assert.IsTrue(progression.TryRecordSuccess("passive"));

            Assert.AreEqual(3, progression.GetCount("passive"));
            Assert.IsFalse(progression.IsEligible("passive"));
            Assert.IsFalse(progression.TryRecordSuccess("passive"));
        }

        [Test]
        public void SamePassiveNeverCreatesDuplicateDistinctSlot()
        {
            CardEffectRuntime.PassiveProgression progression = new CardEffectRuntime.PassiveProgression();

            Assert.IsTrue(progression.TryRecordSuccess("passive"));
            Assert.IsTrue(progression.TryRecordSuccess("passive"));

            Assert.AreEqual(1, progression.DistinctCount);
            CardKind[] acquiredKinds = new CardKind[CardEffectRuntime.MaxDistinctPassiveTypes];
            Assert.AreEqual(1, progression.FillDistinctKinds(acquiredKinds));
        }

        [Test]
        public void ResetClearsAllPassiveProgression()
        {
            CardEffectRuntime.PassiveProgression progression = new CardEffectRuntime.PassiveProgression();

            for (int i = 0; i < CardEffectRuntime.MaxDistinctPassiveTypes; i++)
                Assert.IsTrue(progression.TryRecordSuccess($"passive_{i}"));

            progression.Reset();

            Assert.AreEqual(0, progression.GetCount("passive_0"));
            Assert.AreEqual(0, progression.DistinctCount);
            Assert.IsTrue(progression.IsEligible("passive_new"));
        }
    }
}
