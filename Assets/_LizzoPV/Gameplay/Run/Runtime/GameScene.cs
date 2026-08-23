using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Lizzo.PV.P0.Cards;
using Lizzo.PV.Combat;

using Lizzo.PV.P0.Debugging;
using Lizzo.PV.Flow;

using Lizzo.PV.Legion;
using Lizzo.PV.P0.Telemetry;
using Lizzo.PV.P0.Units;
using Lizzo.PV.P0.Visuals;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;
using UnityEngine.UI;
using Lizzo.PV.Data;using Lizzo.PV.UI;
using Lizzo.PV.Gameplay.Route;
using Lizzo.PV.Gameplay.RunTraits;
using Lizzo.PV.Gameplay.UI.HUD;
using Lizzo.PV.Legion.Synergy;
using Lizzo.PV.Gameplay.World;


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
        _services?.RunTraitOffers?.ExpirePendingOpportunities();
        if (result.Outcome == RunOutcome.Clear && _services?.Registry?.Player != null)
            RetroVfx.Spawn(RetroVfxKind.ResultClear, _services.Registry.Player.transform.position, Vector3.zero, 1.0f);

        _pauseController?.MarkRunEnded();
        _failureResultOpen = result.Outcome == RunOutcome.Failure;
        string resultName = result.Outcome == RunOutcome.Clear ? "clear" : "failure";
        DamageContributionSnapshot contributionSnapshot = _services?.DamageContributions?.CaptureSnapshot(_services?.Synergies);
        DamageContributionSummaryTelemetry.Emit(
            resultName,
            contributionSnapshot,
            (eventName, payload) => P0Telemetry.Log(eventName, payload));
        const string testStageLabel = "1-1";
        if (result.Outcome == RunOutcome.Clear && _services.Context.IsTutorial)
            FirstRunProgress.TryCommitTutorialClear();
        if (result.Outcome == RunOutcome.Clear)
            _services?.App.CompanionUnlockProgress.TryMarkStage1FirstClear();

        PartyService party = _services?.Party;
        List<PauseCompanionPresentation> companionPresentations = new List<PauseCompanionPresentation>(7);
        List<PausePassivePresentation> passivePresentations = new List<PausePassivePresentation>(5);
        List<PauseSynergyPresentation> synergyPresentations = new List<PauseSynergyPresentation>(8);
        PauseBuildSummaryPresentationResolver.Fill(
            party?.GetSquadSlotSnapshot(),
            _services?.PassiveRoster,
            _services?.Synergies,
            _services?.App?.Data,
            companionPresentations,
            passivePresentations,
            synergyPresentations,
            7,
            5,
            this);
        bool hasCompletedSynergy = synergyPresentations.Count > 0;
        string partySummary = string.Empty;
        if (party != null)
        {
            string formationSummary = party.BuildLegionSummary();
            if (string.IsNullOrWhiteSpace(formationSummary) == false && formationSummary != "군단")
                partySummary = $"편성 {formationSummary}";
        }

        RunResultSnapshotSet resultSnapshots = RunResultSnapshotResolver.Capture(_services);
        RunResultBestSynergyPresentation bestActiveSynergy = null;
        if (result.Outcome == RunOutcome.Clear)
        {
            bestActiveSynergy = ResolveBestActiveSynergy(contributionSnapshot, _services?.App?.Data);
        }
        string synergySectionLabel = result.Outcome == RunOutcome.Clear
            ? "이번 클리어 우수 시너지"
            : "이번 런에서 완성한 시너지";
        string synergyName = result.Outcome == RunOutcome.Clear
            ? (bestActiveSynergy?.SummaryText ?? "우수 시너지 없음")
            : JoinSynergyDisplayNames(synergyPresentations);
        string synergyMembers = string.Empty;
        string synergyEffect = string.Empty;
        IReadOnlyList<int> synergyIconIndices = Array.Empty<int>();
        RunResultViewData view = result.Outcome == RunOutcome.Clear
            ? new RunResultViewData(
                true,
                "승리",
                string.Empty,
                testStageLabel,
                string.Empty,
                "다시 출정",
                false,
                string.Empty,
                result.ElapsedSeconds,
                result.KillCount,
                _runState?.Level ?? 1,
                partySummary,
                string.Empty,
                string.Empty,
                hasCompletedSynergy,
                synergySectionLabel,
                synergyName,
                synergyMembers,
                synergyEffect,
                synergyIconIndices,
                resultSnapshots.SquadSlots,
                companionPresentations,
                passivePresentations,
                synergyPresentations,
                bestActiveSynergy,
                FixedCardPool.MaxBuildComplete,
                resultSnapshots.FinalLegion,
                resultSnapshots.CompletedSynergies,
                resultSnapshots.SelectedTraits)
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
                hasCompletedSynergy,
                synergySectionLabel,
                synergyName,
                synergyMembers,
                synergyEffect,
                synergyIconIndices,
                resultSnapshots.SquadSlots,
                companionPresentations,
                passivePresentations,
                synergyPresentations,
                null,
                FixedCardPool.MaxBuildComplete,
                resultSnapshots.FinalLegion,
                resultSnapshots.CompletedSynergies,
                resultSnapshots.SelectedTraits);

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

    private RunResultBestSynergyPresentation ResolveBestActiveSynergy(
        DamageContributionSnapshot snapshot,
        IDataProvider data)
    {
        DamageContributionEntry? best = snapshot?.BestActiveSynergy;
        if (best.HasValue == false)
            return null;

        SynergyData synergy = data?.GetSynergy(best.Value.Id);
        if (synergy == null || string.IsNullOrWhiteSpace(synergy.DisplayName))
        {
            Debug.LogError($"[GameScene] Missing canonical synergy display data: {best.Value.Id}", this);
            return null;
        }

        return new RunResultBestSynergyPresentation(best.Value.Id, synergy.DisplayName, best.Value.TotalScore);
    }

    private static string JoinSynergyDisplayNames(IReadOnlyList<PauseSynergyPresentation> synergies)
    {
        if (synergies == null || synergies.Count == 0)
            return "활성 시너지 없음";

        System.Text.StringBuilder result = new System.Text.StringBuilder(64);
        for (int i = 0; i < synergies.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(synergies[i].DisplayName))
                continue;
            if (result.Length > 0)
                result.Append(" · ");
            result.Append(synergies[i].DisplayName);
        }

        return result.Length == 0 ? "활성 시너지 없음" : result.ToString();
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




    public void Initialize(RunServices services, IGameplayRunUi uiController, RunPauseController pauseController)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _uiController = uiController ?? throw new ArgumentNullException(nameof(uiController));
        _pauseController = pauseController ?? throw new ArgumentNullException(nameof(pauseController));
    }

    void Start()
    {
        if (_services == null)
            return;

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
    [FormerlySerializedAs("_build1CombatHud")]
    [SerializeField] SynergyNotificationBannerController _synergyNotificationBanner;
    Lizzo.PV.Flow.RunState _runState;
    RunPauseController _pauseController; IGameplayRunUi _uiController;

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
        P0Telemetry.BeginRun(
            _services.Context.Mode,
            FixedCardPool.CardOfferPolicyVersion,
            FixedCardPool.CardOfferConfigAssignmentHash,
            CommanderWeaponCatalog.ToId(_services.Context.CommanderWeapon));
        _pauseController.Initialize();

        if (_stageSpawner == null || _eliteSpawnController == null || _bossSpawnController == null)
        {
            Debug.LogError("[GameScene] Authored StageSpawner, EliteSpawnController, and BossSpawnController references are required.", this);
            return;
        }

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
        ArenaBounds arenaBounds = map.GetComponent<ArenaBounds>();
        if (arenaBounds == null)
        {
            Debug.LogError("[GameScene] Authored map is missing ArenaBounds.", map);
            return;
        }

        player.BindArenaBounds(arenaBounds);
        _services.Party.BindArenaBounds(arenaBounds);

        Camera mainCamera = Camera.main;
        CameraController cameraController = mainCamera == null ? null : mainCamera.GetComponent<CameraController>();
        if (cameraController == null)
        {
            Debug.LogError("[GameScene] Main camera or CameraController is missing.");
            return;
        }

        cameraController.Initialize(_services);
        cameraController.BindArenaBounds(arenaBounds);
        _services.BindVisibilityQuery(cameraController.VisibilityQuery);
        cameraController.Target = player.gameObject;
        _stageSpawner.Initialize(_services, _pauseController, arenaBounds);
        _eliteSpawnController.Initialize(_services, _uiController, _pauseController, arenaBounds);
        _bossSpawnController.Initialize(_services, _uiController, _pauseController, arenaBounds);
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
        if (!_uiController.Initialize(_services, mainCamera, _pauseController))
        {
            Debug.LogError("[GameScene] Gameplay UI controller initialization failed.");
            return;
        }

        if (_synergyNotificationBanner == null
            || _synergyNotificationBanner.Configure(_services.Build1SynergyProgression, _pauseController) == false)
        {
            Debug.LogError("[GameScene] Authored synergy notification banner is required.", this);
            return;
        }

        _pauseController.PauseOverlayChanged -= _uiController.SetPauseOverlay;
        _pauseController.PauseOverlayChanged += _uiController.SetPauseOverlay;
        _pauseController.GameplaySpeedChanged -= _uiController.SetGameplaySpeed;
        _pauseController.GameplaySpeedChanged += _uiController.SetGameplaySpeed;
        _uiController.SetGameplaySpeed(_pauseController.SelectedGameplaySpeed);
        _uiController.SetPauseOverlay(_pauseController.IsPaused, false);
        _uiController.SetRunStatus(0, 0.0f);
        _uiController.SetExperienceStatus(_runState.Level, _runState.Experience, _runState.RequiredExperience);
        _uiController.BindPlayer(player);
        _uiController.ShowGameplay();
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
            _uiController.SetRunStatus(killCount, _runState?.ElapsedSeconds ?? 0.0f);
    }

    void ShowLevelUpPopupAndAdvance()
    {
        int nextLevel = (_runState?.Level ?? 1) + 1;
        _runState?.AdvanceLevel(Mathf.Max(1, _services.App.Data.GetLevelExp(nextLevel)));

        if (_services.Registry?.Player != null)
            RetroVfx.Spawn(RetroVfxKind.LevelUp, _services.Registry.Player.transform.position, Vector3.zero, 1.0f);

        if (_uiController?.ShowSkillSelection() == true)
            HitStop.Request(0.15f, "level_up_card_select");
        RefreshExpUi();
    }

    void RefreshExpUi()
    {
        int requiredExp = Mathf.Max(1, _runState.RequiredExperience);
        if (_uiController == null)
            return;

        _uiController.SetExperienceStatus(_runState.Level, _runState.Experience, requiredExp);
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
			_uiController.SetRunStatus(_runState.KillCount, _runState.ElapsedSeconds);
			UpdateBossHud();
			TryPresentRunTraitOffer();
		}

	}

    void TryPresentRunTraitOffer()
    {
        if (_services?.RunTraitOffers == null || _runState == null || _runState.IsLoaded == false
            || _runState.ElapsedSeconds >= BossSpawnController.HungryGiantSpawnDelaySeconds
            || _stageType == Define.StageType.Boss || _uiController is not IRunTraitOfferUi traitOfferUi)
            return;

        bool isPresentationSafe = traitOfferUi.IsModalOpen == false && traitOfferUi.IsPauseOverlayVisible == false;

        RunTraitEligibilityContext context = RunTraitEligibilityContextResolver.Resolve(
            _services,
            emergencyRallyActivated: false,
            secondsUntilBossSpawn: Mathf.Max(0.0f, BossSpawnController.HungryGiantSpawnDelaySeconds - _runState.ElapsedSeconds),
            isPresentationSafe: isPresentationSafe);
        RunTraitOfferPolicy policy = ResolveRunTraitOfferPolicy(_services.RunTraitOffers.GetPendingOpportunityIndex(_runState.ElapsedSeconds));
        if (_services.RunTraitOffers.TryGetPendingOffer(_runState.ElapsedSeconds, context, policy, out RunTraitOfferSnapshot snapshot))
            traitOfferUi.ShowRunTraitOffer(snapshot, HandleRunTraitSelection);
    }

    static RunTraitOfferPolicy ResolveRunTraitOfferPolicy(int opportunityIndex)
    {
        string profileId = CardCatalogProvider.TryGetPool(out CardPoolDefinition pool)
            ? pool.ProfileId
            : CardPoolProfileIds.Standard;
        return RunTraitOfferPolicy.Resolve(profileId, opportunityIndex);
    }

    bool HandleRunTraitSelection(string offerIdentity, int slotIndex, string traitId)
    {
        return _runState != null && _runState.IsLoaded && _stageType != Define.StageType.Boss
            && _services?.RunTraitOffers != null
            && _services.RunTraitOffers.TryAcceptSelection(offerIdentity, slotIndex, traitId);
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
