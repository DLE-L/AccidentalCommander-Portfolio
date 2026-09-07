using System;
using Cysharp.Threading.Tasks;
using Lizzo.PV.Gameplay.CardOffer;
using Lizzo.PV.Gameplay.CardOffer;
using Lizzo.PV.Gameplay.CardOffer;
using Lizzo.PV.Gameplay.Presentation;
using Lizzo.PV.UI;
using UnityEngine;

namespace Lizzo.PV.Gameplay.Route
{
    public sealed partial class GameplayRunUiController
    {
        public bool ShowSkillSelection()
        {
            EnsureInitialized();
            CardOfferRouteResult route = CardOfferRoute.ResolveNextOffer(_services.CardOffers);
            if (!route.ShouldPresentOffer)
            {
                if (route.RequestBuildCompleteBanner)
                    MaxBuildCompleteBannerRequested?.Invoke();
                return false;
            }

            CloseActiveModal();

            CardOfferSnapshot snapshot = _services.CardOffers.ActiveCardOfferSnapshot;
            if (snapshot == null || PresentCardOffer(route.Cards, snapshot) == false)
                return false;

            _activeModal = ModalKind.CardOffer;
            _cardOfferController.gameObject.SetActive(true);
            UpdateBossWarningSuspension();
            UpdateInputGate();
            ModalChanged?.Invoke(true);
            CardOfferOpened?.Invoke();
            return true;
        }

        private bool PresentCardOffer(CardData[] cards, CardOfferSnapshot snapshot)
        {
            if (cards == null || cards.Length == 0 || cards.Length > 3 || snapshot.Slots.Count != cards.Length)
                return false;

            _displayedCards = (CardData[])cards.Clone();
            _displayedOfferIdentity = snapshot.OfferIdentity;
            _selectionInProgress = false;
            _cardOfferController.ClearOffer();
            for (int index = 0; index < cards.Length; index++)
            {
                SkillCardPresentationModel presentation = SkillCardPresentationResolver.Resolve(
                    cards[index],
                    _services.Party,
                    _services.CardOffers);
                string value = string.IsNullOrEmpty(presentation.RoleBadge)
                    ? CardPresentation.GetEffectText(cards[index])
                    : presentation.RoleBadge;
                bool showProgress = presentation.IsCompanion || presentation.IsPassive;
                int progressCount = presentation.IsCompanion
                    ? presentation.OwnedCompanionCount
                    : presentation.OwnedPassiveCount;
                GameplayCardOfferItemPresentation item = new GameplayCardOfferItemPresentation(
                    snapshot.Slots[index].CardId,
                    presentation.Title,
                    presentation.Description,
                    value,
                    presentation.StatusText,
                    presentation.Portrait,
                    presentation.SynergyHint,
                    presentation.SynergyIcon,
                    showProgress,
                    progressCount,
                    recommended: false);
                if (!_cardOfferController.PresentOfferSlot(index, item))
                    return false;
            }

            return true;
        }

        private void HandleCardSelection(int slotIndex, string cardId)
        {
            if (_selectionInProgress)
                return;
            if (_activeModal != ModalKind.CardOffer)
                return;
            if (slotIndex < 0 || slotIndex >= _displayedCards.Length)
                return;

            CardOfferSnapshot snapshot = _services.CardOffers.ActiveCardOfferSnapshot;
            if (snapshot == null
                || !string.Equals(snapshot.OfferIdentity, _displayedOfferIdentity, StringComparison.Ordinal)
                || slotIndex >= snapshot.Slots.Count
                || !string.Equals(snapshot.Slots[slotIndex].CardId, cardId, StringComparison.Ordinal))
            {
                return;
            }

            _selectionInProgress = true;
            if (!_services.CardOffers.TrySelect(_displayedCards[slotIndex]))
            {
                _selectionInProgress = false;
                return;
            }

            FinishCardSelectionAsync().Forget();
        }

        private async UniTaskVoid FinishCardSelectionAsync()
        {
            try
            {
                await UniTask.Delay(
                    TimeSpan.FromSeconds(CardSelectionRevealSeconds),
                    DelayType.UnscaledDeltaTime,
                    PlayerLoopTiming.Update,
                    this.GetCancellationTokenOnDestroy());
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (_selectionInProgress)
                CloseModal();
        }

    }
}
