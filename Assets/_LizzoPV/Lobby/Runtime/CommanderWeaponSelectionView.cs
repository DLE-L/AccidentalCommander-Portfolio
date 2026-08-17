using Lizzo.PV.Flow;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lizzo.PV.Lobby
{
    [DisallowMultipleComponent]
    public sealed class CommanderWeaponSelectionView : MonoBehaviour
    {
        [SerializeField] Button _rapidCrossbowButton;
        [SerializeField] Button _piercingSpearButton;
        [SerializeField] Button _blastStaffButton;
        [SerializeField] Button _sortieButton;
        [SerializeField] GameObject _rapidCrossbowSelectedFrame;
        [SerializeField] GameObject _piercingSpearSelectedFrame;
        [SerializeField] GameObject _blastStaffSelectedFrame;
        [SerializeField] TMP_Text _rapidCrossbowActionLabel;
        [SerializeField] TMP_Text _piercingSpearActionLabel;
        [SerializeField] TMP_Text _blastStaffActionLabel;
        CommanderWeaponId _selectedWeapon;

        public CommanderWeaponId SelectedWeapon => _selectedWeapon;
        public bool HasSelection => CommanderWeaponCatalog.IsSelectable(_selectedWeapon);

        void OnEnable()
        {
            Configure();
        }

        void OnDisable()
        {
            Unbind();
        }

        public bool Configure()
        {
            if (_rapidCrossbowButton == null ||
                _piercingSpearButton == null ||
                _blastStaffButton == null ||
                _sortieButton == null)
            {
                Debug.LogError("[CommanderWeaponSelectionView] Authored weapon and sortie button references are required.", this);
                return false;
            }

            Unbind();
            _selectedWeapon = CommanderWeaponId.None;
            _sortieButton.interactable = false;
            _rapidCrossbowButton.onClick.AddListener(SelectRapidCrossbow);
            _piercingSpearButton.onClick.AddListener(SelectPiercingSpear);
            _blastStaffButton.onClick.AddListener(SelectBlastStaff);
            _sortieButton.onClick.AddListener(LaunchSelectedWeapon);
            ApplySelectionPresentation(null);
            RestoreSavedSelection();
            return true;
        }

        void Unbind()
        {
            if (_rapidCrossbowButton != null)
                _rapidCrossbowButton.onClick.RemoveListener(SelectRapidCrossbow);
            if (_piercingSpearButton != null)
                _piercingSpearButton.onClick.RemoveListener(SelectPiercingSpear);
            if (_blastStaffButton != null)
                _blastStaffButton.onClick.RemoveListener(SelectBlastStaff);
            if (_sortieButton != null)
                _sortieButton.onClick.RemoveListener(LaunchSelectedWeapon);
        }

        void SelectRapidCrossbow() => Select(CommanderWeaponId.RapidCrossbow, _rapidCrossbowButton);
        void SelectPiercingSpear() => Select(CommanderWeaponId.PiercingSpear, _piercingSpearButton);
        void SelectBlastStaff() => Select(CommanderWeaponId.BlastStaff, _blastStaffButton);

        void RestoreSavedSelection()
        {
            if (CommanderWeaponPreferenceStore.TryLoad(out CommanderWeaponId weapon) == false)
                return;

            switch (weapon)
            {
                case CommanderWeaponId.RapidCrossbow:
                    SelectRapidCrossbow();
                    break;
                case CommanderWeaponId.PiercingSpear:
                    SelectPiercingSpear();
                    break;
                case CommanderWeaponId.BlastStaff:
                    SelectBlastStaff();
                    break;
            }
        }

        void Select(CommanderWeaponId weapon, Button selectedButton)
        {
            _selectedWeapon = weapon;
            _sortieButton.interactable = true;
            ApplySelectionPresentation(selectedButton);
            selectedButton.Select();
        }

        void ApplySelectionPresentation(Button selectedButton)
        {
            Button[] buttons = { _rapidCrossbowButton, _piercingSpearButton, _blastStaffButton };
            GameObject[] selectedFrames =
            {
                _rapidCrossbowSelectedFrame,
                _piercingSpearSelectedFrame,
                _blastStaffSelectedFrame
            };
            for (int index = 0; index < buttons.Length; index++)
            {
                if (selectedFrames[index] != null)
                    selectedFrames[index].SetActive(buttons[index] == selectedButton);
            }

            ApplyActionLabel(_rapidCrossbowActionLabel, _rapidCrossbowButton == selectedButton);
            ApplyActionLabel(_piercingSpearActionLabel, _piercingSpearButton == selectedButton);
            ApplyActionLabel(_blastStaffActionLabel, _blastStaffButton == selectedButton);

        }

        static void ApplyActionLabel(TMP_Text label, bool isSelected)
        {
            if (label != null)
                label.text = isSelected ? "선택됨" : "선택";
        }

        void LaunchSelectedWeapon()
        {
            if (HasSelection == false)
                return;

            GameFlowRoutes.LoadGameplay(_selectedWeapon);
        }
    }
}
