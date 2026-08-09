using System;
using System.Collections.Generic;

namespace Lizzo.PV.P0.Cards.CardOffer
{
    public static class DeterministicCardOfferService
    {
        public static CardOfferGenerationResult Generate(
            CardOfferRunState runState,
            IReadOnlyList<CardOfferCandidate> candidates,
            int maxSlots,
            ulong offerSeed,
            CardOfferConfig config,
            string runStateHash)
        {
            return Generate(runState, Array.Empty<CardOfferCandidate>(), candidates, maxSlots, offerSeed, config, runStateHash);
        }

        public static CardOfferGenerationResult Generate(
            CardOfferRunState runState,
            IReadOnlyList<CardOfferCandidate> fixedSlots,
            IReadOnlyList<CardOfferCandidate> candidates,
            int maxSlots,
            ulong offerSeed,
            CardOfferConfig config,
            string runStateHash)
        {
            if (runState == null)
                throw new ArgumentNullException(nameof(runState));

            if (runState.MaxBuildComplete)
                return CardOfferGenerationResult.MaxBuildComplete();

            CardOfferConfig resolvedConfig = config ?? CardOfferConfig.LegacyCompatibility;
            int slotLimit = Math.Max(1, Math.Min(3, maxSlots));
            List<CardOfferSlot> slots = BuildFixedSlots(fixedSlots, slotLimit);
            List<CardOfferCandidate> eligible = BuildStableEligibleCandidates(candidates, resolvedConfig, slots);
            if (slots.Count == 0 && eligible.Count == 0)
            {
                runState.TryMarkMaxBuildComplete();
                return CardOfferGenerationResult.MaxBuildComplete();
            }

            WeightedPrng random = new WeightedPrng(offerSeed);
            while (slots.Count < slotLimit && eligible.Count > 0)
            {
                int selectedIndex = DrawIndex(eligible, ref random);
                CardOfferCandidate selected = eligible[selectedIndex];
                slots.Add(new CardOfferSlot(slots.Count, selected.Kind, selected.CardId, selected.Weight));
                eligible.RemoveAt(selectedIndex);
            }

            CardOfferCandidate[] diagnostics = BuildStableEligibleCandidates(candidates, resolvedConfig, null).ToArray();
            CardOfferSlot[] visibleSlots = slots.ToArray();
            CardOfferSnapshot snapshot = new CardOfferSnapshot(
                runState.RunId,
                runState.ReserveEventSequence(),
                runState.ReserveOfferIndex(),
                offerSeed,
                resolvedConfig.PolicyVersion,
                resolvedConfig.AssignmentHash,
                runStateHash,
                diagnostics.Length,
                visibleSlots,
                diagnostics);
            runState.SetActiveSnapshot(snapshot);
            return CardOfferGenerationResult.Offer(snapshot);
        }

        public static bool TryCommitSelection(CardOfferRunState runState, string offerIdentity, int slotIndex, out CardOfferSlot selected)
        {
            selected = default;
            if (runState == null || runState.TryCommit(offerIdentity, slotIndex) == false)
                return false;

            selected = runState.ActiveSnapshot.Slots[slotIndex];
            return true;
        }

        static List<CardOfferSlot> BuildFixedSlots(IReadOnlyList<CardOfferCandidate> fixedSlots, int slotLimit)
        {
            List<CardOfferSlot> slots = new List<CardOfferSlot>(slotLimit);
            if (fixedSlots == null)
                return slots;

            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < fixedSlots.Count && slots.Count < slotLimit; i++)
            {
                CardOfferCandidate candidate = fixedSlots[i];
                if (IsUsable(candidate) == false || ids.Add(candidate.CardId) == false)
                    continue;

                slots.Add(new CardOfferSlot(slots.Count, candidate.Kind, candidate.CardId, candidate.Weight));
            }

            return slots;
        }

        static List<CardOfferCandidate> BuildStableEligibleCandidates(
            IReadOnlyList<CardOfferCandidate> candidates,
            CardOfferConfig config,
            List<CardOfferSlot> fixedSlots)
        {
            List<CardOfferCandidate> result = new List<CardOfferCandidate>(candidates == null ? 0 : candidates.Count);
            if (candidates == null)
                return result;

            HashSet<string> blocked = new HashSet<string>(StringComparer.Ordinal);
            if (fixedSlots != null)
                for (int i = 0; i < fixedSlots.Count; i++)
                    blocked.Add(fixedSlots[i].CardId);

            Dictionary<string, CardOfferCandidate> byId = new Dictionary<string, CardOfferCandidate>(StringComparer.Ordinal);
            for (int i = 0; i < candidates.Count; i++)
            {
                CardOfferCandidate candidate = candidates[i];
                float weight = config.ResolveWeight(candidate.CardId, candidate.Weight);
                candidate = new CardOfferCandidate(candidate.Kind, candidate.CardId, weight);
                if (IsUsable(candidate) == false || blocked.Contains(candidate.CardId))
                    continue;

                if (byId.TryGetValue(candidate.CardId, out CardOfferCandidate existing) == false || candidate.Kind < existing.Kind)
                    byId[candidate.CardId] = candidate;
            }

            foreach (CardOfferCandidate candidate in byId.Values)
                result.Add(candidate);
            result.Sort(CompareCandidates);
            return result;
        }

        static bool IsUsable(CardOfferCandidate candidate)
        {
            return string.IsNullOrWhiteSpace(candidate.CardId) == false
                && float.IsNaN(candidate.Weight) == false
                && float.IsInfinity(candidate.Weight) == false
                && candidate.Weight > 0.0f;
        }

        static int CompareCandidates(CardOfferCandidate left, CardOfferCandidate right)
        {
            int byId = string.CompareOrdinal(left.CardId, right.CardId);
            return byId != 0 ? byId : left.Kind.CompareTo(right.Kind);
        }

        static int DrawIndex(List<CardOfferCandidate> candidates, ref WeightedPrng random)
        {
            double totalWeight = 0.0;
            for (int i = 0; i < candidates.Count; i++)
                totalWeight += candidates[i].Weight;

            double roll = random.NextUnit() * totalWeight;
            for (int i = 0; i < candidates.Count; i++)
            {
                roll -= candidates[i].Weight;
                if (roll <= 0.0)
                    return i;
            }

            return candidates.Count - 1;
        }

        struct WeightedPrng
        {
            ulong _state;

            public WeightedPrng(ulong seed)
            {
                _state = seed == 0UL ? 0x9E3779B97F4A7C15UL : seed;
            }

            public double NextUnit()
            {
                ulong value = NextUInt64();
                return (value >> 11) * (1.0 / 9007199254740992.0);
            }

            ulong NextUInt64()
            {
                _state ^= _state >> 12;
                _state ^= _state << 25;
                _state ^= _state >> 27;
                return _state * 2685821657736338717UL;
            }
        }
    }
}
