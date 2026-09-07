using System;
using Lizzo.PV.Flow;
using UnityEngine;

namespace Lizzo.PV.Gameplay.Route
{
    internal sealed class RunGameplayUiLifecycleCoordinator : IDisposable
    {
        readonly RunServices _services;
        readonly IGameplayRunUi _ui;
        readonly RunPauseController _pause;

        internal RunGameplayUiLifecycleCoordinator(
            RunServices services,
            IGameplayRunUi ui,
            RunPauseController pause)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            _pause = pause ?? throw new ArgumentNullException(nameof(pause));
        }

        internal bool TryActivate(Camera worldCamera, PlayerController player)
        {
            _pause.Initialize();
            _ui.ModalChanged -= _pause.SetModalOpen;
            _ui.ModalChanged += _pause.SetModalOpen;
            if (!_ui.Initialize(_services, worldCamera, _pause))
            {
                Debug.LogError("[GameScene] Gameplay UI controller initialization failed.");
                return false;
            }

            _pause.PauseOverlayChanged -= _ui.SetPauseOverlay;
            _pause.PauseOverlayChanged += _ui.SetPauseOverlay;
            _pause.GameplaySpeedChanged -= _ui.SetGameplaySpeed;
            _pause.GameplaySpeedChanged += _ui.SetGameplaySpeed;
            _ui.SetGameplaySpeed(_pause.SelectedGameplaySpeed);
            _ui.SetPauseOverlay(_pause.IsPaused, false);
            _ui.SetRunStatus(0, 0.0f);
            _ui.SetExperienceStatus(
                _services.State.Level,
                _services.State.Experience,
                _services.State.RequiredExperience);
            _ui.BindPlayer(player);
            _ui.ShowGameplay();
            return true;
        }

        public void Dispose()
        {
            _ui.ModalChanged -= _pause.SetModalOpen;
            _pause.PauseOverlayChanged -= _ui.SetPauseOverlay;
            _pause.GameplaySpeedChanged -= _ui.SetGameplaySpeed;
        }
    }
}
