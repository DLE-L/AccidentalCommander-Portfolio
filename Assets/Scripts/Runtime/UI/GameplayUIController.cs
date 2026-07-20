using Lizzo.PV.Flow;
using Lizzo.PV.Legion;
using System;
using UnityEngine;

namespace Lizzo.PV.UI
{
    public sealed class GameplayUIController : MonoBehaviour
    {
        [Header("Gameplay UI")]
        [SerializeField] global::UI_GameScene _hud;
        [SerializeField] global::UI_Joystick _joystick;
        [SerializeField] global::UI_SkillSelectPopup _skillSelectPopup;
        [SerializeField] global::UI_GameResultPopup _resultPopup;

        IPrefabFactory _cardFactory;
        bool _initialized;
        global::UI_Base _activeModal;

        public global::UI_GameScene Hud => _hud;
        public event Action<bool> ModalChanged;

        public bool Initialize(IPrefabFactory cardFactory, PartyService party, Action pauseRequested, Action resumeRequested, Func<bool> speedToggleRequested, Func<float> selectedGameplaySpeed)
        {
            if (_initialized)
                return true;

            if (_hud == null || _joystick == null || _skillSelectPopup == null || _resultPopup == null)
            {
                Debug.LogError("[GameplayUIController] Authored HUD, Joystick, SkillSelectPopup, and ResultPopup references are required.", this);
                return false;
            }

            _cardFactory = cardFactory;
            if (_cardFactory == null)
            {
                Debug.LogError("[GameplayUIController] Card factory is required.", this);
                return false;
            }

            if (!_hud.ConfigureParty(party))
                return false;
            if (!_hud.ConfigurePauseCallbacks(pauseRequested, resumeRequested))
                return false;
            if (!_hud.ConfigureGameplaySpeedCallbacks(speedToggleRequested, selectedGameplaySpeed))
                return false;
            if (!_skillSelectPopup.Configure(_cardFactory, party, CloseModal))
                return false;
            if (!_resultPopup.Configure())
                return false;

            _hud.Init();
            _joystick.Init();
            _skillSelectPopup.Init();
            _resultPopup.Init();

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
        }

        public void ShowSkillSelection()
        {
            EnsureInitialized();
            CloseActiveModal();
            _activeModal = _skillSelectPopup;
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
            ModalChanged?.Invoke(false);
        }

        public void SetPauseOverlay(bool visible, bool fromAppBackground)
        {
            EnsureInitialized();
            _hud.ShowPauseOverlay(visible, fromAppBackground);
        }

        public void SetGameplaySpeed(float speed)
        {
            EnsureInitialized();
            _hud.SetGameplaySpeed(speed);
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
            ModalChanged?.Invoke(false);
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
        }
    }
}
