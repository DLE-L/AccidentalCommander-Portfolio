using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_GameResultPopup : UI_Base
{
    [SerializeField]
    private GameObject _layoutRoot;

    [SerializeField]
    private TMP_Text _titleText;

    [SerializeField]
    private TMP_Text _headlineText;

    [SerializeField]
    private TMP_Text _bodyText;

    [SerializeField]
    private TMP_Text _survivalTimeValueText;

    [SerializeField]
    private TMP_Text _deathCauseText;

    [SerializeField]
    private TMP_Text _recommendationText;


    [SerializeField]
    private Button _lobbyButton;

    [SerializeField]
    private TMP_Text _lobbyText;

    [SerializeField]
    private Button _primaryButton;

    [SerializeField]
    private TMP_Text _primaryButtonText;

    [SerializeField]
    private Button _optionalButton;

    [SerializeField]
    private TMP_Text _optionalButtonText;

    [SerializeField]
    private Image _contentPanelImage;

    [SerializeField]
    private Image _titlePanelImage;

    [SerializeField]
    private Image _primaryButtonImage;

    [SerializeField]
    private Image _optionalButtonImage;

    [SerializeField]
    private Color _clearTone = new(0.20f, 0.56f, 0.36f, 1f);

    [SerializeField]
    private Color _failureTone = new(0.62f, 0.20f, 0.20f, 1f);

    [Header("Failure Revive Choice")]
    [SerializeField]
    private GameObject _reviveChoiceRoot;

    [SerializeField]
    private TMP_Text _reviveChoiceTitleText;

    [SerializeField]
    private TMP_Text _reviveChoiceAdText;

    [SerializeField]
    private TMP_Text _reviveChoiceCurrencyText;

    [SerializeField]
    private Button _reviveChoiceAdButton;

    [SerializeField]
    private Button _reviveChoiceCurrencyButton;

    [SerializeField]
    private Button _reviveChoiceCloseButton;

    public bool Configure()
    {
        if (_layoutRoot == null ||
            _titleText == null ||
            _headlineText == null ||
            _bodyText == null ||
            _survivalTimeValueText == null ||
            _deathCauseText == null ||
            _recommendationText == null ||
            _lobbyText == null ||
            _lobbyButton == null ||
            _primaryButton == null ||
            _primaryButtonText == null ||
            _optionalButton == null ||
            _optionalButtonText == null ||
            _optionalButtonImage == null ||
            _reviveChoiceRoot == null ||
            _reviveChoiceTitleText == null ||
            _reviveChoiceAdText == null ||
            _reviveChoiceCurrencyText == null ||
            _reviveChoiceAdButton == null ||
            _reviveChoiceCurrencyButton == null ||
            _reviveChoiceCloseButton == null)
        {
            Debug.LogError("[Result] Authored result references are required. Runtime layout creation is disabled.", this);
            return false;
        }

        return true;
    }

    public bool PresentFailureReviveChoice(
        Lizzo.PV.UI.RunResultViewData view,
        System.Action primaryRequested,
        System.Action lobbyRequested)
    {
        if (view == null || view.IsClear || !Configure())
            return false;

        _layoutRoot.SetActive(false);
        _reviveChoiceRoot.SetActive(true);
        _primaryButton.gameObject.SetActive(false);
        _optionalButton.gameObject.SetActive(false);
        _lobbyButton.gameObject.SetActive(false);
        _reviveChoiceTitleText.text = "부활 방법을 선택하세요";
        _reviveChoiceAdText.text = "광고 보고 부활";
        _reviveChoiceCurrencyText.text = "재화 사용 부활";

        // The choice cards are intentionally presentation-only until revive services are approved.
        _reviveChoiceAdButton.onClick.RemoveAllListeners();
        _reviveChoiceCurrencyButton.onClick.RemoveAllListeners();
        _reviveChoiceCloseButton.onClick.RemoveAllListeners();
        _reviveChoiceCloseButton.onClick.AddListener(() =>
            Present(view, primaryRequested, null, lobbyRequested));

        return true;
    }

    public bool Present(Lizzo.PV.UI.RunResultViewData view, System.Action primaryRequested, System.Action optionalRequested, System.Action lobbyRequested)
    {
        if (view == null || !Configure())
            return false;

        bool isClear = view.IsClear;
        _reviveChoiceRoot.SetActive(false);
        _layoutRoot.SetActive(true);
        _titleText.text = isClear ? view.Title : "쓰러졌습니다";
        _headlineText.gameObject.SetActive(true);
        _headlineText.text = $"레벨 {Mathf.Max(1, view.Level)}";
        _bodyText.gameObject.SetActive(!string.IsNullOrWhiteSpace(isClear ? view.Body : view.FailureCause));
        _bodyText.text = isClear ? view.Body : view.FailureCause;
        _survivalTimeValueText.gameObject.SetActive(true);
        _survivalTimeValueText.text = $"생존 {FormatElapsed(view.ElapsedSeconds)}";
        _deathCauseText.gameObject.SetActive(isClear);
        _deathCauseText.text = $"처치 {Mathf.Max(0, view.KillCount)}";
        _recommendationText.gameObject.SetActive(!isClear);
        _recommendationText.text = view.Recommendation;
        _lobbyText.gameObject.SetActive(true);
        _lobbyText.text = "로비로";
        ApplyOutcomeVisual(isClear);

        _primaryButton.gameObject.SetActive(true);
        _primaryButtonText.text = view.PrimaryButtonLabel;
        _primaryButton.onClick.RemoveAllListeners();
        _primaryButton.onClick.AddListener(() => primaryRequested?.Invoke());

        _optionalButton.gameObject.SetActive(isClear && view.OptionalButtonVisible && optionalRequested != null);
        _optionalButtonText.text = view.OptionalButtonLabel;
        _optionalButton.onClick.RemoveAllListeners();
        _optionalButton.onClick.AddListener(() => optionalRequested?.Invoke());

        _lobbyButton.gameObject.SetActive(true);
        _lobbyButton.onClick.RemoveAllListeners();
        _lobbyButton.onClick.AddListener(() => lobbyRequested?.Invoke());

        return true;
    }

    private static string FormatElapsed(float elapsedSeconds)
    {
        int totalSeconds = Mathf.Max(0, Mathf.RoundToInt(elapsedSeconds));
        return $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
    }

private void ApplyOutcomeVisual(bool isClear)
    {
        _primaryButtonText.color = Color.white;
        _optionalButtonText.color = Color.white;
        _lobbyText.color = new Color(0.72f, 0.72f, 0.72f, 1.0f);
        _headlineText.color = Color.white;
    }
}
