using System;
using Lizzo.PV.Legion;
using Lizzo.PV.Legion.RunCore;

namespace Lizzo.PV.P0.Cards
{
    internal sealed class CardApplicationRouter
    {
        private readonly PartyService _party;
        private readonly CanonicalPassiveCardService _passiveCards;
        private readonly ICompanionCardInput _companionCardInput;
        private long _companionCardSequence;

        internal CardApplicationRouter(
            PartyService party,
            CanonicalPassiveCardService passiveCards,
            ICompanionCardInput companionCardInput)
        {
            _party = party;
            _passiveCards = passiveCards;
            _companionCardInput = companionCardInput;
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
            if (string.IsNullOrWhiteSpace(canonicalPassiveId) == false)
                return _passiveCards != null && _passiveCards.TryApply(canonicalPassiveId, out _);

            if (_companionCardInput != null)
            {
                if (string.IsNullOrWhiteSpace(canonicalBaseUnitId))
                {
                    bool isCompanionCard = CardCompanionKindResolver.TryResolve(card.Kind, out _);
                    return isCompanionCard == false && TryApplyNonCompanion(card);
                }

                if (_companionCardSequence == long.MaxValue)
                    return false;

                _companionCardSequence += 1L;
                return _companionCardInput.SubmitCard(
                    _companionCardSequence,
                    canonicalBaseUnitId).Accepted;
            }

            if (TryResolveCompatibilityKind(canonicalBaseUnitId, out CompanionKind compatibilityKind))
            {
                if (_party == null)
                    return false;

                _party.RecruitFromCard(compatibilityKind);
                return true;
            }

            if (string.IsNullOrWhiteSpace(canonicalBaseUnitId) == false)
                return _party != null && _party.RecruitCanonicalFromCard(canonicalBaseUnitId);

            if (CardCompanionKindResolver.TryResolve(card.Kind, out CompanionKind companionKind))
            {
                if (_party == null)
                    return false;

                _party.RecruitFromCard(companionKind);
                return true;
            }

            return TryApplyNonCompanion(card);
        }

        private static bool TryApplyNonCompanion(CardData card)
        {
            if (CardEffectRuntime.IsPassiveCard(card.Kind)
                && CardEffectRuntime.CanAcquirePassive(card.Kind) == false)
            {
                return false;
            }

            return CardEffectRuntime.TryApply(card.Kind);
        }

        private static bool TryResolveCompatibilityKind(
            string canonicalBaseUnitId,
            out CompanionKind companionKind)
        {
            switch (canonicalBaseUnitId)
            {
                case "shield_guard":
                    companionKind = CompanionKind.ShieldSoldier;
                    return true;
                case "sword_soldier":
                    companionKind = CompanionKind.Swordsman;
                    return true;
                case "cleric":
                    companionKind = CompanionKind.Cleric;
                    return true;
                default:
                    companionKind = default;
                    return false;
            }
        }
    }
}
