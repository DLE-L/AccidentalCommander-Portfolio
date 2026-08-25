using System;

namespace Lizzo.PV.Flow
{
    public enum AchievementCategory
    {
        Progression,
        Legion,
        Synergy,
        Combat,
    }

    public interface IAchievementProgressStore
    {
        int GetInt(string key, int defaultValue);
        void SetInt(string key, int value);
        void Save();
    }

    public readonly struct AchievementRewardEntitlement
    {
        public string StageId { get; }
        public AccountResourceKind Kinds { get; }

        internal AchievementRewardEntitlement(string stageId, AccountResourceKind kinds)
        {
            StageId = stageId;
            Kinds = kinds;
        }
    }

    public sealed class AchievementProgress
    {
        const string KeyPrefix = "lizzo.achievement.v1.";
        const AccountResourceKind RewardCandidates =
            AccountResourceKind.Gold |
            AccountResourceKind.LegionScroll |
            AccountResourceKind.LegionPiece |
            AccountResourceKind.ExpeditionTicket;

        readonly IAchievementProgressStore _store;

        public AchievementProgress(IAchievementProgressStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public int GetTotal(AchievementCategory category, string metricId)
        {
            ValidateCategory(category);
            ValidateIdentifier(metricId, nameof(metricId));
            return Math.Max(0, _store.GetInt(MetricKey(category, metricId), 0));
        }

        public void Record(AchievementCategory category, string metricId, int amount)
        {
            if (amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount));

            int current = GetTotal(category, metricId);
            int next = current > int.MaxValue - amount ? int.MaxValue : current + amount;
            if (next == current)
                return;

            _store.SetInt(MetricKey(category, metricId), next);
            _store.Save();
        }

        public bool TryIssueStageRewardEntitlement(
            string stageId,
            AccountResourceKind kinds,
            out AchievementRewardEntitlement entitlement)
        {
            ValidateIdentifier(stageId, nameof(stageId));
            if (kinds == AccountResourceKind.None || (kinds & ~RewardCandidates) != 0)
                throw new ArgumentOutOfRangeException(nameof(kinds));

            string key = RewardIssuedKey(stageId);
            if (_store.GetInt(key, 0) != 0)
            {
                entitlement = default;
                return false;
            }

            _store.SetInt(key, 1);
            _store.Save();
            entitlement = new AchievementRewardEntitlement(stageId, kinds);
            return true;
        }

        static string MetricKey(AchievementCategory category, string metricId)
        {
            return KeyPrefix + "metric." + category + "." + metricId;
        }

        static string RewardIssuedKey(string stageId)
        {
            return KeyPrefix + "reward_issued." + stageId;
        }

        static void ValidateCategory(AchievementCategory category)
        {
            if (Enum.IsDefined(typeof(AchievementCategory), category) == false)
                throw new ArgumentOutOfRangeException(nameof(category));
        }

        static void ValidateIdentifier(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Achievement identifier is required.", parameterName);
        }
    }
}
