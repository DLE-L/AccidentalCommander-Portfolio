using UnityEngine;

namespace Lizzo.PV.Presentation
{
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
}
