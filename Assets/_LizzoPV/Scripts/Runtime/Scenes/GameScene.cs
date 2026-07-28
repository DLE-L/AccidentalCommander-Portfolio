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
    bool _failureResultOpen;

    bool _runStartRequested;

    public void RestartRun()
    {
        if (_restartRequested)
            return;

        _restartRequested = true;
        GameFlowRoutes.ReloadBattleScene(gameObject.scene);
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
        _failureResultOpen = result.Outcome == RunOutcome.Failure;
        string resultName = result.Outcome == RunOutcome.Clear ? "clear" : "failure";
        const string testStageLabel = "1-1";
        if (result.Outcome == RunOutcome.Clear && _services.Context.IsTutorial)
            FirstRunProgress.TryCommitTutorialClear();
        if (result.Outcome == RunOutcome.Clear)
            _services?.App.CompanionUnlockProgress.TryMarkStage1FirstClear();

        PartyService party = _services?.Party;
        string synergySummary = party?.GetCompletedSynergySummary() ?? "없음";
        bool hasCompletedSynergy = synergySummary == "근위대";
        string partySummary = string.Empty;
        if (party != null)
        {
            if (hasCompletedSynergy)
                partySummary = $"시너지 {synergySummary}";
            else
            {
                string formationSummary = party.BuildLegionSummary();
                if (string.IsNullOrWhiteSpace(formationSummary) == false && formationSummary != "군단")
                    partySummary = $"편성 {formationSummary}";
            }
        }

        IReadOnlyList<RunResultSquadSlotView> squadSlots = BuildResultSquadSlots(party, hasCompletedSynergy);
        string synergySectionLabel = result.Outcome == RunOutcome.Clear
            ? "이번 클리어 시너지"
            : "이번 런에서 완성한 시너지";
        string synergyName = hasCompletedSynergy ? "근위대" : "완성한 시너지 없음";
        string synergyMembers = hasCompletedSynergy ? "방패 계열 + 검병 + 성직자" : string.Empty;
        string synergyEffect = hasCompletedSynergy ? "지휘관 중심 방어 밀치기" : string.Empty;
        IReadOnlyList<int> synergyIconIndices = hasCompletedSynergy
            ? new[] { 0, 1, 2 }
            : Array.Empty<int>();
        RunResultViewData view = result.Outcome == RunOutcome.Clear
            ? new RunResultViewData(
                true,
                "승리",
                string.Empty,
                testStageLabel,
                string.Empty,
                "전투 준비 계속",
                false,
                string.Empty,
                result.ElapsedSeconds,
                result.KillCount,
                _runState?.Level ?? 1,
                partySummary,
                string.Empty,
                string.Empty,
                0,
                hasCompletedSynergy,
                synergySectionLabel,
                synergyName,
                synergyMembers,
                synergyEffect,
                synergyIconIndices,
                squadSlots)
            : new RunResultViewData(
                false,
                "쓰러졌습니다",
                "이번 전투 기록",
                testStageLabel,
                "다시 전장에 들어가 준비를 이어가세요.",
                "다시 도전",
                true,
                "부활하기 1/1",
                result.ElapsedSeconds,
                result.KillCount,
                _runState?.Level ?? 1,
                partySummary,
                "사령관이 전투 중 쓰러졌습니다.",
                "동료를 모아 강화하세요.",
                0,
                hasCompletedSynergy,
                synergySectionLabel,
                synergyName,
                synergyMembers,
                synergyEffect,
                synergyIconIndices,
                squadSlots);

        try
        {
            if (_uiController == null)
            {
                Debug.LogError("[GameScene] Gameplay UI controller is missing when the run ends.", this);
                return;
            }

            Action primaryRequested = result.Outcome == RunOutcome.Clear
                ? GameFlowRoutes.LoadLobby
                : RestartRun;
            Action optionalRequested = result.Outcome == RunOutcome.Failure && _runState != null && _runState.CanRevive
                ? TryReviveRun
                : null;
            Action lobbyRequested = GameFlowRoutes.LoadLobby;
            if (!_uiController.ShowResult(view, primaryRequested, optionalRequested, lobbyRequested))
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

    private static IReadOnlyList<RunResultSquadSlotView> BuildResultSquadSlots(
        PartyService party,
        bool highlightGuardSquad)
    {
        const int slotCount = 7;
        List<RunResultSquadSlotView> result = new List<RunResultSquadSlotView>(slotCount);
        IReadOnlyList<SquadSlotState> snapshot = party?.GetSquadSlotSnapshot();
        for (int i = 0; i < slotCount; i++)
        {
            SquadSlotState state = snapshot != null && i < snapshot.Count
                ? snapshot[i]
                : default;
            result.Add(new RunResultSquadSlotView(
                string.IsNullOrWhiteSpace(state.SlotId) ? $"slot_{i:00}" : state.SlotId,
                string.IsNullOrWhiteSpace(state.DisplayName) ? "빈 슬롯" : state.DisplayName,
                ResolveResultIconIndex(state.SlotId),
                state.CurrentCount,
                highlightGuardSquad && i < 3));
        }

        return result;
    }

    private static int ResolveResultIconIndex(string slotId)
    {
        return slotId switch
        {
            "shield_family" => 0,
            "sword_family" => 1,
            "cleric_family" => 2,
            "ranged_family" => 3,
            "spear_family" => 4,
            "magic_family" => 5,
            "support_family" => 6,
            _ => 7,
        };
    }

void TryReviveRun()
    {
        if (!_failureResultOpen || _runState == null || _pauseController == null || _uiController == null)
            return;

        PlayerController player = _services?.Registry?.Player;
        if (player == null || player.RestoreFullHealth() == false)
        {
            Debug.LogError("[GameScene] Commander health could not be restored for revive.", this);
            return;
        }

        if (_runState.TryResumeAfterRevive() == false)
        {
            Debug.LogError("[GameScene] Run state could not resume after revive.", this);
            return;
        }

        if (_pauseController.ResumeAfterRevive() == false)
        {
            _runState.MarkStopped();
            Debug.LogError("[GameScene] Run pause state could not resume after revive.", this);
            return;
        }

        _failureResultOpen = false;
        _uiController.CloseModal();
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
        BeginRunFromRoute();
    }

    public void BeginRunFromRoute()
    {
        if (_runStartRequested)
            return;

        _runStartRequested = true;
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
        P0Telemetry.BeginRun(_services.Context.Mode);
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
        _services.BindVisibilityQuery(cameraController.VisibilityQuery);
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
        if (!_uiController.Initialize(
                _services.Factory,
                _services.Party,
                mainCamera,
                _pauseController.ToggleUserPause,
                _pauseController.ResumeFromPauseButton,
                _pauseController.ToggleGameplaySpeed,
                () => _pauseController.SelectedGameplaySpeed))
        {
            Debug.LogError("[GameScene] Gameplay UI controller initialization failed.");
            return;
        }

        _pauseController.PauseOverlayChanged -= _uiController.SetPauseOverlay;
        _pauseController.PauseOverlayChanged += _uiController.SetPauseOverlay;
        _pauseController.GameplaySpeedChanged -= _uiController.SetGameplaySpeed;
        _pauseController.GameplaySpeedChanged += _uiController.SetGameplaySpeed;
        _uiController.SetGameplaySpeed(_pauseController.SelectedGameplaySpeed);
        _uiController.SetPauseOverlay(_pauseController.IsPaused, false);
        _uiController.SetRunStatus(0, 0, 0.0f);
        _uiController.SetExperienceStatus(_runState.Level, 0.0f);
        _uiController.ShowGameplay();
        _uiController.BindPlayer(player);
        _runState.MarkLoaded();
        SceneTransitionOverlay.Hide();
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
        if (_uiController != null)
            _uiController.SetRunStatus(0, killCount, _runState?.ElapsedSeconds ?? 0.0f);
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
        if (_uiController == null)
            return;

        _uiController.SetExperienceStatus(_runState.Level, (float)_runState.Experience / requiredExp);
    }

    void UpdateBossHud()
    {
        if (HungryGiantBehaviour.TryGetCurrentHpSnapshot(out int hp, out int maxHp))
        {
            float ratio = maxHp <= 0 ? 0.0f : Mathf.Clamp01((float)hp / maxHp);
            _uiController.ShowBoss("BOSS Hungry Giant", hp, maxHp);
            P0PlaytestDiagnostics.LogBossHpSample(hp, maxHp, ratio, "ui_update");
            P0PlaytestDiagnostics.SampleBossBodyVisibility(_uiController.IsThreatDirectionVisible);
            return;
        }

        _uiController.HideBoss();
    }

    void Update()
	{
		if (!IsRunLoaded)
			return;

		P0Telemetry.SamplePerformance(Time.unscaledDeltaTime);
		_runState.AdvanceTime(Time.deltaTime);
		if (_uiController != null)
		{
			_uiController.SetRunStatus(0, _runState.KillCount, _runState.ElapsedSeconds);
			UpdateBossHud();
		}

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
			_pauseController.GameplaySpeedChanged -= _uiController.SetGameplaySpeed;
		}

		P0Telemetry.FlushRunLog("game_scene_destroy");

	}
}
