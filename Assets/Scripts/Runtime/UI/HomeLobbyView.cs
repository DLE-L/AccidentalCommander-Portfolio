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




    }
}
