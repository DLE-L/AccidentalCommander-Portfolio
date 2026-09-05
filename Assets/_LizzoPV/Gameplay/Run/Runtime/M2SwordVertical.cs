using System;
using System.Collections.Generic;

namespace Lizzo.PV.Gameplay.Run.M2
{
    public readonly struct RunPoint : IEquatable<RunPoint>
    {
        public static RunPoint Zero => default;

        public float X { get; }
        public float Y { get; }

        public RunPoint(float x, float y)
        {
            if (float.IsNaN(x) || float.IsInfinity(x))
                throw new ArgumentOutOfRangeException(nameof(x));
            if (float.IsNaN(y) || float.IsInfinity(y))
                throw new ArgumentOutOfRangeException(nameof(y));

            X = x;
            Y = y;
        }

        public bool Equals(RunPoint other)
        {
            return X.Equals(other.X) && Y.Equals(other.Y);
        }

        public override bool Equals(object obj)
        {
            return obj is RunPoint other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X.GetHashCode() * 397) ^ Y.GetHashCode();
            }
        }

        public static bool operator ==(RunPoint left, RunPoint right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(RunPoint left, RunPoint right)
        {
            return !left.Equals(right);
        }

        internal static float DistanceSquared(RunPoint left, RunPoint right)
        {
            float x = left.X - right.X;
            float y = left.Y - right.Y;
            return x * x + y * y;
        }

    }

    public sealed class SwordVerticalDefinition
    {
        public static SwordVerticalDefinition Disabled { get; } = new SwordVerticalDefinition();

        internal bool IsEnabled { get; }
        public int CommanderHealth { get; }
        public RunPoint CommanderPosition { get; }
        public RunPoint InitialSwordSlot { get; }
        public float CommanderTargetRange { get; }
        public float MeleeMoveSpeed { get; }
        public float SlashRadius { get; }
        public int SlashDamage { get; }
        public float AttackPeriod { get; }
        public float ContactPeriod { get; }
        public int ExperiencePerLevel { get; }
        public float ExperienceFlightSeconds { get; }
        public int PromotedTriggerCount { get; }
        public int CrescentDamage { get; }

        private SwordVerticalDefinition()
        {
            IsEnabled = false;
        }

        public SwordVerticalDefinition(
            int commanderHealth,
            RunPoint commanderPosition,
            RunPoint initialSwordSlot,
            float commanderTargetRange,
            float meleeMoveSpeed,
            float slashRadius,
            int slashDamage,
            float attackPeriod,
            float contactPeriod,
            int experiencePerLevel,
            float experienceFlightSeconds,
            int promotedTriggerCount,
            int crescentDamage)
        {
            if (commanderHealth <= 0)
                throw new ArgumentOutOfRangeException(nameof(commanderHealth));
            if (commanderTargetRange <= 0.0f)
                throw new ArgumentOutOfRangeException(nameof(commanderTargetRange));
            if (meleeMoveSpeed <= 0.0f)
                throw new ArgumentOutOfRangeException(nameof(meleeMoveSpeed));
            if (slashRadius <= 0.0f)
                throw new ArgumentOutOfRangeException(nameof(slashRadius));
            if (slashDamage <= 0)
                throw new ArgumentOutOfRangeException(nameof(slashDamage));
            if (attackPeriod < 0.0f)
                throw new ArgumentOutOfRangeException(nameof(attackPeriod));
            if (contactPeriod <= 0.0f)
                throw new ArgumentOutOfRangeException(nameof(contactPeriod));
            if (experiencePerLevel <= 0)
                throw new ArgumentOutOfRangeException(nameof(experiencePerLevel));
            if (experienceFlightSeconds < 0.0f)
                throw new ArgumentOutOfRangeException(nameof(experienceFlightSeconds));
            if (promotedTriggerCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(promotedTriggerCount));
            if (crescentDamage <= 0)
                throw new ArgumentOutOfRangeException(nameof(crescentDamage));

            IsEnabled = true;
            CommanderHealth = commanderHealth;
            CommanderPosition = commanderPosition;
            InitialSwordSlot = initialSwordSlot;
            CommanderTargetRange = commanderTargetRange;
            MeleeMoveSpeed = meleeMoveSpeed;
            SlashRadius = slashRadius;
            SlashDamage = slashDamage;
            AttackPeriod = attackPeriod;
            ContactPeriod = contactPeriod;
            ExperiencePerLevel = experiencePerLevel;
            ExperienceFlightSeconds = experienceFlightSeconds;
            PromotedTriggerCount = promotedTriggerCount;
            CrescentDamage = crescentDamage;
        }
    }

    public enum SwordActionPhase
    {
        Idle,
        Approaching,
        Returning,
    }

    public readonly struct SwordVerticalSnapshot
    {
        private readonly int[] _enemyIds;
        private readonly int[] _enemyHealth;

        public bool IsEnabled { get; }
        public int CommanderHealth { get; }
        public RunPoint CommanderPosition { get; }
        public int Progression { get; }
        public bool HasBaseMember => Progression > 0;
        public bool HasPromotedMember => Progression >= 3;
        public SwordActionPhase Phase { get; }
        public RunPoint MemberPosition { get; }
        public RunPoint CurrentSlot { get; }
        public float MovementSpeed { get; }
        public int LockedTargetId { get; }
        public RunPoint LockedTargetPoint { get; }
        public RunPoint LastSlashPoint { get; }
        public int CompletedBaseActions { get; }
        public int PromotionActionProgress { get; }
        public int PendingCrescentCount { get; }
        public int CrescentCastCount { get; }
        public int PendingExperienceFlights { get; }
        public int AbsorbedExperience { get; }
        public bool HasGrowthChoice { get; }
        internal ulong StateDigest { get; }

        internal SwordVerticalSnapshot(
            bool isEnabled,
            int commanderHealth,
            RunPoint commanderPosition,
            int progression,
            SwordActionPhase phase,
            RunPoint memberPosition,
            RunPoint currentSlot,
            float movementSpeed,
            int lockedTargetId,
            RunPoint lockedTargetPoint,
            RunPoint lastSlashPoint,
            int completedBaseActions,
            int promotionActionProgress,
            int pendingCrescentCount,
            int crescentCastCount,
            int pendingExperienceFlights,
            int absorbedExperience,
            bool hasGrowthChoice,
            int[] enemyIds,
            int[] enemyHealth,
            ulong stateDigest)
        {
            IsEnabled = isEnabled;
            CommanderHealth = commanderHealth;
            CommanderPosition = commanderPosition;
            Progression = progression;
            Phase = phase;
            MemberPosition = memberPosition;
            CurrentSlot = currentSlot;
            MovementSpeed = movementSpeed;
            LockedTargetId = lockedTargetId;
            LockedTargetPoint = lockedTargetPoint;
            LastSlashPoint = lastSlashPoint;
            CompletedBaseActions = completedBaseActions;
            PromotionActionProgress = promotionActionProgress;
            PendingCrescentCount = pendingCrescentCount;
            CrescentCastCount = crescentCastCount;
            PendingExperienceFlights = pendingExperienceFlights;
            AbsorbedExperience = absorbedExperience;
            HasGrowthChoice = hasGrowthChoice;
            _enemyIds = enemyIds;
            _enemyHealth = enemyHealth;
            StateDigest = stateDigest;
        }

        public int GetEnemyHealth(int enemyId)
        {
            if (_enemyIds == null)
                return 0;

            for (int index = 0; index < _enemyIds.Length; index++)
            {
                if (_enemyIds[index] == enemyId)
                    return _enemyHealth[index];
            }

            return 0;
        }
    }

    internal sealed class SwordVerticalRuntime
    {
        private readonly SwordVerticalDefinition _definition;
        private readonly List<VerticalEnemyState> _enemies = new List<VerticalEnemyState>(8);
        private readonly List<ExperienceFlightState> _experienceFlights = new List<ExperienceFlightState>(4);

        private RunPoint _commanderPosition;
        private RunPoint _slot;
        private RunPoint _memberPosition;
        private RunPoint _lockedTargetPoint;
        private RunPoint _lastSlashPoint;
        private int _commanderHealth;
        private int _progression;
        private int _lockedTargetId;
        private int _completedBaseActions;
        private int _promotionActionProgress;
        private int _pendingCrescentCount;
        private int _crescentCastCount;
        private int _absorbedExperience;
        private int _newlyAbsorbedExperience;
        private int _availableGrowthChoices;
        private int _nextExperienceThreshold;
        private int[] _snapshotEnemyIds = Array.Empty<int>();
        private int[] _snapshotEnemyHealth = Array.Empty<int>();
        private int _enemySnapshotVersion;
        private int _capturedEnemySnapshotVersion = -1;
        private float _attackCooldown;
        private float _elapsedSeconds;
        private bool _growthSelectionRequested;
        private bool _skipIdleOnce;
        private readonly bool _usesExternalGrowth;

        internal SwordActionPhase Phase { get; private set; }
        internal bool CommanderDefeated => _definition.IsEnabled && _commanderHealth <= 0;
        internal bool HasGrowthChoice => _availableGrowthChoices > 0;

        internal SwordVerticalRuntime(SwordVerticalDefinition definition, bool usesExternalGrowth = false)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
            _usesExternalGrowth = usesExternalGrowth;
            _commanderHealth = definition.CommanderHealth;
            _commanderPosition = definition.CommanderPosition;
            _slot = definition.InitialSwordSlot;
            _memberPosition = definition.InitialSwordSlot;
            _nextExperienceThreshold = definition.ExperiencePerLevel;
        }

        internal bool TryChooseGrowthCard(bool initialRecruitAllowed, bool growthChoiceAllowed)
        {
            if (_definition.IsEnabled == false || _progression >= 3)
                return false;

            if (_progression == 0)
            {
                if (initialRecruitAllowed == false)
                    return false;
            }
            else
            {
                if (growthChoiceAllowed == false || _availableGrowthChoices <= 0)
                    return false;
                _availableGrowthChoices--;
            }

            _progression++;
            if (_progression == 3)
            {
                _promotionActionProgress = 0;
                _pendingCrescentCount = 0;
            }
            return true;
        }

        internal bool TryApplyExternalGrowth(string baseUnitId)
        {
            if (_definition.IsEnabled == false ||
                string.Equals(baseUnitId, "sword_soldier", StringComparison.Ordinal) == false ||
                _progression >= 3)
            {
                return false;
            }

            _progression++;
            if (_progression == 3)
            {
                _promotionActionProgress = 0;
                _pendingCrescentCount = 0;
            }
            return true;
        }

        internal int ConsumeNewlyAbsorbedExperience()
        {
            int amount = _newlyAbsorbedExperience;
            _newlyAbsorbedExperience = 0;
            return amount;
        }

        internal void SpawnEnemy(int id, RunPoint position, int health, int contactDamage, int experience)
        {
            if (_definition.IsEnabled == false)
                return;
            if (id <= 0)
                throw new ArgumentOutOfRangeException(nameof(id));
            if (health <= 0)
                throw new ArgumentOutOfRangeException(nameof(health));
            if (contactDamage <= 0)
                throw new ArgumentOutOfRangeException(nameof(contactDamage));
            if (experience < 0)
                throw new ArgumentOutOfRangeException(nameof(experience));
            if (FindEnemy(id) != null)
                throw new InvalidOperationException($"Vertical enemy id is already active: {id}");

            _enemies.Add(new VerticalEnemyState(id, position, health, contactDamage, experience));
            _enemySnapshotVersion++;
        }

        internal void MoveEnemy(int id, RunPoint position)
        {
            VerticalEnemyState enemy = FindEnemy(id);
            if (enemy != null && enemy.Health > 0)
                enemy.Position = position;
        }

        internal void MoveCommander(RunPoint position)
        {
            _commanderPosition = position;
        }

        internal void SetSwordSlot(RunPoint position)
        {
            _slot = position;
            if (_progression == 0)
                _memberPosition = position;
        }

        internal void ApplyEnemyContact(int id)
        {
            VerticalEnemyState enemy = FindEnemy(id);
            if (enemy == null || enemy.Health <= 0 || _commanderHealth <= 0)
                return;
            if (_elapsedSeconds < enemy.NextContactAllowedSeconds)
                return;

            _commanderHealth = Math.Max(0, _commanderHealth - enemy.ContactDamage);
            enemy.NextContactAllowedSeconds = _elapsedSeconds + _definition.ContactPeriod;
        }

        internal void Advance(float deltaSeconds)
        {
            if (_definition.IsEnabled == false)
                return;

            _elapsedSeconds += deltaSeconds;
            AdvanceExperienceFlights(deltaSeconds);
            _attackCooldown = Math.Max(0.0f, _attackCooldown - deltaSeconds);
            if (Phase == SwordActionPhase.Idle)
            {
                if (_skipIdleOnce)
                {
                    _skipIdleOnce = false;
                    return;
                }
                AdvanceIdle();
            }
        }

        internal void ReachActionPoint()
        {
            if (Phase != SwordActionPhase.Approaching)
                return;

            _memberPosition = _lockedTargetPoint;
            ResolveSlash();
            Phase = SwordActionPhase.Returning;
        }

        internal void ReachSlot()
        {
            if (Phase != SwordActionPhase.Returning)
                return;

            _memberPosition = _slot;
            Phase = SwordActionPhase.Idle;
            _lockedTargetId = 0;
            _skipIdleOnce = true;
            if (_pendingCrescentCount > 0 && TrySelectTarget(out VerticalEnemyState target))
            {
                _pendingCrescentCount--;
                _crescentCastCount++;
                ApplyDamage(target, _definition.CrescentDamage);
            }
        }

        internal bool ConsumeGrowthSelectionRequest()
        {
            bool requested = _growthSelectionRequested;
            _growthSelectionRequested = false;
            return requested;
        }

        internal SwordVerticalSnapshot CreateSnapshot()
        {
            RefreshEnemySnapshotArrays();
            ulong digest = 14695981039346656037UL;
            AddDigest(ref digest, _definition.IsEnabled ? 1UL : 0UL);
            AddDigest(ref digest, (ulong)(uint)_commanderHealth);
            AddPointDigest(ref digest, _commanderPosition);
            AddDigest(ref digest, (ulong)(uint)_progression);
            AddDigest(ref digest, (ulong)(uint)Phase);
            AddPointDigest(ref digest, _memberPosition);
            AddPointDigest(ref digest, _slot);
            AddDigest(ref digest, (ulong)(uint)_lockedTargetId);
            AddPointDigest(ref digest, _lockedTargetPoint);
            AddPointDigest(ref digest, _lastSlashPoint);
            AddDigest(ref digest, (ulong)(uint)_completedBaseActions);
            AddDigest(ref digest, (ulong)(uint)_promotionActionProgress);
            AddDigest(ref digest, (ulong)(uint)_pendingCrescentCount);
            AddDigest(ref digest, (ulong)(uint)_crescentCastCount);
            AddDigest(ref digest, (ulong)(uint)_absorbedExperience);
            AddDigest(ref digest, (ulong)(uint)_newlyAbsorbedExperience);
            AddDigest(ref digest, (ulong)(uint)_availableGrowthChoices);
            AddDigest(ref digest, (ulong)(uint)_nextExperienceThreshold);
            AddFloatDigest(ref digest, _attackCooldown);
            AddFloatDigest(ref digest, _elapsedSeconds);
            AddDigest(ref digest, _growthSelectionRequested ? 1UL : 0UL);
            AddDigest(ref digest, _skipIdleOnce ? 1UL : 0UL);
            AddDigest(ref digest, (ulong)(uint)_experienceFlights.Count);
            for (int index = 0; index < _experienceFlights.Count; index++)
            {
                ExperienceFlightState flight = _experienceFlights[index];
                AddDigest(ref digest, (ulong)(uint)flight.Amount);
                AddFloatDigest(ref digest, flight.RemainingSeconds);
            }
            for (int index = 0; index < _enemies.Count; index++)
            {
                VerticalEnemyState enemy = _enemies[index];
                AddDigest(ref digest, (ulong)(uint)enemy.Id);
                AddDigest(ref digest, (ulong)(uint)enemy.Health);
                AddPointDigest(ref digest, enemy.Position);
                AddDigest(ref digest, (ulong)(uint)enemy.ContactDamage);
                AddDigest(ref digest, (ulong)(uint)enemy.Experience);
                AddFloatDigest(ref digest, enemy.NextContactAllowedSeconds);
            }

            return new SwordVerticalSnapshot(
                _definition.IsEnabled,
                _commanderHealth,
                _commanderPosition,
                _progression,
                Phase,
                _memberPosition,
                _slot,
                _definition.MeleeMoveSpeed,
                _lockedTargetId,
                _lockedTargetPoint,
                _lastSlashPoint,
                _completedBaseActions,
                _promotionActionProgress,
                _pendingCrescentCount,
                _crescentCastCount,
                _experienceFlights.Count,
                _absorbedExperience,
                HasGrowthChoice,
                _snapshotEnemyIds,
                _snapshotEnemyHealth,
                digest);
        }

        private void AdvanceIdle()
        {
            if (_progression <= 0)
                return;

            if (_pendingCrescentCount > 0 && TrySelectTarget(out VerticalEnemyState crescentTarget))
            {
                _pendingCrescentCount--;
                _crescentCastCount++;
                ApplyDamage(crescentTarget, _definition.CrescentDamage);
                return;
            }

            if (_attackCooldown > 0.0f || TrySelectTarget(out VerticalEnemyState target) == false)
                return;

            _lockedTargetId = target.Id;
            _lockedTargetPoint = target.Position;
            _attackCooldown = _definition.AttackPeriod;
            Phase = SwordActionPhase.Approaching;
        }

        private void ResolveSlash()
        {
            _lastSlashPoint = _lockedTargetPoint;
            _completedBaseActions++;
            if (_progression >= 3)
            {
                _promotionActionProgress++;
                if (_promotionActionProgress >= _definition.PromotedTriggerCount)
                {
                    _promotionActionProgress -= _definition.PromotedTriggerCount;
                    _pendingCrescentCount++;
                }
            }

            float radiusSquared = _definition.SlashRadius * _definition.SlashRadius;
            for (int index = 0; index < _enemies.Count; index++)
            {
                VerticalEnemyState enemy = _enemies[index];
                if (enemy.Health <= 0 || RunPoint.DistanceSquared(enemy.Position, _lockedTargetPoint) > radiusSquared)
                    continue;
                ApplyDamage(enemy, _definition.SlashDamage);
            }
        }

        private void ApplyDamage(VerticalEnemyState enemy, int damage)
        {
            if (enemy == null || enemy.Health <= 0)
                return;

            int previousHealth = enemy.Health;
            enemy.Health = Math.Max(0, enemy.Health - damage);
            if (enemy.Health != previousHealth)
                _enemySnapshotVersion++;
            if (enemy.Health == 0 && enemy.Experience > 0)
            {
                _experienceFlights.Add(new ExperienceFlightState(
                    enemy.Experience,
                    _definition.ExperienceFlightSeconds));
            }
        }

        private void AdvanceExperienceFlights(float deltaSeconds)
        {
            for (int index = _experienceFlights.Count - 1; index >= 0; index--)
            {
                ExperienceFlightState flight = _experienceFlights[index];
                flight.RemainingSeconds -= deltaSeconds;
                if (flight.RemainingSeconds > 0.0f)
                    continue;

                _absorbedExperience += flight.Amount;
                if (_usesExternalGrowth)
                    _newlyAbsorbedExperience += flight.Amount;
                _experienceFlights.RemoveAt(index);
            }

            if (_usesExternalGrowth)
                return;

            while (_absorbedExperience >= _nextExperienceThreshold)
            {
                _availableGrowthChoices++;
                _nextExperienceThreshold += _definition.ExperiencePerLevel;
                _growthSelectionRequested = true;
            }
        }

        private bool TrySelectTarget(out VerticalEnemyState selected)
        {
            selected = null;
            float bestDistance = _definition.CommanderTargetRange * _definition.CommanderTargetRange;
            for (int index = 0; index < _enemies.Count; index++)
            {
                VerticalEnemyState candidate = _enemies[index];
                if (candidate.Health <= 0)
                    continue;

                float distance = RunPoint.DistanceSquared(candidate.Position, _commanderPosition);
                if (distance > bestDistance)
                    continue;
                if (selected != null && distance == bestDistance && candidate.Id >= selected.Id)
                    continue;

                selected = candidate;
                bestDistance = distance;
            }
            return selected != null;
        }

        private VerticalEnemyState FindEnemy(int id)
        {
            for (int index = 0; index < _enemies.Count; index++)
            {
                if (_enemies[index].Id == id)
                    return _enemies[index];
            }
            return null;
        }

        private void RefreshEnemySnapshotArrays()
        {
            if (_capturedEnemySnapshotVersion == _enemySnapshotVersion)
                return;

            int count = _enemies.Count;
            if (count == 0)
            {
                _snapshotEnemyIds = Array.Empty<int>();
                _snapshotEnemyHealth = Array.Empty<int>();
            }
            else
            {
                int[] ids = new int[count];
                int[] health = new int[count];
                for (int index = 0; index < count; index++)
                {
                    VerticalEnemyState enemy = _enemies[index];
                    ids[index] = enemy.Id;
                    health[index] = enemy.Health;
                }
                _snapshotEnemyIds = ids;
                _snapshotEnemyHealth = health;
            }

            _capturedEnemySnapshotVersion = _enemySnapshotVersion;
        }

        private static void AddDigest(ref ulong value, ulong part)
        {
            const ulong prime = 1099511628211UL;
            for (int shift = 0; shift < 64; shift += 8)
            {
                value ^= (byte)(part >> shift);
                value *= prime;
            }
        }

        private static void AddPointDigest(ref ulong value, RunPoint point)
        {
            AddFloatDigest(ref value, point.X);
            AddFloatDigest(ref value, point.Y);
        }

        private static void AddFloatDigest(ref ulong value, float part)
        {
            AddDigest(ref value, unchecked((ulong)(uint)BitConverter.SingleToInt32Bits(part)));
        }

        private sealed class VerticalEnemyState
        {
            internal int Id { get; }
            internal RunPoint Position { get; set; }
            internal int Health { get; set; }
            internal int ContactDamage { get; }
            internal int Experience { get; }
            internal float NextContactAllowedSeconds { get; set; }

            internal VerticalEnemyState(int id, RunPoint position, int health, int contactDamage, int experience)
            {
                Id = id;
                Position = position;
                Health = health;
                ContactDamage = contactDamage;
                Experience = experience;
            }
        }

        private sealed class ExperienceFlightState
        {
            internal int Amount { get; }
            internal float RemainingSeconds { get; set; }

            internal ExperienceFlightState(int amount, float remainingSeconds)
            {
                Amount = amount;
                RemainingSeconds = remainingSeconds;
            }
        }
    }
}
