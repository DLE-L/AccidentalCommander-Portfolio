using UnityEngine;

namespace Lizzo.PV.Presentation
{
    [CreateAssetMenu(menuName = "Lizzo PV/Presentation/Gameplay/Audio Profile", fileName = "GameplayAudioPresentationProfile")]
    public sealed class GameplayAudioPresentationProfileSO : ScriptableObject
    {
        [SerializeField] private AudioAssetId _gameplayBgmId;
        [SerializeField] private AudioAssetId _bossBgmId;
        [SerializeField] private AudioAssetId _stageAmbienceLoopSfxId;
        [SerializeField, Min(0f)] private float _gameplayBossCrossFadeSeconds;
        [SerializeField, Min(0f)] private float _resultFadeSeconds;

        public AudioAssetId GameplayBgmId => _gameplayBgmId;
        public AudioAssetId BossBgmId => _bossBgmId;
        public AudioAssetId StageAmbienceLoopSfxId => _stageAmbienceLoopSfxId;
        public float GameplayBossCrossFadeSeconds => _gameplayBossCrossFadeSeconds;
        public float ResultFadeSeconds => _resultFadeSeconds;

        public bool TryValidate(out string issue)
        {
            if (!GameplayCoreProfileValidation.Require(_gameplayBgmId, nameof(GameplayBgmId), out issue)
                || !GameplayCoreProfileValidation.Require(_bossBgmId, nameof(BossBgmId), out issue))
            {
                return false;
            }

            if (_gameplayBossCrossFadeSeconds < 0f)
            {
                issue = "GameplayBossCrossFadeSeconds cannot be negative.";
                return false;
            }

            if (_resultFadeSeconds < 0f)
            {
                issue = "ResultFadeSeconds cannot be negative.";
                return false;
            }

            issue = string.Empty;
            return true;
        }

#if UNITY_EDITOR
        public void SetForEditor(
            AudioAssetId gameplayBgmId,
            AudioAssetId bossBgmId,
            AudioAssetId stageAmbienceLoopSfxId,
            float gameplayBossCrossFadeSeconds,
            float resultFadeSeconds)
        {
            _gameplayBgmId = gameplayBgmId;
            _bossBgmId = bossBgmId;
            _stageAmbienceLoopSfxId = stageAmbienceLoopSfxId;
            _gameplayBossCrossFadeSeconds = gameplayBossCrossFadeSeconds;
            _resultFadeSeconds = resultFadeSeconds;
        }
#endif
    }

    [CreateAssetMenu(menuName = "Lizzo PV/Presentation/Gameplay/HUD Profile", fileName = "GameplayHudPresentationProfile")]
    public sealed class GameplayHudPresentationProfileSO : ScriptableObject
    {
        [SerializeField] private SpriteAssetId _killIconSpriteId;
        [SerializeField] private SpriteAssetId _timerFrameSpriteId;
        [SerializeField] private SpriteAssetId _experienceTrackSpriteId;
        [SerializeField] private SpriteAssetId _experienceFillSpriteId;
        [SerializeField] private SpriteAssetId _bossHealthTrackSpriteId;
        [SerializeField] private SpriteAssetId _bossHealthFillSpriteId;
        [SerializeField] private SpriteAssetId _traitSlotBackgroundSpriteId;
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
        public SpriteAssetId TraitSlotBackgroundSpriteId => _traitSlotBackgroundSpriteId;
        public SpriteAssetId PauseIconSpriteId => _pauseIconSpriteId;
        public SpriteAssetId SpeedIconSpriteId => _speedIconSpriteId;
        public ControlStyleRole IconButtonStyleRole => ControlStyleRole.IconButton;
        public AudioAssetId SpeedChangedSfxId => _speedChangedSfxId;
        public MotionAssetId SpeedChangedMotionId => _speedChangedMotionId;
        public MotionAssetId ExperienceToBossMotionId => _experienceToBossMotionId;
        public MotionAssetId BossToExperienceMotionId => _bossToExperienceMotionId;

        public bool TryValidate(out string issue)
        {
            return GameplayCoreProfileValidation.Require(_killIconSpriteId, nameof(KillIconSpriteId), out issue)
                   && GameplayCoreProfileValidation.Require(_timerFrameSpriteId, nameof(TimerFrameSpriteId), out issue)
                   && GameplayCoreProfileValidation.Require(_experienceTrackSpriteId, nameof(ExperienceTrackSpriteId), out issue)
                   && GameplayCoreProfileValidation.Require(_experienceFillSpriteId, nameof(ExperienceFillSpriteId), out issue)
                   && GameplayCoreProfileValidation.Require(_bossHealthTrackSpriteId, nameof(BossHealthTrackSpriteId), out issue)
                   && GameplayCoreProfileValidation.Require(_bossHealthFillSpriteId, nameof(BossHealthFillSpriteId), out issue)
                   && GameplayCoreProfileValidation.Require(_traitSlotBackgroundSpriteId, nameof(TraitSlotBackgroundSpriteId), out issue)
                   && GameplayCoreProfileValidation.Require(_pauseIconSpriteId, nameof(PauseIconSpriteId), out issue)
                   && GameplayCoreProfileValidation.Require(_speedIconSpriteId, nameof(SpeedIconSpriteId), out issue)
                   && GameplayCoreProfileValidation.Require(_speedChangedSfxId, nameof(SpeedChangedSfxId), out issue)
                   && GameplayCoreProfileValidation.Require(_speedChangedMotionId, nameof(SpeedChangedMotionId), out issue)
                   && GameplayCoreProfileValidation.Require(_experienceToBossMotionId, nameof(ExperienceToBossMotionId), out issue)
                   && GameplayCoreProfileValidation.Require(_bossToExperienceMotionId, nameof(BossToExperienceMotionId), out issue);
        }

#if UNITY_EDITOR
        public void SetForEditor(
            SpriteAssetId killIconSpriteId,
            SpriteAssetId timerFrameSpriteId,
            SpriteAssetId experienceTrackSpriteId,
            SpriteAssetId experienceFillSpriteId,
            SpriteAssetId bossHealthTrackSpriteId,
            SpriteAssetId bossHealthFillSpriteId,
            SpriteAssetId traitSlotBackgroundSpriteId,
            SpriteAssetId pauseIconSpriteId,
            SpriteAssetId speedIconSpriteId,
            AudioAssetId speedChangedSfxId,
            MotionAssetId speedChangedMotionId,
            MotionAssetId experienceToBossMotionId,
            MotionAssetId bossToExperienceMotionId)
        {
            _killIconSpriteId = killIconSpriteId;
            _timerFrameSpriteId = timerFrameSpriteId;
            _experienceTrackSpriteId = experienceTrackSpriteId;
            _experienceFillSpriteId = experienceFillSpriteId;
            _bossHealthTrackSpriteId = bossHealthTrackSpriteId;
            _bossHealthFillSpriteId = bossHealthFillSpriteId;
            _traitSlotBackgroundSpriteId = traitSlotBackgroundSpriteId;
            _pauseIconSpriteId = pauseIconSpriteId;
            _speedIconSpriteId = speedIconSpriteId;
            _speedChangedSfxId = speedChangedSfxId;
            _speedChangedMotionId = speedChangedMotionId;
            _experienceToBossMotionId = experienceToBossMotionId;
            _bossToExperienceMotionId = bossToExperienceMotionId;
        }
#endif
    }

    [CreateAssetMenu(menuName = "Lizzo PV/Presentation/Gameplay/Input Profile", fileName = "GameplayInputPresentationProfile")]
    public sealed class GameplayInputPresentationProfileSO : ScriptableObject
    {
        [SerializeField] private SpriteAssetId _joystickBackgroundSpriteId;
        [SerializeField] private SpriteAssetId _joystickCenterSpriteId;
        [SerializeField] private SpriteAssetId _joystickHandleSpriteId;
        [SerializeField] private MotionAssetId _appearMotionId;
        [SerializeField] private MotionAssetId _resetMotionId;

        public SpriteAssetId JoystickBackgroundSpriteId => _joystickBackgroundSpriteId;
        public SpriteAssetId JoystickCenterSpriteId => _joystickCenterSpriteId;
        public SpriteAssetId JoystickHandleSpriteId => _joystickHandleSpriteId;
        public MotionAssetId AppearMotionId => _appearMotionId;
        public MotionAssetId ResetMotionId => _resetMotionId;

        public bool TryValidate(out string issue)
        {
            return GameplayCoreProfileValidation.Require(_joystickBackgroundSpriteId, nameof(JoystickBackgroundSpriteId), out issue)
                   && GameplayCoreProfileValidation.Require(_joystickCenterSpriteId, nameof(JoystickCenterSpriteId), out issue)
                   && GameplayCoreProfileValidation.Require(_joystickHandleSpriteId, nameof(JoystickHandleSpriteId), out issue)
                   && GameplayCoreProfileValidation.Require(_appearMotionId, nameof(AppearMotionId), out issue)
                   && GameplayCoreProfileValidation.Require(_resetMotionId, nameof(ResetMotionId), out issue);
        }

#if UNITY_EDITOR
        public void SetForEditor(
            SpriteAssetId joystickBackgroundSpriteId,
            SpriteAssetId joystickCenterSpriteId,
            SpriteAssetId joystickHandleSpriteId,
            MotionAssetId appearMotionId,
            MotionAssetId resetMotionId)
        {
            _joystickBackgroundSpriteId = joystickBackgroundSpriteId;
            _joystickCenterSpriteId = joystickCenterSpriteId;
            _joystickHandleSpriteId = joystickHandleSpriteId;
            _appearMotionId = appearMotionId;
            _resetMotionId = resetMotionId;
        }
#endif
    }

    internal static class GameplayCoreProfileValidation
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
