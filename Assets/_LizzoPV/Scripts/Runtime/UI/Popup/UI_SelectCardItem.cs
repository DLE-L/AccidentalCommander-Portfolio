using System;
using Lizzo.PV.P0.Cards;
using Lizzo.PV.P0.Presentation;
using Lizzo.PV.Legion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lizzo.PV.UI
{
    public sealed class UI_SelectCardItem : UI_Base
    {
        private const int ProgressDiamondCount = 3;
        private const float CollectionPulseSpeed = 5.0f;
        private const float CollectionPulseMinimumAlpha = 0.3f;

        [Header("Interaction")]
        [SerializeField] private Button _clickButton;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Image _rootGraphic;

        [Header("Card Content")]
        [SerializeField] private TMP_Text _cardNameText;
        [SerializeField] private TMP_Text _statusText;
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _descriptionText;
        [SerializeField] private TMP_Text _valueText;

        [Header("ProgressSlots")]
        [SerializeField] private GameObject[] _progressSlotRoots;
        [SerializeField] private GameObject[] _progressOffVisuals;
        [SerializeField] private GameObject[] _progressOnVisuals;

        [Header("RelationSynergy")]
        [SerializeField] private GameObject _relationSynergy;
        [SerializeField] private GameObject _relationVisual;
        [SerializeField] private TMP_Text _relationLabelText;
        [SerializeField] private Transform _iconList;

        [Header("StateVisuals")]
        [SerializeField] private GameObject _selectedState;
        [SerializeField] private GameObject _disabledState;
        [SerializeField] private GameObject _recommendedState;

        private CardData _cardData;
        private Action<UI_SelectCardItem, CardData> _selectionRequested;
        private PartyService _party;
        private Sprite _authoredIconSprite;
        private bool _authoredIconEnabled;
        private bool _authoredIconPreserveAspect;
        private Image.Type _authoredIconType;
        private Color _authoredIconColor;
        private bool _authoredIconRaycastTarget;
        private bool _authoredIconStateCached;
        private Image[] _progressOnImages = new Image[ProgressDiamondCount];
        private readonly Color[] _progressOnBaseColors = new Color[ProgressDiamondCount];
        private bool _collectionPulseActive;
        private int _collectionPulseIndex = -1;

        public bool Configure(Action<UI_SelectCardItem, CardData> selectionRequested, PartyService party)
        {
            _selectionRequested = selectionRequested;
            _party = party;
            if (_party == null)
            {
                Debug.LogError("[UI_SelectCardItem] PartyService is required.", this);
                return false;
            }

            return ValidateAuthoredReferences();
        }

        public override bool Init()
        {
            if (base.Init() == false)
                return false;

            if (ValidateAuthoredReferences() == false)
                return false;

            CacheAuthoredIconState();
            EnsureCardClickTarget();
            RefreshUI();
            return true;
        }

        public void SetInfo(CardData cardData)
        {
            _cardData = cardData;
            RefreshUI();
        }

        public void ApplySelectionVisual(bool selected, bool faded)
        {
            if (ValidateAuthoredReferences() == false)
                return;

            transform.localScale = selected ? Vector3.one * 1.15f : Vector3.one;
            _canvasGroup.alpha = faded ? 0.22f : 1.0f;
            _canvasGroup.blocksRaycasts = !faded;
            _canvasGroup.interactable = !faded;
            _clickButton.interactable = !faded;
            _selectedState.SetActive(selected);
            _disabledState.SetActive(faded);
        }

        public void OnClickItem()
        {
            if (_selectionRequested == null)
            {
                Debug.LogError("[UI_SelectCardItem] Selection callback is not configured.", this);
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

        private void RefreshUI()
        {
            if (_init == false)
                return;

            if (ValidateAuthoredReferences() == false)
                return;

            SkillCardPresentationModel presentation = SkillCardPresentationResolver.Resolve(_cardData, _party);
            _cardNameText.text = _cardData.Title ?? string.Empty;
            _descriptionText.text = presentation.Description;
            _valueText.text = CardPresentation.GetEffectText(_cardData) ?? string.Empty;
            _statusText.text = presentation.StatusText;
            _statusText.gameObject.SetActive(presentation.HasStatus);
            _recommendedState.SetActive(presentation.Recommended || presentation.HighlightFrame);
            _relationSynergy.SetActive(false);
            _relationLabelText.text = string.Empty;
            ApplyCardIcon(presentation.CatalogEntry);
            ApplyProgress(presentation);
            ApplySelectionVisual(selected: false, faded: false);
        }

        private bool ValidateAuthoredReferences()
        {
            if (_clickButton == null
                || _canvasGroup == null
                || _rootGraphic == null
                || _cardNameText == null
                || _statusText == null
                || _icon == null
                || _descriptionText == null
                || _valueText == null
                || _progressSlotRoots == null
                || _progressSlotRoots.Length != ProgressDiamondCount
                || _progressOffVisuals == null
                || _progressOffVisuals.Length != ProgressDiamondCount
                || _progressOnVisuals == null
                || _progressOnVisuals.Length != ProgressDiamondCount
                || _relationSynergy == null
                || _relationVisual == null
                || _relationLabelText == null
                || _iconList == null
                || _selectedState == null
                || _disabledState == null
                || _recommendedState == null)
            {
                Debug.LogError("[UI_SelectCardItem] Required authored semantic references are missing or ProgressSlots is not three entries.", this);
                return false;
            }

            for (int i = 0; i < ProgressDiamondCount; i++)
            {
                if (_progressSlotRoots[i] == null || _progressOffVisuals[i] == null || _progressOnVisuals[i] == null)
                {
                    Debug.LogError("[UI_SelectCardItem] Every ProgressSlots entry requires Slot, Off, and On references.", this);
                    return false;
                }

                if (_progressOnImages[i] == null)
                    _progressOnImages[i] = _progressOnVisuals[i].GetComponentInChildren<Image>(true);
                if (_progressOnImages[i] == null)
                {
                    Debug.LogError("[UI_SelectCardItem] Every ProgressSlots On reference requires an authored Image visual.", this);
                    return false;
                }

                if (_progressOnBaseColors[i].a <= 0.0f)
                    _progressOnBaseColors[i] = _progressOnImages[i].color;
            }

            return true;
        }

        private void EnsureCardClickTarget()
        {
            Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
                graphics[i].raycastTarget = graphics[i] == _rootGraphic;

            _rootGraphic.raycastTarget = true;
            _clickButton.onClick.RemoveListener(OnClickItem);
            _clickButton.onClick.AddListener(OnClickItem);
            _clickButton.interactable = true;
            _clickButton.targetGraphic = _rootGraphic;
        }

        private void OnValidate()
        {
            if (_rootGraphic == null)
                return;

            Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
                graphics[i].raycastTarget = graphics[i] == _rootGraphic;
        }

        private void ApplyCardIcon(CardPresentationSet.Entry presentationEntry)
        {
            CacheAuthoredIconState();

            bool hasRuntimeIcon = presentationEntry != null && presentationEntry.Icon != null;
            if (hasRuntimeIcon)
            {
                _icon.enabled = true;
                _icon.sprite = presentationEntry.Icon;
                _icon.preserveAspect = true;
                _icon.raycastTarget = false;
            }
            else
            {
                _icon.enabled = _authoredIconEnabled;
                _icon.sprite = _authoredIconSprite;
                _icon.preserveAspect = _authoredIconPreserveAspect;
                _icon.type = _authoredIconType;
                _icon.color = _authoredIconColor;
                _icon.raycastTarget = false;
            }
        }

        private void CacheAuthoredIconState()
        {
            if (_authoredIconStateCached || _icon == null)
                return;

            _authoredIconSprite = _icon.sprite;
            _authoredIconEnabled = _icon.enabled;
            _authoredIconPreserveAspect = _icon.preserveAspect;
            _authoredIconType = _icon.type;
            _authoredIconColor = _icon.color;
            _authoredIconRaycastTarget = _icon.raycastTarget;
            _authoredIconStateCached = true;
        }

        private void ApplyProgress(SkillCardPresentationModel presentation)
        {
            bool visible = presentation.IsCompanion || presentation.IsPassive;
            int ownedCount = presentation.IsCompanion
                ? Mathf.Clamp(presentation.OwnedCompanionCount, 0, ProgressDiamondCount)
                : Mathf.Clamp(presentation.OwnedPassiveCount, 0, ProgressDiamondCount);
            int previewIndex = presentation.IsCompanion
                ? presentation.PreviewCompanionIndex
                : presentation.PreviewPassiveIndex;
            _collectionPulseActive = visible && previewIndex >= 0 && previewIndex < ProgressDiamondCount;
            _collectionPulseIndex = _collectionPulseActive ? previewIndex : -1;

            for (int i = 0; i < ProgressDiamondCount; i++)
            {
                bool used = visible && i < ownedCount;
                bool preview = visible && i == previewIndex;

                _progressSlotRoots[i].SetActive(visible);
                _progressOffVisuals[i].SetActive(visible && !used && !preview);
                _progressOnVisuals[i].SetActive(visible && (used || preview));
                SetProgressOnColor(i, 1.0f);
            }
        }

        private void Update()
        {
            if (_collectionPulseActive == false
                || _collectionPulseIndex < 0
                || _collectionPulseIndex >= _progressOnImages.Length)
            {
                return;
            }

            float pulse = (Mathf.Sin(Time.unscaledTime * CollectionPulseSpeed) + 1.0f) * 0.5f;
            SetProgressOnColor(_collectionPulseIndex, Mathf.Lerp(CollectionPulseMinimumAlpha, 1.0f, pulse));
        }

        private void SetProgressOnColor(int index, float alpha)
        {
            if (_progressOnImages == null
                || index < 0
                || index >= _progressOnImages.Length
                || _progressOnImages[index] == null)
            {
                return;
            }

            Color color = _progressOnBaseColors[index];
            color.a = _progressOnBaseColors[index].a * alpha;
            _progressOnImages[index].color = color;
        }

        private void OnDisable()
        {
            _collectionPulseActive = false;
            _collectionPulseIndex = -1;
        }

        private void OnDestroy()
        {
            if (_clickButton != null)
                _clickButton.onClick.RemoveListener(OnClickItem);
        }
    }
}
