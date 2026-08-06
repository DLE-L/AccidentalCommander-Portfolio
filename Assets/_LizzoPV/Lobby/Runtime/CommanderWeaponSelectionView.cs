using Lizzo.PV.Flow;
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

        void Select(CommanderWeaponId weapon, Button selectedButton)
        {
            _selectedWeapon = weapon;
            _sortieButton.interactable = true;
            selectedButton.Select();
        }

        void LaunchSelectedWeapon()
        {
            if (HasSelection == false)
                return;

            GameFlowRoutes.LoadGameplay(_selectedWeapon);
        }
    }
}
