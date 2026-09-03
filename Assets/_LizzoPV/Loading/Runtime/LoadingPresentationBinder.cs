using Lizzo.PV.Flow;
using UnityEngine;
using UnityEngine.UI;

namespace Lizzo.PV.Presentation
{
    [DefaultExecutionOrder(-800)]
    [DisallowMultipleComponent]
    public sealed class LoadingPresentationBinder : MonoBehaviour
    {
        [SerializeField] private LoadingPresentationSetSO _presentationSet;
        [SerializeField] private Image _background;
        [SerializeField] private Image _barTrack;
        [SerializeField] private Image _barFill;
        [SerializeField] private Image _errorPanel;
        [SerializeField] private UiMotionPlayer _motionPlayer;
        [SerializeField] private AudioSource _audioSource;

        private AssetCatalogBundleRuntime _catalogs;
        private StartLoadingPresentationAssets _startAssets;
        private TransitionLoadingPresentationAssets _transitionAssets;
        private LoadingErrorPresentationAssets _errorAssets;
        private bool _hasStartAssets;
        private bool _hasTransitionAssets;
        private bool _hasErrorAssets;

        public LoadingPresentationSetSO PresentationSet => _presentationSet;

        private void Start()
        {
            if (!TryApply(SceneTransitionKind.Start, out string issue))
                Debug.LogError($"[LoadingPresentationBinder] {issue}", this);
        }

        public bool TryApply(SceneTransitionKind kind, out string issue)
        {
            if (!TryPrepare(out issue))
                return false;

            _hasErrorAssets = false;
            if (kind == SceneTransitionKind.Start)
            {
                if (!LoadingPresentationResolver.TryResolve(
                        _presentationSet.StartLoadingProfile,
                        _catalogs,
                        out _startAssets,
                        out issue))
                {
                    _hasStartAssets = false;
                    return false;
                }

                _hasStartAssets = true;
                _hasTransitionAssets = false;
                Apply(_startAssets.Background, _startAssets.BarTrack, _startAssets.BarFill);
                PlayMotion(_startAssets.EnterMotion);
                return true;
            }

            if (!LoadingPresentationResolver.TryResolve(
                    _presentationSet.TransitionLoadingProfile,
                    _catalogs,
                    out _transitionAssets,
                    out issue))
            {
                _hasTransitionAssets = false;
                return false;
            }

            _hasStartAssets = false;
            _hasTransitionAssets = true;
            Apply(_transitionAssets.Background, _transitionAssets.BarTrack, _transitionAssets.BarFill);
            PlayOneShot(_transitionAssets.EnterSfx);
            PlayMotion(_transitionAssets.EnterMotion);
            return true;
        }

        public bool TryShowError(out string issue)
        {
            if (!TryPrepare(out issue))
                return false;

            if (!LoadingPresentationResolver.TryResolve(
                    _presentationSet.LoadingErrorProfile,
                    _catalogs,
                    out _errorAssets,
                    out issue))
            {
                _hasErrorAssets = false;
                return false;
            }

            _hasErrorAssets = true;
            if (_errorPanel != null && _errorAssets.ErrorPanel != null)
                _errorPanel.sprite = _errorAssets.ErrorPanel;
            PlayOneShot(_errorAssets.LoadErrorSfx);
            PlayMotion(_errorAssets.ErrorEnterMotion);
            return true;
        }

        public float NotifyTransitionReady()
        {
            if (_hasTransitionAssets)
            {
                PlayOneShot(_transitionAssets.ReadySfx);
                PlayMotion(_transitionAssets.ExitMotion);
                return Mathf.Max(_transitionAssets.ExitDelaySeconds,
                    _transitionAssets.ExitMotion == null ? 0f : _transitionAssets.ExitMotion.length);
            }

            if (_hasStartAssets)
            {
                PlayOneShot(_startAssets.CompleteSfx);
                PlayMotion(_startAssets.ExitMotion);
                return Mathf.Max(_startAssets.ExitDelaySeconds,
                    _startAssets.ExitMotion == null ? 0f : _startAssets.ExitMotion.length);
            }

            return 0f;
        }

        public void NotifyRetryAccepted()
        {
            if (!_hasErrorAssets)
                return;
            PlayOneShot(_errorAssets.RetryAcceptedSfx);
            PlayMotion(_errorAssets.RetryProcessingMotion);
        }

        private bool TryPrepare(out string issue)
        {
            if (_presentationSet == null)
            {
                issue = "LoadingPresentationSet is required.";
                return false;
            }

            if (!_presentationSet.TryValidate(out issue))
                return false;

            _catalogs = AppBootstrap.Instance?.Services?.AssetCatalogs;
            if (_catalogs == null)
            {
                issue = "AssetCatalogBundleRuntime is unavailable.";
                return false;
            }

            if (_background == null || _barTrack == null || _barFill == null
                || _motionPlayer == null || _audioSource == null)
            {
                issue = "Background, BarTrack, BarFill, MotionPlayer, and AudioSource are required.";
                return false;
            }

            issue = string.Empty;
            return true;
        }

        private void Apply(Sprite background, Sprite barTrack, Sprite barFill)
        {
            _background.sprite = background;
            _barTrack.sprite = barTrack;
            _barFill.sprite = barFill;
        }

        private void PlayOneShot(AudioClip clip)
        {
            if (clip != null)
                _audioSource.PlayOneShot(clip);
        }

        private void PlayMotion(AnimationClip clip)
        {
            if (clip != null && !_motionPlayer.TryPlay(clip, out string issue))
                Debug.LogError($"[LoadingPresentationBinder] {issue}", this);
        }
    }
}
