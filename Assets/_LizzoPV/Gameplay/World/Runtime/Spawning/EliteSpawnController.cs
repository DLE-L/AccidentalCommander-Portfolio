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

        public void Initialize(RunServices services, IGameplayRunUiFeedback uiController, RunPauseController pauseController, ArenaBounds arenaBounds)
        {
            _services = services ?? throw new System.ArgumentNullException(nameof(services));
            _uiController = uiController;
            _pauseController = pauseController ?? throw new System.ArgumentNullException(nameof(pauseController));
            _arenaBounds = arenaBounds ?? throw new System.ArgumentNullException(nameof(arenaBounds));
            enabled = true;
        }

        private const float RED_CHARGER_MIN_CAMERA_MARGIN = 1.2f;
        private const float RED_CHARGER_MAX_CAMERA_MARGIN = 2.4f;
        private const int RED_CHARGER_SPAWN_COUNT = 3;
        private const float RED_CHARGER_RESPAWN_INTERVAL_SECONDS = 60.0f;

        private float _elapsedSeconds;
        private float _nextRedChargerSpawnSeconds;
        private int _spawnedRedChargerCount;

        private void Start()
        {
            _nextRedChargerSpawnSeconds = _services.App.Data.RunTuning.RedChargerSpawnSeconds;
        }

        private void Update()
        {
            if (IsGameplayPaused())
                return;

            if (_spawnedRedChargerCount >= RED_CHARGER_SPAWN_COUNT)
                return;

            PlayerController player = _services.Registry?.Player;
            if (player == null)
                return;

            _elapsedSeconds += Time.deltaTime;
            if (_elapsedSeconds < _nextRedChargerSpawnSeconds)
                return;

            SpawnRedCharger(player);
        }

        private void SpawnRedCharger(PlayerController player)
        {
            _spawnedRedChargerCount++;
            _nextRedChargerSpawnSeconds += RED_CHARGER_RESPAWN_INTERVAL_SECONDS;

            Vector3 spawnPosition = SpawnPositionResolver.ResolveOutsideCamera(
                player.transform.position,
                RED_CHARGER_MIN_CAMERA_MARGIN,
                RED_CHARGER_MAX_CAMERA_MARGIN,
                _arenaBounds);

            P0PlaytestDiagnostics.LogEnemyAliveSnapshot("before_elite_spawn");
            MonsterController monster = _services.Spawner.SpawnEnemy(spawnPosition, Define.RED_CHARGER_ID);
            if (monster == null)
            {
                Debug.LogWarning("P0 Red Charger spawn failed.");
                return;
            }

            RedChargerBehaviour redCharger = monster.GetComponent<RedChargerBehaviour>();
            if (redCharger == null)
            {
                Debug.LogError("Red Charger prefab is missing required RedChargerBehaviour.", monster);
                return;
            }

            redCharger.Setup(monster);
            P0Telemetry.LogOnce(P0Telemetry.EliteSeen, P0Telemetry.RunTimeSecondsParameter, "elite=RedCharger");
            P0PlaytestDiagnostics.LogEnemyAliveSnapshot("after_elite_spawn");
        }

        private bool IsGameplayPaused() => _pauseController != null && _pauseController.IsPaused;
    }
}
