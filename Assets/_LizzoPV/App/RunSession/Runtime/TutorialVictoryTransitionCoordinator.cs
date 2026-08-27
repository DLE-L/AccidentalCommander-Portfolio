using System;

namespace Lizzo.PV.Flow
{
    public interface ITutorialVictoryTransitionTarget
    {
        RunContext Context { get; }
        void StopEnemySpawning();
        void LockGameplay();
        void ClearRemainingEnemies();
        bool TryCompleteTutorialClear();
    }

    public sealed class TutorialVictoryTransitionCoordinator
    {
        public const float ShowcaseSeconds = 2.5f;

        bool _isActive;
        bool _enemiesCleared;
        bool _isCompleted;
        float _elapsedSeconds;

        public bool TryBegin(ITutorialVictoryTransitionTarget target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            if (target.Context.IsTutorial == false || _isActive || _isCompleted)
                return false;

            target.StopEnemySpawning();
            target.LockGameplay();
            _isActive = true;
            return true;
        }

        public bool Tick(float unscaledDeltaTime, ITutorialVictoryTransitionTarget target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            if (_isActive == false)
                return false;

            if (_enemiesCleared == false)
            {
                target.ClearRemainingEnemies();
                _enemiesCleared = true;
            }

            if (float.IsNaN(unscaledDeltaTime) == false
                && float.IsInfinity(unscaledDeltaTime) == false
                && unscaledDeltaTime > 0.0f)
            {
                _elapsedSeconds += unscaledDeltaTime;
            }

            if (_elapsedSeconds < ShowcaseSeconds || target.TryCompleteTutorialClear() == false)
                return false;

            _isActive = false;
            _isCompleted = true;
            return true;
        }
    }
}
