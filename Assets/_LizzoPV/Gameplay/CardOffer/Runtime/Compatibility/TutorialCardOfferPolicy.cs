using System;
using System.Collections.Generic;
using Lizzo.PV.Flow;

namespace Lizzo.PV.P0.Cards
{
    internal sealed class TutorialCardOfferPolicy
    {
        private static readonly CardKind[] FirstRunRoute =
        {
            CardKind.AddShieldSoldier,
            CardKind.AddShieldSoldier,
            CardKind.AddShieldSoldier,
            CardKind.RecruitSwordsman,
            CardKind.RecruitCleric,
        };

        private readonly RunContext _context;

        internal TutorialCardOfferPolicy(RunContext context)
        {
            _context = context;
        }

        internal bool TryGetRequiredCardData(
            int levelUpCount,
            CardData[] cards,
            out CardData requiredCard)
        {
            requiredCard = default;

            if (TryGetRequiredCardKind(levelUpCount, out CardKind requiredKind) == false
                || cards == null)
            {
                return false;
            }

            for (int i = 0; i < cards.Length; i++)
            {
                if (cards[i].Kind != requiredKind)
                    continue;

                requiredCard = cards[i];
                return true;
            }

            return false;
        }

        internal bool IsOffRouteCard(int levelUpCount, CardData card)
        {
            return TryGetRequiredCardKind(levelUpCount, out CardKind requiredKind)
                && card.Kind != requiredKind;
        }

        internal bool TryAddRequiredCardKind(
            int levelUpCount,
            List<CardKind> selectedKinds,
            CardKind[] excludedKinds,
            Func<CardKind, bool> canCardAppear,
            ref bool filtered)
        {
            if (selectedKinds == null
                || TryGetRequiredCardKind(levelUpCount, out CardKind requiredKind) == false)
            {
                return false;
            }

            if (selectedKinds.Contains(requiredKind))
                return false;

            if (ContainsKind(excludedKinds, requiredKind))
            {
                filtered = true;
                return false;
            }

            if (canCardAppear(requiredKind) == false)
            {
                filtered = true;
                return false;
            }

            selectedKinds.Add(requiredKind);
            return true;
        }

        private bool TryGetRequiredCardKind(int levelUpCount, out CardKind requiredKind)
        {
            requiredKind = default;

            if (_context.IsTutorial == false
                || Lizzo.PV.P0.Config.RemoteConfig.TutorialAssistEnabled == false)
            {
                return false;
            }

            int routeIndex = levelUpCount - 1;
            if (routeIndex < 0 || routeIndex >= FirstRunRoute.Length)
                return false;

            requiredKind = FirstRunRoute[routeIndex];
            return true;
        }

        private static bool ContainsKind(CardKind[] kinds, CardKind candidate)
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
