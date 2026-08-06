using Lizzo.PV.Legion;
using UnityEngine;

namespace Lizzo.PV.Combat.Projectiles
{
    public enum CombatProjectileDeliveryMode
    {
        StraightCollision,
        HomingTarget,
    }

    public enum CombatProjectileFaction
    {
        Ally,
        Enemy,
    }

    public readonly struct CombatProjectileRequest
    {
        public string SourceId { get; }
        public CreatureController Source { get; }
        public CompanionRuntime SourceRuntime { get; }
        public CombatProjectileFaction Faction { get; }
        public CombatProjectileDeliveryMode DeliveryMode { get; }
        public Vector3 Origin { get; }
        public Vector3 Direction { get; }
        public MonsterController Target { get; }
        public int Damage { get; }
        public int MaxDistinctTargetHits { get; }
        public float AttackCollisionSize { get; }
        public float Speed { get; }
        public float Lifetime { get; }
        public float ArrivalDistance { get; }
        public RetroVfxKind StraightHitFeedback { get; }
        public AttackVisualKind HomingHitFeedback { get; }
        public CountableKillAttribution KillAttribution { get; }

        public bool IsValid
        {
            get
            {
                if (string.IsNullOrWhiteSpace(SourceId) || Damage <= 0 || MaxDistinctTargetHits < 1 || MaxDistinctTargetHits > 4 || AttackCollisionSize <= 0.0f || Speed <= 0.0f || Lifetime <= 0.0f)
                    return false;

                if (DeliveryMode == CombatProjectileDeliveryMode.StraightCollision)
                    return Direction.sqrMagnitude > 0.0001f;

                if (DeliveryMode != CombatProjectileDeliveryMode.HomingTarget)
                    return false;

                return Target != null && Target.IsValid() && ArrivalDistance >= 0.0f;
            }
        }

        private CombatProjectileRequest(
            string sourceId,
            CreatureController source,
            CompanionRuntime sourceRuntime,
            CombatProjectileFaction faction,
            CombatProjectileDeliveryMode deliveryMode,
            Vector3 origin,
            Vector3 direction,
            MonsterController target,
            int damage,
            int maxDistinctTargetHits,
            float attackCollisionSize,
            float speed,
            float lifetime,
            float arrivalDistance,
            RetroVfxKind straightHitFeedback,
            AttackVisualKind homingHitFeedback,
            CountableKillAttribution killAttribution)
        {
            SourceId = sourceId;
            Source = source;
            SourceRuntime = sourceRuntime;
            Faction = faction;
            DeliveryMode = deliveryMode;
            Origin = origin;
            Direction = direction;
            Target = target;
            Damage = damage;
            MaxDistinctTargetHits = maxDistinctTargetHits;
            AttackCollisionSize = attackCollisionSize;
            Speed = speed;
            Lifetime = lifetime;
            ArrivalDistance = arrivalDistance;
            StraightHitFeedback = straightHitFeedback;
            HomingHitFeedback = homingHitFeedback;
            KillAttribution = killAttribution;
        }

        public static CombatProjectileRequest CreateStraight(
            string sourceId,
            CreatureController source,
            Vector3 origin,
            Vector3 direction,
            int damage,
            float speed,
            float lifetime,
            RetroVfxKind hitFeedback,
            CombatProjectileFaction faction = CombatProjectileFaction.Ally,
            CountableKillAttribution killAttribution = default,
            int maxDistinctTargetHits = 1,
            float attackCollisionSize = 0.22f)
        {
            return new CombatProjectileRequest(
                sourceId,
                source,
                null,
                faction,
                CombatProjectileDeliveryMode.StraightCollision,
                origin,
                direction,
                null,
                damage,
                maxDistinctTargetHits,
                attackCollisionSize,
                speed,
                lifetime,
                0.0f,
                hitFeedback,
                AttackVisualKind.SingleHit,
                killAttribution);
        }

        public static CombatProjectileRequest CreateHoming(
            string sourceId,
            CreatureController source,
            CompanionRuntime sourceRuntime,
            Vector3 origin,
            MonsterController target,
            int damage,
            float speed,
            float lifetime,
            float arrivalDistance,
            AttackVisualKind hitFeedback,
            CombatProjectileFaction faction = CombatProjectileFaction.Ally,
            CountableKillAttribution killAttribution = default)
        {
            return new CombatProjectileRequest(
                sourceId,
                source,
                sourceRuntime,
                faction,
                CombatProjectileDeliveryMode.HomingTarget,
                origin,
                Vector3.zero,
                target,
                damage,
                1,
                0.22f,
                speed,
                lifetime,
                arrivalDistance,
                RetroVfxKind.ProjectileHit,
                hitFeedback,
                killAttribution);
        }
    }
}
