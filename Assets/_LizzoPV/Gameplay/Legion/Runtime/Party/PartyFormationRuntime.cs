using System;
using System.Collections.Generic;
using Lizzo.PV.Gameplay.World;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    public sealed partial class PartyService
    {
        internal void BindArenaBounds(ArenaBounds arenaBounds)
        {
            _formation.BindArenaBounds(arenaBounds);
        }

        internal CompanionGrowthScale ResolveGrowthScale(string baseUnitId)
        {
            return RosterView.TryGetSlot(baseUnitId, out SquadSlotState slot)
                ? _companionGrowthScale.Resolve(slot)
                : new CompanionGrowthScale(1.0f, 1.0f, 1.0f, 1);
        }

        internal bool TryResolveFormationAnchor(string rosterSlotId, out Vector3 anchor)
        {
            return _formation.TryResolveFormationAnchor(rosterSlotId, out anchor);
        }

        internal bool TryResolveSynergyAnchorAndRange(string rosterSlotId, out Vector3 anchor, out float attackRange)
        {
            if (_companionCombatAnchorSource != null
                && _companionCombatAnchorSource.TryGetRepresentativeAnchor(rosterSlotId, out anchor, out attackRange))
            {
                return true;
            }

            return _formation.TryResolveSynergyAnchorAndRange(rosterSlotId, out anchor, out attackRange);
        }

        /// <summary>Collects exactly one living canonical Beast actor per immutable roster slot.
        /// The formation SlotId ordering is the approved reinforced-squad representative rule.</summary>
        internal void CollectLivingBeastRepresentatives(List<CompanionRuntime> results)
        {
            if (results == null) throw new ArgumentNullException(nameof(results));
            results.Clear();
            for (int i = 0; i < Companions.Count; i++)
            {
                CompanionRuntime candidate = Companions[i];
                if (candidate == null || candidate.IsDown || string.IsNullOrEmpty(candidate.RosterSlotId)
                    || string.IsNullOrEmpty(candidate.SlotId) || HasFamilyTag(candidate.FamilyTags, "beast_family") == false)
                    continue;

                int existingIndex = -1;
                for (int resultIndex = 0; resultIndex < results.Count; resultIndex++)
                {
                    if (results[resultIndex].RosterSlotId == candidate.RosterSlotId)
                    {
                        existingIndex = resultIndex;
                        break;
                    }
                }

                if (existingIndex < 0)
                    results.Add(candidate);
                else if (string.CompareOrdinal(candidate.SlotId, results[existingIndex].SlotId) < 0)
                    results[existingIndex] = candidate;
            }
        }

        static bool HasFamilyTag(string values, string required)
        {
            if (string.IsNullOrEmpty(values) || string.IsNullOrEmpty(required)) return false;
            int start = 0;
            for (int index = 0; index <= values.Length; index++)
            {
                if (index != values.Length && values[index] != ',') continue;
                int length = index - start;
                if (length == required.Length && string.CompareOrdinal(values, start, required, 0, length) == 0)
                    return true;
                start = index + 1;
            }
            return false;
        }

    }
}
