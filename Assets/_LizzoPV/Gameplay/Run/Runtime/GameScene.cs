using System;
using Cysharp.Threading.Tasks;
using Lizzo.PV.P0.Cards;

using Lizzo.PV.Flow;

using Lizzo.PV.P0.Telemetry;
using Lizzo.PV.P0.Units;
using UnityEngine;
using UnityEngine.Serialization;
using Lizzo.PV.UI;
using Lizzo.PV.Gameplay.Route;
using Lizzo.PV.Gameplay.Run;
using Lizzo.PV.Gameplay.UI.HUD;


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
        _gameplayUpdate = new RunGameplayUpdateCoordinator(
            _services,
            _uiController,
            () => _stageType);
        _gameplayUiLifecycle = new RunGameplayUiLifecycleCoordinator(
            _services,
            _uiController,
            _pauseController,
            () => _synergyNotificationBanner != null
                && _synergyNotificationBanner.Configure(
                    _services.Build1SynergyProgression,
                    _pauseController),
            this);
        _worldBootstrap = new RunWorldBootstrapCoordinator(
            _services,
            _uiController,
            _pauseController,
            _stageSpawner,
            _eliteSpawnController,
            _bossSpawnController,
            this);
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
    RunGameplayUpdateCoordinator _gameplayUpdate;
    RunGameplayUiLifecycleCoordinator _gameplayUiLifecycle;
    RunWorldBootstrapCoordinator _worldBootstrap;
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

        if (!_worldBootstrap.TryInitialize(out PlayerController player, out Camera mainCamera))
            return;

        _runState.KillCountChanged -= HandleKillCountChanged;
        _runState.KillCountChanged += HandleKillCountChanged;
        _runState.ExperienceChanged -= _levelProgression.HandleExperienceChanged;
        _runState.ExperienceChanged += _levelProgression.HandleExperienceChanged;
        _runState.RunEnded -= HandleRunEnded;
        _runState.RunEnded += HandleRunEnded;
        if (!_gameplayUiLifecycle.TryActivate(mainCamera, player))
            return;

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

    void Update()
	{
		_gameplayUpdate?.Tick(Time.deltaTime, Time.unscaledDeltaTime);
    }

	private void OnDestroy()
	{
		if (_runState != null)
		{
			_runState.KillCountChanged -= HandleKillCountChanged;
			_runState.ExperienceChanged -= _levelProgression.HandleExperienceChanged;
            _runState.RunEnded -= HandleRunEnded;
		}


        _gameplayUiLifecycle?.Dispose();

		P0Telemetry.FlushRunLog("game_scene_destroy");

	}
}
