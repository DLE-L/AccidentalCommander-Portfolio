using Lizzo.PV.Gameplay.Units;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    public readonly struct ChainTargetCandidate
    {
        public readonly EnemyActor Target;
        public readonly Vector3 Point;
        public readonly int InstanceId;
        public ChainTargetCandidate(EnemyActor target, Vector3 point, int instanceId) { Target = target; Point = point; InstanceId = instanceId; }
    }

    public static class ChainTargetSelector
    {
        public static void Collect(List<ChainTargetCandidate> candidates, Vector3 origin, float initialRange, float chainDistance, int maxTargets, List<ChainTargetCandidate> results)
        {
            if (candidates == null) throw new ArgumentNullException(nameof(candidates));
            if (results == null) throw new ArgumentNullException(nameof(results));
            results.Clear();
            Vector3 pivot = origin;
            float range = Mathf.Max(0.0f, initialRange);
            int limit = Mathf.Max(0, maxTargets);
            for (int selectedCount = 0; selectedCount < limit; selectedCount++)
            {
                int bestIndex = -1; float bestDistance = range * range; int bestId = int.MaxValue;
                for (int i = 0; i < candidates.Count; i++)
                {
                    ChainTargetCandidate candidate = candidates[i];
                    if (Contains(results, candidate.InstanceId)) continue;
                    float distance = (candidate.Point - pivot).sqrMagnitude;
                    if (distance > bestDistance || (Mathf.Approximately(distance, bestDistance) && candidate.InstanceId >= bestId)) continue;
                    bestIndex = i; bestDistance = distance; bestId = candidate.InstanceId;
                }
                if (bestIndex < 0) return;
                ChainTargetCandidate selected = candidates[bestIndex];
                results.Add(selected);
                pivot = selected.Point;
                range = Mathf.Max(0.0f, chainDistance);
            }
        }

        static bool Contains(List<ChainTargetCandidate> targets, int instanceId)
        {
            for (int i = 0; i < targets.Count; i++) if (targets[i].InstanceId == instanceId) return true;
            return false;
        }
    }
}
