using Lizzo.PV.Flow;
using Lizzo.PV.Legion;
using Lizzo.PV.Data;
using Lizzo.PV.Gameplay.CardOffer;
using Lizzo.PV.Gameplay.Presentation;
using Lizzo.PV.Gameplay.Route;
using Lizzo.PV.Gameplay.Run;
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
            SynergyRuntimeSnapshot synergySnapshot,
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

            for (int index = 0; index < synergySnapshot.SynergyCount; index++)
            {
                SynergyStateSnapshot state = synergySnapshot.GetSynergyAt(index);
                if (state.IsActive == false)
                    continue;

                string displayName = ResolveSynergyDisplayName(state.SynergyId);
                if (string.IsNullOrEmpty(displayName))
                {
                    Debug.LogError($"[PauseBuildSummaryPresentationResolver] Missing synergy display name: {state.SynergyId}", context);
                    displayName = state.SynergyId;
                }

                Gameplay.GameplayContentSpriteProvider.TryBuildSummarySynergyIcon(state.SynergyId, out Sprite icon);
                synergyPresentations.Add(new PauseSynergyPresentation(state.SynergyId, displayName, icon));
            }

        }

        private static string ResolveSynergyDisplayName(string synergyId)
        {
            return synergyId switch
            {
                "shield-breakthrough" => "방진 돌파",
                "vulnerable-cut" => "취약 절단",
                "weakening-brew" => "쇠약 조제",
                "soul-guard" => "성혼 수호",
                "cleansing-flame" => "정화의 불꽃",
                "cremation-rite" => "화장 의식",
                "thunder-rite" => "뇌전 의식",
                "conductive-harvest" => "전도 수확",
                "hunting-harvest" => "사냥 수확",
                "tracking-hunt" => "추적 사냥",
                "target-bombardment" => "표적 포격",
                "cover-bombardment" => "엄호 포격",
                "guard-corps" => "근위대",
                "ranged-barrage" => "원거리 포화",
                "magic-rite" => "마력 의식",
                "tracking-party" => "추적 사냥대",
                "undead-march" => "망자의 행진",
                "alchemy-bombardment" => "연금 폭격",
                "assault-corps" => "돌격대",
                "sanctuary-guard" => "성역 수호대",
                _ => string.Empty,
            };
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
