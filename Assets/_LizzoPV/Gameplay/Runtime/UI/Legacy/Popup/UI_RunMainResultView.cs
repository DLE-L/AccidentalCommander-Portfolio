using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lizzo.PV.UI
{
    public sealed class UI_RunMainResultView : MonoBehaviour
    {
        private const int KpiCount = 4;
        private const int LegionSlotCount = 7;
        private const int GradeGemCount = 3;
        private const int SynergyMemberCount = 3;

        [Serializable]
        public sealed class KpiBinding
        {
            [SerializeField] private TMP_Text _labelText;
            [SerializeField] private TMP_Text _valueText;

            public TMP_Text LabelText => _labelText;
            public TMP_Text ValueText => _valueText;
        }

        [Serializable]
        public sealed class LegionSlotBinding
        {
            [SerializeField] private GameObject _root;
            [SerializeField] private GameObject _visual;
            [SerializeField] private Image _highlight;
            [SerializeField] private Image _icon;
            [SerializeField] private GameObject[] _gradeGems;

            public GameObject Root => _root;
            public GameObject Visual => _visual;
            public Image Highlight => _highlight;
            public Image Icon => _icon;
            public GameObject[] GradeGems => _gradeGems;
        }

        [Header("Main Result")]
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _stageText;
        [SerializeField] private KpiBinding[] _kpiItems = new KpiBinding[KpiCount];
        [SerializeField] private TMP_Text _synergySectionLabelText;
        [SerializeField] private TMP_Text _synergyNameText;
        [SerializeField] private Image[] _synergyMemberIcons = new Image[SynergyMemberCount];
        [SerializeField] private GameObject _synergyMembersRoot;
        [SerializeField] private TMP_Text _synergyMembersText;
        [SerializeField] private GameObject _synergyEffectRoot;
        [SerializeField] private TMP_Text _synergyEffectText;
        [SerializeField] private TMP_Text _finalLegionHeaderText;
        [SerializeField] private LegionSlotBinding[] _legionSlots = new LegionSlotBinding[LegionSlotCount];
        [SerializeField] private Sprite[] _legionIconSprites = new Sprite[LegionSlotCount];
        [SerializeField] private Button _damageStatisticsButton;
        [SerializeField] private TMP_Text _damageStatisticsButtonText;
        [SerializeField] private Button _primaryButton;
        [SerializeField] private TMP_Text _primaryButtonText;
        [SerializeField] private Button _lobbyButton;
        [SerializeField] private TMP_Text _lobbyButtonText;

        public bool IsShowing => gameObject.activeSelf;

        public bool Configure()
        {
            if (_titleText == null
                || _stageText == null
                || _kpiItems == null
                || _kpiItems.Length != KpiCount
                || HasInvalidKpiBinding()
                || _synergySectionLabelText == null
                || _synergyNameText == null
                || _synergyMemberIcons == null
                || _synergyMemberIcons.Length != SynergyMemberCount
                || _synergyMembersRoot == null
                || _synergyMembersText == null
                || _synergyEffectRoot == null
                || _synergyEffectText == null
                || _finalLegionHeaderText == null
                || _legionSlots == null
                || _legionSlots.Length != LegionSlotCount
                || HasInvalidLegionSlotBinding()
                || _legionIconSprites == null
                || _legionIconSprites.Length != LegionSlotCount
                || _damageStatisticsButton == null
                || _damageStatisticsButtonText == null
                || _primaryButton == null
                || _primaryButtonText == null
                || _lobbyButton == null
                || _lobbyButtonText == null)
            {
                Debug.LogError("[Result] Authored main result references are required.", this);
                return false;
            }

            return true;
        }

        public bool Present(
            RunResultViewData view,
            Action primaryRequested,
            Action lobbyRequested)
        {
            if (view == null || !view.IsClear || !Configure())
                return false;

            gameObject.SetActive(true);
            _titleText.text = view.Title;
            _stageText.text = view.StageLabel;
            SetKpi("스테이지", view.StageLabel, 0);
            SetKpi("시간", FormatElapsed(view.ElapsedSeconds), 1);
            SetKpi("처치", Mathf.Max(0, view.KillCount).ToString(), 2);
            SetKpi("최종 빌드", view.IsFinalBuildComplete ? "완성" : "진행 중", 3);

            _synergySectionLabelText.text = string.IsNullOrWhiteSpace(view.SynergySectionLabel)
                ? "이번 클리어 우수 시너지"
                : view.SynergySectionLabel;
            _synergyNameText.text = view.BestActiveSynergy?.DisplayName ?? "우수 시너지 없음";
            _synergyMembersRoot.SetActive(false);
            _synergyEffectRoot.SetActive(false);
            _synergyMembersText.text = string.Empty;
            _synergyEffectText.text = string.Empty;
            for (int i = 0; i < _synergyMemberIcons.Length; i++)
            {
                _synergyMemberIcons[i].sprite = null;
                _synergyMemberIcons[i].gameObject.SetActive(false);
            }

            int activeDistinct = 0;
            for (int i = 0; i < _legionSlots.Length; i++)
            {
                LegionSlotBinding binding = _legionSlots[i];
                RunResultSquadSlotView slot = view.SquadSlots != null && i < view.SquadSlots.Count
                    ? view.SquadSlots[i]
                    : null;
                binding.Root.SetActive(true);
                binding.Visual.SetActive(true);
                Sprite icon = slot == null ? null : ResolveIcon(slot.IconIndex);
                binding.Icon.sprite = icon;
                binding.Icon.gameObject.SetActive(slot != null && slot.IsActive && icon != null);
                binding.Highlight.gameObject.SetActive(slot != null && slot.IsHighlighted);
                int grade = slot == null ? 0 : slot.CurrentCount;
                for (int gemIndex = 0; gemIndex < binding.GradeGems.Length; gemIndex++)
                    binding.GradeGems[gemIndex].SetActive(gemIndex < grade);
                if (slot != null && slot.IsActive)
                    activeDistinct++;
            }

            _finalLegionHeaderText.text = $"최종 군단  {activeDistinct}/{LegionSlotCount}";
            _damageStatisticsButtonText.text = "대미지 통계";
            _damageStatisticsButton.interactable = false;
            _damageStatisticsButton.onClick.RemoveAllListeners();
            _primaryButtonText.text = view.PrimaryButtonLabel;
            _lobbyButtonText.text = "로비로";
            _primaryButton.onClick.RemoveAllListeners();
            _primaryButton.onClick.AddListener(() => primaryRequested?.Invoke());
            _lobbyButton.onClick.RemoveAllListeners();
            _lobbyButton.onClick.AddListener(() => lobbyRequested?.Invoke());
            return true;
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void SetKpi(string label, string value, int index)
        {
            _kpiItems[index].LabelText.text = label;
            _kpiItems[index].ValueText.text = value;
        }

        private Sprite ResolveIcon(IReadOnlyList<int> iconIndices, int memberIndex)
        {
            if (iconIndices == null || memberIndex >= iconIndices.Count)
                return null;

            return ResolveIcon(iconIndices[memberIndex]);
        }

        private Sprite ResolveIcon(int iconIndex)
        {
            return iconIndex >= 0 && iconIndex < _legionIconSprites.Length
                ? _legionIconSprites[iconIndex]
                : null;
        }

        private bool HasInvalidKpiBinding()
        {
            for (int i = 0; i < _kpiItems.Length; i++)
            {
                if (_kpiItems[i] == null
                    || _kpiItems[i].LabelText == null
                    || _kpiItems[i].ValueText == null)
                    return true;
            }

            return false;
        }

        private bool HasInvalidLegionSlotBinding()
        {
            for (int i = 0; i < _legionSlots.Length; i++)
            {
                LegionSlotBinding slot = _legionSlots[i];
                if (slot == null
                    || slot.Root == null
                    || slot.Visual == null
                    || slot.Highlight == null
                    || slot.Icon == null
                    || slot.GradeGems == null
                    || slot.GradeGems.Length != GradeGemCount)
                {
                    return true;
                }
            }

            return false;
        }

        private static string FormatElapsed(float elapsedSeconds)
        {
            int totalSeconds = Mathf.Max(0, Mathf.RoundToInt(elapsedSeconds));
            return $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
        }
    }
}
