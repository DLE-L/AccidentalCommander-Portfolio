using UnityEngine;

namespace Lizzo.PV.Presentation
{
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
                return false;
            if (_stingerToBgmDelaySeconds < 0f)
            {
                issue = "StingerToBgmDelaySeconds cannot be negative.";
                return false;
            }
            issue = string.Empty;
            return true;
        }

#if UNITY_EDITOR
        public void SetForEditor(LocalizationKey titleLocalizationKey, SpriteAssetId glowSpriteId,
            SpriteAssetId emblemSpriteId, SpriteAssetId bannerSpriteId, ColorRole colorRole,
            AudioAssetId outcomeStingerId, AudioAssetId resultBgmId, MotionAssetId popupEnterMotionId,
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
}
