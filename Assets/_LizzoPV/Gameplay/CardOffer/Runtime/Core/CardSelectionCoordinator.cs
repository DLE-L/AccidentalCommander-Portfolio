using Lizzo.PV.Combat;
using Lizzo.PV.Gameplay.Units;
using System;
using Lizzo.PV.Gameplay.Telemetry;
using Lizzo.PV.Legion;
using UnityEngine;

namespace Lizzo.PV.Gameplay.CardOffer
{
    internal sealed class CardSelectionCoordinator
    {
        private readonly CardApplicationRouter _applicationRouter;
        private readonly CardIdentityResolver _identityResolver;

        internal CardSelectionCoordinator(
            CardApplicationRouter applicationRouter,
            CardIdentityResolver identityResolver)
        {
            _applicationRouter = applicationRouter ?? throw new ArgumentNullException(nameof(applicationRouter));
            _identityResolver = identityResolver ?? throw new ArgumentNullException(nameof(identityResolver));
        }

        internal bool TrySelect(
            CardData card,
            CardOfferRunState runState,
            int levelUpCount,
            float activeOfferShownAtUnscaledTime)
        {
            _identityResolver.Resolve(
                card,
                out string canonicalBaseUnitId,
                out string canonicalPassiveId);

            CardOfferSnapshot selectedSnapshot = null;
            CardOfferSlot selectedOfferSlot = default;
            CardOfferSnapshot activeSnapshot = runState?.ActiveSnapshot;
            if (activeSnapshot != null)
            {
                int slotIndex = FindSlotIndex(activeSnapshot, card.Kind);
                if (slotIndex < 0
                    || DeterministicCardOfferService.TryCommitSelection(
                        runState,
                        activeSnapshot.OfferIdentity,
                        slotIndex,
                        out CardOfferSlot selectedSlot) == false)
                {
                    return false;
                }

                selectedSnapshot = activeSnapshot;
                selectedOfferSlot = selectedSlot;
            }

            RunTelemetry.Log(
                RunTelemetry.CardSelect,
                RunTelemetry.RunTimeSecondsParameter,
                $"card={card.Kind}",
                $"level_up={levelUpCount}",
                $"highlight={card.Highlight}");
            if (_applicationRouter.TryApply(
                    card,
                    canonicalBaseUnitId,
                    canonicalPassiveId) == false)
            {
                if (selectedSnapshot != null) runState.ReleaseRejectedSelection(selectedSnapshot.OfferIdentity);
                return false;
            }

            if (selectedSnapshot != null)
            {
                RunTelemetry.LogCardOfferSelected(
                    selectedSnapshot,
                    selectedOfferSlot,
                    Mathf.Max(
                        0,
                        Mathf.RoundToInt(
                            (Time.unscaledTime - activeOfferShownAtUnscaledTime) * 1000.0f)),
                    false);
            }

            return true;
        }

        private static int FindSlotIndex(CardOfferSnapshot snapshot, CardKind kind)
        {
            for (int index = 0; index < snapshot.Slots.Count; index += 1)
            {
                if (snapshot.Slots[index].Kind == kind)
                    return index;
            }

            return -1;
        }
    }
}
