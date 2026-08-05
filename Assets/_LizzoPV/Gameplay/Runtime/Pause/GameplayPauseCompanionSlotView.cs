using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Lizzo.PV.UI;

namespace Lizzo.PV.Gameplay.Pause
{
    [DisallowMultipleComponent]
    public sealed class GameplayPauseCompanionSlotView : MonoBehaviour
    {
        [SerializeField]
        private RectTransform _visual;

        [SerializeField]
        private RectTransform _content;

        [SerializeField]
        private GameObject _emptyState;

        [SerializeField]
        private GameObject _filledState;

        [SerializeField]
        private Image _icon;

        [SerializeField]
        private TMP_Text _countText;

        public RectTransform Visual => _visual;
        public RectTransform Content => _content;

        public bool Validate()
        {
            return _visual != null
                && _content != null
                && _emptyState != null
                && _filledState != null
                && _icon != null
                && _countText != null;
        }

        public void Present(PauseCompanionPresentation presentation)
        {
            bool occupied = presentation.CurrentCount > 0;
            _emptyState.SetActive(!occupied);
            _filledState.SetActive(occupied);
            bool hasIcon = occupied && presentation.Icon != null;
            _icon.enabled = hasIcon;
            _icon.sprite = hasIcon ? presentation.Icon : null;
            _icon.raycastTarget = false;
            _countText.gameObject.SetActive(occupied);
            _countText.text = occupied ? "×" + presentation.CurrentCount : string.Empty;
            _countText.raycastTarget = false;
        }
    }
}
