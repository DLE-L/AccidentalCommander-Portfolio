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
    private Color _clearTone = new(0.20f, 0.56f, 0.36f, 1f);

    [SerializeField]
    private Color _failureTone = new(0.62f, 0.20f, 0.20f, 1f);

    public bool Configure()
    {
        if (_layoutRoot == null ||
            _titleText == null ||
            _headlineText == null ||
            _bodyText == null ||
            _primaryButton == null ||
            _primaryButtonText == null)
        {
            Debug.LogError("[Result] Authored result references are required. Runtime layout creation is disabled.", this);
            return false;
        }

        return true;
    }

    public bool Present(Lizzo.PV.UI.RunResultViewData view, System.Action primaryRequested, System.Action optionalRequested)
    {
        if (view == null || !Configure())
            return false;

        _layoutRoot.SetActive(true);
        _titleText.text = view.Title;
        _headlineText.text = view.Headline;
        _bodyText.text = view.Body;
        _primaryButtonText.text = view.PrimaryButtonLabel;
        ApplyOutcomeVisual(view.IsClear);

        _primaryButton.onClick.RemoveAllListeners();
        _primaryButton.onClick.AddListener(() => primaryRequested?.Invoke());

        if (_optionalButton != null)
        {
            _optionalButton.gameObject.SetActive(view.OptionalButtonVisible);
            if (_optionalButtonText != null)
                _optionalButtonText.text = view.OptionalButtonLabel;

            _optionalButton.onClick.RemoveAllListeners();
            _optionalButton.onClick.AddListener(() => optionalRequested?.Invoke());
        }

        return true;
    }

    private void ApplyOutcomeVisual(bool isClear)
    {
        Color tone = isClear ? _clearTone : _failureTone;

        if (_titlePanelImage != null)
            _titlePanelImage.color = tone;

        if (_primaryButtonImage != null)
            _primaryButtonImage.color = tone;

        if (_contentPanelImage != null)
            _contentPanelImage.color = isClear
                ? new Color(0.07f, 0.14f, 0.10f, 0.98f)
                : new Color(0.16f, 0.07f, 0.07f, 0.98f);

        _headlineText.color = Color.white;
    }
}
