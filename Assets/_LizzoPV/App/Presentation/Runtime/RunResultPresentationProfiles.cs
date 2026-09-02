using UnityEngine;

namespace Lizzo.PV.Presentation
{
    [CreateAssetMenu(menuName = "Lizzo PV/Presentation/Gameplay/Run Result Shared Profile", fileName = "RunResultSharedPresentationProfile")]
    public sealed class RunResultSharedPresentationProfileSO : ScriptableObject
    {
        [SerializeField] private SpriteAssetId _dimmerSpriteId;
        [SerializeField] private LocalizationKey _mainLocalizationKey;
        [SerializeField] private AudioAssetId _rewardRevealSfxId;
        [SerializeField] private AudioAssetId _mainAcceptedSfxId;
        [SerializeField] private MotionAssetId _rewardRevealMotionId;
        [SerializeField] private MotionAssetId _mainAcceptedMotionId;
        [SerializeField] private MotionAssetId _popupExitMotionId;
        [SerializeField, Min(0f)] private float _rewardItemStaggerSeconds;

        public SpriteAssetId DimmerSpriteId => _dimmerSpriteId;
        public ControlStyleRole MainButtonStyleRole => ControlStyleRole.PrimaryButton;
        public LocalizationKey MainLocalizationKey => _mainLocalizationKey;
        public AudioAssetId RewardRevealSfxId => _rewardRevealSfxId;
        public AudioAssetId MainAcceptedSfxId => _mainAcceptedSfxId;
        public MotionAssetId RewardRevealMotionId => _rewardRevealMotionId;
        public MotionAssetId MainAcceptedMotionId => _mainAcceptedMotionId;
        public MotionAssetId PopupExitMotionId => _popupExitMotionId;
        public float RewardItemStaggerSeconds => _rewardItemStaggerSeconds;

        public bool TryValidate(out string issue)
        {
            if (!GameplayOverlayProfileValidation.Require(_mainLocalizationKey, nameof(MainLocalizationKey), out issue)
                || !GameplayOverlayProfileValidation.Require(_rewardRevealSfxId, nameof(RewardRevealSfxId), out issue)
                || !GameplayOverlayProfileValidation.Require(_mainAcceptedSfxId, nameof(MainAcceptedSfxId), out issue)
                || !GameplayOverlayProfileValidation.Require(_rewardRevealMotionId, nameof(RewardRevealMotionId), out issue)
                || !GameplayOverlayProfileValidation.Require(_mainAcceptedMotionId, nameof(MainAcceptedMotionId), out issue)
                || !GameplayOverlayProfileValidation.Require(_popupExitMotionId, nameof(PopupExitMotionId), out issue))
            {
                return false;
            }

            if (_rewardItemStaggerSeconds < 0f)
            {
                issue = "RewardItemStaggerSeconds cannot be negative.";
                return false;
            }

            issue = string.Empty;
            return true;
        }

#if UNITY_EDITOR
        public void SetForEditor(
            SpriteAssetId dimmerSpriteId,
            LocalizationKey mainLocalizationKey,
            AudioAssetId rewardRevealSfxId,
            AudioAssetId mainAcceptedSfxId,
            MotionAssetId rewardRevealMotionId,
            MotionAssetId mainAcceptedMotionId,
            MotionAssetId popupExitMotionId,
            float rewardItemStaggerSeconds)
        {
            _dimmerSpriteId = dimmerSpriteId;
            _mainLocalizationKey = mainLocalizationKey;
            _rewardRevealSfxId = rewardRevealSfxId;
            _mainAcceptedSfxId = mainAcceptedSfxId;
            _rewardRevealMotionId = rewardRevealMotionId;
            _mainAcceptedMotionId = mainAcceptedMotionId;
            _popupExitMotionId = popupExitMotionId;
            _rewardItemStaggerSeconds = rewardItemStaggerSeconds;
        }
#endif
    }

    [CreateAssetMenu(menuName = "Lizzo PV/Presentation/Gameplay/Run Result Outcome Profile", fileName = "RunResultPresentationProfile")]
    public sealed class RunResultPresentationProfileSO : ScriptableObject
    {
        [SerializeField] private LocalizationKey _titleLocalizationKey;
        [SerializeField] private SpriteAssetId _glowSpriteId;
        [SerializeField] private SpriteAssetId _emblemSpriteId;
        [SerializeField] private SpriteAssetId _bannerSpriteId;
        [SerializeField] private ColorRole _colorRole;
        [SerializeField] private AudioAssetId _outcomeStingerId;
        [SerializeField] private AudioAssetId _resultBgmId;
        [SerializeField] private MotionAssetId _popupEnterMotionId;
        [SerializeField, Min(0f)] private float _stingerToBgmDelaySeconds;

        public LocalizationKey TitleLocalizationKey => _titleLocalizationKey;
        public SpriteAssetId GlowSpriteId => _glowSpriteId;
        public SpriteAssetId EmblemSpriteId => _emblemSpriteId;
        public SpriteAssetId BannerSpriteId => _bannerSpriteId;
        public ColorRole ColorRole => _colorRole;
        public AudioAssetId OutcomeStingerId => _outcomeStingerId;
        public AudioAssetId ResultBgmId => _resultBgmId;
        public MotionAssetId PopupEnterMotionId => _popupEnterMotionId;
        public float StingerToBgmDelaySeconds => _stingerToBgmDelaySeconds;

        public bool TryValidate(out string issue)
        {
            if (!GameplayOverlayProfileValidation.Require(_titleLocalizationKey, nameof(TitleLocalizationKey), out issue)
                || !GameplayOverlayProfileValidation.Require(_glowSpriteId, nameof(GlowSpriteId), out issue)
                || !GameplayOverlayProfileValidation.Require(_emblemSpriteId, nameof(EmblemSpriteId), out issue)
                || !GameplayOverlayProfileValidation.Require(_bannerSpriteId, nameof(BannerSpriteId), out issue)
                || !GameplayOverlayProfileValidation.Require(_colorRole, nameof(ColorRole), out issue)
                || !GameplayOverlayProfileValidation.Require(_outcomeStingerId, nameof(OutcomeStingerId), out issue)
                || !GameplayOverlayProfileValidation.Require(_popupEnterMotionId, nameof(PopupEnterMotionId), out issue))
            {
                return false;
            }

            if (_stingerToBgmDelaySeconds < 0f)
            {
                issue = "StingerToBgmDelaySeconds cannot be negative.";
                return false;
            }

            issue = string.Empty;
            return true;
        }

#if UNITY_EDITOR
        public void SetForEditor(
            LocalizationKey titleLocalizationKey,
            SpriteAssetId glowSpriteId,
            SpriteAssetId emblemSpriteId,
            SpriteAssetId bannerSpriteId,
            ColorRole colorRole,
            AudioAssetId outcomeStingerId,
            AudioAssetId resultBgmId,
            MotionAssetId popupEnterMotionId,
            float stingerToBgmDelaySeconds)
        {
            _titleLocalizationKey = titleLocalizationKey;
            _glowSpriteId = glowSpriteId;
            _emblemSpriteId = emblemSpriteId;
            _bannerSpriteId = bannerSpriteId;
            _colorRole = colorRole;
            _outcomeStingerId = outcomeStingerId;
            _resultBgmId = resultBgmId;
            _popupEnterMotionId = popupEnterMotionId;
            _stingerToBgmDelaySeconds = stingerToBgmDelaySeconds;
        }
#endif
    }

    [CreateAssetMenu(menuName = "Lizzo PV/Presentation/Gameplay/Run Result Presentation Set", fileName = "RunResultPresentationSet")]
    public sealed class RunResultPresentationSetSO : ScriptableObject
    {
        [SerializeField] private RunResultSharedPresentationProfileSO _sharedProfile;
        [SerializeField] private RunResultPresentationProfileSO _victoryProfile;
        [SerializeField] private RunResultPresentationProfileSO _failureProfile;
        [SerializeField] private RunResultPresentationProfileSO _abandonedProfile;

        public RunResultSharedPresentationProfileSO SharedProfile => _sharedProfile;
        public RunResultPresentationProfileSO VictoryProfile => _victoryProfile;
        public RunResultPresentationProfileSO FailureProfile => _failureProfile;
        public RunResultPresentationProfileSO AbandonedProfile => _abandonedProfile;

        public bool TryValidate(out string issue)
        {
            if (_sharedProfile == null || _victoryProfile == null || _failureProfile == null || _abandonedProfile == null)
            {
                issue = "RunResultPresentationSet requires Shared, Victory, Failure, and Abandoned Profiles.";
                return false;
            }

            return _sharedProfile.TryValidate(out issue)
                   && _victoryProfile.TryValidate(out issue)
                   && _failureProfile.TryValidate(out issue)
                   && _abandonedProfile.TryValidate(out issue);
        }

#if UNITY_EDITOR
        public void SetForEditor(
            RunResultSharedPresentationProfileSO sharedProfile,
            RunResultPresentationProfileSO victoryProfile,
            RunResultPresentationProfileSO failureProfile,
            RunResultPresentationProfileSO abandonedProfile)
        {
            _sharedProfile = sharedProfile;
            _victoryProfile = victoryProfile;
            _failureProfile = failureProfile;
            _abandonedProfile = abandonedProfile;
        }
#endif
    }
}
