using System;
using System.Collections.Generic;

namespace Lizzo.PV.Gameplay.RunTraits
{
    public sealed class RunTraitOfferCoordinator : IDisposable
    {
        readonly RunTraitRunState _runState;
        readonly RunTraitOpportunitySchedule _opportunities = new RunTraitOpportunitySchedule();
        readonly RunTraitEligibilitySetBuilder _eligibility = new RunTraitEligibilitySetBuilder();
        readonly RunTraitOfferSession _session;

        bool _disposed;

        public RunTraitOfferCoordinator(RunTraitRunState runState)
        {
            _runState = runState ?? throw new ArgumentNullException(nameof(runState));
            _session = new RunTraitOfferSession(_runState);
        }

        public bool HasPendingOpportunity
        {
            get
            {
                return _opportunities.HasPending;
            }
        }

        public RunTraitOfferSnapshot ActiveOffer => _session.ActiveOffer;

        public bool ReportEliteDefeated()
        {
            return _disposed == false
                && _runState.IsFull == false
                && _opportunities.ReportEliteDefeated();
        }

        public int GetPendingOpportunityIndex()
        {
            return ResolvePendingOpportunity();
        }

        public int GetPendingOpportunityIndex(float elapsedSeconds)
        {
            return GetPendingOpportunityIndex();
        }

        public bool TryGetPendingOffer(in RunTraitEligibilityContext context, out RunTraitOfferSnapshot snapshot)
        {
            return TryGetPendingOffer(context, RunTraitOfferPolicy.Standard, out snapshot);
        }

        public bool TryGetPendingOffer(float elapsedSeconds, in RunTraitEligibilityContext context, out RunTraitOfferSnapshot snapshot)
        {
            return TryGetPendingOffer(context, out snapshot);
        }

        public bool TryGetPendingOffer(in RunTraitEligibilityContext context, RunTraitOfferPolicy policy, out RunTraitOfferSnapshot snapshot)
        {
            snapshot = null;
            if (_disposed || _runState.IsFull)
                return false;

            int opportunityIndex = ResolvePendingOpportunity();
            if (opportunityIndex < 0)
                return false;
            if (context.IsPresentationSafe == false)
                return false;

            RunTraitOfferPolicy resolvedPolicy = policy ?? RunTraitOfferPolicy.Standard;

            if (_session.ActiveOffer != null)
            {
                snapshot = _session.ActiveOffer;
                return true;
            }

            IReadOnlyList<RunTraitWeightedCandidate> eligible = _eligibility.Build(_runState, context);
            if (eligible.Count <= 1)
                return false;

            snapshot = RunTraitOfferComposer.Create(
                opportunityIndex,
                0.0f,
                resolvedPolicy,
                eligible);
            _session.SetActive(snapshot);
            return true;
        }

        public bool TryGetPendingOffer(float elapsedSeconds, in RunTraitEligibilityContext context, RunTraitOfferPolicy policy, out RunTraitOfferSnapshot snapshot)
        {
            return TryGetPendingOffer(context, policy, out snapshot);
        }

        public bool TryAcceptSelection(string offerIdentity, int slotIndex, string traitId)
        {
            if (_disposed
                || _session.TryAccept(offerIdentity, slotIndex, traitId, out int opportunityIndex) == false)
            {
                return false;
            }

            _opportunities.MarkResolved(opportunityIndex);
            return true;
        }

        public void ExpirePendingOpportunities()
        {
            _opportunities.ExpireAll();
            _session.Clear();
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _eligibility.Clear();
            _session.Clear();
            _disposed = true;
        }

        int ResolvePendingOpportunity()
        {
            int opportunityIndex = _opportunities.ResolvePending();
            if (_session.ActiveOffer != null && _opportunities.IsResolved(_session.ActiveOffer.OpportunityIndex))
                _session.Clear();
            return opportunityIndex;
        }

    }
}
