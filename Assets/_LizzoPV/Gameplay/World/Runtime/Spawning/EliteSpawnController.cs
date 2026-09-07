using Lizzo.PV.Flow;
using Lizzo.PV.Data;
using Lizzo.PV.Gameplay.Spawning;
using Lizzo.PV.Gameplay.World;
using Lizzo.PV.Gameplay.Route;
using Lizzo.PV.Legion;
using Lizzo.PV.Gameplay.Telemetry;

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
            _elapsedSeconds = 0.0f;
            _spawnedEliteCount = 0;
            enabled = TutorialEncounterRules.AllowsTimedEliteSpawns(_services.Context);
        }

        private const float TIMED_ELITE_MIN_CAMERA_MARGIN = 1.2f;
        private const float TIMED_ELITE_MAX_CAMERA_MARGIN = 2.4f;
        private const int TIMED_ELITE_SPAWN_COUNT = 3;
        private const float TIMED_ELITE_RESPAWN_INTERVAL_SECONDS = 60.0f;

        private float _elapsedSeconds;
        private float _nextEliteSpawnSeconds;
        private int _spawnedEliteCount;

        private void Start()
        {
            _nextEliteSpawnSeconds = _services.App.Data.RunTuning.TimedEliteSpawnSeconds;
        }

        private void Update()
        {
            if (IsGameplayPaused())
                return;

            if (_spawnedEliteCount >= TIMED_ELITE_SPAWN_COUNT)
                return;

            PlayerController player = _services.Registry?.Player;
            if (player == null)
                return;

            _elapsedSeconds += Time.deltaTime;
            if (_elapsedSeconds < _nextEliteSpawnSeconds)
                return;

            SpawnTimedElite(player);
        }

        private void SpawnTimedElite(PlayerController player)
        {
            _spawnedEliteCount++;
            _nextEliteSpawnSeconds += TIMED_ELITE_RESPAWN_INTERVAL_SECONDS;

            Vector3 spawnPosition = SpawnPositionResolver.ResolveOutsideCamera(
                player.transform.position,
                TIMED_ELITE_MIN_CAMERA_MARGIN,
                TIMED_ELITE_MAX_CAMERA_MARGIN,
                _arenaBounds);

            RunDiagnostics.LogEnemyAliveSnapshot("before_elite_spawn");
            EnemyEncounterDefinition definition = _services.App.Data.RunTuning.TimedElite;
            MonsterController monster = _services.Spawner.SpawnEnemy(
                spawnPosition,
                definition.EnemyTemplateId,
                definition.EncounterRank,
                definition.ScaleMultiplier);
            if (monster == null)
            {
                Debug.LogWarning($"Timed elite spawn failed. template_id={definition.EnemyTemplateId}");
                return;
            }

            IRunFinalThreatBehaviour encounterBehaviour = monster.GetComponent<IRunFinalThreatBehaviour>();
            encounterBehaviour?.Setup(monster);
            monster.ConfigureEncounterRank(definition.EncounterRank, definition.ScaleMultiplier);
            _uiController?.ShowThreatDirection(
                monster.transform,
                "엘리트 등장",
                new Color(1.0f, 0.2f, 0.08f, 1.0f));
            RunTelemetry.LogOnce(RunTelemetry.EliteSeen, RunTelemetry.RunTimeSecondsParameter, $"enemy={monster.EnemyId}");
            RunDiagnostics.LogEnemyAliveSnapshot("after_elite_spawn");
        }

        private bool IsGameplayPaused() => _pauseController != null && _pauseController.IsPaused;
    }
}
