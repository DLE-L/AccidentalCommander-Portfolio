using System;
using System.Collections.Generic;

namespace Lizzo.PV.Gameplay.RunTraits
{
    public sealed class RunTraitOfferCoordinator : IDisposable
    {
        readonly RunTraitRunState _runState;
        readonly RunTraitOpportunitySchedule _opportunities = new RunTraitOpportunitySchedule();
        readonly RunTraitEligibilitySetBuilder _eligibility = new RunTraitEligibilitySetBuilder();

        RunTraitOfferSnapshot _activeOffer;
        bool _disposed;

        public RunTraitOfferCoordinator(RunTraitRunState runState)
        {
            _runState = runState ?? throw new ArgumentNullException(nameof(runState));
        }

        public bool HasPendingOpportunity
        {
            get
            {
                return _opportunities.HasPending;
            }
        }

        public RunTraitOfferSnapshot ActiveOffer => _activeOffer;

        public int GetPendingOpportunityIndex(float elapsedSeconds)
        {
            return ResolvePendingOpportunity(elapsedSeconds);
        }

        public bool TryGetPendingOffer(float elapsedSeconds, in RunTraitEligibilityContext context, out RunTraitOfferSnapshot snapshot)
        {
            return TryGetPendingOffer(elapsedSeconds, context, RunTraitOfferPolicy.Standard, out snapshot);
        }

        public bool TryGetPendingOffer(float elapsedSeconds, in RunTraitEligibilityContext context, RunTraitOfferPolicy policy, out RunTraitOfferSnapshot snapshot)
        {
            snapshot = null;
            if (_disposed || _runState.IsFull)
                return false;

            int opportunityIndex = ResolvePendingOpportunity(elapsedSeconds);
            if (opportunityIndex < 0)
                return false;
            if (context.IsPresentationSafe == false)
                return false;

            RunTraitOfferPolicy resolvedPolicy = policy ?? RunTraitOfferPolicy.Standard;

            if (_activeOffer != null)
            {
                snapshot = _activeOffer;
                return true;
            }

            IReadOnlyList<RunTraitWeightedCandidate> eligible = _eligibility.Build(_runState, context);
            if (eligible.Count <= 1)
                return false;

            snapshot = RunTraitOfferComposer.Create(
                opportunityIndex,
                _opportunities.GetOpportunitySeconds(opportunityIndex),
                resolvedPolicy,
                eligible);
            _activeOffer = snapshot;
            return true;
        }

        public bool TryAcceptSelection(string offerIdentity, int slotIndex, string traitId)
        {
            if (_disposed || _activeOffer == null
                || string.Equals(_activeOffer.OfferIdentity, offerIdentity, StringComparison.Ordinal) == false
                || slotIndex < 0 || slotIndex >= _activeOffer.Slots.Count)
                return false;

            RunTraitOfferSlot selected = _activeOffer.Slots[slotIndex];
            if (string.Equals(selected.TraitId, traitId, StringComparison.Ordinal) == false
                || _runState.TrySelect(selected.TraitId) == false)
                return false;

            _runState.RecordSelection(_activeOffer, selected.TraitId);
            _opportunities.MarkResolved(_activeOffer.OpportunityIndex);
            _activeOffer = null;
            return true;
        }

        public void ExpirePendingOpportunities()
        {
            _opportunities.ExpireAll();
            _activeOffer = null;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _eligibility.Clear();
            _activeOffer = null;
            _disposed = true;
        }

        int ResolvePendingOpportunity(float elapsedSeconds)
        {
            int opportunityIndex = _opportunities.ResolvePending(elapsedSeconds);
            if (_activeOffer != null && _opportunities.IsResolved(_activeOffer.OpportunityIndex))
                _activeOffer = null;
            return opportunityIndex;
        }

    }
}
