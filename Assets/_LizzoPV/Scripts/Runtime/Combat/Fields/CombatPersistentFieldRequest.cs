using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lizzo.PV.Combat.Fields
{
    public readonly struct CombatPersistentFieldRequest
    {
        public string SourceId { get; }
        public int OwnerId { get; }
        public Vector3 Center { get; }
        public int Damage { get; }
        public float Radius { get; }
        public float TickInterval { get; }
        public float Duration { get; }
        public int MaxTargets { get; }
        public int MaxActiveFields { get; }

        public bool IsValid => string.IsNullOrEmpty(SourceId) == false
            && OwnerId != 0
            && Damage > 0
            && Radius > 0.0f
            && TickInterval > 0.0f
            && Duration > 0.0f
            && MaxTargets > 0
            && MaxActiveFields > 0;

        private CombatPersistentFieldRequest(
            string sourceId,
            int ownerId,
            Vector3 center,
            int damage,
            float radius,
            float tickInterval,
            float duration,
            int maxTargets,
            int maxActiveFields)
        {
            SourceId = sourceId;
            OwnerId = ownerId;
            Center = center;
            Damage = damage;
            Radius = radius;
            TickInterval = tickInterval;
            Duration = duration;
            MaxTargets = maxTargets;
            MaxActiveFields = maxActiveFields;
        }

        public static CombatPersistentFieldRequest CreateAllyDamage(
            string sourceId,
            int ownerId,
            Vector3 center,
            int damage,
            float radius,
            float tickInterval,
            float duration,
            int maxTargets,
            int maxActiveFields)
        {
            return new CombatPersistentFieldRequest(
                sourceId,
                ownerId,
                center,
                damage,
                radius,
                tickInterval,
                duration,
                maxTargets,
                maxActiveFields);
        }
    }

    public readonly struct CombatPersistentFieldTarget
    {
        public ICombatImmediateHitTarget Target { get; }
        public Vector3 Point { get; }
        public int InstanceId { get; }

        public CombatPersistentFieldTarget(ICombatImmediateHitTarget target, Vector3 point, int instanceId)
        {
            Target = target;
            Point = point;
            InstanceId = instanceId;
        }
    }

    public interface ICombatPersistentFieldTargetSource
    {
        void CollectTargets(Vector3 center, List<CombatPersistentFieldTarget> destination);
    }
}
