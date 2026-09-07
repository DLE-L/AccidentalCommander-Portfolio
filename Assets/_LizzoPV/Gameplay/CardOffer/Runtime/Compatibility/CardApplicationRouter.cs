using System;
using Lizzo.PV.Legion.RunCore;

namespace Lizzo.PV.P0.Cards
{
    internal sealed class CardApplicationRouter
    {
        private readonly CanonicalPassiveCardService _passiveCards;
        private readonly ICompanionCardInput _companionCardInput;
        private readonly bool _enforceCurrentProductCardPolicy;
        private long _companionCardSequence;

        internal CardApplicationRouter(
            CanonicalPassiveCardService passiveCards,
            ICompanionCardInput companionCardInput,
            bool enforceCurrentProductCardPolicy)
        {
            _passiveCards = passiveCards;
            _companionCardInput = companionCardInput;
            _enforceCurrentProductCardPolicy = enforceCurrentProductCardPolicy;
        }

        internal void Reset()
        {
            _companionCardSequence = 0L;
        }

        internal bool TryApply(
            CardData card,
            string canonicalBaseUnitId,
            string canonicalPassiveId)
        {
            if (_enforceCurrentProductCardPolicy
                && CardOfferPoolResolver.IsCurrentProductCardAvailable(card.Kind) == false)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(canonicalPassiveId) == false)
                return _passiveCards != null && _passiveCards.TryApply(canonicalPassiveId, out _);

            if (string.IsNullOrWhiteSpace(canonicalBaseUnitId) == false)
            {
                return TryApplyCanonicalCompanion(canonicalBaseUnitId);
            }

            return false;
        }

        internal bool TryApplyCanonicalCompanion(string canonicalBaseUnitId)
        {
            if (_companionCardInput == null
                || string.IsNullOrWhiteSpace(canonicalBaseUnitId)
                || _companionCardSequence == long.MaxValue)
            {
                return false;
            }

            _companionCardSequence += 1L;
            return _companionCardInput.SubmitCard(
                _companionCardSequence,
                canonicalBaseUnitId).Accepted;
        }
    }
}
