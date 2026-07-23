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

            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;
            anchorMin.x /= screenWidth;
            anchorMin.y /= screenHeight;
            anchorMax.x /= screenWidth;
            anchorMax.y /= screenHeight;

            _target.anchorMin = anchorMin;
            _target.anchorMax = anchorMax;
            _target.offsetMin = Vector2.zero;
            _target.offsetMax = Vector2.zero;
        }
    }
}
