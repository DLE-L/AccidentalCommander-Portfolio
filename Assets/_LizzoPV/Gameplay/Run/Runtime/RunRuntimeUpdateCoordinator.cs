using System;

namespace Lizzo.PV.Gameplay.Run
{
    internal sealed class RunRuntimeUpdateCoordinator
    {
        private readonly RunServices _services;
        private bool _runtimeResetForResult;

        internal RunRuntimeUpdateCoordinator(RunServices services)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
        }

        internal void Tick(
            float deltaTime,
            float time,
            int frameCount,
            bool isPaused,
            bool isHitStopActive,
            bool isResultGameplayLocked)
        {
            if (isResultGameplayLocked)
            {
                ResetForResult();
                return;
            }

            RearmResultReset();
            PlayerController commander = _services.Registry.Player;
            if (commander != null)
            {
                _services.RecordingCompanions?.Advance(
                    deltaTime,
                    isPaused || isHitStopActive,
                    commander.transform);
            }

            _services.TickSynergyRuntime(deltaTime, time, frameCount, isPaused);
            _services.PersistentFieldModule.Tick(time);
            _services.PersonalSummonModule.Tick(time, deltaTime);
        }

        private void ResetForResult()
        {
            if (_runtimeResetForResult)
                return;

            _services.ResetRuntimeForResult();
            _runtimeResetForResult = true;
        }

        private void RearmResultReset()
        {
            _runtimeResetForResult = false;
        }
    }
}
