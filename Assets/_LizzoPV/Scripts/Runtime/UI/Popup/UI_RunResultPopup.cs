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
        [SerializeField] private UI_RunBuildSummaryView _mainBuildSummaryView;
        [SerializeField] private UI_RunBuildSummaryView _failureBuildSummaryView;

        public bool IsShowingMainResult => _mainResultView != null && _mainResultView.IsShowing;
        public bool IsShowingFailureResult => _failureResultView != null && _failureResultView.IsShowing;
        public bool IsShowingReviveChoice => _reviveChoiceView != null && _reviveChoiceView.IsShowing;
        public Button ReviveChoiceButton => _reviveChoiceView == null ? null : _reviveChoiceView.ReviveButton;
        public Button ReviveChoiceCurrencyButton => _reviveChoiceView == null ? null : _reviveChoiceView.CurrencyButton;

        public bool Configure()
        {
            return ConfigureForClear() && ConfigureForFailure();
        }

        public bool ConfigureForClear()
        {
            if (_mainResultView == null
                || !_mainResultView.Configure()
                || _mainBuildSummaryView == null
                || !_mainBuildSummaryView.ConfigureForClear())
            {
                Debug.LogError("[Result] Authored result view references are required. Runtime layout creation is disabled.", this);
                return false;
            }

            return true;
        }

        public bool ConfigureForFailure()
        {
            if (_reviveChoiceView == null
                || !_reviveChoiceView.Configure()
                || _failureResultView == null
                || !_failureResultView.Configure()
                || _failureBuildSummaryView == null
                || !_failureBuildSummaryView.ConfigureForFailure())
            {
                Debug.LogError("[Result] Authored result views are required for failure presentation. Runtime layout creation is disabled.", this);
                return false;
            }

            return true;
        }

        public bool Present(
            RunResultViewData view,
            Action primaryRequested,
            Action lobbyRequested)
        {
            if (view == null || !(view.IsClear ? ConfigureForClear() : ConfigureForFailure()))
                return false;

            _reviveChoiceView.Hide();
            if (view.IsClear)
            {
                _failureResultView.Hide();
                _failureBuildSummaryView.Hide();
                bool presented = _mainResultView.Present(view, primaryRequested, lobbyRequested);
                _mainBuildSummaryView.gameObject.SetActive(true);
                return presented && _mainBuildSummaryView.Present(view);
            }

            _mainResultView.Hide();
            _mainBuildSummaryView.Hide();
            bool failurePresented = _failureResultView.Present(view, primaryRequested, lobbyRequested);
            _failureBuildSummaryView.gameObject.SetActive(true);
            return failurePresented && _failureBuildSummaryView.Present(view);
        }

        public bool PresentFailureReviveChoice(
            RunResultViewData view,
            Action primaryRequested,
            Action reviveRequested,
            Action lobbyRequested)
        {
            if (view == null || view.IsClear || !ConfigureForFailure())
                return false;

            _mainResultView.Hide();
            _failureResultView.Hide();
            _mainBuildSummaryView.Hide();
            _failureBuildSummaryView.Hide();
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
