using Lizzo.PV.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lizzo.PV.Gameplay.Pause
{
    [DisallowMultipleComponent]
    public sealed class GameplayPauseSynergyItemView : MonoBehaviour
    {
        [SerializeField]
        private RectTransform _visual;

        [SerializeField]
        private RectTransform _content;

        [SerializeField]
        private TMP_Text _nameText;

        [SerializeField]
        private Image _icon;

        public bool Validate()
        {
            return _visual != null && _content != null && _nameText != null && _icon != null;
        }

        public bool Present(PauseSynergyPresentation presentation)
        {
            if (!Validate() || string.IsNullOrWhiteSpace(presentation.DisplayName))
                return false;

            _nameText.text = presentation.DisplayName;
            _nameText.raycastTarget = false;
            _icon.sprite = presentation.Icon;
            _icon.enabled = presentation.Icon != null;
            _icon.raycastTarget = false;
            return true;
        }

        public void Clear()
        {
            if (_nameText != null)
                _nameText.text = string.Empty;
            if (_icon != null)
            {
                _icon.sprite = null;
                _icon.enabled = false;
            }
        }
    }
}
