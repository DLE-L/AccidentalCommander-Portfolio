using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Lizzo.PV.Flow;
using Lizzo.PV.Gameplay.CardOffer;
using Lizzo.PV.Gameplay.Input;
using Lizzo.PV.Gameplay.Pause;
using Lizzo.PV.Gameplay.Result;
using Lizzo.PV.Gameplay.RunTraits;
using Lizzo.PV.Legion;
using Lizzo.PV.P0.Cards;
using Lizzo.PV.P0.Cards.CardOffer;
using Lizzo.PV.UI;
using UnityEngine;

namespace Lizzo.PV.Gameplay.Route
{
    [DisallowMultipleComponent]
    public sealed class GameplayRunUiController : MonoBehaviour, IGameplayRunUi, IRunTraitOfferUi
    {
        private const int MaxCompanionPauseEntries = 7;
        private const int MaxPassivePauseEntries = 5;
        private const float CardSelectionRevealSeconds = 0.18f;

        private enum ModalKind
        {
            None,
            CardOffer,
            TraitOffer,
            Result,
        }

        [SerializeField]
        private GameplayHudController _hudController;

        [SerializeField]
        private GameplayCardOfferController _cardOfferController;

        [SerializeField]
        private GameplayPauseController _pauseController;

        [SerializeField]
        private GameplayResultController _resultController;

        [SerializeField]
        private GameplayFeedbackController _feedbackController;

        [SerializeField]
        private GameplayInputLayerController _inputController;

        readonly List<PauseCompanionPresentation> _companionPausePresentations = new List<PauseCompanionPresentation>(MaxCompanionPauseEntries);
        readonly List<PausePassivePresentation> _passivePausePresentations = new List<PausePassivePresentation>(MaxPassivePauseEntries);
        readonly List<PauseSynergyPresentation> _pauseSynergyPresentations = new List<PauseSynergyPresentation>(8);

        RunServices _services;
        RunPauseController _runPauseController;
        CardData[] _displayedCards = Array.Empty<CardData>();
        string _displayedOfferIdentity = string.Empty;
        RunTraitOfferSnapshot _displayedTraitOffer;
        Func<string, int, string, bool> _traitOfferSelectionRequested;
        ModalKind _activeModal;
        bool _initialized;
        bool _gameplayVisible;
        bool _pauseOverlayVisible;
        bool _selectionInProgress;
        Action _primaryRequested;
        Action _optionalRequested;
        Action _lobbyRequested;

        public event Action<bool> ModalChanged;
        public event Action MaxBuildCompleteBannerRequested;

        public bool IsThreatDirectionVisible => _initialized && _feedbackController.IsThreatDirectionVisible;
        public bool IsModalOpen => _activeModal != ModalKind.None;
        public bool IsPauseOverlayVisible => _pauseOverlayVisible;

        public bool Initialize(RunServices services, Camera worldCamera, RunPauseController pauseController)
        {
            if (_initialized)
                return true;

            if (services == null || worldCamera == null || pauseController == null)
            {
                Debug.LogError("[GameplayRunUiController] RunServices, world camera, and RunPauseController are required.", this);
                return false;
            }

            if (_hudController == null
                || _cardOfferController == null
                || _pauseController == null
                || _resultController == null
                || _feedbackController == null
                || _inputController == null)
            {
                Debug.LogError("[GameplayRunUiController] Explicit HUD, CardOffer, Pause, Result, Feedback, and Input references are required.", this);
                return false;
            }

            if (!_hudController.Configure()
                || !_inputController.Configure()
                || !_resultController.Configure()
                || !_feedbackController.Configure())
            {
                Debug.LogError("[GameplayRunUiController] Clean gameplay UI authoring validation failed.", this);
                return false;
            }

            _services = services;
            _runPauseController = pauseController;
            _hudController.PauseRequested += _runPauseController.ToggleUserPause;
            _hudController.SpeedToggleRequested += HandleSpeedToggleRequested;
            _pauseController.ResumeRequested += _runPauseController.ResumeFromPauseButton;
            _pauseController.LobbyRequested += HandlePauseLobbyRequested;
            _resultController.PrimaryRequested += HandlePrimaryRequested;
            _resultController.LobbyRequested += HandleLobbyRequested;
            _resultController.ReviveRequested += HandleReviveRequested;
            _cardOfferController.SelectionDispatched += HandleCardSelection;

            _hudController.gameObject.SetActive(false);
            _inputController.gameObject.SetActive(false);
            _cardOfferController.gameObject.SetActive(false);
            _pauseController.gameObject.SetActive(false);
            _resultController.gameObject.SetActive(false);
            _gameplayVisible = false;
            _activeModal = ModalKind.None;
            _initialized = true;
            return true;
        }

        public void ShowGameplay()
        {
            EnsureInitialized();
            _gameplayVisible = true;
            _hudController.gameObject.SetActive(true);
            _inputController.gameObject.SetActive(true);
            UpdateInputGate();
        }

        public void BindPlayer(PlayerController player)
        {
            EnsureInitialized();
            if (!_inputController.BindPlayer(player))
                throw new InvalidOperationException("[GameplayRunUiController] Player binding failed.");

            _inputController.gameObject.SetActive(true);
            UpdateInputGate();
        }

        public bool ShowSkillSelection()
        {
            EnsureInitialized();
            LegacyCardOfferRouteResult route = LegacyCardOfferRoute.ResolveNextOffer();
            if (!route.ShouldPresentOffer)
            {
                if (route.RequestBuildCompleteBanner)
                    MaxBuildCompleteBannerRequested?.Invoke();
                return false;
            }

            CloseActiveModal();

            CardOfferSnapshot snapshot = FixedCardPool.ActiveCardOfferSnapshot;
            if (snapshot == null || PresentCardOffer(route.Cards, snapshot) == false)
                return false;

            _activeModal = ModalKind.CardOffer;
            _cardOfferController.gameObject.SetActive(true);
            UpdateInputGate();
            ModalChanged?.Invoke(true);
            return true;
        }

        public bool ShowRunTraitOffer(RunTraitOfferSnapshot snapshot, Func<string, int, string, bool> selectionRequested)
        {
            EnsureInitialized();
            if (snapshot == null || snapshot.Slots.Count != 3 || selectionRequested == null
                || _activeModal != ModalKind.None || _pauseOverlayVisible)
                return false;

            if (PresentTraitOffer(snapshot) == false)
                return false;

            _displayedTraitOffer = snapshot;
            _traitOfferSelectionRequested = selectionRequested;
            _activeModal = ModalKind.TraitOffer;
            _cardOfferController.gameObject.SetActive(true);
            UpdateInputGate();
            ModalChanged?.Invoke(true);
            return true;
        }

        public bool ShowResult(RunResultViewData data, Action primaryRequested, Action optionalRequested, Action lobbyRequested)
        {
            EnsureInitialized();
            if (data == null)
                return false;

            CloseActiveModal();
            _primaryRequested = primaryRequested;
            _optionalRequested = optionalRequested;
            _lobbyRequested = lobbyRequested;

            bool presented = optionalRequested != null && !data.IsClear
                ? _resultController.PresentReviveChoice(data)
                : _resultController.PresentResult(data);
            if (!presented)
                return false;

            _activeModal = ModalKind.Result;
            _hudController.gameObject.SetActive(false);
            _inputController.gameObject.SetActive(true);
            _resultController.gameObject.SetActive(true);
            UpdateInputGate();
            ModalChanged?.Invoke(true);
            return true;
        }

        public void CloseModal()
        {
            if (!_initialized)
                return;

            bool wasModal = _activeModal != ModalKind.None;
            CloseActiveModal();
            UpdateInputGate();
            if (wasModal)
                ModalChanged?.Invoke(false);
        }

        public void SetPauseOverlay(bool visible, bool fromAppBackground)
        {
            EnsureInitialized();
            _pauseOverlayVisible = visible;
            if (visible)
            {
                RefreshPausePresentation();
                _pauseController.gameObject.SetActive(true);
                if (!_pauseController.Present(
                        fromAppBackground,
                        _companionPausePresentations,
                        _passivePausePresentations,
                        _pauseSynergyPresentations))
                {
                    Debug.LogError("[GameplayRunUiController] Clean pause presentation failed.", this);
                }
            }
            else
            {
                _pauseController.Hide();
                _pauseController.gameObject.SetActive(false);
            }

            UpdateInputGate();
        }

        public void SetGameplaySpeed(float speed)
        {
            EnsureInitialized();
            _hudController.SetGameplaySpeed(speed);
        }

        public void SetRunStatus(int kills, float survivalSeconds)
        {
            EnsureInitialized();
            _hudController.SetRunStatus(kills, survivalSeconds);
        }

        public void SetExperienceStatus(int level, float currentExperience, float requiredExperience)
        {
            EnsureInitialized();
            _hudController.SetExperience(level, currentExperience, requiredExperience);
        }

        public void ShowBoss(string name, int hp, int maxHp)
        {
            EnsureInitialized();
            _hudController.ShowBoss(hp, maxHp);
        }

        public void HideBoss()
        {
            if (_initialized)
                _hudController.HideBoss();
        }

        public void ShowBossPreWarning(string text, Color accentColor, float durationSeconds, bool showEdges)
        {
            if (_initialized)
                _feedbackController.ShowBossWarning(text, accentColor, durationSeconds, showEdges);
        }

        public void HideBossPreWarning()
        {
            if (_initialized)
                _feedbackController.HideBossWarning();
        }

        public void ShowThreatDirection(Transform target, string label, Color accentColor, float durationSeconds = 0.0f)
        {
            if (_initialized)
                _feedbackController.ShowThreatDirection(target, null, label, accentColor, durationSeconds);
        }

        public void HideThreatDirection()
        {
            if (_initialized)
                _feedbackController.HideThreatDirection();
        }

        public void HideGameplay()
        {
            if (!_initialized)
                return;

            CloseActiveModal();
            _gameplayVisible = false;
            _hudController.gameObject.SetActive(false);
            _inputController.gameObject.SetActive(false);
            UpdateInputGate();
        }

        private bool PresentCardOffer(CardData[] cards, CardOfferSnapshot snapshot)
        {
            if (cards == null || cards.Length == 0 || cards.Length > 3 || snapshot.Slots.Count != cards.Length)
                return false;

            _displayedCards = (CardData[])cards.Clone();
            _displayedOfferIdentity = snapshot.OfferIdentity;
            _selectionInProgress = false;
            _cardOfferController.ClearOffer();
            for (int index = 0; index < cards.Length; index++)
            {
                SkillCardPresentationModel presentation = SkillCardPresentationResolver.Resolve(cards[index], _services.Party);
                string value = string.IsNullOrEmpty(presentation.RoleBadge)
                    ? CardPresentation.GetEffectText(cards[index])
                    : presentation.RoleBadge;
                bool showProgress = presentation.IsCompanion || presentation.IsPassive;
                int progressCount = presentation.IsCompanion
                    ? presentation.OwnedCompanionCount
                    : presentation.OwnedPassiveCount;
                GameplayCardOfferItemPresentation item = new GameplayCardOfferItemPresentation(
                    snapshot.Slots[index].CardId,
                    presentation.Title,
                    presentation.Description,
                    value,
                    presentation.StatusText,
                    presentation.Portrait ?? presentation.CatalogEntry?.Icon,
                    presentation.SynergyHint,
                    showProgress,
                    progressCount,
                    presentation.Recommended || presentation.HighlightFrame);
                if (!_cardOfferController.PresentOfferSlot(index, item))
                    return false;
            }

            return true;
        }

        private bool PresentTraitOffer(RunTraitOfferSnapshot snapshot)
        {
            _displayedCards = Array.Empty<CardData>();
            _displayedOfferIdentity = string.Empty;
            _selectionInProgress = false;
            _cardOfferController.ClearOffer();
            for (int index = 0; index < snapshot.Slots.Count; index++)
            {
                RunTraitOfferSlot slot = snapshot.Slots[index];
                if (slot.SlotIndex != index || RunTraitCatalog.TryGet(slot.TraitId, out RunTraitDefinition trait) == false)
                {
                    _cardOfferController.ClearOffer();
                    return false;
                }

                GameplayCardOfferItemPresentation item = new GameplayCardOfferItemPresentation(
                    trait.Id,
                    trait.DisplayName,
                    trait.Description,
                    ToKoreanCategory(trait.Category),
                    "이번 출정 한정",
                    null,
                    trait.RelatedBuild,
                    showProgress: false,
                    progressCount: 0,
                    recommended: false);
                if (_cardOfferController.PresentOfferSlot(index, item) == false)
                {
                    _cardOfferController.ClearOffer();
                    return false;
                }
            }

            return true;
        }

        private void HandleCardSelection(int slotIndex, string cardId)
        {
            if (_selectionInProgress)
                return;
            if (_activeModal == ModalKind.TraitOffer)
            {
                HandleTraitOfferSelection(slotIndex, cardId);
                return;
            }
            if (_activeModal != ModalKind.CardOffer)
                return;
            if (slotIndex < 0 || slotIndex >= _displayedCards.Length)
                return;

            CardOfferSnapshot snapshot = FixedCardPool.ActiveCardOfferSnapshot;
            if (snapshot == null
                || !string.Equals(snapshot.OfferIdentity, _displayedOfferIdentity, StringComparison.Ordinal)
                || slotIndex >= snapshot.Slots.Count
                || !string.Equals(snapshot.Slots[slotIndex].CardId, cardId, StringComparison.Ordinal))
            {
                return;
            }

            if (FixedCardPool.TryGetTutorialRequiredCardData(_displayedCards, out CardData requiredCard)
                && _displayedCards[slotIndex].Kind != requiredCard.Kind)
            {
                int requiredSlotIndex = FindCardSlot(requiredCard.Kind);
                if (requiredSlotIndex >= 0)
                {
                    _cardOfferController.TrySelectSlot(requiredSlotIndex);
                }
                return;
            }

            _selectionInProgress = true;
            if (!FixedCardPool.TrySelect(_displayedCards[slotIndex]))
            {
                _selectionInProgress = false;
                return;
            }

            FinishCardSelectionAsync().Forget();
        }

        private void HandleTraitOfferSelection(int slotIndex, string traitId)
        {
            if (_displayedTraitOffer == null || slotIndex < 0 || slotIndex >= _displayedTraitOffer.Slots.Count)
                return;

            RunTraitOfferSlot slot = _displayedTraitOffer.Slots[slotIndex];
            if (string.Equals(slot.TraitId, traitId, StringComparison.Ordinal) == false
                || _traitOfferSelectionRequested == null
                || _traitOfferSelectionRequested(_displayedTraitOffer.OfferIdentity, slotIndex, traitId) == false)
                return;

            _selectionInProgress = true;
            FinishCardSelectionAsync().Forget();
        }

        private async UniTaskVoid FinishCardSelectionAsync()
        {
            try
            {
                await UniTask.Delay(
                    TimeSpan.FromSeconds(CardSelectionRevealSeconds),
                    DelayType.UnscaledDeltaTime,
                    PlayerLoopTiming.Update,
                    this.GetCancellationTokenOnDestroy());
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (_selectionInProgress)
                CloseModal();
        }

        private int FindCardSlot(CardKind kind)
        {
            for (int index = 0; index < _displayedCards.Length; index++)
                if (_displayedCards[index].Kind == kind)
                    return index;

            return -1;
        }

        private static string ToKoreanCategory(string category)
        {
            if (string.Equals(category, RunTraitCategories.BuildRelated, StringComparison.Ordinal))
                return "빌드 연계";
            if (string.Equals(category, RunTraitCategories.General, StringComparison.Ordinal))
                return "일반";
            return "변칙";
        }

        private void RefreshPausePresentation()
        {
            PauseBuildSummaryPresentationResolver.Fill(
                _services.Party.GetSquadSlotSnapshot(),
                _services.PassiveRoster,
                _services.Synergies,
                _services.App.Data,
                _companionPausePresentations,
                _passivePausePresentations,
                _pauseSynergyPresentations,
                MaxCompanionPauseEntries,
                MaxPassivePauseEntries,
                this);
        }

        private void CloseActiveModal()
        {
            if (_activeModal == ModalKind.CardOffer || _activeModal == ModalKind.TraitOffer)
            {
                _cardOfferController.ClearOffer();
                _cardOfferController.gameObject.SetActive(false);
            }
            else if (_activeModal == ModalKind.Result)
            {
                _resultController.Hide();
                _resultController.gameObject.SetActive(false);
            }

            _activeModal = ModalKind.None;
            _selectionInProgress = false;
            _displayedCards = Array.Empty<CardData>();
            _displayedOfferIdentity = string.Empty;
            _displayedTraitOffer = null;
            _traitOfferSelectionRequested = null;
            if (_gameplayVisible)
                _hudController.gameObject.SetActive(true);
        }

        private void UpdateInputGate()
        {
            bool enabled = _initialized
                && _gameplayVisible
                && _activeModal == ModalKind.None
                && !_pauseOverlayVisible;
            _inputController.SetInputEnabled(enabled);
        }

        private void HandlePrimaryRequested()
        {
            _primaryRequested?.Invoke();
        }

        private void HandleSpeedToggleRequested()
        {
            _runPauseController.ToggleGameplaySpeed();
        }

        private void HandleReviveRequested()
        {
            _optionalRequested?.Invoke();
        }

        private void HandleLobbyRequested()
        {
            _lobbyRequested?.Invoke();
        }

        private void HandlePauseLobbyRequested()
        {
            GameFlowRoutes.LoadLobby();
        }

        private void EnsureInitialized()
        {
            if (!_initialized)
                throw new InvalidOperationException("[GameplayRunUiController] Initialize must be called before using the controller.");
        }

        private void OnDestroy()
        {
            if (_hudController != null)
            {
                if (_runPauseController != null)
                {
                    _hudController.PauseRequested -= _runPauseController.ToggleUserPause;
                    _hudController.SpeedToggleRequested -= HandleSpeedToggleRequested;
                }
            }

            if (_pauseController != null)
            {
                if (_runPauseController != null)
                    _pauseController.ResumeRequested -= _runPauseController.ResumeFromPauseButton;
                _pauseController.LobbyRequested -= HandlePauseLobbyRequested;
            }

            if (_resultController != null)
            {
                _resultController.PrimaryRequested -= HandlePrimaryRequested;
                _resultController.LobbyRequested -= HandleLobbyRequested;
                _resultController.ReviveRequested -= HandleReviveRequested;
            }

            if (_cardOfferController != null)
                _cardOfferController.SelectionDispatched -= HandleCardSelection;

            ModalChanged = null;
            MaxBuildCompleteBannerRequested = null;
            _services = null;
            _runPauseController = null;
        }
    }
}
