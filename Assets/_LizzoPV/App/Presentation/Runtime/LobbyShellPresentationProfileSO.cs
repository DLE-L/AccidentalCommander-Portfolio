using System;
using System.Collections.Generic;
using Lizzo.PV.Lobby;
using UnityEngine;

namespace Lizzo.PV.Presentation
{
    [CreateAssetMenu(menuName = "Lizzo PV/Presentation/Lobby/Lobby Shell Profile", fileName = "LobbyShellPresentationProfile")]
    public sealed class LobbyShellPresentationProfileSO : ScriptableObject
    {
        [SerializeField] private AudioAssetId _lobbyBgmId;
        [SerializeField] private List<LobbyTabPresentationBinding> _tabBindings = new List<LobbyTabPresentationBinding>();
        [SerializeField] private SpriteAssetId _lockedToastSpriteId;
        [SerializeField] private SpriteAssetId _lockedBadgeSpriteId;
        [SerializeField] private LocalizationKey _lockedToastLocalizationKey;
        [SerializeField] private AudioAssetId _lockedFeatureSfxId;
        [SerializeField] private MotionAssetId _toastEnterMotionId;
        [SerializeField] private MotionAssetId _toastExitMotionId;
        [SerializeField] private AudioAssetId _tabSelectedSfxId;

        public AudioAssetId LobbyBgmId => _lobbyBgmId;
        public IReadOnlyList<LobbyTabPresentationBinding> TabBindings => _tabBindings;
        public SpriteAssetId LockedToastSpriteId => _lockedToastSpriteId;
        public SpriteAssetId LockedBadgeSpriteId => _lockedBadgeSpriteId;
        public LocalizationKey LockedToastLocalizationKey => _lockedToastLocalizationKey;
        public AudioAssetId LockedFeatureSfxId => _lockedFeatureSfxId;
        public MotionAssetId ToastEnterMotionId => _toastEnterMotionId;
        public MotionAssetId ToastExitMotionId => _toastExitMotionId;
        public AudioAssetId TabSelectedSfxId => _tabSelectedSfxId;

        public bool TryGetTab(LobbySection section, out LobbyTabPresentationBinding binding)
        {
            for (int i = 0; i < _tabBindings.Count; i++)
            {
                if (_tabBindings[i].Section == section)
                {
                    binding = _tabBindings[i];
                    return true;
                }
            }

            binding = default;
            return false;
        }

        public bool TryValidate(out string issue)
        {
            if (!LobbyProfileValidation.Require(_lobbyBgmId, nameof(LobbyBgmId), out issue)
                || !LobbyProfileValidation.Require(_lockedBadgeSpriteId, nameof(LockedBadgeSpriteId), out issue)
                || !LobbyProfileValidation.Require(_lockedToastLocalizationKey, nameof(LockedToastLocalizationKey), out issue)
                || !LobbyProfileValidation.Require(_lockedFeatureSfxId, nameof(LockedFeatureSfxId), out issue)
                || !LobbyProfileValidation.Require(_toastEnterMotionId, nameof(ToastEnterMotionId), out issue)
                || !LobbyProfileValidation.Require(_toastExitMotionId, nameof(ToastExitMotionId), out issue)
                || !LobbyProfileValidation.Require(_tabSelectedSfxId, nameof(TabSelectedSfxId), out issue))
                return false;

            var seen = new HashSet<LobbySection>();
            int selectedCount = 0;
            for (int i = 0; i < _tabBindings.Count; i++)
            {
                LobbyTabPresentationBinding binding = _tabBindings[i];
                if (!seen.Add(binding.Section))
                {
                    issue = $"Duplicate LobbySection binding: {binding.Section}.";
                    return false;
                }

                if (!LobbyProfileValidation.Require(binding.IconSpriteId, $"TabBindings[{binding.Section}].IconSpriteId", out issue)
                    || !LobbyProfileValidation.Require(binding.SelectedMotionId, $"TabBindings[{binding.Section}].SelectedMotionId", out issue))
                    return false;

                if (binding.InitialState == LobbyTabInitialState.Selected)
                {
                    selectedCount++;
                    if (binding.Section != LobbySection.Departure)
                    {
                        issue = "The lobby requires Departure as the selected LobbySection.";
                        return false;
                    }
                }
            }

            foreach (LobbySection section in Enum.GetValues(typeof(LobbySection)))
            {
                if (!seen.Contains(section))
                {
                    issue = $"Missing required LobbySection binding: {section}.";
                    return false;
                }
            }

            if (selectedCount != 1)
            {
                issue = "The lobby requires exactly one selected LobbySection.";
                return false;
            }

            issue = string.Empty;
            return true;
        }

#if UNITY_EDITOR
        public void SetForEditor(AudioAssetId lobbyBgmId, IEnumerable<LobbyTabPresentationBinding> tabBindings, SpriteAssetId lockedToastSpriteId, SpriteAssetId lockedBadgeSpriteId, LocalizationKey lockedToastLocalizationKey, AudioAssetId lockedFeatureSfxId, MotionAssetId toastEnterMotionId, MotionAssetId toastExitMotionId, AudioAssetId tabSelectedSfxId)
        {
            _lobbyBgmId = lobbyBgmId;
            _tabBindings = tabBindings == null ? new List<LobbyTabPresentationBinding>() : new List<LobbyTabPresentationBinding>(tabBindings);
            _lockedToastSpriteId = lockedToastSpriteId;
            _lockedBadgeSpriteId = lockedBadgeSpriteId;
            _lockedToastLocalizationKey = lockedToastLocalizationKey;
            _lockedFeatureSfxId = lockedFeatureSfxId;
            _toastEnterMotionId = toastEnterMotionId;
            _toastExitMotionId = toastExitMotionId;
            _tabSelectedSfxId = tabSelectedSfxId;
        }
#endif
    }
}
