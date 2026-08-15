using System;
using System.Collections.Generic;

namespace Lizzo.PV.Legion.RunCore
{
    internal sealed class CombatExecutionModule
    {
        private const int MaxChainDepth = 3;
        private const int MaxFollowUpsPerResolution = 8;
        private const int MaxPendingExecutions = 64;

        private readonly List<PendingDetachedExecution> _pendingDetachedExecutions;
        private int _droppedChainRequestCount;

        public CombatExecutionModule()
        {
            _pendingDetachedExecutions = new List<PendingDetachedExecution>(MaxPendingExecutions);
        }

        public bool TryCreateEffectIntent(
            long executionSequence,
            CompanionSquadModule squad,
            int memberOrder,
            CompanionPoint targetPosition,
            out EffectIntent intent)
        {
            intent = default;
            if (squad == null)
            {
                return false;
            }

            ActionStep step = squad.ActionStep;
            if (!IsFinite(step.Magnitude) || step.Magnitude < 0.0f)
            {
                return false;
            }

            if (!IsFinite(step.DeliveryDelaySeconds) || step.DeliveryDelaySeconds < 0.0f)
            {
                return false;
            }

            if (!Enum.IsDefined(typeof(AttackDelivery), step.Delivery))
            {
                return false;
            }

            if (!Enum.IsDefined(typeof(CombatMotion), step.Motion))
            {
                return false;
            }

            if (string.IsNullOrEmpty(step.EffectId) || step.EffectId.Trim().Length == 0)
            {
                return false;
            }

            if (string.IsNullOrEmpty(step.PresentationCueId) || step.PresentationCueId.Trim().Length == 0)
            {
                return false;
            }

            intent = new EffectIntent(
                executionSequence,
                squad.SquadId,
                squad.CompanionId,
                step.EffectId,
                step.Magnitude,
                targetPosition,
                step.Motion,
                step.Delivery,
                memberOrder,
                step.PresentationCueId,
                step.DeliveryDelaySeconds,
                executionSequence,
                0);

            return true;
        }

        public int PendingCount => _pendingDetachedExecutions.Count;

        public int DroppedChainRequestCount => _droppedChainRequestCount;

        public void Reset()
        {
            _pendingDetachedExecutions.Clear();
            _droppedChainRequestCount = 0;
        }

        public void EnqueueDetachedEffect(in EffectIntent intent)
        {
            if (_pendingDetachedExecutions.Count >= MaxPendingExecutions)
            {
                _droppedChainRequestCount += 1;
                return;
            }

            _pendingDetachedExecutions.Add(new PendingDetachedExecution(intent, MathF.Max(0.0f, intent.DeliveryDelaySeconds)));
        }

        public void AdvancePending(float deltaSeconds, List<EffectIntent> readyIntents)
        {
            readyIntents.Clear();
            if (_pendingDetachedExecutions.Count <= 0 || !IsFinite(deltaSeconds) || deltaSeconds <= 0.0f)
            {
                return;
            }

            int processCount = _pendingDetachedExecutions.Count;
            int writeIndex = 0;
            for (int index = 0; index < processCount; index += 1)
            {
                PendingDetachedExecution queuedExecution = _pendingDetachedExecutions[index];
                queuedExecution.RemainingDelay -= deltaSeconds;
                if (queuedExecution.RemainingDelay <= 0.0f)
                {
                    readyIntents.Add(queuedExecution.Intent);
                    continue;
                }

                _pendingDetachedExecutions[writeIndex] = queuedExecution;
                writeIndex += 1;
            }

            if (writeIndex < _pendingDetachedExecutions.Count)
            {
                _pendingDetachedExecutions.RemoveRange(writeIndex, _pendingDetachedExecutions.Count - writeIndex);
            }
        }

        public void EnqueueFollowUps(
            in EffectIntent sourceIntent,
            in EffectResolution resolution,
            ref long nextExecutionSequence)
        {
            IReadOnlyList<IndependentEffectRequest> followUps = resolution.FollowUps;
            int followUpCount = followUps.Count;
            for (int index = 0; index < followUpCount; index += 1)
            {
                if (index >= MaxFollowUpsPerResolution)
                {
                    _droppedChainRequestCount += followUpCount - index;
                    break;
                }

                IndependentEffectRequest request = followUps[index];
                TryCreateFollowUp(sourceIntent, request, ref nextExecutionSequence);
            }
        }

        private void TryCreateFollowUp(
            in EffectIntent sourceIntent,
            in IndependentEffectRequest request,
            ref long nextExecutionSequence)
        {
            if (!IsValidFollowUpSource(sourceIntent) || !IsValidIndependentRequest(in request))
            {
                _droppedChainRequestCount += 1;
                return;
            }

            if (_pendingDetachedExecutions.Count >= MaxPendingExecutions)
            {
                _droppedChainRequestCount += 1;
                return;
            }

            long executionSequence = nextExecutionSequence + 1L;
            int chainDepth = sourceIntent.ChainDepth + 1;
            _pendingDetachedExecutions.Add(
                new PendingDetachedExecution(
                    new EffectIntent(
                        executionSequence,
                        request.SourceSquadId,
                        request.SourceCompanionId,
                        request.EffectId,
                        request.SourceMagnitude,
                        request.TargetPosition,
                        request.Motion,
                        request.Delivery,
                        request.MemberOrder,
                        request.PresentationCueId,
                        request.DeliveryDelaySeconds,
                        sourceIntent.RootExecutionSequence,
                        chainDepth),
                    MathF.Max(0.0f, request.DeliveryDelaySeconds)));
            nextExecutionSequence = executionSequence;
        }

        private static bool IsValidIndependentRequest(in IndependentEffectRequest request)
        {
            if (request.SourceMagnitude < 0.0f || !IsFinite(request.SourceMagnitude))
            {
                return false;
            }

            if (!IsFinite(request.DeliveryDelaySeconds) || request.DeliveryDelaySeconds < 0.0f)
            {
                return false;
            }

            if (!Enum.IsDefined(typeof(AttackDelivery), request.Delivery))
            {
                return false;
            }

            if (!Enum.IsDefined(typeof(CombatMotion), request.Motion))
            {
                return false;
            }

            if (string.IsNullOrEmpty(request.EffectId) || request.EffectId.Trim().Length == 0)
            {
                return false;
            }

            if (string.IsNullOrEmpty(request.PresentationCueId) || request.PresentationCueId.Trim().Length == 0)
            {
                return false;
            }

            if (string.IsNullOrEmpty(request.SourceSquadId) || request.SourceSquadId.Trim().Length == 0)
            {
                return false;
            }

            if (string.IsNullOrEmpty(request.SourceCompanionId) || request.SourceCompanionId.Trim().Length == 0)
            {
                return false;
            }

            return request.MemberOrder >= -1;
        }

        private static bool IsValidFollowUpSource(in EffectIntent sourceIntent)
        {
            return sourceIntent.ChainDepth < MaxChainDepth;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private struct PendingDetachedExecution
        {
            public PendingDetachedExecution(EffectIntent intent, float remainingDelay)
            {
                Intent = intent;
                RemainingDelay = remainingDelay;
            }

            public EffectIntent Intent;

            public float RemainingDelay;
        }
    }
}
