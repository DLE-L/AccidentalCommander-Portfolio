using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Lizzo.PV.UI;

namespace Lizzo.PV.Gameplay.Pause
{
    [DisallowMultipleComponent]
    public sealed class GameplayPausePassiveSlotView : MonoBehaviour
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
        private TMP_Text _levelText;

        public RectTransform Visual => _visual;
        public RectTransform Content => _content;

        public bool Validate()
        {
            return _visual != null
                && _content != null
                && _emptyState != null
                && _filledState != null
                && _icon != null
                && _levelText != null;
        }

        public void Present(bool occupied, PausePassivePresentation presentation)
        {
            _emptyState.SetActive(!occupied);
            _filledState.SetActive(occupied);
            bool hasIcon = occupied && presentation.Icon != null;
            _icon.enabled = hasIcon;
            _icon.sprite = hasIcon ? presentation.Icon : null;
            _icon.raycastTarget = false;
            bool hasLevel = occupied && presentation.Level > 0;
            _levelText.gameObject.SetActive(hasLevel);
            _levelText.text = hasLevel ? "Lv." + presentation.Level : string.Empty;
            _levelText.raycastTarget = false;
        }
    }
}
