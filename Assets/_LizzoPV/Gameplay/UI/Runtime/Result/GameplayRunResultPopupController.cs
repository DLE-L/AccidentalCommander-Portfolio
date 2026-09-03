using System;
using Lizzo.PV.UI;
using UnityEngine;

namespace Lizzo.PV.Gameplay.Result
{
    [DisallowMultipleComponent]
    public sealed class GameplayRunResultPopupController : MonoBehaviour
    {
        [SerializeField] private GameplayRunResultPopupView _view;

        private bool _mainAccepted;

        public event Action MainRequested;

        public bool IsShowing => _view != null && _view.IsShowing;

        public bool Configure()
        {
            if (_view == null || !_view.Configure())
            {
                Debug.LogError("[Result] Authored common result popup is required.", this);
                return false;
            }

            return true;
        }

        public bool Present(RunResultViewData view)
        {
            if (view == null || !Configure())
                return false;

            _mainAccepted = false;
            gameObject.SetActive(true);
            return _view.Present(view, RaiseMainRequested);
        }

        public void Hide()
        {
            if (_view != null)
                _view.Hide();
            gameObject.SetActive(false);
        }

        private void RaiseMainRequested()
        {
            if (_mainAccepted)
                return;

            _mainAccepted = true;
            MainRequested?.Invoke();
        }

        private void OnDestroy()
        {
            MainRequested = null;
        }
    }
}
