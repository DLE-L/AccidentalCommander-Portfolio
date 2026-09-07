using System;

namespace Lizzo.PV.Gameplay.CardOffer
{
    public readonly struct CardOfferRouteResult
    {
        public CardOfferRouteResult(CardData[] cards, bool requestBuildCompleteBanner)
        {
            Cards = cards ?? Array.Empty<CardData>();
            RequestBuildCompleteBanner = requestBuildCompleteBanner;
        }

        public CardData[] Cards { get; }
        public bool ShouldPresentOffer => Cards.Length > 0;
        public bool RequestBuildCompleteBanner { get; }
    }

    public static class CardOfferRoute
    {
        public static CardOfferRouteResult ResolveNextOffer(CardOfferRuntime cardOffers)
        {
            if (cardOffers == null)
                throw new ArgumentNullException(nameof(cardOffers));

            CardData[] cards = cardOffers.GetNextLevelUpCards();
            if (cardOffers.MaxBuildComplete)
                return new CardOfferRouteResult(Array.Empty<CardData>(), cardOffers.TryRequestBuildCompleteBanner());

            return new CardOfferRouteResult(cards, false);
        }
    }
}
