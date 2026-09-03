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
                return false;
            if (_rewardItemStaggerSeconds < 0f)
            {
                issue = "RewardItemStaggerSeconds cannot be negative.";
                return false;
            }
            issue = string.Empty;
            return true;
        }

#if UNITY_EDITOR
        public void SetForEditor(SpriteAssetId dimmerSpriteId, LocalizationKey mainLocalizationKey,
            AudioAssetId rewardRevealSfxId, AudioAssetId mainAcceptedSfxId,
            MotionAssetId rewardRevealMotionId, MotionAssetId mainAcceptedMotionId,
            MotionAssetId popupExitMotionId, float rewardItemStaggerSeconds)
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
}
