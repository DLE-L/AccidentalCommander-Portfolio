using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lizzo.PV.Gameplay.Feedback
{
    [DisallowMultipleComponent]
    public sealed class GameplayBossWarningView : MonoBehaviour
    {
        private const float FadeOutSeconds = 0.2f;
        private const float PulseFrequency = 7f;

        [SerializeField]
        private CanvasGroup _canvasGroup;

        [SerializeField]
        private TMP_Text _warningText;

        [SerializeField]
        private Image[] _accentImages;

        private CancellationTokenSource _displayCancellation;
        private float[] _authoredAccentAlphas;
        private bool _isConfigured;
        private bool _showEdges;
        private bool _suspended;

        public bool IsVisible => gameObject.activeSelf && _canvasGroup != null && _canvasGroup.alpha > 0f;
        public bool IsSuspended => _suspended;

        public bool Configure()
        {
            if (_canvasGroup == null
                || _warningText == null
                || _accentImages == null
                || _accentImages.Length == 0)
            {
                Debug.LogError("[GameplayBossWarningView] Authored warning references are required.", this);
                return false;
            }

            for (int i = 0; i < _accentImages.Length; i++)
            {
                if (_accentImages[i] == null)
                {
                    Debug.LogError("[GameplayBossWarningView] Authored accent Image references are required.", this);
                    return false;
                }
            }

            if (!_isConfigured)
            {
                _authoredAccentAlphas = new float[_accentImages.Length];
                for (int i = 0; i < _accentImages.Length; i++)
                    _authoredAccentAlphas[i] = _accentImages[i].color.a;

                _isConfigured = true;
                Hide();
            }

            return true;
        }

        public bool Show(
            string text,
            Color accent,
            float durationSeconds,
            bool showEdges)
        {
            if (!Configure())
                return false;

            _warningText.text = text ?? string.Empty;
            for (int i = 0; i < _accentImages.Length; i++)
            {
                Color tint = accent;
                tint.a = _authoredAccentAlphas[i];
                _accentImages[i].color = tint;
                _accentImages[i].gameObject.SetActive(showEdges && !_suspended);
            }

            float duration = Mathf.Max(0f, durationSeconds);
            if (duration <= 0f)
            {
                Hide();
                return true;
            }

            CancelDisplay();
            gameObject.SetActive(true);
            _showEdges = showEdges;
            _canvasGroup.alpha = _suspended ? 0f : 1f;
            _displayCancellation = new CancellationTokenSource();
            RunDisplayAsync(duration, _displayCancellation.Token).Forget();
            return true;
        }

        public void SetSuspended(bool suspended)
        {
            if (_suspended == suspended)
                return;

            _suspended = suspended;
            if (!gameObject.activeSelf || _displayCancellation == null || _canvasGroup == null)
                return;

            if (_suspended)
            {
                _canvasGroup.alpha = 0f;
                SetEdgeVisibility(false);
                return;
            }

            _canvasGroup.alpha = 1f;
            SetEdgeVisibility(_showEdges);
        }

        public void Hide()
        {
            CancelDisplay();
            SetEdgeVisibility(false);
            _showEdges = false;

            if (_canvasGroup != null)
                _canvasGroup.alpha = 0f;

            if (gameObject.activeSelf)
                gameObject.SetActive(false);
        }

        private async UniTask RunDisplayAsync(float durationSeconds, CancellationToken cancellationToken)
        {
            float timeRemaining = durationSeconds;
            float pulseTime = 0f;
            float lastTimestamp = Time.realtimeSinceStartup;

            try
            {
                while (timeRemaining > 0f)
                {
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);

                    if (cancellationToken.IsCancellationRequested || _canvasGroup == null)
                        return;

                    float timestamp = Time.realtimeSinceStartup;
                    float delta = Mathf.Max(0f, timestamp - lastTimestamp);
                    lastTimestamp = timestamp;
                    if (_suspended)
                        continue;

                    timeRemaining -= delta;
                    pulseTime += delta * PulseFrequency;
                    if (timeRemaining <= 0f)
                    {
                        Hide();
                        return;
                    }

                    float fade = Mathf.Clamp01(timeRemaining / FadeOutSeconds);
                    float pulse = 0.82f + Mathf.Sin(pulseTime) * 0.18f;
                    _canvasGroup.alpha = fade * pulse;
                }
            }
            catch (OperationCanceledException)
            {
            }
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

        private void LateUpdate()
        {
            if (_suspended && _canvasGroup != null && _canvasGroup.alpha != 0f)
                _canvasGroup.alpha = 0f;
        }

        private void CancelDisplay()
        {
            if (_displayCancellation == null)
                return;

            _displayCancellation.Cancel();
            _displayCancellation.Dispose();
            _displayCancellation = null;
        }

        private void OnDisable()
        {
            CancelDisplay();
        }

        private void OnDestroy()
        {
            CancelDisplay();
        }
    }
}
