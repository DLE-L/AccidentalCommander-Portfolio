using System;
using Lizzo.PV.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lizzo.PV.Gameplay.Result
{
    [DisallowMultipleComponent]
    public sealed class GameplayRunResultPopupView : MonoBehaviour
    {
        private const int RewardItemCount = 2;

        [Serializable]
        public sealed class RewardBinding
        {
            [SerializeField] private GameObject _root;
            [SerializeField] private TMP_Text _labelText;
            [SerializeField] private TMP_Text _valueText;
            [SerializeField] private Image _icon;

            public GameObject Root => _root;
            public TMP_Text LabelText => _labelText;
            public TMP_Text ValueText => _valueText;
            public Image Icon => _icon;
        }

        [SerializeField] private TMP_Text _stageGroupText;
        [SerializeField] private TMP_Text _stageNameText;
        [SerializeField] private TMP_Text _resultTitleText;
        [SerializeField] private RewardBinding[] _rewardItems = new RewardBinding[RewardItemCount];
        [SerializeField] private Button _mainButton;
        [SerializeField] private TMP_Text _mainButtonText;

        public bool IsShowing => gameObject.activeSelf;

        public bool Configure()
        {
            if (_stageGroupText == null
                || _stageNameText == null
                || _resultTitleText == null
                || _rewardItems == null
                || _rewardItems.Length != RewardItemCount
                || HasInvalidRewardBinding()
                || _mainButton == null
                || _mainButtonText == null)
            {
                Debug.LogError("[Result] Authored common result popup references are required.", this);
                return false;
            }

            return true;
        }

        public bool Present(RunResultViewData view, Action mainRequested)
        {
            if (view == null || !Configure())
                return false;

            gameObject.SetActive(true);
            _stageGroupText.text = view.StageGroupLabel;
            _stageNameText.text = view.StageNameLabel;
            _resultTitleText.text = view.Title;

            for (int index = 0; index < _rewardItems.Length; index++)
            {
                RewardBinding binding = _rewardItems[index];
                RunResultRewardPresentation reward = view.RewardPresentations != null
                    && index < view.RewardPresentations.Count
                    ? view.RewardPresentations[index]
                    : null;
                bool visible = reward != null;
                binding.Root.SetActive(visible);
                binding.LabelText.text = visible ? reward.DisplayName : string.Empty;
                binding.ValueText.text = visible ? $"x{reward.Amount}" : string.Empty;
                binding.Icon.sprite = visible ? reward.Icon : null;
                binding.Icon.enabled = visible && reward.Icon != null;
                binding.Icon.raycastTarget = false;
            }

            _mainButtonText.text = "메인으로";
            _mainButton.interactable = true;
            _mainButton.onClick.RemoveAllListeners();
            _mainButton.onClick.AddListener(() =>
            {
                _mainButton.interactable = false;
                mainRequested?.Invoke();
            });
            return true;
        }

        public void Hide()
        {
            if (_mainButton != null)
                _mainButton.onClick.RemoveAllListeners();
            gameObject.SetActive(false);
        }

        private bool HasInvalidRewardBinding()
        {
            for (int index = 0; index < _rewardItems.Length; index++)
            {
                RewardBinding binding = _rewardItems[index];
                if (binding == null
                    || binding.Root == null
                    || binding.LabelText == null
                    || binding.ValueText == null
                    || binding.Icon == null)
                    return true;
            }

            return false;
        }
    }
}
