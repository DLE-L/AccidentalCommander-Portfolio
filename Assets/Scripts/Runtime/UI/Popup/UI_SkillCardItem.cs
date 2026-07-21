using Lizzo.PV.P0.Cards;
using Lizzo.PV.Legion;
using Lizzo.PV.P0.Presentation;
using Lizzo.PV.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_SkillCardItem : UI_Base
{
    const float CardAccentBackgroundBlend = 0.3f;
    const float CardAccentTitleBlend = 0.38f;
    static readonly Color DefaultCardBackground = new Color(0.12f, 0.17f, 0.24f, 0.98f);
    static readonly Color RecommendedGold = new Color(1.0f, 0.78f, 0.12f, 0.95f);
    const float CollectionPulseSpeed = 5.0f;
    const float CollectionPulseMinimumAlpha = 0.3f;
    static readonly Color CompanionCollectionGold = new Color(1.0f, 0.86f, 0.45f, 1.0f);

    [Header("Interaction")]
    [SerializeField] Button _clickButton;
    [SerializeField] CanvasGroup _canvasGroup;
    [SerializeField] Outline _highlightOutline;
    [SerializeField] Image _backgroundImage;

    [Header("Content")]
    [SerializeField] TMP_Text _cardNameText;
    [SerializeField] TMP_Text _skillDescriptionText;
    [SerializeField] Image _skillIcon;

    [Header("Companion Presentation")]
    [SerializeField] GameObject _skillDescriptionRoot;
    [SerializeField] GameObject _combatTraitRoot;
    [SerializeField] TMP_Text _combatTraitText;
    [SerializeField] GameObject _collectionProgressRoot;
    [SerializeField] GameObject[] _collectionFilledPips;
    [SerializeField] GameObject[] _collectionEmptyPips;

    [Header("State Labels")]
    [SerializeField] GameObject _newIndicatorRoot;
    [SerializeField] TMP_Text _newText;

    CardData _cardData;
    System.Action<UI_SkillCardItem, CardData> _selectionRequested;
    PartyService _party;
    Sprite _authoredSkillIconSprite;
    bool _authoredSkillIconEnabled;
    bool _authoredSkillIconPreserveAspect;
    Image.Type _authoredSkillIconType;
    Color _authoredSkillIconColor;
    bool _authoredSkillIconRaycastTarget;
    bool _authoredSkillIconStateCached;
    Image[] _collectionFilledImages = new Image[3];
    bool _collectionPulseActive;
    int _collectionPulseIndex = -1;


    public bool Configure(System.Action<UI_SkillCardItem, CardData> selectionRequested, PartyService party)
    {
        _selectionRequested = selectionRequested;
        _party = party;
        if (_party == null)
        {
            UnityEngine.Debug.LogError("[UI_SkillCardItem] PartyService is required.", this);
            return false;
        }

        return ValidateAuthoredReferences();
    }

    bool ValidateAuthoredReferences()
    {
        if (_clickButton == null
            || _canvasGroup == null
            || _highlightOutline == null
            || _backgroundImage == null
            || _cardNameText == null
            || _skillDescriptionText == null
            || _skillIcon == null
            || _skillDescriptionRoot == null
            || _combatTraitRoot == null
            || _combatTraitText == null
            || _collectionProgressRoot == null
            || _collectionFilledPips == null
            || _collectionFilledPips.Length != 3
            || _collectionEmptyPips == null
            || _collectionEmptyPips.Length != 3
            || _newIndicatorRoot == null
            || _newText == null)
        {
            Debug.LogError("[UI_SkillCardItem] All authored interaction, content, companion presentation, and state-label references are required.", this);
            return false;
        }

        for (int i = 0; i < 3; i++)
        {
            Image filledImage = _collectionFilledPips[i] != null
                ? _collectionFilledPips[i].GetComponent<Image>()
                : null;
            if (_collectionFilledPips[i] == null
                || _collectionEmptyPips[i] == null
                || filledImage == null
                || filledImage.sprite == null)
            {
                Debug.LogError("[UI_SkillCardItem] Three authored filled and empty collection pips with RoundSquare20 filled sprites are required.", this);
                return false;
            }

            _collectionFilledImages[i] = filledImage;
        }

        return true;
    }



    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        if (!ValidateAuthoredReferences())
            return false;

        CacheAuthoredSkillIconState();
        EnsureCardClickTarget();
        RefreshUI();
        return true;
    }

    public void SetInfo(CardData cardData)
    {
        _cardData = cardData;
        RefreshUI();
    }

    void RefreshUI()
    {
        if (_init == false)
            return;

        if (!ValidateAuthoredReferences())
            return;

        _skillDescriptionText.maxVisibleLines = 2;
        _skillDescriptionText.overflowMode = TextOverflowModes.Ellipsis;

        SkillCardPresentationModel presentation = SkillCardPresentationResolver.Resolve(_cardData, _party);
        ApplyCardAccentStyle(presentation.CatalogEntry);
        ApplyCardIcon(presentation.CatalogEntry);
        ApplySelectionVisual(selected: false, faded: false);

        _cardNameText.text = _cardData.Title ?? string.Empty;
        _skillDescriptionRoot.SetActive(true);
        _skillDescriptionText.text = presentation.Description;
        ApplyCompanionPresentation(presentation);

        _newIndicatorRoot.SetActive(presentation.HasStatus);
        SetHighlightFrame(
            presentation.HighlightFrame,
            presentation.Recommended,
            ResolveCardAccentColor(presentation.CatalogEntry));
        _newText.text = presentation.StatusText;
    }

    void EnsureCardClickTarget()
    {
        if (!ValidateAuthoredReferences())
            return;

        _backgroundImage.raycastTarget = true;
        _clickButton.interactable = true;
        _clickButton.targetGraphic = _backgroundImage;
    }

    void ApplyCardIcon(CardPresentationSet.Entry presentationEntry)
    {
        CacheAuthoredSkillIconState();

        bool hasRuntimeIcon = presentationEntry != null && presentationEntry.Icon != null;
        if (hasRuntimeIcon)
        {
            _skillIcon.enabled = true;
            _skillIcon.sprite = presentationEntry.Icon;
            _skillIcon.preserveAspect = true;
            _skillIcon.raycastTarget = false;
        }
        else
        {
            _skillIcon.enabled = _authoredSkillIconEnabled;
            _skillIcon.sprite = _authoredSkillIconSprite;
            _skillIcon.preserveAspect = _authoredSkillIconPreserveAspect;
            _skillIcon.type = _authoredSkillIconType;
            _skillIcon.color = _authoredSkillIconColor;
            _skillIcon.raycastTarget = _authoredSkillIconRaycastTarget;
        }

        ApplyCardDescriptionLayout();
    }

    void CacheAuthoredSkillIconState()
    {
        if (_authoredSkillIconStateCached || _skillIcon == null)
            return;

        _authoredSkillIconSprite = _skillIcon.sprite;
        _authoredSkillIconEnabled = _skillIcon.enabled;
        _authoredSkillIconPreserveAspect = _skillIcon.preserveAspect;
        _authoredSkillIconType = _skillIcon.type;
        _authoredSkillIconColor = _skillIcon.color;
        _authoredSkillIconRaycastTarget = _skillIcon.raycastTarget;
        _authoredSkillIconStateCached = true;
    }


    void ApplyCardDescriptionLayout()
    {
        RectTransform descriptionRect = _skillDescriptionText.rectTransform.parent as RectTransform;
        if (descriptionRect == null)
            return;

        descriptionRect.anchorMin = new Vector2(0.5f, 0.0f);
        descriptionRect.anchorMax = new Vector2(0.5f, 0.0f);
        descriptionRect.anchoredPosition = new Vector2(0.0f, 90.0f);
        descriptionRect.sizeDelta = new Vector2(280.0f, 150.0f);
    }


    void ApplyCardAccentStyle(CardPresentationSet.Entry presentationEntry)
    {
        Color accent = presentationEntry != null
            ? presentationEntry.AccentColor
            : new Color(1.0f, 0.86f, 0.22f, 0.72f);

        Color.RGBToHSV(accent, out _, out float saturation, out _);
        if (saturation < 0.08f)
        {
            _backgroundImage.color = Color.white;
            _cardNameText.color = Color.white;
            return;
        }

        Color backgroundColor = Color.Lerp(DefaultCardBackground, accent, CardAccentBackgroundBlend);
        backgroundColor.a = DefaultCardBackground.a;
        _backgroundImage.color = Color.white;

        Color titleColor = Color.Lerp(Color.white, accent, CardAccentTitleBlend);
        titleColor.a = 1.0f;
        _cardNameText.color = titleColor;
    }

    static Color ResolveCardAccentColor(CardPresentationSet.Entry entry)
    {
        return entry != null ? entry.AccentColor : new Color(1.0f, 0.86f, 0.22f, 0.72f);
    }

    public void ApplySelectionVisual(bool selected, bool faded)
    {
        if (!ValidateAuthoredReferences())
            return;

        transform.localScale = selected ? Vector3.one * 1.15f : Vector3.one;
        _canvasGroup.alpha = faded ? 0.22f : 1.0f;
        _canvasGroup.blocksRaycasts = !faded;
        _canvasGroup.interactable = !faded;
    }

    void SetHighlightFrame(bool active, bool recommended, Color accentColor)
    {
        if (!ValidateAuthoredReferences())
            return;

        _highlightOutline.enabled = active;
        _highlightOutline.effectColor = recommended ? RecommendedGold : accentColor;
        _highlightOutline.effectDistance = recommended ? new Vector2(8.0f, -8.0f) : new Vector2(3.0f, -3.0f);
    }

    void ApplyCompanionPresentation(SkillCardPresentationModel presentation)
    {
        _combatTraitRoot.SetActive(false);
        _collectionProgressRoot.SetActive(presentation.IsCompanion);
        if (!presentation.IsCompanion)
        {
            _combatTraitText.text = string.Empty;
            ResetCollectionVisuals();
            return;
        }

        _combatTraitText.text = string.Empty;
        _collectionPulseActive = presentation.PreviewCompanionIndex >= 0;
        _collectionPulseIndex = presentation.PreviewCompanionIndex;

        for (int i = 0; i < 3; i++)
        {
            bool owned = i < presentation.OwnedCompanionCount;
            bool preview = i == _collectionPulseIndex;
            _collectionFilledPips[i].SetActive(owned || preview);
            _collectionEmptyPips[i].SetActive(!owned && !preview);
            SetCollectionFilledColor(i, 1.0f);
        }
    }

    void Update()
    {
        if (_collectionPulseActive == false
            || _collectionPulseIndex < 0
            || _collectionFilledImages == null
            || _collectionPulseIndex >= _collectionFilledImages.Length)
        {
            return;
        }

        float pulse = (Mathf.Sin(Time.unscaledTime * CollectionPulseSpeed) + 1.0f) * 0.5f;
        SetCollectionFilledColor(_collectionPulseIndex, Mathf.Lerp(CollectionPulseMinimumAlpha, 1.0f, pulse));
    }

    void SetCollectionFilledColor(int index, float alpha)
    {
        if (_collectionFilledImages == null
            || index < 0
            || index >= _collectionFilledImages.Length
            || _collectionFilledImages[index] == null)
        {
            return;
        }

        Color color = CompanionCollectionGold;
        color.a = alpha;
        _collectionFilledImages[index].color = color;
    }

    void ResetCollectionVisuals()
    {
        _collectionPulseActive = false;
        _collectionPulseIndex = -1;

        if (_collectionFilledPips == null || _collectionEmptyPips == null)
            return;

        for (int i = 0; i < 3; i++)
        {
            if (_collectionFilledPips[i] != null)
                _collectionFilledPips[i].SetActive(false);

            if (_collectionEmptyPips[i] != null)
                _collectionEmptyPips[i].SetActive(true);

            SetCollectionFilledColor(i, 1.0f);
        }
    }

    void OnDisable()
    {
        ResetCollectionVisuals();
    }



    public void OnClickItem()
    {
        if (_selectionRequested == null)
        {
            Debug.LogError("[UI_SkillCardItem] Selection callback is not configured.", this);
            return;
        }

        _selectionRequested.Invoke(this, _cardData);
    }

#if UNITY_EDITOR
    public bool EditorAutomationCanSelect =>
        isActiveAndEnabled &&
        _clickButton != null &&
        _clickButton.interactable;
#endif
}
