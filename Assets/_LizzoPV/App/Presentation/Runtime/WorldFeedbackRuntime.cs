using Lizzo.PV.Gameplay.Units;
using System;
using Lizzo.PV.Combat;
using Lizzo.PV.Data;
using Lizzo.PV.Flow;
using UnityEngine;
using Lizzo.PV.Legion.Combat;
using Lizzo.PV.Gameplay.Combat;

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
        private EnemyDeathResolver _enemyDeaths;
        private Lizzo.PV.Gameplay.CardOffer.CardOfferRuntime _cardOffers;
        private RuntimeObjectRegistry _registry;
        private RuntimeObjectSpawner _spawner;
        private Lizzo.PV.Gameplay.Commander.CommanderGemCollector _collector;

        internal void BindSpawner(RuntimeObjectSpawner spawner)
        {
            if (_spawner != null) { _spawner.EnemySpawned -= OnEnemySpawned; _spawner.GemSpawned -= OnGemSpawned; }
            _spawner = spawner;
            if (_spawner != null) { _spawner.EnemySpawned += OnEnemySpawned; _spawner.GemSpawned += OnGemSpawned; }
        }
        public void BindCommanderExperience(Lizzo.PV.Gameplay.Commander.CommanderGemCollector collector)
        {
            if (_disposed && collector != null) return;
            if (_collector != null) _collector.ExperienceCollected -= OnExperienceCollected;
            _collector = collector;
            if (_collector != null) _collector.ExperienceCollected += OnExperienceCollected;
        }
        private void OnEnemySpawned(EnemyActor enemy)
        {
            try { TryPresentEnemySpawn(enemy); }
            catch (Exception exception) { Debug.LogException(exception); }
        }
        private void OnGemSpawned(GemController gem)
        {
            try { TryPresentExperience(OrbVisualTier.Small, ExperienceFeedbackEventKind.Spawn, gem.transform.position, gem.GetInstanceID(), 1); }
            catch (Exception exception) { Debug.LogException(exception); }
        }
        private void OnExperienceCollected(Vector3 position, int instanceId, int amount)
        {
            try { TryPresentExperience(OrbVisualTier.Small, ExperienceFeedbackEventKind.AbsorbComplete, position, instanceId, amount); }
            catch (Exception exception) { Debug.LogException(exception); }
        }
        private readonly System.Collections.Generic.HashSet<EnemyActor> _observedEnemies = new System.Collections.Generic.HashSet<EnemyActor>();

        internal void ObserveEnemy(EnemyActor enemy)
        {
            if (_disposed || enemy == null || !_observedEnemies.Add(enemy)) return;
            enemy.StatusApplied += OnStatusApplied;
            enemy.StatusConsumed += OnStatusConsumed;
        }

        private void OnStatusApplied(EnemyActor enemy, CompanionEnemyStatusKind kind)
        {
            try { TryPresentStatusApplied(kind, enemy.transform.position, enemy.GetInstanceID()); }
            catch (Exception exception) { Debug.LogException(exception); }
        }

        private void OnStatusConsumed(EnemyActor enemy, CompanionEnemyStatusKind kind)
        {
            try { TryPresentStatusReaction(kind, StatusReactionKind.Consumed, enemy.transform.position, enemy.GetInstanceID()); }
            catch (Exception exception) { Debug.LogException(exception); }
        }

        internal void BindCardSelections(Lizzo.PV.Gameplay.CardOffer.CardOfferRuntime cards, RuntimeObjectRegistry registry)
        {
            if (_cardOffers != null) _cardOffers.Selected -= OnCardSelected;
            _cardOffers = cards;
            _registry = registry;
            if (_cardOffers != null) _cardOffers.Selected += OnCardSelected;
        }

        private void OnCardSelected(Lizzo.PV.Gameplay.CardOffer.CardKind kind)
        {
            try
            {
                if (_registry?.Player != null)
                    Lizzo.PV.Gameplay.Visuals.RetroVfx.Spawn(Lizzo.PV.Gameplay.Visuals.RetroVfxKind.CardSelect, _registry.Player.transform.position);
            }
            catch (Exception exception) { Debug.LogException(exception); }
        }

        internal void BindEnemyDeaths(EnemyDeathResolver deaths)
        {
            if (_enemyDeaths != null) _enemyDeaths.Resolved -= OnEnemyDeathResolved;
            _enemyDeaths = deaths;
            if (_enemyDeaths != null) _enemyDeaths.Resolved += OnEnemyDeathResolved;
        }

        private void OnEnemyDeathResolved(EnemyActor enemy, int reward, Lizzo.PV.Legion.CompanionEnemyDeathStatusSnapshot status)
        {
            try
            {
                if (status.WasVulnerable)
                    TryPresentStatusReaction(CompanionEnemyStatusKind.Vulnerable, StatusReactionKind.TargetDeath, enemy.transform.position, enemy.GetInstanceID());
                if (status.WasCursed)
                    TryPresentStatusReaction(CompanionEnemyStatusKind.Curse, StatusReactionKind.TargetDeath, enemy.transform.position, enemy.GetInstanceID());
                TryPresentEnemyDeath(enemy, enemy.IsBoss, enemy.IsElite);
            }
            catch (Exception exception) { Debug.LogException(exception); }
            try
            {
                if (enemy.EnemyId == CombatIds.RedCharger)
                    Lizzo.PV.Gameplay.Visuals.EnemyDeathFeedback.ShowRedChargerDefeatFeedback(enemy.transform.position, reward);
                else if (enemy.EnemyId == CombatIds.SmallGoblin || enemy.EnemyId == CombatIds.HungryWolf)
                    Lizzo.PV.Gameplay.Visuals.EnemyDeathFeedback.RecordNormalDeathFeedback(enemy.EnemyId, reward);
                if (enemy.IsShieldOrcEnemy)
                {
                    Lizzo.PV.Gameplay.Visuals.FloatingDamageText.ShowLabel(enemy.transform.position + Vector3.up * .55f,
                        "EXP!", new Color(.28f, 1f, .35f, 1f), large: true, lifeTime: .85f);
                    Lizzo.PV.Gameplay.Telemetry.RunDiagnostics.LogShieldOrcFeedbackCheck("death", enemy);
                }
            }
            catch (Exception exception) { Debug.LogException(exception); }
        }

        private readonly Func<float> _cursePullRadius;

        public WorldFeedbackRuntime(
            WorldFeedbackProfileSetSO profiles,
            CombatImmediateHitModule immediateHits,
            RunState runState, Func<float> cursePullRadius = null)
        {
            if (profiles == null) throw new ArgumentNullException(nameof(profiles));
            if (!profiles.TryValidate(out string issue))
                throw new ArgumentException($"World feedback profiles are invalid: {issue}", nameof(profiles));

            _immediateHits = immediateHits ?? throw new ArgumentNullException(nameof(immediateHits));
            _runState = runState ?? throw new ArgumentNullException(nameof(runState));
            _cursePullRadius = cursePullRadius;
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
                reactionKind,
                statusKind == CompanionEnemyStatusKind.Curse && reactionKind == StatusReactionKind.TargetDeath
                    ? (_cursePullRadius?.Invoke() ?? 1f) : 1f);
            return _status.TryPresent(in presentation);
        }

        public bool TryPresentEnemySpawn(EnemyActor enemy)
        {
            if (_disposed || enemy == null)
                return false;

            var presentation = new EnemySpawnPresentation(
                new EnemyId(enemy.EnemyId),
                enemy.transform.position,
                enemy.GetInstanceID());
            return _spawnDeath.TryPresentSpawn(in presentation);
        }

        public bool TryPresentEnemyDeath(EnemyActor enemy, bool isBoss, bool isElite)
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
            BindEnemyDeaths(null);
            BindCardSelections(null, null);
            BindSpawner(null);
            BindCommanderExperience(null);
            foreach (var enemy in _observedEnemies)
            {
                if (enemy == null) continue;
                enemy.StatusApplied -= OnStatusApplied;
                enemy.StatusConsumed -= OnStatusConsumed;
            }
            _observedEnemies.Clear();
            _runState.RunEnded -= OnRunEnded;
            if (_companionCasts != null)
                _companionCasts.Completed -= OnCompanionCastCompleted;
            _companionCasts = null;
            Sink.Clear();
            _disposed = true;
        }

        private void OnImmediateHitApplied(CombatImmediateHitRequest request)
        {
            try { PresentImmediateHit(request); }
            catch (Exception exception) { Debug.LogException(exception); }
        }

        private void PresentImmediateHit(CombatImmediateHitRequest request)
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
            Lizzo.PV.Gameplay.Visuals.RetroVfx.Spawn(request.EnemyFeedback, position, request.Direction);
            var enemyPresentation = new EnemyAttackPresentation(
                new EnemyAttackId(enemyAttackKey),
                EnemyAttackFeedbackEventKind.Impact,
                position,
                request.Direction,
                request.Source == null ? 0 : request.Source.GetInstanceID(),
                targetInstanceId);
            _enemyAttack.TryPresent(in enemyPresentation);
        }

        private void OnCompanionCastCompleted(CanonicalCompanionCastCompleted completed)
        {
            try { PresentCompanionCast(completed); }
            catch (Exception exception) { Debug.LogException(exception); }
        }

        private void PresentCompanionCast(CanonicalCompanionCastCompleted completed)
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
            try { PresentRunEnded(result); }
            catch (Exception exception) { Debug.LogException(exception); }
        }

        private void PresentRunEnded(RunResult result)
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
