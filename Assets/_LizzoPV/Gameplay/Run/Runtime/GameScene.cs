using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using Lizzo.PV.P0.Cards;

using Lizzo.PV.P0.Debugging;
using Lizzo.PV.Flow;

using Lizzo.PV.Legion;
using Lizzo.PV.P0.Telemetry;
using Lizzo.PV.P0.Units;
using Lizzo.PV.P0.Visuals;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Lizzo.PV.UI;
using Lizzo.PV.Gameplay.Route;
using Lizzo.PV.Gameplay.Run;
using Lizzo.PV.Gameplay.RunTraits;
using Lizzo.PV.Gameplay.UI.HUD;
using Lizzo.PV.Legion.Synergy;
using Lizzo.PV.Gameplay.World;


public partial class GameScene : MonoBehaviour
{
    bool _restartRequested;

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
        _resultFlow.HandleRunEnded(result);
    }




    public void Initialize(RunServices services, IGameplayRunUi uiController, RunPauseController pauseController)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _uiController = uiController ?? throw new ArgumentNullException(nameof(uiController));
        _pauseController = pauseController ?? throw new ArgumentNullException(nameof(pauseController));
        _resultFlow = new RunResultFlowCoordinator(
            _services,
            _uiController,
            _pauseController,
            RestartRun,
            GameFlowRoutes.LoadLobby,
            this);
        _levelProgression = new RunLevelProgressionCoordinator(_services, _uiController);
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
            if (!await RunStartupResourceLoader.PrepareAsync(
                    _services.App,
                    this.GetCancellationTokenOnDestroy()))
                return;

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

    RunServices _services;
    RunResultFlowCoordinator _resultFlow;
    RunLevelProgressionCoordinator _levelProgression;
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
        _runState.ExperienceChanged -= _levelProgression.HandleExperienceChanged;
        _runState.ExperienceChanged += _levelProgression.HandleExperienceChanged;
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

    public void HandleKillCountChanged(int killCount)
    {
        if (_uiController != null)
            _uiController.SetRunStatus(killCount, _runState?.ElapsedSeconds ?? 0.0f);
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
			_runState.ExperienceChanged -= _levelProgression.HandleExperienceChanged;
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
