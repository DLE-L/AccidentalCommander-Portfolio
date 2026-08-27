using System;

namespace Lizzo.PV.Flow
{
    public enum ExpeditionTicketReservationState
    {
        None = 0,
        Reserved = 1,
        CommitReady = 2,
        Cancelled = 3,
    }

    public readonly struct ExpeditionTicketCommitIntent
    {
        public string RunId { get; }
        public CampaignStageId StageId { get; }
        public int TicketCost { get; }
        public bool RequiresBaseRewardGrant { get; }

        internal ExpeditionTicketCommitIntent(string runId, CampaignStageId stageId)
        {
            RunId = runId;
            StageId = stageId;
            TicketCost = 1;
            RequiresBaseRewardGrant = true;
        }
    }

    public interface IExpeditionTicketReservationStore
    {
        int GetInt(string key, int defaultValue);
        void SetInt(string key, int value);
        void Save();
    }

    public sealed class ExpeditionTicketReservationLedger
    {
        const string KeyPrefix = "lizzo.expedition_ticket_reservation.v1.";

        readonly IExpeditionTicketReservationStore _store;

        public ExpeditionTicketReservationLedger(IExpeditionTicketReservationStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public ExpeditionTicketReservationState GetState(string runId)
        {
            return ReadState(StateKey(runId));
        }

        public bool TryReserve(string runId, RunContext context, int availableTicketCount)
        {
            string stateKey = StateKey(runId);
            ValidateContext(context);
            if (context.IsTutorial || availableTicketCount < context.ExpeditionTicketCost)
                return false;
            if (ReadState(stateKey) != ExpeditionTicketReservationState.None)
                return false;

            _store.SetInt(StageKey(runId), (int)context.StageId);
            _store.SetInt(stateKey, (int)ExpeditionTicketReservationState.Reserved);
            _store.Save();
            return true;
        }

        public bool TryMarkInitializationSucceeded(
            string runId,
            out ExpeditionTicketCommitIntent intent)
        {
            string stateKey = StateKey(runId);
            if (ReadState(stateKey) != ExpeditionTicketReservationState.Reserved)
            {
                intent = default;
                return false;
            }

            intent = CreateIntent(runId);
            _store.SetInt(stateKey, (int)ExpeditionTicketReservationState.CommitReady);
            _store.Save();
            return true;
        }

        public bool TryGetCommitIntent(string runId, out ExpeditionTicketCommitIntent intent)
        {
            if (GetState(runId) != ExpeditionTicketReservationState.CommitReady)
            {
                intent = default;
                return false;
            }

            intent = CreateIntent(runId);
            return true;
        }

        public bool TryCancel(string runId)
        {
            string stateKey = StateKey(runId);
            if (ReadState(stateKey) != ExpeditionTicketReservationState.Reserved)
                return false;

            _store.SetInt(stateKey, (int)ExpeditionTicketReservationState.Cancelled);
            _store.Save();
            return true;
        }

        ExpeditionTicketCommitIntent CreateIntent(string runId)
        {
            int value = _store.GetInt(StageKey(runId), 0);
            CampaignStageId stageId = value switch
            {
                (int)CampaignStageId.Stage1 => CampaignStageId.Stage1,
                (int)CampaignStageId.Stage2 => CampaignStageId.Stage2,
                (int)CampaignStageId.Stage3 => CampaignStageId.Stage3,
                _ => throw new InvalidOperationException($"Unknown reserved campaign stage: {value}"),
            };
            return new ExpeditionTicketCommitIntent(runId, stageId);
        }

        ExpeditionTicketReservationState ReadState(string key)
        {
            int value = _store.GetInt(key, (int)ExpeditionTicketReservationState.None);
            return value switch
            {
                (int)ExpeditionTicketReservationState.None => ExpeditionTicketReservationState.None,
                (int)ExpeditionTicketReservationState.Reserved => ExpeditionTicketReservationState.Reserved,
                (int)ExpeditionTicketReservationState.CommitReady => ExpeditionTicketReservationState.CommitReady,
                (int)ExpeditionTicketReservationState.Cancelled => ExpeditionTicketReservationState.Cancelled,
                _ => throw new InvalidOperationException($"Unknown ticket reservation state: {value}"),
            };
        }

        static string StateKey(string runId)
        {
            return BaseKey(runId) + ".state";
        }

        static string StageKey(string runId)
        {
            return BaseKey(runId) + ".stage";
        }

        static string BaseKey(string runId)
        {
            if (string.IsNullOrWhiteSpace(runId))
                throw new ArgumentException("Run id is required.", nameof(runId));

            return KeyPrefix + runId;
        }

        static void ValidateContext(RunContext context)
        {
            if (context.StageId < CampaignStageId.Stage1 || context.StageId > CampaignStageId.Stage3)
                throw new ArgumentOutOfRangeException(nameof(context));
        }
    }
}
