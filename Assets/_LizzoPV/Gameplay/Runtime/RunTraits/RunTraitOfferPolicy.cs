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
}
