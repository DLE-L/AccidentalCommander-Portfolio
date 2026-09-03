using UnityEngine;

namespace Lizzo.PV.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaLayout : MonoBehaviour
    {
        [SerializeField] RectTransform _target;

        Rect _lastSafeArea;
        int _lastScreenWidth;
        int _lastScreenHeight;
        bool _hasScreenState;
        bool _missingTargetLogged;

        void Reset()
        {
            _target = GetComponent<RectTransform>();
        }

        void OnEnable()
        {
            ApplyIfChanged(force: true);
        }

        void Update()
        {
            ApplyIfChanged(force: false);
        }

        void ApplyIfChanged(bool force)
        {
            if (_target == null)
            {
                if (!_missingTargetLogged)
                {
                    Debug.LogError("[SafeAreaLayout] Target RectTransform is required.", this);
                    _missingTargetLogged = true;
                }

                return;
            }

            int screenWidth = Screen.width;
            int screenHeight = Screen.height;
            if (screenWidth <= 0 || screenHeight <= 0)
                return;

            Rect safeArea = Screen.safeArea;
            if (!force
                && _hasScreenState
                && screenWidth == _lastScreenWidth
                && screenHeight == _lastScreenHeight
                && safeArea == _lastSafeArea)
            {
                return;
            }

            _lastScreenWidth = screenWidth;
            _lastScreenHeight = screenHeight;
            _lastSafeArea = safeArea;
            _hasScreenState = true;

            Rect normalizedSafeArea = NormalizeSafeArea(safeArea, screenWidth, screenHeight);
            Vector2 anchorMin = normalizedSafeArea.min;
            Vector2 anchorMax = normalizedSafeArea.max;

            _target.anchorMin = anchorMin;
            _target.anchorMax = anchorMax;
            _target.offsetMin = Vector2.zero;
            _target.offsetMax = Vector2.zero;
        }

        static Rect NormalizeSafeArea(Rect safeArea, int screenWidth, int screenHeight)
        {
            float inverseWidth = 1f / screenWidth;
            float inverseHeight = 1f / screenHeight;
            return Rect.MinMaxRect(
                safeArea.xMin * inverseWidth,
                safeArea.yMin * inverseHeight,
                safeArea.xMax * inverseWidth,
                safeArea.yMax * inverseHeight);
        }
    }
}
