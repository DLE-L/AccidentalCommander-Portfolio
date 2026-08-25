using Lizzo.PV.Flow;
using Lizzo.PV.Gameplay.Pause;
using Lizzo.PV.UI;
using UnityEngine;

namespace Lizzo.PV.Gameplay.Route
{
    public sealed partial class GameplayRunUiController
    {
        public void SetPauseOverlay(bool visible, bool fromAppBackground)
        {
            EnsureInitialized();
            _pauseOverlayVisible = visible;
            if (visible)
            {
                RefreshPausePresentation();
                _pauseController.gameObject.SetActive(true);
                if (!_pauseController.Present(
                        fromAppBackground,
                        _companionPausePresentations,
                        _passivePausePresentations,
                        _pauseSynergyPresentations))
                {
                    Debug.LogError("[GameplayRunUiController] Clean pause presentation failed.", this);
                }
            }
            else
            {
                _pauseController.Hide();
                _pauseController.gameObject.SetActive(false);
            }

            UpdateInputGate();
        }

        private void RefreshPausePresentation()
        {
            PauseBuildSummaryPresentationResolver.Fill(
                _services.Party.GetSquadSlotSnapshot(),
                _services.PassiveRoster,
                _services.Synergies,
                _services.App.Data,
                _companionPausePresentations,
                _passivePausePresentations,
                _pauseSynergyPresentations,
                MaxCompanionPauseEntries,
                MaxPassivePauseEntries,
                this);
        }

        private void HandleSpeedToggleRequested()
        {
            _runPauseController.ToggleGameplaySpeed();
        }

        private void HandlePauseLobbyRequested()
        {
            _services.State.TryAbandon();
        }

    }
}
