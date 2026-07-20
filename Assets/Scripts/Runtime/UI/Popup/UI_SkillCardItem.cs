using Lizzo.PV.P0.Cards;
using Lizzo.PV.Legion;
using Lizzo.PV.P0.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_SkillCardItem : UI_Base
{
    const int RecommendedShieldCaptainLevelUp = 3;
    const float CardAccentBackgroundBlend = 0.3f;
    const float CardAccentTitleBlend = 0.38f;
    static readonly Color DefaultCardBackground = new Color(0.12f, 0.17f, 0.24f, 0.98f);
    static readonly Color RecommendedGold = new Color(1.0f, 0.78f, 0.12f, 0.95f);

    [Header("Interaction")]
    [SerializeField] Button _clickButton;
    [SerializeField] CanvasGroup _canvasGroup;
    [SerializeField] Outline _highlightOutline;
    [SerializeField] Image _backgroundImage;

    [Header("Content")]
    [SerializeField] TMP_Text _cardNameText;
    [SerializeField] TMP_Text _skillDescriptionText;
    [SerializeField] Image _skillIcon;
    [SerializeField] GameObject _skillLevelGroup;

    [Header("State Labels")]
    [SerializeField] GameObject _newIndicatorRoot;
    [SerializeField] TMP_Text _newText;
    [SerializeField] GameObject _evolutionInfoRoot;
    [SerializeField] TMP_Text _evolutionText;

    CardData _cardData;
    System.Action<UI_SkillCardItem, CardData> _selectionRequested;
    PartyService _party;

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
            || _skillLevelGroup == null
            || _newIndicatorRoot == null
            || _newText == null
            || _evolutionInfoRoot == null
            || _evolutionText == null)
        {
            Debug.LogError("[UI_SkillCardItem] All authored interaction, content, and state-label references are required.", this);
            return false;
        }

        return true;
    }


    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        if (!ValidateAuthoredReferences())
            return false;

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

        CardPresentationSet.Entry presentationEntry = ResolveCardPresentationEntry(_cardData);
        ApplyCardTextStyle();
        ApplyCardAccentStyle(presentationEntry);
        ApplyCardIcon(presentationEntry);
        ApplySelectionVisual(selected: false, faded: false);

        _cardNameText.text = _cardData.Title ?? string.Empty;
        _skillDescriptionText.text = BuildDescription(_cardData);

        bool isNew = _cardData.Highlight == CardHighlight.New;
        bool isPromotionReady = _cardData.Highlight == CardHighlight.PromotionReady;
        bool isSynergyOneMore = _cardData.Highlight == CardHighlight.SynergyOneMore;
        bool isRecommendedShieldCaptain = IsRecommendedShieldCaptainPromotion();

        _newIndicatorRoot.SetActive(isNew || isRecommendedShieldCaptain);
        _evolutionInfoRoot.SetActive(isPromotionReady || isSynergyOneMore);
        SetHighlightFrame(isPromotionReady || isSynergyOneMore, isRecommendedShieldCaptain);

        _newText.text = isRecommendedShieldCaptain ? "추천" : isNew ? "신규" : string.Empty;
        _evolutionText.text = isRecommendedShieldCaptain ? "선택 시 방패대장 진급!" : isPromotionReady ? "선택 시 진급!" : isSynergyOneMore ? "선택 시 근위대 결성!" : string.Empty;
    }

    bool IsRecommendedShieldCaptainPromotion()
    {
        return FixedCardPool.CurrentLevelUpCount == RecommendedShieldCaptainLevelUp
            && _cardData.Kind == CardKind.AddShieldSoldier
            && _cardData.Highlight == CardHighlight.PromotionReady;
    }

    void EnsureCardClickTarget()
    {
        if (!ValidateAuthoredReferences())
            return;

        _backgroundImage.raycastTarget = true;
        _clickButton.interactable = true;
        _clickButton.targetGraphic = _backgroundImage;
    }

    void ApplyCardTextStyle()
    {
        _skillLevelGroup.SetActive(false);

        ConfigureText(_cardNameText, 30.0f, 21.0f, TextAlignmentOptions.Center);
        ConfigureText(_skillDescriptionText, 18.0f, 13.0f, TextAlignmentOptions.TopLeft);
        ConfigureText(_newText, 21.0f, 16.0f, TextAlignmentOptions.Center);
        ConfigureText(_evolutionText, 21.0f, 16.0f, TextAlignmentOptions.Center);

        _skillDescriptionText.lineSpacing = -4.0f;
        _skillDescriptionText.margin = new Vector4(6.0f, 1.0f, 6.0f, 1.0f);
    }

    void ApplyCardIcon(CardPresentationSet.Entry presentationEntry)
    {
        if (presentationEntry == null || presentationEntry.Icon == null)
        {
            _skillIcon.enabled = false;
            return;
        }

        _skillIcon.enabled = true;
        _skillIcon.sprite = presentationEntry.Icon;
        _skillIcon.preserveAspect = true;
        _skillIcon.raycastTarget = false;
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

    CardPresentationSet.Entry ResolveCardPresentationEntry(CardData cardData)
    {
        if (cardData.Highlight == CardHighlight.SynergyOneMore
            && PresentationCatalogProvider.TryGetCard("synergy_complete", out CardPresentationSet.Entry synergyEntry))
        {
            return synergyEntry;
        }

        if (cardData.Highlight == CardHighlight.PromotionReady
            && PresentationCatalogProvider.TryGetCard("promotion", out CardPresentationSet.Entry promotionEntry))
        {
            return promotionEntry;
        }

        return PresentationCatalogProvider.TryGetCard(cardData.Kind.ToString(), out CardPresentationSet.Entry entry)
            ? entry
            : null;
    }

    Color ResolveCardAccentColor()
    {
        CardPresentationSet.Entry entry = ResolveCardPresentationEntry(_cardData);
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

    void SetHighlightFrame(bool active, bool recommended)
    {
        if (!ValidateAuthoredReferences())
            return;

        _highlightOutline.enabled = active;
        _highlightOutline.effectColor = recommended ? RecommendedGold : ResolveCardAccentColor();
        _highlightOutline.effectDistance = recommended ? new Vector2(8.0f, -8.0f) : new Vector2(3.0f, -3.0f);
    }

    static void ConfigureText(TMP_Text text, float fontSize, float minSize, TextAlignmentOptions alignment)
    {
        if (text == null)
            return;

        text.fontSize = fontSize;
        text.fontSizeMax = fontSize;
        text.fontSizeMin = minSize;
        text.enableAutoSizing = true;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.alignment = alignment;
        text.raycastTarget = false;
        text.richText = true;
    }

    string BuildDescription(CardData cardData)
    {
        string progressTag = ResolveProgressTag(cardData);
        string description =
            $"타입: {CardPresentation.GetTypeLabel(cardData)}\n" +
            $"대상: {CardPresentation.GetTargetLabel(cardData.Kind)}\n" +
            $"효과: {CardPresentation.GetEffectText(cardData)}";

        if (string.IsNullOrEmpty(progressTag) == false)
            description += $"\n{FormatProgressTag(progressTag)}";

        description += $"\n선택 시: {CardPresentation.GetSelectionText(cardData)}";
        return description;
    }

    string FormatProgressTag(string progressTag)
    {
        return $"<color=#F7C846><b>{progressTag}</b></color>";
    }

    string ResolveProgressTag(CardData cardData)
    {
        if (TryGetCompanionKind(cardData.Kind, out CompanionKind companionKind)
            && _party.TryGetSquadSlotForCompanion(companionKind, out SquadSlotState slotState))
        {
            int afterCount = _party.PreviewSquadSlotCountAfterRecruit(companionKind);
            string arrow = afterCount == slotState.CurrentCount
                ? $"{slotState.CurrentCount}/{slotState.MaxCount}"
                : $"{slotState.CurrentCount}/{slotState.MaxCount} -> {afterCount}/{slotState.MaxCount}";

            if (cardData.Highlight == CardHighlight.PromotionReady)
                return $"진행도: {slotState.DisplayName} {arrow} / 진급";

            return $"진행도: {slotState.DisplayName} {arrow}";
        }

        if (cardData.Highlight == CardHighlight.SynergyOneMore)
        {
            int beforeCount = CountGuardMaterials();
            return $"진행도: Guard Squad {beforeCount}/3 -> 3/3";
        }

        if (IsGuardMaterialCard(cardData.Kind))
        {
            int afterCount = CountGuardMaterialsAfter(cardData.Kind);
            return $"진행도: Guard Squad {afterCount}/3";
        }

        return string.Empty;
    }

    bool TryGetCompanionKind(CardKind kind, out CompanionKind companionKind)
    {
        switch (kind)
        {
            case CardKind.AddShieldSoldier:
                companionKind = CompanionKind.ShieldSoldier;
                return true;
            case CardKind.RecruitSwordsman:
                companionKind = CompanionKind.Swordsman;
                return true;
            case CardKind.RecruitCleric:
                companionKind = CompanionKind.Cleric;
                return true;
            case CardKind.RecruitArcher:
                companionKind = CompanionKind.Archer;
                return true;
            default:
                companionKind = default;
                return false;
        }
    }

    int CountGuardMaterials()
    {
        int count = 0;
        if (_party.ShieldSoldierCount > 0 || _party.ShieldCaptainCount > 0)
            count++;
        if (_party.SwordsmanCount > 0)
            count++;
        if (_party.ClericCount > 0)
            count++;

        return count;
    }

    int CountGuardMaterialsAfter(CardKind kind)
    {
        bool hasShield = _party.ShieldSoldierCount > 0 || _party.ShieldCaptainCount > 0;
        bool hasSword = _party.SwordsmanCount > 0;
        bool hasCleric = _party.ClericCount > 0;

        switch (kind)
        {
            case CardKind.AddShieldSoldier:
                hasShield = true;
                break;
            case CardKind.RecruitSwordsman:
                hasSword = true;
                break;
            case CardKind.RecruitCleric:
                hasCleric = true;
                break;
        }

        return (hasShield ? 1 : 0) + (hasSword ? 1 : 0) + (hasCleric ? 1 : 0);
    }

    bool IsGuardMaterialCard(CardKind kind)
    {
        return kind == CardKind.AddShieldSoldier
            || kind == CardKind.RecruitSwordsman
            || kind == CardKind.RecruitCleric;
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
