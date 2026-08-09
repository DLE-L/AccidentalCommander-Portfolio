using System;

namespace Lizzo.PV.P0.Cards.CardOffer
{
    public readonly struct LegacyCardOfferRouteResult
    {
        public LegacyCardOfferRouteResult(CardData[] cards, bool requestBuildCompleteBanner)
        {
            Cards = cards ?? Array.Empty<CardData>();
            RequestBuildCompleteBanner = requestBuildCompleteBanner;
        }

        public CardData[] Cards { get; }
        public bool ShouldPresentOffer => Cards.Length > 0;
        public bool RequestBuildCompleteBanner { get; }
    }

    public static class LegacyCardOfferRoute
    {
        public static LegacyCardOfferRouteResult ResolveNextOffer()
        {
            CardData[] cards = FixedCardPool.GetNextLevelUpCards();
            if (FixedCardPool.MaxBuildComplete)
                return new LegacyCardOfferRouteResult(Array.Empty<CardData>(), FixedCardPool.TryRequestBuildCompleteBanner());

            return new LegacyCardOfferRouteResult(cards, false);
        }
    }
}
