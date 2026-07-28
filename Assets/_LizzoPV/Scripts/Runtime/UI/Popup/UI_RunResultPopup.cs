using System;
using UnityEngine;
using UnityEngine.UI;

namespace Lizzo.PV.UI
{
    public sealed class UI_RunResultPopup : global::UI_Base
    {
        [Header("Result Views")]
        [SerializeField] private UI_RunMainResultView _mainResultView;
        [SerializeField] private UI_RunReviveChoiceView _reviveChoiceView;
        [SerializeField] private UI_RunFailureResultView _failureResultView;

        public bool IsShowingMainResult => _mainResultView != null && _mainResultView.IsShowing;
        public bool IsShowingFailureResult => _failureResultView != null && _failureResultView.IsShowing;
        public bool IsShowingReviveChoice => _reviveChoiceView != null && _reviveChoiceView.IsShowing;
        public Button ReviveChoiceButton => _reviveChoiceView == null ? null : _reviveChoiceView.ReviveButton;
        public Button ReviveChoiceCurrencyButton => _reviveChoiceView == null ? null : _reviveChoiceView.CurrencyButton;

        public bool Configure()
        {
            if (_mainResultView == null
                || !_mainResultView.Configure()
                || _reviveChoiceView == null
                || !_reviveChoiceView.Configure()
                || _failureResultView == null
                || !_failureResultView.Configure())
            {
                Debug.LogError("[Result] Authored result view references are required. Runtime layout creation is disabled.", this);
                return false;
            }

            return true;
        }

        public bool Present(
            RunResultViewData view,
            Action primaryRequested,
            Action lobbyRequested)
        {
            if (view == null || !Configure())
                return false;

            _reviveChoiceView.Hide();
            if (view.IsClear)
            {
                _failureResultView.Hide();
                return _mainResultView.Present(view, primaryRequested, lobbyRequested);
            }

            _mainResultView.Hide();
            return _failureResultView.Present(view, primaryRequested, lobbyRequested);
        }

        public bool PresentFailureReviveChoice(
            RunResultViewData view,
            Action primaryRequested,
            Action reviveRequested,
            Action lobbyRequested)
        {
            if (view == null || view.IsClear || !Configure())
                return false;

            _mainResultView.Hide();
            _failureResultView.Hide();
            return _reviveChoiceView.Present(
                view,
                reviveRequested,
                () => Present(view, primaryRequested, lobbyRequested));
        }

        public bool CloseReviveChoice()
        {
            return _reviveChoiceView != null && _reviveChoiceView.Close();
        }
    }
}
