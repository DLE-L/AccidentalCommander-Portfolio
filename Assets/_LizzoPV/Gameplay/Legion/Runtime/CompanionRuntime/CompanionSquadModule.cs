using System;
using System.Collections.Generic;

namespace Lizzo.PV.Legion.RunCore
{
    internal static class CompanionActionDefinitionValidator
    {
        internal static bool TryValidate(ActionSet actionSet)
        {
            if (actionSet == null
                || IsFinite(actionSet.CooldownSeconds) == false
                || actionSet.CooldownSeconds <= 0.0f
                || string.IsNullOrWhiteSpace(actionSet.Id))
            {
                return false;
            }

            IReadOnlyList<ActionStep> steps = actionSet.Steps;
            if (steps.Count < 1)
            {
                return false;
            }

            for (int index = 0; index < steps.Count; index++)
            {
                if (TryValidate(steps[index]) == false)
                {
                    return false;
                }
            }

            return true;
        }

        internal static bool IsFinite(float value)
        {
            return float.IsNaN(value) == false && float.IsInfinity(value) == false;
        }

        private static bool TryValidate(ActionStep step)
        {
            if (string.IsNullOrWhiteSpace(step.EffectId)
                || string.IsNullOrWhiteSpace(step.PresentationCueId)
                || IsFinite(step.Magnitude) == false
                || IsFinite(step.ActionDurationSeconds) == false
                || step.ActionDurationSeconds < 0.0f
                || Enum.IsDefined(typeof(AttackDelivery), step.Delivery) == false
                || (step.Motion != CombatMotion.Stationary && step.Motion != CombatMotion.Excursion)
                || IsFinite(step.DeliveryDelaySeconds) == false
                || step.DeliveryDelaySeconds < 0.0f
                || IsFinite(step.ExcursionStandOffDistance) == false
                || step.ExcursionStandOffDistance < 0.0f
                || IsFinite(step.TargetAcquisitionRange) == false
                || step.TargetAcquisitionRange < 0.0f
                || IsFinite(step.ExcursionLateralOffset) == false
                || step.ExcursionLateralOffset < 0.0f
                || IsFinite(step.ExcursionMaxDepartureDistance) == false
                || step.ExcursionMaxDepartureDistance < 0.0f
                || IsFinite(step.ExcursionSpeed) == false)
            {
                return false;
            }

            return step.Motion == CombatMotion.Stationary
                ? step.ExcursionSpeed >= 0.0f
                : step.ExcursionSpeed > 0.0f;
        }
    }

    internal static class CompanionSquadSnapshotFactory
    {
        internal static SquadSnapshot Create(
            string squadId,
            int slotId,
            string companionId,
            CompanionSquadProgressionState progression,
            CompanionSquadActionCycle actionCycle,
            IReadOnlyList<CompanionSquadActionCycle> independentActionCycles,
            bool combatEligible)
        {
            CompanionMemberSnapshot[] members = progression.CopyMemberSnapshots();
            if (independentActionCycles != null && independentActionCycles.Count == members.Length)
            {
                for (int index = 0; index < members.Length; index++)
                {
                    CompanionMemberSnapshot member = members[index];
                    CompanionSquadActionCycle memberCycle = independentActionCycles[index];
                    members[index] = new CompanionMemberSnapshot(
                        member.MemberOrder,
                        member.IsPromotedLeader,
                        member.LocalOffset,
                        memberCycle.ActionPhase,
                        memberCycle.ActiveMemberPosition,
                        memberCycle.CommittedTargetPosition);
                }
            }

            return new SquadSnapshot(
                squadId,
                slotId,
                companionId,
                progression.ActiveActionSet.Id,
                progression.MemberCount,
                progression.Promoted,
                combatEligible,
                actionCycle.CooldownRemainingSeconds,
                actionCycle.FormationAnchor,
                actionCycle.ActionPhase,
                actionCycle.ActiveMemberOrder,
                actionCycle.ActiveMemberPosition,
                actionCycle.CommittedTargetPosition,
                members);
        }
    }

    internal sealed class CompanionSquadModule
    {
        private readonly CompanionSquadActionCycle _actionCycle;
        private readonly string _companionId;
        private readonly CompanionSquadProgressionState _progression;
        private readonly List<CompanionSquadActionCycle> _independentActionCycles =
            new List<CompanionSquadActionCycle>(3);
        private bool _independentMemberActions;

        private string _squadId;
        private int _slotId;

        public CompanionSquadModule(
            string companionId,
            ActionSet baseActionSet,
            ActionSet promotedActionSet)
        {
            _companionId = companionId ?? throw new ArgumentNullException(nameof(companionId));
            _progression = new CompanionSquadProgressionState(baseActionSet, promotedActionSet);
            _actionCycle = new CompanionSquadActionCycle(_progression);
            CombatEligible = true;
        }

        public string SquadId => _squadId;
        public int SlotId => _slotId;
        public string CompanionId => _companionId;
        public string ActionSetId => _progression.ActiveActionSet.Id;
        public bool Promoted => _progression.Promoted;
        public bool CombatEligible { get; }
        public ActionStep ActionStep => ResolveSnapshotCycle().ActionStep;
        public float CooldownRemainingSeconds => ResolveSnapshotCycle().CooldownRemainingSeconds;
        public CompanionPoint FormationAnchor => ResolveSnapshotCycle().FormationAnchor;
        public SquadActionPhase ActionPhase => ResolveSnapshotCycle().ActionPhase;
        public int ActiveMemberOrder => ResolveSnapshotCycle().ActiveMemberOrder;
        public CompanionPoint ActiveMemberPosition => ResolveSnapshotCycle().ActiveMemberPosition;
        public CompanionPoint? CommittedTargetPosition => ResolveSnapshotCycle().CommittedTargetPosition;

        public static bool TryCreate(
            string normalizedCompanionId,
            CompanionDefinition definition,
            out CompanionSquadModule squad)
        {
            squad = null;
            if (definition == null || definition.CompanionId == null)
            {
                return false;
            }

            if (!string.Equals(
                    definition.CompanionId.Trim(),
                    normalizedCompanionId,
                    StringComparison.Ordinal))
            {
                return false;
            }

            if (!CompanionActionDefinitionValidator.TryValidate(definition.BaseActionSet)
                || !CompanionActionDefinitionValidator.TryValidate(definition.PromotedActionSet))
            {
                return false;
            }

            squad = new CompanionSquadModule(
                normalizedCompanionId,
                definition.BaseActionSet,
                definition.PromotedActionSet);
            return true;
        }

        public void AssignIdentity(int index)
        {
            _squadId = "squad-" + index;
        }

        public void AssignSlot(int slotId)
        {
            _slotId = slotId;
        }

        public void AssignFormationAnchor(CompanionPoint anchor)
        {
            _actionCycle.AssignFormationAnchor(anchor);
            for (int index = 0; index < _independentActionCycles.Count; index++)
                _independentActionCycles[index].AssignFormationAnchor(anchor);
        }

        public bool TryReinforce()
        {
            if (!_progression.TryReinforce())
                return false;

            if (_independentMemberActions)
                EnsureIndependentActionCycleCount();

            return true;
        }

        public bool TryPromote()
        {
            if (_progression.TryPromote() == false)
            {
                return false;
            }

            if (_independentMemberActions)
            {
                EnsureIndependentActionCycleCount();
                for (int index = 0; index < _independentActionCycles.Count; index++)
                    _independentActionCycles[index].ResetAfterPromotion();
            }
            else
            {
                _actionCycle.ResetAfterPromotion();
            }
            return true;
        }

        internal void EnableIndependentMemberActions()
        {
            if (_independentMemberActions)
                return;

            _independentMemberActions = true;
            _independentActionCycles.Clear();
            EnsureIndependentActionCycleCount();
        }

        public bool TryAdvance(
            float deltaSeconds,
            ICompanionCombatWorld combatWorld,
            out CompanionSquadAdvanceIntent effectIntent)
        {
            return TryAdvance(deltaSeconds, combatWorld, FormationAnchor, out effectIntent);
        }

        public bool TryAdvance(
            float deltaSeconds,
            ICompanionCombatWorld combatWorld,
            CompanionPoint targetAcquisitionOrigin,
            out CompanionSquadAdvanceIntent effectIntent)
        {
            return _actionCycle.TryAdvance(
                deltaSeconds,
                combatWorld,
                targetAcquisitionOrigin,
                out effectIntent);
        }

        internal int CollectAdvanceIntents(
            float deltaSeconds,
            ICompanionCombatWorld combatWorld,
            CompanionPoint targetAcquisitionOrigin,
            List<CompanionSquadAdvanceIntent> output)
        {
            if (output == null)
                throw new ArgumentNullException(nameof(output));

            int initialCount = output.Count;
            if (!_independentMemberActions)
            {
                if (_actionCycle.TryAdvance(
                        deltaSeconds,
                        combatWorld,
                        targetAcquisitionOrigin,
                        out CompanionSquadAdvanceIntent intent))
                {
                    output.Add(intent);
                }

                return output.Count - initialCount;
            }

            ICompanionTargetReservationWorld reservationWorld = null;
            if (_independentActionCycles.Count > 1
                && _independentActionCycles[0].ActionStep.AvoidSharedTarget)
            {
                reservationWorld = combatWorld as ICompanionTargetReservationWorld;
                if (reservationWorld != null)
                {
                    reservationWorld.BeginTargetReservationScope();
                    for (int index = 0; index < _independentActionCycles.Count; index++)
                    {
                        CompanionPoint? committed = _independentActionCycles[index].CommittedTargetPosition;
                        if (committed.HasValue)
                            reservationWorld.ReserveTargetPosition(committed.Value);
                    }
                }
            }

            for (int index = 0; index < _independentActionCycles.Count; index++)
            {
                if (_independentActionCycles[index].TryAdvance(
                        deltaSeconds,
                        combatWorld,
                        targetAcquisitionOrigin,
                        out CompanionSquadAdvanceIntent intent))
                {
                    output.Add(intent);
                }
            }

            return output.Count - initialCount;
        }

        public SquadSnapshot ToSnapshot()
        {
            return CompanionSquadSnapshotFactory.Create(
                SquadId,
                SlotId,
                CompanionId,
                _progression,
                ResolveSnapshotCycle(),
                _independentMemberActions ? _independentActionCycles : null,
                CombatEligible);
        }

        public void CancelActiveActions()
        {
            if (_independentMemberActions)
            {
                for (int index = 0; index < _independentActionCycles.Count; index++)
                    _independentActionCycles[index].CancelActiveActions();
                return;
            }

            _actionCycle.CancelActiveActions();
        }

        private CompanionSquadActionCycle ResolveSnapshotCycle()
        {
            for (int index = 0; index < _independentActionCycles.Count; index++)
            {
                if (_independentActionCycles[index].ActionPhase != SquadActionPhase.Idle)
                    return _independentActionCycles[index];
            }

            return _independentActionCycles.Count > 0
                ? _independentActionCycles[0]
                : _actionCycle;
        }

        private void EnsureIndependentActionCycleCount()
        {
            while (_independentActionCycles.Count < _progression.MemberCount)
            {
                int memberOrder = _independentActionCycles.Count;
                CompanionSquadActionCycle cycle = new CompanionSquadActionCycle(_progression, memberOrder);
                cycle.AssignFormationAnchor(_actionCycle.FormationAnchor);
                _independentActionCycles.Add(cycle);
            }
        }
    }
}
