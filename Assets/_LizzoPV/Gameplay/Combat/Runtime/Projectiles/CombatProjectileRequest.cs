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

    public static class CombatProjectilePresentationIds
    {
        public const string CommanderBasic = "commander_basic";
        public const string CommanderRapidCrossbow = "commander_rapid_crossbow";
        public const string CommanderPiercingSpear = "commander_piercing_spear";
        public const string CommanderBlastStaff = "commander_blast_staff";
        public const string MagicChain = "synergy_magic_chain";
    }

    public readonly struct CombatProjectileRequest
    {
        public string SourceId { get; }
        public string PresentationId { get; }
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
        public float ImpactRadius { get; }
        public int ImpactMaxTargets { get; }
        public float Speed { get; }
        public float Lifetime { get; }
        public float ArrivalDistance { get; }
        public RetroVfxKind StraightHitFeedback { get; }
        public AttackVisualKind HomingHitFeedback { get; }
        public CountableKillAttribution KillAttribution { get; }
        public bool HasImpactArea => ImpactRadius > 0.0f && ImpactMaxTargets > 0;

        public bool IsValid
        {
            get
            {
                if (string.IsNullOrWhiteSpace(SourceId) || string.IsNullOrWhiteSpace(PresentationId) || Damage <= 0 || MaxDistinctTargetHits < 1 || MaxDistinctTargetHits > 4 || AttackCollisionSize <= 0.0f || Speed <= 0.0f || Lifetime <= 0.0f)
                    return false;

                if ((ImpactRadius <= 0.0f) != (ImpactMaxTargets <= 0) || ImpactMaxTargets > 8)
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
            string presentationId,
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
            float impactRadius,
            int impactMaxTargets,
            float speed,
            float lifetime,
            float arrivalDistance,
            RetroVfxKind straightHitFeedback,
            AttackVisualKind homingHitFeedback,
            CountableKillAttribution killAttribution)
        {
            SourceId = sourceId;
            PresentationId = string.IsNullOrWhiteSpace(presentationId) ? sourceId : presentationId;
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
            ImpactRadius = impactRadius;
            ImpactMaxTargets = impactMaxTargets;
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
            float attackCollisionSize = 0.22f,
            float impactRadius = 0.0f,
            int impactMaxTargets = 0,
            string presentationId = null)
        {
            return new CombatProjectileRequest(
                sourceId,
                presentationId,
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
                impactRadius,
                impactMaxTargets,
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
            CountableKillAttribution killAttribution = default,
            string presentationId = null)
        {
            return new CombatProjectileRequest(
                sourceId,
                presentationId,
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
                0.0f,
                0,
                speed,
                lifetime,
                arrivalDistance,
                RetroVfxKind.None,
                hitFeedback,
                killAttribution);
        }
    }
}
