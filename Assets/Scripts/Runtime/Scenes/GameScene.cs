using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Lizzo.PV.P0.Cards;

using Lizzo.PV.P0.Debugging;
using Lizzo.PV.Flow;

using Lizzo.PV.Legion;
using Lizzo.PV.P0.Telemetry;
using Lizzo.PV.P0.Units;
using Lizzo.PV.P0.Visuals;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEngine.UI;
using Lizzo.PV.Data;using Lizzo.PV.UI;


public partial class GameScene : MonoBehaviour
{
    bool _restartRequested;

    public void RestartRun()
    {
        if (_restartRequested)
            return;

        _restartRequested = true;
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }


public void ShowClearResult()
    {
        _runState?.TryEnd(RunOutcome.Clear, 0);
    }


public void ShowFailureResult(int bossHpPercent)
    {
        _runState?.TryEnd(RunOutcome.Failure, bossHpPercent);
    }

void HandleRunEnded(RunResult result)
    {
        _pauseController?.MarkRunEnded();
        string resultName = result.Outcome == RunOutcome.Clear ? "clear" : "failure";

        RunResultViewData view = result.Outcome == RunOutcome.Clear
            ? new RunResultViewData(true, "승리", "전투 종료", "보스를 처치했습니다.", "다음 실행", false, string.Empty)
            : new RunResultViewData(false, "실패", "전투 종료", "사령관이 쓰러졌습니다.", "계속하기", false, string.Empty);

        try
        {
            if (_uiController == null)
            {
                Debug.LogError("[GameScene] Gameplay UI controller is missing when the run ends.", this);
                return;
            }

            if (!_uiController.ShowResult(view, RestartRun, null))
            {
                Debug.LogError("[GameScene] Result popup could not present the run result.", this);
                return;
            }

            P0Telemetry.Log(
                P0Telemetry.ResultView,
                P0Telemetry.RunTimeSecondsParameter,
                $"result={resultName}",
                $"duration_seconds={Mathf.Max(0, Mathf.RoundToInt(result.ElapsedSeconds))}",
                $"kill_count={result.KillCount}",
                $"boss_hp_percent={result.BossHpPercent}");

            if (_services?.Party != null)
            {
                P0Telemetry.Log(
                    P0Telemetry.ResultBuildSummaryShow,
                    P0Telemetry.RunTimeSecondsParameter,
                    $"result={resultName}",
                    $"legion={_services.Party.BuildLegionSummary().Replace(' ', '_')}",
                    $"synergy={_services.Party.GetCompletedSynergySummary().Replace(' ', '_')}");
            }
        }
        catch (InvalidOperationException exception)
        {
            Debug.LogException(exception, this);
        }
        finally
        {
            P0Telemetry.EndRun(resultName, result.BossHpPercent);
        }
    }



    public void Initialize(RunServices services, GameplayUIController uiController, RunPauseController pauseController)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _uiController = uiController ?? throw new ArgumentNullException(nameof(uiController));
        _pauseController = pauseController ?? throw new ArgumentNullException(nameof(pauseController));
    }

    void Start()
    {
        if (_services == null)
        {
            Debug.LogError("[GameScene] RunServices must be initialized by RunBootstrap before Start().", this);
            return;
        }
        InitializeResourcesAsync().Forget();
    }
    async UniTaskVoid InitializeResourcesAsync()
    {
        try
        {
            AssetPreloadResult preload = await _services.App.Assets.PreloadLabelAsync<Object>(
                "PreLoad",
                this.GetCancellationTokenOnDestroy());

            if (!preload.Succeeded)
            {
                Debug.LogError($"[GameScene] PreLoad failed. total={preload.TotalCount}, success={preload.SuccessCount}, failed={preload.FailedAddresses.Count}");
                return;
            }

            if (!await ValidateRequiredResourcesAsync(this.GetCancellationTokenOnDestroy()))
                return;
            DataLoadResult dataResult = await _services.App.Data.InitializeAsync(this.GetCancellationTokenOnDestroy());
            if (!dataResult.Succeeded)
            {
                Debug.LogError($"[GameScene] Data provider initialization failed. missing={dataResult.MissingRequiredIds.Count}");
                return;
            }
            StartLoaded();

        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    async UniTask<bool> ValidateRequiredResourcesAsync(CancellationToken cancellationToken)
    {
        bool valid = true;
        valid &= await _services.App.Assets.LoadAsync<TextAsset>("PlayerData.xml", cancellationToken) != null;
        valid &= await _services.App.Assets.LoadAsync<GameObject>("Map_01.prefab", cancellationToken) != null;
        valid &= await _services.App.Assets.LoadAsync<GameObject>("P0/Units/Commander/Commander.prefab", cancellationToken) != null;
        valid &= await _services.App.Assets.LoadAsync<GameObject>("CommanderProjectile.prefab", cancellationToken) != null;
        valid &= await _services.App.Assets.LoadAsync<GameObject>("BossArenaAuthoring.prefab", cancellationToken) != null;
        if (!valid)
            Debug.LogError("[GameScene] One or more required startup resources are missing or have the wrong type.");
        return valid;
    }

    RunServices _services;
    public RunServices Services => _services;

    [Header("Authored Spawn Controllers")]
    [SerializeField] StageSpawner _stageSpawner;
    [SerializeField] EliteSpawnController _eliteSpawnController;
    [SerializeField] BossSpawnController _bossSpawnController;
    Lizzo.PV.Flow.RunState _runState;
    RunPauseController _pauseController; GameplayUIController _uiController;

    Define.StageType _stageType;
    public Define.StageType StageType
    {
        get => _stageType;
        set
        {
            _stageType = value;
            if (_stageSpawner == null)
                return;

            _stageSpawner.Stopped = value == Define.StageType.Boss;
        }
    }

	void StartLoaded()
    {
        _runState = _services.State;
        _runState.Reset(_services.App.Data.GetLevelExp(1));
        RetroVfx.PreloadDefaults();
        P0Telemetry.BeginRun();
        UI_GameScene sceneUi = _uiController == null ? null : _uiController.Hud;
        if (sceneUi == null)
        {
            Debug.LogError("[GameScene] Scene HUD could not be created.");
            return;
        }


        sceneUi.SetBattleTime(0.0f, BossSpawnController.HungryGiantSpawnDelaySeconds);

        _pauseController.Initialize();

        if (_stageSpawner == null || _eliteSpawnController == null || _bossSpawnController == null)
        {
            Debug.LogError("[GameScene] Authored StageSpawner, EliteSpawnController, and BossSpawnController references are required.", this);
            return;
        }

        _stageSpawner.Initialize(_services, _pauseController);
        _eliteSpawnController.Initialize(_services, _uiController, _pauseController);
        _bossSpawnController.Initialize(_services, _uiController, _pauseController);

        PlayerController player = _services.Spawner.SpawnPlayer(Vector3.zero);
        if (player == null)
        {
            Debug.LogError("[GameScene] Commander spawn failed.");
            return;
        }



        GameObject map = _services.Factory.Spawn("Map_01.prefab");
        if (map == null)
            return;
        map.name = "@Map";
        SortingOrder.ApplyToRenderers(map, SortingOrder.Map);

        Camera mainCamera = Camera.main;
        CameraController cameraController = mainCamera == null ? null : mainCamera.GetComponent<CameraController>();
        if (cameraController == null)
        {
            Debug.LogError("[GameScene] Main camera or CameraController is missing.");
            return;
        }

        cameraController.Initialize(_services);
        cameraController.Target = player.gameObject;
        P0GuardSquadPushTestScenario.TryStart(player, _stageSpawner);

        _runState.KillCountChanged -= HandleKillCountChanged;
        _runState.KillCountChanged += HandleKillCountChanged;
        _runState.ExperienceChanged -= HandleExperienceChanged;
        _runState.ExperienceChanged += HandleExperienceChanged;
        _runState.RunEnded -= HandleRunEnded;
        _runState.RunEnded += HandleRunEnded;
        _pauseController.Initialize();
        _uiController.ModalChanged -= _pauseController.SetModalOpen;
        _uiController.ModalChanged += _pauseController.SetModalOpen;
        if (!_uiController.Initialize(_services.Factory, _services.Party, _pauseController.ToggleUserPause, _pauseController.ResumeFromPauseButton))
        {
            Debug.LogError("[GameScene] Gameplay UI controller initialization failed.");
            return;
        }

        _pauseController.PauseOverlayChanged -= _uiController.SetPauseOverlay;
        _pauseController.PauseOverlayChanged += _uiController.SetPauseOverlay;
        _uiController.SetPauseOverlay(_pauseController.IsPaused, false);
        _uiController.ShowGameplay();
        _uiController.BindPlayer(player);
        _runState.MarkLoaded();
    }

    public bool IsRunLoaded => _runState != null && _runState.IsLoaded;
    public int TestCurrentRunLevel => _runState?.Level ?? 0;
    public int TestCollectedExp => _runState?.Experience ?? 0;
    public int TestRequiredExp => _runState?.RequiredExperience ?? 0;
    public float TestRunElapsedSeconds => _runState?.ElapsedSeconds ?? 0.0f;

    public void HandleExperienceChanged(int currentExperience, int requiredExperience)
    {
        if (currentExperience >= requiredExperience)
        {
            ShowLevelUpPopupAndAdvance();
            return;
        }

        RefreshExpUi();
    }

    public void HandleKillCountChanged(int killCount)
    {
        _uiController?.Hud?.SetKillCount(killCount);
    }

    void ShowLevelUpPopupAndAdvance()
    {
        int nextLevel = (_runState?.Level ?? 1) + 1;
        _runState?.AdvanceLevel(Mathf.Max(1, _services.App.Data.GetLevelExp(nextLevel)));

        if (_services.Registry?.Player != null)
            RetroVfx.Spawn(RetroVfxKind.LevelUp, _services.Registry.Player.transform.position, Vector3.zero, 1.0f);

        HitStop.Request(0.15f, "level_up_card_select");
        _uiController?.ShowSkillSelection();
        RefreshExpUi();
    }

    void RefreshExpUi()
    {
        int requiredExp = Mathf.Max(1, _runState.RequiredExperience);
        UI_GameScene ui = _uiController?.Hud;
        if (ui == null)
            return;

        ui.SetGemCountRatio((float)_runState.Experience / requiredExp);
        ui.SetRunLevel(_runState.Level);
    }

    void Update()
	{
		if (!IsRunLoaded)
			return;

		P0Telemetry.SamplePerformance(Time.unscaledDeltaTime);
		_runState.AdvanceTime(Time.deltaTime);
		float bossRemainingSeconds = BossSpawnController.HungryGiantSpawnDelaySeconds - _runState.ElapsedSeconds;
		_uiController?.Hud?.SetBattleTime(_runState.ElapsedSeconds, bossRemainingSeconds);

	}

	private void OnDestroy()
	{
		if (_runState != null)
		{
			_runState.KillCountChanged -= HandleKillCountChanged;
			_runState.ExperienceChanged -= HandleExperienceChanged;
            _runState.RunEnded -= HandleRunEnded;
		}


		if (_uiController != null && _pauseController != null)
		{
			_uiController.ModalChanged -= _pauseController.SetModalOpen;
			_pauseController.PauseOverlayChanged -= _uiController.SetPauseOverlay;
		}

		P0Telemetry.FlushRunLog("game_scene_destroy");

	}
}
