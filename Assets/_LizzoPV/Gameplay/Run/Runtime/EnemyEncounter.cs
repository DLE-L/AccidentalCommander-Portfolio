using System;
using System.Collections.Generic;

namespace Lizzo.PV.Gameplay.Run
{
    public enum EnemyRank
    {
        Normal,
        Elite,
        Boss,
    }

    public sealed class EnemyArchetypeDefinition
    {
        public string ArchetypeId { get; }
        public EnemyRank Rank { get; }
        public int ContactDamage { get; }
        public int ChargeContactDamage { get; }
        public string PhysicsProfileId { get; }
        public CombatDamageKind ContactDamageKind => CombatDamageKind.Contact;
        public CombatDamageKind ChargeDamageKind => CombatDamageKind.ChargeContact;

        public EnemyArchetypeDefinition(
            string archetypeId,
            EnemyRank rank,
            int contactDamage,
            int chargeContactDamage,
            string physicsProfileId)
        {
            if (string.IsNullOrWhiteSpace(archetypeId))
                throw new ArgumentException("Enemy archetype id is required.", nameof(archetypeId));
            if (contactDamage <= 0 || chargeContactDamage <= 0)
                throw new ArgumentOutOfRangeException(nameof(contactDamage));
            if (string.IsNullOrWhiteSpace(physicsProfileId))
                throw new ArgumentException("Authored physics profile id is required.", nameof(physicsProfileId));
            ArchetypeId = archetypeId;
            Rank = rank;
            ContactDamage = contactDamage;
            ChargeContactDamage = chargeContactDamage;
            PhysicsProfileId = physicsProfileId;
        }
    }

    public sealed class EnemyRewardTable
    {
        public int NormalExperience { get; }
        public int EliteExperience { get; }
        public int BossExperience { get; }

        public EnemyRewardTable(int normalExperience, int eliteExperience, int bossExperience)
        {
            if (normalExperience <= 0 || eliteExperience <= 0 || bossExperience <= 0)
                throw new ArgumentOutOfRangeException(nameof(normalExperience));
            NormalExperience = normalExperience;
            EliteExperience = eliteExperience;
            BossExperience = bossExperience;
        }

        public int Get(EnemyRank rank)
        {
            if (rank == EnemyRank.Normal)
                return NormalExperience;
            if (rank == EnemyRank.Elite)
                return EliteExperience;
            return BossExperience;
        }
    }

    public sealed class EnemySpawnEntry
    {
        public float TimeSeconds { get; }
        public string ArchetypeId { get; }
        public int Count { get; }
        public string PatternId { get; }

        public EnemySpawnEntry(float timeSeconds, string archetypeId, int count, string patternId)
        {
            if (float.IsNaN(timeSeconds) || float.IsInfinity(timeSeconds) || timeSeconds < 0.0f)
                throw new ArgumentOutOfRangeException(nameof(timeSeconds));
            if (string.IsNullOrWhiteSpace(archetypeId))
                throw new ArgumentException("Enemy archetype id is required.", nameof(archetypeId));
            if (count <= 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (string.IsNullOrWhiteSpace(patternId))
                throw new ArgumentException("Spawn pattern id is required.", nameof(patternId));
            TimeSeconds = timeSeconds;
            ArchetypeId = archetypeId;
            Count = count;
            PatternId = patternId;
        }
    }

    public sealed class CombatEncounterDefinition
    {
        private readonly EnemyArchetypeDefinition[] _archetypes;
        private readonly EnemySpawnEntry[] _schedule;

        public int CommanderEntityId { get; }
        public float DurationSeconds { get; }
        public int ExperienceMultiplierPermille { get; }
        public EnemyRewardTable Rewards { get; }
        public int ScheduleCount => _schedule.Length;

        public CombatEncounterDefinition(
            int commanderEntityId,
            float durationSeconds,
            int experienceMultiplierPermille,
            EnemyRewardTable rewards,
            EnemyArchetypeDefinition[] archetypes,
            EnemySpawnEntry[] schedule)
        {
            if (commanderEntityId <= 0)
                throw new ArgumentOutOfRangeException(nameof(commanderEntityId));
            if (float.IsNaN(durationSeconds) || float.IsInfinity(durationSeconds) || durationSeconds <= 0.0f)
                throw new ArgumentOutOfRangeException(nameof(durationSeconds));
            if (experienceMultiplierPermille <= 0)
                throw new ArgumentOutOfRangeException(nameof(experienceMultiplierPermille));
            Rewards = rewards ?? throw new ArgumentNullException(nameof(rewards));
            if (archetypes == null || archetypes.Length == 0)
                throw new ArgumentOutOfRangeException(nameof(archetypes));
            if (schedule == null || schedule.Length == 0)
                throw new ArgumentOutOfRangeException(nameof(schedule));

            CommanderEntityId = commanderEntityId;
            DurationSeconds = durationSeconds;
            ExperienceMultiplierPermille = experienceMultiplierPermille;
            _archetypes = (EnemyArchetypeDefinition[])archetypes.Clone();
            _schedule = (EnemySpawnEntry[])schedule.Clone();
            ValidateAndSort();
        }

        public EnemyArchetypeDefinition GetArchetype(string archetypeId)
        {
            for (int index = 0; index < _archetypes.Length; index++)
            {
                if (string.Equals(_archetypes[index].ArchetypeId, archetypeId, StringComparison.Ordinal))
                    return _archetypes[index];
            }
            throw new ArgumentOutOfRangeException(nameof(archetypeId));
        }

        internal EnemySpawnEntry GetScheduleEntry(int index) => _schedule[index];

        private void ValidateAndSort()
        {
            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < _archetypes.Length; index++)
            {
                if (_archetypes[index] == null || ids.Add(_archetypes[index].ArchetypeId) == false)
                    throw new ArgumentException("Enemy archetypes must be non-null and unique.");
            }
            Array.Sort(_schedule, (left, right) => left.TimeSeconds.CompareTo(right.TimeSeconds));
            for (int index = 0; index < _schedule.Length; index++)
            {
                if (_schedule[index] == null || _schedule[index].TimeSeconds > DurationSeconds ||
                    ids.Contains(_schedule[index].ArchetypeId) == false)
                {
                    throw new ArgumentException("Encounter schedule references invalid content.");
                }
            }
        }
    }

    public readonly struct EnemySpawnCommand
    {
        public string ArchetypeId { get; }
        public EnemyRank Rank { get; }
        public int Count { get; }
        public string PatternId { get; }
        public float ScheduledTime { get; }

        internal EnemySpawnCommand(EnemySpawnEntry entry, EnemyArchetypeDefinition archetype)
        {
            ArchetypeId = entry.ArchetypeId;
            Rank = archetype.Rank;
            Count = entry.Count;
            PatternId = entry.PatternId;
            ScheduledTime = entry.TimeSeconds;
        }
    }

    public readonly struct ExperienceOrbCommand
    {
        public int SourceEntityId { get; }
        public int TargetEntityId { get; }
        public int Amount { get; }
        public bool IsImmediateHoming { get; }
        public bool HasExperience => Amount > 0;

        internal ExperienceOrbCommand(int sourceEntityId, int targetEntityId, int amount)
        {
            SourceEntityId = sourceEntityId;
            TargetEntityId = targetEntityId;
            Amount = amount;
            IsImmediateHoming = amount > 0;
        }
    }

    public sealed class EnemyEncounterRuntime
    {
        private readonly CombatEncounterDefinition _definition;
        private readonly Queue<EnemySpawnCommand> _pending = new Queue<EnemySpawnCommand>();
        private float _elapsedSeconds;
        private int _nextScheduleIndex;

        public EnemyEncounterRuntime(CombatEncounterDefinition definition)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
        }

        public void Advance(float deltaSeconds)
        {
            if (float.IsNaN(deltaSeconds) || float.IsInfinity(deltaSeconds) || deltaSeconds < 0.0f)
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
            _elapsedSeconds = Math.Min(_definition.DurationSeconds, _elapsedSeconds + deltaSeconds);
            while (_nextScheduleIndex < _definition.ScheduleCount)
            {
                EnemySpawnEntry entry = _definition.GetScheduleEntry(_nextScheduleIndex);
                if (entry.TimeSeconds > _elapsedSeconds)
                    break;
                _pending.Enqueue(new EnemySpawnCommand(entry, _definition.GetArchetype(entry.ArchetypeId)));
                _nextScheduleIndex++;
            }
        }

        public bool TryDequeueSpawn(out EnemySpawnCommand command)
        {
            if (_pending.Count == 0)
            {
                command = default;
                return false;
            }
            command = _pending.Dequeue();
            return true;
        }

        public ExperienceOrbCommand ResolveCombatKill(int enemyEntityId, EnemyRank rank)
        {
            if (enemyEntityId <= 0)
                throw new ArgumentOutOfRangeException(nameof(enemyEntityId));
            int amount = _definition.Rewards.Get(rank) * _definition.ExperienceMultiplierPermille / 1000;
            return new ExperienceOrbCommand(enemyEntityId, _definition.CommanderEntityId, amount);
        }

        public ExperienceOrbCommand ResolveRemoval(int enemyEntityId)
        {
            if (enemyEntityId <= 0)
                throw new ArgumentOutOfRangeException(nameof(enemyEntityId));
            return new ExperienceOrbCommand(enemyEntityId, _definition.CommanderEntityId, 0);
        }
    }
}
