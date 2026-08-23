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

            _services.SynergyTriggers.Tick(deltaTime, _services.State.IsLoaded, isPaused, frameCount);
            _services.Build1SynergyProgression.Tick(deltaTime, _services.State.IsLoaded, isPaused);
            _services.MixedCommand.TryResolvePending(time);
            _services.MixedCommand.Tick(time);
            _services.HealingBond.TryResolvePending(time);
            _services.HealingBond.Tick(time);
            _services.UndeadSummon.TryResolvePending(time, frameCount);
            _services.UndeadSummon.Tick(time, deltaTime);
            _services.GuardShockwave.TryResolvePending(time);
            _services.ArcherRain.Tick(time);
            _services.MagicChain.TryResolvePending();
            _services.ExplosionChain.TryResolvePending();
            _services.BeastHunt.TryResolvePending(time);
            _services.BeastHunt.Tick(time);
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
