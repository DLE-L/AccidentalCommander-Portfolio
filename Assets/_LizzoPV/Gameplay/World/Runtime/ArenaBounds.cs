using UnityEngine;

namespace Lizzo.PV.Gameplay.World
{
    public sealed class ArenaBounds : MonoBehaviour
    {
        private const string RenderedGroundName = "ArenaGround";

        public const float CameraOuterPadding = 1.0f;
        public const float FriendlyActorInset = 1.5f;
        public const float PartyAnchorInset = FriendlyActorInset;

        [SerializeField] private Vector2 _size = new Vector2(100.0f, 100.0f);
        private Vector2 _renderedSize;

        public Vector2 Size => _size;
        public Vector2 RenderedSize => _renderedSize.sqrMagnitude > 0.0f ? _renderedSize : _size;

        private Rect WorldRect
        {
            get
            {
                Vector2 center = transform.position;
                return new Rect(center - _size * 0.5f, _size);
            }
        }

        private Rect RenderedWorldRect
        {
            get
            {
                Vector2 center = transform.position;
                return new Rect(center - RenderedSize * 0.5f, RenderedSize);
            }
        }

        public void Configure(Vector2 size)
        {
            _size = new Vector2(Mathf.Max(0.0f, size.x), Mathf.Max(0.0f, size.y));
            _renderedSize = _size;
            ResizeRenderedGround(_renderedSize);
        }

        public void ConfigureRenderedGround(float maximumOrthographicSize, float aspect)
        {
            float halfHeight = Mathf.Max(0.0f, maximumOrthographicSize);
            float halfWidth = halfHeight * Mathf.Max(0.0f, aspect);
            _renderedSize = _size + new Vector2(halfWidth * 2.0f, halfHeight * 2.0f);
            ResizeRenderedGround(_renderedSize);
        }

        public Vector3 ClampCameraCenter(Vector3 desiredCenter, Camera camera)
        {
            if (camera == null || camera.orthographic == false)
                return desiredCenter;

            Vector2 clamped = ClampCameraCenter(desiredCenter, camera.orthographicSize, camera.aspect);
            return new Vector3(clamped.x, clamped.y, desiredCenter.z);
        }

        public Vector2 ClampCameraCenter(Vector2 desiredCenter, float orthographicSize, float aspect)
        {
            float halfHeight = Mathf.Max(0.0f, orthographicSize);
            float halfWidth = halfHeight * Mathf.Max(0.0f, aspect);
            Rect rect = WorldRect;
            return new Vector2(
                ClampAxis(desiredCenter.x, rect.xMin + CameraOuterPadding + halfWidth, rect.xMax - CameraOuterPadding - halfWidth),
                ClampAxis(desiredCenter.y, rect.yMin + CameraOuterPadding + halfHeight, rect.yMax - CameraOuterPadding - halfHeight));
        }

        public Vector2 ClampRenderedCameraCenter(Vector2 desiredCenter, float orthographicSize, float aspect)
        {
            float halfHeight = Mathf.Max(0.0f, orthographicSize);
            float halfWidth = halfHeight * Mathf.Max(0.0f, aspect);
            Rect rect = RenderedWorldRect;
            return new Vector2(
                ClampAxis(desiredCenter.x, rect.xMin + CameraOuterPadding + halfWidth, rect.xMax - CameraOuterPadding - halfWidth),
                ClampAxis(desiredCenter.y, rect.yMin + CameraOuterPadding + halfHeight, rect.yMax - CameraOuterPadding - halfHeight));
        }

        public Vector3 ClampRenderedCameraCenter(Vector3 desiredCenter, Camera camera)
        {
            if (camera == null || camera.orthographic == false)
                return desiredCenter;

            Vector2 clamped = ClampRenderedCameraCenter(desiredCenter, camera.orthographicSize, camera.aspect);
            return new Vector3(clamped.x, clamped.y, desiredCenter.z);
        }

        public Vector2 ClampPartyAnchor(Vector2 desiredPosition)
        {
            Rect rect = WorldRect;
            return new Vector2(
                ClampAxis(desiredPosition.x, rect.xMin + PartyAnchorInset, rect.xMax - PartyAnchorInset),
                ClampAxis(desiredPosition.y, rect.yMin + PartyAnchorInset, rect.yMax - PartyAnchorInset));
        }

        public Vector2 ClampFriendlyActor(Vector2 desiredPosition)
        {
            Rect rect = WorldRect;
            return new Vector2(
                ClampAxis(desiredPosition.x, rect.xMin + FriendlyActorInset, rect.xMax - FriendlyActorInset),
                ClampAxis(desiredPosition.y, rect.yMin + FriendlyActorInset, rect.yMax - FriendlyActorInset));
        }

        public Vector2 ResolveOutsideCamera(
            Vector2 cameraCenter,
            float orthographicSize,
            float aspect,
            Vector2 direction,
            float margin)
        {
            if (direction.sqrMagnitude <= 0.0001f)
                direction = Vector2.right;

            direction.Normalize();
            cameraCenter = ClampRenderedCameraCenter(cameraCenter, orthographicSize, aspect);
            float halfHeight = Mathf.Max(0.0f, orthographicSize);
            float halfWidth = halfHeight * Mathf.Max(0.0f, aspect);
            float xDistance = Mathf.Abs(direction.x) <= 0.0001f
                ? float.PositiveInfinity
                : halfWidth / Mathf.Abs(direction.x);
            float yDistance = Mathf.Abs(direction.y) <= 0.0001f
                ? float.PositiveInfinity
                : halfHeight / Mathf.Abs(direction.y);
            float distance = Mathf.Min(xDistance, yDistance) + Mathf.Max(0.0f, margin);
            return ClampInsideRendered(cameraCenter + direction * distance);
        }

        public Vector2 ResolveBossArenaCenter(Vector2 commanderPosition, float arenaWidth, float arenaHeight, float bossOffsetFromCommander)
        {
            Vector2 desiredCenter = commanderPosition + Vector2.up * (bossOffsetFromCommander * 0.5f);
            return ClampRectCenter(desiredCenter, new Vector2(arenaWidth, arenaHeight));
        }

        public Vector2 ResolveBossSpawnPosition(Vector2 arenaCenter, float bossOffsetFromCommander)
        {
            return ClampInside(arenaCenter + Vector2.up * (bossOffsetFromCommander * 0.5f));
        }

        public Vector2 ResolveTutorialEdgeSpawn(
            Vector2 cameraCenter,
            float orthographicSize,
            float aspect,
            int edgeIndex,
            float margin,
            float tangentOffset)
        {
            Vector2 direction = edgeIndex switch
            {
                0 => Vector2.up,
                1 => Vector2.right,
                2 => Vector2.left,
                _ => Vector2.down,
            };
            Vector2 position = ResolveOutsideCamera(
                cameraCenter,
                orthographicSize,
                aspect,
                direction,
                margin);
            position += edgeIndex == 0 || edgeIndex == 3
                ? Vector2.right * tangentOffset
                : Vector2.up * tangentOffset;
            return ClampInsideRendered(position);
        }

        public Vector2 ResolveOuterEdgeSpawn(int edgeIndex, float tangentOffset)
        {
            Rect rect = RenderedWorldRect;
            float cornerInset = 1.0f;
            return edgeIndex switch
            {
                0 => new Vector2(
                    ClampAxis(rect.center.x + tangentOffset, rect.xMin + cornerInset, rect.xMax - cornerInset),
                    rect.yMax),
                1 => new Vector2(
                    rect.xMax,
                    ClampAxis(rect.center.y + tangentOffset, rect.yMin + cornerInset, rect.yMax - cornerInset)),
                2 => new Vector2(
                    rect.xMin,
                    ClampAxis(rect.center.y + tangentOffset, rect.yMin + cornerInset, rect.yMax - cornerInset)),
                _ => new Vector2(
                    ClampAxis(rect.center.x + tangentOffset, rect.xMin + cornerInset, rect.xMax - cornerInset),
                    rect.yMin),
            };
        }

        public bool Contains(Vector2 position)
        {
            Rect rect = WorldRect;
            return position.x >= rect.xMin
                && position.x <= rect.xMax
                && position.y >= rect.yMin
                && position.y <= rect.yMax;
        }

        public bool ContainsRendered(Vector2 position)
        {
            Rect rect = RenderedWorldRect;
            return position.x >= rect.xMin
                && position.x <= rect.xMax
                && position.y >= rect.yMin
                && position.y <= rect.yMax;
        }

        public bool ContainsRect(Vector2 center, Vector2 size)
        {
            Rect rect = WorldRect;
            Vector2 halfSize = size * 0.5f;
            return center.x - halfSize.x >= rect.xMin
                && center.x + halfSize.x <= rect.xMax
                && center.y - halfSize.y >= rect.yMin
                && center.y + halfSize.y <= rect.yMax;
        }

        public bool IsOutsideCameraView(Vector2 position, Vector2 cameraCenter, float orthographicSize, float aspect)
        {
            float halfHeight = Mathf.Max(0.0f, orthographicSize);
            float halfWidth = halfHeight * Mathf.Max(0.0f, aspect);
            return Mathf.Abs(position.x - cameraCenter.x) > halfWidth
                || Mathf.Abs(position.y - cameraCenter.y) > halfHeight;
        }

        private Vector2 ClampInside(Vector2 position)
        {
            Rect rect = WorldRect;
            return new Vector2(
                ClampAxis(position.x, rect.xMin, rect.xMax),
                ClampAxis(position.y, rect.yMin, rect.yMax));
        }

        private Vector2 ClampInsideRendered(Vector2 position)
        {
            Rect rect = RenderedWorldRect;
            return new Vector2(
                ClampAxis(position.x, rect.xMin, rect.xMax),
                ClampAxis(position.y, rect.yMin, rect.yMax));
        }

        private Vector2 ClampRectCenter(Vector2 desiredCenter, Vector2 size)
        {
            Rect rect = WorldRect;
            Vector2 halfSize = new Vector2(Mathf.Max(0.0f, size.x), Mathf.Max(0.0f, size.y)) * 0.5f;
            return new Vector2(
                ClampAxis(desiredCenter.x, rect.xMin + halfSize.x, rect.xMax - halfSize.x),
                ClampAxis(desiredCenter.y, rect.yMin + halfSize.y, rect.yMax - halfSize.y));
        }

        private void ResizeRenderedGround(Vector2 renderedSize)
        {
            Transform renderedGround = transform.Find(RenderedGroundName);
            if (renderedGround == null)
                return;

            SpriteRenderer[] surfaces = renderedGround.GetComponentsInChildren<SpriteRenderer>(true);
            if (surfaces.Length == 0)
            {
                Debug.LogError("[ArenaBounds] ArenaGround requires at least one SpriteRenderer.", this);
                return;
            }

            for (int index = 0; index < surfaces.Length; index++)
                surfaces[index].size = renderedSize;
        }

        private static float ClampAxis(float value, float min, float max)
        {
            if (min > max)
                return (min + max) * 0.5f;

            return Mathf.Clamp(value, min, max);
        }
    }
}
