using Lizzo.PV.UI;
using TMPro;
using UnityEngine;

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

        public bool Validate()
        {
            return _visual != null && _content != null && _nameText != null;
        }

        public bool Present(PauseSynergyPresentation presentation)
        {
            if (!Validate() || string.IsNullOrWhiteSpace(presentation.DisplayName))
                return false;

            _nameText.text = presentation.DisplayName;
            _nameText.raycastTarget = false;
            return true;
        }

        public void Clear()
        {
            if (_nameText != null)
                _nameText.text = string.Empty;
        }
    }
}
