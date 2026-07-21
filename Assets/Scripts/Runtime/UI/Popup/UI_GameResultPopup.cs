using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

public class UI_GameResultPopup : UI_Base
{
    [SerializeField]
    private GameObject _layoutRoot;

    [SerializeField]
    private TMP_Text _titleText;

    [SerializeField]
    [FormerlySerializedAs("_headlineText")]
    private TMP_Text _stageValueText;

    [SerializeField]
    [FormerlySerializedAs("_bodyText")]
    private TMP_Text _survivalTimeLabelText;

    [SerializeField]
    private TMP_Text _survivalTimeValueText;

    [SerializeField]
    private TMP_Text _killCountValueText;

    [SerializeField]
    [FormerlySerializedAs("_deathCauseText")]
    private TMP_Text _primaryDetailText;

    [SerializeField]
    [FormerlySerializedAs("_recommendationText")]
    private TMP_Text _secondaryDetailText;


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
    private TMP_Text _reviveChoiceTimeoutText;

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

    private const int ReviveChoiceTimeoutSeconds = 10;
    private CancellationTokenSource _reviveChoiceTimeoutCancellation;
    private bool _reviveChoiceTransitioned;

    public bool Configure()
    {
        if (_layoutRoot == null ||
            _titleText == null ||
            _stageValueText == null ||
            _survivalTimeLabelText == null ||
            _survivalTimeValueText == null ||
            _killCountValueText == null ||
            _primaryDetailText == null ||
            _secondaryDetailText == null ||
            _lobbyText == null ||
            _lobbyButton == null ||
            _primaryButton == null ||
            _primaryButtonText == null ||
            _optionalButton == null ||
            _optionalButtonText == null ||
            _optionalButtonImage == null ||
            _reviveChoiceRoot == null ||
            _reviveChoiceTitleText == null ||
            _reviveChoiceTimeoutText == null ||
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

        CancelReviveChoiceTimeout();
        _reviveChoiceTransitioned = false;
        _layoutRoot.SetActive(false);
        _reviveChoiceRoot.SetActive(true);
        _primaryButton.gameObject.SetActive(false);
        _optionalButton.gameObject.SetActive(false);
        _lobbyButton.gameObject.SetActive(false);
        _reviveChoiceTitleText.text = "부활 방법을 선택하세요";
        _reviveChoiceTimeoutText.text = ReviveChoiceTimeoutSeconds.ToString();
        _reviveChoiceAdText.text = "광고 보고 부활";
        _reviveChoiceCurrencyText.text = "재화 사용 부활";

        // The choice cards are intentionally presentation-only until revive services are approved.
        _reviveChoiceAdButton.onClick.RemoveAllListeners();
        _reviveChoiceCurrencyButton.onClick.RemoveAllListeners();
        _reviveChoiceCloseButton.onClick.RemoveAllListeners();
        _reviveChoiceCloseButton.onClick.AddListener(() => TransitionFromReviveChoice(view, primaryRequested, lobbyRequested));

        _reviveChoiceTimeoutCancellation = new CancellationTokenSource();
        RunReviveChoiceTimeoutAsync(view, primaryRequested, lobbyRequested, _reviveChoiceTimeoutCancellation.Token).Forget();

        return true;
    }

    public bool Present(Lizzo.PV.UI.RunResultViewData view, System.Action primaryRequested, System.Action optionalRequested, System.Action lobbyRequested)
    {
        if (view == null || !Configure())
            return false;

        CancelReviveChoiceTimeout();
        _reviveChoiceTransitioned = true;
        bool isClear = view.IsClear;
        _reviveChoiceRoot.SetActive(false);
        _layoutRoot.SetActive(true);
        _titleText.text = isClear ? view.Title : "쓰러졌습니다";
        _stageValueText.gameObject.SetActive(true);
        _stageValueText.text = view.StageLabel;
        _survivalTimeLabelText.gameObject.SetActive(true);
        _survivalTimeValueText.gameObject.SetActive(true);
        _survivalTimeValueText.text = FormatElapsed(view.ElapsedSeconds);
        _killCountValueText.gameObject.SetActive(true);
        _killCountValueText.text = Mathf.Max(0, view.KillCount).ToString();
        _primaryDetailText.gameObject.SetActive(true);
        _primaryDetailText.text = isClear
            ? $"도달 레벨 {Mathf.Max(1, view.Level)}"
            : view.FailureCause;
        _secondaryDetailText.gameObject.SetActive(true);
        _secondaryDetailText.text = isClear ? view.PartySummary : view.Recommendation;
        _lobbyText.gameObject.SetActive(true);
        _lobbyText.text = "로비로";
        ApplyOutcomeVisual(isClear);

        _primaryButton.gameObject.SetActive(true);
        _primaryButtonText.text = view.PrimaryButtonLabel;
        _primaryButton.onClick.RemoveAllListeners();
        _primaryButton.onClick.AddListener(() => primaryRequested?.Invoke());

        // StatisticsButton is an authored bottom-left control without an approved action yet.
        _optionalButton.gameObject.SetActive(true);
        _optionalButton.interactable = false;
        _optionalButtonText.text = string.Empty;
        _optionalButton.onClick.RemoveAllListeners();

        _lobbyButton.gameObject.SetActive(true);
        _lobbyButton.onClick.RemoveAllListeners();
        _lobbyButton.onClick.AddListener(() => lobbyRequested?.Invoke());

        return true;
    }

    private async UniTask RunReviveChoiceTimeoutAsync(
        Lizzo.PV.UI.RunResultViewData view,
        Action primaryRequested,
        Action lobbyRequested,
        CancellationToken cancellationToken)
    {
        int remainingSeconds = ReviveChoiceTimeoutSeconds;
        try
        {
            while (remainingSeconds > 0)
            {
                await UniTask.Delay(
                    TimeSpan.FromSeconds(1),
                    DelayType.Realtime,
                    PlayerLoopTiming.Update,
                    cancellationToken);

                remainingSeconds--;
                _reviveChoiceTimeoutText.text = remainingSeconds.ToString();
            }

            TransitionFromReviveChoice(view, primaryRequested, lobbyRequested);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void TransitionFromReviveChoice(
        Lizzo.PV.UI.RunResultViewData view,
        Action primaryRequested,
        Action lobbyRequested)
    {
        if (_reviveChoiceTransitioned)
            return;

        _reviveChoiceTransitioned = true;
        CancelReviveChoiceTimeout();
        Present(view, primaryRequested, null, lobbyRequested);
    }

    private void CancelReviveChoiceTimeout()
    {
        if (_reviveChoiceTimeoutCancellation == null)
            return;

        _reviveChoiceTimeoutCancellation.Cancel();
        _reviveChoiceTimeoutCancellation.Dispose();
        _reviveChoiceTimeoutCancellation = null;
    }

    private void OnDestroy()
    {
        CancelReviveChoiceTimeout();
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
        _stageValueText.color = Color.white;
    }
}
