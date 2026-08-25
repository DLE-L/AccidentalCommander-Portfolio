using System;
using Lizzo.PV.Gameplay.Diagnostics;
using Lizzo.PV.UI;
using UnityEngine;

namespace Lizzo.PV.Gameplay.Result
{
    [DisallowMultipleComponent]
    public sealed class GameplayResultController : MonoBehaviour
    {
        [SerializeField]
        private GameplayResultMainView _mainResultView;

        [SerializeField]
        private GameplayResultReviveView _reviveChoiceView;

        [SerializeField]
        private GameplayResultFailureView _failureResultView;

        [SerializeField]
        private GameplayResultBuildSummaryView _mainBuildSummaryView;

        [SerializeField]
        private GameplayResultBuildSummaryView _failureBuildSummaryView;

        private bool _primaryAccepted;
        private bool _clearResultPresented;

        public event Action PrimaryRequested;
        public event Action LobbyRequested;
        public event Action ReviveRequested;

        public bool IsShowingMainResult => _mainResultView != null && _mainResultView.IsShowing;
        public bool IsShowingFailureResult => _failureResultView != null && _failureResultView.IsShowing;
        public bool IsShowingReviveChoice => _reviveChoiceView != null && _reviveChoiceView.IsShowing;

        public bool Configure()
        {
            return ConfigureForClear() && ConfigureForFailure();
        }

        public bool PresentResult(RunResultViewData view)
        {
            if (view == null || !(view.IsClear ? ConfigureForClear() : ConfigureForFailure()))
                return false;

            _primaryAccepted = false;
            _clearResultPresented = view.IsClear;
            gameObject.SetActive(true);
            _reviveChoiceView.Hide();

            if (view.IsClear)
            {
                _failureResultView.Hide();
                _failureBuildSummaryView.Hide();

                bool presented = _mainResultView.Present(
                    view,
                    RaisePrimaryRequested,
                    RaiseLobbyRequested);

                _mainBuildSummaryView.gameObject.SetActive(true);
                return presented && _mainBuildSummaryView.Present(view);
            }

            _mainResultView.Hide();
            _mainBuildSummaryView.Hide();

            bool failurePresented = _failureResultView.Present(
                view,
                RaisePrimaryRequested,
                RaiseLobbyRequested);

            _failureBuildSummaryView.gameObject.SetActive(true);
            return failurePresented && _failureBuildSummaryView.Present(view);
        }

        public bool PresentReviveChoice(RunResultViewData view)
        {
            if (view == null || view.IsClear || !ConfigureForFailure())
                return false;

            gameObject.SetActive(true);
            _mainResultView.Hide();
            _failureResultView.Hide();
            _mainBuildSummaryView.Hide();
            _failureBuildSummaryView.Hide();

            return _reviveChoiceView.Present(
                view,
                RaiseReviveRequested,
                () => PresentResult(view));
        }

        public void Hide()
        {
            if (_reviveChoiceView != null)
                _reviveChoiceView.Hide();

            if (_mainResultView != null)
                _mainResultView.Hide();

            if (_failureResultView != null)
                _failureResultView.Hide();

            if (_mainBuildSummaryView != null)
                _mainBuildSummaryView.Hide();

            if (_failureBuildSummaryView != null)
                _failureBuildSummaryView.Hide();

            gameObject.SetActive(false);
        }

        private bool ConfigureForClear()
        {
            if (_mainResultView == null
                || !_mainResultView.Configure()
                || _mainBuildSummaryView == null
                || !_mainBuildSummaryView.ConfigureForClear())
            {
                Debug.LogError("[Result] Authored clear result references are required.", this);
                return false;
            }

            return true;
        }

        private bool ConfigureForFailure()
        {
            if (_reviveChoiceView == null
                || !_reviveChoiceView.Configure()
                || _failureResultView == null
                || !_failureResultView.Configure()
                || _failureBuildSummaryView == null
                || !_failureBuildSummaryView.ConfigureForFailure())
            {
                Debug.LogError("[Result] Authored failure result references are required.", this);
                return false;
            }

            return true;
        }

        private void RaisePrimaryRequested()
        {
            if (_primaryAccepted)
                return;

            _primaryAccepted = true;
            if (_clearResultPresented)
            {
                Build1RuntimeDiagnostics.Log(
                    "result_next_run_click",
                    Build1RuntimeDiagnostics.Text("result", "clear"));
            }

            PrimaryRequested?.Invoke();
        }

        private void RaiseLobbyRequested()
        {
            LobbyRequested?.Invoke();
        }

        private void RaiseReviveRequested()
        {
            ReviveRequested?.Invoke();
        }

        private void OnDestroy()
        {
            PrimaryRequested = null;
            LobbyRequested = null;
            ReviveRequested = null;
        }
    }
}
