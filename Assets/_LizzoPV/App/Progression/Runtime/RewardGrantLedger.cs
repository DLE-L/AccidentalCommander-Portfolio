using System;

namespace Lizzo.PV.Flow
{
    public enum RewardGrantState
    {
        Unavailable = 0,
        Claimable = 1,
        Granted = 2,
    }

    public interface IRewardGrantLedgerStore
    {
        int GetInt(string key, int defaultValue);
        void SetInt(string key, int value);
        void Save();
    }

    public sealed class RewardGrantLedger
    {
        const string KeyPrefix = "lizzo.reward_grant.v1.";

        readonly IRewardGrantLedgerStore _store;

        public RewardGrantLedger(IRewardGrantLedgerStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public RewardGrantState GetState(string rewardId)
        {
            return ReadState(ResolveKey(rewardId));
        }

        public bool TryMarkClaimable(string rewardId)
        {
            return TryTransition(rewardId, RewardGrantState.Unavailable, RewardGrantState.Claimable);
        }

        public bool TryCommitGrant(string rewardId)
        {
            return TryTransition(rewardId, RewardGrantState.Claimable, RewardGrantState.Granted);
        }

        bool TryTransition(string rewardId, RewardGrantState currentState, RewardGrantState nextState)
        {
            string key = ResolveKey(rewardId);
            if (ReadState(key) != currentState)
                return false;

            _store.SetInt(key, (int)nextState);
            _store.Save();
            return true;
        }

        RewardGrantState ReadState(string key)
        {
            int value = _store.GetInt(key, (int)RewardGrantState.Unavailable);
            return value switch
            {
                (int)RewardGrantState.Unavailable => RewardGrantState.Unavailable,
                (int)RewardGrantState.Claimable => RewardGrantState.Claimable,
                (int)RewardGrantState.Granted => RewardGrantState.Granted,
                _ => throw new InvalidOperationException($"Unknown reward grant state: {value}"),
            };
        }

        static string ResolveKey(string rewardId)
        {
            if (string.IsNullOrWhiteSpace(rewardId))
                throw new ArgumentException("Reward id is required.", nameof(rewardId));

            return KeyPrefix + rewardId;
        }
    }
}
