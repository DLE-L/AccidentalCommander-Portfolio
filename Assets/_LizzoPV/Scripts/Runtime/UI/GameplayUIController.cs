using Lizzo.PV.Flow;
using Lizzo.PV.Legion;
using Lizzo.PV.Legion.Synergy;
using Lizzo.PV.Data;
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
        [SerializeField] UI_RunResultPopup _resultPopup;

        readonly List<PauseCompanionPresentation> _companionPausePresentations = new List<PauseCompanionPresentation>(MaxCompanionPauseEntries);
        readonly List<PausePassivePresentation> _passivePausePresentations = new List<PausePassivePresentation>(MaxPassivePauseEntries);
        readonly List<PauseSynergyPresentation> _pauseSynergies = new List<PauseSynergyPresentation>(8);

        IPrefabFactory _cardFactory;
        PartyService _party;
        PassiveRosterState _passiveRoster;
        SynergyActivationState _synergies;
        IDataProvider _data;
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
            Func<float> selectedGameplaySpeed,
            PassiveRosterState passiveRoster,
            SynergyActivationState synergies,
            IDataProvider data)
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
            _passiveRoster = passiveRoster;
            _synergies = synergies;
            _data = data;
            if (_cardFactory == null || _party == null || _passiveRoster == null || _synergies == null || _data == null)
            {
                Debug.LogError("[GameplayUIController] Card factory, PartyService, PassiveRosterState, SynergyActivationState, and IDataProvider are required.", this);
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
            bool presented;
            if (data != null && data.IsClear)
                presented = _resultPopup.Present(data, primaryRequested, lobbyRequested);
            else if (optionalRequested != null)
                presented = _resultPopup.PresentFailureReviveChoice(data, primaryRequested, optionalRequested, lobbyRequested);
            else
                presented = _resultPopup.Present(data, primaryRequested, lobbyRequested);
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
                _hud.ShowPause(fromAppBackground, _companionPausePresentations, _passivePausePresentations, _pauseSynergies);
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
            PauseBuildSummaryPresentationResolver.Fill(
                _party.GetSquadSlotSnapshot(),
                _passiveRoster,
                _synergies,
                _data,
                _companionPausePresentations,
                _passivePausePresentations,
                _pauseSynergies,
                MaxCompanionPauseEntries,
                MaxPassivePauseEntries,
                this);
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
            _passiveRoster = null;
            _synergies = null;
            _data = null;
            _companionPausePresentations.Clear();
            _passivePausePresentations.Clear();
            _pauseSynergies.Clear();
        }
    }

    public static class PauseBuildSummaryPresentationResolver
    {
        public static void Fill(
            IReadOnlyList<SquadSlotState> squadSnapshot,
            PassiveRosterState passiveRoster,
            SynergyActivationState synergies,
            IDataProvider data,
            List<PauseCompanionPresentation> companions,
            List<PausePassivePresentation> passives,
            List<PauseSynergyPresentation> synergyPresentations,
            int maxCompanionEntries,
            int maxPassiveEntries,
            UnityEngine.Object context)
        {
            PauseCompanionPresentationResolver.Fill(squadSnapshot, companions, maxCompanionEntries, context);
            passives.Clear();
            synergyPresentations.Clear();

            if (passiveRoster != null && data != null)
            {
                IReadOnlyList<PassiveSlotState> snapshot = passiveRoster.Snapshot;
                for (int i = 0; i < snapshot.Count && passives.Count < maxPassiveEntries; i++)
                {
                    PassiveSlotState slot = snapshot[i];
                    if (slot == null || slot.IsEmpty)
                        continue;

                    if (data.GetPassive(slot.PassiveId) == null)
                    {
                        Debug.LogError($"[PauseBuildSummaryPresentationResolver] Missing passive data: {slot.PassiveId}", context);
                        passives.Add(new PausePassivePresentation(null));
                        continue;
                    }

                    if (TryGetPassiveCardKind(slot.PassiveId, out CardKind kind) == false
                        || PresentationCatalogProvider.TryGetCard(kind.ToString(), out CardPresentationSet.Entry entry) == false
                        || entry == null
                        || entry.Icon == null)
                    {
                        passives.Add(new PausePassivePresentation(null, slot.Level));
                        continue;
                    }

                    passives.Add(new PausePassivePresentation(entry.Icon, slot.Level));
                }
            }

            if (synergies == null || data == null)
                return;

            IReadOnlyList<SynergyActivationSnapshot> snapshots = synergies.Snapshot;
            for (int i = 0; i < snapshots.Count; i++)
            {
                SynergyActivationSnapshot snapshot = snapshots[i];
                if (snapshot.IsActive == false)
                    continue;

                SynergyData synergy = data.GetSynergy(snapshot.SynergyId);
                if (synergy == null || string.IsNullOrWhiteSpace(synergy.DisplayName))
                {
                    Debug.LogError($"[PauseBuildSummaryPresentationResolver] Missing synergy display data: {snapshot.SynergyId}", context);
                    continue;
                }

                synergyPresentations.Add(new PauseSynergyPresentation(snapshot.SynergyId, synergy.DisplayName));
            }
        }

        static bool TryGetPassiveCardKind(string passiveId, out CardKind kind)
        {
            int first = (int)CardKind.PassiveMeleeTraining;
            int last = (int)CardKind.PassiveSupplyPouch;
            for (int value = first; value <= last; value++)
            {
                CardKind candidate = (CardKind)value;
                if (CanonicalPassiveCardService.TryGetPassiveId(candidate, out string candidateId)
                    && candidateId == passiveId)
                {
                    kind = candidate;
                    return true;
                }
            }

            kind = default;
            return false;
        }
    }

    public static class PauseCompanionPresentationResolver
    {
        public static void Fill(
            IReadOnlyList<SquadSlotState> squadSnapshot,
            List<PauseCompanionPresentation> presentations,
            int maxEntries,
            UnityEngine.Object context)
        {
            presentations.Clear();
            if (squadSnapshot == null)
                return;

            for (int i = 0; i < squadSnapshot.Count && presentations.Count < maxEntries; i++)
            {
                SquadSlotState state = squadSnapshot[i];
                Sprite icon = null;
                if (state.IsActive == false)
                {
                    presentations.Add(new PauseCompanionPresentation(null, 0));
                    continue;
                }

                if (!PresentationCatalogProvider.TryGetUnit(state.BaseUnitId, out UnitPresentationSet.Entry entry))
                {
                    Debug.LogError($"[GameplayUIController] Missing UnitPresentationSet entry for active companion: {state.BaseUnitId} (roster slot: {state.SlotId})", context);
                }
                else if (entry.Portrait == null)
                {
                    Debug.LogError($"[GameplayUIController] Missing UnitPresentationSet Portrait for active companion: {state.BaseUnitId} (roster slot: {state.SlotId})", context);
                }
                else
                {
                    icon = entry.Portrait;
                }

                presentations.Add(new PauseCompanionPresentation(icon, state.CurrentCount));
            }
        }
    }
}
