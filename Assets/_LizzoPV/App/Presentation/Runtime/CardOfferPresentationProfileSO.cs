using UnityEngine;

namespace Lizzo.PV.Presentation
{
    [CreateAssetMenu(menuName = "Lizzo PV/Presentation/Gameplay/Card Offer Profile", fileName = "CardOfferPresentationProfile")]
    public sealed class CardOfferPresentationProfileSO : ScriptableObject
    {
        [SerializeField] private SpriteAssetId _dimmerSpriteId;
        [SerializeField] private SpriteAssetId _headerSpriteId;
        [SerializeField] private LocalizationKey _titleLocalizationKey;
        [SerializeField] private SpriteAssetId _statusBadgeSpriteId;
        [SerializeField] private SpriteAssetId _progressEmptySpriteId;
        [SerializeField] private SpriteAssetId _progressFilledSpriteId;
        [SerializeField] private AudioAssetId _cardOfferOpenSfxId;
        [SerializeField] private AudioAssetId _cardAcceptedSfxId;
        [SerializeField] private AudioAssetId _cardOfferCloseSfxId;
        [SerializeField] private MotionAssetId _overlayEnterMotionId;
        [SerializeField] private MotionAssetId _overlayExitMotionId;
        [SerializeField] private MotionAssetId _cardAcceptedMotionId;
        [SerializeField] private MotionAssetId _cardDisabledMotionId;
        [SerializeField, Min(0f)] private float _acceptedDisplaySeconds;

        public SpriteAssetId DimmerSpriteId => _dimmerSpriteId;
        public SpriteAssetId HeaderSpriteId => _headerSpriteId;
        public LocalizationKey TitleLocalizationKey => _titleLocalizationKey;
        public ControlStyleRole ChoiceCardStyleRole => ControlStyleRole.ChoiceCard;
        public SpriteAssetId StatusBadgeSpriteId => _statusBadgeSpriteId;
        public SpriteAssetId ProgressEmptySpriteId => _progressEmptySpriteId;
        public SpriteAssetId ProgressFilledSpriteId => _progressFilledSpriteId;
        public AudioAssetId CardOfferOpenSfxId => _cardOfferOpenSfxId;
        public AudioAssetId CardAcceptedSfxId => _cardAcceptedSfxId;
        public AudioAssetId CardOfferCloseSfxId => _cardOfferCloseSfxId;
        public MotionAssetId OverlayEnterMotionId => _overlayEnterMotionId;
        public MotionAssetId OverlayExitMotionId => _overlayExitMotionId;
        public MotionAssetId CardAcceptedMotionId => _cardAcceptedMotionId;
        public MotionAssetId CardDisabledMotionId => _cardDisabledMotionId;
        public float AcceptedDisplaySeconds => _acceptedDisplaySeconds;

        public bool TryValidate(out string issue)
        {
            if (!GameplayOverlayProfileValidation.Require(_titleLocalizationKey, nameof(TitleLocalizationKey), out issue)
                || !GameplayOverlayProfileValidation.Require(_statusBadgeSpriteId, nameof(StatusBadgeSpriteId), out issue)
                || !GameplayOverlayProfileValidation.Require(_progressEmptySpriteId, nameof(ProgressEmptySpriteId), out issue)
                || !GameplayOverlayProfileValidation.Require(_progressFilledSpriteId, nameof(ProgressFilledSpriteId), out issue)
                || !GameplayOverlayProfileValidation.Require(_cardOfferOpenSfxId, nameof(CardOfferOpenSfxId), out issue)
                || !GameplayOverlayProfileValidation.Require(_cardAcceptedSfxId, nameof(CardAcceptedSfxId), out issue)
                || !GameplayOverlayProfileValidation.Require(_cardOfferCloseSfxId, nameof(CardOfferCloseSfxId), out issue)
                || !GameplayOverlayProfileValidation.Require(_overlayEnterMotionId, nameof(OverlayEnterMotionId), out issue)
                || !GameplayOverlayProfileValidation.Require(_overlayExitMotionId, nameof(OverlayExitMotionId), out issue)
                || !GameplayOverlayProfileValidation.Require(_cardAcceptedMotionId, nameof(CardAcceptedMotionId), out issue)
                || !GameplayOverlayProfileValidation.Require(_cardDisabledMotionId, nameof(CardDisabledMotionId), out issue))
                return false;
            if (_acceptedDisplaySeconds <= 0f)
            {
                issue = "AcceptedDisplaySeconds must be greater than zero.";
                return false;
            }
            issue = string.Empty;
            return true;
        }

#if UNITY_EDITOR
        public void SetForEditor(SpriteAssetId dimmerSpriteId, SpriteAssetId headerSpriteId,
            LocalizationKey titleLocalizationKey, SpriteAssetId statusBadgeSpriteId,
            SpriteAssetId progressEmptySpriteId, SpriteAssetId progressFilledSpriteId,
            AudioAssetId cardOfferOpenSfxId, AudioAssetId cardAcceptedSfxId,
            AudioAssetId cardOfferCloseSfxId, MotionAssetId overlayEnterMotionId,
            MotionAssetId overlayExitMotionId, MotionAssetId cardAcceptedMotionId,
            MotionAssetId cardDisabledMotionId, float acceptedDisplaySeconds)
        {
            _dimmerSpriteId = dimmerSpriteId;
            _headerSpriteId = headerSpriteId;
            _titleLocalizationKey = titleLocalizationKey;
            _statusBadgeSpriteId = statusBadgeSpriteId;
            _progressEmptySpriteId = progressEmptySpriteId;
            _progressFilledSpriteId = progressFilledSpriteId;
            _cardOfferOpenSfxId = cardOfferOpenSfxId;
            _cardAcceptedSfxId = cardAcceptedSfxId;
            _cardOfferCloseSfxId = cardOfferCloseSfxId;
            _overlayEnterMotionId = overlayEnterMotionId;
            _overlayExitMotionId = overlayExitMotionId;
            _cardAcceptedMotionId = cardAcceptedMotionId;
            _cardDisabledMotionId = cardDisabledMotionId;
            _acceptedDisplaySeconds = acceptedDisplaySeconds;
        }
#endif
    }
}
