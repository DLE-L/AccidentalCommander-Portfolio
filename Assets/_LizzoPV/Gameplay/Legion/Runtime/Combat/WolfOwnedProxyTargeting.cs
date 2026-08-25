using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    public readonly struct WolfOwnedProxyTargetCandidate
    {
        public readonly int InstanceId;
        public readonly Vector3 Position;
        public readonly bool IsValid;

        public WolfOwnedProxyTargetCandidate(int instanceId, Vector3 position, bool isValid)
        {
            InstanceId = instanceId;
            Position = position;
            IsValid = isValid;
        }
    }

    public static class WolfOwnedProxyTargetSelector
    {
        public static bool TrySelectNearest(
            Vector3 ownerPosition,
            float searchRange,
            IReadOnlyList<WolfOwnedProxyTargetCandidate> candidates,
            out WolfOwnedProxyTargetCandidate target)
        {
            if (candidates == null)
                throw new ArgumentNullException(nameof(candidates));

            float bestDistance = Mathf.Max(0.0f, searchRange);
            bestDistance *= bestDistance;
            int bestInstanceId = int.MaxValue;
            int bestIndex = -1;

            for (int i = 0; i < candidates.Count; i++)
            {
                WolfOwnedProxyTargetCandidate candidate = candidates[i];
                if (candidate.IsValid == false)
                    continue;

                float distance = (candidate.Position - ownerPosition).sqrMagnitude;
                if (distance > bestDistance
                    || (Mathf.Approximately(distance, bestDistance) && candidate.InstanceId >= bestInstanceId))
                {
                    continue;
                }

                bestDistance = distance;
                bestInstanceId = candidate.InstanceId;
                bestIndex = i;
            }

            if (bestIndex < 0)
            {
                target = default;
                return false;
            }

            target = candidates[bestIndex];
            return true;
        }
    }

}
