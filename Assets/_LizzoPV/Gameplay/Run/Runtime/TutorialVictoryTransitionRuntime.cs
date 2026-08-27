using System;
using Lizzo.PV.Flow;
using Lizzo.PV.P0.Units;

namespace Lizzo.PV.Gameplay.Run
{
    internal sealed class TutorialVictoryTransitionRuntime
    {
        readonly TutorialVictoryTransitionCoordinator _coordinator =
            new TutorialVictoryTransitionCoordinator();
        readonly RunServicesTutorialVictoryTransitionTarget _target;

        internal TutorialVictoryTransitionRuntime(
            RunServices services,
            RunPauseController pauseController,
            StageSpawner stageSpawner,
            EliteSpawnController eliteSpawnController,
            BossSpawnController bossSpawnController)
        {
            _target = new RunServicesTutorialVictoryTransitionTarget(
                services,
                pauseController,
                stageSpawner,
                eliteSpawnController,
                bossSpawnController);
        }

        internal bool TryBegin()
        {
            return _coordinator.TryBegin(_target);
        }

        internal bool Tick(float unscaledDeltaTime)
        {
            return _coordinator.Tick(unscaledDeltaTime, _target);
        }
    }

    internal sealed class RunServicesTutorialVictoryTransitionTarget
        : ITutorialVictoryTransitionTarget
    {
        readonly RunServices _services;
        readonly RunPauseController _pauseController;
        readonly StageSpawner _stageSpawner;
        readonly EliteSpawnController _eliteSpawnController;
        readonly BossSpawnController _bossSpawnController;

        internal RunServicesTutorialVictoryTransitionTarget(
            RunServices services,
            RunPauseController pauseController,
            StageSpawner stageSpawner,
            EliteSpawnController eliteSpawnController,
            BossSpawnController bossSpawnController)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _pauseController = pauseController ?? throw new ArgumentNullException(nameof(pauseController));
            _stageSpawner = stageSpawner;
            _eliteSpawnController = eliteSpawnController;
            _bossSpawnController = bossSpawnController;
        }

        public RunContext Context => _services.Context;

        public void StopEnemySpawning()
        {
            if (_stageSpawner != null)
                _stageSpawner.Stopped = true;
            if (_eliteSpawnController != null)
                _eliteSpawnController.enabled = false;
            if (_bossSpawnController != null)
                _bossSpawnController.enabled = false;
        }

        public void LockGameplay()
        {
            _pauseController.MarkRunEnded();
        }

        public void ClearRemainingEnemies()
        {
            _services.Registry.ReleaseAllEnemies();
        }

        public bool TryCompleteTutorialClear()
        {
            return _services.State.TryEnd(RunOutcome.Clear, 0);
        }
    }
}
