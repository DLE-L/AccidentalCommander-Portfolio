using System;
using Cysharp.Threading.Tasks;
using Lizzo.PV.Gameplay.CardOffer;
using Lizzo.PV.Gameplay.RunTraits;
using Lizzo.PV.P0.Cards;
using Lizzo.PV.P0.Cards.CardOffer;
using Lizzo.PV.P0.Presentation;
using Lizzo.PV.UI;
using UnityEngine;

namespace Lizzo.PV.Gameplay.Route
{
    public sealed partial class GameplayRunUiController
    {
        public bool ShowSkillSelection()
        {
            EnsureInitialized();
            LegacyCardOfferRouteResult route = LegacyCardOfferRoute.ResolveNextOffer();
            if (!route.ShouldPresentOffer)
            {
                if (route.RequestBuildCompleteBanner)
                    MaxBuildCompleteBannerRequested?.Invoke();
                return false;
            }

            CloseActiveModal();

            CardOfferSnapshot snapshot = FixedCardPool.ActiveCardOfferSnapshot;
            if (snapshot == null || PresentCardOffer(route.Cards, snapshot) == false)
                return false;

            _activeModal = ModalKind.CardOffer;
            _cardOfferController.gameObject.SetActive(true);
            UpdateInputGate();
            ModalChanged?.Invoke(true);
            return true;
        }

        public bool ShowRunTraitOffer(RunTraitOfferSnapshot snapshot, Func<string, int, string, bool> selectionRequested)
        {
            EnsureInitialized();
            if (snapshot == null || snapshot.Slots.Count < 2 || snapshot.Slots.Count > 3 || selectionRequested == null
                || _activeModal != ModalKind.None || _pauseOverlayVisible)
                return false;

            if (PresentTraitOffer(snapshot) == false)
                return false;

            _displayedTraitOffer = snapshot;
            _traitOfferSelectionRequested = selectionRequested;
            _activeModal = ModalKind.TraitOffer;
            _cardOfferController.gameObject.SetActive(true);
            UpdateInputGate();
            ModalChanged?.Invoke(true);
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
                SkillCardPresentationModel presentation = SkillCardPresentationResolver.Resolve(cards[index], _services.Party);
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
                    showProgress,
                    progressCount,
                    presentation.Recommended || presentation.HighlightFrame);
                if (!_cardOfferController.PresentOfferSlot(index, item))
                    return false;
            }

            return true;
        }

        private bool PresentTraitOffer(RunTraitOfferSnapshot snapshot)
        {
            _displayedCards = Array.Empty<CardData>();
            _displayedOfferIdentity = string.Empty;
            _selectionInProgress = false;
            _cardOfferController.ClearOffer();
            for (int index = 0; index < snapshot.Slots.Count; index++)
            {
                RunTraitOfferSlot slot = snapshot.Slots[index];
                if (slot.SlotIndex != index || RunTraitCatalog.TryGet(slot.TraitId, out RunTraitDefinition trait) == false)
                {
                    _cardOfferController.ClearOffer();
                    return false;
                }

                if (_runTraitPresentationCatalog == null
                    || _runTraitPresentationCatalog.TryResolve(trait.Id, out Sprite traitIcon) == false)
                {
                    _cardOfferController.ClearOffer();
                    return false;
                }

                GameplayCardOfferItemPresentation item = new GameplayCardOfferItemPresentation(
                    trait.Id,
                    trait.DisplayName,
                    trait.Description,
                    ToKoreanCategory(trait.Category),
                    "이번 출정 한정",
                    traitIcon,
                    trait.RelatedBuild,
                    showProgress: false,
                    progressCount: 0,
                    recommended: false);
                if (_cardOfferController.PresentOfferSlot(index, item) == false)
                {
                    _cardOfferController.ClearOffer();
                    return false;
                }
            }

            return true;
        }

        private void HandleCardSelection(int slotIndex, string cardId)
        {
            if (_selectionInProgress)
                return;
            if (_activeModal == ModalKind.TraitOffer)
            {
                HandleTraitOfferSelection(slotIndex, cardId);
                return;
            }
            if (_activeModal != ModalKind.CardOffer)
                return;
            if (slotIndex < 0 || slotIndex >= _displayedCards.Length)
                return;

            CardOfferSnapshot snapshot = FixedCardPool.ActiveCardOfferSnapshot;
            if (snapshot == null
                || !string.Equals(snapshot.OfferIdentity, _displayedOfferIdentity, StringComparison.Ordinal)
                || slotIndex >= snapshot.Slots.Count
                || !string.Equals(snapshot.Slots[slotIndex].CardId, cardId, StringComparison.Ordinal))
            {
                return;
            }

            _selectionInProgress = true;
            if (!FixedCardPool.TrySelect(_displayedCards[slotIndex]))
            {
                _selectionInProgress = false;
                return;
            }

            FinishCardSelectionAsync().Forget();
        }

        private void HandleTraitOfferSelection(int slotIndex, string traitId)
        {
            if (_displayedTraitOffer == null || slotIndex < 0 || slotIndex >= _displayedTraitOffer.Slots.Count)
                return;

            RunTraitOfferSlot slot = _displayedTraitOffer.Slots[slotIndex];
            if (string.Equals(slot.TraitId, traitId, StringComparison.Ordinal) == false
                || _traitOfferSelectionRequested == null
                || _traitOfferSelectionRequested(_displayedTraitOffer.OfferIdentity, slotIndex, traitId) == false)
                return;

            _selectionInProgress = true;
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

        private static string ToKoreanCategory(string category)
        {
            if (string.Equals(category, RunTraitCategories.BuildRelated, StringComparison.Ordinal))
                return "빌드 연계";
            if (string.Equals(category, RunTraitCategories.General, StringComparison.Ordinal))
                return "일반";
            return "변칙";
        }

    }
}
