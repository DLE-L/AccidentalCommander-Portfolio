using System.Collections.Generic;
using Lizzo.PV.Legion.Party.Roster;

namespace Lizzo.PV.P0.Cards
{
    internal sealed partial class CardOfferGenerationService
    {
        private bool IsFirstRecruitOffer()
        {
            return _session.LevelUpCount == 1 && _canonicalCompanionEligibility != null;
        }

        private bool IsFirstRecruitCard(CardKind kind)
        {
            return _canonicalCompanionEligibility != null
                && _canonicalCompanionEligibility.IsCanonicalCompanionCard(kind);
        }

        private void KeepFirstRecruitCandidates(List<WeightedGrowthCandidate> candidates)
        {
            for (int index = candidates.Count - 1; index >= 0; index--)
                if (IsFirstRecruitCard(candidates[index].Kind) == false)
                    candidates.RemoveAt(index);
        }

        private bool CanCardAppear(CardKind kind)
        {
            if (_enforceCurrentProductCardPolicy
                && CardOfferPoolResolver.IsCurrentProductCardAvailable(kind) == false)
            {
                return false;
            }

            if (_canonicalPassiveCards != null && _canonicalPassiveCards.TryGetCandidate(kind, out _))
                return true;
            if (_canonicalPassiveCards != null && CanonicalPassiveCardService.TryGetPassiveId(kind, out _))
                return false;

            if (_canonicalCompanionEligibility != null
                && _canonicalCompanionEligibility.IsCanonicalCompanionCard(kind))
            {
                return _canonicalCompanionEligibility.TryGetCandidate(kind, out _);
            }

            return true;
        }

        private CardHighlight ResolveRuntimeHighlight(CardKind kind)
        {
            if (_canonicalCompanionEligibility != null
                && _canonicalCompanionEligibility.TryGetCandidate(kind, out CanonicalCompanionCardCandidate canonicalCandidate))
            {
                return canonicalCandidate.Change switch
                {
                    PartyRosterChangeResult.Promote => CardHighlight.PromotionReady,
                    PartyRosterChangeResult.Recruit => CardHighlight.New,
                    _ => CardHighlight.None,
                };
            }

            return CardHighlight.None;
        }

        private bool IsNewCompanionCard(CardKind kind)
        {
            if (_canonicalCompanionEligibility != null
                && _canonicalCompanionEligibility.TryGetCandidate(kind, out CanonicalCompanionCardCandidate canonicalCandidate))
            {
                return canonicalCandidate.Change == PartyRosterChangeResult.Recruit;
            }

            return false;
        }

    }
}
