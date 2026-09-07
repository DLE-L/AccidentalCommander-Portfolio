using System;
using System.Collections.Generic;

namespace Lizzo.PV.Gameplay.CardOffer
{
    internal sealed partial class CardOfferGenerationService
    {
        readonly struct WeightedGrowthCandidate
        {
            public WeightedGrowthCandidate(CardKind kind, float weight)
            {
                Kind = kind;
                Weight = weight;
            }

            public CardKind Kind { get; }
            public float Weight { get; }
        }

        private List<WeightedGrowthCandidate> BuildUnifiedGrowthCandidates(CardKind[] excludedKinds, List<CardKind> selectedKinds)
        {
            List<WeightedGrowthCandidate> candidates = new List<WeightedGrowthCandidate>(32);
            if (_canonicalCompanionEligibility != null)
            {
                _canonicalCompanionEligibility.CollectEligibleCandidates(_canonicalCompanionCandidates);
                for (int i = 0; i < _canonicalCompanionCandidates.Count; i++)
                {
                    CanonicalCompanionCardCandidate candidate = _canonicalCompanionCandidates[i];
                    AddGrowthCandidate(candidates, candidate.CardKind, candidate.Weight, excludedKinds, selectedKinds);
                }
            }

            if (_canonicalPassiveCards != null)
            {
                _canonicalPassiveCards.CollectEligibleCandidates(_canonicalPassiveCandidates);
                for (int i = 0; i < _canonicalPassiveCandidates.Count; i++)
                {
                    CanonicalPassiveCardCandidate candidate = _canonicalPassiveCandidates[i];
                    AddGrowthCandidate(candidates, candidate.CardKind, candidate.Weight, excludedKinds, selectedKinds);
                }
            }

            return candidates;
        }

        private void AddGrowthCandidate(List<WeightedGrowthCandidate> candidates, CardKind kind, float weight, CardKind[] excludedKinds, List<CardKind> selectedKinds)
        {
            if (weight <= 0.0f || IsGrowthCard(kind) == false || ContainsKind(excludedKinds, kind) || selectedKinds.Contains(kind) || CanCardAppear(kind) == false)
                return;
            for (int i = 0; i < candidates.Count; i++)
                if (candidates[i].Kind == kind) return;
            candidates.Add(new WeightedGrowthCandidate(kind, weight));
        }

        private bool IsGrowthCard(CardKind kind)
        {
            return CardOfferPoolResolver.IsCurrentProductCardAvailable(kind);
        }

        private bool TryAddCardKind(
            List<CardKind> selectedKinds,
            CardKind kind,
            CardKind[] excludedKinds,
            int cardOptionCount,
            Func<CardKind, bool> canCardAppear,
            ref bool filtered)
        {
            if (selectedKinds.Count >= cardOptionCount)
                return false;

            if (selectedKinds.Contains(kind))
                return false;

            if (ContainsKind(excludedKinds, kind))
            {
                filtered = true;
                return false;
            }

            if (canCardAppear(kind) == false)
            {
                filtered = true;
                return false;
            }

            selectedKinds.Add(kind);
            return true;
        }

        private bool ContainsKind(CardKind[] kinds, CardKind candidate)
        {
            if (kinds == null)
                return false;

            for (int i = 0; i < kinds.Length; i++)
            {
                if (kinds[i] == candidate)
                    return true;
            }

            return false;
        }

    }
}
