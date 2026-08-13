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
        [SerializeField] GameObject _rapidCrossbowSelectedFrame;
        [SerializeField] GameObject _piercingSpearSelectedFrame;
        [SerializeField] GameObject _blastStaffSelectedFrame;
        [SerializeField] float _sideCardX = 250f;
        [SerializeField] float _sideCardY = -470f;
        [SerializeField] float _selectedCardY = -445f;
        [SerializeField] float _selectedCardScale = 1.22f;

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
            int selectedIndex = -1;
            for (int index = 0; index < buttons.Length; index++)
            {
                if (buttons[index] == selectedButton)
                    selectedIndex = index;

                if (selectedFrames[index] != null)
                    selectedFrames[index].SetActive(buttons[index] == selectedButton);
            }

            if (selectedIndex < 0)
            {
                SetCardTransform(buttons[0], -_sideCardX, _sideCardY, 1f, 0);
                SetCardTransform(buttons[1], 0f, _sideCardY, 1f, 1);
                SetCardTransform(buttons[2], _sideCardX, _sideCardY, 1f, 2);
                return;
            }

            int sideSlot = 0;
            for (int index = 0; index < buttons.Length; index++)
            {
                if (index == selectedIndex)
                    continue;

                float x = sideSlot == 0 ? -_sideCardX : _sideCardX;
                SetCardTransform(buttons[index], x, _sideCardY, 1f, sideSlot);
                sideSlot++;
            }

            SetCardTransform(selectedButton, 0f, _selectedCardY, _selectedCardScale, 2);
        }

        static void SetCardTransform(Button button, float x, float y, float scale, int siblingIndex)
        {
            RectTransform rect = button.transform as RectTransform;
            if (rect == null)
                return;

            rect.anchoredPosition = new Vector2(x, y);
            rect.localScale = new Vector3(scale, scale, 1f);
            rect.SetSiblingIndex(siblingIndex);
        }

        void LaunchSelectedWeapon()
        {
            if (HasSelection == false)
                return;

            GameFlowRoutes.LoadGameplay(_selectedWeapon);
        }
    }
}
