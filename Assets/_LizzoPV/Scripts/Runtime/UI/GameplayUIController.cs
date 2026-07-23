using Lizzo.PV.Flow;
using Lizzo.PV.Legion;
using Lizzo.PV.P0.Cards;
using Lizzo.PV.P0.Presentation;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lizzo.PV.UI
{
    public sealed class GameplayUIController : MonoBehaviour
    {
        [Header("Gameplay UI")]
        private const int MaxCompanionPauseEntries = 7;
        private const int MaxPassivePauseEntries = 5;

        [SerializeField] UI_GameplayHud _hud;
        [SerializeField] global::UI_Joystick _joystick;
        [SerializeField] UI_CardSelectPopup _skillSelectPopup;
        [SerializeField] global::UI_GameResultPopup _resultPopup;

        readonly List<Sprite> _companionPauseIcons = new List<Sprite>(MaxCompanionPauseEntries);
        readonly List<Sprite> _passivePauseIcons = new List<Sprite>(MaxPassivePauseEntries);
        readonly CardKind[] _passiveKinds = new CardKind[MaxPassivePauseEntries];

        IPrefabFactory _cardFactory;
        PartyService _party;
        bool _initialized;
        bool _pauseOverlayVisible;
        global::UI_Base _activeModal;

        public UI_GameplayHud Hud => _hud;
        public bool IsThreatDirectionVisible => _initialized && _hud.IsThreatDirectionVisible;
        public event Action<bool> ModalChanged;

        public bool Initialize(
            IPrefabFactory cardFactory,
            PartyService party,
            Camera worldCamera,
            Action pauseRequested,
            Action resumeRequested,
            Func<bool> speedToggleRequested,
            Func<float> selectedGameplaySpeed)
        {
            if (_initialized)
                return true;

            if (_hud == null || _joystick == null || _skillSelectPopup == null || _resultPopup == null)
            {
                Debug.LogError("[GameplayUIController] Authored HUD, Joystick, CardSelectPopup, and ResultPopup references are required.", this);
                return false;
            }

            _cardFactory = cardFactory;
            _party = party;
            if (_cardFactory == null || _party == null)
            {
                Debug.LogError("[GameplayUIController] Card factory and PartyService are required.", this);
                return false;
            }

            if (!_hud.Configure(
                    pauseRequested,
                    resumeRequested,
                    GameFlowRoutes.LoadLobby,
                    speedToggleRequested,
                    selectedGameplaySpeed))
                return false;
            if (!_skillSelectPopup.Configure(_cardFactory, party, CloseModal))
                return false;
            if (!_resultPopup.Configure())
                return false;

            if (!_hud.Init())
                return false;
            if (!_hud.ConfigureThreatIndicator(worldCamera))
                return false;
            _joystick.Init();
            _skillSelectPopup.Init();
            _resultPopup.Init();

            _joystick.SetInputEnabled(false);

            _hud.gameObject.SetActive(false);
            _joystick.gameObject.SetActive(false);
            _skillSelectPopup.gameObject.SetActive(false);
            _resultPopup.gameObject.SetActive(false);
            _initialized = true;
            return true;
        }

        public void ShowGameplay()
        {
            EnsureInitialized();
            _hud.gameObject.SetActive(true);
        }

        public void BindPlayer(PlayerController player)
        {
            EnsureInitialized();
            _joystick.BindPlayer(player);
            _joystick.gameObject.SetActive(true);
            UpdateJoystickInputState();
        }

        public void ShowSkillSelection()
        {
            EnsureInitialized();
            CloseActiveModal();
            _activeModal = _skillSelectPopup;
            UpdateJoystickInputState();
            _skillSelectPopup.gameObject.SetActive(true);
            ModalChanged?.Invoke(true);
        }

        public bool ShowResult(RunResultViewData data, Action primaryRequested, Action optionalRequested, Action lobbyRequested)
        {
            EnsureInitialized();
            CloseActiveModal();
            bool presented = data != null && data.IsClear
                ? _resultPopup.Present(data, primaryRequested, optionalRequested, lobbyRequested)
                : _resultPopup.PresentFailureReviveChoice(data, primaryRequested, lobbyRequested);
            if (!presented)
                return false;

            _activeModal = _resultPopup;
            _hud.gameObject.SetActive(false);
            _joystick.gameObject.SetActive(false);
            UpdateJoystickInputState();
            _resultPopup.gameObject.SetActive(true);
            ModalChanged?.Invoke(true);
            return true;
        }

        public void CloseModal()
        {
            if (_activeModal == null)
                return;

            bool closingResult = _activeModal == _resultPopup;
            _activeModal.gameObject.SetActive(false);
            _activeModal = null;
            if (closingResult)
            {
                _hud.gameObject.SetActive(true);
                _joystick.gameObject.SetActive(true);
            }
            UpdateJoystickInputState();
            ModalChanged?.Invoke(false);
        }

        public void SetPauseOverlay(bool visible, bool fromAppBackground)
        {
            EnsureInitialized();
            _pauseOverlayVisible = visible;
            if (visible)
            {
                RefreshPauseIconLists();
                _hud.ShowPause(fromAppBackground, _companionPauseIcons, _passivePauseIcons);
            }
            else
            {
                _hud.HidePause();
            }

            UpdateJoystickInputState();
        }

        public void SetGameplaySpeed(float speed)
        {
            EnsureInitialized();
            _hud.SetGameplaySpeed(speed);
        }

        public void SetRunStatus(int gold, int kills, float survivalSeconds)
        {
            EnsureInitialized();
            _hud.SetRunStatus(gold, kills, survivalSeconds);
        }

        public void SetExperienceStatus(int level, float experienceRatio)
        {
            EnsureInitialized();
            _hud.SetExperienceStatus(level, experienceRatio);
        }

        public void ShowBoss(string name, int hp, int maxHp)
        {
            EnsureInitialized();
            _hud.ShowBoss(name, hp, maxHp);
        }

        public void HideBoss()
        {
            if (_initialized)
                _hud.HideBoss();
        }

        public void ShowBossPreWarning(string text, Color accentColor, float durationSeconds, bool showEdges)
        {
            if (_initialized)
                _hud.ShowBossWarning(text, accentColor, durationSeconds, showEdges);
        }

        public void ShowBossCountdown(int seconds)
        {
            if (_initialized)
                _hud.ShowBossWarning(Mathf.Clamp(seconds, 1, 5).ToString(), new Color(1.0f, 0.18f, 0.08f, 1.0f), 1.05f, true);
        }

        public void HideBossPreWarning()
        {
            if (_initialized)
                _hud.HideBossWarning();
        }

        public void ShowThreatDirection(Transform target, string label, Color accentColor, float durationSeconds = 0f)
        {
            if (_initialized)
                _hud.ShowThreatDirection(target, null, label, accentColor, durationSeconds);
        }

        public void HideThreatDirection()
        {
            if (_initialized)
                _hud.HideThreatDirection();
        }

        public void BindPlayerHud()
        {
            EnsureInitialized();
            _hud.gameObject.SetActive(true);
        }

        public void HideGameplay()
        {
            if (_initialized == false)
                return;

            CloseActiveModal();
            _hud.gameObject.SetActive(false);
            _joystick.gameObject.SetActive(false);
            _joystick.SetInputEnabled(false);
        }

        void RefreshPauseIconLists()
        {
            _companionPauseIcons.Clear();
            _passivePauseIcons.Clear();

            IReadOnlyList<SquadSlotState> squadSnapshot = _party.GetSquadSlotSnapshot();
            for (int i = 0; i < squadSnapshot.Count && _companionPauseIcons.Count < MaxCompanionPauseEntries; i++)
            {
                SquadSlotState state = squadSnapshot[i];
                if (!state.IsActive
                    || PresentationCatalogProvider.TryGetSquadSlot(state.SlotId, out SquadSlotPresentationSet.Entry entry) == false
                    || entry.ShowInHud == false
                    || entry.Icon == null)
                {
                    continue;
                }

                _companionPauseIcons.Add(entry.Icon);
            }

            int passiveCount = CardEffectRuntime.FillAcquiredPassiveKinds(_passiveKinds);
            for (int i = 0; i < passiveCount && _passivePauseIcons.Count < MaxPassivePauseEntries; i++)
            {
                if (PresentationCatalogProvider.TryGetCard(_passiveKinds[i].ToString(), out CardPresentationSet.Entry entry)
                    && entry.Icon != null)
                {
                    _passivePauseIcons.Add(entry.Icon);
                }
            }
        }

        void CloseActiveModal()
        {
            if (_activeModal == null)
                return;

            bool closingResult = _activeModal == _resultPopup;
            _activeModal.gameObject.SetActive(false);
            _activeModal = null;
            if (closingResult)
            {
                _hud.gameObject.SetActive(true);
                _joystick.gameObject.SetActive(true);
            }
            UpdateJoystickInputState();
            ModalChanged?.Invoke(false);
        }

        void UpdateJoystickInputState()
        {
            bool shouldEnable = _initialized
                && _activeModal == null
                && !_pauseOverlayVisible
                && _joystick.gameObject.activeSelf;
            _joystick.SetInputEnabled(shouldEnable);
        }

        void EnsureInitialized()
        {
            if (!_initialized)
                throw new InvalidOperationException("[GameplayUIController] Initialize must be called before using the controller.");
        }

void OnDestroy()
        {
            if (_activeModal != null)
                _activeModal.gameObject.SetActive(false);

            ModalChanged = null;
            _cardFactory = null;
            _party = null;
            _companionPauseIcons.Clear();
            _passivePauseIcons.Clear();
        }
    }
}
