using UnityEngine;
using Lizzo.PV.Gameplay.World;

namespace Lizzo.PV.Gameplay.Spawning
{
    public static class SpawnPositionResolver
    {
        private const float FallbackCameraEdgeDistance = 5.0f;

        public static Vector2 ResolveOutsideCamera(Vector2 characterPosition, float minMargin, float maxMargin)
        {
            float angle = Random.Range(0, 360) * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            float margin = Random.Range(minMargin, maxMargin);
            return ResolveOutsideCamera(characterPosition, direction, margin);
        }

        public static Vector2 ResolveOutsideCamera(Vector2 characterPosition, float minMargin, float maxMargin, ArenaBounds arenaBounds)
        {
            float angle = Random.Range(0, 360) * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            float margin = Random.Range(minMargin, maxMargin);
            return ResolveOutsideCamera(characterPosition, direction, margin, arenaBounds);
        }

        public static Vector2 ResolveOutsideCamera(Vector2 characterPosition, Vector2 direction, float margin)
        {
            if (direction.sqrMagnitude <= 0.0001f)
                direction = Vector2.right;

            direction.Normalize();
            float distance = ResolveCameraEdgeDistance(direction) + Mathf.Max(0.0f, margin);
            return characterPosition + direction * distance;
        }

        public static Vector2 ResolveOutsideCamera(Vector2 characterPosition, Vector2 direction, float margin, ArenaBounds arenaBounds)
        {
            if (arenaBounds == null)
                return ResolveOutsideCamera(characterPosition, direction, margin);

            Camera camera = Camera.main;
            if (camera == null || camera.orthographic == false)
                return ResolveOutsideCamera(characterPosition, direction, margin);

            return arenaBounds.ResolveOutsideCamera(
                camera.transform.position,
                camera.orthographicSize,
                camera.aspect,
                direction,
                margin);
        }

        private static float ResolveCameraEdgeDistance(Vector2 direction)
        {
            Camera camera = Camera.main;
            if (camera == null || camera.orthographic == false)
                return FallbackCameraEdgeDistance;

            float halfHeight = camera.orthographicSize;
            float halfWidth = halfHeight * camera.aspect;
            float xDistance = Mathf.Abs(direction.x) <= 0.0001f
                ? float.PositiveInfinity
                : halfWidth / Mathf.Abs(direction.x);
            float yDistance = Mathf.Abs(direction.y) <= 0.0001f
                ? float.PositiveInfinity
                : halfHeight / Mathf.Abs(direction.y);
            return Mathf.Min(xDistance, yDistance);
        }
    }
}
