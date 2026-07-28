using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lizzo.PV.UI
{
    public sealed class UI_PauseOverlay : MonoBehaviour
    {
        private const int MaxCompanionEntries = 7;
        private const int MaxPassiveEntries = 5;

        [SerializeField] private GameObject _root;
        [SerializeField] private CanvasGroup _rootCanvasGroup;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _companionCountText;
        [SerializeField] private TMP_Text _passiveCountText;
        [SerializeField] private GameObject[] _companionSlotRoots;
        [SerializeField] private GameObject[] _companionFilledSlotRoots;
        [SerializeField] private GameObject[] _companionEmptySlotRoots;
        [SerializeField] private TMP_Text[] _companionSlotCountTexts;
        [SerializeField] private Image[] _companionIconImages;
        [SerializeField] private GameObject[] _passiveSlotRoots;
        [SerializeField] private GameObject[] _passiveFilledSlotRoots;
        [SerializeField] private GameObject[] _passiveEmptySlotRoots;
        [SerializeField] private Image[] _passiveIconImages;
        [SerializeField] private ScrollRect _infoScroll;
        [SerializeField] private RectTransform _synergyList;
        [SerializeField] private GameObject _synergyItemPrefab;
        [SerializeField] private TMP_Text _synergyEmptyStateText;
        [SerializeField] private Button _lobbyButton;
        [SerializeField] private Button _resumeButton;

        private Action _resumeRequested;
        private Action _lobbyRequested;
        private readonly List<UI_PauseSynergyItem> _synergyItems = new List<UI_PauseSynergyItem>(4);
        private bool _isInitialized;

        public bool Configure(Action resumeRequested, Action lobbyRequested)
        {
            _resumeRequested = resumeRequested;
            _lobbyRequested = lobbyRequested;

            if (_isInitialized)
                BindListeners();

            return resumeRequested != null && lobbyRequested != null;
        }

        public bool Init()
        {
            if (_isInitialized)
                return true;

            if (!Validate())
                return false;

            _rootCanvasGroup.alpha = 0f;
            _rootCanvasGroup.interactable = false;
            _rootCanvasGroup.blocksRaycasts = false;
            _root.SetActive(false);
            BindListeners();
            _isInitialized = true;
            return true;
        }

        public bool Validate()
        {
            if (_root == null
                || _rootCanvasGroup == null
                || _titleText == null
                || _companionCountText == null
                || _passiveCountText == null
                || _companionSlotRoots == null
                || _companionSlotRoots.Length != MaxCompanionEntries
                || _companionFilledSlotRoots == null
                || _companionFilledSlotRoots.Length != MaxCompanionEntries
                || _companionEmptySlotRoots == null
                || _companionEmptySlotRoots.Length != MaxCompanionEntries
                || _companionSlotCountTexts == null
                || _companionSlotCountTexts.Length != MaxCompanionEntries
                || _companionIconImages == null
                || _companionIconImages.Length != MaxCompanionEntries
                || _passiveSlotRoots == null
                || _passiveSlotRoots.Length != MaxPassiveEntries
                || _passiveFilledSlotRoots == null
                || _passiveFilledSlotRoots.Length != MaxPassiveEntries
                || _passiveEmptySlotRoots == null
                || _passiveEmptySlotRoots.Length != MaxPassiveEntries
                || _passiveIconImages == null
                || _passiveIconImages.Length != MaxPassiveEntries
                || _infoScroll == null
                || _synergyList == null
                || _synergyItemPrefab == null
                || _synergyItemPrefab.GetComponent<UI_PauseSynergyItem>() == null
                || _synergyEmptyStateText == null
                || _lobbyButton == null
                || _resumeButton == null)
            {
                Debug.LogError("[UI_PauseOverlay] Required authored references are incomplete or companion/passive arrays are not exactly 7/5 entries.", this);
                return false;
            }

            for (int i = 0; i < _companionSlotRoots.Length; i++)
            {
                if (_companionSlotRoots[i] == null
                    || _companionFilledSlotRoots[i] == null
                    || _companionEmptySlotRoots[i] == null
                    || _companionSlotCountTexts[i] == null
                    || _companionIconImages[i] == null)
                {
                    Debug.LogError("[UI_PauseOverlay] Every companion slot requires an explicit root and Icon Image reference.", this);
                    return false;
                }
            }

            for (int i = 0; i < _passiveSlotRoots.Length; i++)
            {
                if (_passiveSlotRoots[i] == null
                    || _passiveFilledSlotRoots[i] == null
                    || _passiveEmptySlotRoots[i] == null
                    || _passiveIconImages[i] == null)
                {
                    Debug.LogError("[UI_PauseOverlay] Every passive slot requires an explicit root and Icon Image reference.", this);
                    return false;
                }
            }

            return true;
        }

        public void Present(
            bool fromAppBackground,
            IReadOnlyList<PauseCompanionPresentation> companionPresentations,
            IReadOnlyList<PausePassivePresentation> passivePresentations,
            IReadOnlyList<PauseSynergyPresentation> synergies)
        {
            if (!_isInitialized)
                return;

            _titleText.text = fromAppBackground ? "복귀 후 일시정지" : "일시정지";
            PresentCompanions(companionPresentations);
            PresentPassives(passivePresentations);
            PresentSynergies(synergies);
            _infoScroll.verticalNormalizedPosition = 1f;

            _root.SetActive(true);
            _rootCanvasGroup.alpha = 1f;
            _rootCanvasGroup.interactable = true;
            _rootCanvasGroup.blocksRaycasts = true;
        }

        public void Hide()
        {
            if (!_isInitialized)
                return;

            _rootCanvasGroup.alpha = 0f;
            _rootCanvasGroup.interactable = false;
            _rootCanvasGroup.blocksRaycasts = false;
            _root.SetActive(false);
        }

        private void PresentSynergies(IReadOnlyList<PauseSynergyPresentation> synergies)
        {
            int count = synergies == null ? 0 : synergies.Count;
            _synergyEmptyStateText.gameObject.SetActive(count == 0);
            _synergyEmptyStateText.raycastTarget = false;
            if (count == 0)
                _synergyEmptyStateText.text = "활성 시너지 없음";

            for (int i = 0; i < count; i++)
            {
                UI_PauseSynergyItem item;
                if (i < _synergyItems.Count)
                {
                    item = _synergyItems[i];
                }
                else
                {
                    GameObject instance = Instantiate(_synergyItemPrefab, _synergyList, false);
                    item = instance.GetComponent<UI_PauseSynergyItem>();
                    if (item == null)
                    {
                        Debug.LogError("[UI_PauseOverlay] Synergy item prefab is missing UI_PauseSynergyItem.", instance);
                        instance.SetActive(false);
                        continue;
                    }

                    _synergyItems.Add(item);
                }

                if (!item.Present(synergies[i]))
                {
                    Debug.LogError("[UI_PauseOverlay] Synergy item presentation data is invalid.", item);
                    item.gameObject.SetActive(false);
                    continue;
                }

                item.gameObject.SetActive(true);
            }

            for (int i = count; i < _synergyItems.Count; i++)
            {
                _synergyItems[i].Clear();
                _synergyItems[i].gameObject.SetActive(false);
            }
        }

        private void PresentCompanions(IReadOnlyList<PauseCompanionPresentation> presentations)
        {
            int count = presentations == null ? 0 : Mathf.Min(MaxCompanionEntries, presentations.Count);
            int occupiedCount = 0;

            for (int i = 0; i < MaxCompanionEntries; i++)
            {
                PauseCompanionPresentation presentation = i < count ? presentations[i] : default;
                bool occupied = presentation.CurrentCount > 0;
                if (occupied)
                    occupiedCount++;

                _companionSlotRoots[i].SetActive(true);
                _companionFilledSlotRoots[i].SetActive(occupied);
                _companionEmptySlotRoots[i].SetActive(!occupied);
                bool hasIcon = occupied && presentation.Icon != null;
                _companionIconImages[i].enabled = hasIcon;
                _companionIconImages[i].sprite = hasIcon ? presentation.Icon : null;
                _companionIconImages[i].raycastTarget = false;
                _companionSlotCountTexts[i].gameObject.SetActive(occupied);
                _companionSlotCountTexts[i].text = occupied ? "×" + presentation.CurrentCount : string.Empty;
                _companionSlotCountTexts[i].raycastTarget = false;
            }

            _companionCountText.text = "동료 " + occupiedCount + " / " + MaxCompanionEntries;
        }

        private void PresentPassives(IReadOnlyList<PausePassivePresentation> presentations)
        {
            int count = presentations == null ? 0 : Mathf.Min(MaxPassiveEntries, presentations.Count);
            for (int i = 0; i < MaxPassiveEntries; i++)
            {
                PausePassivePresentation presentation = i < count ? presentations[i] : default;
                bool occupied = i < count;
                _passiveSlotRoots[i].SetActive(true);
                _passiveFilledSlotRoots[i].SetActive(occupied);
                _passiveEmptySlotRoots[i].SetActive(!occupied);
                bool hasIcon = occupied && presentation.Icon != null;
                _passiveIconImages[i].enabled = hasIcon;
                _passiveIconImages[i].sprite = hasIcon ? presentation.Icon : null;
                _passiveIconImages[i].raycastTarget = false;
            }

            _passiveCountText.text = "패시브 " + count + " / " + MaxPassiveEntries;
        }

        private void BindListeners()
        {
            _lobbyButton.onClick.RemoveListener(OnLobbyClicked);
            _resumeButton.onClick.RemoveListener(OnResumeClicked);
            _lobbyButton.onClick.AddListener(OnLobbyClicked);
            _resumeButton.onClick.AddListener(OnResumeClicked);
        }

        private void OnLobbyClicked()
        {
            _lobbyRequested?.Invoke();
        }

        private void OnResumeClicked()
        {
            _resumeRequested?.Invoke();
        }

        private void OnDestroy()
        {
            if (_lobbyButton != null)
                _lobbyButton.onClick.RemoveListener(OnLobbyClicked);
            if (_resumeButton != null)
                _resumeButton.onClick.RemoveListener(OnResumeClicked);

            _resumeRequested = null;
            _lobbyRequested = null;
            _synergyItems.Clear();
        }
    }
}
