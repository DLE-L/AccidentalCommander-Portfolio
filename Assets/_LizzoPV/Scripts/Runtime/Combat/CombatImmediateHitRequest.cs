using Lizzo.PV.Legion;
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
        public MonsterController EnemySource { get; }
        public Vector3 Origin { get; }
        public Vector3 Direction { get; }
        public Vector3 FeedbackPosition { get; }
        public int Damage { get; }
        public AttackVisualKind AllyFeedback { get; }
        public bool SpawnAllyFeedback { get; }
        public string EnemyPatternId { get; }
        public RetroVfxKind EnemyFeedback { get; }
        public CountableKillAttribution KillAttribution { get; }
        public string EffectId { get; }
        public bool IsFuseSecondary { get; }

        public bool IsValid => !string.IsNullOrEmpty(SourceId) && Target != null && Damage > 0;

        private CombatImmediateHitRequest(
            CombatImmediateHitMode mode,
            CombatImmediateHitFaction faction,
            string sourceId,
            ICombatImmediateHitTarget target,
            MonsterController enemySource,
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
            bool isFuseSecondary = false)
        {
            Mode = mode;
            Faction = faction;
            SourceId = sourceId;
            Target = target;
            EnemySource = enemySource;
            Origin = origin;
            Direction = direction;
            FeedbackPosition = feedbackPosition;
            Damage = damage;
            AllyFeedback = allyFeedback;
            SpawnAllyFeedback = spawnAllyFeedback;
            EnemyPatternId = enemyPatternId;
            EnemyFeedback = enemyFeedback;
            KillAttribution = killAttribution;
            EffectId = effectId;
            IsFuseSecondary = isFuseSecondary;
        }

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
            bool isFuseSecondary = false)
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
                RetroVfxKind.SingleHit,
                killAttribution,
                effectId,
                isFuseSecondary);
        }

        public static CombatImmediateHitRequest CreateEnemyContact(
            string sourceId,
            ICombatImmediateHitTarget target,
            Vector3 origin,
            Vector3 direction,
            int damage,
            string patternId,
            RetroVfxKind feedback,
            CountableKillAttribution killAttribution = default)
        {
            return new CombatImmediateHitRequest(
                CombatImmediateHitMode.EnemyContact,
                CombatImmediateHitFaction.Enemy,
                sourceId,
                target,
                null,
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

        internal static CombatImmediateHitRequest CreateEnemyContact(
            MonsterController source,
            PlayerController target,
            Vector3 origin,
            Vector3 direction,
            int damage,
            string patternId,
            RetroVfxKind feedback)
        {
            CombatImmediateHitRequest request = CreateEnemyContact(
                source == null ? null : source.GetDamageEnemyId(),
                target,
                origin,
                direction,
                damage,
                patternId,
                feedback);
            return new CombatImmediateHitRequest(
                request.Mode,
                request.Faction,
                request.SourceId,
                request.Target,
                source,
                request.Origin,
                request.Direction,
                request.FeedbackPosition,
                request.Damage,
                request.AllyFeedback,
                request.SpawnAllyFeedback,
                request.EnemyPatternId,
                request.EnemyFeedback,
                request.KillAttribution,
                request.EffectId,
                request.IsFuseSecondary);
        }
    }
}
