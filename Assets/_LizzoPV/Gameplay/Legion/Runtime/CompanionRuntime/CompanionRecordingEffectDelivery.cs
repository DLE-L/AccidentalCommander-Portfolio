using System;
using System.Collections.Generic;
using Lizzo.PV.Combat;
using Lizzo.PV.Combat.Fields;
using Lizzo.PV.Combat.Projectiles;
using Lizzo.PV.Data;
using Lizzo.PV.Legion.RunCore.Presentation;
using Lizzo.PV.P0.Presentation;
using Lizzo.PV.P0.Visuals;
using UnityEngine;

namespace Lizzo.PV.Legion.RunCore
{
    internal sealed class CompanionRecordingImmediateTargetCollector
    {
        private readonly List<MonsterController> _targets = new List<MonsterController>(16);

        internal IReadOnlyList<MonsterController> Collect(
            IReadOnlyCollection<MonsterController> candidates,
            CombatEffectData effect,
            Vector3 source,
            Vector3 target,
            bool area)
        {
            _targets.Clear();
            float radius = area ? Mathf.Max(0.01f, effect.Radius) : Mathf.Max(0.01f, effect.Range);
            float radiusSquared = radius * radius;
            Vector3 forward = target - source;
            if (forward.sqrMagnitude > 0.0001f)
            {
                forward.Normalize();
            }

            foreach (MonsterController candidate in candidates)
            {
                if (!CompanionRecordingTargetSelector.IsValid(candidate))
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

    internal sealed class CompanionRecordingSpawnedDeliveryResolver
    {
        private const float DefaultProjectileSpeed = 7.0f;
        private const float DefaultProjectileLifetime = 2.0f;

        private readonly ICombatProjectileModule _projectiles;
        private readonly ICombatPersistentFieldModule _persistentFields;

        internal CompanionRecordingSpawnedDeliveryResolver(
            ICombatProjectileModule projectiles,
            ICombatPersistentFieldModule persistentFields)
        {
            _projectiles = projectiles ?? throw new ArgumentNullException(nameof(projectiles));
            _persistentFields = persistentFields ?? throw new ArgumentNullException(nameof(persistentFields));
        }

        internal EffectResolution ResolveProjectile(
            in EffectIntent intent,
            CombatEffectData effect,
            Vector3 source,
            Vector3 target,
            int damage,
            CountableKillAttribution attribution)
        {
            Vector3 direction = target - source;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return new EffectResolution(false, intent.EffectId, 0.0f, 0);
            }

            direction.Normalize();
            bool spawned = _projectiles.TrySpawn(CombatProjectileRequest.CreateStraight(
                intent.SourceCompanionId,
                null,
                source,
                direction,
                damage,
                DefaultProjectileSpeed,
                effect.ProjectileLifetime > 0.0f ? effect.ProjectileLifetime : DefaultProjectileLifetime,
                RetroVfxKind.None,
                CombatProjectileFaction.Ally,
                attribution,
                Mathf.Max(1, effect.MaxTargets),
                presentationId: effect.Id));
            if (spawned)
            {
                CompanionRecordingEffectPresenter.Present(
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
            float elapsedSeconds)
        {
            bool spawned = _persistentFields.TrySpawn(
                CombatPersistentFieldRequest.CreateAllyDamage(
                    intent.SourceCompanionId,
                    effect.Id,
                    ownerId,
                    target,
                    damage,
                    Mathf.Max(0.01f, effect.Radius),
                    Mathf.Max(0.01f, effect.TickInterval),
                    Mathf.Max(0.01f, effect.Duration),
                    Mathf.Max(1, effect.MaxTargets),
                    Mathf.Max(1, effect.MaxActiveCount)),
                elapsedSeconds);
            if (spawned)
            {
                CompanionRecordingEffectPresenter.Present(
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
