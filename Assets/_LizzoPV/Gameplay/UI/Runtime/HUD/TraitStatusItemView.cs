using UnityEngine;
using UnityEngine.UI;

namespace Lizzo.PV.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class TraitStatusItemView : MonoBehaviour
    {
        [SerializeField]
        private Image _icon;

        [SerializeField]
        private Image _durationRadial;

        bool _configured;

        public Sprite Icon => _icon != null ? _icon.sprite : null;
        public bool IsDurationVisible => _durationRadial != null && _durationRadial.enabled;
        public float DurationRatio => _durationRadial != null ? _durationRadial.fillAmount : 0.0f;

        public bool Configure()
        {
            if (_configured)
                return true;

            if (_icon == null || _durationRadial == null)
            {
                Debug.LogError("[TraitStatusItemView] Authored icon and duration radial references are required.", this);
                return false;
            }

            _icon.raycastTarget = false;
            _durationRadial.raycastTarget = false;
            _durationRadial.type = Image.Type.Filled;
            _durationRadial.fillMethod = Image.FillMethod.Radial360;
            _durationRadial.fillClockwise = true;
            _configured = true;
            SetInactive();
            return true;
        }

        public void SetPresentation(Sprite icon, bool showDuration, float durationRatio)
        {
            if (!Configure())
                return;

            gameObject.SetActive(true);
            _icon.sprite = icon;
            _icon.enabled = icon != null;
            _durationRadial.enabled = showDuration;
            if (showDuration)
                _durationRadial.fillAmount = Mathf.Clamp01(durationRatio);
        }

        public void SetInactive()
        {
            if (_durationRadial != null)
                _durationRadial.enabled = false;
            gameObject.SetActive(false);
        }
    }
}
