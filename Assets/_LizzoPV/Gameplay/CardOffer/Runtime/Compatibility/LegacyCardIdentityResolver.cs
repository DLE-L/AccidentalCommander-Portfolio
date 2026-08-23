namespace Lizzo.PV.P0.Cards
{
    internal sealed class LegacyCardIdentityResolver
    {
        private readonly CanonicalCompanionCardEligibility _canonicalCompanionEligibility;
        private readonly CanonicalPassiveCardService _canonicalPassiveCards;

        internal LegacyCardIdentityResolver(
            CanonicalCompanionCardEligibility canonicalCompanionEligibility,
            CanonicalPassiveCardService canonicalPassiveCards)
        {
            _canonicalCompanionEligibility = canonicalCompanionEligibility;
            _canonicalPassiveCards = canonicalPassiveCards;
        }

        internal void Resolve(
            CardData card,
            out string canonicalBaseUnitId,
            out string canonicalPassiveId)
        {
            canonicalBaseUnitId = card.CanonicalBaseUnitId;
            canonicalPassiveId = card.CanonicalPassiveId;

            if (string.IsNullOrWhiteSpace(canonicalBaseUnitId)
                && _canonicalCompanionEligibility != null)
            {
                _canonicalCompanionEligibility.TryGetBaseUnitId(
                    card.Kind,
                    out canonicalBaseUnitId);
            }

            if (string.IsNullOrWhiteSpace(canonicalPassiveId)
                && _canonicalPassiveCards != null)
            {
                CanonicalPassiveCardService.TryGetPassiveId(
                    card.Kind,
                    out canonicalPassiveId);
            }
        }
    }
}
