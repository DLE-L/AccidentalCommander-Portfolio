using System;
using Lizzo.PV.Combat;
using Lizzo.PV.Data;
using Lizzo.PV.Flow;
using UnityEngine;
using Lizzo.PV.Legion.Combat;

namespace Lizzo.PV.Presentation
{
    public sealed class WorldFeedbackRuntime : IDisposable
    {
        private readonly CombatImmediateHitModule _immediateHits;
        private readonly RunState _runState;
        private readonly CombatImpactPresenter _combatImpact;
        private readonly StatusFeedbackPresenter _status;
        private readonly CompanionAttackFeedbackPresenter _companionAttack;
        private readonly EnemyAttackFeedbackPresenter _enemyAttack;
        private readonly SpawnDeathFeedbackPresenter _spawnDeath;
        private readonly ExperienceFeedbackPresenter _experience;
        private readonly RunOutcomeFeedbackPresenter _runOutcome;
        private bool _disposed;
        private CanonicalCompanionCastStream _companionCasts;

        public WorldFeedbackRuntime(
            WorldFeedbackProfileSetSO profiles,
            CombatImmediateHitModule immediateHits,
            RunState runState)
        {
            if (profiles == null) throw new ArgumentNullException(nameof(profiles));
            if (!profiles.TryValidate(out string issue))
                throw new ArgumentException($"World feedback profiles are invalid: {issue}", nameof(profiles));

            _immediateHits = immediateHits ?? throw new ArgumentNullException(nameof(immediateHits));
            _runState = runState ?? throw new ArgumentNullException(nameof(runState));
            Sink = new WorldFeedbackRuntimeSink();
            _combatImpact = new CombatImpactPresenter(profiles.CombatImpactBindings, Sink);
            _status = new StatusFeedbackPresenter(profiles.StatusBindings, Sink);
            _companionAttack = new CompanionAttackFeedbackPresenter(
                profiles.AttackBindings,
                Sink,
                _combatImpact);
            _enemyAttack = new EnemyAttackFeedbackPresenter(
                profiles.EnemyAttackBindings,
                Sink,
                _combatImpact);
            _spawnDeath = new SpawnDeathFeedbackPresenter(
                profiles.EnemySpawnBindings,
                profiles.EnemyDeathBindings,
                Sink);
            _experience = new ExperienceFeedbackPresenter(profiles.ExperienceOrbBindings, Sink);
            _runOutcome = new RunOutcomeFeedbackPresenter(profiles.RunOutcomeProfile, Sink);
            _immediateHits.Applied += OnImmediateHitApplied;
            _runState.RunEnded += OnRunEnded;
        }

        public WorldFeedbackRuntimeSink Sink { get; }

        public void BindCanonicalCompanionCasts(CanonicalCompanionCastStream companionCasts)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(WorldFeedbackRuntime));
            if (ReferenceEquals(_companionCasts, companionCasts))
                return;

            if (_companionCasts != null)
                _companionCasts.Completed -= OnCompanionCastCompleted;
            _companionCasts = companionCasts;
            if (_companionCasts != null)
                _companionCasts.Completed += OnCompanionCastCompleted;
        }

        public bool TryPresentStatusApplied(
            CompanionEnemyStatusKind statusKind,
            Vector3 position,
            int targetInstanceId)
        {
            if (_disposed || !TryMapStatusId(statusKind, out StatusId statusId))
                return false;

            var presentation = new StatusFeedbackPresentation(
                statusId,
                StatusFeedbackEventKind.Applied,
                position,
                targetInstanceId);
            return _status.TryPresent(in presentation);
        }

        public bool TryPresentStatusReaction(
            CompanionEnemyStatusKind statusKind,
            StatusReactionKind reactionKind,
            Vector3 position,
            int targetInstanceId)
        {
            if (_disposed || !TryMapStatusId(statusKind, out StatusId statusId))
                return false;

            var presentation = new StatusFeedbackPresentation(
                statusId,
                StatusFeedbackEventKind.Reaction,
                position,
                targetInstanceId,
                reactionKind);
            return _status.TryPresent(in presentation);
        }

        public bool TryPresentEnemySpawn(MonsterController enemy)
        {
            if (_disposed || enemy == null)
                return false;

            var presentation = new EnemySpawnPresentation(
                new EnemyId(enemy.EnemyId),
                enemy.transform.position,
                enemy.GetInstanceID());
            return _spawnDeath.TryPresentSpawn(in presentation);
        }

        public bool TryPresentEnemyDeath(MonsterController enemy, bool isBoss, bool isElite)
        {
            if (_disposed || enemy == null)
                return false;

            var presentation = new EnemyDeathPresentation(
                new EnemyId(enemy.EnemyId),
                enemy.transform.position,
                enemy.GetInstanceID(),
                isBoss,
                isElite);
            return _spawnDeath.TryPresentDeath(in presentation);
        }

        public bool TryPresentExperience(
            OrbVisualTier visualTier,
            ExperienceFeedbackEventKind eventKind,
            Vector3 position,
            int orbInstanceId,
            int rewardValue)
        {
            if (_disposed)
                return false;

            var presentation = new ExperienceFeedbackPresentation(
                visualTier,
                eventKind,
                position,
                orbInstanceId,
                rewardValue);
            return _experience.TryPresent(in presentation);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _immediateHits.Applied -= OnImmediateHitApplied;
            _runState.RunEnded -= OnRunEnded;
            if (_companionCasts != null)
                _companionCasts.Completed -= OnCompanionCastCompleted;
            _companionCasts = null;
            Sink.Clear();
            _disposed = true;
        }

        private void OnImmediateHitApplied(CombatImmediateHitRequest request)
        {
            if (_disposed)
                return;

            int targetInstanceId = ResolveInstanceId(request.Target);
            if (request.Mode == CombatImmediateHitMode.AllyDirectTarget)
            {
                string attackKey = string.IsNullOrEmpty(request.EffectId)
                    ? request.SourceId
                    : request.EffectId;
                var presentation = new CompanionAttackPresentation(
                    new AttackId(attackKey),
                    CompanionAttackFeedbackEventKind.Impact,
                    request.FeedbackPosition,
                    request.Direction,
                    request.KillAttribution.OwnerInstanceId,
                    targetInstanceId);
                _companionAttack.TryPresent(in presentation);
                return;
            }

            string enemyAttackKey = string.IsNullOrEmpty(request.EnemyPatternId)
                ? request.SourceId
                : request.EnemyPatternId;
            Vector3 position = ResolvePosition(request.Target, request.Origin);
            var enemyPresentation = new EnemyAttackPresentation(
                new EnemyAttackId(enemyAttackKey),
                EnemyAttackFeedbackEventKind.Impact,
                position,
                request.Direction,
                request.EnemySource == null ? 0 : request.EnemySource.GetInstanceID(),
                targetInstanceId);
            _enemyAttack.TryPresent(in enemyPresentation);
        }

        private void OnCompanionCastCompleted(CanonicalCompanionCastCompleted completed)
        {
            if (_disposed || string.IsNullOrEmpty(completed.AttackId))
                return;

            var presentation = new CompanionAttackPresentation(
                new AttackId(completed.AttackId),
                CompanionAttackFeedbackEventKind.Cast,
                completed.Position,
                completed.Direction,
                completed.OwnerInstanceId);
            _companionAttack.TryPresent(in presentation);
        }

        private void OnRunEnded(RunResult result)
        {
            RunOutcomeFeedbackKind outcomeKind = result.Outcome switch
            {
                RunOutcome.Clear => RunOutcomeFeedbackKind.Victory,
                RunOutcome.Failure => RunOutcomeFeedbackKind.Failure,
                RunOutcome.Abandoned => RunOutcomeFeedbackKind.Abandoned,
                _ => throw new ArgumentOutOfRangeException(nameof(result), result.Outcome, null),
            };
            var presentation = new RunOutcomeFeedbackPresentation(
                outcomeKind,
                result.BossHpPercent,
                result.ElapsedSeconds,
                result.KillCount);
            _runOutcome.TryPresent(in presentation);
        }

        private static bool TryMapStatusId(CompanionEnemyStatusKind kind, out StatusId statusId)
        {
            string value = kind switch
            {
                CompanionEnemyStatusKind.Vulnerable => "vulnerable",
                CompanionEnemyStatusKind.Shock => "shock",
                CompanionEnemyStatusKind.Weakening => "weakening",
                CompanionEnemyStatusKind.Curse => "curse",
                _ => null,
            };
            statusId = new StatusId(value);
            return !statusId.IsNone;
        }

        private static int ResolveInstanceId(ICombatImmediateHitTarget target) =>
            target is UnityEngine.Object unityObject && unityObject != null
                ? unityObject.GetInstanceID()
                : 0;

        private static Vector3 ResolvePosition(ICombatImmediateHitTarget target, Vector3 fallback) =>
            target is Component component && component != null
                ? component.transform.position
                : fallback;
    }
}
