using Lizzo.PV.Gameplay.Units;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    internal static class AllyTargeting
    {
        internal static Vector3 ResolveTargetPoint(MonsterController target, Vector3 sourcePosition)
        {
            if (target == null)
                return sourcePosition;

            Collider2D collider = target.CombatCollider;
            if (collider == null || collider.enabled == false)
                return target.transform.position;

            Vector2 closestPoint = collider.ClosestPoint(sourcePosition);
            return new Vector3(closestPoint.x, closestPoint.y, target.transform.position.z);
        }
    }

}
