using System;

namespace Lizzo.PV.Gameplay.Run
{
    public sealed class RunCombatDefinition
    {
        public RunDefinitionSnapshot CoreDefinition { get; }
        public PairSynergyDefinitionSet PairSynergies { get; }
        public TrioSynergyDefinitionSet TrioSynergies { get; }
        public CombatEncounterDefinition Encounter { get; }
        public int EventCapacity { get; }

        public RunCombatDefinition(
            RunDefinitionSnapshot foundation,
            PairSynergyDefinitionSet pairSynergies,
            TrioSynergyDefinitionSet trioSynergies,
            CombatEncounterDefinition encounter,
            int maxConcurrentPairExecutions,
            int maxConcurrentTrioExecutions,
            int eventCapacity)
        {
            if (foundation == null)
                throw new ArgumentNullException(nameof(foundation));
            PairSynergies = pairSynergies ?? throw new ArgumentNullException(nameof(pairSynergies));
            TrioSynergies = trioSynergies ?? throw new ArgumentNullException(nameof(trioSynergies));
            Encounter = encounter ?? throw new ArgumentNullException(nameof(encounter));
            if (pairSynergies.Count != 12)
                throw new ArgumentException("Exactly twelve pair synergies are required.", nameof(pairSynergies));
            if (trioSynergies.Count != 8)
                throw new ArgumentException("Exactly eight trio synergies are required.", nameof(trioSynergies));
            if (foundation.Synergies.Synergies.Count != 0)
                throw new ArgumentException("Foundation must not already own a synergy catalog.", nameof(foundation));
            if (eventCapacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(eventCapacity));

            SynergyDefinition[] synergies = new SynergyDefinition[pairSynergies.Count + trioSynergies.Count];
            for (int index = 0; index < pairSynergies.Count; index++)
                synergies[index] = pairSynergies.GetAt(index).RuntimeDefinition;
            for (int index = 0; index < trioSynergies.Count; index++)
                synergies[pairSynergies.Count + index] = trioSynergies.GetAt(index).RuntimeDefinition;

            CoreDefinition = new RunDefinitionSnapshot(
                foundation.Seed,
                foundation.SwordVertical,
                foundation.FormationGrowth,
                foundation.FrontlineLegions,
                foundation.RangedLegions,
                foundation.CasterLegions,
                foundation.SummonedLegions,
                foundation.CommonPassives,
                new SynergyRuntimeDefinition(
                    maxConcurrentPairExecutions,
                    maxConcurrentTrioExecutions,
                    synergies));
            EventCapacity = eventCapacity;
        }
    }

    public static class RunCombatRuntimeFactory
    {
        public static bool TryCreate(
            bool isTutorial,
            RunCombatDefinition definition,
            out RunCombatRuntime runtime)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));
            if (isTutorial)
            {
                runtime = null;
                return false;
            }

            runtime = new RunCombatRuntime(definition);
            return true;
        }
    }

    public sealed class RunCombatRuntime : IDisposable
    {
        private readonly RunRuntimeHost _core;
        private readonly PairSynergyRuntime _pairSynergies;
        private readonly TrioSynergyRuntime _trioSynergies;
        private readonly EnemyEncounterRuntime _encounter;
        private readonly BossOutcomeRuntime _outcome = new BossOutcomeRuntime();
        private readonly RunEventBuffer _events;
        private bool _started;
        private bool _finishedRecorded;
        private bool _disposed;

        public RunRuntimeSnapshot CurrentSnapshot => _core.CurrentSnapshot;
        public BossOutcomeSnapshot Outcome => _outcome.CreateSnapshot();
        public RunEventBuffer Events => _events;

        public RunCombatRuntime(RunCombatDefinition definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));
            _core = RunCompositionRoot.Build(definition.CoreDefinition);
            _pairSynergies = new PairSynergyRuntime(
                definition.PairSynergies,
                _core.SynergyScheduler);
            _trioSynergies = new TrioSynergyRuntime(
                definition.TrioSynergies,
                _core.SynergyScheduler);
            _encounter = new EnemyEncounterRuntime(definition.Encounter);
            _events = new RunEventBuffer(definition.EventCapacity);
        }

        public bool Start()
        {
            EnsureNotDisposed();
            if (_started || _core.Start() == false)
                return false;
            _started = true;
            _events.Record(0.0f, RunEventKind.RunStarted);
            return true;
        }

        public void Submit(RunCommand command)
        {
            EnsureStarted();
            _core.Submit(command);
        }

        public void Advance(float deltaSeconds)
        {
            EnsureStarted();
            float before = _core.CurrentSnapshot.ElapsedSeconds;
            _core.Advance(deltaSeconds);
            float advanced = _core.CurrentSnapshot.ElapsedSeconds - before;
            if (advanced > 0.0f && Outcome.Outcome == CombatRunOutcome.InProgress)
                _encounter.Advance(advanced);
            RecordFinishedIfNeeded();
        }

        public bool TryDequeueSpawn(out EnemySpawnCommand command)
        {
            EnsureStarted();
            if (_encounter.TryDequeueSpawn(out command) == false)
                return false;
            _events.Record(
                _core.CurrentSnapshot.ElapsedSeconds,
                RunEventKind.EnemySpawned,
                command.ArchetypeId,
                command.Count);
            return true;
        }

        public bool RegisterBoss(int entityId)
        {
            EnsureStarted();
            if (_outcome.RegisterBoss(entityId) == false)
                return false;
            _events.Record(
                _core.CurrentSnapshot.ElapsedSeconds,
                RunEventKind.BossSpawned,
                entityId.ToString());
            return true;
        }

        public bool ResolveCombatDeath(int entityId, CombatEntityKind kind)
        {
            EnsureStarted();
            if (_outcome.ResolveCombatDeath(entityId, kind) == false)
                return false;
            if (kind == CombatEntityKind.Commander)
                _core.Submit(RunCommand.CommanderDefeated());
            else if (kind == CombatEntityKind.BossEnemy)
                _core.Submit(RunCommand.BossDefeated());
            _core.Advance(0.0f);
            RecordFinishedIfNeeded();
            return true;
        }

        public ExperienceOrbCommand ResolveEnemyCombatKill(int entityId, EnemyRank rank)
        {
            EnsureStarted();
            return _encounter.ResolveCombatKill(entityId, rank);
        }

        public ExperienceOrbCommand ResolveEnemyRemoval(int entityId)
        {
            EnsureStarted();
            return _encounter.ResolveRemoval(entityId);
        }

        public bool AbsorbExperience(ExperienceOrbCommand orb)
        {
            EnsureStarted();
            if (orb.HasExperience == false)
                return false;
            _core.Submit(RunCommand.ExperienceAbsorbed(orb.Amount));
            _core.Advance(0.0f);
            _events.Record(
                _core.CurrentSnapshot.ElapsedSeconds,
                RunEventKind.ExperienceAbsorbed,
                orb.SourceEntityId.ToString(),
                orb.Amount);
            return true;
        }

        public bool TryReactPair(
            PairSynergyTrigger trigger,
            out PairSynergyReactionSnapshot reaction)
        {
            EnsureStarted();
            if (_pairSynergies.TryReact(trigger, out reaction) == false)
                return false;
            float time = _core.CurrentSnapshot.ElapsedSeconds;
            _events.Record(time, RunEventKind.SynergyQueued, reaction.SynergyId);
            _events.Record(time, RunEventKind.SynergyStarted, reaction.SynergyId);
            return true;
        }

        public bool CompletePair(PairSynergyReactionSnapshot reaction)
        {
            EnsureStarted();
            if (_pairSynergies.Complete(reaction.ExecutionId) == false)
                return false;
            _events.Record(
                _core.CurrentSnapshot.ElapsedSeconds,
                RunEventKind.SynergyCompleted,
                reaction.SynergyId);
            return true;
        }

        public bool TryQueueTrio(TrioSynergyTrigger trigger)
        {
            EnsureStarted();
            if (_trioSynergies.TryQueue(trigger) == false)
                return false;
            _events.Record(
                _core.CurrentSnapshot.ElapsedSeconds,
                RunEventKind.SynergyQueued,
                trigger.Kind.ToString());
            return true;
        }

        public bool TryStartNextTrio(out TrioSynergyExecutionSnapshot execution)
        {
            EnsureStarted();
            if (_trioSynergies.TryStartNext(out execution) == false)
                return false;
            _events.Record(
                _core.CurrentSnapshot.ElapsedSeconds,
                RunEventKind.SynergyStarted,
                execution.SynergyId);
            return true;
        }

        public bool CompleteTrio(TrioSynergyExecutionSnapshot execution)
        {
            EnsureStarted();
            if (_trioSynergies.Complete(execution.ExecutionId) == false)
                return false;
            _events.Record(
                _core.CurrentSnapshot.ElapsedSeconds,
                RunEventKind.SynergyCompleted,
                execution.SynergyId);
            return true;
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            _core.Dispose();
        }

        private void RecordFinishedIfNeeded()
        {
            if (_finishedRecorded || Outcome.Outcome == CombatRunOutcome.InProgress)
                return;
            _finishedRecorded = true;
            _events.Record(
                _core.CurrentSnapshot.ElapsedSeconds,
                RunEventKind.RunFinished,
                Outcome.Outcome.ToString());
        }

        private void EnsureStarted()
        {
            EnsureNotDisposed();
            if (_started == false)
                throw new InvalidOperationException("Run combat runtime must be started first.");
        }

        private void EnsureNotDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(RunCombatRuntime));
        }
    }
}
