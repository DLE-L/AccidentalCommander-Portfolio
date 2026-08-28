using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Lizzo.PV.P0.Combat;
using Lizzo.PV.P0.Config;
using Lizzo.PV.Data;
using Lizzo.PV.Gameplay.Spawning;
using Lizzo.PV.Gameplay.World;
using Lizzo.PV.P0.Telemetry;
using Lizzo.PV.Flow;

using UnityEngine;

namespace Lizzo.PV.P0.Units
{
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
        private bool _tutorialFirstGroupSpawned;
        private float _tutorialSpawnAccumulator;
        private int _tutorialSpawnSequence;
        private int[] _tutorialEdgeCycle = Array.Empty<int>();
        private int _tutorialEdgeCycleIndex;

        public bool Stopped { get; set; }

        private void Start()
        {
            RunSpawnLoopAsync(this.GetCancellationTokenOnDestroy()).Forget();
        }

        private async UniTask RunSpawnLoopAsync(CancellationToken cancellationToken)
        {
            if (_services.Context.IsTutorial)
            {
                await RunTutorialSpawnLoopAsync(cancellationToken);
                return;
            }

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
                spawnBudget *= _services.RunTraitEffects.GetNormalSpawnDensityMultiplier();
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

        private async UniTask RunTutorialSpawnLoopAsync(CancellationToken cancellationToken)
        {
            const float tickSeconds = 0.25f;
            while (cancellationToken.IsCancellationRequested == false)
            {
                if (!IsGameplayPaused())
                    TickTutorialSpawns(tickSeconds);

                try
                {
                    await UniTask.Delay(
                        TimeSpan.FromSeconds(tickSeconds),
                        cancellationToken: cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private void TickTutorialSpawns(float tickSeconds)
        {
            if (Stopped || _services.Registry?.Player == null)
                return;

            float elapsedSeconds = _services.State.ElapsedSeconds;
            if (!_tutorialFirstGroupSpawned)
            {
                if (elapsedSeconds < 7.0f || _services.Party.ActiveCompanionSlotCount <= 0)
                    return;

                _tutorialFirstGroupSpawned = true;
                if (_services.Party.ActiveCompanionCount > 1)
                    return;
                for (int index = 0; index < 9; index++)
                    TrySpawnTutorialEnemy(elapsedSeconds, forceTopEdge: true);
                return;
            }

            float rate = TutorialCombatBaseline.ResolveSpawnRate(elapsedSeconds);
            _tutorialSpawnAccumulator += rate * tickSeconds;
            int count = Mathf.FloorToInt(_tutorialSpawnAccumulator);
            _tutorialSpawnAccumulator -= count;
            for (int index = 0; index < count; index++)
                TrySpawnTutorialEnemy(elapsedSeconds, forceTopEdge: false);
        }

        private void TrySpawnTutorialEnemy(float elapsedSeconds, bool forceTopEdge)
        {
            if (_services.Registry.Enemies.Count >= RemoteConfig.MaxEnemyStage1)
                return;

            PlayerController player = _services.Registry.Player;
            Camera camera = Camera.main;
            if (player == null || camera == null || !camera.orthographic)
                return;

            int edge = forceTopEdge ? 0 : ResolveTutorialEdge(elapsedSeconds);
            float tangentLimit = Mathf.Max(
                0.0f,
                (forceTopEdge ? 2.5f : TutorialCombatBaseline.ArenaSize * 0.5f - 1.0f));
            float tangentOffset = UnityEngine.Random.Range(-tangentLimit, tangentLimit);
            Vector3 spawnPosition = forceTopEdge
                ? _arenaBounds.ResolveTutorialEdgeSpawn(
                    player.transform.position,
                    camera.orthographicSize,
                    camera.aspect,
                    edge,
                    0.5f,
                    tangentOffset)
                : _arenaBounds.ResolveOuterEdgeSpawn(edge, tangentOffset);
            _tutorialSpawnSequence++;
            int templateId = ResolveTutorialEnemyTemplate(elapsedSeconds, _tutorialSpawnSequence);
            _services.Spawner.SpawnEnemy(spawnPosition, templateId);
        }

        private int ResolveTutorialEdge(float elapsedSeconds)
        {
            int activeEdgeCount = TutorialCombatBaseline.ResolveActiveSpawnEdgeCount(elapsedSeconds);
            if (_tutorialEdgeCycle.Length != activeEdgeCount || _tutorialEdgeCycleIndex >= _tutorialEdgeCycle.Length)
            {
                _tutorialEdgeCycle = new int[Mathf.Max(1, activeEdgeCount)];
                for (int index = 0; index < _tutorialEdgeCycle.Length; index++)
                    _tutorialEdgeCycle[index] = index;
                for (int index = _tutorialEdgeCycle.Length - 1; index > 0; index--)
                {
                    int swapIndex = UnityEngine.Random.Range(0, index + 1);
                    (_tutorialEdgeCycle[index], _tutorialEdgeCycle[swapIndex]) =
                        (_tutorialEdgeCycle[swapIndex], _tutorialEdgeCycle[index]);
                }
                _tutorialEdgeCycleIndex = 0;
            }

            return _tutorialEdgeCycle[_tutorialEdgeCycleIndex++];
        }

        private static int ResolveTutorialEnemyTemplate(float elapsedSeconds, int sequence)
        {
            if (elapsedSeconds < 60.0f || elapsedSeconds >= TutorialRunTimeline.ShowcaseStartSeconds)
                return Define.GOBLIN_ID;
            if (elapsedSeconds < 90.0f)
                return sequence % 8 == 0 ? Define.ORC_ID : Define.GOBLIN_ID;
            return sequence % 5 == 0 ? Define.ORC_ID : Define.GOBLIN_ID;
        }

        private void TrySpawn()
        {
            if (Stopped)
                return;

            if (_services.Registry == null || _services.Registry.Enemies.Count >= RemoteConfig.MaxEnemyStage1)
                return;

            PlayerController player = _services.Registry.Player;
            if (player == null)
                return;

            Vector3 spawnPosition = SpawnPositionResolver.ResolveOutsideCamera(
                player.transform.position,
                NORMAL_SPAWN_MIN_CAMERA_MARGIN,
                NORMAL_SPAWN_MAX_CAMERA_MARGIN,
                _arenaBounds);
            _services.Spawner.SpawnEnemy(spawnPosition, PickStage1EnemyTemplateId());
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
            P0PlaytestDiagnostics.LogEnemyAliveSnapshot("before_stage_ring_surge");

            int currentCount = _services.Registry.Enemies.Count;
            int spawnCount = Mathf.Min(RING_SURGE_COUNT, Mathf.Max(0, RemoteConfig.MaxEnemyStage1 - currentCount));
            for (int i = 0; i < spawnCount; i++)
            {
                float angle = i * Mathf.PI * 2.0f / spawnCount;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector3 spawnPosition = SpawnPositionResolver.ResolveOutsideCamera(
                    player.transform.position,
                    direction,
                    RING_SURGE_CAMERA_MARGIN,
                    _arenaBounds);
                _services.Spawner.SpawnEnemy(spawnPosition, PickStage1EnemyTemplateId());
            }

            P0PlaytestDiagnostics.LogEnemyAliveSnapshot("after_stage_ring_surge");
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
                return 0.0f;

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
