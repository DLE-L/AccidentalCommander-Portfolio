using UnityEngine;

namespace Lizzo.PV.Presentation
{
    [CreateAssetMenu(menuName = "Lizzo PV/Presentation/Gameplay/HUD Profile", fileName = "GameplayHudPresentationProfile")]
    public sealed class GameplayHudPresentationProfileSO : ScriptableObject
    {
        [SerializeField] private SpriteAssetId _killIconSpriteId;
        [SerializeField] private SpriteAssetId _timerFrameSpriteId;
        [SerializeField] private SpriteAssetId _experienceTrackSpriteId;
        [SerializeField] private SpriteAssetId _experienceFillSpriteId;
        [SerializeField] private SpriteAssetId _bossHealthTrackSpriteId;
        [SerializeField] private SpriteAssetId _bossHealthFillSpriteId;
        [SerializeField] private SpriteAssetId _pauseIconSpriteId;
        [SerializeField] private SpriteAssetId _speedIconSpriteId;
        [SerializeField] private AudioAssetId _speedChangedSfxId;
        [SerializeField] private MotionAssetId _speedChangedMotionId;
        [SerializeField] private MotionAssetId _experienceToBossMotionId;
        [SerializeField] private MotionAssetId _bossToExperienceMotionId;

        public SpriteAssetId KillIconSpriteId => _killIconSpriteId;
        public SpriteAssetId TimerFrameSpriteId => _timerFrameSpriteId;
        public SpriteAssetId ExperienceTrackSpriteId => _experienceTrackSpriteId;
        public SpriteAssetId ExperienceFillSpriteId => _experienceFillSpriteId;
        public SpriteAssetId BossHealthTrackSpriteId => _bossHealthTrackSpriteId;
        public SpriteAssetId BossHealthFillSpriteId => _bossHealthFillSpriteId;
        public SpriteAssetId PauseIconSpriteId => _pauseIconSpriteId;
        public SpriteAssetId SpeedIconSpriteId => _speedIconSpriteId;
        public ControlStyleRole IconButtonStyleRole => ControlStyleRole.IconButton;
        public AudioAssetId SpeedChangedSfxId => _speedChangedSfxId;
        public MotionAssetId SpeedChangedMotionId => _speedChangedMotionId;
        public MotionAssetId ExperienceToBossMotionId => _experienceToBossMotionId;
        public MotionAssetId BossToExperienceMotionId => _bossToExperienceMotionId;

        public bool TryValidate(out string issue) =>
            GameplayCoreProfileValidation.Require(_killIconSpriteId, nameof(KillIconSpriteId), out issue)
            && GameplayCoreProfileValidation.Require(_timerFrameSpriteId, nameof(TimerFrameSpriteId), out issue)
            && GameplayCoreProfileValidation.Require(_experienceTrackSpriteId, nameof(ExperienceTrackSpriteId), out issue)
            && GameplayCoreProfileValidation.Require(_experienceFillSpriteId, nameof(ExperienceFillSpriteId), out issue)
            && GameplayCoreProfileValidation.Require(_bossHealthTrackSpriteId, nameof(BossHealthTrackSpriteId), out issue)
            && GameplayCoreProfileValidation.Require(_bossHealthFillSpriteId, nameof(BossHealthFillSpriteId), out issue)
            && GameplayCoreProfileValidation.Require(_pauseIconSpriteId, nameof(PauseIconSpriteId), out issue)
            && GameplayCoreProfileValidation.Require(_speedIconSpriteId, nameof(SpeedIconSpriteId), out issue)
            && GameplayCoreProfileValidation.Require(_speedChangedSfxId, nameof(SpeedChangedSfxId), out issue)
            && GameplayCoreProfileValidation.Require(_speedChangedMotionId, nameof(SpeedChangedMotionId), out issue)
            && GameplayCoreProfileValidation.Require(_experienceToBossMotionId, nameof(ExperienceToBossMotionId), out issue)
            && GameplayCoreProfileValidation.Require(_bossToExperienceMotionId, nameof(BossToExperienceMotionId), out issue);

#if UNITY_EDITOR
        public void SetForEditor(SpriteAssetId killIconSpriteId, SpriteAssetId timerFrameSpriteId,
            SpriteAssetId experienceTrackSpriteId, SpriteAssetId experienceFillSpriteId,
            SpriteAssetId bossHealthTrackSpriteId, SpriteAssetId bossHealthFillSpriteId,
            SpriteAssetId pauseIconSpriteId,
            SpriteAssetId speedIconSpriteId, AudioAssetId speedChangedSfxId,
            MotionAssetId speedChangedMotionId, MotionAssetId experienceToBossMotionId,
            MotionAssetId bossToExperienceMotionId)
        {
            _killIconSpriteId = killIconSpriteId;
            _timerFrameSpriteId = timerFrameSpriteId;
            _experienceTrackSpriteId = experienceTrackSpriteId;
            _experienceFillSpriteId = experienceFillSpriteId;
            _bossHealthTrackSpriteId = bossHealthTrackSpriteId;
            _bossHealthFillSpriteId = bossHealthFillSpriteId;
            _pauseIconSpriteId = pauseIconSpriteId;
            _speedIconSpriteId = speedIconSpriteId;
            _speedChangedSfxId = speedChangedSfxId;
            _speedChangedMotionId = speedChangedMotionId;
            _experienceToBossMotionId = experienceToBossMotionId;
            _bossToExperienceMotionId = bossToExperienceMotionId;
        }
#endif
    }
}
