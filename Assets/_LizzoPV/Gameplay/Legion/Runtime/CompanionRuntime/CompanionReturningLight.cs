using System;
using System.Collections.Generic;
using Lizzo.PV.Combat;
using Lizzo.PV.Data;
using Lizzo.PV.Gameplay.Units;
using Lizzo.PV.Gameplay.Visuals;
using UnityEngine;

namespace Lizzo.PV.Legion.RunCore
{
    // A volley owns one return result, regardless of its number of light branches.
    internal sealed class CompanionReturningLight
    {
        private sealed class Volley
        {
            internal EffectIntent Intent;
            internal ReturningAttackFlight[] Flights;
            internal int Damage, Heal, Capacity;
            internal float Width, ReturnSpeed;
            internal CountableKillAttribution Attribution;
        }

        private readonly IDataProvider _data;
        private readonly RuntimeObjectRegistry _registry;
        private readonly ICombatImmediateHitModule _hits;
        private readonly List<Volley> _volleys = new List<Volley>();
        private readonly List<TargetAreaImpactCandidate> _candidates = new List<TargetAreaImpactCandidate>(32);
        private readonly List<TargetAreaImpactCandidate> _selected = new List<TargetAreaImpactCandidate>(8);
        private int _generation;
        internal event Action<ReturningAttackFlight, string> Launched;
        internal event Action<EffectIntent> Returned;
        internal event Action<ReturningAttackFlight> Turning;

        internal CompanionReturningLight(IDataProvider data, RuntimeObjectRegistry registry, ICombatImmediateHitModule hits)
        { _data = data; _registry = registry; _hits = hits; }

        internal EffectResolution Launch(in EffectIntent intent, CombatEffectData effect,
            Vector3 source, Vector3 target, int damage, CompanionPassiveCombatModifiers modifiers)
        {
            var heal = _data.GetCombatEffect(_data.GetCompanionCombatProfile(intent.SourceCompanionId).SecondaryEffectId);
            float speed = Mathf.Max(0.1f, effect.MotionSpeed * modifiers.ProjectileSpeedMultiplier);
            float distance = Mathf.Max(0.1f, effect.Range * modifiers.RangeMultiplier);
            Vector3 direction = target - source;
            if (direction.sqrMagnitude < 0.0001f) direction = Vector3.up;
            var volley = new Volley
            {
                Intent = intent, Damage = damage,
                Heal = Mathf.Max(1, Mathf.RoundToInt(heal.BaseValue * intent.SourceMagnitude / effect.BaseValue * modifiers.HealMultiplier)),
                Capacity = Mathf.Max(1, effect.MaxTargets + modifiers.ProjectilePierceBonus),
                Width = effect.Radius, ReturnSpeed = speed * modifiers.ReturnSpeedMultiplier,
                Attribution = CompanionRuntimeEffectAttribution.Resolve(in intent).KillAttribution,
                Flights = new ReturningAttackFlight[Mathf.Max(1, modifiers.ProjectileCount)]
            };
            _volleys.Add(volley);
            for (int i = 0; i < volley.Flights.Length; i++)
            {
                Vector3 branch = Quaternion.Euler(0f, 0f, (i - (volley.Flights.Length - 1) * 0.5f) * 12f) * direction.normalized;
                var flight = new ReturningAttackFlight(source, source + branch * distance, distance / speed);
                volley.Flights[i] = flight;
                try { Launched?.Invoke(flight, effect.Id); }
                catch (Exception exception) { Debug.LogException(exception); }
            }
            return new EffectResolution(true, effect.Id, damage, 0);
        }

        internal void Advance(float deltaSeconds)
        {
            CommanderActor commander = _registry.Player;
            if (commander == null || !commander.isActiveAndEnabled || commander.Hp <= 0)
            { Cancel(); return; }
            int generation = _generation;
            for (int i = _volleys.Count - 1; i >= 0; i--)
            {
                Volley volley = _volleys[i];
                bool complete = true;
                foreach (var flight in volley.Flights)
                {
                    if (flight.IsComplete) continue;
                    flight.Advance(deltaSeconds, commander.transform.position);
                    // Preserve the outgoing projectile's damage contract; returning light heals, without a second damage pass.
                    if (!flight.IsReturning) ApplyHits(volley, flight);
                    if (_generation != generation) return;
                    if (flight.HasReachedLegEnd)
                    {
                        if (flight.IsReturning) flight.Complete();
                        else
                        {
                            flight.BeginReturn(Vector3.Distance(flight.Position, commander.transform.position) / Mathf.Max(0.1f, volley.ReturnSpeed));
                            try { Turning?.Invoke(flight); }
                            catch (Exception exception) { Debug.LogException(exception); }
                        }
                    }
                    complete &= flight.IsComplete;
                }
                if (!complete) continue;
                _volleys.RemoveAt(i);
                int before = commander.Hp;
                commander.Heal(volley.Heal);
                try { commander.NotifyHealingResolved(commander.Hp - before); }
                catch (Exception exception) { Debug.LogException(exception); }
                if (_generation != generation) return;
                try { Returned?.Invoke(volley.Intent); }
                catch (Exception exception) { Debug.LogException(exception); }
                if (_generation != generation) return;
            }
        }

        private void ApplyHits(Volley volley, ReturningAttackFlight flight)
        {
            if (flight.HitCount >= volley.Capacity) return;
            _candidates.Clear();
            Vector3 segment = flight.Position - flight.PreviousPosition;
            float lengthSquared = segment.sqrMagnitude;
            foreach (EnemyActor enemy in _registry.Enemies)
            {
                if (!CompanionRuntimeTargetSelector.IsValid(enemy) || flight.HasHit(enemy.GetInstanceID())) continue;
                var collider = enemy.CombatCollider;
                if (collider == null || !collider.enabled) continue;
                Vector3 center = collider.bounds.center;
                float progress = lengthSquared > 0f
                    ? Mathf.Clamp01(Vector3.Dot(center - flight.PreviousPosition, segment) / lengthSquared) : 0f;
                // Match the actual combat shape, rather than requiring the light to cross the unit pivot.
                Vector3 contact = collider.ClosestPoint(flight.PreviousPosition + segment * progress);
                _candidates.Add(new TargetAreaImpactCandidate(enemy, contact, enemy.GetInstanceID()));
            }
            CompanionReturningAttackTargetSelector.Collect(_candidates, flight.PreviousPosition, flight.Position,
                volley.Width, volley.Capacity - flight.HitCount, ReturningAttackPass.Outbound, _selected);
            int generation = _generation;
            for (int i = 0; i < _selected.Count; i++)
            {
                var candidate = _selected[i];
                if (!CompanionRuntimeTargetSelector.IsValid(candidate.Target) || !flight.RegisterHit(candidate.InstanceId, volley.Capacity)) continue;
                _hits.TryApply(CombatImmediateHitRequest.CreateAllyDirectTarget(volley.Intent.SourceCompanionId,
                    candidate.Target, flight.PreviousPosition, candidate.Point, volley.Damage,
                    AttackVisualKind.SingleHit, false, volley.Attribution, volley.Intent.EffectId));
                if (_generation != generation) return;
            }
        }

        internal void Cancel()
        {
            _generation++;
            foreach (var volley in _volleys)
                foreach (var flight in volley.Flights) flight?.Complete();
            _volleys.Clear();
        }
    }
}
