using Lizzo.PV.P0.Cards;
using Lizzo.PV.P0.Cards.CardOffer;
using NUnit.Framework;
using System;
using System.Reflection;

namespace Lizzo.PV.EditorTests
{
    public sealed class CardOfferDataCoreTests
    {
        [Test]
        public void SameSeedAndPolicy_ReplaysTheSameThreeSlots()
        {
            CardOfferCandidate[] candidates = CreateCandidates();
            CardOfferConfig config = new CardOfferConfig("test-policy", "assignment-a");

            CardOfferGenerationResult first = DeterministicCardOfferService.Generate(
                new CardOfferRunState("run-a"), candidates, 3, 17UL, config, "state-a");
            CardOfferGenerationResult second = DeterministicCardOfferService.Generate(
                new CardOfferRunState("run-a"), Reverse(candidates), 3, 17UL, config, "state-a");

            Assert.That(first.HasOffer, Is.True);
            Assert.That(second.HasOffer, Is.True);
            Assert.That(first.Snapshot.RunId, Is.EqualTo("run-a"));
            Assert.That(first.Snapshot.EventSequence, Is.EqualTo(1));
            Assert.That(first.Snapshot.OfferIndex, Is.EqualTo(1));
            Assert.That(first.Snapshot.OfferSeed, Is.EqualTo(second.Snapshot.OfferSeed));
            Assert.That(first.Snapshot.PolicyVersion, Is.EqualTo("test-policy"));
            Assert.That(first.Snapshot.ConfigAssignmentHash, Is.EqualTo("assignment-a"));
            Assert.That(first.Snapshot.RunStateHash, Is.EqualTo("state-a"));
            Assert.That(first.Snapshot.CandidateCount, Is.EqualTo(4));
            Assert.That(first.Snapshot.EligibleDiagnostics.Count, Is.EqualTo(4));
            Assert.That(first.Snapshot.Slots.Count, Is.EqualTo(3));
            for (int i = 0; i < first.Snapshot.Slots.Count; i++)
            {
                Assert.That(first.Snapshot.Slots[i].CardId, Is.EqualTo(second.Snapshot.Slots[i].CardId));
                Assert.That(first.Snapshot.Slots[i].Weight, Is.EqualTo(second.Snapshot.Slots[i].Weight));
            }
        }

        [Test]
        public void DifferentSeeds_CanProduceDifferentValidOffers()
        {
            CardOfferConfig config = new CardOfferConfig("test-policy", "assignment-a");
            CardOfferGenerationResult baseline = DeterministicCardOfferService.Generate(
                new CardOfferRunState("run-a"), CreateCandidates(), 3, 1UL, config, "state-a");
            bool foundDifferentOffer = false;

            for (ulong seed = 2UL; seed <= 32UL && foundDifferentOffer == false; seed++)
            {
                CardOfferGenerationResult candidate = DeterministicCardOfferService.Generate(
                    new CardOfferRunState("run-a"), CreateCandidates(), 3, seed, config, "state-a");
                Assert.That(candidate.HasOffer, Is.True);
                Assert.That(candidate.Snapshot.Slots.Count, Is.EqualTo(3));
                foundDifferentOffer = HasDifferentOrder(baseline.Snapshot, candidate.Snapshot);
            }

            Assert.That(foundDifferentOffer, Is.True);
        }

        [Test]
        public void WeightedOffer_IsDistinctAndUsesConfiguredWeightOverride()
        {
            CardOfferConfig config = new CardOfferConfig(
                "test-policy",
                "assignment-a",
                new[] { new CardOfferWeightOverride("card_c", 100.0f) });

            CardOfferGenerationResult result = DeterministicCardOfferService.Generate(
                new CardOfferRunState("run-a"), CreateCandidates(), 3, 2UL, config, "state-a");

            Assert.That(result.HasOffer, Is.True);
            Assert.That(result.Snapshot.Slots.Count, Is.EqualTo(3));
            CollectionAssert.AllItemsAreUnique(new[]
            {
                result.Snapshot.Slots[0].CardId,
                result.Snapshot.Slots[1].CardId,
                result.Snapshot.Slots[2].CardId,
            });
            int overriddenIndex = -1;
            for (int i = 0; i < result.Snapshot.Slots.Count; i++)
                if (result.Snapshot.Slots[i].CardId == "card_c")
                    overriddenIndex = i;
            Assert.That(overriddenIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(result.Snapshot.Slots[overriddenIndex].Weight, Is.EqualTo(100.0f));
        }

        [Test]
        public void SelectionCommit_IsIdempotentForAnOfferIdentity()
        {
            CardOfferRunState state = new CardOfferRunState("run-a");
            CardOfferGenerationResult result = DeterministicCardOfferService.Generate(
                state, CreateCandidates(), 3, 17UL, new CardOfferConfig("test-policy", "assignment-a"), "state-a");

            Assert.That(DeterministicCardOfferService.TryCommitSelection(state, result.Snapshot.OfferIdentity, 1, out CardOfferSlot selected), Is.True);
            Assert.That(selected.CardId, Is.EqualTo(result.Snapshot.Slots[1].CardId));
            Assert.That(DeterministicCardOfferService.TryCommitSelection(state, result.Snapshot.OfferIdentity, 1, out _), Is.False);
            Assert.That(DeterministicCardOfferService.TryCommitSelection(state, result.Snapshot.OfferIdentity, 0, out _), Is.False);
        }

        [Test]
        public void ExhaustedCandidates_CompletesBuildOnceAndSuppressesLaterOffers()
        {
            CardOfferRunState state = new CardOfferRunState("run-a");
            CardOfferConfig config = new CardOfferConfig("test-policy", "assignment-a");

            CardOfferGenerationResult first = DeterministicCardOfferService.Generate(
                state, System.Array.Empty<CardOfferCandidate>(), 3, 17UL, config, "state-a");
            CardOfferGenerationResult later = DeterministicCardOfferService.Generate(
                state, CreateCandidates(), 3, 18UL, config, "state-b");

            Assert.That(first.IsMaxBuildComplete, Is.True);
            Assert.That(first.HasOffer, Is.False);
            Assert.That(state.MaxBuildComplete, Is.True);
            Assert.That(state.TryRequestBuildCompleteBanner(), Is.True);
            Assert.That(state.TryRequestBuildCompleteBanner(), Is.False);
            Assert.That(later.IsMaxBuildComplete, Is.True);
            Assert.That(later.HasOffer, Is.False);
        }

        [Test]
        public void CurrentProductOfferPolicy_ExcludesRetiredAttackCards()
        {
            MethodInfo isAvailable = ResolveInternalType("CardOfferPoolResolver")
                .GetMethod("IsCurrentProductCardAvailable", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(isAvailable, Is.Not.Null);
            Assert.That(
                isAvailable.Invoke(null, new object[] { CardKind.BasicAttackUp }),
                Is.False);
            Assert.That(
                isAvailable.Invoke(null, new object[] { CardKind.LegionBanner }),
                Is.False);
            Assert.That(
                isAvailable.Invoke(null, new object[] { CardKind.MoveSpeedUp }),
                Is.True);
        }

        [Test]
        public void CurrentProductApplicationRoute_RejectsRetiredAttackCards()
        {
            Type routerType = ResolveInternalType("CardApplicationRouter");
            object router = Activator.CreateInstance(
                routerType,
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new object[] { null, null, null, true },
                null);
            MethodInfo tryApply = routerType.GetMethod("TryApply", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(tryApply, Is.Not.Null);

            Assert.That(tryApply.Invoke(router, new object[]
            {
                new CardData(CardKind.BasicAttackUp, "retired", "retired", CardHighlight.None),
                null,
                null,
            }),
                Is.False);
            Assert.That(tryApply.Invoke(router, new object[]
            {
                new CardData(CardKind.LegionBanner, "retired", "retired", CardHighlight.None),
                null,
                null,
            }),
                Is.False);
        }

        static Type ResolveInternalType(string name)
        {
            Type type = typeof(FixedCardPool).Assembly.GetType($"Lizzo.PV.P0.Cards.{name}");
            Assert.That(type, Is.Not.Null);
            return type;
        }

        static CardOfferCandidate[] CreateCandidates()
        {
            return new[]
            {
                new CardOfferCandidate(CardKind.BasicAttackUp, "card_a", 1.0f),
                new CardOfferCandidate(CardKind.MoveSpeedUp, "card_b", 1.0f),
                new CardOfferCandidate(CardKind.LegionBanner, "card_c", 1.0f),
                new CardOfferCandidate(CardKind.GuardShockwaveCrest, "card_d", 1.0f),
            };
        }

        static CardOfferCandidate[] Reverse(CardOfferCandidate[] source)
        {
            CardOfferCandidate[] result = new CardOfferCandidate[source.Length];
            for (int i = 0; i < source.Length; i++)
                result[i] = source[source.Length - 1 - i];
            return result;
        }

        static bool HasDifferentOrder(CardOfferSnapshot left, CardOfferSnapshot right)
        {
            for (int i = 0; i < left.Slots.Count; i++)
                if (left.Slots[i].CardId != right.Slots[i].CardId)
                    return true;
            return false;
        }
    }
}
