using System.Globalization;
using System.Text;
using Lizzo.PV.P0.Cards.CardOffer;

namespace Lizzo.PV.Gameplay.Telemetry
{
    public static partial class RunTelemetry
    {
        public static void LogCardOfferGenerated(CardOfferSnapshot snapshot)
        {
            if (snapshot == null)
                return;

            CardOfferSlot offer1 = GetSlot(snapshot, 0);
            CardOfferSlot offer2 = GetSlot(snapshot, 1);
            CardOfferSlot offer3 = GetSlot(snapshot, 2);

            Log(
                CardOfferGenerated,
                $"run_id={ResolveCardOfferRunId(snapshot)}",
                $"event_sequence={snapshot.EventSequence.ToString(CultureInfo.InvariantCulture)}",
                $"offer_index={snapshot.OfferIndex.ToString(CultureInfo.InvariantCulture)}",
                $"offer_seed={snapshot.OfferSeed.ToString(CultureInfo.InvariantCulture)}",
                $"policy_version={snapshot.PolicyVersion}",
                $"config_assignment_hash={snapshot.ConfigAssignmentHash}",
                $"run_state_hash={snapshot.RunStateHash}",
                $"candidate_count={snapshot.CandidateCount.ToString(CultureInfo.InvariantCulture)}",
                $"offer_1_id={offer1.CardId}",
                $"offer_1_weight={FormatWeight(offer1.Weight)}",
                $"offer_2_id={offer2.CardId}",
                $"offer_2_weight={FormatWeight(offer2.Weight)}",
                $"offer_3_id={offer3.CardId}",
                $"offer_3_weight={FormatWeight(offer3.Weight)}");

            Log(
                CardOfferDiagnostic,
                $"run_id={ResolveCardOfferRunId(snapshot)}",
                $"offer_index={snapshot.OfferIndex.ToString(CultureInfo.InvariantCulture)}",
                $"eligible_ids={BuildEligibleIds(snapshot)}",
                $"final_weights={BuildEligibleWeights(snapshot)}");
        }

        public static void LogCardOfferSelected(CardOfferSnapshot snapshot, CardOfferSlot selectedSlot, int decisionTimeMs, bool skipOrTimeout)
        {
            if (snapshot == null)
                return;

            Log(
                CardOfferSelected,
                $"run_id={ResolveCardOfferRunId(snapshot)}",
                $"offer_index={snapshot.OfferIndex.ToString(CultureInfo.InvariantCulture)}",
                $"chosen_id={selectedSlot.CardId}",
                $"decision_time_ms={System.Math.Max(0, decisionTimeMs).ToString(CultureInfo.InvariantCulture)}",
                $"skip_or_timeout={skipOrTimeout.ToString().ToLowerInvariant()}",
                $"active_build_snapshot_id={snapshot.OfferIdentity}");
        }

        public static void LogMaxBuildComplete(string runStateHash, int nextOfferIndex)
        {
            Log(
                MaxBuildComplete,
                $"run_id={CurrentRunId}",
                $"run_state_hash={runStateHash ?? string.Empty}",
                $"next_offer_index={nextOfferIndex.ToString(CultureInfo.InvariantCulture)}");
        }

        private static string ResolveCardOfferRunId(CardOfferSnapshot snapshot)
        {
            return string.IsNullOrWhiteSpace(CurrentRunId) == false ? CurrentRunId : snapshot.RunId;
        }

        private static CardOfferSlot GetSlot(CardOfferSnapshot snapshot, int slotIndex)
        {
            if (slotIndex >= snapshot.Slots.Count)
                return default;

            return snapshot.Slots[slotIndex];
        }

        private static string FormatWeight(float value) => value.ToString("0.####", CultureInfo.InvariantCulture);

        private static string BuildEligibleIds(CardOfferSnapshot snapshot)
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < snapshot.EligibleDiagnostics.Count; i++)
            {
                if (i > 0)
                    builder.Append(';');
                builder.Append(snapshot.EligibleDiagnostics[i].CardId);
            }

            return builder.ToString();
        }

        private static string BuildEligibleWeights(CardOfferSnapshot snapshot)
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < snapshot.EligibleDiagnostics.Count; i++)
            {
                if (i > 0)
                    builder.Append(';');
                CardOfferCandidate candidate = snapshot.EligibleDiagnostics[i];
                builder.Append(candidate.CardId);
                builder.Append(':');
                builder.Append(FormatWeight(candidate.Weight));
            }

            return builder.ToString();
        }
    }
}
