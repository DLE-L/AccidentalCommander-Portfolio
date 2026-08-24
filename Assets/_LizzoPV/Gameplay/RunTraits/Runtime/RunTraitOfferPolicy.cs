using System;

namespace Lizzo.PV.Gameplay.RunTraits
{
    public sealed class RunTraitOfferPolicy
    {
        public const string StandardPolicyId = "standard";
        public const string RecordingFirstPolicyId = "recording_first";

        RunTraitOfferPolicy(string policyId, bool reservePromotionShoutInCenter, bool requiresBuildRelated)
        {
            PolicyId = policyId;
            ReservePromotionShoutInCenter = reservePromotionShoutInCenter;
            RequiresBuildRelated = requiresBuildRelated;
        }

        public string PolicyId { get; }
        public bool ReservePromotionShoutInCenter { get; }
        public bool RequiresBuildRelated { get; }

        public static RunTraitOfferPolicy Standard { get; } = new RunTraitOfferPolicy(StandardPolicyId, false, true);
        public static RunTraitOfferPolicy RecordingFirst { get; } = new RunTraitOfferPolicy(RecordingFirstPolicyId, true, false);

        public static RunTraitOfferPolicy Resolve(string cardPoolProfileId, int opportunityIndex)
        {
            return opportunityIndex == 0
                && string.Equals(cardPoolProfileId, "recording", StringComparison.Ordinal)
                    ? RecordingFirst
                    : Standard;
        }
    }

    internal sealed class RunTraitOpportunitySchedule
    {
        static readonly float[] OpportunitySeconds = { 60.0f, 150.0f, 240.0f };

        readonly bool[] _resolved = new bool[OpportunitySeconds.Length];

        internal bool HasPending
        {
            get
            {
                for (int index = 0; index < _resolved.Length; index++)
                    if (_resolved[index] == false)
                        return true;
                return false;
            }
        }

        internal int ResolvePending(float elapsedSeconds)
        {
            int latestReachedIndex = -1;
            for (int index = 0; index < OpportunitySeconds.Length; index++)
            {
                if (elapsedSeconds < OpportunitySeconds[index])
                    break;
                latestReachedIndex = index;
            }

            for (int index = 0; index < latestReachedIndex; index++)
                _resolved[index] = true;

            for (int index = 0; index < OpportunitySeconds.Length; index++)
                if (_resolved[index] == false && elapsedSeconds >= OpportunitySeconds[index])
                    return index;
            return -1;
        }

        internal bool IsResolved(int opportunityIndex)
        {
            return opportunityIndex >= 0
                && opportunityIndex < _resolved.Length
                && _resolved[opportunityIndex];
        }

        internal void MarkResolved(int opportunityIndex)
        {
            if (opportunityIndex >= 0 && opportunityIndex < _resolved.Length)
                _resolved[opportunityIndex] = true;
        }

        internal void ExpireAll()
        {
            for (int index = 0; index < _resolved.Length; index++)
                _resolved[index] = true;
        }

        internal float GetOpportunitySeconds(int opportunityIndex)
        {
            return OpportunitySeconds[opportunityIndex];
        }
    }
}
