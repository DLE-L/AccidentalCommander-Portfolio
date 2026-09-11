using Lizzo.PV.Gameplay.Units;
using System;
using System.Collections.Generic;
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
            List<CombatPersistentFieldTarget> results,
            CombatImmediateHitFaction sourceFaction = CombatImmediateHitFaction.Ally)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (results == null)
                throw new ArgumentNullException(nameof(results));

            results.Clear();
            int limit = source.Count; // Area effects affect every eligible target in the shape.
            if (limit == 0)
                return;

            float clampedRadius = Mathf.Max(0.0f, radius);
            for (int i = 0; i < source.Count; i++)
            {
                CombatPersistentFieldTarget candidate = source[i];
                if (!CombatAreaAttack.ContainsTarget(sourceFaction, candidate.Target, candidate.Point, center, clampedRadius))
                    continue;

                float candidateDistance = (candidate.Point - center).sqrMagnitude;
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
            CommanderActor player = _registry.Player;
            if (player != null && player.isActiveAndEnabled && player.Hp > 0)
                destination.Add(new CombatPersistentFieldTarget(player, player.transform.position, player.GetInstanceID()));
            foreach (EnemyActor monster in _registry.Enemies)
            {
                if (monster == null || monster.IsValid() == false)
                    continue;

                Vector3 point = CombatTargeting.ResolveTargetPoint(monster, center);
                destination.Add(new CombatPersistentFieldTarget(monster, point, monster.GetInstanceID()));
            }
        }
    }
}
