using Lizzo.PV.Combat;
using Lizzo.PV.Gameplay.Visuals;
using UnityEngine;

namespace Lizzo.PV.Combat
{
    public enum CombatImmediateHitMode
    {
        AllyDirectTarget,
        EnemyContact,
    }

    public enum CombatImmediateHitFaction
    {
        Ally,
        Enemy,
    }

    public readonly struct CombatImmediateHitRequest
    {
        public CombatImmediateHitMode Mode { get; }
        public CombatImmediateHitFaction Faction { get; }
        public string SourceId { get; }
        public ICombatImmediateHitTarget Target { get; }
        public Component Source { get; }
        public Vector3 Origin { get; }
        public Vector3 Direction { get; }
        public Vector3 FeedbackPosition { get; }
        public int Damage { get; }
        public float ExecutionThreshold { get; }
        public AttackVisualKind AllyFeedback { get; }
        public bool SpawnAllyFeedback { get; }
        public string EnemyPatternId { get; }
        public RetroVfxKind EnemyFeedback { get; }
        public CountableKillAttribution KillAttribution { get; }
        public string EffectId { get; }
        public bool IsFuseSecondary { get; }
        public CombatStatusPayload StatusPayload { get; }

        public bool IsValid => !string.IsNullOrEmpty(SourceId) && Target != null && Damage > 0;

        private CombatImmediateHitRequest(
            CombatImmediateHitMode mode,
            CombatImmediateHitFaction faction,
            string sourceId,
            ICombatImmediateHitTarget target,
            Component source,
            Vector3 origin,
            Vector3 direction,
            Vector3 feedbackPosition,
            int damage,
            AttackVisualKind allyFeedback,
            bool spawnAllyFeedback,
            string enemyPatternId,
            RetroVfxKind enemyFeedback,
            CountableKillAttribution killAttribution,
            string effectId = null,
            bool isFuseSecondary = false,
            CombatStatusPayload statusPayload = default, float executionThreshold = 0f)
        {
            Mode = mode;
            Faction = faction;
            SourceId = sourceId;
            Target = target;
            Source = source;
            Origin = origin;
            Direction = direction;
            FeedbackPosition = feedbackPosition;
            Damage = damage;
            ExecutionThreshold = Mathf.Clamp01(executionThreshold);
            AllyFeedback = allyFeedback;
            SpawnAllyFeedback = spawnAllyFeedback;
            EnemyPatternId = enemyPatternId;
            EnemyFeedback = enemyFeedback;
            KillAttribution = killAttribution;
            EffectId = effectId;
            IsFuseSecondary = isFuseSecondary;
            StatusPayload = statusPayload;
        }

        public CombatImmediateHitRequest WithDamage(int damage) => new CombatImmediateHitRequest(Mode, Faction, SourceId, Target, Source, Origin, Direction, FeedbackPosition, damage, AllyFeedback, SpawnAllyFeedback, EnemyPatternId, EnemyFeedback, KillAttribution, EffectId, IsFuseSecondary, StatusPayload, ExecutionThreshold);

        public static CombatImmediateHitRequest CreateAllyDirectTarget(
            string sourceId,
            ICombatImmediateHitTarget target,
            Vector3 origin,
            Vector3 feedbackPosition,
            int damage,
            AttackVisualKind feedback,
            bool spawnFeedback,
            CountableKillAttribution killAttribution = default,
            string effectId = null,
            bool isFuseSecondary = false,
            CombatStatusPayload statusPayload = default, float executionThreshold = 0f)
        {
            return new CombatImmediateHitRequest(
                CombatImmediateHitMode.AllyDirectTarget,
                CombatImmediateHitFaction.Ally,
                sourceId,
                target,
                null,
                origin,
                Vector3.zero,
                feedbackPosition,
                damage,
                feedback,
                spawnFeedback,
                null,
                RetroVfxKind.None,
                killAttribution,
                effectId,
                isFuseSecondary,
                statusPayload, executionThreshold);
        }

        public static CombatImmediateHitRequest CreateEnemyContact(
            string sourceId,
            ICombatImmediateHitTarget target,
            Vector3 origin,
            Vector3 direction,
            int damage,
            string patternId,
            RetroVfxKind feedback,
            CountableKillAttribution killAttribution = default,
            Component source = null)
        {
            return new CombatImmediateHitRequest(
                CombatImmediateHitMode.EnemyContact,
                CombatImmediateHitFaction.Enemy,
                sourceId,
                target,
                source,
                origin,
                direction,
                Vector3.zero,
                damage,
                AttackVisualKind.SingleHit,
                false,
                patternId,
                feedback,
                killAttribution);
        }

    }
}
