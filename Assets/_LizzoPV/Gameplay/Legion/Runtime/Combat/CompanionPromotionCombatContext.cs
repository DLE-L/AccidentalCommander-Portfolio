using System;
using System.Collections.Generic;
using Lizzo.PV.Combat;
using Lizzo.PV.Legion.Combat;
using Lizzo.PV.Gameplay.Units;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    internal sealed class CompanionPromotionCombatContext
    {
        private readonly RuntimeObjectRegistry _registry;
        private readonly ICompanionCombatRepresentativeSource _representativeSource;

        internal CompanionPromotionCombatContext(
            RuntimeObjectRegistry registry,
            ICompanionCombatRepresentativeSource representativeSource)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _representativeSource = representativeSource ?? throw new ArgumentNullException(nameof(representativeSource));
        }

        internal PlayerController Player => _registry.Player;

        internal IEnumerable<MonsterController> Enemies => _registry.Enemies;

        internal bool TryGetPromotedRepresentative(
            string baseUnitId,
            int ownerInstanceId,
            out CompanionCombatRepresentative result)
        {
            return _representativeSource.TryGetPromotedRepresentative(
                baseUnitId,
                ownerInstanceId,
                out result);
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
