using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    public static class CompanionReturningAttackTargetSelector
    {
        public static void Collect(
            IReadOnlyList<TargetAreaImpactCandidate> source,
            Vector3 start,
            Vector3 end,
            float width,
            int maxTargets,
            ReturningAttackPass pass,
            List<TargetAreaImpactCandidate> results)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (results == null)
                throw new ArgumentNullException(nameof(results));

            results.Clear();
            Vector3 segment = end - start;
            float segmentLengthSquared = segment.sqrMagnitude;
            float widthSquared = Mathf.Max(0.0f, width);
            widthSquared *= widthSquared;
            int limit = Mathf.Max(0, maxTargets);
            if (segmentLengthSquared <= 0.0001f || limit == 0)
                return;

            for (int index = 0; index < source.Count; index += 1)
            {
                TargetAreaImpactCandidate candidate = source[index];
                float progress = Mathf.Clamp01(Vector3.Dot(candidate.Point - start, segment) / segmentLengthSquared);
                Vector3 closest = start + segment * progress;
                if ((candidate.Point - closest).sqrMagnitude > widthSquared)
                    continue;

                int insertIndex = 0;
                while (insertIndex < results.Count)
                {
                    TargetAreaImpactCandidate existing = results[insertIndex];
                    float existingProgress = Mathf.Clamp01(Vector3.Dot(existing.Point - start, segment) / segmentLengthSquared);
                    bool comesFirst = pass == ReturningAttackPass.Outbound
                        ? progress < existingProgress
                            || (Mathf.Approximately(progress, existingProgress) && candidate.InstanceId < existing.InstanceId)
                        : progress > existingProgress
                            || (Mathf.Approximately(progress, existingProgress) && candidate.InstanceId < existing.InstanceId);
                    if (comesFirst)
                        break;
                    insertIndex += 1;
                }

                if (insertIndex >= limit)
                    continue;

                results.Insert(insertIndex, candidate);
                if (results.Count > limit)
                    results.RemoveAt(limit);
            }
        }
    }
}
