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
            _rapidCrossbowButton.interactable = false;
            _piercingSpearButton.interactable = false;
            _blastStaffButton.interactable = false;
            _sortieButton.interactable = true;
            _sortieButton.onClick.AddListener(LaunchGameplay);
            ResetWeaponPresentation();
            return true;
        }

        void Unbind()
        {
            if (_sortieButton != null)
                _sortieButton.onClick.RemoveListener(LaunchGameplay);
        }

        void ResetWeaponPresentation()
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
                    selectedFrames[index].SetActive(false);
            }

            SetCardTransform(buttons[0], -_sideCardX, _sideCardY, 1f, 0);
            SetCardTransform(buttons[1], 0f, _sideCardY, 1f, 1);
            SetCardTransform(buttons[2], _sideCardX, _sideCardY, 1f, 2);
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

        void LaunchGameplay()
        {
            GameFlowRoutes.LoadGameplay();
        }
    }
}
