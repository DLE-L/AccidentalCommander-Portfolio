using System.Collections.Generic;
using Lizzo.PV.Combat;
using Lizzo.PV.P0.Presentation;
using UnityEngine;

namespace Lizzo.PV.Legion.RunCore
{
    internal static class CompanionRecordingTargetSelector
    {
        internal static bool TrySelect(
            IReadOnlyCollection<MonsterController> candidates,
            CompanionPoint origin,
            float maxRange,
            out CompanionPoint targetPosition)
        {
            return TrySelect(candidates, origin, maxRange, null, out targetPosition, out _);
        }

        internal static bool TrySelect(
            IReadOnlyCollection<MonsterController> candidates,
            CompanionPoint origin,
            float maxRange,
            ISet<int> excludedTargetIds,
            out CompanionPoint targetPosition,
            out int selectedTargetId)
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

                int candidateId = candidate.GetInstanceID();
                if (excludedTargetIds != null && excludedTargetIds.Contains(candidateId))
                    continue;

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
                selectedTargetId = 0;
                return false;
            }

            Vector3 position = selected.transform.position;
            targetPosition = new CompanionPoint(position.x, position.y);
            selectedTargetId = selected.GetInstanceID();
            return true;
        }

        internal static bool IsValid(MonsterController target)
        {
            return target != null && target.isActiveAndEnabled && target.Hp > 0;
        }
    }

    internal readonly struct CompanionRecordingEffectOwnership
    {
        internal CompanionRecordingEffectOwnership(
            int ownerId,
            CountableKillAttribution killAttribution)
        {
            OwnerId = ownerId;
            KillAttribution = killAttribution;
        }

        internal int OwnerId { get; }
        internal CountableKillAttribution KillAttribution { get; }
    }

    internal static class CompanionRecordingEffectAttribution
    {
        internal static CompanionRecordingEffectOwnership Resolve(in EffectIntent intent)
        {
            int ownerId = StableOwnerId(intent.SquadId);
            return new CompanionRecordingEffectOwnership(
                ownerId,
                new CountableKillAttribution(
                    ownerId,
                    intent.SourceCompanionId,
                    CombatKillSourceCategory.CompanionOwnedAction));
        }

        private static int StableOwnerId(string squadId)
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
