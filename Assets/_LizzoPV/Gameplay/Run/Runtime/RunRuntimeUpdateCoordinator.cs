using System;

namespace Lizzo.PV.Gameplay.Run
{
    internal sealed class RunRuntimeUpdateCoordinator
    {
        private readonly RunServices _services;
        private bool _persistentFieldsResetForResult;
        private bool _personalSummonsResetForResult;
        private bool _passiveRosterResetForResult;
        private bool _synergyModulesResetForResult;
        private bool _recordingCompanionsStoppedForResult;

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
            if (_persistentFieldsResetForResult == false)
            {
                _services.PersistentFieldModule.Reset();
                _persistentFieldsResetForResult = true;
            }

            if (_personalSummonsResetForResult == false)
            {
                _services.PersonalSummonModule.Reset();
                _personalSummonsResetForResult = true;
            }

            if (_passiveRosterResetForResult == false)
            {
                _services.PassiveRoster.Reset();
                _passiveRosterResetForResult = true;
            }

            if (_synergyModulesResetForResult == false)
            {
                _services.ResetSynergyRuntimeForResult();
                _synergyModulesResetForResult = true;
            }

            if (_recordingCompanionsStoppedForResult == false)
            {
                _services.RecordingCompanions?.StopForResult();
                _recordingCompanionsStoppedForResult = true;
            }
        }

        private void RearmResultReset()
        {
            _persistentFieldsResetForResult = false;
            _personalSummonsResetForResult = false;
            _passiveRosterResetForResult = false;
            _synergyModulesResetForResult = false;
            _recordingCompanionsStoppedForResult = false;
        }
    }
}
