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
            _services.TickRuntime(deltaTime, time, frameCount, isPaused, isHitStopActive);
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
