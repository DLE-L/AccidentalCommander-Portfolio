using UnityEngine;

namespace Lizzo.PV.Presentation
{
    [CreateAssetMenu(menuName = "Lizzo PV/Presentation/Loading/Start Loading Profile", fileName = "StartLoadingPresentationProfile")]
    public sealed class StartLoadingPresentationProfileSO : ScriptableObject
    {
        [SerializeField] private SpriteAssetId _backgroundSpriteId;
        [SerializeField] private SpriteAssetId _barTrackSpriteId;
        [SerializeField] private SpriteAssetId _barFillSpriteId;
        [SerializeField] private AudioAssetId _completeSfxId;
        [SerializeField] private MotionAssetId _enterMotionId;
        [SerializeField] private MotionAssetId _loopMotionId;
        [SerializeField] private MotionAssetId _exitMotionId;
        [SerializeField, Min(0f)] private float _minimumVisibleSeconds;
        [SerializeField, Min(0.0001f)] private float _progressSmoothing = 1f;
        [SerializeField, Min(0f)] private float _exitDelaySeconds;

        public SpriteAssetId BackgroundSpriteId => _backgroundSpriteId;
        public SpriteAssetId BarTrackSpriteId => _barTrackSpriteId;
        public SpriteAssetId BarFillSpriteId => _barFillSpriteId;
        public AudioAssetId CompleteSfxId => _completeSfxId;
        public MotionAssetId EnterMotionId => _enterMotionId;
        public MotionAssetId LoopMotionId => _loopMotionId;
        public MotionAssetId ExitMotionId => _exitMotionId;
        public float MinimumVisibleSeconds => _minimumVisibleSeconds;
        public float ProgressSmoothing => _progressSmoothing;
        public float ExitDelaySeconds => _exitDelaySeconds;

        public bool TryValidate(out string issue)
        {
            return LoadingProfileValidation.Require(_backgroundSpriteId, nameof(BackgroundSpriteId), out issue)
                   && LoadingProfileValidation.Require(_barTrackSpriteId, nameof(BarTrackSpriteId), out issue)
                   && LoadingProfileValidation.Require(_barFillSpriteId, nameof(BarFillSpriteId), out issue)
                   && LoadingProfileValidation.Require(_completeSfxId, nameof(CompleteSfxId), out issue)
                   && LoadingProfileValidation.Require(_enterMotionId, nameof(EnterMotionId), out issue)
                   && LoadingProfileValidation.Require(_loopMotionId, nameof(LoopMotionId), out issue)
                   && LoadingProfileValidation.Require(_exitMotionId, nameof(ExitMotionId), out issue)
                   && LoadingProfileValidation.RequireTiming(
                       _minimumVisibleSeconds,
                       _progressSmoothing,
                       _exitDelaySeconds,
                       out issue);
        }

#if UNITY_EDITOR
        public void SetForEditor(
            SpriteAssetId backgroundSpriteId,
            SpriteAssetId barTrackSpriteId,
            SpriteAssetId barFillSpriteId,
            AudioAssetId completeSfxId,
            MotionAssetId enterMotionId,
            MotionAssetId loopMotionId,
            MotionAssetId exitMotionId,
            float minimumVisibleSeconds,
            float progressSmoothing,
            float exitDelaySeconds)
        {
            _backgroundSpriteId = backgroundSpriteId;
            _barTrackSpriteId = barTrackSpriteId;
            _barFillSpriteId = barFillSpriteId;
            _completeSfxId = completeSfxId;
            _enterMotionId = enterMotionId;
            _loopMotionId = loopMotionId;
            _exitMotionId = exitMotionId;
            _minimumVisibleSeconds = minimumVisibleSeconds;
            _progressSmoothing = progressSmoothing;
            _exitDelaySeconds = exitDelaySeconds;
        }
#endif
    }

    [CreateAssetMenu(menuName = "Lizzo PV/Presentation/Loading/Transition Loading Profile", fileName = "TransitionLoadingPresentationProfile")]
    public sealed class TransitionLoadingPresentationProfileSO : ScriptableObject
    {
        [SerializeField] private SpriteAssetId _backgroundSpriteId;
        [SerializeField] private SpriteAssetId _barTrackSpriteId;
        [SerializeField] private SpriteAssetId _barFillSpriteId;
        [SerializeField] private AudioAssetId _transitionEnterSfxId;
        [SerializeField] private AudioAssetId _transitionReadySfxId;
        [SerializeField] private MotionAssetId _enterMotionId;
        [SerializeField] private MotionAssetId _loopMotionId;
        [SerializeField] private MotionAssetId _exitMotionId;
        [SerializeField, Min(0f)] private float _minimumVisibleSeconds;
        [SerializeField, Min(0.0001f)] private float _progressSmoothing = 1f;
        [SerializeField, Min(0f)] private float _exitDelaySeconds;

        public SpriteAssetId BackgroundSpriteId => _backgroundSpriteId;
        public SpriteAssetId BarTrackSpriteId => _barTrackSpriteId;
        public SpriteAssetId BarFillSpriteId => _barFillSpriteId;
        public AudioAssetId TransitionEnterSfxId => _transitionEnterSfxId;
        public AudioAssetId TransitionReadySfxId => _transitionReadySfxId;
        public MotionAssetId EnterMotionId => _enterMotionId;
        public MotionAssetId LoopMotionId => _loopMotionId;
        public MotionAssetId ExitMotionId => _exitMotionId;
        public float MinimumVisibleSeconds => _minimumVisibleSeconds;
        public float ProgressSmoothing => _progressSmoothing;
        public float ExitDelaySeconds => _exitDelaySeconds;

        public bool TryValidate(out string issue)
        {
            return LoadingProfileValidation.Require(_backgroundSpriteId, nameof(BackgroundSpriteId), out issue)
                   && LoadingProfileValidation.Require(_barTrackSpriteId, nameof(BarTrackSpriteId), out issue)
                   && LoadingProfileValidation.Require(_barFillSpriteId, nameof(BarFillSpriteId), out issue)
                   && LoadingProfileValidation.Require(_transitionEnterSfxId, nameof(TransitionEnterSfxId), out issue)
                   && LoadingProfileValidation.Require(_transitionReadySfxId, nameof(TransitionReadySfxId), out issue)
                   && LoadingProfileValidation.Require(_enterMotionId, nameof(EnterMotionId), out issue)
                   && LoadingProfileValidation.Require(_loopMotionId, nameof(LoopMotionId), out issue)
                   && LoadingProfileValidation.Require(_exitMotionId, nameof(ExitMotionId), out issue)
                   && LoadingProfileValidation.RequireTiming(
                       _minimumVisibleSeconds,
                       _progressSmoothing,
                       _exitDelaySeconds,
                       out issue);
        }

#if UNITY_EDITOR
        public void SetForEditor(
            SpriteAssetId backgroundSpriteId,
            SpriteAssetId barTrackSpriteId,
            SpriteAssetId barFillSpriteId,
            AudioAssetId transitionEnterSfxId,
            AudioAssetId transitionReadySfxId,
            MotionAssetId enterMotionId,
            MotionAssetId loopMotionId,
            MotionAssetId exitMotionId,
            float minimumVisibleSeconds,
            float progressSmoothing,
            float exitDelaySeconds)
        {
            _backgroundSpriteId = backgroundSpriteId;
            _barTrackSpriteId = barTrackSpriteId;
            _barFillSpriteId = barFillSpriteId;
            _transitionEnterSfxId = transitionEnterSfxId;
            _transitionReadySfxId = transitionReadySfxId;
            _enterMotionId = enterMotionId;
            _loopMotionId = loopMotionId;
            _exitMotionId = exitMotionId;
            _minimumVisibleSeconds = minimumVisibleSeconds;
            _progressSmoothing = progressSmoothing;
            _exitDelaySeconds = exitDelaySeconds;
        }
#endif
    }

    [CreateAssetMenu(menuName = "Lizzo PV/Presentation/Loading/Loading Error Profile", fileName = "LoadingErrorPresentationProfile")]
    public sealed class LoadingErrorPresentationProfileSO : ScriptableObject
    {
        [SerializeField] private SpriteAssetId _errorPanelSpriteId;
        [SerializeField] private AudioAssetId _loadErrorSfxId;
        [SerializeField] private AudioAssetId _retryAcceptedSfxId;
        [SerializeField] private MotionAssetId _errorEnterMotionId;
        [SerializeField] private MotionAssetId _errorExitMotionId;
        [SerializeField] private MotionAssetId _retryProcessingMotionId;

        public SpriteAssetId ErrorPanelSpriteId => _errorPanelSpriteId;
        public ControlStyleRole RetryButtonStyleRole => ControlStyleRole.PrimaryButton;
        public AudioAssetId LoadErrorSfxId => _loadErrorSfxId;
        public AudioAssetId RetryAcceptedSfxId => _retryAcceptedSfxId;
        public MotionAssetId ErrorEnterMotionId => _errorEnterMotionId;
        public MotionAssetId ErrorExitMotionId => _errorExitMotionId;
        public MotionAssetId RetryProcessingMotionId => _retryProcessingMotionId;

        public bool TryValidate(out string issue)
        {
            return LoadingProfileValidation.Require(_errorPanelSpriteId, nameof(ErrorPanelSpriteId), out issue)
                   && LoadingProfileValidation.Require(_loadErrorSfxId, nameof(LoadErrorSfxId), out issue)
                   && LoadingProfileValidation.Require(_retryAcceptedSfxId, nameof(RetryAcceptedSfxId), out issue)
                   && LoadingProfileValidation.Require(_errorEnterMotionId, nameof(ErrorEnterMotionId), out issue)
                   && LoadingProfileValidation.Require(_errorExitMotionId, nameof(ErrorExitMotionId), out issue)
                   && LoadingProfileValidation.Require(_retryProcessingMotionId, nameof(RetryProcessingMotionId), out issue);
        }

#if UNITY_EDITOR
        public void SetForEditor(
            SpriteAssetId errorPanelSpriteId,
            AudioAssetId loadErrorSfxId,
            AudioAssetId retryAcceptedSfxId,
            MotionAssetId errorEnterMotionId,
            MotionAssetId errorExitMotionId,
            MotionAssetId retryProcessingMotionId)
        {
            _errorPanelSpriteId = errorPanelSpriteId;
            _loadErrorSfxId = loadErrorSfxId;
            _retryAcceptedSfxId = retryAcceptedSfxId;
            _errorEnterMotionId = errorEnterMotionId;
            _errorExitMotionId = errorExitMotionId;
            _retryProcessingMotionId = retryProcessingMotionId;
        }
#endif
    }

    internal static class LoadingProfileValidation
    {
        public static bool Require(SpriteAssetId id, string fieldName, out string issue)
        {
            return Require(id.Value, "Sprite", fieldName, out issue);
        }

        public static bool Require(AudioAssetId id, string fieldName, out string issue)
        {
            return Require(id.Value, "Audio", fieldName, out issue);
        }

        public static bool Require(MotionAssetId id, string fieldName, out string issue)
        {
            return Require(id.Value, "Motion", fieldName, out issue);
        }

        public static bool RequireTiming(
            float minimumVisibleSeconds,
            float progressSmoothing,
            float exitDelaySeconds,
            out string issue)
        {
            if (minimumVisibleSeconds < 0f)
            {
                issue = "MinimumVisibleSeconds cannot be negative.";
                return false;
            }

            if (progressSmoothing <= 0f)
            {
                issue = "ProgressSmoothing must be greater than zero.";
                return false;
            }

            if (exitDelaySeconds < 0f)
            {
                issue = "ExitDelaySeconds cannot be negative.";
                return false;
            }

            issue = string.Empty;
            return true;
        }

        private static bool Require(int value, string assetKind, string fieldName, out string issue)
        {
            if (value <= 0)
            {
                issue = $"Required {assetKind} Asset ID {fieldName} must be positive.";
                return false;
            }

            issue = string.Empty;
            return true;
        }
    }
}
