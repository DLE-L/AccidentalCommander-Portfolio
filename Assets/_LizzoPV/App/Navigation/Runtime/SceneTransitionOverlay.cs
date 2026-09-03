using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using Lizzo.PV.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace Lizzo.PV.Flow
{
    public sealed class SceneTransitionOverlay : MonoBehaviour, ISceneTransitionPresentation
    {
        private static SceneTransitionOverlay s_instance;

        [SerializeField] private GameObject _visualRoot;
        [SerializeField] private Slider _progressBar;
        [SerializeField] private TMP_Text _progressText;
        [SerializeField] private GameObject _errorPanel;
        [SerializeField] private Button _retryButton;
        [SerializeField] private LoadingPresentationBinder _presentationBinder;

        private CancellationTokenSource _hideCancellation;

        public static bool IsVisible => s_instance != null && s_instance._visualRoot != null && s_instance._visualRoot.activeSelf;

        private void Awake()
        {
            if (s_instance != null && s_instance != this)
            {
                Destroy(gameObject);
                return;
            }

            s_instance = this;
            DontDestroyOnLoad(gameObject);
            if (_visualRoot == null)
            {
                Debug.LogError("[SceneTransitionOverlay] Authored transition visual is required.", this);
                return;
            }

            if (_presentationBinder == null)
                Debug.LogError("[SceneTransitionOverlay] Loading Presentation Binder is required.", this);

            _visualRoot.SetActive(true);
            if (_errorPanel != null)
                _errorPanel.SetActive(false);
            if (_retryButton != null)
                _retryButton.onClick.AddListener(HandleRetry);
        }

        private void OnDestroy()
        {
            CancelHide();
            if (_retryButton != null)
                _retryButton.onClick.RemoveListener(HandleRetry);
            if (s_instance == this)
                s_instance = null;
        }

        public static void Show()
        {
            if (s_instance?._visualRoot == null)
                return;
            s_instance.CancelHide();
            s_instance._visualRoot.SetActive(true);
        }

        public static void Hide()
        {
            if (s_instance?._visualRoot == null)
                return;
            s_instance.CancelHide();
            s_instance._visualRoot.SetActive(false);
        }

        public void Show(SceneTransitionKind kind)
        {
            CancelHide();
            if (_visualRoot != null)
                _visualRoot.SetActive(true);
            if (_errorPanel != null)
                _errorPanel.SetActive(false);
            if (_presentationBinder != null && !_presentationBinder.TryApply(kind, out string issue))
                Debug.LogError($"[SceneTransitionOverlay] {issue}", this);
        }

        public void SetProgress(float progress)
        {
            float normalized = Mathf.Clamp01(progress);
            if (_progressBar != null)
                _progressBar.SetValueWithoutNotify(normalized);
            if (_progressText != null)
                _progressText.SetText("{0:0}%", normalized * 100f);
        }

        public void ShowError()
        {
            CancelHide();
            if (_visualRoot != null)
                _visualRoot.SetActive(true);
            if (_errorPanel != null)
                _errorPanel.SetActive(true);
            if (_presentationBinder != null && !_presentationBinder.TryShowError(out string issue))
                Debug.LogError($"[SceneTransitionOverlay] {issue}", this);
        }

        void ISceneTransitionPresentation.Hide()
        {
            HideAfterPresentationAsync().Forget();
        }

        private async UniTaskVoid HideAfterPresentationAsync()
        {
            CancelHide();
            float delaySeconds = _presentationBinder == null ? 0f : _presentationBinder.NotifyTransitionReady();
            if (delaySeconds <= 0f)
            {
                if (_visualRoot != null)
                    _visualRoot.SetActive(false);
                return;
            }

            var cancellation = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            _hideCancellation = cancellation;
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(delaySeconds), ignoreTimeScale: true, cancellationToken: cancellation.Token);
                if (_visualRoot != null)
                    _visualRoot.SetActive(false);
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                if (ReferenceEquals(_hideCancellation, cancellation))
                    _hideCancellation = null;
                cancellation.Dispose();
            }
        }

        private void HandleRetry()
        {
            if (_presentationBinder != null)
                _presentationBinder.NotifyRetryAccepted();
            SceneTransitionCoordinatorHost.Retry();
        }

        private void CancelHide()
        {
            _hideCancellation?.Cancel();
            _hideCancellation?.Dispose();
            _hideCancellation = null;
        }
    }
}
