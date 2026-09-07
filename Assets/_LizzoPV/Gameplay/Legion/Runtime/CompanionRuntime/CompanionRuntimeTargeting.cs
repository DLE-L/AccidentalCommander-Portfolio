using System.Collections.Generic;
using Lizzo.PV.Combat;
using Lizzo.PV.P0.Presentation;
using UnityEngine;

namespace Lizzo.PV.Legion.RunCore
{
    internal static class CompanionRuntimeTargetSelector
    {
        internal static bool TrySelect(
            IReadOnlyCollection<MonsterController> candidates,
            CompanionPoint origin,
            float maxRange,
            out CompanionPoint targetPosition)
        {
            MonsterController selected = null;
            float selectedDistance = float.PositiveInfinity;
            long selectedSequence = long.MaxValue;
            Vector3 worldOrigin = new Vector3(origin.X, origin.Y, 0.0f);
            float maxRangeSquared = maxRange > 0.0f ? maxRange * maxRange : float.PositiveInfinity;
            foreach (MonsterController candidate in candidates)
            {
                if (!IsValid(candidate))
                {
                    continue;
                }

                float distance = (candidate.transform.position - worldOrigin).sqrMagnitude;
                if (distance > maxRangeSquared)
                {
                    continue;
                }

                if (distance < selectedDistance
                    || (Mathf.Approximately(distance, selectedDistance)
                        && candidate.SpawnSequence < selectedSequence))
                {
                    selected = candidate;
                    selectedDistance = distance;
                    selectedSequence = candidate.SpawnSequence;
                }
            }

            if (selected == null)
            {
                targetPosition = default;
                return false;
            }

            Vector3 position = selected.transform.position;
            targetPosition = new CompanionPoint(position.x, position.y);
            return true;
        }

        internal static bool IsValid(MonsterController target)
        {
            return target != null && target.isActiveAndEnabled && target.Hp > 0;
        }
    }

    internal readonly struct CompanionRuntimeEffectOwnership
    {
        internal CompanionRuntimeEffectOwnership(
            int ownerId,
            CountableKillAttribution killAttribution)
        {
            OwnerId = ownerId;
            KillAttribution = killAttribution;
        }

        internal int OwnerId { get; }
        internal CountableKillAttribution KillAttribution { get; }
    }

    internal static class CompanionRuntimeEffectAttribution
    {
        internal static CompanionRuntimeEffectOwnership Resolve(in EffectIntent intent)
        {
            int ownerId = StableOwnerId(intent.SquadId);
            return new CompanionRuntimeEffectOwnership(
                ownerId,
                new CountableKillAttribution(
                    ownerId,
                    intent.SourceCompanionId,
                    CombatKillSourceCategory.CompanionOwnedAction));
        }

        internal static int StableOwnerId(string squadId)
        {
            unchecked
            {
                int hash = 17;
                if (squadId != null)
                {
                    for (int index = 0; index < squadId.Length; index += 1)
                    {
                        hash = hash * 31 + squadId[index];
                    }
                }

                return hash == 0 ? 1 : hash;
            }
        }
    }

}
