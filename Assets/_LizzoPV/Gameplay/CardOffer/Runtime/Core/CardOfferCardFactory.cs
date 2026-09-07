using Lizzo.PV.Data;
using Lizzo.PV.Legion;

namespace Lizzo.PV.Gameplay.CardOffer
{
    internal sealed class CardOfferCardFactory
    {
        private readonly PartyService _party;
        private readonly CanonicalCompanionCardEligibility _canonicalCompanionEligibility;
        private readonly CanonicalPassiveCardService _canonicalPassiveCards;

        internal CardOfferCardFactory(
            PartyService party,
            CanonicalCompanionCardEligibility canonicalCompanionEligibility,
            CanonicalPassiveCardService canonicalPassiveCards)
        {
            _party = party;
            _canonicalCompanionEligibility = canonicalCompanionEligibility;
            _canonicalPassiveCards = canonicalPassiveCards;
        }

        internal CardData Create(CardKind kind, CardHighlight highlight = CardHighlight.None)
        {
            string canonicalBaseUnitId = null;
            string canonicalPassiveId = null;
            _canonicalCompanionEligibility?.TryGetBaseUnitId(kind, out canonicalBaseUnitId);
            CanonicalPassiveCardService.TryGetPassiveId(kind, out canonicalPassiveId);
            if (string.IsNullOrWhiteSpace(canonicalPassiveId) == false
                && _canonicalPassiveCards.TryGetCandidate(kind, out CanonicalPassiveCardCandidate passiveCandidate))
            {
                PassiveData passive = _canonicalPassiveCards.GetPassiveData(passiveCandidate.PassiveId);
                string title = passive == null ? string.Empty : passive.TitleKo;
                string description = PassiveCardPresentation.FormatCurrentToNext(
                    passive,
                    _canonicalPassiveCards.Roster.GetLevel(passiveCandidate.PassiveId));
                return new CardData(
                    kind,
                    title,
                    description,
                    CardHighlight.None,
                    canonicalBaseUnitId,
                    canonicalPassiveId);
            }

            return new CardData(
                kind,
                ResolveFallbackTitle(kind),
                ResolveFallbackDescription(kind),
                highlight,
                canonicalBaseUnitId,
                canonicalPassiveId);
        }

        private static string ResolveFallbackTitle(CardKind kind)
        {
            return kind.ToString();
        }

        private static string ResolveFallbackDescription(CardKind kind)
        {
            return string.Empty;
        }
    }
}
