using System;
using Lizzo.PV.Legion.Presentation;
using Lizzo.PV.P0.Presentation;
using Lizzo.PV.P0.Visuals;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    internal static partial class CompanionConstructionModule
    {
        internal static AllyFollower CreateCanonicalCompanion(
            this PartyService party,
            Transform player,
            CompanionRuntimeSpec spec,
            int index)
        {
            if (player == null)
                throw new InvalidOperationException("Canonical companion spawn requires a player transform.");
            if (spec == null || string.IsNullOrWhiteSpace(spec.PresentedUnitId))
                throw new InvalidOperationException("Canonical companion runtime spec is invalid.");
            if (PresentationCatalogProvider.TryGetUnit(spec.PresentedUnitId, out UnitPresentationSet.Entry presentation) == false
                || presentation.Prefab == null
                || string.IsNullOrWhiteSpace(presentation.AddressableKey))
            {
                throw new InvalidOperationException($"Canonical companion presentation is missing: {spec.PresentedUnitId}");
            }

            GameObject allyObject = null;
            AllyFollower follower = null;
            CompanionRuntime companion = null;
            try
            {
                PartyService.FormationSlot slot = party.GetRoleSlot(spec, index);
                if (party.Roster.TryGetPreviewSlotId(spec.BaseUnitId, out string rosterSlotId) == false)
                    throw new InvalidOperationException($"Canonical roster slot is missing: {spec.BaseUnitId}");
                allyObject = party.InstantiateAllyPrefab(presentation.AddressableKey, spec.PresentedUnitId);
                allyObject.transform.position = player.position + slot.Offset;
                allyObject.transform.localScale = Vector3.one;

                party.RequireComponent<CommanderAllyVisual>(allyObject);
                AllyCombat combat = party.RequireComponent<AllyCombat>(allyObject);
                follower = party.RequireComponent<AllyFollower>(allyObject);
                companion = party.RegisterCompanion(allyObject, spec, slot.Id, rosterSlotId);
                follower.BindParty(party);
                follower.SetDirectionalTarget(player, slot.Offset, party.ResolveFollowSpeed(spec.MoveSpeed), slot.Id);
                party.Allies.Add(follower);

                if (party.ApplyCanonicalTargetAreaCombat(combat, spec.BaseUnitId) == false
                    && party.ApplyCanonicalProjectileCombat(combat, spec.BaseUnitId) == false
                    && party.ApplyCanonicalPersistentFieldCombat(combat, spec.BaseUnitId) == false
                    && party.ApplyCanonicalChainCombat(combat, spec.BaseUnitId) == false
                    && party.ApplyCanonicalWolfCombat(combat, spec.BaseUnitId) == false
                    && party.ApplyCanonicalWraithCombat(combat, spec.BaseUnitId) == false
                    && party.ApplyCanonicalRangedSupportCombat(combat, spec.BaseUnitId) == false)
                {
                    throw new InvalidOperationException($"Canonical combat setup is missing: {spec.BaseUnitId}");
                }

                if (spec.BaseUnitId == "wolf_tamer")
                    party.ConfigureCanonicalWolfSupportPresenter(allyObject, combat);

                return follower;
            }
            catch
            {
                if (follower != null)
                    party.Allies.Remove(follower);
                if (companion != null)
                    party.Companions.Remove(companion);
                if (allyObject != null)
                    party.Factory.Release(allyObject);
                throw;
            }
        }

        internal static void ReleaseCanonicalCompanion(this PartyService party, AllyFollower follower)
        {
            if (follower == null)
                return;

            party.Allies.Remove(follower);
            party.RemoveCompanion(follower);
            party.Factory.Release(follower.gameObject);
        }

        private static void ConfigureCanonicalWolfSupportPresenter(this PartyService party, GameObject allyObject, AllyCombat combat)
        {
            if (PresentationCatalogProvider.TryGetOwnedSupport("grey_wolf_support", out OwnedSupportPresentationSet.Entry support) == false
                || string.IsNullOrWhiteSpace(support.AddressableKey))
            {
                throw new InvalidOperationException("Canonical Wolf support presentation is missing.");
            }

            OwnerBoundSupportPresenterBehaviour presenter = party.RequireComponent<OwnerBoundSupportPresenterBehaviour>(allyObject);
            presenter.Configure(
                combat,
                new OwnerBoundSupportPresentationData(
                    support.Id,
                    support.AddressableKey,
                    support.RunCategory,
                    support.AttackCategory),
                party.Factory);
        }

    }
}
