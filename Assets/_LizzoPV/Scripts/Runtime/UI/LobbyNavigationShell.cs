using System;
using TMPro;
using Lizzo.PV.Flow;
using UnityEngine;
using UnityEngine.UI;

namespace Lizzo.PV.UI
{
    public sealed class LobbyNavigationShell : MonoBehaviour
    {
        enum Destination
        {
            Shop,
            Collection,
            Battle,
            Social,
            Progression,
        }

        [Header("Panels")]
        [SerializeField] RectTransform _battlePanel;
        [SerializeField] CanvasGroup _battlePanelCanvasGroup;
        [SerializeField] RectTransform _placeholderPanel;
        [SerializeField] CanvasGroup _placeholderPanelCanvasGroup;

        [Header("Battle Home")]
        [SerializeField] TMP_Text _statusText;
        [SerializeField] TMP_Text _nextRunText;
        [SerializeField] Button _startBattleButton;
        [SerializeField] Button _battleProgressionButton;

        [Header("Placeholder")]
        [SerializeField] TMP_Text _placeholderTitleText;
        [SerializeField] TMP_Text _placeholderBodyText;
        [SerializeField] Button _backButton;
        [SerializeField] Button _collectionDecksButton;
        [SerializeField] Button _collectionCardsButton;
        [SerializeField] GameObject _collectionTabs;
        [SerializeField] GameObject _defaultPlaceholderContent;
        [SerializeField] GameObject _shopContent;
        [SerializeField] RectTransform _placeholderScrollContent;
        [SerializeField] ScrollRect _placeholderScrollRect;
        [SerializeField] RectTransform _placeholderScrollViewport;
        [SerializeField] GameObject _placeholderIcon;

        [Header("Bottom Navigation")]
        [SerializeField] Button _shopButton;
        [SerializeField] Image _shopSelectedImage;
        [SerializeField] TMP_Text _shopLabel;
        [SerializeField] Button _collectionButton;
        [SerializeField] Image _collectionSelectedImage;
        [SerializeField] TMP_Text _collectionLabel;
        [SerializeField] Button _battleButton;
        [SerializeField] Image _battleSelectedImage;
        [SerializeField] TMP_Text _battleLabel;
        [SerializeField] Button _socialButton;
        [SerializeField] Image _socialSelectedImage;
        [SerializeField] TMP_Text _socialLabel;
        [SerializeField] Button _progressionNavButton;
        [SerializeField] Image _progressionSelectedImage;
        [SerializeField] TMP_Text _progressionLabel;

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
        bool _isPlaceholderToPlaceholderTransition;
        bool _isPlaceholderContentPending;

        public bool Configure()
        {
            if (_battlePanel == null ||
                _battlePanelCanvasGroup == null ||
                _placeholderPanel == null ||
                _placeholderPanelCanvasGroup == null ||
                _statusText == null ||
                _nextRunText == null ||
                _startBattleButton == null ||
                _battleProgressionButton == null ||
                _placeholderTitleText == null ||
                _placeholderBodyText == null ||
                _backButton == null ||
                _collectionDecksButton == null ||
                _collectionCardsButton == null ||
                _collectionTabs == null ||
                _defaultPlaceholderContent == null ||
                _shopContent == null ||
                _placeholderScrollContent == null ||
                _placeholderScrollRect == null ||
                _placeholderScrollViewport == null ||
                _placeholderIcon == null ||
                _shopButton == null || _shopSelectedImage == null || _shopLabel == null ||
                _collectionButton == null || _collectionSelectedImage == null || _collectionLabel == null ||
                _battleButton == null || _battleSelectedImage == null || _battleLabel == null ||
                _socialButton == null || _socialSelectedImage == null || _socialLabel == null ||
                _progressionNavButton == null || _progressionSelectedImage == null || _progressionLabel == null)
            {
                Debug.LogError("[LobbyNavigationShell] Authored lobby navigation references are required.", this);
                return false;
            }

            return true;
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

            if (_isPlaceholderToPlaceholderTransition)
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
            _isPlaceholderToPlaceholderTransition = false;
            _currentDestination = _targetDestination;
            _battlePanel.gameObject.SetActive(_currentDestination == Destination.Battle);
            _placeholderPanel.gameObject.SetActive(_currentDestination != Destination.Battle);
            ApplyPanelState(_battlePanel, _battlePanelCanvasGroup, _currentDestination == Destination.Battle);
            ApplyPanelState(_placeholderPanel, _placeholderPanelCanvasGroup, _currentDestination != Destination.Battle);

            if (_currentDestination == Destination.Battle)
                ConfigureDestinationContent(Destination.Battle);
        }

        void UpdateBattleAndPlaceholderTransition(float easedProgress)
        {
            bool targetIsBattle = _targetDestination == Destination.Battle;

            if (targetIsBattle)
            {
                ApplyTransitionState(_battlePanel, _battlePanelCanvasGroup, easedProgress, -_slideDistance * (1f - easedProgress));
                ApplyTransitionState(_placeholderPanel, _placeholderPanelCanvasGroup, 1f - easedProgress, _slideDistance * easedProgress);
            }
            else
            {
                ApplyTransitionState(_battlePanel, _battlePanelCanvasGroup, 1f - easedProgress, _slideDistance * easedProgress);
                ApplyTransitionState(_placeholderPanel, _placeholderPanelCanvasGroup, easedProgress, -_slideDistance * (1f - easedProgress));
            }

        }

        void UpdatePlaceholderToPlaceholderTransition(float progress)
        {
            const float midpoint = 0.5f;

            if (progress < midpoint)
            {
                float outgoingProgress = Mathf.SmoothStep(0f, 1f, progress / midpoint);
                ApplyTransitionState(_placeholderPanel, _placeholderPanelCanvasGroup, 1f - outgoingProgress, _slideDistance * outgoingProgress);
                return;
            }

            if (_isPlaceholderContentPending)
            {
                _isPlaceholderContentPending = false;
                ConfigureDestinationContent(_targetDestination);
                _collectionTabs.SetActive(_targetDestination == Destination.Collection);
            }

            float incomingProgress = Mathf.SmoothStep(0f, 1f, (progress - midpoint) / midpoint);
            ApplyTransitionState(_placeholderPanel, _placeholderPanelCanvasGroup, incomingProgress, -_slideDistance * (1f - incomingProgress));
        }

        void BindButtons()
        {
            ClearListeners();
            _startBattleButton.onClick.AddListener(HandleStartBattle);
            _battleProgressionButton.onClick.AddListener(ShowProgression);
            _backButton.onClick.AddListener(ShowBattleHome);
            _collectionDecksButton.onClick.AddListener(ShowCollectionDecks);
            _collectionCardsButton.onClick.AddListener(ShowCollectionCards);
            _shopButton.onClick.AddListener(ShowShop);
            _collectionButton.onClick.AddListener(ShowCollection);
            _battleButton.onClick.AddListener(ShowBattleHome);
            _socialButton.onClick.AddListener(ShowSocial);
            _progressionNavButton.onClick.AddListener(ShowProgression);
        }

        void ClearListeners()
        {
            if (_startBattleButton != null)
                _startBattleButton.onClick.RemoveAllListeners();
            if (_battleProgressionButton != null)
                _battleProgressionButton.onClick.RemoveAllListeners();
            if (_backButton != null)
                _backButton.onClick.RemoveAllListeners();
            if (_collectionDecksButton != null)
                _collectionDecksButton.onClick.RemoveAllListeners();
            if (_collectionCardsButton != null)
                _collectionCardsButton.onClick.RemoveAllListeners();
            if (_shopButton != null)
                _shopButton.onClick.RemoveAllListeners();
            if (_collectionButton != null)
                _collectionButton.onClick.RemoveAllListeners();
            if (_battleButton != null)
                _battleButton.onClick.RemoveAllListeners();
            if (_socialButton != null)
                _socialButton.onClick.RemoveAllListeners();
            if (_progressionNavButton != null)
                _progressionNavButton.onClick.RemoveAllListeners();
        }

        void HandleStartBattle()
        {
            if (_currentDestination == Destination.Battle && _isTransitioning == false)
                _startBattleRequested?.Invoke();
        }

        void ShowShop() => SetDestination(Destination.Shop, false);
        void ShowCollection() => SetDestination(Destination.Collection, false);
        void ShowSocial() => SetDestination(Destination.Social, false);
        void ShowProgression() => SetDestination(Destination.Progression, false);
        void ShowBattleHome() => SetDestination(Destination.Battle, false);

        void ShowCollectionDecks()
        {
            if (_currentDestination != Destination.Collection || _isTransitioning)
                return;

            _placeholderTitleText.text = "COLLECTION / DECKS";
            _placeholderBodyText.text = "Deck editing is not available in this internal build.\n\nThis shell confirms the destination and Back flow only.";
        }

        void ShowCollectionCards()
        {
            if (_currentDestination != Destination.Collection || _isTransitioning)
                return;

            _placeholderTitleText.text = "COLLECTION / CARDS";
            _placeholderBodyText.text = "Collection management is not available in this internal build.\n\nThis shell confirms the destination and Back flow only.";
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
                _collectionTabs.SetActive(destination == Destination.Collection);
            }

            if (immediate)
            {
                _isTransitioning = false;
                _currentDestination = destination;
                _battlePanel.gameObject.SetActive(destination == Destination.Battle);
                _placeholderPanel.gameObject.SetActive(destination != Destination.Battle);
                ApplyPanelState(_battlePanel, _battlePanelCanvasGroup, destination == Destination.Battle);
                ApplyPanelState(_placeholderPanel, _placeholderPanelCanvasGroup, destination != Destination.Battle);
                return;
            }

            _battlePanel.gameObject.SetActive(true);
            _placeholderPanel.gameObject.SetActive(true);
            _battlePanelCanvasGroup.blocksRaycasts = false;
            _placeholderPanelCanvasGroup.blocksRaycasts = false;
            _transitionElapsed = 0f;
            _isTransitioning = true;
            _isPlaceholderToPlaceholderTransition = isPlaceholderToPlaceholder;
            _isPlaceholderContentPending = isPlaceholderToPlaceholder;

            if (isPlaceholderToPlaceholder)
                _battlePanel.gameObject.SetActive(false);
        }

        void ConfigureDestinationContent(Destination destination)
        {
            bool isShop = destination == Destination.Shop;
            _defaultPlaceholderContent.SetActive(isShop == false);
            _shopContent.SetActive(isShop);
            _placeholderIcon.SetActive(isShop == false);
            _placeholderScrollViewport.anchorMax = new Vector2(
                _placeholderScrollViewport.anchorMax.x,
                isShop ? 0.70f : 0.61f);
            _placeholderScrollContent.sizeDelta = new Vector2(
                _placeholderScrollContent.sizeDelta.x,
                isShop ? 1500f : 980f);
            _placeholderScrollRect.verticalNormalizedPosition = 1f;

            switch (destination)
            {
                case Destination.Shop:
                    _placeholderTitleText.text = "SHOP";
                    break;
                case Destination.Collection:
                    _placeholderTitleText.text = "COLLECTION / DECKS";
                    _placeholderBodyText.text = "Deck editing is not available in this internal build.\n\nThis shell confirms the destination and Back flow only.";
                    break;
                case Destination.Social:
                    _placeholderTitleText.text = "SOCIAL";
                    _placeholderBodyText.text = "Social features are not available in this internal build.\n\nNo account, clan, or server connection is active.";
                    break;
                case Destination.Progression:
                    _placeholderTitleText.text = "PROGRESSION";
                    _placeholderBodyText.text = "Progression and rewards are not available in this internal build.\n\nThis shell confirms the destination and Back flow only.";
                    break;
            }
        }

        void ApplyNavigationSelection(Destination destination)
        {
            ApplyNavigationState(_shopSelectedImage, _shopLabel, destination == Destination.Shop);
            ApplyNavigationState(_collectionSelectedImage, _collectionLabel, destination == Destination.Collection);
            ApplyNavigationState(_battleSelectedImage, _battleLabel, destination == Destination.Battle);
            ApplyNavigationState(_socialSelectedImage, _socialLabel, destination == Destination.Social);
            ApplyNavigationState(_progressionSelectedImage, _progressionLabel, destination == Destination.Progression);
        }

        void ApplyNavigationState(Image selectedImage, TMP_Text label, bool selected)
        {
            selectedImage.enabled = selected;
            label.color = selected ? _selectedLabelColor : _defaultLabelColor;
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
