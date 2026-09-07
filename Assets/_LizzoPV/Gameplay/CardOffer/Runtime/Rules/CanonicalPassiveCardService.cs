using System;
using System.Collections.Generic;
using Lizzo.PV.Data;
using Lizzo.PV.Legion;

namespace Lizzo.PV.P0.Cards
{
    public readonly struct CanonicalPassiveCardCandidate
    {
        public CanonicalPassiveCardCandidate(CardKind cardKind, string passiveId, int nextLevel, float weight)
        { CardKind = cardKind; PassiveId = passiveId; NextLevel = nextLevel; Weight = weight; }
        public CardKind CardKind { get; }
        public string PassiveId { get; }
        public int NextLevel { get; }
        public float Weight { get; }
        public bool IsNew => NextLevel == 1;
    }

    public sealed class CanonicalPassiveCardService
    {
        readonly PartyService _party;
        public CanonicalPassiveCardService(PartyService party, PassiveRosterState roster)
        {
            _party = party ?? throw new ArgumentNullException(nameof(party));
            Roster = roster ?? throw new ArgumentNullException(nameof(roster));
        }
        public PassiveRosterState Roster { get; }

        public static bool TryGetPassiveId(CardKind kind, out string passiveId)
        {
            if (CompanionPassiveCatalog.TryGet(kind, out CompanionPassiveCatalogEntry entry))
            {
                passiveId = entry.Id;
                return true;
            }

            passiveId = null;
            return false;
        }

        public bool TryGetCandidate(CardKind kind, out CanonicalPassiveCardCandidate candidate)
        {
            candidate = default;
            if (TryGetPassiveId(kind, out string passiveId) == false) return false;
            PassiveData data = GetPassiveData(passiveId);
            if (CompanionPassiveCatalog.TryGet(passiveId, out CompanionPassiveCatalogEntry entry)
                && entry.IsCommon == false
                && HasLineage(entry.RequiredLineageId) == false)
            {
                return false;
            }
            if (Roster.CanApply(data, out PassiveRosterChangeResult change) == false) return false;
            int nextLevel = change == PassiveRosterChangeResult.New ? 1 : Roster.GetLevel(passiveId) + 1;
            float weight = data == null ? 0.0f : 1.0f;
            if (weight <= 0.0f) return false;
            candidate = new CanonicalPassiveCardCandidate(kind, passiveId, nextLevel, weight);
            return true;
        }

        public void CollectEligibleCandidates(List<CanonicalPassiveCardCandidate> destination)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            destination.Clear();
            IReadOnlyList<CompanionPassiveCatalogEntry> entries = CompanionPassiveCatalog.All;
            for (int i = 0; i < entries.Count; i++)
            {
                CardKind kind = entries[i].CardKind;
                if (TryGetCandidate(kind, out CanonicalPassiveCardCandidate candidate)) destination.Add(candidate);
            }
        }

        public bool TryApply(string passiveId, out PassiveRosterChangeResult result) => Roster.TryApply(GetPassiveData(passiveId), out result);

        public PassiveData GetPassiveData(string passiveId)
        {
            return CompanionPassiveCatalog.TryGet(passiveId, out CompanionPassiveCatalogEntry entry)
                ? CompanionPassiveCatalog.CreateData(in entry)
                : null;
        }

        bool HasLineage(string lineageId)
        {
            if (string.IsNullOrWhiteSpace(lineageId)) return true;
            IReadOnlyList<Lizzo.PV.Legion.SquadSlotState> slots = _party.GetSquadSlotSnapshot();
            for (int i = 0; i < slots.Count; i++)
                if (string.Equals(slots[i].BaseUnitId, lineageId, StringComparison.Ordinal)) return true;
            return false;
        }
    }
}
