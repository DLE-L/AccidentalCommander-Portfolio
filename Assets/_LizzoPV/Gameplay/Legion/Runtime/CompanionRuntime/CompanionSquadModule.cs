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
                || !IsFinite(step.RecoverySeconds) || step.RecoverySeconds < 0.0f
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
            bool combatEligible)
        {
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
                progression.CopyMemberSnapshots());
        }
    }

    internal sealed class CompanionSquadModule
    {
        private readonly CompanionSquadActionCycle _actionCycle;
        private readonly string _companionId;
        private readonly CompanionSquadProgressionState _progression;

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
        internal bool HasReturnSegment => _actionCycle.HasReturnSegment;
        internal CompanionReturnSegment ReturnSegment => _actionCycle.ReturnSegment;
        public int SlotId => _slotId;
        public string CompanionId => _companionId;
        public string ActionSetId => _progression.ActiveActionSet.Id;
        public bool Promoted => _progression.Promoted;
        public bool CombatEligible { get; }
        public ActionStep ActionStep => _actionCycle.ActionStep;
        public float CooldownRemainingSeconds => _actionCycle.CooldownRemainingSeconds;
        public CompanionPoint FormationAnchor => _actionCycle.FormationAnchor;
        public SquadActionPhase ActionPhase => _actionCycle.ActionPhase;
        public int ActiveMemberOrder => _actionCycle.ActiveMemberOrder;
        public CompanionPoint ActiveMemberPosition => _actionCycle.ActiveMemberPosition;
        public CompanionPoint? CommittedTargetPosition => _actionCycle.CommittedTargetPosition;

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
        }

        public void AssignFormationDirection(CompanionPoint groupCenter)
        {
            _progression.AssignFormationDirection(groupCenter);
        }

        public void AssignRuntimeModifiers(CompanionPassiveCombatModifiers modifiers)
        {
            _actionCycle.AssignRuntimeModifiers(modifiers);
        }

        internal void AssignAttackIntervalDivisor(float divisor) => _actionCycle.AssignAttackIntervalDivisor(divisor);

        public bool TryReinforce()
        {
            return _progression.TryReinforce();
        }

        public bool TryPromote()
        {
            return _progression.TryPromote();
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

        public SquadSnapshot ToSnapshot()
        {
            return CompanionSquadSnapshotFactory.Create(
                SquadId,
                SlotId,
                CompanionId,
                _progression,
                _actionCycle,
                CombatEligible);
        }

        public void CancelActiveActions()
        {
            _actionCycle.CancelActiveActions();
        }
    }
}
