using System;
using System.Collections.Generic;

namespace Lizzo.PV.Gameplay.Run
{
    public enum SynergyTier
    {
        Pair = 2,
        Trio = 3,
    }

    public enum SynergyCasterPresentation
    {
        Afterimage,
        Representative,
    }

    public enum SynergyTriggerPriority
    {
        Periodic,
        Cumulative,
        ConditionReactive,
    }

    public enum SynergyExecutionPhase
    {
        Executing,
        WaitingForMovement,
        FollowupReady,
        Complete,
    }

    public enum SynergyControlResolution
    {
        None,
        MovementEnded,
        Immune,
    }

    public sealed class SynergyDefinition
    {
        private readonly string[] _requiredLegionIds;

        public string SynergyId { get; }
        public SynergyTier Tier { get; }
        public IReadOnlyList<string> RequiredLegionIds => _requiredLegionIds;
        public string CasterUnitId { get; }
        public SynergyCasterPresentation Presentation { get; }
        public float CooldownSeconds { get; }
        public SynergyTriggerPriority TriggerPriority { get; }

        public SynergyDefinition(
            string synergyId,
            SynergyTier tier,
            string[] requiredLegionIds,
            string casterUnitId,
            SynergyCasterPresentation presentation,
            float cooldownSeconds,
            SynergyTriggerPriority triggerPriority = SynergyTriggerPriority.ConditionReactive)
        {
            if (string.IsNullOrWhiteSpace(synergyId))
                throw new ArgumentException("Synergy id is required.", nameof(synergyId));
            if (tier != SynergyTier.Pair && tier != SynergyTier.Trio)
                throw new ArgumentOutOfRangeException(nameof(tier));
            if (requiredLegionIds == null || requiredLegionIds.Length != (int)tier)
                throw new ArgumentException("Required legion count must match synergy tier.", nameof(requiredLegionIds));
            if (string.IsNullOrWhiteSpace(casterUnitId))
                throw new ArgumentException("Promoted caster unit id is required.", nameof(casterUnitId));
            if (float.IsNaN(cooldownSeconds) || float.IsInfinity(cooldownSeconds) || cooldownSeconds < 0.0f)
                throw new ArgumentOutOfRangeException(nameof(cooldownSeconds));

            _requiredLegionIds = new string[requiredLegionIds.Length];
            for (int index = 0; index < requiredLegionIds.Length; index++)
            {
                string legionId = requiredLegionIds[index];
                if (string.IsNullOrWhiteSpace(legionId))
                    throw new ArgumentException("Required legion id cannot be empty.", nameof(requiredLegionIds));
                for (int other = 0; other < index; other++)
                {
                    if (string.Equals(_requiredLegionIds[other], legionId, StringComparison.Ordinal))
                        throw new ArgumentException($"Duplicate required legion id: {legionId}", nameof(requiredLegionIds));
                }
                _requiredLegionIds[index] = legionId;
            }

            SynergyId = synergyId;
            Tier = tier;
            CasterUnitId = casterUnitId;
            Presentation = presentation;
            CooldownSeconds = cooldownSeconds;
            TriggerPriority = triggerPriority;
        }
    }
}
