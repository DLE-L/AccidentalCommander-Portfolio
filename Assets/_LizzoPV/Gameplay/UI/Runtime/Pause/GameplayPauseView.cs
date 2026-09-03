using System;
using System.Collections.Generic;
using Lizzo.PV.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Lizzo.PV.Gameplay.Pause
{
    [DisallowMultipleComponent]
    public sealed class GameplayPauseView : MonoBehaviour
    {
        private const int CompanionSlotCount = 7;
        private const int PassiveSlotCount = 5;
        private const int MaxSynergyEntries = 8;

        [SerializeField]
        private CanvasGroup _canvasGroup;

        [SerializeField]
        private RectTransform _modalInputBlocker;

        [SerializeField]
        private RectTransform _header;

        [SerializeField]
        private RectTransform _buildSummary;

        [SerializeField]
        private RectTransform _actions;

        [SerializeField]
        private TMP_Text _titleText;

        [SerializeField]
        private TMP_Text _companionCountText;

        [SerializeField]
        private TMP_Text _passiveCountText;

        [SerializeField]
        private GameplayPauseCompanionSlotView[] _companionSlots;

        [SerializeField]
        private GameplayPausePassiveSlotView[] _passiveSlots;

        [SerializeField]
        private RectTransform _completedSynergyList;

        [SerializeField]
        private GameplayPauseSynergyItemView _synergyItemPrefab;

        [SerializeField]
        private RectTransform _emptyState;

        [SerializeField]
        private TMP_Text _emptyStateText;

        [SerializeField]
        [FormerlySerializedAs("_lobbyButton")]
        private Button _abandonButton;

        [SerializeField]
        private Button _resumeButton;

        private Action _resumeRequested;
        private Action _abandonRequested;
        private readonly List<GameplayPauseSynergyItemView> _synergyItems = new List<GameplayPauseSynergyItemView>(MaxSynergyEntries);

        public bool Present(
            bool fromAppBackground,
            IReadOnlyList<PauseCompanionPresentation> companions,
            IReadOnlyList<PausePassivePresentation> passives,
            IReadOnlyList<PauseSynergyPresentation> synergies,
            Action resumeRequested,
            Action abandonRequested)
        {
            if (!Validate())
                return false;

            _resumeRequested = resumeRequested;
            _abandonRequested = abandonRequested;
            BindListeners();
            _titleText.text = "일시정지";
            PresentCompanions(companions);
            PresentPassives(passives);
            PresentSynergies(synergies);
            SetVisible(true);
            return true;
        }

        public void Hide()
        {
            if (_canvasGroup == null)
            {
                Debug.LogError("[GameplayPauseView] CanvasGroup is required.", this);
                return;
            }

            SetVisible(false);
        }

        private bool Validate()
        {
            if (_canvasGroup == null
                || _modalInputBlocker == null
                || _header == null
                || _buildSummary == null
                || _actions == null
                || _titleText == null
                || _companionCountText == null
                || _passiveCountText == null
                || _completedSynergyList == null
                || _synergyItemPrefab == null
                || _emptyState == null
                || _emptyStateText == null
                || _abandonButton == null
                || _resumeButton == null
                || !ValidateSlots(_companionSlots, CompanionSlotCount)
                || !ValidateSlots(_passiveSlots, PassiveSlotCount))
            {
                Debug.LogError("[GameplayPauseView] Required Pause references are incomplete.", this);
                return false;
            }

            return true;
        }

        private static bool ValidateSlots(GameplayPauseCompanionSlotView[] slots, int expectedCount)
        {
            if (slots == null || slots.Length != expectedCount)
                return false;

            for (int index = 0; index < slots.Length; index++)
            {
                if (slots[index] == null || !slots[index].Validate())
                    return false;
            }

            return true;
        }

        private static bool ValidateSlots(GameplayPausePassiveSlotView[] slots, int expectedCount)
        {
            if (slots == null || slots.Length != expectedCount)
                return false;

            for (int index = 0; index < slots.Length; index++)
            {
                if (slots[index] == null || !slots[index].Validate())
                    return false;
            }

            return true;
        }

        private void PresentCompanions(IReadOnlyList<PauseCompanionPresentation> presentations)
        {
            int sourceCount = presentations == null ? 0 : Mathf.Min(CompanionSlotCount, presentations.Count);
            int occupiedCount = 0;
            for (int index = 0; index < CompanionSlotCount; index++)
            {
                PauseCompanionPresentation presentation = index < sourceCount ? presentations[index] : default;
                if (presentation.CurrentCount > 0)
                    occupiedCount++;
                _companionSlots[index].Present(presentation);
            }

            _companionCountText.text = "동료 " + occupiedCount + " / " + CompanionSlotCount;
        }

        private void PresentPassives(IReadOnlyList<PausePassivePresentation> presentations)
        {
            int occupiedCount = presentations == null ? 0 : Mathf.Min(PassiveSlotCount, presentations.Count);
            for (int index = 0; index < PassiveSlotCount; index++)
            {
                bool occupied = index < occupiedCount;
                _passiveSlots[index].Present(occupied, occupied ? presentations[index] : default);
            }

            _passiveCountText.text = "패시브 " + occupiedCount + " / " + PassiveSlotCount;
        }

        private void PresentSynergies(IReadOnlyList<PauseSynergyPresentation> synergies)
        {
            int count = synergies == null ? 0 : Mathf.Min(MaxSynergyEntries, synergies.Count);
            _emptyState.gameObject.SetActive(count == 0);
            _emptyStateText.raycastTarget = false;
            if (count == 0)
                _emptyStateText.text = "활성 시너지 없음";

            for (int index = 0; index < count; index++)
            {
                GameplayPauseSynergyItemView item;
                if (index < _synergyItems.Count)
                {
                    item = _synergyItems[index];
                }
                else
                {
                    item = Instantiate(_synergyItemPrefab, _completedSynergyList, false);
                    _synergyItems.Add(item);
                }

                if (!item.Present(synergies[index]))
                {
                    item.Clear();
                    item.gameObject.SetActive(false);
                    continue;
                }

                item.gameObject.SetActive(true);
            }

            for (int index = count; index < _synergyItems.Count; index++)
            {
                _synergyItems[index].Clear();
                _synergyItems[index].gameObject.SetActive(false);
            }
        }

        private void BindListeners()
        {
            _abandonButton.onClick.RemoveListener(OnAbandonClicked);
            _resumeButton.onClick.RemoveListener(OnResumeClicked);
            _abandonButton.onClick.AddListener(OnAbandonClicked);
            _resumeButton.onClick.AddListener(OnResumeClicked);
        }

        private void SetVisible(bool visible)
        {
            _canvasGroup.alpha = visible ? 1f : 0f;
            _canvasGroup.interactable = visible;
            _canvasGroup.blocksRaycasts = visible;
        }

        private void OnAbandonClicked()
        {
            _abandonRequested?.Invoke();
        }

        private void OnResumeClicked()
        {
            _resumeRequested?.Invoke();
        }

        private void OnDestroy()
        {
            if (_abandonButton != null)
                _abandonButton.onClick.RemoveListener(OnAbandonClicked);
            if (_resumeButton != null)
                _resumeButton.onClick.RemoveListener(OnResumeClicked);

            _resumeRequested = null;
            _abandonRequested = null;
            _synergyItems.Clear();
        }
    }
}
