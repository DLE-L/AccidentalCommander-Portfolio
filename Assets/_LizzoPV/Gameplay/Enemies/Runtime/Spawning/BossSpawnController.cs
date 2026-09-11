using Lizzo.PV.Combat;
using Lizzo.PV.Gameplay.Units;
using System;
using Lizzo.PV.Flow;
using Lizzo.PV.Data;
using Lizzo.PV.Gameplay.Diagnostics;
using Lizzo.PV.Gameplay.Spawning;
using Lizzo.PV.Legion;
using Lizzo.PV.Gameplay.Combat;
using Lizzo.PV.Gameplay.Telemetry;
using Lizzo.PV.Gameplay.Visuals;
using Lizzo.PV.UI;
using Lizzo.PV.Gameplay.Route;
using Lizzo.PV.Gameplay.World;

using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Lizzo.PV.Gameplay.Units
{
    [MovedFrom(true, "Lizzo.PV.P0.Units")]
    public sealed class BossSpawnController : MonoBehaviour
    {
        RunServices _services;
        IGameplayRunUiFeedback _uiController;
        RunPauseController _pauseController;
        ArenaBounds _arenaBounds;
        Action _bossPhaseStarted;

        public void Initialize(
            RunServices services,
            IGameplayRunUiFeedback uiController,
            RunPauseController pauseController,
            ArenaBounds arenaBounds,
            Action bossPhaseStarted)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _uiController = uiController;
            _pauseController = pauseController ?? throw new ArgumentNullException(nameof(pauseController));
            _arenaBounds = arenaBounds ?? throw new ArgumentNullException(nameof(arenaBounds));
            _bossPhaseStarted = bossPhaseStarted ?? throw new ArgumentNullException(nameof(bossPhaseStarted));
            _elapsedSeconds = 0.0f;
            _hasSpawnedFinalThreat = false;
            _footstepWarningShown = false;
            _edgeWarningShown = false;
            _tutorialClearRequested = false;
            _activeFinalThreat = null;
            _activeBossHudLabel = string.Empty;
            enabled = true;
        }

        private const float BOSS_FOOTSTEP_WARNING_SECONDS = 15.0f;
        private const float BOSS_EDGE_WARNING_SECONDS = 5.0f;
        private const float BOSS_SPAWN_HIT_STOP_SECONDS = 1.2f;
        private const float BOSS_INTRO_CAMERA_SECONDS = 1.2f;
        private const float BOSS_DIRECTION_PREVIEW_DISTANCE = 40.0f;
        private const float TUTORIAL_FINAL_THREAT_MIN_CAMERA_MARGIN = 1.2f;
        private const float TUTORIAL_FINAL_THREAT_MAX_CAMERA_MARGIN = 2.4f;

        float BossSpawnSeconds => BossSpawnReadiness.ResolveTargetSeconds(
            _services.Context,
            _services.App.Data.RunTuning.BossSpawnSeconds);

        private float _elapsedSeconds;
        private bool _hasSpawnedFinalThreat;
        private bool _footstepWarningShown;
        private bool _edgeWarningShown;
        private bool _tutorialClearRequested;
        private EnemyActor _activeFinalThreat;
        private string _activeBossHudLabel = string.Empty;
        [SerializeField] private Transform _authoredBossDirectionPreviewTarget;

private Transform _bossDirectionPreviewTarget;

        [ContextMenu("Debug/Jump To Boss Prelude")]
        public void DebugJumpToBossPrelude()
        {
            if (_services == null)
            {
                Debug.LogError("[BossSpawnController] Boss prelude requires initialized run services.", this);
                return;
            }

            _elapsedSeconds = BossSpawnSeconds - BOSS_FOOTSTEP_WARNING_SECONDS;
            _footstepWarningShown = false;
            _edgeWarningShown = false;
            DestroyBossDirectionPreview();
        }

        private void Update()
        {
            if (IsGameplayPaused())
                return;

            if (_hasSpawnedFinalThreat)
            {
                RunBossDpsTracker.Tick();
                if (TutorialEncounterRules.UsesTutorialFinalThreat(_services.Context))
                    TryCompleteTutorialAfterFinalThreatDefeat();
                return;
            }

            CommanderActor player = _services.Registry?.Player;
            if (player == null)
                return;

            _elapsedSeconds += Time.deltaTime;
            float bossSpawnSeconds = BossSpawnSeconds;
            if (TutorialEncounterRules.UsesTutorialFinalThreat(_services.Context) == false)
            {
                float remainingSeconds = bossSpawnSeconds - _elapsedSeconds;
                UpdateBossDirectionPreview(player);
                TryShowBossPreSpawnSignals(player, remainingSeconds);
            }

            if (BossSpawnReadiness.CanSpawn(
                    _services.Context,
                    _elapsedSeconds,
                    _services.App.Data.RunTuning.BossSpawnSeconds,
                    _services.Party.ActiveCompanionSlotCount,
                    _services.Party.ActiveCompanionCount) == false)
                return;

            if (TutorialEncounterRules.UsesTutorialFinalThreat(_services.Context))
                SpawnTutorialFinalThreat(player);
            else
                SpawnConfiguredBoss(player);
        }

        private void SpawnTutorialFinalThreat(CommanderActor player)
        {
            Vector3 spawnPosition = SpawnPositionResolver.ResolveOutsideCamera(
                player.transform.position,
                TUTORIAL_FINAL_THREAT_MIN_CAMERA_MARGIN,
                TUTORIAL_FINAL_THREAT_MAX_CAMERA_MARGIN,
            _arenaBounds);

            RunDiagnostics.LogEnemyAliveSnapshot("before_tutorial_final_threat_spawn");
            EnemyEncounterDefinition definition = ResolveConfiguredFinalThreat();
            EnemyActor monster = _services.Spawner.SpawnEnemy(
                spawnPosition,
                definition.EnemyTemplateId,
                definition.EncounterRank,
                definition.ScaleMultiplier);
            if (monster == null)
            {
                Debug.LogWarning($"Tutorial final threat spawn failed. template_id={definition.EnemyTemplateId}");
                return;
            }

            FinalizeFinalThreatSpawn(
                monster,
                isTutorial: true,
                afterSpawnSnapshot: "after_tutorial_final_threat_spawn");
        }

        private void TryCompleteTutorialAfterFinalThreatDefeat()
        {
            if (_tutorialClearRequested
                || _activeFinalThreat == null
                || _activeFinalThreat.Hp > 0)
            {
                return;
            }

            _tutorialClearRequested = true;
            FindFirstObjectByType<GameScene>()?.ShowClearResult();
        }

        private void SpawnConfiguredBoss(CommanderActor player)
        {
            BossArena arena = BossArena.Create(player.transform.position, _services.Factory, _arenaBounds);
            if (arena == null)
                return;

            Vector3 spawnPosition = arena.BossSpawnPosition;
            RunDiagnostics.LogEnemyAliveSnapshot("before_boss_spawn");
            EnemyEncounterDefinition definition = ResolveConfiguredFinalThreat();
            EnemyActor monster = _services.Spawner.SpawnEnemy(
                spawnPosition,
                definition.EnemyTemplateId,
                definition.EncounterRank,
                definition.ScaleMultiplier);
            if (monster == null)
            {
                Debug.LogWarning($"Configured final threat spawn failed. template_id={definition.EnemyTemplateId}");
                BossArena.Clear();
                return;
            }

            FinalizeFinalThreatSpawn(
                monster,
                isTutorial: false,
                afterSpawnSnapshot: "after_boss_spawn");
        }

        private void FinalizeFinalThreatSpawn(EnemyActor monster, bool isTutorial, string afterSpawnSnapshot)
        {
            IRunFinalThreatBehaviour finalThreatBehaviour = monster.GetComponent<IRunFinalThreatBehaviour>();
            finalThreatBehaviour?.Setup(monster);

            _hasSpawnedFinalThreat = true;
            _activeFinalThreat = monster;
            string bossName = string.IsNullOrWhiteSpace(monster.RuntimeStats?.Data?.DisplayName)
                ? monster.EnemyId
                : monster.RuntimeStats.Data.DisplayName;
            _activeBossHudLabel = $"BOSS {bossName}";
            _bossPhaseStarted();
            RunTelemetry.Log(
                RunTelemetry.BossPhaseStart,
                RunTelemetry.RunTimeSecondsParameter,
                $"boss={monster.EnemyId}",
                isTutorial ? "tutorial_rules=active" : "normal_spawn=continued");

            _uiController?.HideBossPreWarning();
            DestroyBossDirectionPreview();

            RetroVfx.Spawn(RetroVfxKind.BossSpawn, monster.transform.position, Vector3.zero, 1.0f);
            HitStop.Request(BOSS_SPAWN_HIT_STOP_SECONDS, RunTelemetry.BossSpawnMarkerShow);
            CameraController.PlayFocusShot(monster.transform.position, BOSS_INTRO_CAMERA_SECONDS);
            RunTelemetry.Log(
                RunTelemetry.BossSpawnMarkerShow,
                RunTelemetry.RunTimeSecondsParameter,
                $"boss={monster.EnemyId}",
                "copy=boss_appears",
                "hp_bar=shown");
            _uiController?.ShowThreatDirection(
                monster.transform,
                "보스 등장",
                new Color(1.0f, 0.72f, 0.12f, 1.0f));
            RunTelemetry.LogOnce(RunTelemetry.FirstBossSeen, RunTelemetry.RunTimeSecondsParameter, $"boss={monster.EnemyId}");
            RunBossDpsTracker.BeginBossFight(monster);
            RunDiagnostics.LogEnemyAliveSnapshot(afterSpawnSnapshot);
        }

        public bool TryGetActiveBossHpSnapshot(out string hudLabel, out int hp, out int maxHp, out EnemyActor boss)
        {
            boss = null;
            hudLabel = _activeBossHudLabel;
            hp = 0;
            maxHp = 0;
            if (_activeFinalThreat == null
                || _activeFinalThreat.IsBoss == false
                || _activeFinalThreat.MaxHp <= 0)
            {
                return false;
            }

            boss = _activeFinalThreat;
            hp = Mathf.Clamp(_activeFinalThreat.Hp, 0, _activeFinalThreat.MaxHp);
            maxHp = _activeFinalThreat.MaxHp;
            return true;
        }

        public int GetActiveBossHpPercent()
        {
            return TryGetActiveBossHpSnapshot(out _, out int hp, out int maxHp, out _)
                ? Mathf.CeilToInt((float)hp / maxHp * 100.0f)
                : -1;
        }

        private void TryShowBossPreSpawnSignals(CommanderActor player, float remainingSeconds)
        {
            if (_uiController == null)
                return;

            if (_footstepWarningShown == false && remainingSeconds <= BOSS_FOOTSTEP_WARNING_SECONDS)
            {
                _footstepWarningShown = true;
                RunTelemetry.Log(
                    RunTelemetry.BossWarning15s,
                    RunTelemetry.RunTimeSecondsParameter,
                    "seconds_before_spawn=15",
                    "copy=boss_approach");
            }

            if (_edgeWarningShown == false && remainingSeconds <= BOSS_EDGE_WARNING_SECONDS)
            {
                _edgeWarningShown = true;
                EnsureBossDirectionPreviewTarget(player);
                _uiController.ShowBossPreWarning("WARNING", new Color(1.0f, 0.12f, 0.06f, 1.0f), remainingSeconds + 0.35f, showEdges: true);
                CombatRuntimeDiagnostics.Log(
                    "boss_warning",
                    CombatRuntimeDiagnostics.Text("boss_id", ResolveConfiguredBossId()),
                    CombatRuntimeDiagnostics.Float("warning_seconds", remainingSeconds + 0.35f),
                    CombatRuntimeDiagnostics.Float("remaining_seconds", remainingSeconds));
                RunTelemetry.Log(
                    RunTelemetry.BossWarning10s,
                    RunTelemetry.RunTimeSecondsParameter,
                    "seconds_before_spawn=5",
                    "red_edge=true",
                    "direction_indicator=false");
            }

        }

        private string ResolveConfiguredBossId()
        {
            EnemyEncounterDefinition definition = ResolveConfiguredFinalThreat();
            EnemyData data = _services?.App?.Data?.GetEnemyByTemplateId(definition.EnemyTemplateId);
            return string.IsNullOrWhiteSpace(data?.Id)
                ? $"template_{definition.EnemyTemplateId}"
                : data.Id;
        }

        private EnemyEncounterDefinition ResolveConfiguredFinalThreat()
        {
            return RunFinalThreatResolver.Resolve(
                _services.Context,
                _services.App.Data.RunTuning);
        }

private void EnsureBossDirectionPreviewTarget(CommanderActor player)
{
    _bossDirectionPreviewTarget ??= _authoredBossDirectionPreviewTarget ?? transform.Find("BossDirectionPreviewTarget");
    if (_bossDirectionPreviewTarget == null)
    {
        Debug.LogError("[BossSpawnController] Missing authored BossDirectionPreviewTarget.", this);
        return;
    }

    UpdateBossDirectionPreview(player);
}

        private void UpdateBossDirectionPreview(CommanderActor player)
        {
            if (_bossDirectionPreviewTarget == null || player == null)
                return;

            _bossDirectionPreviewTarget.position = player.transform.position + Vector3.up * BOSS_DIRECTION_PREVIEW_DISTANCE;
        }

private void DestroyBossDirectionPreview()
{
    _bossDirectionPreviewTarget = _authoredBossDirectionPreviewTarget;
}

        private void OnDestroy()
        {
            DestroyBossDirectionPreview();
        }

        private bool IsGameplayPaused() => _pauseController != null && _pauseController.IsPaused;
    }

}
