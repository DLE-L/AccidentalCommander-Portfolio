using System;
using System.Collections.Generic;
using Lizzo.PV.Legion;
using UnityEngine;

namespace Lizzo.PV.Combat.Fields
{
    public static class CombatPersistentFieldTargetCollector
    {
        public static void Collect(
            List<CombatPersistentFieldTarget> source,
            Vector3 center,
            float radius,
            int maxTargets,
            List<CombatPersistentFieldTarget> results)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (results == null)
                throw new ArgumentNullException(nameof(results));

            results.Clear();
            int limit = Mathf.Max(0, maxTargets);
            if (limit == 0)
                return;

            float clampedRadius = Mathf.Max(0.0f, radius);
            float sqrRadius = clampedRadius * clampedRadius;
            for (int i = 0; i < source.Count; i++)
            {
                CombatPersistentFieldTarget candidate = source[i];
                if (candidate.Target == null || candidate.Target.IsAlive == false)
                    continue;

                float candidateDistance = (candidate.Point - center).sqrMagnitude;
                if (candidateDistance > sqrRadius)
                    continue;

                int insertIndex = 0;
                while (insertIndex < results.Count)
                {
                    CombatPersistentFieldTarget existing = results[insertIndex];
                    float existingDistance = (existing.Point - center).sqrMagnitude;
                    if (candidateDistance < existingDistance
                        || (Mathf.Approximately(candidateDistance, existingDistance)
                            && candidate.InstanceId < existing.InstanceId))
                    {
                        break;
                    }

                    insertIndex++;
                }

                if (insertIndex >= limit)
                    continue;

                results.Insert(insertIndex, candidate);
                if (results.Count > limit)
                    results.RemoveAt(limit);
            }
        }
    }

    public sealed class RegistryPersistentFieldTargetSource : ICombatPersistentFieldTargetSource
    {
        private readonly RuntimeObjectRegistry _registry;

        public RegistryPersistentFieldTargetSource(RuntimeObjectRegistry registry)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        public void CollectTargets(Vector3 center, List<CombatPersistentFieldTarget> destination)
        {
            foreach (MonsterController monster in _registry.Enemies)
            {
                if (monster == null || monster.IsValid() == false)
                    continue;

                Collider2D collider = monster.CombatCollider;
                Vector3 point = collider == null || collider.enabled == false
                    ? monster.transform.position
                    : (Vector3)collider.ClosestPoint(center);
                point.z = monster.transform.position.z;
                destination.Add(new CombatPersistentFieldTarget(monster, point, monster.GetInstanceID()));
            }
        }
    }
}
