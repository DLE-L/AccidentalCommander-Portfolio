using Lizzo.PV.P0.Cards.CardOffer;
using Lizzo.PV.P0.Telemetry;
using Lizzo.PV.Legion;
using UnityEngine;

namespace Lizzo.PV.P0.Cards
{
    internal sealed class CardSelectionCoordinator
    {
        private readonly RuntimeObjectRegistry _registry;
        private readonly CardApplicationRouter _applicationRouter;

        internal CardSelectionCoordinator(
            RuntimeObjectRegistry registry,
            CardApplicationRouter applicationRouter)
        {
            _registry = registry;
            _applicationRouter = applicationRouter;
        }

        internal bool TrySelect(
            CardData card,
            string canonicalBaseUnitId,
            string canonicalPassiveId,
            CardOfferRunState runState,
            int levelUpCount,
            float activeOfferShownAtUnscaledTime)
        {
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

            P0Telemetry.Log(
                P0Telemetry.CardSelect,
                P0Telemetry.RunTimeSecondsParameter,
                $"card={card.Kind}",
                $"level_up={levelUpCount}",
                $"highlight={card.Highlight}");
            if (_registry?.Player != null)
            {
                RetroVfx.Spawn(
                    RetroVfxKind.CardSelect,
                    _registry.Player.transform.position,
                    Vector3.zero,
                    1.0f);
            }

            if (_applicationRouter == null
                || _applicationRouter.TryApply(
                    card,
                    canonicalBaseUnitId,
                    canonicalPassiveId) == false)
            {
                return false;
            }

            if (selectedSnapshot != null)
            {
                P0Telemetry.LogCardOfferSelected(
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
