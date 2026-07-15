using Lizzo.PV.P0.Telemetry;
using Lizzo.PV.Legion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class UI_GameScene : UI_Base
{
    private PartyService _party;

public bool ConfigureParty(PartyService party)
    {
        _party = party;
        if (_party == null)
        {
            Debug.LogError("[UI_GameScene] PartyService is required.", this);
            return false;
        }

        if (_init)
            InitializeSquadSlotHud();

        return true;
    }
    [Header("Core HUD")]
    [SerializeField] private TextMeshProUGUI _killCountText;
    [SerializeField] private Slider _gemSlider;
    [SerializeField] private TextMeshProUGUI _runLevelText;
    [SerializeField] private TextMeshProUGUI _survivalTimeText;

    [Header("Boss")]
    [SerializeField] private GameObject _bossHpRoot;
    [SerializeField] private RectTransform _bossHpRectTransform;
    [SerializeField] private Image _bossHpFillImage;
    [SerializeField] private TMP_Text _bossNameText;
    [SerializeField] private TMP_Text _bossHpText;

    [Header("Pause")]
    [SerializeField] private Button _pauseButton;
    [SerializeField] private GameObject _pauseOverlay;
    [SerializeField] private GraphicRaycaster _pauseOverlayRaycaster;
    [SerializeField] private TMP_Text _pauseTitleText;
    [SerializeField] private TMP_Text _pauseBodyText;
    [SerializeField] private TMP_Text _pauseContinueText;

    [Header("Announcement")]
    [SerializeField] private GameObject _announcementRoot;
    [SerializeField] private CanvasGroup _announcementGroup;
    [SerializeField] private Image _announcementPanelImage;
    [SerializeField] private Image _announcementAccentImage;
    [SerializeField] private TMP_Text _announcementTitleText;
    [SerializeField] private TMP_Text _announcementBodyText;

    [Header("Threat")]
    [SerializeField] private GameObject _threatIndicatorRoot;
    [SerializeField] private RectTransform _threatIndicatorRectTransform;
    [SerializeField] private RectTransform _threatIndicatorArrowRectTransform;
    [SerializeField] private Image _threatIndicatorBackgroundImage;
    [SerializeField] private TMP_Text _threatIndicatorArrowText;
    [SerializeField] private TMP_Text _threatIndicatorLabelText;

    private Transform _threatIndicatorTarget;
    private int _lastSurvivalSeconds = -1;
    private int _lastBossSeconds = -1;
    private int _lastBossHpPercent = int.MinValue;
    private int _lastBossHp = int.MinValue;
    private int _lastBossMaxHp = int.MinValue;
    private float _announcementStartedAt = -999.0f;
    private float _announcementDuration = ANNOUNCEMENT_DURATION;
    private float _threatIndicatorUntil = -999.0f;

    private const float ANNOUNCEMENT_DURATION = 2.1f;
    private const float ANNOUNCEMENT_FADE_IN = 0.12f;
    private const float ANNOUNCEMENT_FADE_OUT = 0.38f;
    private const float THREAT_INDICATOR_DURATION = -1.0f;
    private const float THREAT_VIEWPORT_MARGIN = 0.06f;
    private const float THREAT_EDGE_MARGIN_X = 62.0f;
    private const float THREAT_EDGE_MARGIN_Y = 116.0f;

public override bool Init()
    {
        if (base.Init() == false)
            return false;

        ResolveTimerTextReferences();
        ResolveBossHpBarReferences();
        if (_party != null)
            InitializeSquadSlotHud();
        BindPauseControls();
        SetBattleTime(0.0f, 0.0f);
        P0Telemetry.Log(
            P0Telemetry.HudVisibilityCheck,
            "scene_ui=shown",
            "boss_hpbar=under_exp",
            "debug_overlay_expected_hidden=true");
        return true;
    }

    public void SetGemCountRatio(float ratio)
    {
        _gemSlider.value = ratio;
    }

    public void SetRunLevel(int level)
    {
        ResolveRunLevelText();
        if (_runLevelText != null)
            _runLevelText.text = Mathf.Max(1, level).ToString();
    }

    public void SetKillCount(int killCount)
    {
        _killCountText.text = $"{killCount}";
    }

public void SetBattleTime(float survivalSeconds, float bossRemainingSeconds)
{
    ResolveTimerTextReferences();
    if (_survivalTimeText == null)
        return;

    int survival = Mathf.Max(0, Mathf.FloorToInt(survivalSeconds));
    int boss = Mathf.Max(0, Mathf.CeilToInt(bossRemainingSeconds));
    if (survival == _lastSurvivalSeconds && boss == _lastBossSeconds)
        return;

    _lastSurvivalSeconds = survival;
    _lastBossSeconds = boss;
    _survivalTimeText.text = FormatTime(survival);
}

    private void ResolveRunLevelText()
    {
        _runLevelText ??= Utils.FindChild<TextMeshProUGUI>(gameObject, "CharacterLevelValueText", true);
        if (_runLevelText == null)
            Debug.LogError("UI_GameScene prefab is missing required CharacterLevelValueText.", this);
    }

    private void ResolveTimerTextReferences()
    {
        _survivalTimeText ??= Utils.FindChild<TextMeshProUGUI>(gameObject, "TimeLimitValueText", true);

        TextMeshProUGUI bossLabelText = Utils.FindChild<TextMeshProUGUI>(gameObject, "WaveText", true);
        if (bossLabelText != null)
            bossLabelText.gameObject.SetActive(false);

        if (_survivalTimeText == null)
            Debug.LogError("UI_GameScene prefab is missing required TimeLimitValueText for the survival timer.", this);
    }

    private static string FormatTime(int totalSeconds)
    {
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        return $"{minutes:00}:{seconds:00}";
    }

    private void Update()
    {
        UpdateBossHpBar();
        UpdateAnnouncement();
        UpdateThreatIndicator();
        UpdateBossWarningOverlay();
        UpdateSquadSlotHud();
        P0PlaytestDiagnostics.SampleBossBodyVisibility(_threatIndicatorRoot != null && _threatIndicatorRoot.activeSelf);
    }
}
