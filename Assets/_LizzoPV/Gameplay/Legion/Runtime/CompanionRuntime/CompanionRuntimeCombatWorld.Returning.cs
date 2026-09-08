using System.Collections.Generic;
using Lizzo.PV.Combat;
using Lizzo.PV.Data;
using UnityEngine;

namespace Lizzo.PV.Legion.RunCore
{
    internal sealed partial class CompanionRuntimeCombatWorld
    {
        private sealed class ReturningDelivery
        {
            internal EffectIntent Intent;
            internal CombatEffectData Effect;
            internal CompanionPassiveCombatModifiers Modifiers;
            internal ReturningAttackFlight Flight;
            internal Transform Owner;
            internal Vector3 LastOwnerPosition;
            internal bool Bound;
        }

        private readonly Dictionary<long, ReturningDelivery> _returningFlights =
            new Dictionary<long, ReturningDelivery>();
        private readonly List<TargetAreaImpactCandidate> _flightCandidates = new List<TargetAreaImpactCandidate>(32);
        private readonly List<TargetAreaImpactCandidate> _flightTargets = new List<TargetAreaImpactCandidate>(16);
        private readonly List<ReturningDelivery> _advancingFlights = new List<ReturningDelivery>(16);

        internal void CommitReturningFlight(EffectIntent intent)
        {
            if (intent.Delivery != AttackDelivery.ReturningProjectile)
                return;
            CombatEffectData effect = _data.GetCombatEffect(intent.EffectId);
            if (effect == null || effect.EffectKind == CombatEffectKind.Heal)
                return;
            Vector3 source = new Vector3(intent.SourcePosition.X, intent.SourcePosition.Y);
            Vector3 target = new Vector3(intent.TargetPosition.X, intent.TargetPosition.Y);
            _returningFlights.Add(intent.RootExecutionSequence, new ReturningDelivery
            {
                Intent = intent,
                Effect = effect,
                Modifiers = ResolveModifiers(intent.SourceCompanionId),
                Flight = new ReturningAttackFlight(source, target, intent.DeliveryDelaySeconds),
                LastOwnerPosition = source,
            });
        }

        internal ReturningAttackFlight BindReturningFlight(in PresentationCue cue, Transform owner)
        {
            foreach (ReturningDelivery delivery in _returningFlights.Values)
            {
                if (delivery.Bound || delivery.Intent.SquadId != cue.SquadId
                    || delivery.Intent.MemberOrder != cue.MemberOrder
                    || delivery.Intent.PresentationCueId != cue.PresentationId)
                    continue;
                delivery.Bound = true;
                delivery.Owner = owner;
                if (owner != null)
                {
                    delivery.LastOwnerPosition = owner.position;
                    delivery.Flight.SetLaunchPosition(owner.position);
                }
                return delivery.Flight;
            }
            return null;
        }

        internal void AdvanceReturningFlights(float deltaSeconds)
        {
            if (deltaSeconds <= 0.0f)
                return;
            _advancingFlights.Clear();
            _advancingFlights.AddRange(_returningFlights.Values);
            for (int index = 0; index < _advancingFlights.Count; index++)
                AdvanceReturningFlight(_advancingFlights[index], deltaSeconds);
        }

        private void AdvanceReturningFlight(ReturningDelivery delivery, float deltaSeconds)
        {
            ReturningAttackFlight flight = delivery.Flight;
            if (flight.IsComplete)
                return;
            if (delivery.Owner != null)
                delivery.LastOwnerPosition = delivery.Owner.position;
            flight.Advance(deltaSeconds, delivery.LastOwnerPosition);
            if ((flight.Position - flight.PreviousPosition).sqrMagnitude <= 0.000001f)
                return;

            int capacity = Mathf.Max(1, delivery.Effect.MaxTargets + delivery.Modifiers.ProjectilePierceBonus);
            if (flight.HitCount >= capacity)
                return;
            _flightCandidates.Clear();
            foreach (var enemy in _registry.Enemies)
            {
                if (CompanionRuntimeTargetSelector.IsValid(enemy) && !flight.HasHit(enemy.GetInstanceID()))
                    _flightCandidates.Add(new TargetAreaImpactCandidate(enemy, enemy.transform.position, enemy.GetInstanceID()));
            }
            // Segments are already oriented in actual travel order, including the return leg.
            CompanionReturningAttackTargetSelector.Collect(_flightCandidates,
                flight.PreviousPosition, flight.Position,
                Mathf.Max(0.01f, delivery.Effect.Radius * delivery.Modifiers.AreaRadiusMultiplier),
                capacity - flight.HitCount, ReturningAttackPass.Outbound, _flightTargets);
            EffectIntent intent = delivery.Intent;
            CompanionRuntimeEffectOwnership ownership = CompanionRuntimeEffectAttribution.Resolve(in intent);
            int damage = ResolveReturningDamage(delivery);
            for (int index = 0; index < _flightTargets.Count; index++)
            {
                if (flight.IsComplete)
                    break;
                TargetAreaImpactCandidate candidate = _flightTargets[index];
                if (_immediateHits.TryApply(CombatImmediateHitRequest.CreateAllyDirectTarget(
                    intent.SourceCompanionId, candidate.Target, flight.PreviousPosition,
                    candidate.Target.transform.position, damage, AttackVisualKind.SingleHit,
                    false, ownership.KillAttribution, intent.EffectId)))
                {
                    flight.RegisterHit(candidate.InstanceId, capacity);
                    ApplyStatus(candidate.Target, intent.SourceCompanionId, ownership.OwnerId,
                        delivery.Effect, delivery.Modifiers);
                }
            }
        }

        internal void CancelReturningFlights()
        {
            foreach (ReturningDelivery delivery in _returningFlights.Values)
                delivery.Flight.Complete();
            _returningFlights.Clear();
        }

        private static int ResolveReturningDamage(ReturningDelivery delivery)
        {
            float multiplier = delivery.Modifiers.DamageMultiplier
                * (delivery.Intent.MemberOrder == 2 ? delivery.Modifiers.PromotedDamageMultiplier : 1.0f)
                * (delivery.Flight.IsReturning ? delivery.Modifiers.ReturnDamageMultiplier : 1.0f);
            return Mathf.Max(1, Mathf.RoundToInt(delivery.Intent.SourceMagnitude * multiplier));
        }
    }
}
