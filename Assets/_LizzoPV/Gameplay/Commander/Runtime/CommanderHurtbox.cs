using Lizzo.PV.Gameplay.Units;
using System;
using UnityEngine;

namespace Lizzo.PV.Gameplay.Commander
{
    internal sealed class CommanderHurtbox
    {
        private readonly CommanderActor _owner;

        internal CommanderHurtbox(CommanderActor owner, CircleCollider2D collider)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            Collider = collider;
        }

        internal CircleCollider2D Collider { get; }

        internal void ValidateRequired()
        {
            if (Collider == null)
            {
                Debug.LogError("Commander prefab is missing required CombatCollider reference.", _owner);
                return;
            }

            if (Collider.isTrigger == false)
                Debug.LogError("Commander CombatCollider must be trigger.", _owner);
        }

        internal void ValidateEnabled()
        {
            if (Collider == null)
            {
                ValidateRequired();
                return;
            }

            if (Collider.enabled == false)
                Debug.LogError("Commander CombatCollider is disabled.", _owner);
        }

        internal bool OverlapsCircle(Vector2 circleCenter, float circleRadius)
        {
            if (Collider == null)
            {
                ValidateRequired();
                return false;
            }
            if (Collider.enabled == false)
                return false;

            Vector2 hurtboxCenter = ResolveCenter();
            float overlapDistance = Mathf.Max(0.0f, circleRadius) + ResolveRadius();
            return (hurtboxCenter - circleCenter).sqrMagnitude <= overlapDistance * overlapDistance;
        }

        internal bool OverlapsCapsule(Vector2 segmentStart, Vector2 segmentEnd, float radius)
        {
            if (Collider == null)
            {
                ValidateRequired();
                return false;
            }
            if (Collider.enabled == false)
                return false;

            Vector2 hurtboxCenter = ResolveCenter();
            float overlapDistance = Mathf.Max(0.0f, radius) + ResolveRadius();
            Vector2 closestPoint = GetClosestPointOnSegment(segmentStart, segmentEnd, hurtboxCenter);
            return (hurtboxCenter - closestPoint).sqrMagnitude <= overlapDistance * overlapDistance;
        }

        private Vector2 ResolveCenter()
        {
            return Collider.transform.TransformPoint(Collider.offset);
        }

        private float ResolveRadius()
        {
            float maxScale = Mathf.Max(
                Mathf.Abs(Collider.transform.lossyScale.x),
                Mathf.Abs(Collider.transform.lossyScale.y));
            return Collider.radius * maxScale;
        }

        private static Vector2 GetClosestPointOnSegment(Vector2 segmentStart, Vector2 segmentEnd, Vector2 point)
        {
            Vector2 segment = segmentEnd - segmentStart;
            float lengthSqr = segment.sqrMagnitude;
            if (lengthSqr <= 0.000001f)
                return segmentStart;

            float t = Vector2.Dot(point - segmentStart, segment) / lengthSqr;
            return segmentStart + segment * Mathf.Clamp01(t);
        }
    }

}
