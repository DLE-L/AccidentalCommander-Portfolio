using System;
using System.Collections.Generic;

namespace Lizzo.PV.Legion.RunCore
{
    public enum CompanionRunEventKind
    {
        SquadRecruited,
        SquadReinforced,
        SquadPromoted,
        EffectCommitted,
        EffectResolved
    }

    public readonly struct EffectIntent
    {
        public EffectIntent(
            long executionSequence,
            string squadId,
            string sourceCompanionId,
            string effectId,
            float sourceMagnitude,
            CompanionPoint targetPosition,
            CombatMotion motion,
            AttackDelivery delivery)
            : this(
                executionSequence,
                squadId,
                sourceCompanionId,
                effectId,
                sourceMagnitude,
                targetPosition,
                motion,
                delivery,
                0,
                string.Empty,
                0.0f,
                executionSequence,
                0)
        {
        }

        public EffectIntent(
            long executionSequence,
            string squadId,
            string sourceCompanionId,
            string effectId,
            float sourceMagnitude,
            CompanionPoint targetPosition,
            CombatMotion motion,
            AttackDelivery delivery,
            int memberOrder)
            : this(
                executionSequence,
                squadId,
                sourceCompanionId,
                effectId,
                sourceMagnitude,
                targetPosition,
                motion,
                delivery,
                memberOrder,
                string.Empty,
                0.0f,
                executionSequence,
                0)
        {
        }

        public EffectIntent(
            long executionSequence,
            string squadId,
            string sourceCompanionId,
            string effectId,
            float sourceMagnitude,
            CompanionPoint targetPosition,
            CombatMotion motion,
            AttackDelivery delivery,
            int memberOrder,
            string presentationCueId,
            float deliveryDelaySeconds,
            long rootExecutionSequence,
            int chainDepth)
            : this(
                executionSequence,
                squadId,
                sourceCompanionId,
                effectId,
                sourceMagnitude,
                targetPosition,
                motion,
                delivery,
                memberOrder,
                presentationCueId,
                deliveryDelaySeconds,
                rootExecutionSequence,
                chainDepth,
                default)
        {
        }

        public EffectIntent(
            long executionSequence,
            string squadId,
            string sourceCompanionId,
            string effectId,
            float sourceMagnitude,
            CompanionPoint targetPosition,
            CombatMotion motion,
            AttackDelivery delivery,
            int memberOrder,
            string presentationCueId,
            float deliveryDelaySeconds,
            long rootExecutionSequence,
            int chainDepth,
            CompanionPoint sourcePosition)
        {
            ExecutionSequence = executionSequence;
            SquadId = squadId;
            SourceCompanionId = sourceCompanionId;
            EffectId = effectId;
            SourceMagnitude = sourceMagnitude;
            TargetPosition = targetPosition;
            Motion = motion;
            Delivery = delivery;
            MemberOrder = memberOrder;
            PresentationCueId = presentationCueId;
            DeliveryDelaySeconds = deliveryDelaySeconds;
            RootExecutionSequence = rootExecutionSequence;
            ChainDepth = chainDepth;
            SourcePosition = sourcePosition;
        }

        public long ExecutionSequence { get; }
        public string SquadId { get; }
        public string SourceCompanionId { get; }
        public string EffectId { get; }
        public float SourceMagnitude { get; }
        public CompanionPoint TargetPosition { get; }
        public CompanionPoint SourcePosition { get; }
        public CombatMotion Motion { get; }
        public AttackDelivery Delivery { get; }
        public int MemberOrder { get; }
        public string PresentationCueId { get; }
        public float DeliveryDelaySeconds { get; }
        public long RootExecutionSequence { get; }
        public int ChainDepth { get; }
    }

    public readonly struct IndependentEffectRequest
    {
        public IndependentEffectRequest(
            string sourceSquadId,
            string sourceCompanionId,
            string effectId,
            float sourceMagnitude,
            CompanionPoint targetPosition,
            CombatMotion motion,
            AttackDelivery delivery,
            int memberOrder,
            string presentationCueId,
            float deliveryDelaySeconds)
        {
            SourceSquadId = sourceSquadId;
            SourceCompanionId = sourceCompanionId;
            EffectId = effectId;
            SourceMagnitude = sourceMagnitude;
            TargetPosition = targetPosition;
            Motion = motion;
            Delivery = delivery;
            MemberOrder = memberOrder;
            PresentationCueId = presentationCueId;
            DeliveryDelaySeconds = deliveryDelaySeconds;
        }

        public string SourceSquadId { get; }
        public string SourceCompanionId { get; }
        public string EffectId { get; }
        public float SourceMagnitude { get; }
        public CompanionPoint TargetPosition { get; }
        public CombatMotion Motion { get; }
        public AttackDelivery Delivery { get; }
        public int MemberOrder { get; }
        public string PresentationCueId { get; }
        public float DeliveryDelaySeconds { get; }
    }

    public readonly struct EffectResolution
    {
        public EffectResolution(bool applied, string effectId, float appliedMagnitude, int affectedTargetCount)
            : this(applied, effectId, appliedMagnitude, affectedTargetCount, null)
        {
        }

        public EffectResolution(
            bool applied,
            string effectId,
            float appliedMagnitude,
            int affectedTargetCount,
            IReadOnlyList<IndependentEffectRequest> followUps)
        {
            Applied = applied;
            EffectId = effectId;
            AppliedMagnitude = appliedMagnitude;
            AffectedTargetCount = affectedTargetCount;
            if (followUps == null)
            {
                FollowUps = Array.Empty<IndependentEffectRequest>();
                return;
            }

            IndependentEffectRequest[] copy = new IndependentEffectRequest[followUps.Count];
            for (int index = 0; index < followUps.Count; index += 1)
            {
                copy[index] = followUps[index];
            }

            FollowUps = copy;
        }

        public bool Applied { get; }
        public string EffectId { get; }
        public float AppliedMagnitude { get; }
        public int AffectedTargetCount { get; }
        public IReadOnlyList<IndependentEffectRequest> FollowUps { get; }
    }

    public readonly struct PresentationCue
    {
        public PresentationCue(string presentationId, string squadId, CompanionPoint position)
            : this(
                presentationId,
                squadId,
                -1,
                position,
                position,
                AttackDelivery.Direct,
                0.0f)
        {
        }

        public PresentationCue(
            string presentationId,
            string squadId,
            int memberOrder,
            CompanionPoint sourcePosition,
            CompanionPoint targetPosition,
            AttackDelivery delivery,
            float deliveryDelaySeconds)
        {
            PresentationId = presentationId;
            SquadId = squadId;
            MemberOrder = memberOrder;
            SourcePosition = sourcePosition;
            TargetPosition = targetPosition;
            Delivery = delivery;
            DeliveryDelaySeconds = deliveryDelaySeconds;
        }

        public string PresentationId { get; }
        public string SquadId { get; }
        public int MemberOrder { get; }
        public CompanionPoint SourcePosition { get; }
        public CompanionPoint TargetPosition { get; }
        public AttackDelivery Delivery { get; }
        public float DeliveryDelaySeconds { get; }
        public CompanionPoint Position => TargetPosition;
    }

    public readonly struct CompanionRunEvent
    {
        public CompanionRunEvent(
            long order,
            CompanionRunEventKind kind,
            string squadId,
            string companionId,
            EffectResolution? resolution,
            PresentationCue? presentationCue)
        {
            Order = order;
            Kind = kind;
            SquadId = squadId;
            CompanionId = companionId;
            Resolution = resolution;
            PresentationCue = presentationCue;
        }

        public long Order { get; }
        public CompanionRunEventKind Kind { get; }
        public string SquadId { get; }
        public string CompanionId { get; }
        public EffectResolution? Resolution { get; }
        public PresentationCue? PresentationCue { get; }
    }
}
