using UnityEngine;

namespace Lizzo.PV.Combat
{
    /// <summary>
    /// Authored world-space constraints for combat displacement. The obstacle set is explicit so
    /// current maps without authored obstacles retain their existing collision interactions.
    /// </summary>
    public sealed class SafeKnockbackWorld : MonoBehaviour
    {
        private const float ObstacleSkin = 0.01f;
        private const int PointSearchRings = 12;
        private static readonly Vector2[] PointSearchDirections =
        {
            Vector2.right,
            new Vector2(0.9238795f, 0.3826834f),
            new Vector2(0.7071068f, 0.7071068f),
            new Vector2(0.3826834f, 0.9238795f),
            Vector2.up,
            new Vector2(-0.3826834f, 0.9238795f),
            new Vector2(-0.7071068f, 0.7071068f),
            new Vector2(-0.9238795f, 0.3826834f),
            Vector2.left,
            new Vector2(-0.9238795f, -0.3826834f),
            new Vector2(-0.7071068f, -0.7071068f),
            new Vector2(-0.3826834f, -0.9238795f),
            Vector2.down,
            new Vector2(0.3826834f, -0.9238795f),
            new Vector2(0.7071068f, -0.7071068f),
            new Vector2(0.9238795f, -0.3826834f),
        };

        [SerializeField] private Collider2D _boundaryCollider;
        [SerializeField] private Collider2D[] _obstacleColliders = System.Array.Empty<Collider2D>();

        private readonly RaycastHit2D[] _castHits = new RaycastHit2D[16];

        public Collider2D BoundaryCollider => _boundaryCollider;
        public int ExplicitObstacleCount => _obstacleColliders == null ? 0 : _obstacleColliders.Length;

        public void ConfigureForRuntime(Collider2D boundaryCollider, Collider2D[] obstacleColliders)
        {
            _boundaryCollider = boundaryCollider;
            _obstacleColliders = obstacleColliders ?? System.Array.Empty<Collider2D>();
        }

        public Vector2 ResolveDisplacement(Collider2D mover, Vector2 requestedDisplacement)
        {
            if (mover == null || requestedDisplacement.sqrMagnitude <= 0.000001f)
                return Vector2.zero;

            Vector2 clampedDisplacement = ClampToBoundary(mover, requestedDisplacement);
            if (clampedDisplacement.sqrMagnitude <= 0.000001f)
                return Vector2.zero;

            return TruncateAtExplicitObstacle(mover, clampedDisplacement);
        }

        public bool TryResolveNearestValidPoint(Vector2 desiredPoint, float radius, out Vector2 point)
        {
            if (IsValidPoint(desiredPoint))
            {
                point = desiredPoint;
                return true;
            }

            if (radius <= 0.0f)
            {
                point = default;
                return false;
            }

            float step = radius / PointSearchRings;
            for (int ring = 1; ring <= PointSearchRings; ring++)
            {
                float distance = ring * step;
                for (int direction = 0; direction < PointSearchDirections.Length; direction++)
                {
                    Vector2 candidate = desiredPoint + PointSearchDirections[direction] * distance;
                    if (IsValidPoint(candidate))
                    {
                        point = candidate;
                        return true;
                    }
                }
            }

            point = default;
            return false;
        }

        private Vector2 ClampToBoundary(Collider2D mover, Vector2 requestedDisplacement)
        {
            if (_boundaryCollider == null || _boundaryCollider.enabled == false)
                return requestedDisplacement;

            Bounds boundaryBounds = _boundaryCollider.bounds;
            Bounds moverBounds = mover.bounds;
            Vector2 extents = moverBounds.extents;
            Vector2 minimum = (Vector2)boundaryBounds.min + extents;
            Vector2 maximum = (Vector2)boundaryBounds.max - extents;
            if (minimum.x > maximum.x || minimum.y > maximum.y)
                return Vector2.zero;

            Vector2 center = moverBounds.center;
            Vector2 desiredCenter = center + requestedDisplacement;
            Vector2 clampedCenter = new Vector2(
                Mathf.Clamp(desiredCenter.x, minimum.x, maximum.x),
                Mathf.Clamp(desiredCenter.y, minimum.y, maximum.y));
            return clampedCenter - center;
        }

        private Vector2 TruncateAtExplicitObstacle(Collider2D mover, Vector2 requestedDisplacement)
        {
            if (_obstacleColliders == null || _obstacleColliders.Length == 0)
                return requestedDisplacement;

            float distance = requestedDisplacement.magnitude;
            Vector2 direction = requestedDisplacement / distance;
            ContactFilter2D filter = new ContactFilter2D();
            filter.NoFilter();
            filter.useTriggers = false;
            int hitCount = mover.Cast(direction, filter, _castHits, distance);
            float firstObstacleDistance = distance;
            bool foundObstacle = false;

            for (int hitIndex = 0; hitIndex < hitCount; hitIndex++)
            {
                Collider2D hitCollider = _castHits[hitIndex].collider;
                if (IsExplicitObstacle(hitCollider) == false || IsMoverCollider(mover, hitCollider))
                    continue;

                float hitDistance = _castHits[hitIndex].distance;
                if (hitDistance < firstObstacleDistance)
                {
                    firstObstacleDistance = hitDistance;
                    foundObstacle = true;
                }
            }

            if (foundObstacle == false)
                return requestedDisplacement;

            return direction * Mathf.Max(0.0f, firstObstacleDistance - ObstacleSkin);
        }

        private bool IsExplicitObstacle(Collider2D collider)
        {
            if (collider == null || collider.isTrigger)
                return false;

            for (int index = 0; index < _obstacleColliders.Length; index++)
            {
                if (_obstacleColliders[index] == collider)
                    return true;
            }

            return false;
        }

        private bool IsValidPoint(Vector2 point)
        {
            if (_boundaryCollider != null
                && _boundaryCollider.enabled
                && _boundaryCollider.OverlapPoint(point) == false)
            {
                return false;
            }

            if (_obstacleColliders == null)
                return true;

            for (int index = 0; index < _obstacleColliders.Length; index++)
            {
                Collider2D obstacle = _obstacleColliders[index];
                if (obstacle != null
                    && obstacle.enabled
                    && obstacle.isTrigger == false
                    && obstacle.OverlapPoint(point))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsMoverCollider(Collider2D mover, Collider2D collider)
        {
            return collider == mover || collider.transform.IsChildOf(mover.transform) || mover.transform.IsChildOf(collider.transform);
        }
    }
}
