using System;
using UnityEngine;

namespace Lizzo.PV.Gameplay.CardOffer
{
    [DisallowMultipleComponent]
    public sealed class GameplayCardOfferView : MonoBehaviour
    {
        [SerializeField]
        private RectTransform _header;

        [SerializeField]
        private RectTransform _cardRow;

        [SerializeField]
        private RectTransform _modalInputBlocker;

        [SerializeField]
        private GameplayCardOfferItemView _cardItem01;

        [SerializeField]
        private GameplayCardOfferItemView _cardItem02;

        [SerializeField]
        private GameplayCardOfferItemView _cardItem03;

        public event Action<int> SelectionRequested;

        public void PresentOfferSlot(int slotIndex, string cardId)
        {
            if (TryGetItem(slotIndex, out GameplayCardOfferItemView item) == false)
            {
                Debug.LogError("[GameplayCardOfferView] Slot index is out of range.", this);
                return;
            }

            item.Present(cardId);
        }

        public bool PresentOfferSlot(int slotIndex, GameplayCardOfferItemPresentation presentation)
        {
            if (TryGetItem(slotIndex, out GameplayCardOfferItemView item) == false)
            {
                Debug.LogError("[GameplayCardOfferView] Slot index is out of range.", this);
                return false;
            }

            return item.Present(presentation);
        }

        public void ClearOffer()
        {
            for (int index = 0; index < 3; index++)
            {
                if (TryGetItem(index, out GameplayCardOfferItemView item))
                    item.Clear();
            }
        }

        public void SetItemState(int slotIndex, bool selected, bool disabled, bool recommended)
        {
            if (TryGetItem(slotIndex, out GameplayCardOfferItemView item) == false)
            {
                Debug.LogError("[GameplayCardOfferView] Slot index is out of range.", this);
                return;
            }

            item.SetState(selected, disabled, recommended);
        }

        private void Awake()
        {
            if (_header == null
                || _cardRow == null
                || _modalInputBlocker == null
                || _cardItem01 == null
                || _cardItem02 == null
                || _cardItem03 == null)
            {
                Debug.LogError("[GameplayCardOfferView] Header, CardRow, ModalInputBlocker, and all three card item references are required.", this);
                return;
            }

            RegisterItem(_cardItem01);
            RegisterItem(_cardItem02);
            RegisterItem(_cardItem03);
        }

        private void OnDestroy()
        {
            UnregisterItem(_cardItem01);
            UnregisterItem(_cardItem02);
            UnregisterItem(_cardItem03);
        }

        private void RegisterItem(GameplayCardOfferItemView item)
        {
            item.SelectionRequested += HandleItemSelection;
        }

        private void UnregisterItem(GameplayCardOfferItemView item)
        {
            if (item != null)
                item.SelectionRequested -= HandleItemSelection;
        }

        private void HandleItemSelection(int slotIndex)
        {
            SelectionRequested?.Invoke(slotIndex);
        }

        private bool TryGetItem(int slotIndex, out GameplayCardOfferItemView item)
        {
            item = slotIndex switch
            {
                0 => _cardItem01,
                1 => _cardItem02,
                2 => _cardItem03,
                _ => null,
            };
            return item != null;
        }
    }
}
