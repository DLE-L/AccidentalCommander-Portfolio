using System;
using System.Collections.Generic;
using Lizzo.PV.Combat;
using Lizzo.PV.Legion.Combat;
using Lizzo.PV.P0.Units;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    internal sealed class CompanionPromotionCombatContext
    {
        private readonly PartyService _party;
        private readonly RuntimeObjectRegistry _registry;
        private ICompanionCombatRepresentativeSource _representativeSource;

        internal CompanionPromotionCombatContext(
            PartyService party,
            RuntimeObjectRegistry registry)
        {
            _party = party ?? throw new ArgumentNullException(nameof(party));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        internal PlayerController Player => _registry.Player;

        internal IEnumerable<MonsterController> Enemies => _registry.Enemies;

        internal void BindRepresentativeSource(ICompanionCombatRepresentativeSource source)
        {
            _representativeSource = source ?? throw new ArgumentNullException(nameof(source));
        }

        internal bool TryGetPromotedRepresentative(
            string baseUnitId,
            int ownerInstanceId,
            out CompanionCombatRepresentative result)
        {
            if (_representativeSource != null)
            {
                return _representativeSource.TryGetPromotedRepresentative(
                    baseUnitId,
                    ownerInstanceId,
                    out result);
            }

            result = default;
            int lowestInstanceId = int.MaxValue;
            IReadOnlyList<CompanionRuntime> companions = _party.ActiveCompanions;
            for (int index = 0; index < companions.Count; index++)
            {
                CompanionRuntime companion = companions[index];
                if (companion == null
                    || companion.IsDown
                    || companion.IsPromoted == false
                    || companion.BaseUnitId != baseUnitId
                    || (ownerInstanceId != 0 && companion.GetInstanceID() != ownerInstanceId))
                {
                    continue;
                }

                int instanceId = companion.GetInstanceID();
                if (instanceId >= lowestInstanceId)
                    continue;

                result = new CompanionCombatRepresentative(
                    instanceId,
                    companion.RosterSlotId,
                    companion.BaseUnitId,
                    companion.transform);
                lowestInstanceId = instanceId;
            }

            return result.IsValid;
        }

        internal void CollectPromotionTargets(List<CompanionPromotionTargetCandidate> targets)
        {
            targets.Clear();
            foreach (MonsterController target in _registry.Enemies)
            {
                if (target == null || target.IsValid() == false)
                    continue;

                targets.Add(new CompanionPromotionTargetCandidate(
                    target,
                    target.transform.position,
                    target.GetInstanceID(),
                    target.Hp,
                    target.IsBoss,
                    target.IsElite));
            }
        }

        internal void CollectAreaTargets(
            Vector3 origin,
            List<TargetAreaImpactCandidate> targets,
            MonsterController excluded = null)
        {
            targets.Clear();
            foreach (MonsterController target in _registry.Enemies)
            {
                if (target == null || target == excluded || target.IsValid() == false)
                    continue;

                targets.Add(new TargetAreaImpactCandidate(
                    target,
                    AllyTargeting.ResolveTargetPoint(target, origin),
                    target.GetInstanceID()));
            }
        }

        internal CountableKillAttribution CreateAttribution(
            CompanionCombatRepresentative representative,
            string sourceId)
        {
            return new CountableKillAttribution(
                representative.OwnerInstanceId,
                sourceId,
                CombatKillSourceCategory.CompanionOwnedAction);
        }

        internal void ReleaseProjectilesBySourceId(string sourceId)
        {
            _registry.ReleaseProjectilesBySourceId(sourceId);
        }
    }
}
