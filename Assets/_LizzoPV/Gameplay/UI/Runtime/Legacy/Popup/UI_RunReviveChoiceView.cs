using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Lizzo.PV.UI
{
    public sealed class UI_RunReviveChoiceView : MonoBehaviour
    {
        private const int TimeoutSeconds = 10;

        [Header("Failure Revive Choice")]
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _timeoutText;
        [SerializeField] private TMP_Text _bodyText;
        [FormerlySerializedAs("_reviveChoiceAdButton")]
        [SerializeField] private Button _reviveButton;
        [SerializeField] private Button _currencyButton;
        [SerializeField] private Button _closeButton;

        private CancellationTokenSource _timeoutCancellation;
        private bool _transitioned;

        public bool IsShowing => gameObject.activeSelf;
        public Button ReviveButton => _reviveButton;
        public Button CurrencyButton => _currencyButton;

        public bool Configure()
        {
            if (_titleText == null
                || _timeoutText == null
                || _bodyText == null
                || _reviveButton == null
                || _currencyButton == null
                || _closeButton == null)
            {
                Debug.LogError("[ReviveChoice] Authored revive choice references are required.", this);
                return false;
            }

            return true;
        }

        public bool Present(
            RunResultViewData view,
            Action reviveRequested,
            Action declined)
        {
            if (view == null || view.IsClear || !Configure())
                return false;

            CancelTimeout();
            _transitioned = false;
            gameObject.SetActive(true);
            _titleText.text = "부활 방법을 선택하세요";
            _timeoutText.text = TimeoutSeconds.ToString();
            _bodyText.text = view.Body;

            _reviveButton.gameObject.SetActive(true);
            _reviveButton.interactable = reviveRequested != null;
            _currencyButton.interactable = false;
            _currencyButton.gameObject.SetActive(false);
            _reviveButton.onClick.RemoveAllListeners();
            if (reviveRequested != null)
                _reviveButton.onClick.AddListener(() => AcceptRevive(reviveRequested));
            _currencyButton.onClick.RemoveAllListeners();
            _closeButton.onClick.RemoveAllListeners();
            _closeButton.interactable = true;
            _closeButton.onClick.AddListener(() => Decline(declined));

            _timeoutCancellation = new CancellationTokenSource();
            RunTimeoutAsync(declined, _timeoutCancellation.Token).Forget();
            return true;
        }

        public bool Close()
        {
            if (!IsShowing || _closeButton == null)
                return false;

            _closeButton.onClick.Invoke();
            return true;
        }

        public void Hide()
        {
            _transitioned = true;
            CancelTimeout();
            gameObject.SetActive(false);
        }

        private void AcceptRevive(Action reviveRequested)
        {
            if (_transitioned)
                return;

            _transitioned = true;
            CancelTimeout();
            gameObject.SetActive(false);
            reviveRequested?.Invoke();
        }

        private void Decline(Action declined)
        {
            if (_transitioned)
                return;

            _transitioned = true;
            CancelTimeout();
            gameObject.SetActive(false);
            declined?.Invoke();
        }

        private async UniTask RunTimeoutAsync(Action declined, CancellationToken cancellationToken)
        {
            int remainingSeconds = TimeoutSeconds;
            try
            {
                while (remainingSeconds > 0)
                {
                    await UniTask.Delay(
                        TimeSpan.FromSeconds(1),
                        DelayType.Realtime,
                        PlayerLoopTiming.Update,
                        cancellationToken);

                    remainingSeconds--;
                    _timeoutText.text = remainingSeconds.ToString();
                }

                Decline(declined);
            }
            catch (OperationCanceledException)
            {
            }
        }

        private void CancelTimeout()
        {
            if (_timeoutCancellation == null)
                return;

            _timeoutCancellation.Cancel();
            _timeoutCancellation.Dispose();
            _timeoutCancellation = null;
        }

        private void OnDisable()
        {
            CancelTimeout();
        }

        private void OnDestroy()
        {
            CancelTimeout();
        }
    }
}
