using Lizzo.PV.Flow;
using Lizzo.PV.Legion;
using Lizzo.PV.Data;
using Lizzo.PV.Gameplay.CardOffer;
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

                    if (CompanionPassiveCatalog.TryGet(slot.PassiveId, out _) == false)
                    {
                        Debug.LogError($"[PauseBuildSummaryPresentationResolver] Missing passive data: {slot.PassiveId}", context);
                        passives.Add(new PausePassivePresentation(null));
                        continue;
                    }

                    if (!Gameplay.GameplayContentSpriteProvider.TryBuildSummaryPassiveIcon(slot.PassiveId, out Sprite icon))
                    {
                        Debug.LogError($"[PauseBuildSummaryPresentationResolver] Missing passive Sprite Asset binding: {slot.PassiveId}", context);
                        passives.Add(new PausePassivePresentation(null, slot.Level));
                        continue;
                    }

                    passives.Add(new PausePassivePresentation(icon, slot.Level));
                }
            }

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

                if (Gameplay.GameplayContentSpriteProvider.TryBuildSummaryCompanionIcon(state.BaseUnitId, out icon))
                {
                }
                else if (!PresentationCatalogProvider.TryGetUnit(state.BaseUnitId, out UnitPresentationSet.Entry entry))
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
