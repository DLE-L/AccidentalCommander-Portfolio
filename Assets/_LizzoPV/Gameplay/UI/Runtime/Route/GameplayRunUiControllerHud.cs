using UnityEngine;

namespace Lizzo.PV.Gameplay.Route
{
    public sealed partial class GameplayRunUiController
    {
        public void SetGameplaySpeed(float speed)
        {
            EnsureInitialized();
            _hudController.SetGameplaySpeed(speed);
        }

        public void SetRunStatus(int kills, float survivalSeconds)
        {
            EnsureInitialized();
            int remainingSeconds = RunTimerDisplayPolicy.ResolveRemainingSeconds(
                _services.Context,
                _services.App.Data.RunTuning,
                survivalSeconds);
            _hudController.SetRunStatus(kills, remainingSeconds);
        }

        public void SetExperienceStatus(int level, float currentExperience, float requiredExperience)
        {
            EnsureInitialized();
            _hudController.SetExperience(level, currentExperience, requiredExperience);
        }

        public void ShowBoss(string name, int hp, int maxHp)
        {
            EnsureInitialized();
            if (_hudController.ShowBoss(hp, maxHp))
                BossVisibilityChanged?.Invoke(true);
        }

        public void HideBoss()
        {
            if (_initialized)
            {
                if (_hudController.HideBoss())
                    BossVisibilityChanged?.Invoke(false);
            }
        }

        public void ShowBossPreWarning(string text, Color accentColor, float durationSeconds, bool showEdges)
        {
            if (_initialized)
            {
                UpdateBossWarningSuspension();
                if (_feedbackController.ShowBossWarning(text, accentColor, durationSeconds, showEdges)
                    && !_feedbackController.IsBossWarningSuspended
                    && _feedbackController.IsBossWarningVisible)
                    BossWarningOpened?.Invoke();
            }
        }

        public void HideBossPreWarning()
        {
            if (_initialized)
                _feedbackController.HideBossWarning();
        }

        public void ShowThreatDirection(Transform target, string label, Color accentColor, float durationSeconds = 0.0f)
        {
            if (_initialized)
                _feedbackController.ShowThreatDirection(target, null, label, accentColor, durationSeconds);
        }

        public void HideThreatDirection()
        {
            if (_initialized)
                _feedbackController.HideThreatDirection();
        }

    }
}
