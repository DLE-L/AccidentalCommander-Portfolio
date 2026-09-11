using Lizzo.PV.Gameplay.Visuals;
using Lizzo.PV.Gameplay.Units;
using System;
using System.Collections.Generic;
using Lizzo.PV.Combat;
using Lizzo.PV.Data;
using UnityEngine;

namespace Lizzo.PV.Legion.RunCore
{
    internal sealed class CompanionReturningAttack
    {
        private readonly IDataProvider _data;
        private readonly RuntimeObjectRegistry _registry;
        private readonly ICombatImmediateHitModule _immediateHits;
        private readonly Func<string, CompanionPassiveCombatModifiers> _resolveModifiers;
        private readonly Action<EnemyActor, string, int, CombatEffectData, CompanionPassiveCombatModifiers> _applyStatus;

        internal CompanionReturningAttack(IDataProvider data, RuntimeObjectRegistry registry, ICombatImmediateHitModule hits,
            Func<string, CompanionPassiveCombatModifiers> resolveModifiers,
            Action<EnemyActor, string, int, CombatEffectData, CompanionPassiveCombatModifiers> applyStatus)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _immediateHits = hits ?? throw new ArgumentNullException(nameof(hits));
            _resolveModifiers = resolveModifiers ?? throw new ArgumentNullException(nameof(resolveModifiers));
            _applyStatus = applyStatus ?? throw new ArgumentNullException(nameof(applyStatus));
        }

        internal EffectResolution Resolve(
            in EffectIntent intent,
            CombatEffectData effect)
        {
            if (!_returningFlights.TryGetValue(intent.RootExecutionSequence, out ReturningDelivery delivery))
                return new EffectResolution(false, intent.EffectId, 0.0f, 0);

            // Flush only the final unswept part before the core records this leg's result.
            AdvanceReturningFlight(delivery, float.PositiveInfinity);
            if (delivery.Flight.IsComplete)
                return new EffectResolution(false, intent.EffectId, 0.0f, 0);
            int affected = delivery.Flight.HitCount;
            delivery.AnyHit |= affected > 0;
            int damage = ResolveReturningDamage(delivery);
            float returnSeconds = Mathf.Max(0.01f, delivery.Effect.Duration /
                Mathf.Max(0.01f, delivery.Modifiers.ReturnSpeedMultiplier));
            IReadOnlyList<IndependentEffectRequest> followUps = intent.ChainDepth == 0
                ? new[]
                {
                    new IndependentEffectRequest(
                        intent.SquadId,
                        intent.SourceCompanionId,
                        intent.EffectId,
                        intent.SourceMagnitude,
                        intent.TargetPosition,
                        intent.Motion,
                        intent.Delivery,
                        intent.MemberOrder,
                        intent.PresentationCueId,
                        returnSeconds),
                }
                : Array.Empty<IndependentEffectRequest>();
            if (intent.ChainDepth == 0)
                delivery.Flight.BeginReturn(returnSeconds);
            else
            {
                delivery.Flight.Complete();
                _returningFlights.Remove(intent.RootExecutionSequence);
            }
            return new EffectResolution(
                affected > 0,
                effect.Id,
                affected > 0 ? damage : 0.0f,
                affected,
                followUps, completedReturningAttack: intent.ChainDepth > 0 && delivery.AnyHit);
        }

        private sealed class ReturningDelivery
        {
            internal EffectIntent Intent;
            internal CombatEffectData Effect;
            internal CompanionPassiveCombatModifiers Modifiers;
            internal ReturningAttackFlight Flight;
            internal Transform Owner;
            internal Vector3 LastOwnerPosition;
            internal bool Bound;
            internal bool AnyHit;
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
                Modifiers = _resolveModifiers(intent.SourceCompanionId),
                Flight = new ReturningAttackFlight(
                    source,
                    target,
                    intent.DeliveryDelaySeconds,
                    effect.MinimumTravelDistance, effect.FixedTravelDistance),
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

            int capacity = delivery.Effect.AffectsAllTargetsInShape ? int.MaxValue
                : Mathf.Max(1, delivery.Effect.MaxTargets + delivery.Modifiers.ProjectilePierceBonus);
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
                    _applyStatus(candidate.Target, intent.SourceCompanionId, ownership.OwnerId,
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
