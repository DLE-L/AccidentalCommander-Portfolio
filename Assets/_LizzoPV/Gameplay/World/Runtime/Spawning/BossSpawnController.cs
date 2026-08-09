using Lizzo.PV.Flow;
using Lizzo.PV.Data;
using Lizzo.PV.Gameplay.Diagnostics;
using Lizzo.PV.Legion;
using Lizzo.PV.P0.Combat;
using Lizzo.PV.P0.Telemetry;
using Lizzo.PV.P0.Visuals;using Lizzo.PV.UI;
using Lizzo.PV.Gameplay.Route;

using UnityEngine;

namespace Lizzo.PV.P0.Units
{
    public sealed class BossSpawnController : MonoBehaviour
    {
        RunServices _services;
        IGameplayRunUiFeedback _uiController;
        RunPauseController _pauseController;

        public void Initialize(RunServices services, IGameplayRunUiFeedback uiController, RunPauseController pauseController)
        {
            _services = services ?? throw new System.ArgumentNullException(nameof(services));
            _uiController = uiController;
            _pauseController = pauseController ?? throw new System.ArgumentNullException(nameof(pauseController));
            enabled = true;
        }

        private const float BOSS_FOOTSTEP_WARNING_SECONDS = 15.0f;
        private const float BOSS_EDGE_WARNING_SECONDS = 5.0f;
        private const float BOSS_SPAWN_HIT_STOP_SECONDS = 1.2f;
        private const float BOSS_INTRO_CAMERA_SECONDS = 1.2f;
        private const float BOSS_DIRECTION_PREVIEW_DISTANCE = 40.0f;

        public static float HungryGiantSpawnDelaySeconds => Lizzo.PV.P0.Config.RemoteConfig.BossSpawnSeconds;

        private float _elapsedSeconds;
        private bool _hasSpawnedHungryGiant;
        private bool _footstepWarningShown;
        private bool _edgeWarningShown;
        [SerializeField] private Transform _authoredBossDirectionPreviewTarget;

private Transform _bossDirectionPreviewTarget;

        [ContextMenu("Debug/Jump To Hungry Giant Prelude")]
        public void DebugJumpToHungryGiantPrelude()
        {
            _elapsedSeconds = HungryGiantSpawnDelaySeconds - BOSS_FOOTSTEP_WARNING_SECONDS;
            _footstepWarningShown = false;
            _edgeWarningShown = false;
            DestroyBossDirectionPreview();
        }

        private void Update()
        {
            if (IsGameplayPaused())
                return;

            if (_hasSpawnedHungryGiant)
            {
                P0BossDpsTracker.Tick();
                return;
            }

            PlayerController player = _services.Registry?.Player;
            if (player == null)
                return;

            _elapsedSeconds += Time.deltaTime;
            float remainingSeconds = HungryGiantSpawnDelaySeconds - _elapsedSeconds;
            UpdateBossDirectionPreview(player);
            TryShowBossPreSpawnSignals(player, remainingSeconds);

            if (_elapsedSeconds < HungryGiantSpawnDelaySeconds)
                return;

            SpawnHungryGiant(player);
        }

private void SpawnHungryGiant(PlayerController player)
        {
            BossArena arena = BossArena.Create(player.transform.position, _services.Factory);
            if (arena == null)
                return;

            Vector3 spawnPosition = arena.BossSpawnPosition;
            P0PlaytestDiagnostics.LogEnemyAliveSnapshot("before_boss_spawn");
            MonsterController monster = _services.Spawner.SpawnEnemy(spawnPosition, Define.BOSS_ID);
            if (monster == null)
            {
                Debug.LogWarning("P0 Hungry Giant spawn failed.");
                BossArena.Clear();
                return;
            }

            HungryGiantBehaviour hungryGiant = monster.GetComponent<HungryGiantBehaviour>();
            if (hungryGiant == null)
            {
                Debug.LogError("Hungry Giant prefab is missing required HungryGiantBehaviour.", monster);
                Destroy(monster.gameObject);
                BossArena.Clear();
                return;
            }

            _hasSpawnedHungryGiant = true;
            GameScene gameScene = GetComponent<GameScene>();
            if (gameScene != null)
                gameScene.StageType = Define.StageType.Boss;
            P0Telemetry.Log(
                P0Telemetry.BossPhaseStart,
                P0Telemetry.RunTimeSecondsParameter,
                "boss=HungryGiant",
                "normal_spawn=stopped");

            hungryGiant.Setup(monster);
            _services.UndeadSummon.OnBossPhaseStarted(monster, Time.time);
            _uiController?.HideBossPreWarning();
            DestroyBossDirectionPreview();

            RetroVfx.Spawn(RetroVfxKind.BossSpawn, monster.transform.position, Vector3.zero, 1.0f);
            RetroVfx.Spawn(RetroVfxKind.BossWarning, monster.transform.position, Vector3.zero, 1.15f);
            HitStop.Request(BOSS_SPAWN_HIT_STOP_SECONDS, P0Telemetry.BossSpawnMarkerShow);
            CameraController.PlayFocusShot(monster.transform.position, BOSS_INTRO_CAMERA_SECONDS);
            P0Telemetry.Log(
                P0Telemetry.BossSpawnMarkerShow,
                P0Telemetry.RunTimeSecondsParameter,
                "boss=HungryGiant",
                "copy=hungry_giant_appears",
                "hp_bar=shown");
            _uiController?.ShowThreatDirection(
                monster.transform,
                "보스 등장",
                new Color(1.0f, 0.72f, 0.12f, 1.0f));
            P0Telemetry.LogOnce(P0Telemetry.FirstBossSeen, P0Telemetry.RunTimeSecondsParameter, "boss=HungryGiant");
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
                    "copy=giant_footsteps");
            }

            if (_edgeWarningShown == false && remainingSeconds <= BOSS_EDGE_WARNING_SECONDS)
            {
                _edgeWarningShown = true;
                EnsureBossDirectionPreviewTarget(player);
                _uiController.ShowBossPreWarning("WARNING", new Color(1.0f, 0.12f, 0.06f, 1.0f), remainingSeconds + 0.35f, showEdges: true);
                Build1RuntimeDiagnostics.Log(
                    "boss_warning",
                    Build1RuntimeDiagnostics.Text("boss_id", CombatIds.BossHungryGiant),
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
            DestroyBossDirectionPreview();
        }

        private bool IsGameplayPaused() => _pauseController != null && _pauseController.IsPaused;
    }

}
