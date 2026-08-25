using System.Collections.Generic;
using Lizzo.PV.Legion;
using UnityEngine;

namespace Lizzo.PV.Legion.Synergy
{
    public readonly struct ArcherRainCandidate
    {
        public ArcherRainCandidate(MonsterController target, Vector3 point, long spawnSequence)
        {
            Target = target;
            Point = point;
            SpawnSequence = spawnSequence;
        }

        public MonsterController Target { get; }
        public Vector3 Point { get; }
        public long SpawnSequence { get; }
    }

    public static class ArcherRainRules
    {
        public static void SelectDensest(
            IReadOnlyList<ArcherRainCandidate> source,
            Vector3 anchor,
            float densityRadius,
            List<ArcherRainCandidate> results)
        {
            results.Clear();
            if (source == null || source.Count == 0)
                return;

            int selectedIndex = -1;
            int bestCount = -1;
            float bestAnchorDistance = float.MaxValue;
            long bestSequence = long.MaxValue;
            float radiusSquared = densityRadius * densityRadius;

            for (int candidateIndex = 0; candidateIndex < source.Count; candidateIndex++)
            {
                int clusterCount = 0;
                for (int neighborIndex = 0; neighborIndex < source.Count; neighborIndex++)
                {
                    if ((source[neighborIndex].Point - source[candidateIndex].Point).sqrMagnitude <= radiusSquared)
                        clusterCount++;
                }

                float anchorDistance = (source[candidateIndex].Point - anchor).sqrMagnitude;
                bool isBetter = clusterCount > bestCount ||
                    (clusterCount == bestCount &&
                     (anchorDistance < bestAnchorDistance ||
                      (Mathf.Approximately(anchorDistance, bestAnchorDistance) &&
                       source[candidateIndex].SpawnSequence < bestSequence)));
                if (isBetter == false)
                    continue;

                selectedIndex = candidateIndex;
                bestCount = clusterCount;
                bestAnchorDistance = anchorDistance;
                bestSequence = source[candidateIndex].SpawnSequence;
            }

            if (selectedIndex >= 0)
                results.Add(source[selectedIndex]);
        }

        public static void InsertImpactCandidate(
            ArcherRainCandidate candidate,
            Vector3 center,
            int maxTargets,
            List<ArcherRainCandidate> results)
        {
            float candidateDistance = (candidate.Point - center).sqrMagnitude;
            int insertIndex = results.Count;
            for (int index = 0; index < results.Count; index++)
            {
                ArcherRainCandidate existing = results[index];
                float existingDistance = (existing.Point - center).sqrMagnitude;
                if (candidateDistance < existingDistance ||
                    (Mathf.Approximately(candidateDistance, existingDistance) && candidate.SpawnSequence < existing.SpawnSequence))
                {
                    insertIndex = index;
                    break;
                }
            }

            if (insertIndex >= maxTargets)
                return;

            results.Insert(insertIndex, candidate);
            if (results.Count > maxTargets)
                results.RemoveAt(results.Count - 1);
        }
    }

    public sealed class ArcherRainDelayedCastState
    {
        public bool IsPending { get; private set; }
        public Vector3 LockedCenter { get; private set; }
        public float ResolveAt { get; private set; }

        public void Begin(Vector3 lockedCenter, float now)
        {
            LockedCenter = lockedCenter;
            ResolveAt = now + 0.4f;
            IsPending = true;
        }

        public bool TryConsumeDue(float now, out Vector3 lockedCenter)
        {
            lockedCenter = default;
            if (IsPending == false || now < ResolveAt)
                return false;

            lockedCenter = LockedCenter;
            IsPending = false;
            return true;
        }

        public void Reset()
        {
            IsPending = false;
            LockedCenter = default;
            ResolveAt = 0.0f;
        }
    }
}
