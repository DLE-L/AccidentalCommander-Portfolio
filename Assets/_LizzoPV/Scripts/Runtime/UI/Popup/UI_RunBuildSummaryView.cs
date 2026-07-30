using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lizzo.PV.UI
{
    public sealed class UI_RunBuildSummaryView : MonoBehaviour
    {
        private const int CompanionCount = 7;
        private const int PassiveCount = 5;

        [Serializable]
        public sealed class CompanionSlotBinding
        {
            [SerializeField] private GameObject _root;
            [SerializeField] private GameObject _activeVisual;
            [SerializeField] private GameObject _emptyVisual;
            [SerializeField] private Image _icon;
            [SerializeField] private TMP_Text _countText;

            public GameObject Root => _root;
            public GameObject ActiveVisual => _activeVisual;
            public GameObject EmptyVisual => _emptyVisual;
            public Image Icon => _icon;
            public TMP_Text CountText => _countText;
        }

        [Serializable]
        public sealed class PassiveSlotBinding
        {
            [SerializeField] private GameObject _root;
            [SerializeField] private GameObject _activeVisual;
            [SerializeField] private GameObject _emptyVisual;
            [SerializeField] private Image _icon;
            [SerializeField] private TMP_Text _levelText;

            public GameObject Root => _root;
            public GameObject ActiveVisual => _activeVisual;
            public GameObject EmptyVisual => _emptyVisual;
            public Image Icon => _icon;
            public TMP_Text LevelText => _levelText;
        }

        [Header("Canonical Build Summary")]
        [SerializeField] private CompanionSlotBinding[] _companionSlots = new CompanionSlotBinding[CompanionCount];
        [SerializeField] private PassiveSlotBinding[] _passiveSlots = new PassiveSlotBinding[PassiveCount];
        [SerializeField] private TMP_Text _synergySummaryText;

        public bool Configure()
        {
            return ConfigureForFailure();
        }

        public bool ConfigureForClear()
        {
            return Configure(false);
        }

        public bool ConfigureForFailure()
        {
            return Configure(true);
        }

        private bool Configure(bool requirePassiveAuthoring)
        {
            if (_companionSlots == null || _companionSlots.Length != CompanionCount
                || _synergySummaryText == null
                || HasInvalidCompanionBinding()
                || (requirePassiveAuthoring
                    && (_passiveSlots == null || _passiveSlots.Length != PassiveCount
                        || HasInvalidPassiveBinding())))
            {
                Debug.LogError("[Result] Authored canonical build summary references are required.", this);
                return false;
            }

            return true;
        }

        public bool Present(RunResultViewData view)
        {
            if (view == null || !(view.IsClear ? ConfigureForClear() : ConfigureForFailure()))
                return false;

            PresentCompanions(view.CompanionPresentations);
            if (!view.IsClear)
                PresentPassives(view.PassivePresentations);
            PresentSynergies(view);
            return true;
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void PresentCompanions(IReadOnlyList<PauseCompanionPresentation> values)
        {
            for (int i = 0; i < _companionSlots.Length; i++)
            {
                CompanionSlotBinding binding = _companionSlots[i];
                bool active = values != null && i < values.Count && values[i].CurrentCount > 0;
                Sprite icon = active ? values[i].Icon : null;
                if (active && icon == null)
                    Debug.LogError($"[Result] Missing companion icon for summary slot {i}", this);
                binding.Root.SetActive(true);
                binding.ActiveVisual.SetActive(active);
                binding.EmptyVisual.SetActive(!active);
                binding.Icon.sprite = icon;
                binding.Icon.gameObject.SetActive(active && icon != null);
                binding.CountText.text = active && icon != null ? $"x{values[i].CurrentCount}" : string.Empty;
                binding.CountText.gameObject.SetActive(active && icon != null);
            }
        }

        private void PresentPassives(IReadOnlyList<PausePassivePresentation> values)
        {
            for (int i = 0; i < _passiveSlots.Length; i++)
            {
                PassiveSlotBinding binding = _passiveSlots[i];
                bool active = values != null && i < values.Count && values[i].Level > 0;
                Sprite icon = active ? values[i].Icon : null;
                if (active && icon == null)
                    Debug.LogError($"[Result] Missing passive icon for summary slot {i}", this);
                binding.Root.SetActive(true);
                binding.ActiveVisual.SetActive(active);
                binding.EmptyVisual.SetActive(!active);
                binding.Icon.sprite = icon;
                binding.Icon.gameObject.SetActive(active && icon != null);
                binding.LevelText.text = active && icon != null ? $"Lv.{values[i].Level}" : string.Empty;
                binding.LevelText.gameObject.SetActive(active && icon != null);
            }
        }

        private void PresentSynergies(RunResultViewData view)
        {
            if (view.IsClear)
            {
                _synergySummaryText.text = view.BestActiveSynergy?.DisplayName ?? "우수 시너지 없음";
                return;
            }

            PresentSynergies(view.SynergyPresentations);
        }

        private void PresentSynergies(IReadOnlyList<PauseSynergyPresentation> values)
        {
            StringBuilder text = new StringBuilder(64);
            if (values != null)
            {
                for (int i = 0; i < values.Count; i++)
                {
                    string displayName = values[i].DisplayName;
                    if (string.IsNullOrWhiteSpace(displayName))
                    {
                        Debug.LogError($"[Result] Missing synergy display name: {values[i].Id}", this);
                        continue;
                    }

                    if (text.Length > 0)
                        text.Append(" · ");
                    text.Append(displayName);
                }
            }

            _synergySummaryText.text = text.Length == 0 ? "활성 시너지 없음" : text.ToString();
        }

        private bool HasInvalidCompanionBinding()
        {
            for (int i = 0; i < _companionSlots.Length; i++)
            {
                CompanionSlotBinding binding = _companionSlots[i];
                if (binding == null || binding.Root == null || binding.ActiveVisual == null
                    || binding.EmptyVisual == null || binding.Icon == null || binding.CountText == null)
                    return true;
            }

            return false;
        }

        private bool HasInvalidPassiveBinding()
        {
            for (int i = 0; i < _passiveSlots.Length; i++)
            {
                PassiveSlotBinding binding = _passiveSlots[i];
                if (binding == null || binding.Root == null || binding.ActiveVisual == null
                    || binding.EmptyVisual == null || binding.Icon == null || binding.LevelText == null)
                    return true;
            }

            return false;
        }
    }
}
