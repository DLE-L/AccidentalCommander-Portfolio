using System;
using TMPro;
using Lizzo.PV.Flow;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Lizzo.PV.UI
{
    public sealed class LobbyNavigationShell : MonoBehaviour
    {
        enum Destination
        {
            Shop,
            Legion,
            Battle,
            Relic,
            Trait,
        }

        sealed class NavigationBinding
        {
            public readonly Destination Destination;
            public readonly Button Button;
            public readonly UnityAction Click;

            public NavigationBinding(
                LobbyNavigationShell owner,
                Destination destination,
                Button button)
            {
                Destination = destination;
                Button = button;
                Click = () => owner.SetDestination(destination, false);
            }
        }

        [Header("Panels")]
        [SerializeField] RectTransform _battlePanel;
        [SerializeField] CanvasGroup _battlePanelCanvasGroup;
        [FormerlySerializedAs("_placeholderPanel")]
        [SerializeField] RectTransform _destinationPanel;
        [FormerlySerializedAs("_placeholderPanelCanvasGroup")]
        [SerializeField] CanvasGroup _destinationPanelCanvasGroup;

        [Header("Battle Home")]
        [SerializeField] TMP_Text _statusText;
        [SerializeField] TMP_Text _nextRunText;
        [SerializeField] Button _startBattleButton;
        [FormerlySerializedAs("_battleProgressionButton")]
        [SerializeField] Button _traitEntryButton;

        [Header("Placeholder")]
        [FormerlySerializedAs("_placeholderTitleText")]
        [SerializeField] TMP_Text _destinationTitleText;
        [FormerlySerializedAs("_placeholderBodyText")]
        [SerializeField] TMP_Text _destinationBodyText;
        [SerializeField] Button _backButton;
        [FormerlySerializedAs("_collectionDecksButton")]
        [SerializeField] Button _legionDecksButton;
        [FormerlySerializedAs("_collectionCardsButton")]
        [SerializeField] Button _legionCardsButton;
        [FormerlySerializedAs("_collectionTabs")]
        [SerializeField] GameObject _legionTabs;
        [FormerlySerializedAs("_defaultPlaceholderContent")]
        [SerializeField] GameObject _comingSoonContent;
        [SerializeField] GameObject _shopContent;
        [SerializeField] GameObject _legionContent;
        [SerializeField] GameObject _legionPassEntry;
        [FormerlySerializedAs("_placeholderScrollContent")]
        [SerializeField] RectTransform _destinationScrollContent;
        [FormerlySerializedAs("_placeholderScrollRect")]
        [SerializeField] ScrollRect _destinationScrollRect;
        [FormerlySerializedAs("_placeholderScrollViewport")]
        [SerializeField] RectTransform _destinationScrollViewport;
        [FormerlySerializedAs("_placeholderIcon")]
        [SerializeField] GameObject _comingSoonIcon;

        [Header("Bottom Navigation")]
        [SerializeField] Button _shopButton;
        [FormerlySerializedAs("_collectionButton")]
        [SerializeField] Button _legionButton;
        [SerializeField] Button _battleButton;
        [FormerlySerializedAs("_socialButton")]
        [SerializeField] Button _relicButton;
        [FormerlySerializedAs("_progressionNavButton")]
        [SerializeField] Button _traitButton;
        [SerializeField] LobbyBottomNavigationView _bottomNavigationView;

        [Header("Transition")]
        [SerializeField, Min(0.05f)] float _slideDuration = 0.16f;
        [SerializeField] float _slideDistance = 160f;
        [SerializeField] Color _selectedLabelColor = new(1f, 0.73f, 0.23f, 1f);
        [SerializeField] Color _defaultLabelColor = new(0.72f, 0.78f, 0.84f, 1f);

        Action _startBattleRequested;
        Destination _currentDestination = Destination.Battle;
        Destination _targetDestination = Destination.Battle;
        float _transitionElapsed;
        bool _isTransitioning;
        bool _isDestinationToDestinationTransition;
        bool _isDestinationContentPending;
        NavigationBinding[] _navigationBindings;

        public bool Configure()
        {
            if (_battlePanel == null ||
                _battlePanelCanvasGroup == null ||
                _destinationPanel == null ||
                _destinationPanelCanvasGroup == null ||
                _statusText == null ||
                _nextRunText == null ||
                _startBattleButton == null ||
                _traitEntryButton == null ||
                _destinationTitleText == null ||
                _destinationBodyText == null ||
                _backButton == null ||
                _legionDecksButton == null ||
                _legionCardsButton == null ||
                _legionTabs == null ||
                _comingSoonContent == null ||
                _shopContent == null ||
                _legionContent == null ||
                _legionPassEntry == null ||
                _destinationScrollContent == null ||
                _destinationScrollRect == null ||
                _destinationScrollViewport == null ||
                _comingSoonIcon == null ||
                _bottomNavigationView == null ||
                _shopButton == null || _legionButton == null || _battleButton == null ||
                _relicButton == null || _traitButton == null)
            {
                Debug.LogError("[LobbyNavigationShell] Authored lobby navigation references are required.", this);
                return false;
            }

            return EnsureNavigationBindings();
        }

        bool EnsureNavigationBindings()
        {
            if (_navigationBindings != null)
                return true;

            NavigationBinding[] bindings =
            {
                CreateNavigationBinding(Destination.Legion, _legionButton),
                CreateNavigationBinding(Destination.Battle, _battleButton),
                CreateNavigationBinding(Destination.Trait, _traitButton),
                CreateNavigationBinding(Destination.Relic, _relicButton),
                CreateNavigationBinding(Destination.Shop, _shopButton),
            };

            for (int i = 0; i < bindings.Length; i++)
            {
                if (bindings[i] != null)
                    continue;

                Debug.LogError("[LobbyNavigationShell] Bottom navigation visual bindings are incomplete.", this);
                return false;
            }

            _navigationBindings = bindings;
            return true;
        }

        NavigationBinding CreateNavigationBinding(
            Destination destination,
            Button button)
        {
            if (button == null)
                return null;

            return new NavigationBinding(this, destination, button);
        }

        public void Show(Action startBattleRequested)
        {
            if (Configure() == false)
                return;

            _startBattleRequested = startBattleRequested;
            BindButtons();
            _statusText.text = "INTERNAL TEST  •  BATTLE READY";
            _nextRunText.text = FirstRunProgress.ResolveNextBattleMode(forceNormal: false) == RunMode.Tutorial
                ? "NEXT RUN: TUTORIAL BATTLE"
                : "NEXT RUN: NORMAL BATTLE";
            SetDestination(Destination.Battle, true);
        }

        public void Hide()
        {
            ClearListeners();
            _startBattleRequested = null;
        }

        public bool TryHandleBack()
        {
            if (_isTransitioning)
                return true;

            if (_currentDestination == Destination.Battle)
                return false;

            ShowBattleHome();
            return true;
        }

        void OnDestroy()
        {
            ClearListeners();
        }

        void Update()
        {
            if (_isTransitioning == false)
                return;

            _transitionElapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(_transitionElapsed / _slideDuration);
            float easedProgress = Mathf.SmoothStep(0f, 1f, progress);

            if (_isDestinationToDestinationTransition)
            {
                UpdatePlaceholderToPlaceholderTransition(progress);
            }
            else
            {
                UpdateBattleAndPlaceholderTransition(easedProgress);
            }

            if (progress < 1f)
                return;

            _isTransitioning = false;
            _isDestinationToDestinationTransition = false;
            _currentDestination = _targetDestination;
            _battlePanel.gameObject.SetActive(_currentDestination == Destination.Battle);
            _destinationPanel.gameObject.SetActive(_currentDestination != Destination.Battle);
            ApplyPanelState(_battlePanel, _battlePanelCanvasGroup, _currentDestination == Destination.Battle);
            ApplyPanelState(_destinationPanel, _destinationPanelCanvasGroup, _currentDestination != Destination.Battle);

            if (_currentDestination == Destination.Battle)
                ConfigureDestinationContent(Destination.Battle);
        }

        void UpdateBattleAndPlaceholderTransition(float easedProgress)
        {
            bool targetIsBattle = _targetDestination == Destination.Battle;

            if (targetIsBattle)
            {
                ApplyTransitionState(_battlePanel, _battlePanelCanvasGroup, easedProgress, -_slideDistance * (1f - easedProgress));
                ApplyTransitionState(_destinationPanel, _destinationPanelCanvasGroup, 1f - easedProgress, _slideDistance * easedProgress);
            }
            else
            {
                ApplyTransitionState(_battlePanel, _battlePanelCanvasGroup, 1f - easedProgress, _slideDistance * easedProgress);
                ApplyTransitionState(_destinationPanel, _destinationPanelCanvasGroup, easedProgress, -_slideDistance * (1f - easedProgress));
            }

        }

        void UpdatePlaceholderToPlaceholderTransition(float progress)
        {
            const float midpoint = 0.5f;

            if (progress < midpoint)
            {
                float outgoingProgress = Mathf.SmoothStep(0f, 1f, progress / midpoint);
                ApplyTransitionState(_destinationPanel, _destinationPanelCanvasGroup, 1f - outgoingProgress, _slideDistance * outgoingProgress);
                return;
            }

            if (_isDestinationContentPending)
            {
                _isDestinationContentPending = false;
                ConfigureDestinationContent(_targetDestination);
                _legionTabs.SetActive(_targetDestination == Destination.Legion);
            }

            float incomingProgress = Mathf.SmoothStep(0f, 1f, (progress - midpoint) / midpoint);
            ApplyTransitionState(_destinationPanel, _destinationPanelCanvasGroup, incomingProgress, -_slideDistance * (1f - incomingProgress));
        }

        void BindButtons()
        {
            ClearListeners();
            _startBattleButton.onClick.AddListener(HandleStartBattle);
            _traitEntryButton.onClick.AddListener(ShowTrait);
            _backButton.onClick.AddListener(ShowBattleHome);
            _legionDecksButton.onClick.AddListener(ShowLegionDecks);
            _legionCardsButton.onClick.AddListener(ShowLegionCards);
            for (int i = 0; i < _navigationBindings.Length; i++)
                _navigationBindings[i].Button.onClick.AddListener(_navigationBindings[i].Click);
        }

        void ClearListeners()
        {
            if (_startBattleButton != null)
                _startBattleButton.onClick.RemoveAllListeners();
            if (_traitEntryButton != null)
                _traitEntryButton.onClick.RemoveAllListeners();
            if (_backButton != null)
                _backButton.onClick.RemoveAllListeners();
            if (_legionDecksButton != null)
                _legionDecksButton.onClick.RemoveAllListeners();
            if (_legionCardsButton != null)
                _legionCardsButton.onClick.RemoveAllListeners();
            if (_navigationBindings != null)
            {
                for (int i = 0; i < _navigationBindings.Length; i++)
                    _navigationBindings[i].Button.onClick.RemoveListener(_navigationBindings[i].Click);
            }
        }

        void HandleStartBattle()
        {
            if (_currentDestination == Destination.Battle && _isTransitioning == false)
                _startBattleRequested?.Invoke();
        }

        void ShowTrait() => SetDestination(Destination.Trait, false);
        void ShowBattleHome() => SetDestination(Destination.Battle, false);

        void ShowLegionDecks()
        {
            if (_currentDestination != Destination.Legion || _isTransitioning)
                return;

            _destinationTitleText.text = "LEGION";
            _destinationBodyText.text = "Deck editing is not available in this internal build.\n\nThis shell confirms the destination and Back flow only.";
        }

        void ShowLegionCards()
        {
            if (_currentDestination != Destination.Legion || _isTransitioning)
                return;

            _destinationTitleText.text = "LEGION";
            _destinationBodyText.text = "Collection management is not available in this internal build.\n\nThis shell confirms the destination and Back flow only.";
        }

        void SetDestination(Destination destination, bool immediate)
        {
            if (_isTransitioning || (destination == _currentDestination && immediate == false))
                return;

            _targetDestination = destination;
            ApplyNavigationSelection(destination);

            bool isPlaceholderToPlaceholder = _currentDestination != Destination.Battle && destination != Destination.Battle;
            if (isPlaceholderToPlaceholder == false &&
                (destination != Destination.Battle || immediate))
            {
                ConfigureDestinationContent(destination);
                _legionTabs.SetActive(destination == Destination.Legion);
            }

            if (immediate)
            {
                _isTransitioning = false;
                _currentDestination = destination;
                _battlePanel.gameObject.SetActive(destination == Destination.Battle);
                _destinationPanel.gameObject.SetActive(destination != Destination.Battle);
                ApplyPanelState(_battlePanel, _battlePanelCanvasGroup, destination == Destination.Battle);
                ApplyPanelState(_destinationPanel, _destinationPanelCanvasGroup, destination != Destination.Battle);
                return;
            }

            _battlePanel.gameObject.SetActive(true);
            _destinationPanel.gameObject.SetActive(true);
            _battlePanelCanvasGroup.blocksRaycasts = false;
            _destinationPanelCanvasGroup.blocksRaycasts = false;
            _transitionElapsed = 0f;
            _isTransitioning = true;
            _isDestinationToDestinationTransition = isPlaceholderToPlaceholder;
            _isDestinationContentPending = isPlaceholderToPlaceholder;

            if (isPlaceholderToPlaceholder)
                _battlePanel.gameObject.SetActive(false);

            // Edit-mode preview has no player loop to advance the transition. Apply the
            // resolved destination immediately there so public navigation remains
            // truthful for editor previews and live-scene tests without changing runtime motion.
            if (Application.isPlaying == false)
            {
                if (isPlaceholderToPlaceholder)
                {
                    ConfigureDestinationContent(destination);
                    _legionTabs.SetActive(destination == Destination.Legion);
                }
                _isTransitioning = false;
                _isDestinationToDestinationTransition = false;
                _isDestinationContentPending = false;
                _currentDestination = destination;
                _battlePanel.gameObject.SetActive(destination == Destination.Battle);
                _destinationPanel.gameObject.SetActive(destination != Destination.Battle);
                ApplyPanelState(_battlePanel, _battlePanelCanvasGroup, destination == Destination.Battle);
                ApplyPanelState(_destinationPanel, _destinationPanelCanvasGroup, destination != Destination.Battle);
            }
        }

        void ConfigureDestinationContent(Destination destination)
        {
            bool isShop = destination == Destination.Shop;
            bool isLegion = destination == Destination.Legion;
            _destinationTitleText.gameObject.SetActive(isShop == false);
            _backButton.gameObject.SetActive(isShop == false);
            _legionTabs.SetActive(false);
            _comingSoonContent.SetActive(isShop == false && isLegion == false);
            _shopContent.SetActive(isShop);
            _legionContent.SetActive(isLegion);
            _legionPassEntry.SetActive(isShop == false && isLegion == false);
            _comingSoonIcon.SetActive(isShop == false);
            if (isShop || isLegion)
            {
                _destinationScrollViewport.anchorMin = Vector2.zero;
                _destinationScrollViewport.anchorMax = Vector2.one;
                _destinationScrollViewport.offsetMin = new Vector2(48f, 24f);
                _destinationScrollViewport.offsetMax = new Vector2(-48f, -24f);
            }
            else
            {
                _destinationScrollViewport.anchorMin = new Vector2(0.1f, 0.1f);
                _destinationScrollViewport.anchorMax = new Vector2(0.9f, 0.61f);
                _destinationScrollViewport.offsetMin = Vector2.zero;
                _destinationScrollViewport.offsetMax = Vector2.zero;
            }
            _destinationScrollContent.sizeDelta = new Vector2(
                _destinationScrollContent.sizeDelta.x,
                isShop ? 2288f : isLegion ? 1240f : 980f);
            _destinationScrollRect.vertical = isShop || isLegion;
            _destinationScrollRect.verticalNormalizedPosition = 1f;

            switch (destination)
            {
                case Destination.Shop:
                    _destinationTitleText.text = "SHOP";
                    break;
                case Destination.Legion:
                    _destinationTitleText.text = "LEGION";
                    _destinationBodyText.text = "Deck editing is not available in this internal build.\n\nThis shell confirms the destination and Back flow only.";
                    break;
                case Destination.Relic:
                    _destinationTitleText.text = "RELIC";
                    _destinationBodyText.text = "Relic content is not available in this internal build.\n\nNo relic collection or server connection is active.";
                    break;
                case Destination.Trait:
                    _destinationTitleText.text = "TRAIT";
                    _destinationBodyText.text = "Trait content is not available in this internal build.\n\nThis shell confirms the destination and Back flow only.";
                    break;
            }
        }

        void ApplyNavigationSelection(Destination destination)
        {
            Button selectedButton = null;
            for (int i = 0; i < _navigationBindings.Length; i++)
            {
                NavigationBinding binding = _navigationBindings[i];
                if (destination == binding.Destination)
                    selectedButton = binding.Button;
            }
            _bottomNavigationView.SetSelected(selectedButton);
        }

        static void ApplyPanelState(RectTransform panel, CanvasGroup canvasGroup, bool active)
        {
            panel.anchoredPosition = Vector2.zero;
            canvasGroup.alpha = active ? 1f : 0f;
            canvasGroup.blocksRaycasts = active;
            canvasGroup.interactable = active;
        }

        static void ApplyTransitionState(
            RectTransform panel,
            CanvasGroup canvasGroup,
            float alpha,
            float horizontalOffset)
        {
            panel.anchoredPosition = new Vector2(horizontalOffset, 0f);
            canvasGroup.alpha = alpha;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
    }
}
