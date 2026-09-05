using System;

namespace Lizzo.PV.Gameplay.Run
{
    public enum CombatRunOutcome
    {
        InProgress,
        Victory,
        Defeat,
    }

    public readonly struct BossOutcomeSnapshot
    {
        public CombatRunOutcome Outcome { get; }
        public int ActiveBossEntityId { get; }
        public bool ShouldStopNewSpawns { get; }
        public bool ShouldRemoveRemainingEnemies { get; }
        public bool RemovedEnemiesGrantExperience { get; }

        internal BossOutcomeSnapshot(CombatRunOutcome outcome, int activeBossEntityId)
        {
            Outcome = outcome;
            ActiveBossEntityId = activeBossEntityId;
            ShouldStopNewSpawns = outcome != CombatRunOutcome.InProgress;
            ShouldRemoveRemainingEnemies = outcome != CombatRunOutcome.InProgress;
            RemovedEnemiesGrantExperience = false;
        }
    }

    public sealed class BossOutcomeRuntime
    {
        private int _activeBossEntityId;
        private CombatRunOutcome _outcome;

        public bool RegisterBoss(int entityId)
        {
            if (entityId <= 0)
                throw new ArgumentOutOfRangeException(nameof(entityId));
            if (_outcome != CombatRunOutcome.InProgress || _activeBossEntityId != 0)
                return false;
            _activeBossEntityId = entityId;
            return true;
        }

        public bool ResolveCombatDeath(int entityId, CombatEntityKind kind)
        {
            if (_outcome != CombatRunOutcome.InProgress)
                return false;
            if (kind == CombatEntityKind.Commander)
            {
                _outcome = CombatRunOutcome.Defeat;
                return true;
            }
            if (kind == CombatEntityKind.BossEnemy && entityId == _activeBossEntityId)
            {
                _activeBossEntityId = 0;
                _outcome = CombatRunOutcome.Victory;
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
