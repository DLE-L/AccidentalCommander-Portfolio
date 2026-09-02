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
            {
                return false;
            }

            if (_acceptedDisplaySeconds <= 0f)
            {
                issue = "AcceptedDisplaySeconds must be greater than zero.";
                return false;
            }

            issue = string.Empty;
            return true;
        }

#if UNITY_EDITOR
        public void SetForEditor(
            SpriteAssetId dimmerSpriteId,
            SpriteAssetId headerSpriteId,
            LocalizationKey titleLocalizationKey,
            SpriteAssetId statusBadgeSpriteId,
            SpriteAssetId progressEmptySpriteId,
            SpriteAssetId progressFilledSpriteId,
            AudioAssetId cardOfferOpenSfxId,
            AudioAssetId cardAcceptedSfxId,
            AudioAssetId cardOfferCloseSfxId,
            MotionAssetId overlayEnterMotionId,
            MotionAssetId overlayExitMotionId,
            MotionAssetId cardAcceptedMotionId,
            MotionAssetId cardDisabledMotionId,
            float acceptedDisplaySeconds)
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

    [CreateAssetMenu(menuName = "Lizzo PV/Presentation/Gameplay/Pause Profile", fileName = "PausePresentationProfile")]
    public sealed class PausePresentationProfileSO : ScriptableObject
    {
        private static readonly LocalizationKey PauseTitleKey = new LocalizationKey("ui.pause.title");

        [SerializeField] private SpriteAssetId _dimmerSpriteId;
        [SerializeField] private SpriteAssetId _panelSpriteId;
        [SerializeField] private LocalizationKey _synergyEmptyLocalizationKey;
        [SerializeField] private AudioAssetId _pauseEnterSfxId;
        [SerializeField] private AudioAssetId _resumeAcceptedSfxId;
        [SerializeField] private AudioAssetId _abandonAcceptedSfxId;
        [SerializeField] private MotionAssetId _overlayEnterMotionId;
        [SerializeField] private MotionAssetId _overlayExitMotionId;
        [SerializeField] private MotionAssetId _resumeAcceptedMotionId;
        [SerializeField] private MotionAssetId _abandonAcceptedMotionId;
        [SerializeField] private MotionAssetId _summaryItemEnterMotionId;

        public SpriteAssetId DimmerSpriteId => _dimmerSpriteId;
        public SpriteAssetId PanelSpriteId => _panelSpriteId;
        public LocalizationKey TitleLocalizationKey => PauseTitleKey;
        public LocalizationKey SynergyEmptyLocalizationKey => _synergyEmptyLocalizationKey;
        public ControlStyleRole ResumeButtonStyleRole => ControlStyleRole.PrimaryButton;
        public ControlStyleRole AbandonButtonStyleRole => ControlStyleRole.DestructiveButton;
        public AudioAssetId PauseEnterSfxId => _pauseEnterSfxId;
        public AudioAssetId ResumeAcceptedSfxId => _resumeAcceptedSfxId;
        public AudioAssetId AbandonAcceptedSfxId => _abandonAcceptedSfxId;
        public MotionAssetId OverlayEnterMotionId => _overlayEnterMotionId;
        public MotionAssetId OverlayExitMotionId => _overlayExitMotionId;
        public MotionAssetId ResumeAcceptedMotionId => _resumeAcceptedMotionId;
        public MotionAssetId AbandonAcceptedMotionId => _abandonAcceptedMotionId;
        public MotionAssetId SummaryItemEnterMotionId => _summaryItemEnterMotionId;

        public bool TryValidate(out string issue)
        {
            return GameplayOverlayProfileValidation.Require(_panelSpriteId, nameof(PanelSpriteId), out issue)
                   && GameplayOverlayProfileValidation.Require(_synergyEmptyLocalizationKey, nameof(SynergyEmptyLocalizationKey), out issue)
                   && GameplayOverlayProfileValidation.Require(_pauseEnterSfxId, nameof(PauseEnterSfxId), out issue)
                   && GameplayOverlayProfileValidation.Require(_resumeAcceptedSfxId, nameof(ResumeAcceptedSfxId), out issue)
                   && GameplayOverlayProfileValidation.Require(_abandonAcceptedSfxId, nameof(AbandonAcceptedSfxId), out issue)
                   && GameplayOverlayProfileValidation.Require(_overlayEnterMotionId, nameof(OverlayEnterMotionId), out issue)
                   && GameplayOverlayProfileValidation.Require(_overlayExitMotionId, nameof(OverlayExitMotionId), out issue)
                   && GameplayOverlayProfileValidation.Require(_resumeAcceptedMotionId, nameof(ResumeAcceptedMotionId), out issue)
                   && GameplayOverlayProfileValidation.Require(_abandonAcceptedMotionId, nameof(AbandonAcceptedMotionId), out issue)
                   && GameplayOverlayProfileValidation.Require(_summaryItemEnterMotionId, nameof(SummaryItemEnterMotionId), out issue);
        }

#if UNITY_EDITOR
        public void SetForEditor(
            SpriteAssetId dimmerSpriteId,
            SpriteAssetId panelSpriteId,
            LocalizationKey synergyEmptyLocalizationKey,
            AudioAssetId pauseEnterSfxId,
            AudioAssetId resumeAcceptedSfxId,
            AudioAssetId abandonAcceptedSfxId,
            MotionAssetId overlayEnterMotionId,
            MotionAssetId overlayExitMotionId,
            MotionAssetId resumeAcceptedMotionId,
            MotionAssetId abandonAcceptedMotionId,
            MotionAssetId summaryItemEnterMotionId)
        {
            _dimmerSpriteId = dimmerSpriteId;
            _panelSpriteId = panelSpriteId;
            _synergyEmptyLocalizationKey = synergyEmptyLocalizationKey;
            _pauseEnterSfxId = pauseEnterSfxId;
            _resumeAcceptedSfxId = resumeAcceptedSfxId;
            _abandonAcceptedSfxId = abandonAcceptedSfxId;
            _overlayEnterMotionId = overlayEnterMotionId;
            _overlayExitMotionId = overlayExitMotionId;
            _resumeAcceptedMotionId = resumeAcceptedMotionId;
            _abandonAcceptedMotionId = abandonAcceptedMotionId;
            _summaryItemEnterMotionId = summaryItemEnterMotionId;
        }
#endif
    }

    internal static class GameplayOverlayProfileValidation
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

        public static bool Require(LocalizationKey key, string fieldName, out string issue)
        {
            if (key.IsNone)
            {
                issue = $"Required LocalizationKey {fieldName} cannot be empty.";
                return false;
            }

            issue = string.Empty;
            return true;
        }

        public static bool Require(ColorRole role, string fieldName, out string issue)
        {
            if (role.IsNone)
            {
                issue = $"Required ColorRole {fieldName} cannot be empty.";
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
