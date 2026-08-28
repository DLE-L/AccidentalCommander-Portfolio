using Lizzo.PV.Flow;
using Lizzo.PV.Data;
using Lizzo.PV.Gameplay.Spawning;
using Lizzo.PV.Gameplay.World;
using Lizzo.PV.Gameplay.Route;
using Lizzo.PV.Legion;
using Lizzo.PV.P0.Telemetry;

using UnityEngine;

namespace Lizzo.PV.P0.Units
{
    public sealed class EliteSpawnController : MonoBehaviour
    {
        RunServices _services;
        IGameplayRunUiFeedback _uiController;
        RunPauseController _pauseController;
        ArenaBounds _arenaBounds;
        RunEliteSpawnSchedule _schedule;

        public void Initialize(RunServices services, IGameplayRunUiFeedback uiController, RunPauseController pauseController, ArenaBounds arenaBounds)
        {
            _services = services ?? throw new System.ArgumentNullException(nameof(services));
            _uiController = uiController;
            _pauseController = pauseController ?? throw new System.ArgumentNullException(nameof(pauseController));
            _arenaBounds = arenaBounds ?? throw new System.ArgumentNullException(nameof(arenaBounds));
            _schedule = _services.Definition.EliteSpawnSchedule;
            enabled = _schedule != null && _schedule.SpawnCount > 0;
        }

        private float _elapsedSeconds;
        private float _nextEliteSpawnSeconds;
        private int _spawnedEliteCount;

        private void Start()
        {
            if (_schedule != null)
                _nextEliteSpawnSeconds = _schedule.FirstSpawnSeconds;
        }

        private void Update()
        {
            if (IsGameplayPaused())
                return;

            if (_schedule == null || _spawnedEliteCount >= _schedule.SpawnCount)
                return;

            PlayerController player = _services.Registry?.Player;
            if (player == null)
                return;

            _elapsedSeconds = _services.State.ElapsedSeconds;
            if (_elapsedSeconds < _nextEliteSpawnSeconds)
                return;

            SpawnElite(player);
        }

        private void SpawnElite(PlayerController player)
        {
            _spawnedEliteCount++;
            _nextEliteSpawnSeconds += _schedule.RespawnIntervalSeconds;

            Vector3 spawnPosition = SpawnPositionResolver.ResolveOutsideCamera(
                player.transform.position,
                _schedule.MinimumCameraMargin,
                _schedule.MaximumCameraMargin,
                _arenaBounds);

            P0PlaytestDiagnostics.LogEnemyAliveSnapshot("before_elite_spawn");
            MonsterController monster = _services.Spawner.SpawnEnemy(spawnPosition, _schedule.TemplateId);
            if (monster == null)
            {
                Debug.LogWarning($"[EliteSpawnController] Elite spawn failed: {_schedule.ContentId}.");
                return;
            }

            IRunEliteRuntime eliteRuntime = monster.GetComponent<IRunEliteRuntime>();
            if (eliteRuntime == null)
            {
                Debug.LogError(
                    $"[EliteSpawnController] Elite prefab '{_schedule.ContentId}' is missing required IRunEliteRuntime.",
                    monster);
                Destroy(monster.gameObject);
                return;
            }

            eliteRuntime.Setup(monster);
            _uiController?.ShowThreatDirection(
                monster.transform,
                _schedule.DisplayName + " 등장",
                new Color(1.0f, 0.2f, 0.08f, 1.0f));
            P0Telemetry.LogOnce(
                P0Telemetry.EliteSeen,
                P0Telemetry.RunTimeSecondsParameter,
                $"elite={_schedule.ContentId}");
            P0PlaytestDiagnostics.LogEnemyAliveSnapshot("after_elite_spawn");
        }

        private bool IsGameplayPaused() => _pauseController != null && _pauseController.IsPaused;
    }
}
