using Lizzo.PV.Gameplay.Units;
using System;
using System.Collections.Generic;
using Lizzo.PV.Combat;
using Lizzo.PV.Combat.Fields;
using Lizzo.PV.Combat.Projectiles;
using Lizzo.PV.Data;
using Lizzo.PV.Legion.RunCore.Presentation;
using Lizzo.PV.Gameplay.Presentation;
using Lizzo.PV.Gameplay.Visuals;
using UnityEngine;

namespace Lizzo.PV.Legion.RunCore
{
    internal sealed class CompanionRuntimeImmediateTargetCollector
    {
        private readonly List<EnemyActor> _targets = new List<EnemyActor>(16);

        internal IReadOnlyList<EnemyActor> Collect(
            IReadOnlyCollection<EnemyActor> candidates,
            CombatEffectData effect,
            Vector3 source,
            Vector3 target,
            bool area,
            float rangeMultiplier = 1.0f)
        {
            _targets.Clear();
            float radius = (area ? Mathf.Max(0.01f, effect.Radius) : Mathf.Max(0.01f, effect.Range))
                * Mathf.Max(0.01f, rangeMultiplier);
            float radiusSquared = radius * radius;
            Vector3 forward = target - source;
            if (forward.sqrMagnitude > 0.0001f)
            {
                forward.Normalize();
            }

            foreach (EnemyActor candidate in candidates)
            {
                if (!CompanionRuntimeTargetSelector.IsValid(candidate))
                {
                    continue;
                }

                Vector3 origin = area ? target : source;
                Vector3 delta = candidate.transform.position - origin;
                if (delta.sqrMagnitude > radiusSquared)
                {
                    continue;
                }

                if (!area && effect.Angle > 0.0f && Vector3.Angle(forward, delta) > effect.Angle * 0.5f)
                {
                    continue;
                }

                _targets.Add(candidate);
            }

            _targets.Sort((left, right) =>
            {
                Vector3 origin = area ? target : source;
                int distanceOrder = (left.transform.position - origin).sqrMagnitude.CompareTo(
                    (right.transform.position - origin).sqrMagnitude);
                return distanceOrder != 0
                    ? distanceOrder
                    : left.SpawnSequence.CompareTo(right.SpawnSequence);
            });
            return _targets;
        }

        internal void Reset()
        {
            _targets.Clear();
        }
    }

    internal sealed class CompanionRuntimeSpawnedDeliveryResolver
    {
        private const float DefaultProjectileSpeed = 7.0f;
        private const float DefaultProjectileLifetime = 2.0f;

        private readonly List<PendingVolley> _pending = new List<PendingVolley>();
        private float _time;
        private int _generation;
        private struct PendingVolley
        {
            internal float Due;
            internal CombatProjectileRequest[] Shots;
            internal EffectIntent Intent;
            internal CombatEffectData Effect;
            internal Vector3 Source, Target;
        }
        internal void Reset() { _generation++; _pending.Clear(); _time = 0f; }
        internal void Advance(float deltaSeconds)
        {
            int generation = _generation;
            _time += deltaSeconds;
            for (int i = 0; i < _pending.Count;)
            {
                var pending = _pending[i];
                if (pending.Due > _time) { i++; continue; }
                _pending.RemoveAt(i);
                bool spawned = false;
                foreach (var shot in pending.Shots)
                {
                    spawned |= _projectiles.TrySpawn(shot);
                    if (generation != _generation) return;
                }
                if (spawned) _events?.PublishEffect(pending.Effect.Id, pending.Intent.PresentationCueId,
                    pending.Source, pending.Target, pending.Target - pending.Source,
                    pending.Effect.Range, pending.Effect.Radius, pending.Intent.MemberOrder);
            }
        }
        private readonly ICombatProjectileModule _projectiles;
        private readonly CompanionCombatEvents _events;
        private readonly ICombatPersistentFieldModule _persistentFields;

        internal CompanionRuntimeSpawnedDeliveryResolver(
            ICombatProjectileModule projectiles,
            ICombatPersistentFieldModule persistentFields,
            CompanionCombatEvents presentation = null)
        {
            _projectiles = projectiles ?? throw new ArgumentNullException(nameof(projectiles));
            _events = presentation;
            _persistentFields = persistentFields ?? throw new ArgumentNullException(nameof(persistentFields));
        }

        internal EffectResolution ResolveProjectile(
            in EffectIntent intent,
            CombatEffectData effect,
            Vector3 source,
            Vector3 target,
            int damage,
            CountableKillAttribution attribution,
            float speedMultiplier = 1.0f,
            CombatStatusPayload statusPayload = default,
            int projectileCount = 1,
            int pierceBonus = 0,
            float penetrationDamageStep = 0.0f,
            int repeatCount = 0)
        {
            if (repeatCount > 0 && effect.RepeatInterval <= 0f)
                throw new InvalidOperationException("Repeated projectile attack requires a positive RepeatInterval: " + effect.Id);
            int generation = _generation;
            Vector3 direction = target - source;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return new EffectResolution(false, intent.EffectId, 0.0f, 0);
            }

            direction.Normalize();
            int count = Mathf.Max(1, projectileCount);
            bool spawned = false;
            var repeats = repeatCount > 0 ? new CombatProjectileRequest[count] : null;
            for (int index = 0; index < count; index++)
            {
                float angle = (index - (count - 1) * 0.5f) * 12.0f;
                Vector3 shotDirection = Quaternion.Euler(0.0f, 0.0f, angle) * direction;
                var shot = CombatProjectileRequest.CreateStraight(
                    intent.SourceCompanionId,
                    null,
                    source,
                    shotDirection,
                    damage,
                    DefaultProjectileSpeed * Mathf.Max(0.01f, speedMultiplier),
                    effect.ProjectileLifetime > 0.0f ? effect.ProjectileLifetime : DefaultProjectileLifetime,
                    RetroVfxKind.None,
                    CombatProjectileFaction.Ally,
                    attribution,
                    effect.MaxTargets == 0 ? 0 : Mathf.Max(1, effect.MaxTargets + pierceBonus),
                    presentationId: effect.Id,
                    statusPayload: statusPayload,
                    penetrationDamageStep: penetrationDamageStep);
                spawned |= _projectiles.TrySpawn(shot);
                if (generation != _generation) return new EffectResolution(spawned, effect.Id, damage, spawned ? 1 : 0);
                if (repeats != null) repeats[index] = shot;
            }
            if (spawned)
            {
                for (int repeat = 1; repeat <= repeatCount; repeat++)
                    _pending.Add(new PendingVolley { Due = _time + repeat * effect.RepeatInterval, Shots = repeats, Intent = intent, Effect = effect, Source = source, Target = target });
                _events?.PublishEffect(
                    effect.Id,
                    intent.PresentationCueId,
                    source,
                    target,
                    direction,
                    effect.Range,
                    effect.Radius,
                    intent.MemberOrder);
            }

            return new EffectResolution(spawned, effect.Id, spawned ? damage : 0.0f, spawned ? 1 : 0);
        }

        internal EffectResolution ResolvePersistentField(
            in EffectIntent intent,
            CombatEffectData effect,
            Vector3 source,
            Vector3 target,
            int damage,
            int ownerId,
            float elapsedSeconds,
            float radiusMultiplier = 1.0f,
            float durationMultiplier = 1.0f,
            float tickIntervalMultiplier = 1.0f,
            int capacityBonus = 0)
        {
            bool spawned = _persistentFields.TrySpawn(
                CombatPersistentFieldRequest.CreateAllyDamage(
                    intent.SourceCompanionId,
                    effect.Id,
                    ownerId,
                    target,
                    damage,
                    Mathf.Max(0.01f, effect.Radius * radiusMultiplier),
                    Mathf.Max(0.01f, effect.TickInterval * tickIntervalMultiplier),
                    Mathf.Max(0.01f, effect.Duration * durationMultiplier),
                    Mathf.Max(1, effect.MaxTargets),
                    Mathf.Max(1, effect.MaxActiveCount + capacityBonus)),
                elapsedSeconds);
            if (spawned)
            {
                _events?.PublishEffect(
                    effect.Id,
                    intent.PresentationCueId,
                    source,
                    target,
                    target - source,
                    effect.Range,
                    effect.Radius,
                    intent.MemberOrder);
            }

            return new EffectResolution(spawned, effect.Id, spawned ? damage : 0.0f, spawned ? 1 : 0);
        }
    }

}
