using System;
using Lizzo.PV.Flow;
using Lizzo.PV.Data;
using Lizzo.PV.Gameplay.Diagnostics;
using Lizzo.PV.Legion;
using Lizzo.PV.P0.Combat;
using Lizzo.PV.P0.Telemetry;
using Lizzo.PV.P0.Visuals;using Lizzo.PV.UI;
using Lizzo.PV.Gameplay.Route;
using Lizzo.PV.Gameplay.World;

using UnityEngine;

namespace Lizzo.PV.P0.Units
{
    public sealed class BossSpawnController : MonoBehaviour
    {
        RunServices _services;
        IGameplayRunUiFeedback _uiController;
        RunPauseController _pauseController;
        ArenaBounds _arenaBounds;
        Action _bossPhaseStarted;
        MonsterController _activeBoss;

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
            enabled = true;
        }

        private const float BOSS_FOOTSTEP_WARNING_SECONDS = 15.0f;
        private const float BOSS_EDGE_WARNING_SECONDS = 5.0f;
        private const float BOSS_SPAWN_HIT_STOP_SECONDS = 1.2f;
        private const float BOSS_INTRO_CAMERA_SECONDS = 1.2f;
        private const float BOSS_DIRECTION_PREVIEW_DISTANCE = 40.0f;

        float BossSpawnSeconds => BossSpawnReadiness.ResolveTargetSeconds(_services.Definition);

        private float _elapsedSeconds;
        private bool _hasSpawnedBoss;
        private bool _footstepWarningShown;
        private bool _edgeWarningShown;
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

            if (_hasSpawnedBoss)
            {
                P0BossDpsTracker.Tick();
                return;
            }

            PlayerController player = _services.Registry?.Player;
            if (player == null)
                return;

            _elapsedSeconds = _services.State.ElapsedSeconds;
            float bossSpawnSeconds = BossSpawnSeconds;
            float remainingSeconds = bossSpawnSeconds - _elapsedSeconds;
            UpdateBossDirectionPreview(player);
            TryShowBossPreSpawnSignals(player, remainingSeconds);

            if (BossSpawnReadiness.CanSpawn(
                    _services.Definition,
                    _elapsedSeconds,
                    _services.Party.ActiveCompanionSlotCount,
                    _services.Party.ActiveCompanionCount) == false)
                return;

            SpawnBoss(player);
        }

private void SpawnBoss(PlayerController player)
        {
            BossArena arena = BossArena.Create(player.transform.position, _services.Factory, _arenaBounds);
            if (arena == null)
                return;

            Vector3 spawnPosition = arena.BossSpawnPosition;
            P0PlaytestDiagnostics.LogEnemyAliveSnapshot("before_boss_spawn");
            RunBossDefinition boss = _services.Definition.Boss;
            MonsterController monster = _services.Spawner.SpawnEnemy(spawnPosition, boss.TemplateId);
            if (monster == null)
            {
                Debug.LogWarning($"[BossSpawnController] Boss spawn failed: {boss.ContentId}.");
                BossArena.Clear();
                return;
            }

            IRunBossRuntime bossRuntime = monster.GetComponent<IRunBossRuntime>();
            if (bossRuntime == null)
            {
                Debug.LogError(
                    $"[BossSpawnController] Boss prefab '{boss.ContentId}' is missing required IRunBossRuntime.",
                    monster);
                Destroy(monster.gameObject);
                BossArena.Clear();
                return;
            }

            _hasSpawnedBoss = true;
            _activeBoss = monster;
            _bossPhaseStarted();
            P0Telemetry.Log(
                P0Telemetry.BossPhaseStart,
                P0Telemetry.RunTimeSecondsParameter,
                $"boss={boss.ContentId}",
                "normal_spawn=continued");

            bossRuntime.Setup(monster);
            _services.UndeadSummon.OnBossPhaseStarted(monster, Time.time);
            _uiController?.HideBossPreWarning();
            DestroyBossDirectionPreview();

            RetroVfx.Spawn(RetroVfxKind.BossSpawn, monster.transform.position, Vector3.zero, 1.0f);
            HitStop.Request(BOSS_SPAWN_HIT_STOP_SECONDS, P0Telemetry.BossSpawnMarkerShow);
            CameraController.PlayFocusShot(monster.transform.position, BOSS_INTRO_CAMERA_SECONDS);
            P0Telemetry.Log(
                P0Telemetry.BossSpawnMarkerShow,
                P0Telemetry.RunTimeSecondsParameter,
                $"boss={boss.ContentId}",
                "copy=boss_appears",
                "hp_bar=shown");
            _uiController?.ShowThreatDirection(
                monster.transform,
                "보스 등장",
                new Color(1.0f, 0.72f, 0.12f, 1.0f));
            P0Telemetry.LogOnce(
                P0Telemetry.FirstBossSeen,
                P0Telemetry.RunTimeSecondsParameter,
                $"boss={boss.ContentId}");
            P0BossDpsTracker.BeginBossFight(monster);
            P0PlaytestDiagnostics.LogEnemyAliveSnapshot("after_boss_spawn");
        }

        private void TryShowBossPreSpawnSignals(PlayerController player, float remainingSeconds)
        {
            if (_uiController == null)
                return;

            if (_footstepWarningShown == false && remainingSeconds <= BOSS_FOOTSTEP_WARNING_SECONDS)
            {
                _footstepWarningShown = true;
                P0Telemetry.Log(
                    P0Telemetry.BossWarning15s,
                    P0Telemetry.RunTimeSecondsParameter,
                    "seconds_before_spawn=15",
                    "copy=boss_approaches");
            }

            if (_edgeWarningShown == false && remainingSeconds <= BOSS_EDGE_WARNING_SECONDS)
            {
                _edgeWarningShown = true;
                EnsureBossDirectionPreviewTarget(player);
                _uiController.ShowBossPreWarning("WARNING", new Color(1.0f, 0.12f, 0.06f, 1.0f), remainingSeconds + 0.35f, showEdges: true);
                Build1RuntimeDiagnostics.Log(
                    "boss_warning",
                    Build1RuntimeDiagnostics.Text("boss_id", _services.Definition.Boss.ContentId),
                    Build1RuntimeDiagnostics.Float("warning_seconds", remainingSeconds + 0.35f),
                    Build1RuntimeDiagnostics.Float("remaining_seconds", remainingSeconds));
                P0Telemetry.Log(
                    P0Telemetry.BossWarning10s,
                    P0Telemetry.RunTimeSecondsParameter,
                    "seconds_before_spawn=5",
                    "red_edge=true",
                    "direction_indicator=false");
            }

        }

private void EnsureBossDirectionPreviewTarget(PlayerController player)
{
    _bossDirectionPreviewTarget ??= _authoredBossDirectionPreviewTarget ?? transform.Find("BossDirectionPreviewTarget");
    if (_bossDirectionPreviewTarget == null)
    {
        Debug.LogError("[BossSpawnController] Missing authored BossDirectionPreviewTarget.", this);
        return;
    }

    UpdateBossDirectionPreview(player);
}

        private void UpdateBossDirectionPreview(PlayerController player)
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
            _activeBoss = null;
            DestroyBossDirectionPreview();
        }

        public bool TryGetCurrentHpSnapshot(out int hp, out int maxHp)
        {
            hp = 0;
            maxHp = 0;
            if (_activeBoss == null || _activeBoss.MaxHp <= 0)
                return false;

            hp = Mathf.Clamp(_activeBoss.Hp, 0, _activeBoss.MaxHp);
            maxHp = _activeBoss.MaxHp;
            return true;
        }

        private bool IsGameplayPaused() => _pauseController != null && _pauseController.IsPaused;
    }

}
