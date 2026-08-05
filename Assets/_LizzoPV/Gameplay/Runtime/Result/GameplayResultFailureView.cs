using System;
using Lizzo.PV.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Lizzo.PV.Gameplay.Result
{
    [DisallowMultipleComponent]
    public sealed class GameplayResultFailureView : MonoBehaviour
    {
        private const int KpiCount = 4;

        [Serializable]
        public sealed class KpiBinding
        {
            [FormerlySerializedAs("LabelText")]
            [SerializeField]
            private TMP_Text _labelText;

            [FormerlySerializedAs("ValueText")]
            [SerializeField]
            private TMP_Text _valueText;

            public TMP_Text LabelText => _labelText;
            public TMP_Text ValueText => _valueText;
        }

        [SerializeField]
        private TMP_Text _titleText;

        [SerializeField]
        private TMP_Text _subtitleText;

        [SerializeField]
        private KpiBinding[] _kpiItems = new KpiBinding[KpiCount];

        [SerializeField]
        private Button _lobbyButton;

        [SerializeField]
        private TMP_Text _lobbyButtonText;

        [SerializeField]
        private Button _retryButton;

        [SerializeField]
        private TMP_Text _retryButtonText;

        public bool IsShowing => gameObject.activeSelf;

        public bool Configure()
        {
            if (_titleText == null
                || _subtitleText == null
                || _kpiItems == null
                || _kpiItems.Length != KpiCount
                || HasInvalidKpiBinding()
                || _lobbyButton == null
                || _lobbyButtonText == null
                || _retryButton == null
                || _retryButtonText == null)
            {
                Debug.LogError("[FailureResult] Authored failure result references are required.", this);
                return false;
            }

            return true;
        }

        public bool Present(
            RunResultViewData view,
            Action retryRequested,
            Action lobbyRequested)
        {
            if (view == null || view.IsClear || !Configure())
                return false;

            gameObject.SetActive(true);
            _titleText.text = "쓰러졌습니다";
            _subtitleText.text = "이번 전투 기록";
            SetKpi("스테이지", view.StageLabel, 0);
            SetKpi("시간", FormatElapsed(view.ElapsedSeconds), 1);
            SetKpi("처치", Mathf.Max(0, view.KillCount).ToString(), 2);
            SetKpi("최종 빌드", view.IsFinalBuildComplete ? "완성" : "진행 중", 3);

            _lobbyButtonText.text = "로비로";
            _retryButtonText.text = view.PrimaryButtonLabel;
            _lobbyButton.onClick.RemoveAllListeners();
            _lobbyButton.onClick.AddListener(() => lobbyRequested?.Invoke());
            _retryButton.onClick.RemoveAllListeners();
            _retryButton.onClick.AddListener(() => retryRequested?.Invoke());
            return true;
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void SetKpi(string label, string value, int index)
        {
            _kpiItems[index].LabelText.text = label;
            _kpiItems[index].ValueText.text = value;
        }

        private bool HasInvalidKpiBinding()
        {
            for (int i = 0; i < _kpiItems.Length; i++)
            {
                if (_kpiItems[i] == null
                    || _kpiItems[i].LabelText == null
                    || _kpiItems[i].ValueText == null)
                    return true;
            }

            return false;
        }

        private static string FormatElapsed(float elapsedSeconds)
        {
            int seconds = Mathf.Max(0, Mathf.RoundToInt(elapsedSeconds));
            return $"{seconds / 60:00}:{seconds % 60:00}";
        }
    }
}
