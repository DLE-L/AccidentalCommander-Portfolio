using System.Text;
using Lizzo.PV.Flow;
using Lizzo.PV.Gameplay.Telemetry;
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
                LogPausePassiveSnapshot();
                _pauseController.gameObject.SetActive(true);
                if (!_pauseController.Present(
                        fromAppBackground,
                        _companionPausePresentations,
                        _passivePausePresentations,
                        _pauseSynergyPresentations))
                {
                    Debug.LogError("[GameplayRunUiController] Clean pause presentation failed.", this);
                }
                PauseOpened?.Invoke();
            }
            else
            {
                _pauseController.Hide();
                _pauseController.gameObject.SetActive(false);
                PauseClosed?.Invoke();
            }

            UpdateBossWarningSuspension();
            UpdateInputGate();
        }

        private void RefreshPausePresentation()
        {
            PauseBuildSummaryPresentationResolver.Fill(
                _services.Party.GetSquadSlotSnapshot(),
                _services.PassiveRoster,
                _services.App.Data,
                _services.ProductionSynergies?.CurrentSnapshot ?? default,
                _companionPausePresentations,
                _passivePausePresentations,
                _pauseSynergyPresentations,
                MaxCompanionPauseEntries,
                MaxPassivePauseEntries,
                this);
        }

        private void LogPausePassiveSnapshot()
        {
            var roster = _services.PassiveRoster;
            var snapshot = roster?.Snapshot;
            var slots = new StringBuilder(128);
            int occupiedCount = 0;
            if (snapshot != null)
            {
                for (int i = 0; i < snapshot.Count; i++)
                {
                    var slot = snapshot[i];
                    if (slot == null || slot.IsEmpty)
                        continue;

                    if (slots.Length > 0)
                        slots.Append(';');
                    slots.Append(slot.SlotId)
                        .Append(':')
                        .Append(slot.PassiveId)
                        .Append(':')
                        .Append(slot.Level);
                    occupiedCount++;
                }
            }

            RunTelemetry.Log(
                RunTelemetry.PassiveSlotStateUpdate,
                "reason=pause_open",
                $"active_count={roster?.ActiveSlotCount ?? 0}",
                $"occupied_count={occupiedCount}",
                $"presented_count={_passivePausePresentations.Count}",
                $"slots={(slots.Length == 0 ? "none" : slots.ToString())}");
        }

        private void HandleSpeedToggleRequested()
        {
            _runPauseController.ToggleGameplaySpeed();
        }

        private void HandlePauseAbandonRequested()
        {
            _services.State.TryAbandon();
        }

    }
}
