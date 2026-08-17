using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lizzo.PV.Gameplay.CardOffer
{
    public readonly struct GameplayCardOfferItemPresentation
    {
        public GameplayCardOfferItemPresentation(
            string cardId,
            string title,
            string description,
            string value,
            string status,
            Sprite portrait,
            string relation,
            bool showProgress,
            int progressCount,
            bool recommended)
        {
            CardId = cardId ?? string.Empty;
            Title = title ?? string.Empty;
            Description = description ?? string.Empty;
            Value = value ?? string.Empty;
            Status = status ?? string.Empty;
            Portrait = portrait;
            Relation = relation ?? string.Empty;
            ShowProgress = showProgress;
            ProgressCount = progressCount;
            Recommended = recommended;
        }

        public string CardId { get; }
        public string Title { get; }
        public string Description { get; }
        public string Value { get; }
        public string Status { get; }
        public Sprite Portrait { get; }
        public string Relation { get; }
        public bool ShowProgress { get; }
        public int ProgressCount { get; }
        public bool Recommended { get; }
    }

    [DisallowMultipleComponent]
    public sealed class GameplayCardOfferItemView : MonoBehaviour
    {
        private const int ProgressSlotCount = 3;
        private const float DisabledAlpha = 0.22f;
        private const float GeneratedIconScale = 1.5f;
        private const string GeneratedIconTextureName = "card_icons_sheet_v2";

        [SerializeField]
        private int _slotIndex;

        [SerializeField]
        private Button _button;

        [SerializeField]
        private CanvasGroup _canvasGroup;

        [SerializeField]
        private RectTransform _visual;

        [SerializeField]
        private RectTransform _content;

        [SerializeField]
        private GameObject _statusBadge;

        [SerializeField]
        private TMP_Text _statusText;

        [SerializeField]
        private Image _portrait;

        [SerializeField]
        private TMP_Text _titleText;

        [SerializeField]
        private TMP_Text _descriptionText;

        [SerializeField]
        private TMP_Text _valueText;

        [SerializeField]
        private GameObject _relationSynergy;

        [SerializeField]
        private TMP_Text _relationLabelText;

        [SerializeField]
        private GameObject[] _progressSlotRoots;

        [SerializeField]
        private GameObject[] _progressOffVisuals;

        [SerializeField]
        private GameObject[] _progressOnVisuals;

        [SerializeField]
        private GameObject _selectedState;

        [SerializeField]
        private GameObject _disabledState;

        [SerializeField]
        private GameObject _recommendedState;

        private bool _isConfigured;
        private bool _isPresented;
        private bool _isSelected;
        private bool _isDisabled;
        private bool _isRecommended;

        public event Action<int> SelectionRequested;

        public void Present(string cardId)
        {
            _isPresented = string.IsNullOrWhiteSpace(cardId) == false;
            _isSelected = false;
            _isDisabled = false;
            _isRecommended = false;
            ApplyInteractionState();
        }

        public bool Present(GameplayCardOfferItemPresentation presentation)
        {
            if (string.IsNullOrWhiteSpace(presentation.CardId))
            {
                Debug.LogError("[GameplayCardOfferItemView] A non-empty card ID is required.", this);
                return false;
            }

            if (_isConfigured == false && Configure() == false)
                return false;

            _titleText.text = presentation.Title;
            _descriptionText.text = presentation.Description;
            _valueText.text = presentation.Value;
            _statusText.text = presentation.Status;
            _statusBadge.SetActive(string.IsNullOrEmpty(presentation.Status) == false);
            _portrait.sprite = presentation.Portrait;
            _portrait.enabled = presentation.Portrait != null;
            _portrait.preserveAspect = true;
            _portrait.rectTransform.localScale = IsGeneratedIcon(presentation.Portrait)
                ? Vector3.one * GeneratedIconScale
                : Vector3.one;
            _relationLabelText.text = presentation.Relation;
            _relationSynergy.SetActive(string.IsNullOrEmpty(presentation.Relation) == false);
            ApplyProgress(presentation.ShowProgress, presentation.ProgressCount);

            _isPresented = true;
            _isSelected = false;
            _isDisabled = false;
            _isRecommended = presentation.Recommended;
            ApplyInteractionState();
            return true;
        }

        public void Clear()
        {
            ClearContent();
            _isPresented = false;
            _isSelected = false;
            _isDisabled = false;
            _isRecommended = false;
            ApplyInteractionState();
        }

        public void SetState(bool selected, bool disabled, bool recommended)
        {
            _isSelected = selected;
            _isDisabled = disabled;
            _isRecommended = recommended;
            ApplyInteractionState();
        }

        public bool Configure()
        {
            if (_button == null
                || _canvasGroup == null
                || _visual == null
                || _content == null
                || _statusBadge == null
                || _statusText == null
                || _portrait == null
                || _titleText == null
                || _descriptionText == null
                || _valueText == null
                || _relationSynergy == null
                || _relationLabelText == null
                || HasThreeEntries(_progressSlotRoots) == false
                || HasThreeEntries(_progressOffVisuals) == false
                || HasThreeEntries(_progressOnVisuals) == false
                || _selectedState == null
                || _disabledState == null
                || _recommendedState == null)
            {
                Debug.LogError("[GameplayCardOfferItemView] Authored interaction, content, progress, and state references are required.", this);
                return false;
            }

            _button.onClick.RemoveListener(RequestSelection);
            _button.onClick.AddListener(RequestSelection);
            ApplyCenteredTextAlignment();
            _isConfigured = true;
            ApplyInteractionState();
            return true;
        }

        private static bool IsGeneratedIcon(Sprite portrait)
        {
            return portrait != null
                && portrait.texture != null
                && string.Equals(
                    portrait.texture.name,
                    GeneratedIconTextureName,
                    StringComparison.Ordinal);
        }

        private void ApplyCenteredTextAlignment()
        {
            _titleText.alignment = TextAlignmentOptions.Center;
            _descriptionText.alignment = TextAlignmentOptions.Center;
            _valueText.alignment = TextAlignmentOptions.Center;
            _statusText.alignment = TextAlignmentOptions.Center;
            _relationLabelText.alignment = TextAlignmentOptions.Center;
        }

        private void Awake()
        {
            Configure();
        }

        private void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(RequestSelection);
        }

        private void RequestSelection()
        {
            if (_isPresented == false || _isDisabled)
                return;

            SelectionRequested?.Invoke(_slotIndex);
        }

        private void ApplyInteractionState()
        {
            if (_button == null || _canvasGroup == null)
                return;

            bool interactable = _isPresented && _isDisabled == false;
            _button.interactable = interactable;
            _canvasGroup.interactable = interactable;
            _canvasGroup.blocksRaycasts = interactable;
            _canvasGroup.alpha = _isDisabled ? DisabledAlpha : 1f;

            if (_selectedState != null)
                _selectedState.SetActive(_isSelected);
            if (_disabledState != null)
                _disabledState.SetActive(_isDisabled);
            if (_recommendedState != null)
                _recommendedState.SetActive(_isRecommended);
        }

        private void ApplyProgress(bool visible, int progressCount)
        {
            int clampedCount = Mathf.Clamp(progressCount, 0, ProgressSlotCount);
            for (int index = 0; index < ProgressSlotCount; index++)
            {
                bool filled = visible && index < clampedCount;
                _progressSlotRoots[index].SetActive(visible);
                _progressOffVisuals[index].SetActive(visible && filled == false);
                _progressOnVisuals[index].SetActive(filled);
            }
        }

        private void ClearContent()
        {
            if (_titleText != null)
                _titleText.text = string.Empty;
            if (_descriptionText != null)
                _descriptionText.text = string.Empty;
            if (_valueText != null)
                _valueText.text = string.Empty;
            if (_statusText != null)
                _statusText.text = string.Empty;
            if (_statusBadge != null)
                _statusBadge.SetActive(false);
            if (_portrait != null)
            {
                _portrait.sprite = null;
                _portrait.enabled = false;
            }
            if (_relationLabelText != null)
                _relationLabelText.text = string.Empty;
            if (_relationSynergy != null)
                _relationSynergy.SetActive(false);

            if (HasThreeEntries(_progressSlotRoots)
                && HasThreeEntries(_progressOffVisuals)
                && HasThreeEntries(_progressOnVisuals))
            {
                ApplyProgress(false, 0);
            }
        }

        private static bool HasThreeEntries(GameObject[] items)
        {
            if (items == null || items.Length != ProgressSlotCount)
                return false;

            for (int index = 0; index < items.Length; index++)
            {
                if (items[index] == null)
                    return false;
            }

            return true;
        }
    }
}
