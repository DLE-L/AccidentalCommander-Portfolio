using Lizzo.PV.Flow;
using Lizzo.PV.Legion;
using Lizzo.PV.Legion.Synergy;
using Lizzo.PV.Data;
using Lizzo.PV.P0.Cards;
using Lizzo.PV.P0.Cards.CardOffer;
using Lizzo.PV.P0.Presentation;
using Lizzo.PV.Gameplay.Route;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lizzo.PV.UI
{
    public static class PauseBuildSummaryPresentationResolver
    {
        public static void Fill(
            IReadOnlyList<SquadSlotState> squadSnapshot,
            PassiveRosterState passiveRoster,
            SynergyActivationState synergies,
            IDataProvider data,
            List<PauseCompanionPresentation> companions,
            List<PausePassivePresentation> passives,
            List<PauseSynergyPresentation> synergyPresentations,
            int maxCompanionEntries,
            int maxPassiveEntries,
            UnityEngine.Object context)
        {
            PauseCompanionPresentationResolver.Fill(squadSnapshot, companions, maxCompanionEntries, context);
            passives.Clear();
            synergyPresentations.Clear();

            if (passiveRoster != null && data != null)
            {
                IReadOnlyList<PassiveSlotState> snapshot = passiveRoster.Snapshot;
                for (int i = 0; i < snapshot.Count && passives.Count < maxPassiveEntries; i++)
                {
                    PassiveSlotState slot = snapshot[i];
                    if (slot == null || slot.IsEmpty)
                        continue;

                    if (data.GetPassive(slot.PassiveId) == null)
                    {
                        Debug.LogError($"[PauseBuildSummaryPresentationResolver] Missing passive data: {slot.PassiveId}", context);
                        passives.Add(new PausePassivePresentation(null));
                        continue;
                    }

                    if (TryGetPassiveCardKind(slot.PassiveId, out CardKind kind) == false
                        || PresentationCatalogProvider.TryGetCard(kind.ToString(), out CardPresentationSet.Entry entry) == false
                        || entry == null
                        || entry.Icon == null)
                    {
                        passives.Add(new PausePassivePresentation(null, slot.Level));
                        continue;
                    }

                    passives.Add(new PausePassivePresentation(entry.Icon, slot.Level));
                }
            }

            if (synergies == null || data == null)
                return;

            IReadOnlyList<SynergyActivationSnapshot> snapshots = synergies.Snapshot;
            for (int i = 0; i < snapshots.Count; i++)
            {
                SynergyActivationSnapshot snapshot = snapshots[i];
                if (snapshot.IsActive == false)
                    continue;

                SynergyData synergy = data.GetSynergy(snapshot.SynergyId);
                if (synergy == null || string.IsNullOrWhiteSpace(synergy.DisplayName))
                {
                    Debug.LogError($"[PauseBuildSummaryPresentationResolver] Missing synergy display data: {snapshot.SynergyId}", context);
                    continue;
                }

                synergyPresentations.Add(new PauseSynergyPresentation(snapshot.SynergyId, synergy.DisplayName));
            }
        }

        static bool TryGetPassiveCardKind(string passiveId, out CardKind kind)
        {
            int first = (int)CardKind.PassiveMeleeTraining;
            int last = (int)CardKind.PassiveSupplyPouch;
            for (int value = first; value <= last; value++)
            {
                CardKind candidate = (CardKind)value;
                if (CanonicalPassiveCardService.TryGetPassiveId(candidate, out string candidateId)
                    && candidateId == passiveId)
                {
                    kind = candidate;
                    return true;
                }
            }

            kind = default;
            return false;
        }
    }

    public static class PauseCompanionPresentationResolver
    {
        public static void Fill(
            IReadOnlyList<SquadSlotState> squadSnapshot,
            List<PauseCompanionPresentation> presentations,
            int maxEntries,
            UnityEngine.Object context)
        {
            presentations.Clear();
            if (squadSnapshot == null)
                return;

            for (int i = 0; i < squadSnapshot.Count && presentations.Count < maxEntries; i++)
            {
                SquadSlotState state = squadSnapshot[i];
                Sprite icon = null;
                if (state.IsActive == false)
                {
                    presentations.Add(new PauseCompanionPresentation(null, 0));
                    continue;
                }

                if (!PresentationCatalogProvider.TryGetUnit(state.BaseUnitId, out UnitPresentationSet.Entry entry))
                {
                    Debug.LogError($"[GameplayUIController] Missing UnitPresentationSet entry for active companion: {state.BaseUnitId} (roster slot: {state.SlotId})", context);
                }
                else if (entry.Portrait == null)
                {
                    Debug.LogError($"[GameplayUIController] Missing UnitPresentationSet Portrait for active companion: {state.BaseUnitId} (roster slot: {state.SlotId})", context);
                }
                else
                {
                    icon = entry.Portrait;
                }

                presentations.Add(new PauseCompanionPresentation(icon, state.CurrentCount));
            }
        }
    }
}
