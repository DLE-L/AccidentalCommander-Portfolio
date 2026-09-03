using System;

namespace Lizzo.PV.Presentation
{
    public sealed class WorldFeedbackRuntimeSink :
        ICombatImpactFeedbackSink,
        IStatusFeedbackSink,
        ICompanionAttackFeedbackSink,
        IEnemyAttackFeedbackSink,
        ISpawnDeathFeedbackSink,
        IExperienceFeedbackSink,
        IRunOutcomeFeedbackSink
    {
        public event Action<CombatImpactPresentation, CombatImpactFeedbackProfileSO> CombatImpactPresented;
        public event Action<StatusFeedbackPresentation, StatusFeedbackProfileSO> StatusPresented;
        public event Action<CompanionAttackPresentation, AttackFeedbackProfileSO> CompanionAttackPresented;
        public event Action<EnemyAttackPresentation, EnemyAttackFeedbackProfileSO> EnemyAttackPresented;
        public event Action<EnemySpawnPresentation, EnemySpawnFeedbackProfileSO> EnemySpawnPresented;
        public event Action<EnemyDeathPresentation, EnemyDeathFeedbackProfileSO> EnemyDeathPresented;
        public event Action<ExperienceFeedbackPresentation, ExperienceOrbFeedbackProfileSO> ExperiencePresented;
        public event Action<RunOutcomeFeedbackPresentation, RunOutcomeWorldFeedbackProfileSO> RunOutcomePresented;

        void ICombatImpactFeedbackSink.Present(
            in CombatImpactPresentation presentation,
            CombatImpactFeedbackProfileSO profile) => CombatImpactPresented?.Invoke(presentation, profile);

        void IStatusFeedbackSink.Present(
            in StatusFeedbackPresentation presentation,
            StatusFeedbackProfileSO profile) => StatusPresented?.Invoke(presentation, profile);

        void ICompanionAttackFeedbackSink.Present(
            in CompanionAttackPresentation presentation,
            AttackFeedbackProfileSO profile) => CompanionAttackPresented?.Invoke(presentation, profile);

        void IEnemyAttackFeedbackSink.Present(
            in EnemyAttackPresentation presentation,
            EnemyAttackFeedbackProfileSO profile) => EnemyAttackPresented?.Invoke(presentation, profile);

        void ISpawnDeathFeedbackSink.PresentSpawn(
            in EnemySpawnPresentation presentation,
            EnemySpawnFeedbackProfileSO profile) => EnemySpawnPresented?.Invoke(presentation, profile);

        void ISpawnDeathFeedbackSink.PresentDeath(
            in EnemyDeathPresentation presentation,
            EnemyDeathFeedbackProfileSO profile) => EnemyDeathPresented?.Invoke(presentation, profile);

        void IExperienceFeedbackSink.Present(
            in ExperienceFeedbackPresentation presentation,
            ExperienceOrbFeedbackProfileSO profile) => ExperiencePresented?.Invoke(presentation, profile);

        void IRunOutcomeFeedbackSink.Present(
            in RunOutcomeFeedbackPresentation presentation,
            RunOutcomeWorldFeedbackProfileSO profile) => RunOutcomePresented?.Invoke(presentation, profile);

        internal void Clear()
        {
            CombatImpactPresented = null;
            StatusPresented = null;
            CompanionAttackPresented = null;
            EnemyAttackPresented = null;
            EnemySpawnPresented = null;
            EnemyDeathPresented = null;
            ExperiencePresented = null;
            RunOutcomePresented = null;
        }
    }
}
