using Lizzo.PV.Gameplay.Feedback;
using UnityEngine;

namespace Lizzo.PV.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class GameplayFeedbackController : MonoBehaviour
    {
        [SerializeField]
        private GameplayBossWarningView _bossWarning;

        [SerializeField]
        private GameplayThreatDirectionView _threatDirection;

        [SerializeField]
        private RectTransform _viewport;

        [SerializeField]
        private Camera _worldCamera;

        public bool IsThreatDirectionVisible => _threatDirection != null && _threatDirection.IsVisible;
        public bool IsBossWarningVisible => _bossWarning != null && _bossWarning.IsVisible;
        public bool IsBossWarningSuspended => _bossWarning != null && _bossWarning.IsSuspended;

        public bool Configure()
        {
            if (_bossWarning == null
                || _threatDirection == null
                || _viewport == null
                || _worldCamera == null)
            {
                Debug.LogError("[GameplayFeedbackController] Authored feedback references are required.", this);
                return false;
            }

            return _bossWarning.Configure()
                && _threatDirection.Configure(_viewport, _worldCamera);
        }

        public bool ShowBossWarning(
            string text,
            Color accent,
            float durationSeconds,
            bool showEdges)
        {
            return Configure() && _bossWarning.Show(text, accent, durationSeconds, showEdges);
        }

        public void HideBossWarning()
        {
            if (_bossWarning == null)
            {
                Debug.LogError("[GameplayFeedbackController] GameplayBossWarningView is required.", this);
                return;
            }

            _bossWarning.Hide();
        }

        public void SetBossWarningSuspended(bool suspended)
        {
            if (_bossWarning == null)
            {
                Debug.LogError("[GameplayFeedbackController] GameplayBossWarningView is required.", this);
                return;
            }

            _bossWarning.SetSuspended(suspended);
        }

        public bool ShowThreatDirection(
            Transform target,
            Sprite icon,
            string label,
            Color accent,
            float durationSeconds = 0f)
        {
            return Configure()
                && _threatDirection.Show(target, icon, label, accent, durationSeconds);
        }

        public void HideThreatDirection()
        {
            if (_threatDirection == null)
            {
                Debug.LogError("[GameplayFeedbackController] GameplayThreatDirectionView is required.", this);
                return;
            }

            _threatDirection.Hide();
        }
    }
}
