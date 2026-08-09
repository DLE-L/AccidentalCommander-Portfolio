using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lizzo.PV.Gameplay.Feedback
{
    [DisallowMultipleComponent]
    public sealed class GameplayThreatDirectionView : MonoBehaviour
    {
        [SerializeField]
        private RectTransform _root;

        [SerializeField]
        private RectTransform _visualRoot;

        [SerializeField]
        private RectTransform _contentRoot;

        [SerializeField]
        private RectTransform _arrowIconRoot;

        [SerializeField]
        private Image _backgroundImage;

        [SerializeField]
        private Image _iconImage;

        [SerializeField]
        private TMP_Text _labelText;

        [SerializeField, Min(0f)]
        private float _horizontalEdgeMargin = 32f;

        [SerializeField, Min(0f)]
        private float _verticalEdgeMargin = 32f;

        private CancellationTokenSource _trackingCancellation;
        private RectTransform _viewport;
        private Camera _worldCamera;
        private Transform _target;
        private Sprite _authoredIconSprite;
        private bool _authoredIconEnabled;
        private bool _isConfigured;
        private bool _isVisible;

        public bool IsVisible => _isVisible;

        public bool Configure(RectTransform viewport, Camera worldCamera)
        {
            if (_root == null
                || _visualRoot == null
                || _contentRoot == null
                || _arrowIconRoot == null
                || _backgroundImage == null
                || _iconImage == null
                || viewport == null
                || worldCamera == null)
            {
                Debug.LogError("[GameplayThreatDirectionView] Authored threat direction references are required; LabelText is optional.", this);
                return false;
            }

            _viewport = viewport;
            _worldCamera = worldCamera;
            if (!_isConfigured)
            {
                _authoredIconSprite = _iconImage.sprite;
                _authoredIconEnabled = _iconImage.enabled;
                _isConfigured = true;
                Hide();
            }

            return true;
        }

        public bool Show(
            Transform target,
            Sprite icon,
            string label,
            Color accent,
            float durationSeconds)
        {
            if (!_isConfigured || target == null)
            {
                Hide();
                return false;
            }

            CancelTracking();
            _target = target;
            _iconImage.sprite = icon != null ? icon : _authoredIconSprite;
            _iconImage.enabled = icon != null || _authoredIconEnabled;
            _backgroundImage.color = accent;
            if (_labelText != null)
                _labelText.text = label ?? string.Empty;

            gameObject.SetActive(true);
            _trackingCancellation = new CancellationTokenSource();
            UpdatePresentation();
            if (_target == null)
                return false;

            TrackTargetAsync(durationSeconds, _trackingCancellation.Token).Forget();
            return true;
        }

        public void Hide()
        {
            CancelTracking();
            _target = null;
            SetVisible(false);

            if (gameObject.activeSelf)
                gameObject.SetActive(false);
        }

        private async UniTask TrackTargetAsync(float durationSeconds, CancellationToken cancellationToken)
        {
            float timeRemaining = durationSeconds <= 0f ? float.PositiveInfinity : durationSeconds;
            float lastTimestamp = Time.realtimeSinceStartup;

            try
            {
                while (true)
                {
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);

                    if (this == null)
                        return;

                    if (_target == null)
                    {
                        HideDestroyedTarget();
                        return;
                    }

                    if (!float.IsPositiveInfinity(timeRemaining))
                    {
                        float timestamp = Time.realtimeSinceStartup;
                        timeRemaining -= Mathf.Max(0f, timestamp - lastTimestamp);
                        lastTimestamp = timestamp;
                        if (timeRemaining <= 0f)
                        {
                            Hide();
                            return;
                        }
                    }

                    UpdatePresentation();
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        private void HideDestroyedTarget()
        {
            _target = null;
            SetVisible(false);

            if (gameObject.activeSelf)
                gameObject.SetActive(false);
        }

        private void UpdatePresentation()
        {
            if (_target == null || _viewport == null || _worldCamera == null)
            {
                SetVisible(false);
                return;
            }

            Vector3 viewportPoint = _worldCamera.WorldToViewportPoint(_target.position);
            bool behindCamera = viewportPoint.z <= 0f;
            bool onScreen = !behindCamera
                && viewportPoint.x > 0f
                && viewportPoint.x < 1f
                && viewportPoint.y > 0f
                && viewportPoint.y < 1f;
            if (onScreen)
            {
                SetVisible(false);
                return;
            }

            SetVisible(true);
            PositionAtViewportEdge(viewportPoint);
        }

        private void PositionAtViewportEdge(Vector3 viewportPoint)
        {
            Rect viewport = _viewport.rect;
            float minX = viewport.xMin + _horizontalEdgeMargin;
            float maxX = viewport.xMax - _horizontalEdgeMargin;
            float minY = viewport.yMin + _verticalEdgeMargin;
            float maxY = viewport.yMax - _verticalEdgeMargin;
            if (minX > maxX)
                minX = maxX = viewport.center.x;
            if (minY > maxY)
                minY = maxY = viewport.center.y;

            Vector2 direction = new Vector2(viewportPoint.x - 0.5f, viewportPoint.y - 0.5f);
            if (viewportPoint.z <= 0f)
                direction = -direction;
            if (direction.sqrMagnitude < 0.0001f)
                direction = Vector2.up;

            float x = Mathf.Clamp(viewport.center.x + direction.x * viewport.width, minX, maxX);
            float y = Mathf.Clamp(viewport.center.y + direction.y * viewport.height, minY, maxY);
            _root.position = _viewport.TransformPoint(new Vector2(x, y));

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            _arrowIconRoot.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void SetVisible(bool visible)
        {
            _isVisible = visible;
            if (_visualRoot != null)
                _visualRoot.gameObject.SetActive(visible);
            if (_contentRoot != null)
                _contentRoot.gameObject.SetActive(visible);
        }

        private void CancelTracking()
        {
            if (_trackingCancellation == null)
                return;

            _trackingCancellation.Cancel();
            _trackingCancellation.Dispose();
            _trackingCancellation = null;
        }

        private void OnDisable()
        {
            CancelTracking();
        }

        private void OnDestroy()
        {
            CancelTracking();
        }
    }
}
