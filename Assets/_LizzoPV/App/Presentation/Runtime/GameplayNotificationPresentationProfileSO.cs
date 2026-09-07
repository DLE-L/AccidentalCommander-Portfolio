using UnityEngine;

namespace Lizzo.PV.Presentation
{
    [CreateAssetMenu(menuName = "Lizzo PV/Presentation/Gameplay/Notification Profile", fileName = "GameplayNotificationPresentationProfile")]
    public sealed class GameplayNotificationPresentationProfileSO : ScriptableObject
    {
        [SerializeField] private CombatPhasePresentation _combatPhasePresentation;
        [SerializeField] private EliteAlertPresentation _eliteAlertPresentation;
        [SerializeField] private BossWarningPresentation _bossWarningPresentation;
        [SerializeField, Min(0f)] private float _notificationQueueGapSeconds;

        public CombatPhasePresentation CombatPhasePresentation => _combatPhasePresentation;
        public EliteAlertPresentation EliteAlertPresentation => _eliteAlertPresentation;
        public BossWarningPresentation BossWarningPresentation => _bossWarningPresentation;
        public float NotificationQueueGapSeconds => _notificationQueueGapSeconds;

        public bool TryValidate(out string issue)
        {
            if (!_combatPhasePresentation.TryValidate(out issue)
                || !_eliteAlertPresentation.TryValidate(out issue)
                || !_bossWarningPresentation.TryValidate(out issue))
                return false;
            if (_notificationQueueGapSeconds < 0f)
            {
                issue = "NotificationQueueGapSeconds cannot be negative.";
                return false;
            }
            issue = string.Empty;
            return true;
        }

#if UNITY_EDITOR
        public void SetForEditor(CombatPhasePresentation combatPhasePresentation,
            EliteAlertPresentation eliteAlertPresentation, BossWarningPresentation bossWarningPresentation,
            float notificationQueueGapSeconds)
        {
            _combatPhasePresentation = combatPhasePresentation;
            _eliteAlertPresentation = eliteAlertPresentation;
            _bossWarningPresentation = bossWarningPresentation;
            _notificationQueueGapSeconds = notificationQueueGapSeconds;
        }
#endif
    }
}
