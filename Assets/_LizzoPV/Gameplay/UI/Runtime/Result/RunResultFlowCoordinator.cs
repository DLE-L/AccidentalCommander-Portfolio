using System;
using Lizzo.PV.Flow;
using Lizzo.PV.Gameplay.Route;
using Lizzo.PV.Legion;
using Lizzo.PV.Gameplay.Telemetry;
using UnityEngine;

namespace Lizzo.PV.UI
{
    internal sealed class RunResultFlowCoordinator
    {
        private readonly RunServices _services;
        private readonly IGameplayRunUi _ui;
        private readonly Action _lobbyRequested;
        private readonly Action _clearNotifications;
        private readonly UnityEngine.Object _context;

        internal RunResultFlowCoordinator(
            RunServices services,
            IGameplayRunUi ui,
            Action lobbyRequested,
            UnityEngine.Object context,
            Action clearNotifications = null)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            _lobbyRequested = lobbyRequested ?? throw new ArgumentNullException(nameof(lobbyRequested));
            _clearNotifications = clearNotifications;
            _context = context;
        }

        internal void HandleRunEnded(RunResult result)
        {
            _clearNotifications?.Invoke();
            if (result.Outcome == RunOutcome.Clear && _services.Registry.Player != null)
                RetroVfx.Spawn(RetroVfxKind.ResultClear, _services.Registry.Player.transform.position, Vector3.zero, 1.0f);

            string resultName = result.Outcome switch
            {
                RunOutcome.Clear => "clear",
                RunOutcome.Failure => "failure",
                RunOutcome.Abandoned => "abandoned",
                _ => throw new ArgumentOutOfRangeException(nameof(result), result.Outcome, null),
            };
            _services.CombatTelemetry?.LogSummary(resultName);
            if (result.Outcome == RunOutcome.Clear && _services.Context.IsTutorial)
            {
                FirstRunProgress.TryCommitTutorialClear();
                TutorialCheckpointProgress.Reset();
            }

            string settlementIssue = "Run reward service is not configured.";
            RunRewardSettlement settlement = null;
            if (_services.ResultRewards == null
                || !_services.ResultRewards.TrySettle(
                    result,
                    _services.Context.StageId,
                    out settlement,
                    out settlementIssue))
            {
                Debug.LogError($"[GameScene] Run rewards could not be settled: {settlementIssue}", _context);
                RunTelemetry.EndRun(resultName, result.BossHpPercent);
                return;
            }

            RunResultViewData view = RunResultViewDataResolver.Resolve(result, _services, settlement);

            try
            {
                if (!_ui.ShowResult(view, _lobbyRequested))
                {
                    Debug.LogError("[GameScene] Result popup could not present the run result.", _context);
                    return;
                }

                RunTelemetry.Log(
                    RunTelemetry.ResultView,
                    RunTelemetry.RunTimeSecondsParameter,
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
                RunTelemetry.EndRun(resultName, result.BossHpPercent);
            }
        }

    }
}
