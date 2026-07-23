using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lizzo.PV.UI
{
    public sealed class UI_ThreatDirectionIndicator : MonoBehaviour
    {
        [SerializeField] private RectTransform _root;
        [SerializeField] private RectTransform _arrowIconRoot;
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private Image _iconImage;
        [SerializeField] private TMP_Text _labelText;
        [SerializeField, Min(0f)] private float _horizontalEdgeMargin = 32f;
        [SerializeField, Min(0f)] private float _verticalEdgeMargin = 32f;

        private RectTransform _viewportRect;
        private Camera _worldCamera;
        private Transform _target;
        private Sprite _authoredIconSprite;
        private bool _authoredIconEnabled;
        private float _timeRemaining;
        private bool _isConfigured;
        private bool _isInitialized;

        public bool IsVisible => _root != null && _root.gameObject.activeSelf;

        public bool Init()
        {
            if (_isInitialized)
                return true;

            if (!Validate())
                return false;

            _authoredIconSprite = _iconImage.sprite;
            _authoredIconEnabled = _iconImage.enabled;
            Hide();
            _isInitialized = true;
            return true;
        }

        public bool Validate()
        {
            if (_root == null
                || _arrowIconRoot == null
                || _backgroundImage == null
                || _iconImage == null)
            {
                Debug.LogError("[UI_ThreatDirectionIndicator] Required authored indicator references are incomplete; LabelText is optional.", this);
                return false;
            }

            return true;
        }

        public bool Configure(RectTransform viewportRect, Camera worldCamera)
        {
            if (!_isInitialized || viewportRect == null || worldCamera == null)
                return false;

            _viewportRect = viewportRect;
            _worldCamera = worldCamera;
            _isConfigured = true;
            return true;
        }

        public void Show(
            Transform target,
            Sprite icon,
            string label,
            Color accent,
            float duration)
        {
            if (!_isInitialized || !_isConfigured || target == null)
            {
                Hide();
                return;
            }

            _target = target;
            if (icon != null)
            {
                _iconImage.sprite = icon;
                _iconImage.enabled = true;
            }
            else
            {
                _iconImage.sprite = _authoredIconSprite;
                _iconImage.enabled = _authoredIconEnabled;
            }
            _backgroundImage.color = accent;
            if (_labelText != null)
                _labelText.text = label ?? string.Empty;

            _timeRemaining = duration <= 0f ? float.PositiveInfinity : duration;
            _root.gameObject.SetActive(true);
        }

        public void Hide()
        {
            _target = null;
            _timeRemaining = 0f;
            if (_root != null)
                _root.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (_target == null || !_isConfigured)
                return;

            _timeRemaining -= Time.unscaledDeltaTime;
            if (_timeRemaining <= 0f)
            {
                Hide();
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
                _root.gameObject.SetActive(false);
                return;
            }

            _root.gameObject.SetActive(true);
            PositionAtViewportEdge(viewportPoint);
        }

        private void PositionAtViewportEdge(Vector3 viewportPoint)
        {
            Rect viewport = _viewportRect.rect;
            float width = viewport.width;
            float height = viewport.height;
            if (width <= 0f || height <= 0f)
                return;

            float minX = viewport.xMin + _horizontalEdgeMargin;
            float maxX = viewport.xMax - _horizontalEdgeMargin;
            float minY = viewport.yMin + _verticalEdgeMargin;
            float maxY = viewport.yMax - _verticalEdgeMargin;
            if (minX > maxX)
            {
                minX = viewport.center.x;
                maxX = viewport.center.x;
            }

            if (minY > maxY)
            {
                minY = viewport.center.y;
                maxY = viewport.center.y;
            }

            Vector2 direction = new Vector2(viewportPoint.x - 0.5f, viewportPoint.y - 0.5f);
            if (viewportPoint.z <= 0f)
                direction = -direction;

            if (direction.sqrMagnitude < 0.0001f)
                direction = Vector2.up;

            float x = Mathf.Clamp(viewport.center.x + direction.x * width, minX, maxX);
            float y = Mathf.Clamp(viewport.center.y + direction.y * height, minY, maxY);
            Vector2 localPoint = new Vector2(x, y);
            _root.position = _viewportRect.TransformPoint(localPoint);

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            _arrowIconRoot.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}
