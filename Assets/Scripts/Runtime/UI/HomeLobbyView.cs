using System;
using UnityEngine;

namespace Lizzo.PV.UI
{
    public sealed class HomeLobbyView : MonoBehaviour
    {
        [SerializeField] GameObject _layoutRoot;
        [SerializeField] RectTransform _safeAreaRoot;
        [SerializeField] LobbyNavigationShell _navigationShell;

        public bool Configure()
        {
            if (_layoutRoot == null ||
                _safeAreaRoot == null ||
                _navigationShell == null)
            {
                Debug.LogError("[HomeLobbyView] Authored layout, safe-area, and navigation-shell references are required.", this);
                return false;
            }

            return true;
        }

        public void Show(Action startBattleRequested)
        {
            if (Configure() == false)
                return;

            _layoutRoot.SetActive(true);
            ApplySafeArea();
            _navigationShell.Show(startBattleRequested);
        }

        public void Hide()
        {
            if (_layoutRoot != null)
                _layoutRoot.SetActive(false);

            if (_navigationShell != null)
                _navigationShell.Hide();
        }

        public bool TryHandleBack()
        {
            return _navigationShell != null && _navigationShell.TryHandleBack();
        }

        void OnEnable()
        {
            ApplySafeArea();
        }

        void ApplySafeArea()
        {
            if (_safeAreaRoot == null)
                return;

            Rect safeArea = Screen.safeArea;
            Vector2 min = safeArea.position;
            Vector2 max = safeArea.position + safeArea.size;
            min.x /= Screen.width;
            min.y /= Screen.height;
            max.x /= Screen.width;
            max.y /= Screen.height;
            _safeAreaRoot.anchorMin = min;
            _safeAreaRoot.anchorMax = max;
            _safeAreaRoot.offsetMin = Vector2.zero;
            _safeAreaRoot.offsetMax = Vector2.zero;
        }
    }
}
