using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lizzo.PV.UI
{
    public sealed class UI_BossWarningOverlay : MonoBehaviour
    {
        private const float FadeOutSeconds = 0.2f;
        private const float PulseFrequency = 7f;

        [SerializeField] private GameObject _root;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private TMP_Text _warningText;
        [SerializeField] private Image[] _accentImages;

        private float _timeRemaining;
        private float _pulseTime;
        private float[] _authoredAccentAlphas;
        private bool _isVisible;
        private bool _isInitialized;

        public bool Init()
        {
            if (_isInitialized)
                return true;

            if (!Validate())
                return false;

            _authoredAccentAlphas = new float[_accentImages.Length];
            for (int i = 0; i < _accentImages.Length; i++)
                _authoredAccentAlphas[i] = _accentImages[i].color.a;

            Hide();
            _isInitialized = true;
            return true;
        }

        public bool Validate()
        {
            if (_root == null
                || _canvasGroup == null
                || _warningText == null
                || _accentImages == null
                || _accentImages.Length == 0)
            {
                Debug.LogError("[UI_BossWarningOverlay] Required authored warning or accent Image references are incomplete.", this);
                return false;
            }

            for (int i = 0; i < _accentImages.Length; i++)
            {
                if (_accentImages[i] == null)
                {
                    Debug.LogError("[UI_BossWarningOverlay] Required authored accent Image references are incomplete.", this);
                    return false;
                }
            }

            return true;
        }

        public void Show(
            string text,
            Color accent,
            float duration,
            bool showEdges)
        {
            if (!_isInitialized)
                return;

            _warningText.text = text ?? string.Empty;
            for (int i = 0; i < _accentImages.Length; i++)
            {
                Color tint = accent;
                tint.a = _authoredAccentAlphas[i];
                _accentImages[i].color = tint;
            }

            SetEdgeVisibility(showEdges);
            _timeRemaining = Mathf.Max(0f, duration);
            _pulseTime = 0f;
            _isVisible = _timeRemaining > 0f;
            _root.SetActive(_isVisible);
            _canvasGroup.alpha = _isVisible ? 1f : 0f;
        }

        public void Hide()
        {
            _timeRemaining = 0f;
            _pulseTime = 0f;
            _isVisible = false;

            SetEdgeVisibility(false);
            if (_canvasGroup != null)
                _canvasGroup.alpha = 0f;
            if (_root != null)
                _root.SetActive(false);
        }

        private void Update()
        {
            if (!_isVisible)
                return;

            _timeRemaining -= Time.unscaledDeltaTime;
            _pulseTime += Time.unscaledDeltaTime * PulseFrequency;
            if (_timeRemaining <= 0f)
            {
                Hide();
                return;
            }

            float fade = Mathf.Clamp01(_timeRemaining / FadeOutSeconds);
            float pulse = 0.82f + Mathf.Sin(_pulseTime) * 0.18f;
            _canvasGroup.alpha = fade * pulse;
        }

        private void SetEdgeVisibility(bool visible)
        {
            if (_accentImages == null)
                return;

            for (int i = 0; i < _accentImages.Length; i++)
            {
                if (_accentImages[i] != null)
                    _accentImages[i].gameObject.SetActive(visible);
            }
        }
    }
}
