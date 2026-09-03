using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lizzo.PV.Presentation
{
    [Serializable]
    public struct ContentSpriteBinding
    {
        [SerializeField] private string _gameDataId;
        [SerializeField] private SpriteAssetId _spriteId;

        public ContentSpriteBinding(string gameDataId, SpriteAssetId spriteId)
        {
            _gameDataId = gameDataId;
            _spriteId = spriteId;
        }

        public string GameDataId => _gameDataId;
        public SpriteAssetId SpriteId => _spriteId;
    }

    [Serializable]
    public struct CardSynergySpriteBinding
    {
        [SerializeField] private string _cardGameDataId;
        [SerializeField] private string _synergyId;
        [SerializeField] private SpriteAssetId _spriteId;

        public CardSynergySpriteBinding(string cardGameDataId, string synergyId, SpriteAssetId spriteId)
        {
            _cardGameDataId = cardGameDataId;
            _synergyId = synergyId;
            _spriteId = spriteId;
        }

        public string CardGameDataId => _cardGameDataId;
        public string SynergyId => _synergyId;
        public SpriteAssetId SpriteId => _spriteId;
    }

    [CreateAssetMenu(menuName = "Lizzo PV/Presentation/Gameplay/Content Sprite Profile", fileName = "GameplayContentSpriteProfile")]
    public sealed class GameplayContentSpriteProfileSO : ScriptableObject
    {
        [SerializeField] private ContentSpriteBinding[] _cardPortraits = Array.Empty<ContentSpriteBinding>();
        [SerializeField] private CardSynergySpriteBinding[] _cardSynergyIcons = Array.Empty<CardSynergySpriteBinding>();
        [SerializeField] private ContentSpriteBinding[] _notificationSynergyIcons = Array.Empty<ContentSpriteBinding>();
        [SerializeField] private ContentSpriteBinding[] _buildSummaryCompanionIcons = Array.Empty<ContentSpriteBinding>();
        [SerializeField] private ContentSpriteBinding[] _buildSummaryPassiveIcons = Array.Empty<ContentSpriteBinding>();
        [SerializeField] private ContentSpriteBinding[] _buildSummarySynergyIcons = Array.Empty<ContentSpriteBinding>();
        [SerializeField] private ContentSpriteBinding[] _rewardIcons = Array.Empty<ContentSpriteBinding>();

        public bool TryGetCardPortrait(string gameDataId, out SpriteAssetId id) => TryGet(_cardPortraits, gameDataId, out id);

        public bool TryGetCardSynergy(string cardGameDataId, out string synergyId, out SpriteAssetId id)
        {
            if (string.IsNullOrWhiteSpace(cardGameDataId) == false && _cardSynergyIcons != null)
            {
                for (int index = 0; index < _cardSynergyIcons.Length; index++)
                {
                    CardSynergySpriteBinding binding = _cardSynergyIcons[index];
                    if (binding.CardGameDataId == cardGameDataId)
                    {
                        synergyId = binding.SynergyId;
                        id = binding.SpriteId;
                        return true;
                    }
                }
            }

            synergyId = string.Empty;
            id = SpriteAssetId.None;
            return false;
        }

        public bool TryGetNotificationSynergyIcon(string synergyId, out SpriteAssetId id) => TryGet(_notificationSynergyIcons, synergyId, out id);
        public bool TryGetBuildSummaryCompanionIcon(string companionId, out SpriteAssetId id) => TryGet(_buildSummaryCompanionIcons, companionId, out id);
        public bool TryGetBuildSummaryPassiveIcon(string passiveId, out SpriteAssetId id) => TryGet(_buildSummaryPassiveIcons, passiveId, out id);
        public bool TryGetBuildSummarySynergyIcon(string synergyId, out SpriteAssetId id) => TryGet(_buildSummarySynergyIcons, synergyId, out id);
        public bool TryGetRewardIcon(string rewardId, out SpriteAssetId id) => TryGet(_rewardIcons, rewardId, out id);

        public bool TryValidate(out string issue)
        {
            if (!Validate(_cardPortraits, nameof(_cardPortraits), out issue)
                || !ValidateCardSynergies(out issue)
                || !Validate(_notificationSynergyIcons, nameof(_notificationSynergyIcons), out issue)
                || !Validate(_buildSummaryCompanionIcons, nameof(_buildSummaryCompanionIcons), out issue)
                || !Validate(_buildSummaryPassiveIcons, nameof(_buildSummaryPassiveIcons), out issue)
                || !Validate(_buildSummarySynergyIcons, nameof(_buildSummarySynergyIcons), out issue)
                || !Validate(_rewardIcons, nameof(_rewardIcons), out issue))
                return false;

            issue = string.Empty;
            return true;
        }

        private static bool TryGet(ContentSpriteBinding[] bindings, string gameDataId, out SpriteAssetId id)
        {
            if (string.IsNullOrWhiteSpace(gameDataId) == false && bindings != null)
            {
                for (int index = 0; index < bindings.Length; index++)
                {
                    if (bindings[index].GameDataId == gameDataId)
                    {
                        id = bindings[index].SpriteId;
                        return true;
                    }
                }
            }

            id = SpriteAssetId.None;
            return false;
        }

        private static bool Validate(ContentSpriteBinding[] bindings, string field, out string issue)
        {
            if (bindings == null || bindings.Length == 0)
            {
                issue = $"{field} requires at least one binding.";
                return false;
            }

            var keys = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < bindings.Length; index++)
            {
                ContentSpriteBinding binding = bindings[index];
                if (string.IsNullOrWhiteSpace(binding.GameDataId) || !keys.Add(binding.GameDataId))
                {
                    issue = $"{field}[{index}] requires a unique GameData ID.";
                    return false;
                }
                if (binding.SpriteId.IsNone)
                {
                    issue = $"{field}[{binding.GameDataId}] requires a Sprite Asset ID.";
                    return false;
                }
            }

            issue = string.Empty;
            return true;
        }

        private bool ValidateCardSynergies(out string issue)
        {
            if (_cardSynergyIcons == null || _cardSynergyIcons.Length == 0)
            {
                issue = "Card synergy icons require at least one binding.";
                return false;
            }

            var keys = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < _cardSynergyIcons.Length; index++)
            {
                CardSynergySpriteBinding binding = _cardSynergyIcons[index];
                if (string.IsNullOrWhiteSpace(binding.CardGameDataId)
                    || string.IsNullOrWhiteSpace(binding.SynergyId)
                    || binding.SpriteId.IsNone
                    || !keys.Add(binding.CardGameDataId))
                {
                    issue = $"Card synergy binding {index} requires unique Card GameData ID, Synergy ID, and Sprite Asset ID.";
                    return false;
                }
            }

            issue = string.Empty;
            return true;
        }

#if UNITY_EDITOR
        public void SetForEditor(
            ContentSpriteBinding[] cardPortraits,
            CardSynergySpriteBinding[] cardSynergyIcons,
            ContentSpriteBinding[] notificationSynergyIcons,
            ContentSpriteBinding[] buildSummaryCompanionIcons,
            ContentSpriteBinding[] buildSummaryPassiveIcons,
            ContentSpriteBinding[] buildSummarySynergyIcons,
            ContentSpriteBinding[] rewardIcons)
        {
            _cardPortraits = cardPortraits ?? Array.Empty<ContentSpriteBinding>();
            _cardSynergyIcons = cardSynergyIcons ?? Array.Empty<CardSynergySpriteBinding>();
            _notificationSynergyIcons = notificationSynergyIcons ?? Array.Empty<ContentSpriteBinding>();
            _buildSummaryCompanionIcons = buildSummaryCompanionIcons ?? Array.Empty<ContentSpriteBinding>();
            _buildSummaryPassiveIcons = buildSummaryPassiveIcons ?? Array.Empty<ContentSpriteBinding>();
            _buildSummarySynergyIcons = buildSummarySynergyIcons ?? Array.Empty<ContentSpriteBinding>();
            _rewardIcons = rewardIcons ?? Array.Empty<ContentSpriteBinding>();
        }
#endif
    }
}
