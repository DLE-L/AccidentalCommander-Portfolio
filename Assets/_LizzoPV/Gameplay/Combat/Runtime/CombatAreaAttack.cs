using Lizzo.PV.Gameplay.Units;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lizzo.PV.Combat
{
    // Snapshot selection and hit routing are shared. Callers own timing, visuals and per-target
    // damage/status rules. Each attack owner keeps its own instance to isolate nested attacks.
    public sealed class CombatAreaAttack
    {
        private readonly RuntimeObjectRegistry _registry;
        private readonly ICombatImmediateHitModule _hits;
        private readonly List<ICombatImmediateHitTarget> _targets = new List<ICombatImmediateHitTarget>(16);
        private readonly Comparison<ICombatImmediateHitTarget> _compareTargets;
        private Vector3 _center;
        private CombatImmediateHitFaction _sourceFaction;

        public CombatAreaAttack(RuntimeObjectRegistry registry, ICombatImmediateHitModule hits)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _hits = hits ?? throw new ArgumentNullException(nameof(hits));
            _compareTargets = CompareTargets;
        }

        public IReadOnlyList<ICombatImmediateHitTarget> SelectTargets(CombatImmediateHitFaction sourceFaction,
            Vector3 center, float radius)
        {
            _targets.Clear();
            if (float.IsNaN(radius) || float.IsInfinity(radius) || radius < 0f)
                throw new ArgumentOutOfRangeException(nameof(radius));
            _center = center;
            _sourceFaction = sourceFaction;
            if (sourceFaction == CombatImmediateHitFaction.Enemy)
            {
                CommanderActor player = _registry.Player;
                if (ContainsTarget(sourceFaction, player, player == null ? center : player.transform.position, center, radius))
                    _targets.Add(player);
            }
            else if (sourceFaction == CombatImmediateHitFaction.Ally)
            {
                foreach (EnemyActor enemy in _registry.Enemies)
                    if (ContainsTarget(sourceFaction, enemy, enemy == null ? center : enemy.transform.position, center, radius))
                        _targets.Add(enemy);
                _targets.Sort(_compareTargets);
            }
            else throw new ArgumentOutOfRangeException(nameof(sourceFaction));
            return _targets;
        }

        public bool TryApply(int selectedIndex, in CombatImmediateHitRequest request)
        {
            if (selectedIndex < 0 || selectedIndex >= _targets.Count
                || request.Faction != _sourceFaction || !ReferenceEquals(request.Target, _targets[selectedIndex]))
                return false;
            // Do not recheck geometry: existing area effects resolve their selected snapshot,
            // even when a previous target's synchronous reaction changes the world.
            return _hits.TryApply(in request);
        }

        public void Reset() => _targets.Clear();

        // Callers provide the existing point contract (center or collider closest point).
        // Commander attacks always use the authored hurtbox rather than the body collider.
        public static bool ContainsTarget(CombatImmediateHitFaction sourceFaction, ICombatImmediateHitTarget target,
            Vector3 point, Vector3 center, float radius)
        {
            if (target == null || !target.IsAlive || target.Faction == sourceFaction) return false;
            if (sourceFaction == CombatImmediateHitFaction.Enemy && target is CommanderActor player)
                return player.IsHurtboxOverlappingCircle(center, radius);
            return (point - center).sqrMagnitude <= radius * radius;
        }

        private int CompareTargets(ICombatImmediateHitTarget left, ICombatImmediateHitTarget right)
        {
            int distance = (((EnemyActor)left).transform.position - _center).sqrMagnitude.CompareTo(
                (((EnemyActor)right).transform.position - _center).sqrMagnitude);
            return distance != 0 ? distance : ((EnemyActor)left).SpawnSequence.CompareTo(((EnemyActor)right).SpawnSequence);
        }
    }
}
