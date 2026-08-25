using System;
using Lizzo.PV.Gameplay.RunTraits;
using Lizzo.PV.P0.Cards;
using NUnit.Framework;

namespace Lizzo.PV.EditorTests
{
    public sealed class RunTraitOfferCoordinatorTests
    {
        [Test]
        public void PendingOpportunity_ProducesExactlyThreeDistinctEligibleTraitsInStableOrder()
        {
            using RunTraitRunState state = new RunTraitRunState();
            using RunTraitOfferCoordinator coordinator = new RunTraitOfferCoordinator(state);
            RunTraitEligibilityContext context = new RunTraitEligibilityContext(
                explosiveFamilyOwned: true,
                hasReadySynergy: false,
                hasPromotionOpportunity: true,
                emergencyRallyActivated: false,
                secondsUntilBossSpawn: 240.0f,
                activeSquadCount: 2,
                isPresentationSafe: true);
            Assert.That(coordinator.ReportEliteDefeated(), Is.True);

            Assert.That(coordinator.TryGetPendingOffer(context, out RunTraitOfferSnapshot first), Is.True);
            Assert.That(first.Slots, Has.Count.EqualTo(3));
            Assert.That(first.Slots[0].TraitId, Is.Not.EqualTo(first.Slots[1].TraitId));
            Assert.That(first.Slots[0].TraitId, Is.Not.EqualTo(first.Slots[2].TraitId));
            Assert.That(first.Slots[1].TraitId, Is.Not.EqualTo(first.Slots[2].TraitId));
            Assert.That(first.OrderedEligibleTraitIds, Has.Count.GreaterThanOrEqualTo(3));
            Assert.That(coordinator.TryGetPendingOffer(context, out RunTraitOfferSnapshot repeated), Is.True);
            Assert.That(repeated.OfferIdentity, Is.EqualTo(first.OfferIdentity));
            CollectionAssert.AreEqual(first.Slots, repeated.Slots);
            Assert.That(first.Slots, Has.Some.Matches<RunTraitOfferSlot>(slot => RunTraitCatalog.TryGet(slot.TraitId, out RunTraitDefinition trait) && trait.Category == RunTraitCategories.BuildRelated));
            Assert.That(first.Slots, Has.Some.Matches<RunTraitOfferSlot>(slot => RunTraitCatalog.TryGet(slot.TraitId, out RunTraitDefinition trait) && trait.Category != RunTraitCategories.BuildRelated));
        }

        [Test]
        public void ElapsedTimeNeverCreatesAnOpportunityWithoutEliteDefeat()
        {
            using RunTraitRunState state = new RunTraitRunState();
            using RunTraitOfferCoordinator coordinator = new RunTraitOfferCoordinator(state);

            Assert.That(coordinator.TryGetPendingOffer(300.0f, CreateSafeEligibleContext(), out _), Is.False);
            Assert.That(coordinator.GetPendingOpportunityIndex(300.0f), Is.EqualTo(-1));
            Assert.That(coordinator.HasPendingOpportunity, Is.False);
            Assert.That(coordinator.ActiveOffer, Is.Null);
        }

        [Test]
        public void PendingOpportunity_DefersUntilPresentationWindowIsSafe()
        {
            using RunTraitRunState state = new RunTraitRunState();
            using RunTraitOfferCoordinator coordinator = new RunTraitOfferCoordinator(state);
            RunTraitEligibilityContext blocked = new RunTraitEligibilityContext(true, false, true, false, 240.0f, 2, isPresentationSafe: false);
            Assert.That(coordinator.ReportEliteDefeated(), Is.True);

            Assert.That(coordinator.TryGetPendingOffer(blocked, out _), Is.False);
            Assert.That(coordinator.HasPendingOpportunity, Is.True);
            Assert.That(coordinator.ActiveOffer, Is.Null);
            Assert.That(coordinator.TryGetPendingOffer(CreateSafeEligibleContext(), out _), Is.True);
        }

        [Test]
        public void PendingOpportunity_RemainsFirstUntilItCanBePresented()
        {
            using RunTraitRunState state = new RunTraitRunState();
            using RunTraitOfferCoordinator coordinator = new RunTraitOfferCoordinator(state);
            RunTraitEligibilityContext context = new RunTraitEligibilityContext(
                explosiveFamilyOwned: false,
                hasReadySynergy: false,
                hasPromotionOpportunity: false,
                emergencyRallyActivated: false,
                secondsUntilBossSpawn: 30.0f,
                activeSquadCount: 4,
                isPresentationSafe: true);
            Assert.That(coordinator.ReportEliteDefeated(), Is.True);

            Assert.That(coordinator.TryGetPendingOffer(context, out _), Is.False);
            Assert.That(coordinator.GetPendingOpportunityIndex(), Is.EqualTo(0));
            Assert.That(coordinator.ActiveOffer, Is.Null);
            Assert.That(coordinator.ReportEliteDefeated(), Is.True);
            Assert.That(coordinator.GetPendingOpportunityIndex(), Is.EqualTo(0));
            Assert.That(coordinator.TryGetPendingOffer(CreateSafeEligibleContext(), out RunTraitOfferSnapshot next), Is.True);
            Assert.That(next.OpportunityIndex, Is.EqualTo(0));
        }

        [Test]
        public void PendingOpportunity_ProducesExactlyTwoRelevantTraitsAndAcceptsNormally()
        {
            using RunTraitRunState state = new RunTraitRunState();
            using RunTraitOfferCoordinator coordinator = new RunTraitOfferCoordinator(state);
            RunTraitEligibilityContext context = new RunTraitEligibilityContext(
                explosiveFamilyOwned: false,
                hasReadySynergy: false,
                hasPromotionOpportunity: false,
                emergencyRallyActivated: false,
                secondsUntilBossSpawn: 30.0f,
                activeSquadCount: 2,
                isPresentationSafe: true);
            Assert.That(coordinator.ReportEliteDefeated(), Is.True);

            Assert.That(coordinator.TryGetPendingOffer(context, out RunTraitOfferSnapshot offer), Is.True);
            Assert.That(offer.Slots, Has.Count.EqualTo(2));
            CollectionAssert.AreEquivalent(offer.OrderedEligibleTraitIds, new[] { offer.Slots[0].TraitId, offer.Slots[1].TraitId });

            RunTraitOfferSlot selected = offer.Slots[1];
            Assert.That(coordinator.TryAcceptSelection(offer.OfferIdentity, selected.SlotIndex, selected.TraitId), Is.True);
            CollectionAssert.AreEqual(new[] { selected.TraitId }, state.SelectedTraitIds);
        }

        [Test]
        public void AcceptedOffer_AllowsTheNextEliteDefeatOpportunity()
        {
            using RunTraitRunState state = new RunTraitRunState();
            using RunTraitOfferCoordinator coordinator = new RunTraitOfferCoordinator(state);
            RunTraitEligibilityContext context = CreateSafeEligibleContext();
            Assert.That(coordinator.ReportEliteDefeated(), Is.True);

            Assert.That(coordinator.TryGetPendingOffer(context, out RunTraitOfferSnapshot first), Is.True);
            Assert.That(first.OpportunityIndex, Is.EqualTo(0));
            RunTraitOfferSlot selected = first.Slots[0];
            Assert.That(coordinator.TryAcceptSelection(first.OfferIdentity, selected.SlotIndex, selected.TraitId), Is.True);

            Assert.That(coordinator.ReportEliteDefeated(), Is.True);
            Assert.That(coordinator.TryGetPendingOffer(context, out RunTraitOfferSnapshot second), Is.True);
            Assert.That(second.OpportunityIndex, Is.EqualTo(1));
            Assert.That(second.OfferIdentity, Is.Not.EqualTo(first.OfferIdentity));
        }

        [Test]
        public void ReadySynergyEligibility_OffersMomentOfCompletionWithoutSyntheticTraits()
        {
            using RunTraitRunState state = new RunTraitRunState();
            using RunTraitOfferCoordinator coordinator = new RunTraitOfferCoordinator(state);
            RunTraitEligibilityContext context = new RunTraitEligibilityContext(
                explosiveFamilyOwned: false,
                hasReadySynergy: true,
                hasPromotionOpportunity: false,
                emergencyRallyActivated: false,
                secondsUntilBossSpawn: 30.0f,
                activeSquadCount: 2,
                isPresentationSafe: true);
            Assert.That(coordinator.ReportEliteDefeated(), Is.True);

            Assert.That(coordinator.TryGetPendingOffer(context, out RunTraitOfferSnapshot offer), Is.True);
            Assert.That(offer.Slots, Has.Some.Matches<RunTraitOfferSlot>(slot => slot.TraitId == RunTraitIds.MomentOfCompletion));
        }

        [Test]
        public void AcceptSelection_ValidatesSnapshotAndCommitsOnce()
        {
            using RunTraitRunState state = new RunTraitRunState();
            using RunTraitOfferCoordinator coordinator = new RunTraitOfferCoordinator(state);
            RunTraitEligibilityContext context = CreateSafeEligibleContext();
            Assert.That(coordinator.ReportEliteDefeated(), Is.True);
            Assert.That(coordinator.TryGetPendingOffer(context, out RunTraitOfferSnapshot offer), Is.True);

            RunTraitOfferSlot slot = offer.Slots[1];
            Assert.That(coordinator.TryAcceptSelection(offer.OfferIdentity, slot.SlotIndex, slot.TraitId), Is.True);
            Assert.That(coordinator.TryAcceptSelection(offer.OfferIdentity, slot.SlotIndex, slot.TraitId), Is.False);
            CollectionAssert.AreEqual(new[] { slot.TraitId }, state.SelectedTraitIds);
            RunTraitRunStateSnapshot stateSnapshot = state.CaptureSnapshot();
            Assert.That(stateSnapshot.SelectedTraitIds, Is.EqualTo(new[] { slot.TraitId }));
            Assert.That(stateSnapshot.SelectionRecords, Has.Count.EqualTo(1));
            Assert.That(stateSnapshot.SelectionRecords[0].Offer.OfferSeed, Is.EqualTo(offer.OfferSeed));
            Assert.That(stateSnapshot.SelectionRecords[0].Offer.OrderedEligibleTraitIds, Is.EqualTo(offer.OrderedEligibleTraitIds));
            Assert.That(stateSnapshot.SelectionRecords[0].SelectedTraitId, Is.EqualTo(slot.TraitId));
        }

        [Test]
        public void AcceptedSelection_IsExcludedFromTheNextOpportunity()
        {
            using RunTraitRunState state = new RunTraitRunState();
            using RunTraitOfferCoordinator coordinator = new RunTraitOfferCoordinator(state);
            RunTraitEligibilityContext context = CreateSafeEligibleContext();
            Assert.That(coordinator.ReportEliteDefeated(), Is.True);
            Assert.That(coordinator.TryGetPendingOffer(context, out RunTraitOfferSnapshot first), Is.True);

            RunTraitOfferSlot selected = FindNonBuildSlot(first);
            Assert.That(coordinator.TryAcceptSelection(first.OfferIdentity, selected.SlotIndex, selected.TraitId), Is.True);
            Assert.That(coordinator.ReportEliteDefeated(), Is.True);
            Assert.That(coordinator.TryGetPendingOffer(context, out RunTraitOfferSnapshot next), Is.True);
            Assert.That(next.Slots, Has.None.Matches<RunTraitOfferSlot>(slot => slot.TraitId == selected.TraitId));
        }

        [Test]
        public void AcceptedSelections_NeverExceedThree()
        {
            using RunTraitRunState state = new RunTraitRunState();
            using RunTraitOfferCoordinator coordinator = new RunTraitOfferCoordinator(state);
            RunTraitEligibilityContext context = CreateSafeEligibleContext();

            for (int index = 0; index < RunTraitRunState.MaxSelections; index++)
            {
                Assert.That(coordinator.ReportEliteDefeated(), Is.True);
                Assert.That(coordinator.TryGetPendingOffer(context, out RunTraitOfferSnapshot offer), Is.True);
                RunTraitOfferSlot selected = FindNonBuildSlot(offer);
                Assert.That(coordinator.TryAcceptSelection(offer.OfferIdentity, selected.SlotIndex, selected.TraitId), Is.True);
            }

            Assert.That(state.SelectionCount, Is.EqualTo(RunTraitRunState.MaxSelections));
            Assert.That(coordinator.ReportEliteDefeated(), Is.False);
            Assert.That(coordinator.TryGetPendingOffer(context, out _), Is.False);
        }

        [Test]
        public void Restore_PreservesValidRecordsAndRejectsInvalidRecordsAtomically()
        {
            using RunTraitRunState source = new RunTraitRunState();
            using RunTraitOfferCoordinator coordinator = new RunTraitOfferCoordinator(source);
            Assert.That(coordinator.ReportEliteDefeated(), Is.True);
            Assert.That(coordinator.TryGetPendingOffer(CreateSafeEligibleContext(), out RunTraitOfferSnapshot offer), Is.True);
            RunTraitOfferSlot selected = offer.Slots[0];
            Assert.That(coordinator.TryAcceptSelection(offer.OfferIdentity, selected.SlotIndex, selected.TraitId), Is.True);
            RunTraitRunStateSnapshot valid = source.CaptureSnapshot();

            using RunTraitRunState restored = new RunTraitRunState();
            Assert.That(restored.TryRestore(valid), Is.True);
            CollectionAssert.AreEqual(valid.SelectedTraitIds, restored.SelectedTraitIds);
            Assert.That(restored.SelectionRecords, Has.Count.EqualTo(1));

            RunTraitRunStateSnapshot invalid = new RunTraitRunStateSnapshot(
                new[] { RunTraitIds.EmergencyRally },
                new[] { new RunTraitSelectionRecord(offer, RunTraitIds.FuseLink) });
            Assert.That(restored.TryRestore(invalid), Is.False);
            CollectionAssert.AreEqual(valid.SelectedTraitIds, restored.SelectedTraitIds);
            Assert.That(restored.SelectionRecords, Has.Count.EqualTo(1));
        }

        [Test]
        public void RecordingFirstOffer_UsesOnlyNaturalEligibleTraits()
        {
            using RunTraitRunState state = new RunTraitRunState();
            using RunTraitOfferCoordinator coordinator = new RunTraitOfferCoordinator(state);
            RunTraitOfferPolicy recording = RunTraitOfferPolicy.Resolve(CardPoolProfileIds.Recording, 0);
            Assert.That(coordinator.ReportEliteDefeated(), Is.True);

            Assert.That(coordinator.TryGetPendingOffer(CreateRecordingEligibleContext(), recording, out RunTraitOfferSnapshot offer), Is.True);
            Assert.That(offer.PolicyId, Is.EqualTo(RunTraitOfferPolicy.RecordingFirstPolicyId));
            Assert.That(offer.Slots, Has.Count.EqualTo(3));
            Assert.That(offer.Slots[1].TraitId, Is.EqualTo(RunTraitIds.PromotionShout));
            Assert.That(offer.Slots, Has.None.Matches<RunTraitOfferSlot>(slot => RunTraitCatalog.TryGet(slot.TraitId, out RunTraitDefinition trait) && trait.Category == RunTraitCategories.BuildRelated));
        }

        [Test]
        public void RecordingFirstOffer_UsesTwoNaturalCandidatesWhenPromotionShoutIsIneligible()
        {
            using RunTraitRunState withoutPromotion = new RunTraitRunState();
            using RunTraitOfferCoordinator firstCoordinator = new RunTraitOfferCoordinator(withoutPromotion);
            RunTraitOfferPolicy recording = RunTraitOfferPolicy.Resolve(CardPoolProfileIds.Recording, 0);
            RunTraitEligibilityContext promotionIneligible = new RunTraitEligibilityContext(false, false, false, false, 240.0f, 2, true);
            Assert.That(firstCoordinator.ReportEliteDefeated(), Is.True);
            Assert.That(firstCoordinator.TryGetPendingOffer(promotionIneligible, recording, out RunTraitOfferSnapshot offer), Is.True);
            Assert.That(offer.Slots, Has.Count.EqualTo(3));
            Assert.That(offer.PolicyId, Is.EqualTo(RunTraitOfferPolicy.RecordingFirstPolicyId));
            Assert.That(offer.Slots, Has.None.Matches<RunTraitOfferSlot>(slot => slot.TraitId == RunTraitIds.PromotionShout));

            using RunTraitRunState tooFew = new RunTraitRunState();
            using RunTraitOfferCoordinator secondCoordinator = new RunTraitOfferCoordinator(tooFew);
            RunTraitEligibilityContext fewerThanThree = new RunTraitEligibilityContext(false, false, true, false, 30.0f, 4, true);
            Assert.That(secondCoordinator.ReportEliteDefeated(), Is.True);
            Assert.That(secondCoordinator.TryGetPendingOffer(fewerThanThree, recording, out RunTraitOfferSnapshot twoTraitOffer), Is.True);
            Assert.That(twoTraitOffer.Slots, Has.Count.EqualTo(2));
        }

        [Test]
        public void RecordingProfile_SubsequentOpportunityUsesStandardPolicy()
        {
            using RunTraitRunState state = new RunTraitRunState();
            using RunTraitOfferCoordinator coordinator = new RunTraitOfferCoordinator(state);
            RunTraitOfferPolicy recordingFirst = RunTraitOfferPolicy.Resolve(CardPoolProfileIds.Recording, 0);
            Assert.That(coordinator.ReportEliteDefeated(), Is.True);
            Assert.That(coordinator.TryGetPendingOffer(CreateRecordingEligibleContext(), recordingFirst, out RunTraitOfferSnapshot first), Is.True);
            Assert.That(first.PolicyId, Is.EqualTo(RunTraitOfferPolicy.RecordingFirstPolicyId));
            RunTraitOfferSlot selected = FindNonBuildSlot(first);
            Assert.That(coordinator.TryAcceptSelection(first.OfferIdentity, selected.SlotIndex, selected.TraitId), Is.True);

            RunTraitOfferPolicy subsequent = RunTraitOfferPolicy.Resolve(CardPoolProfileIds.Recording, 1);
            Assert.That(subsequent.PolicyId, Is.EqualTo(RunTraitOfferPolicy.StandardPolicyId));
            Assert.That(coordinator.ReportEliteDefeated(), Is.True);
            Assert.That(coordinator.TryGetPendingOffer(CreateSafeEligibleContext(), subsequent, out RunTraitOfferSnapshot next), Is.True);
            Assert.That(next.PolicyId, Is.EqualTo(RunTraitOfferPolicy.StandardPolicyId));
            Assert.That(next.Slots, Has.Some.Matches<RunTraitOfferSlot>(slot => RunTraitCatalog.TryGet(slot.TraitId, out RunTraitDefinition trait) && trait.Category == RunTraitCategories.BuildRelated));
        }

        [Test]
        public void PolicyAwareIdentity_DistinguishesRecordingFromStandardWithoutStrictGate()
        {
            RunTraitEligibilityContext zeroBuild = CreateRecordingEligibleContext();
            using RunTraitRunState standardState = new RunTraitRunState();
            using RunTraitOfferCoordinator standardCoordinator = new RunTraitOfferCoordinator(standardState);
            RunTraitOfferPolicy standard = RunTraitOfferPolicy.Resolve(CardPoolProfileIds.Standard, 0);
            Assert.That(standardCoordinator.ReportEliteDefeated(), Is.True);
            Assert.That(standardCoordinator.TryGetPendingOffer(zeroBuild, standard, out RunTraitOfferSnapshot zeroBuildOffer), Is.True);
            Assert.That(zeroBuildOffer.Slots, Has.Count.EqualTo(3));

            using RunTraitRunState firstState = new RunTraitRunState();
            using RunTraitOfferCoordinator firstCoordinator = new RunTraitOfferCoordinator(firstState);
            Assert.That(firstCoordinator.ReportEliteDefeated(), Is.True);
            Assert.That(firstCoordinator.TryGetPendingOffer(CreateSafeEligibleContext(), standard, out RunTraitOfferSnapshot standardOffer), Is.True);
            using RunTraitRunState secondState = new RunTraitRunState();
            using RunTraitOfferCoordinator secondCoordinator = new RunTraitOfferCoordinator(secondState);
            RunTraitOfferPolicy recording = RunTraitOfferPolicy.Resolve(CardPoolProfileIds.Recording, 0);
            Assert.That(secondCoordinator.ReportEliteDefeated(), Is.True);
            Assert.That(secondCoordinator.TryGetPendingOffer(CreateSafeEligibleContext(), recording, out RunTraitOfferSnapshot recordingOffer), Is.True);
            Assert.That(recordingOffer.OfferIdentity, Is.Not.EqualTo(standardOffer.OfferIdentity));
            Assert.That(recordingOffer.OfferSeed, Is.Not.EqualTo(standardOffer.OfferSeed));
        }

        static RunTraitEligibilityContext CreateSafeEligibleContext()
        {
            return new RunTraitEligibilityContext(true, false, true, false, 240.0f, 2, isPresentationSafe: true);
        }

        static RunTraitEligibilityContext CreateRecordingEligibleContext()
        {
            return new RunTraitEligibilityContext(false, false, true, false, 240.0f, 2, isPresentationSafe: true);
        }

        static RunTraitOfferSlot FindNonBuildSlot(RunTraitOfferSnapshot offer)
        {
            for (int index = 0; index < offer.Slots.Count; index++)
            {
                RunTraitOfferSlot slot = offer.Slots[index];
                if (RunTraitCatalog.TryGet(slot.TraitId, out RunTraitDefinition trait)
                    && trait.Category != RunTraitCategories.BuildRelated)
                    return slot;
            }

            throw new AssertionException("Expected a non-build trait slot.");
        }
    }
}
