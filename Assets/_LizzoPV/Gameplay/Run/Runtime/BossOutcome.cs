using System;

namespace Lizzo.PV.Gameplay.Run
{
    public enum RunOutcome
    {
        InProgress,
        Victory,
        Defeat,
    }

    public readonly struct BossOutcomeSnapshot
    {
        public RunOutcome Outcome { get; }
        public int ActiveBossEntityId { get; }
        public bool ShouldStopNewSpawns { get; }
        public bool ShouldRemoveRemainingEnemies { get; }
        public bool RemovedEnemiesGrantExperience { get; }

        internal BossOutcomeSnapshot(RunOutcome outcome, int activeBossEntityId)
        {
            Outcome = outcome;
            ActiveBossEntityId = activeBossEntityId;
            ShouldStopNewSpawns = outcome != RunOutcome.InProgress;
            ShouldRemoveRemainingEnemies = outcome != RunOutcome.InProgress;
            RemovedEnemiesGrantExperience = false;
        }
    }

    public sealed class BossOutcomeRuntime
    {
        private int _activeBossEntityId;
        private RunOutcome _outcome;

        public bool RegisterBoss(int entityId)
        {
            if (entityId <= 0)
                throw new ArgumentOutOfRangeException(nameof(entityId));
            if (_outcome != RunOutcome.InProgress || _activeBossEntityId != 0)
                return false;
            _activeBossEntityId = entityId;
            return true;
        }

        public bool ResolveCombatDeath(int entityId, CombatEntityKind kind)
        {
            if (_outcome != RunOutcome.InProgress)
                return false;
            if (kind == CombatEntityKind.Commander)
            {
                _outcome = RunOutcome.Defeat;
                return true;
            }
            if (kind == CombatEntityKind.BossEnemy && entityId == _activeBossEntityId)
            {
                _activeBossEntityId = 0;
                _outcome = RunOutcome.Victory;
                return true;
            }
            return false;
        }

        public bool ResolveSystemRemoval(int entityId)
        {
            return false;
        }

        public BossOutcomeSnapshot CreateSnapshot()
        {
            return new BossOutcomeSnapshot(_outcome, _activeBossEntityId);
        }
    }
}
