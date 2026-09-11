using System;
using UnityEngine;

namespace Lizzo.PV.Gameplay.CardOffer
{
    [DisallowMultipleComponent]
    public sealed class GameplayCardOfferController : MonoBehaviour
    {
        private const int SlotCount = 3;

        [SerializeField]
        private GameplayCardOfferView _view;

        private readonly string[] _cardIds = new string[SlotCount];
        private bool _hasSelection;

        public event Action<int, string> SelectionDispatched;

        public bool HasSelection => _hasSelection;

        private void Awake()
        {
            if (_view == null)
            {
                Debug.LogError("[GameplayCardOfferController] GameplayCardOfferView is required.", this);
                return;
            }

            _view.SelectionRequested += HandleSelectionRequested;
        }

        private void OnDestroy()
        {
            if (_view != null)
                _view.SelectionRequested -= HandleSelectionRequested;
        }

        public void PresentOfferSlot(int slotIndex, string cardId)
        {
            if (IsValidSlot(slotIndex) == false || string.IsNullOrWhiteSpace(cardId))
            {
                Debug.LogError("[GameplayCardOfferController] A valid slot index and card ID are required.", this);
                return;
            }

            _cardIds[slotIndex] = cardId;
            _view.PresentOfferSlot(slotIndex, cardId);
        }

        public bool PresentOfferSlot(int slotIndex, GameplayCardOfferItemPresentation presentation)
        {
            if (IsValidSlot(slotIndex) == false || string.IsNullOrWhiteSpace(presentation.CardId))
            {
                Debug.LogError("[GameplayCardOfferController] A valid slot index and card presentation are required.", this);
                return false;
            }

            _cardIds[slotIndex] = presentation.CardId;
            return _view.PresentOfferSlot(slotIndex, presentation);
        }

        public void ClearOffer()
        {
            Array.Clear(_cardIds, 0, _cardIds.Length);
            _hasSelection = false;
            _view.ClearOffer();
        }

        public bool TrySelectSlot(int slotIndex)
        {
            if (_hasSelection || IsValidSlot(slotIndex) == false || string.IsNullOrWhiteSpace(_cardIds[slotIndex]))
                return false;

            _hasSelection = true;

            for (int index = 0; index < SlotCount; index++)
                _view.SetItemState(index, index == slotIndex, index != slotIndex, false);

            SelectionDispatched?.Invoke(slotIndex, _cardIds[slotIndex]);
            return true;
        }

        public void RejectSelection()
        {
            _hasSelection = false;
            for (int index = 0; index < SlotCount; index++)
                _view.SetItemState(index, false, false, false);
        }

        public void SetItemState(int slotIndex, bool selected, bool disabled, bool recommended)
        {
            if (IsValidSlot(slotIndex) == false)
            {
                Debug.LogError("[GameplayCardOfferController] Slot index is out of range.", this);
                return;
            }

            _view.SetItemState(slotIndex, selected, disabled, recommended);
        }

        private static bool IsValidSlot(int slotIndex)
        {
            return slotIndex >= 0 && slotIndex < SlotCount;
        }

        private void HandleSelectionRequested(int slotIndex)
        {
            TrySelectSlot(slotIndex);
        }
    }
}
