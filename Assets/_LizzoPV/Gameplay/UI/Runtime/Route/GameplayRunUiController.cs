using System;
using System.Collections.Generic;
using Lizzo.PV.Flow;
using Lizzo.PV.Gameplay.CardOffer;
using Lizzo.PV.Gameplay.Input;
using Lizzo.PV.Gameplay.Pause;
using Lizzo.PV.Gameplay.Result;
using Lizzo.PV.Legion;
using Lizzo.PV.Gameplay.CardOffer;
using Lizzo.PV.Gameplay.CardOffer;
using Lizzo.PV.Gameplay.Presentation;
using Lizzo.PV.UI;
using UnityEngine;

namespace Lizzo.PV.Gameplay.Route
{
    [DisallowMultipleComponent]
    public sealed partial class GameplayRunUiController : MonoBehaviour, IGameplayRunUi
    {
        private const int MaxCompanionPauseEntries = 7;
        private const int MaxPassivePauseEntries = 5;
        private const float CardSelectionRevealSeconds = 0.18f;

        private enum ModalKind
        {
            None,
            CardOffer,
            Result,
        }

        [SerializeField]
        private GameplayHudController _hudController;

        [SerializeField]
        private GameplayCardOfferController _cardOfferController;

        [SerializeField]
        private GameplayPauseController _pauseController;

        [SerializeField]
        private GameplayRunResultPopupController _resultController;

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
        ModalKind _activeModal;
        bool _initialized;
        bool _gameplayVisible;
        bool _pauseOverlayVisible;
        bool _selectionInProgress;
        Action _mainRequested;

        public event Action<bool> ModalChanged;
        public event Action MaxBuildCompleteBannerRequested;
        public event Action CardOfferOpened;
        public event Action CardOfferClosed;
        public event Action PauseOpened;
        public event Action PauseClosed;
        public event Action<RunResultViewData> ResultOpened;
        public event Action<bool> BossVisibilityChanged;
        public event Action BossWarningOpened;

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
            _pauseController.AbandonRequested += HandlePauseAbandonRequested;
            _resultController.MainRequested += HandleMainRequested;
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

        private void CloseActiveModal()
        {
            ModalKind closingModal = _activeModal;
            if (_activeModal == ModalKind.CardOffer)
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
            _mainRequested = null;
            UpdateBossWarningSuspension();
            if (_gameplayVisible)
                _hudController.gameObject.SetActive(true);

            if (closingModal == ModalKind.CardOffer)
                CardOfferClosed?.Invoke();
        }

        private void UpdateInputGate()
        {
            bool enabled = _initialized
                && _gameplayVisible
                && _activeModal == ModalKind.None
                && !_pauseOverlayVisible;
            _inputController.SetInputEnabled(enabled);
        }

        private void UpdateBossWarningSuspension()
        {
            if (_initialized && _feedbackController != null)
            {
                bool wasSuspended = _feedbackController.IsBossWarningSuspended;
                bool shouldSuspend = _activeModal != ModalKind.None || _pauseOverlayVisible;
                _feedbackController.SetBossWarningSuspended(shouldSuspend);
                if (wasSuspended
                    && !shouldSuspend
                    && _feedbackController.IsBossWarningVisible)
                {
                    BossWarningOpened?.Invoke();
                }
            }
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
                _pauseController.AbandonRequested -= HandlePauseAbandonRequested;
            }

            if (_resultController != null)
            {
                _resultController.MainRequested -= HandleMainRequested;
            }

            if (_cardOfferController != null)
                _cardOfferController.SelectionDispatched -= HandleCardSelection;

            ModalChanged = null;
            MaxBuildCompleteBannerRequested = null;
            CardOfferOpened = null;
            CardOfferClosed = null;
            PauseOpened = null;
            PauseClosed = null;
            ResultOpened = null;
            BossVisibilityChanged = null;
            BossWarningOpened = null;
            _services = null;
            _runPauseController = null;
        }
    }
}
