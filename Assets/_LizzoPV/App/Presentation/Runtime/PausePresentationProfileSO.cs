using UnityEngine;

namespace Lizzo.PV.Presentation
{
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

        public bool TryValidate(out string issue) =>
            GameplayOverlayProfileValidation.Require(_panelSpriteId, nameof(PanelSpriteId), out issue)
            && GameplayOverlayProfileValidation.Require(_synergyEmptyLocalizationKey, nameof(SynergyEmptyLocalizationKey), out issue)
            && GameplayOverlayProfileValidation.Require(_pauseEnterSfxId, nameof(PauseEnterSfxId), out issue)
            && GameplayOverlayProfileValidation.Require(_resumeAcceptedSfxId, nameof(ResumeAcceptedSfxId), out issue)
            && GameplayOverlayProfileValidation.Require(_abandonAcceptedSfxId, nameof(AbandonAcceptedSfxId), out issue)
            && GameplayOverlayProfileValidation.Require(_overlayEnterMotionId, nameof(OverlayEnterMotionId), out issue)
            && GameplayOverlayProfileValidation.Require(_overlayExitMotionId, nameof(OverlayExitMotionId), out issue)
            && GameplayOverlayProfileValidation.Require(_resumeAcceptedMotionId, nameof(ResumeAcceptedMotionId), out issue)
            && GameplayOverlayProfileValidation.Require(_abandonAcceptedMotionId, nameof(AbandonAcceptedMotionId), out issue)
            && GameplayOverlayProfileValidation.Require(_summaryItemEnterMotionId, nameof(SummaryItemEnterMotionId), out issue);

#if UNITY_EDITOR
        public void SetForEditor(SpriteAssetId dimmerSpriteId, SpriteAssetId panelSpriteId,
            LocalizationKey synergyEmptyLocalizationKey, AudioAssetId pauseEnterSfxId,
            AudioAssetId resumeAcceptedSfxId, AudioAssetId abandonAcceptedSfxId,
            MotionAssetId overlayEnterMotionId, MotionAssetId overlayExitMotionId,
            MotionAssetId resumeAcceptedMotionId, MotionAssetId abandonAcceptedMotionId,
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
}
