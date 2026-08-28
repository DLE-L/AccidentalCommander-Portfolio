using System;
using Lizzo.PV.Combat;
using Lizzo.PV.Flow;
using Lizzo.PV.Gameplay.Route;
using Lizzo.PV.Legion;
using Lizzo.PV.P0.Config;
using Lizzo.PV.P0.Telemetry;
using UnityEngine;

namespace Lizzo.PV.UI
{
    internal sealed class RunResultFlowCoordinator
    {
        private readonly RunServices _services;
        private readonly IGameplayRunUi _ui;
        private readonly RunPauseController _pause;
        private readonly Action _restartRequested;
        private readonly Action _lobbyRequested;
        private readonly UnityEngine.Object _context;
        private bool _failureResultOpen;

        internal RunResultFlowCoordinator(
            RunServices services,
            IGameplayRunUi ui,
            RunPauseController pause,
            Action restartRequested,
            Action lobbyRequested,
            UnityEngine.Object context)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            _pause = pause ?? throw new ArgumentNullException(nameof(pause));
            _restartRequested = restartRequested ?? throw new ArgumentNullException(nameof(restartRequested));
            _lobbyRequested = lobbyRequested ?? throw new ArgumentNullException(nameof(lobbyRequested));
            _context = context;
        }

        internal void HandleRunEnded(RunResult result)
        {
            _services.RunTraitOffers?.ExpirePendingOpportunities();
            if (result.Outcome == RunOutcome.Clear && _services.Registry.Player != null)
                RetroVfx.Spawn(RetroVfxKind.ResultClear, _services.Registry.Player.transform.position, Vector3.zero, 1.0f);

            _pause.MarkRunEnded();
            _failureResultOpen = result.Outcome == RunOutcome.Failure;
            string resultName = result.Outcome == RunOutcome.Clear ? "clear" : "failure";
            DamageContributionSnapshot contributionSnapshot = _services.DamageContributions?.CaptureSnapshot(_services.Synergies);
            DamageContributionSummaryTelemetry.Emit(
                resultName,
                contributionSnapshot,
                (eventName, payload) => P0Telemetry.Log(eventName, payload));
            _services.SessionOutput.ReportResult(result);

            if (result.Outcome == RunOutcome.Failure && _services.Context.IsTutorial)
            {
                _failureResultOpen = false;
                P0Telemetry.EndRun(resultName, result.BossHpPercent);
                _restartRequested();
                return;
            }

            RunResultViewData view = RunResultViewDataResolver.Resolve(result, _services, contributionSnapshot, _context);

            try
            {
                Action primaryRequested = result.Outcome == RunOutcome.Clear
                    ? _lobbyRequested
                    : _restartRequested;
                Action optionalRequested = result.Outcome == RunOutcome.Failure
                    && RemoteConfig.ReviveAdEnabled
                    && _services.State.CanRevive
                    ? TryReviveRun
                    : null;
                if (!_ui.ShowResult(view, primaryRequested, optionalRequested, _lobbyRequested))
                {
                    Debug.LogError("[GameScene] Result popup could not present the run result.", _context);
                    return;
                }

                P0Telemetry.Log(
                    P0Telemetry.ResultView,
                    P0Telemetry.RunTimeSecondsParameter,
                    $"result={resultName}",
                    $"duration_seconds={Mathf.Max(0, Mathf.RoundToInt(result.ElapsedSeconds))}",
                    $"kill_count={result.KillCount}",
                    $"boss_hp_percent={result.BossHpPercent}");
            }
            catch (InvalidOperationException exception)
            {
                Debug.LogException(exception, _context);
            }
            finally
            {
                P0Telemetry.EndRun(resultName, result.BossHpPercent);
            }
        }

        private void TryReviveRun()
        {
            if (!_failureResultOpen)
                return;

            PlayerController player = _services.Registry.Player;
            if (player == null || player.RestoreFullHealth() == false)
            {
                Debug.LogError("[GameScene] Commander health could not be restored for revive.", _context);
                return;
            }

            if (_services.State.TryResumeAfterRevive() == false)
            {
                Debug.LogError("[GameScene] Run state could not resume after revive.", _context);
                return;
            }

            if (_pause.ResumeAfterRevive() == false)
            {
                _services.State.MarkStopped();
                Debug.LogError("[GameScene] Run pause state could not resume after revive.", _context);
                return;
            }

            _failureResultOpen = false;
            _ui.CloseModal();
        }
    }
}
