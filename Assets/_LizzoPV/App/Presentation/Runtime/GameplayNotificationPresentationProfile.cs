using System;
using UnityEngine;

namespace Lizzo.PV.Presentation
{
    [Serializable]
    public struct CombatPhasePresentation
    {
        [SerializeField] private SpriteAssetId _frameSpriteId;
        [SerializeField] private LocalizationKey _localizationKey;
        [SerializeField] private AudioAssetId _alertSfxId;
        [SerializeField] private MotionAssetId _enterMotionId;
        [SerializeField] private MotionAssetId _pulseMotionId;
        [SerializeField] private MotionAssetId _exitMotionId;
        [SerializeField, Min(0f)] private float _displaySeconds;

        public CombatPhasePresentation(
            SpriteAssetId frameSpriteId,
            LocalizationKey localizationKey,
            AudioAssetId alertSfxId,
            MotionAssetId enterMotionId,
            MotionAssetId pulseMotionId,
            MotionAssetId exitMotionId,
            float displaySeconds)
        {
            _frameSpriteId = frameSpriteId;
            _localizationKey = localizationKey;
            _alertSfxId = alertSfxId;
            _enterMotionId = enterMotionId;
            _pulseMotionId = pulseMotionId;
            _exitMotionId = exitMotionId;
            _displaySeconds = displaySeconds;
        }

        public SpriteAssetId FrameSpriteId => _frameSpriteId;
        public LocalizationKey LocalizationKey => _localizationKey;
        public AudioAssetId AlertSfxId => _alertSfxId;
        public MotionAssetId EnterMotionId => _enterMotionId;
        public MotionAssetId PulseMotionId => _pulseMotionId;
        public MotionAssetId ExitMotionId => _exitMotionId;
        public float DisplaySeconds => _displaySeconds;

        public bool TryValidate(out string issue)
        {
            return NotificationProfileValidation.Require(_localizationKey, nameof(LocalizationKey), out issue)
                   && NotificationProfileValidation.Require(_enterMotionId, nameof(EnterMotionId), out issue)
                   && NotificationProfileValidation.Require(_exitMotionId, nameof(ExitMotionId), out issue)
                   && NotificationProfileValidation.RequireDuration(_displaySeconds, out issue);
        }
    }

    [Serializable]
    public struct EliteAlertPresentation
    {
        [SerializeField] private SpriteAssetId _frameSpriteId;
        [SerializeField] private LocalizationKey _localizationKey;
        [SerializeField] private AudioAssetId _alertSfxId;
        [SerializeField] private MotionAssetId _enterMotionId;
        [SerializeField] private MotionAssetId _pulseMotionId;
        [SerializeField] private MotionAssetId _exitMotionId;
        [SerializeField, Min(0f)] private float _displaySeconds;

        public EliteAlertPresentation(
            SpriteAssetId frameSpriteId,
            LocalizationKey localizationKey,
            AudioAssetId alertSfxId,
            MotionAssetId enterMotionId,
            MotionAssetId pulseMotionId,
            MotionAssetId exitMotionId,
            float displaySeconds)
        {
            _frameSpriteId = frameSpriteId;
            _localizationKey = localizationKey;
            _alertSfxId = alertSfxId;
            _enterMotionId = enterMotionId;
            _pulseMotionId = pulseMotionId;
            _exitMotionId = exitMotionId;
            _displaySeconds = displaySeconds;
        }

        public bool TryValidate(out string issue)
        {
            return NotificationProfileValidation.Require(_frameSpriteId, nameof(_frameSpriteId), out issue)
                   && NotificationProfileValidation.Require(_localizationKey, nameof(_localizationKey), out issue)
                   && NotificationProfileValidation.Require(_alertSfxId, nameof(_alertSfxId), out issue)
                   && NotificationProfileValidation.Require(_enterMotionId, nameof(_enterMotionId), out issue)
                   && NotificationProfileValidation.Require(_pulseMotionId, nameof(_pulseMotionId), out issue)
                   && NotificationProfileValidation.Require(_exitMotionId, nameof(_exitMotionId), out issue)
                   && NotificationProfileValidation.RequireDuration(_displaySeconds, out issue);
        }
    }

    [Serializable]
    public struct BossWarningPresentation
    {
        [SerializeField] private SpriteAssetId _frameSpriteId;
        [SerializeField] private SpriteAssetId _edgeAccentSpriteId;
        [SerializeField] private LocalizationKey _localizationKey;
        [SerializeField] private AudioAssetId _warningSfxId;
        [SerializeField] private MotionAssetId _enterMotionId;
        [SerializeField] private MotionAssetId _pulseMotionId;
        [SerializeField] private MotionAssetId _exitMotionId;
        [SerializeField, Min(0f)] private float _displaySeconds;

        public BossWarningPresentation(
            SpriteAssetId frameSpriteId,
            SpriteAssetId edgeAccentSpriteId,
            LocalizationKey localizationKey,
            AudioAssetId warningSfxId,
            MotionAssetId enterMotionId,
            MotionAssetId pulseMotionId,
            MotionAssetId exitMotionId,
            float displaySeconds)
        {
            _frameSpriteId = frameSpriteId;
            _edgeAccentSpriteId = edgeAccentSpriteId;
            _localizationKey = localizationKey;
            _warningSfxId = warningSfxId;
            _enterMotionId = enterMotionId;
            _pulseMotionId = pulseMotionId;
            _exitMotionId = exitMotionId;
            _displaySeconds = displaySeconds;
        }

        public bool TryValidate(out string issue)
        {
            return NotificationProfileValidation.Require(_frameSpriteId, nameof(_frameSpriteId), out issue)
                   && NotificationProfileValidation.Require(_edgeAccentSpriteId, nameof(_edgeAccentSpriteId), out issue)
                   && NotificationProfileValidation.Require(_localizationKey, nameof(_localizationKey), out issue)
                   && NotificationProfileValidation.Require(_warningSfxId, nameof(_warningSfxId), out issue)
                   && NotificationProfileValidation.Require(_enterMotionId, nameof(_enterMotionId), out issue)
                   && NotificationProfileValidation.Require(_pulseMotionId, nameof(_pulseMotionId), out issue)
                   && NotificationProfileValidation.Require(_exitMotionId, nameof(_exitMotionId), out issue)
                   && NotificationProfileValidation.RequireDuration(_displaySeconds, out issue);
        }
    }

    [Serializable]
    public struct SynergyNotificationPresentation
    {
        [SerializeField] private SpriteAssetId _bannerSpriteId;
        [SerializeField] private LocalizationKey _localizationKey;
        [SerializeField] private AudioAssetId _showSfxId;
        [SerializeField] private MotionAssetId _enterMotionId;
        [SerializeField] private MotionAssetId _exitMotionId;
        [SerializeField, Min(0f)] private float _displaySeconds;

        public SynergyNotificationPresentation(
            SpriteAssetId bannerSpriteId,
            LocalizationKey localizationKey,
            AudioAssetId showSfxId,
            MotionAssetId enterMotionId,
            MotionAssetId exitMotionId,
            float displaySeconds)
        {
            _bannerSpriteId = bannerSpriteId;
            _localizationKey = localizationKey;
            _showSfxId = showSfxId;
            _enterMotionId = enterMotionId;
            _exitMotionId = exitMotionId;
            _displaySeconds = displaySeconds;
        }

        public bool TryValidate(out string issue)
        {
            return NotificationProfileValidation.Require(_bannerSpriteId, nameof(_bannerSpriteId), out issue)
                   && NotificationProfileValidation.Require(_localizationKey, nameof(_localizationKey), out issue)
                   && NotificationProfileValidation.Require(_showSfxId, nameof(_showSfxId), out issue)
                   && NotificationProfileValidation.Require(_enterMotionId, nameof(_enterMotionId), out issue)
                   && NotificationProfileValidation.Require(_exitMotionId, nameof(_exitMotionId), out issue)
                   && NotificationProfileValidation.RequireDuration(_displaySeconds, out issue);
        }
    }

    [CreateAssetMenu(menuName = "Lizzo PV/Presentation/Gameplay/Notification Profile", fileName = "GameplayNotificationPresentationProfile")]
    public sealed class GameplayNotificationPresentationProfileSO : ScriptableObject
    {
        [SerializeField] private CombatPhasePresentation _combatPhasePresentation;
        [SerializeField] private EliteAlertPresentation _eliteAlertPresentation;
        [SerializeField] private BossWarningPresentation _bossWarningPresentation;
        [SerializeField] private SynergyNotificationPresentation _synergyNotificationPresentation;
        [SerializeField, Min(0f)] private float _notificationQueueGapSeconds;

        public CombatPhasePresentation CombatPhasePresentation => _combatPhasePresentation;
        public EliteAlertPresentation EliteAlertPresentation => _eliteAlertPresentation;
        public BossWarningPresentation BossWarningPresentation => _bossWarningPresentation;
        public SynergyNotificationPresentation SynergyNotificationPresentation => _synergyNotificationPresentation;
        public float NotificationQueueGapSeconds => _notificationQueueGapSeconds;

        public bool TryValidate(out string issue)
        {
            if (!_combatPhasePresentation.TryValidate(out issue)
                || !_eliteAlertPresentation.TryValidate(out issue)
                || !_bossWarningPresentation.TryValidate(out issue)
                || !_synergyNotificationPresentation.TryValidate(out issue))
            {
                return false;
            }

            if (_notificationQueueGapSeconds < 0f)
            {
                issue = "NotificationQueueGapSeconds cannot be negative.";
                return false;
            }

            issue = string.Empty;
            return true;
        }

#if UNITY_EDITOR
        public void SetForEditor(
            CombatPhasePresentation combatPhasePresentation,
            EliteAlertPresentation eliteAlertPresentation,
            BossWarningPresentation bossWarningPresentation,
            SynergyNotificationPresentation synergyNotificationPresentation,
            float notificationQueueGapSeconds)
        {
            _combatPhasePresentation = combatPhasePresentation;
            _eliteAlertPresentation = eliteAlertPresentation;
            _bossWarningPresentation = bossWarningPresentation;
            _synergyNotificationPresentation = synergyNotificationPresentation;
            _notificationQueueGapSeconds = notificationQueueGapSeconds;
        }
#endif
    }

    internal static class NotificationProfileValidation
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

        public static bool RequireDuration(float displaySeconds, out string issue)
        {
            if (displaySeconds <= 0f)
            {
                issue = "DisplaySeconds must be greater than zero.";
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
