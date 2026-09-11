using Lizzo.PV.Gameplay.Units;
using UnityEngine;

namespace Lizzo.PV.Combat.Projectiles
{
    public readonly struct CombatProjectileImpactTargetCandidate
    {
        public CombatProjectileImpactTargetCandidate(
            EnemyActor target,
            Vector3 point,
            long spawnSequence,
            bool isValid)
        {
            Target = target;
            Point = point;
            SpawnSequence = spawnSequence;
            IsValid = isValid;
        }

        public EnemyActor Target { get; }
        public Vector3 Point { get; }
        public long SpawnSequence { get; }
        public bool IsValid { get; }
    }

    public sealed class CombatProjectileImpactTargetSelector
    {
        private readonly System.Collections.Generic.List<SelectedTarget> _selected;
        private Vector3 _impactPoint;
        private float _radiusSquared;

        public CombatProjectileImpactTargetSelector(int capacity)
        {
            _selected = new System.Collections.Generic.List<SelectedTarget>(Mathf.Max(1, capacity));
        }

        public int Count { get; private set; }

        public void Begin(Vector3 impactPoint, float radius, int limit)
        {
            _impactPoint = impactPoint;
            _radiusSquared = Mathf.Max(0.0f, radius) * Mathf.Max(0.0f, radius);
            _selected.Clear();
            Count = 0;
        }

        public void Consider(in CombatProjectileImpactTargetCandidate candidate)
        {
            if (candidate.IsValid == false || candidate.Target == null || candidate.SpawnSequence <= 0L)
                return;

            for (int i = 0; i < Count; i++)
            {
                if (_selected[i].Target == candidate.Target)
                    return;
            }

            float distanceSquared = (candidate.Point - _impactPoint).sqrMagnitude;
            if (distanceSquared > _radiusSquared)
                return;

            int insertionIndex = Count;
            for (int i = 0; i < Count; i++)
            {
                SelectedTarget existing = _selected[i];
                if (distanceSquared < existing.DistanceSquared ||
                    (Mathf.Approximately(distanceSquared, existing.DistanceSquared) && candidate.SpawnSequence < existing.SpawnSequence))
                {
                    insertionIndex = i;
                    break;
                }
            }

            _selected.Insert(insertionIndex, new SelectedTarget(candidate.Target, distanceSquared, candidate.SpawnSequence));
            Count = _selected.Count;
        }

        public EnemyActor GetTarget(int index)
        {
            if (index < 0 || index >= Count)
                throw new System.ArgumentOutOfRangeException(nameof(index));

            return _selected[index].Target;
        }

        public bool Contains(EnemyActor target)
        {
            for (int i = 0; i < Count; i++)
            {
                if (_selected[i].Target == target)
                    return true;
            }

            return false;
        }

        private readonly struct SelectedTarget
        {
            public SelectedTarget(EnemyActor target, float distanceSquared, long spawnSequence)
            {
                Target = target;
                DistanceSquared = distanceSquared;
                SpawnSequence = spawnSequence;
            }

            public EnemyActor Target { get; }
            public float DistanceSquared { get; }
            public long SpawnSequence { get; }
        }
    }
}
