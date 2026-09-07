using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Lizzo.PV.Gameplay.Combat;
using Lizzo.PV.Data;
using Lizzo.PV.Gameplay.Spawning;
using Lizzo.PV.Gameplay.World;
using Lizzo.PV.Gameplay.Telemetry;using Lizzo.PV.Flow;

using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Lizzo.PV.Gameplay.Units
{
    [MovedFrom(true, "Lizzo.PV.P0.Units")]
    public sealed class StageSpawner : MonoBehaviour
    {
        RunServices _services;
        RunPauseController _pauseController;
        ArenaBounds _arenaBounds;

        public void Initialize(RunServices services, RunPauseController pauseController, ArenaBounds arenaBounds)
        {
            _services = services ?? throw new System.ArgumentNullException(nameof(services));
            _pauseController = pauseController ?? throw new System.ArgumentNullException(nameof(pauseController));
            _arenaBounds = arenaBounds ?? throw new System.ArgumentNullException(nameof(arenaBounds));
            enabled = true;
        }

        private const float NORMAL_SPAWN_MIN_CAMERA_MARGIN = 0.8f;
        private const float NORMAL_SPAWN_MAX_CAMERA_MARGIN = 1.8f;
        private const float RING_SURGE_SECONDS = 60.0f;
        private const float RING_SURGE_CAMERA_MARGIN = 0.9f;
        private const int RING_SURGE_COUNT = 24;
        private const float ORC_SPAWN_CHANCE = 0.15f;
        private const float WOLF_SPAWN_CHANCE = 0.45f;
        private const float BOSS_PRELUDE_SLOWDOWN_SECONDS = 10.0f;
        private const float BOSS_PRELUDE_READY_SECONDS = 5.0f;
        private const float BOSS_PRELUDE_MIN_SPAWN_MULTIPLIER = 0.15f;
        private const float BOSS_PRELUDE_MAX_SPAWN_MULTIPLIER = 0.65f;

        private float _elapsedSeconds;
        private bool _hasSpawnedRingSurge;

        public bool Stopped { get; set; }

        private void Start()
        {
            RunSpawnLoopAsync(this.GetCancellationTokenOnDestroy()).Forget();
        }

        private async UniTask RunSpawnLoopAsync(CancellationToken cancellationToken)
        {
            while (cancellationToken.IsCancellationRequested == false)
            {
                if (IsGameplayPaused())
                {
                    try
                    {
                        await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }

                    continue;
                }

                TrySpawn();
                TrySpawnRingSurge();

                float spawnBudget = _services.App.Data.GetStage1SpawnBudget(_elapsedSeconds);
                spawnBudget *= ResolveBossPreludeSpawnMultiplier();
                float spawnInterval = 1.0f / Mathf.Max(0.1f, spawnBudget);
                _elapsedSeconds += spawnInterval;

                try
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(spawnInterval), cancellationToken: cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private void TrySpawn()
        {
            if (Stopped)
                return;

            if (_services.Registry == null || _services.Registry.Enemies.Count >= _services.Tuning.MaxEnemyStage1)
                return;

            PlayerController player = _services.Registry.Player;
            if (player == null)
                return;

            Vector3 spawnPosition = SpawnPositionResolver.ResolveOutsideCamera(
                player.transform.position,
                NORMAL_SPAWN_MIN_CAMERA_MARGIN,
                NORMAL_SPAWN_MAX_CAMERA_MARGIN,
                _arenaBounds);
            _services.Spawner.SpawnEnemy(
                spawnPosition,
                PickStage1EnemyTemplateId(),
                EnemyEncounterRank.Normal,
                1.0f);
        }

        private void TrySpawnRingSurge()
        {
            if (_hasSpawnedRingSurge || Stopped)
                return;

            float scaledSurgeSeconds = RING_SURGE_SECONDS * _services.App.Data.RunTuning.TimelineScale;
            if (_elapsedSeconds < scaledSurgeSeconds)
                return;

            PlayerController player = _services.Registry?.Player;
            if (player == null)
                return;

            _hasSpawnedRingSurge = true;
            RunDiagnostics.LogEnemyAliveSnapshot("before_stage_ring_surge");

            int currentCount = _services.Registry.Enemies.Count;
            int spawnCount = Mathf.Min(RING_SURGE_COUNT, Mathf.Max(0, _services.Tuning.MaxEnemyStage1 - currentCount));
            for (int i = 0; i < spawnCount; i++)
            {
                float angle = i * Mathf.PI * 2.0f / spawnCount;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector3 spawnPosition = SpawnPositionResolver.ResolveOutsideCamera(
                    player.transform.position,
                    direction,
                    RING_SURGE_CAMERA_MARGIN,
                    _arenaBounds);
                _services.Spawner.SpawnEnemy(
                    spawnPosition,
                    PickStage1EnemyTemplateId(),
                    EnemyEncounterRank.Normal,
                    1.0f);
            }

            RunDiagnostics.LogEnemyAliveSnapshot("after_stage_ring_surge");
        }

        private int PickStage1EnemyTemplateId()
        {
            EnemyData wolf = _services.App.Data.GetEnemy(CombatIds.HungryWolf);
            EnemyData orc = _services.App.Data.GetEnemy(CombatIds.ShieldOrc);

            bool canSpawnWolf = wolf != null && _elapsedSeconds >= _services.App.Data.GetEffectiveSpawnSeconds(wolf);
            bool canSpawnOrc = orc != null && _elapsedSeconds >= _services.App.Data.GetEffectiveSpawnSeconds(orc);

            if (canSpawnOrc && UnityEngine.Random.value < ORC_SPAWN_CHANCE)
                return Define.ORC_ID;

            if (canSpawnWolf && UnityEngine.Random.value < WOLF_SPAWN_CHANCE)
                return Define.SNAKE_ID;

            return Define.GOBLIN_ID;
        }

        private float ResolveBossPreludeSpawnMultiplier()
        {
            float remainingSeconds = _services.App.Data.RunTuning.BossSpawnSeconds - _elapsedSeconds;
            if (remainingSeconds <= 0.0f)
                return 1.0f;

            if (remainingSeconds <= BOSS_PRELUDE_READY_SECONDS)
                return BOSS_PRELUDE_MIN_SPAWN_MULTIPLIER;

            if (remainingSeconds <= BOSS_PRELUDE_SLOWDOWN_SECONDS)
            {
                float ratio = Mathf.InverseLerp(BOSS_PRELUDE_READY_SECONDS, BOSS_PRELUDE_SLOWDOWN_SECONDS, remainingSeconds);
                return Mathf.Lerp(BOSS_PRELUDE_MIN_SPAWN_MULTIPLIER, BOSS_PRELUDE_MAX_SPAWN_MULTIPLIER, ratio);
            }

            return 1.0f;
        }

        private bool IsGameplayPaused() => _pauseController != null && _pauseController.IsPaused;
    }
}
