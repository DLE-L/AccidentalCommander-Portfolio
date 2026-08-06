using UnityEngine;

namespace Lizzo.PV.Combat.Projectiles
{
    public readonly struct CombatProjectileImpactTargetCandidate
    {
        public CombatProjectileImpactTargetCandidate(
            MonsterController target,
            Vector3 point,
            long spawnSequence,
            bool isValid)
        {
            Target = target;
            Point = point;
            SpawnSequence = spawnSequence;
            IsValid = isValid;
        }

        public MonsterController Target { get; }
        public Vector3 Point { get; }
        public long SpawnSequence { get; }
        public bool IsValid { get; }
    }

    public sealed class CombatProjectileImpactTargetSelector
    {
        private readonly SelectedTarget[] _selected;
        private Vector3 _impactPoint;
        private float _radiusSquared;
        private int _limit;

        public CombatProjectileImpactTargetSelector(int capacity)
        {
            _selected = new SelectedTarget[Mathf.Max(1, capacity)];
        }

        public int Count { get; private set; }

        public void Begin(Vector3 impactPoint, float radius, int limit)
        {
            _impactPoint = impactPoint;
            _radiusSquared = Mathf.Max(0.0f, radius) * Mathf.Max(0.0f, radius);
            _limit = Mathf.Clamp(limit, 0, _selected.Length);
            Count = 0;
        }

        public void Consider(in CombatProjectileImpactTargetCandidate candidate)
        {
            if (candidate.IsValid == false || candidate.Target == null || candidate.SpawnSequence <= 0L || _limit == 0)
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

            if (insertionIndex >= _limit)
                return;

            int newCount = Mathf.Min(Count + 1, _limit);
            for (int i = newCount - 1; i > insertionIndex; i--)
                _selected[i] = _selected[i - 1];

            _selected[insertionIndex] = new SelectedTarget(candidate.Target, distanceSquared, candidate.SpawnSequence);
            Count = newCount;
        }

        public MonsterController GetTarget(int index)
        {
            if (index < 0 || index >= Count)
                throw new System.ArgumentOutOfRangeException(nameof(index));

            return _selected[index].Target;
        }

        public bool Contains(MonsterController target)
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
            public SelectedTarget(MonsterController target, float distanceSquared, long spawnSequence)
            {
                Target = target;
                DistanceSquared = distanceSquared;
                SpawnSequence = spawnSequence;
            }

            public MonsterController Target { get; }
            public float DistanceSquared { get; }
            public long SpawnSequence { get; }
        }
    }
}
