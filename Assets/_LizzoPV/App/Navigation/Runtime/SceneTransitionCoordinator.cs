using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Lizzo.PV.Flow
{
    public enum SceneTransitionPhase
    {
        Idle = 0,
        LoadingTarget = 1,
        WaitingForTargetReady = 2,
        Finalizing = 3,
        Failed = 4,
        Completed = 5,
    }

    public interface ISceneTransitionDriver
    {
        UniTask<bool> LoadTargetAsync(
            string targetScenePath,
            Action<float> reportProgress,
            CancellationToken cancellationToken);

        UniTask<bool> ActivateTargetAndUnloadSourceAsync(
            string sourceScenePath,
            string targetScenePath,
            CancellationToken cancellationToken);

        UniTask CleanupTargetAsync(string targetScenePath, CancellationToken cancellationToken);
    }

    public interface ISceneTransitionPresentation
    {
        void Show(SceneTransitionKind kind);
        void SetProgress(float progress);
        void ShowError();
        void Hide();
    }

    public sealed class SceneTransitionCoordinator
    {
        const float ReadyProgressCeiling = 0.9f;

        readonly ISceneTransitionDriver _driver;
        readonly ISceneTransitionPresentation _presentation;
        UniTaskCompletionSource<TargetReadyResult> _targetReady;
        SceneTransitionRequest _activeRequest;
        string _sourceScenePath;
        bool _isRunning;

        public SceneTransitionCoordinator(
            ISceneTransitionDriver driver,
            ISceneTransitionPresentation presentation)
        {
            _driver = driver ?? throw new ArgumentNullException(nameof(driver));
            _presentation = presentation ?? throw new ArgumentNullException(nameof(presentation));
        }

        public SceneTransitionPhase Phase { get; private set; } = SceneTransitionPhase.Idle;
        public float Progress { get; private set; }
        public string LastFailure { get; private set; } = string.Empty;
        public bool IsRunning => _isRunning;

        public UniTask<bool> TryRunAsync(
            SceneTransitionRequest request,
            string sourceScenePath,
            CancellationToken cancellationToken)
        {
            if (_isRunning || !request.IsValid || string.IsNullOrWhiteSpace(sourceScenePath))
                return UniTask.FromResult(false);

            _activeRequest = request;
            _sourceScenePath = sourceScenePath;
            return RunAsync(request, sourceScenePath, cancellationToken);
        }

        public UniTask<bool> RetryAsync(CancellationToken cancellationToken)
        {
            if (_isRunning || Phase != SceneTransitionPhase.Failed || !_activeRequest.IsValid)
                return UniTask.FromResult(false);

            return RunAsync(_activeRequest, _sourceScenePath, cancellationToken);
        }

        public bool ReportTargetReady(string targetScenePath)
        {
            return TryCompleteTarget(targetScenePath, new TargetReadyResult(true, string.Empty));
        }

        public bool ReportTargetFailure(string targetScenePath, string issue)
        {
            string failure = string.IsNullOrWhiteSpace(issue) ? "Target preparation failed." : issue;
            return TryCompleteTarget(targetScenePath, new TargetReadyResult(false, failure));
        }

        async UniTask<bool> RunAsync(
            SceneTransitionRequest request,
            string sourceScenePath,
            CancellationToken cancellationToken)
        {
            _isRunning = true;
            Phase = SceneTransitionPhase.LoadingTarget;
            Progress = 0f;
            LastFailure = string.Empty;
            _targetReady = new UniTaskCompletionSource<TargetReadyResult>();
            _presentation.Show(request.Kind);
            _presentation.SetProgress(0f);

            try
            {
                bool loaded = await _driver.LoadTargetAsync(
                    request.TargetScenePath,
                    ReportLoadProgress,
                    cancellationToken);
                if (!loaded)
                    return await FailAsync("Target Scene load failed.", cancellationToken);

                Phase = SceneTransitionPhase.WaitingForTargetReady;
                SetProgress(ReadyProgressCeiling);
                TargetReadyResult ready = await _targetReady.Task.AttachExternalCancellation(cancellationToken);
                if (!ready.Succeeded)
                    return await FailAsync(ready.Issue, cancellationToken);

                Phase = SceneTransitionPhase.Finalizing;
                SetProgress(1f);
                bool finalized = await _driver.ActivateTargetAndUnloadSourceAsync(
                    sourceScenePath,
                    request.TargetScenePath,
                    cancellationToken);
                if (!finalized)
                    return await FailAsync("Target Scene activation failed.", cancellationToken);

                Phase = SceneTransitionPhase.Completed;
                _presentation.Hide();
                return true;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                await CleanupAfterCancellationAsync(request.TargetScenePath);
                Phase = SceneTransitionPhase.Idle;
                _presentation.Hide();
                return false;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return await FailAsync("Unexpected transition failure.", CancellationToken.None);
            }
            finally
            {
                _isRunning = false;
                _targetReady = null;
            }
        }

        bool TryCompleteTarget(string targetScenePath, TargetReadyResult result)
        {
            if (!_isRunning
                || _targetReady == null
                || !string.Equals(_activeRequest.TargetScenePath, targetScenePath, StringComparison.Ordinal))
            {
                return false;
            }

            return _targetReady.TrySetResult(result);
        }

        void ReportLoadProgress(float progress)
        {
            float normalized = Mathf.Clamp01(progress) * ReadyProgressCeiling;
            SetProgress(normalized);
        }

        void SetProgress(float progress)
        {
            float monotonic = Mathf.Max(Progress, Mathf.Clamp01(progress));
            if (Mathf.Approximately(monotonic, Progress))
                return;

            Progress = monotonic;
            _presentation.SetProgress(Progress);
        }

        async UniTask<bool> FailAsync(string issue, CancellationToken cancellationToken)
        {
            try
            {
                await _driver.CleanupTargetAsync(_activeRequest.TargetScenePath, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                await CleanupAfterCancellationAsync(_activeRequest.TargetScenePath);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }

            LastFailure = string.IsNullOrWhiteSpace(issue) ? "Transition failed." : issue;
            Phase = SceneTransitionPhase.Failed;
            _presentation.ShowError();
            return false;
        }

        async UniTask CleanupAfterCancellationAsync(string targetScenePath)
        {
            try
            {
                await _driver.CleanupTargetAsync(targetScenePath, CancellationToken.None);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        readonly struct TargetReadyResult
        {
            public TargetReadyResult(bool succeeded, string issue)
            {
                Succeeded = succeeded;
                Issue = issue;
            }

            public bool Succeeded { get; }
            public string Issue { get; }
        }
    }
}
