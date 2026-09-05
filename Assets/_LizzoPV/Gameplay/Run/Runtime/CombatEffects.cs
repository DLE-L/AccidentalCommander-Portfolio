using System;
using System.Collections.Generic;

namespace Lizzo.PV.Gameplay.Run
{
    public enum CombatEntityKind
    {
        Commander,
        NormalEnemy,
        EliteEnemy,
        BossEnemy,
    }

    public enum CombatDamageKind
    {
        Contact,
        ChargeContact,
        Direct,
        Projectile,
        Area,
        Periodic,
    }

    public enum CombatStatusKind
    {
        Vulnerable,
        Weakened,
        Curse,
        Shock,
    }

    public enum ForcedMovementKind
    {
        Push,
        Pull,
    }

    public enum ForcedMovementOutcome
    {
        Invalid,
        Applied,
        Locked,
        Immune,
    }

    public enum ForcedMovementEndReason
    {
        None,
        Arrived,
        Stopped,
        Collision,
        Timeout,
        Death,
    }

    public sealed class CombatEntityDefinition
    {
        public int EntityId { get; }
        public CombatEntityKind Kind { get; }
        public int MaximumHealth { get; }
        public RunPoint InitialPosition { get; }
        public float ReceivedDamageMultiplier { get; }
        public float CommanderDamageReduction { get; }
        public int DirectGuardCharges { get; }

        public CombatEntityDefinition(
            int entityId,
            CombatEntityKind kind,
            int maximumHealth,
            RunPoint initialPosition,
            float receivedDamageMultiplier = 1.0f,
            float commanderDamageReduction = 0.0f,
            int directGuardCharges = 0)
        {
            if (entityId <= 0)
                throw new ArgumentOutOfRangeException(nameof(entityId));
            if (maximumHealth <= 0)
                throw new ArgumentOutOfRangeException(nameof(maximumHealth));
            if (float.IsNaN(receivedDamageMultiplier) ||
                float.IsInfinity(receivedDamageMultiplier) ||
                receivedDamageMultiplier <= 0.0f)
            {
                throw new ArgumentOutOfRangeException(nameof(receivedDamageMultiplier));
            }
            if (float.IsNaN(commanderDamageReduction) ||
                float.IsInfinity(commanderDamageReduction) ||
                commanderDamageReduction < 0.0f ||
                commanderDamageReduction >= 1.0f)
            {
                throw new ArgumentOutOfRangeException(nameof(commanderDamageReduction));
            }
            if (directGuardCharges < 0)
                throw new ArgumentOutOfRangeException(nameof(directGuardCharges));
            if (kind != CombatEntityKind.Commander &&
                (commanderDamageReduction > 0.0f || directGuardCharges > 0))
            {
                throw new ArgumentException("Commander defenses require a commander entity.");
            }

            EntityId = entityId;
            Kind = kind;
            MaximumHealth = maximumHealth;
            InitialPosition = initialPosition;
            ReceivedDamageMultiplier = receivedDamageMultiplier;
            CommanderDamageReduction = commanderDamageReduction;
            DirectGuardCharges = directGuardCharges;
        }
    }

    public readonly struct DamageRequest
    {
        public int SourceEntityId { get; }
        public int TargetEntityId { get; }
        public float BaseDamage { get; }
        public CombatDamageKind Kind { get; }
        public float AttackerDamageMultiplier { get; }
        public float ReceivedDamageMultiplier { get; }
        public string AttackId { get; }

        public DamageRequest(
            int sourceEntityId,
            int targetEntityId,
            float baseDamage,
            CombatDamageKind kind,
            float attackerDamageMultiplier = 1.0f,
            float receivedDamageMultiplier = 1.0f,
            string attackId = null)
        {
            if (sourceEntityId <= 0)
                throw new ArgumentOutOfRangeException(nameof(sourceEntityId));
            if (targetEntityId <= 0)
                throw new ArgumentOutOfRangeException(nameof(targetEntityId));
            ValidatePositiveFinite(baseDamage, nameof(baseDamage));
            ValidatePositiveFinite(attackerDamageMultiplier, nameof(attackerDamageMultiplier));
            ValidatePositiveFinite(receivedDamageMultiplier, nameof(receivedDamageMultiplier));

            SourceEntityId = sourceEntityId;
            TargetEntityId = targetEntityId;
            BaseDamage = baseDamage;
            Kind = kind;
            AttackerDamageMultiplier = attackerDamageMultiplier;
            ReceivedDamageMultiplier = receivedDamageMultiplier;
            AttackId = attackId ?? string.Empty;
        }

        internal bool CanConsumeDirectGuard => Kind != CombatDamageKind.Periodic;

        private static void ValidatePositiveFinite(float value, string parameterName)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0.0f)
                throw new ArgumentOutOfRangeException(parameterName);
        }
    }

    public readonly struct HealingRequest
    {
        public int SourceEntityId { get; }
        public int TargetEntityId { get; }
        public float BaseHealing { get; }
        public float ReceivedHealingMultiplier { get; }

        public HealingRequest(
            int sourceEntityId,
            int targetEntityId,
            float baseHealing,
            float receivedHealingMultiplier)
        {
            if (sourceEntityId <= 0)
                throw new ArgumentOutOfRangeException(nameof(sourceEntityId));
            if (targetEntityId <= 0)
                throw new ArgumentOutOfRangeException(nameof(targetEntityId));
            if (float.IsNaN(baseHealing) || float.IsInfinity(baseHealing) || baseHealing <= 0.0f)
                throw new ArgumentOutOfRangeException(nameof(baseHealing));
            if (float.IsNaN(receivedHealingMultiplier) ||
                float.IsInfinity(receivedHealingMultiplier) ||
                receivedHealingMultiplier <= 0.0f)
            {
                throw new ArgumentOutOfRangeException(nameof(receivedHealingMultiplier));
            }

            SourceEntityId = sourceEntityId;
            TargetEntityId = targetEntityId;
            BaseHealing = baseHealing;
            ReceivedHealingMultiplier = receivedHealingMultiplier;
        }
    }

    public readonly struct StatusRequest
    {
        public int SourceEntityId { get; }
        public int TargetEntityId { get; }
        public CombatStatusKind Kind { get; }
        public float Magnitude { get; }
        public float DurationSeconds { get; }
        public int Charges { get; }

        public StatusRequest(
            int sourceEntityId,
            int targetEntityId,
            CombatStatusKind kind,
            float magnitude,
            float durationSeconds,
            int charges = 0)
        {
            if (sourceEntityId <= 0)
                throw new ArgumentOutOfRangeException(nameof(sourceEntityId));
            if (targetEntityId <= 0)
                throw new ArgumentOutOfRangeException(nameof(targetEntityId));
            if (float.IsNaN(magnitude) || float.IsInfinity(magnitude) || magnitude <= 0.0f)
                throw new ArgumentOutOfRangeException(nameof(magnitude));
            if (float.IsNaN(durationSeconds) || float.IsInfinity(durationSeconds) || durationSeconds <= 0.0f)
                throw new ArgumentOutOfRangeException(nameof(durationSeconds));
            if (charges < 0)
                throw new ArgumentOutOfRangeException(nameof(charges));

            SourceEntityId = sourceEntityId;
            TargetEntityId = targetEntityId;
            Kind = kind;
            Magnitude = magnitude;
            DurationSeconds = durationSeconds;
            Charges = charges;
        }
    }

    public readonly struct ForcedMovementRequest
    {
        public int SourceEntityId { get; }
        public int TargetEntityId { get; }
        public ForcedMovementKind Kind { get; }
        public RunPoint Direction { get; }
        public float Distance { get; }
        public int Priority { get; }
        public float Strength { get; }
        public float MaximumSeconds { get; }

        public ForcedMovementRequest(
            int sourceEntityId,
            int targetEntityId,
            ForcedMovementKind kind,
            RunPoint direction,
            float distance,
            int priority,
            float strength,
            float maximumSeconds)
        {
            if (sourceEntityId <= 0)
                throw new ArgumentOutOfRangeException(nameof(sourceEntityId));
            if (targetEntityId <= 0)
                throw new ArgumentOutOfRangeException(nameof(targetEntityId));
            if (direction.X == 0.0f && direction.Y == 0.0f)
                throw new ArgumentOutOfRangeException(nameof(direction));
            ValidatePositiveFinite(distance, nameof(distance));
            ValidatePositiveFinite(strength, nameof(strength));
            ValidatePositiveFinite(maximumSeconds, nameof(maximumSeconds));

            SourceEntityId = sourceEntityId;
            TargetEntityId = targetEntityId;
            Kind = kind;
            Direction = direction;
            Distance = distance;
            Priority = priority;
            Strength = strength;
            MaximumSeconds = maximumSeconds;
        }

        private static void ValidatePositiveFinite(float value, string parameterName)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0.0f)
                throw new ArgumentOutOfRangeException(parameterName);
        }
    }

    public readonly struct CombatResolution
    {
        public int AppliedAmount { get; }
        public bool WasGuarded { get; }
        public bool DidKill { get; }

        internal CombatResolution(int appliedAmount, bool wasGuarded, bool didKill)
        {
            AppliedAmount = appliedAmount;
            WasGuarded = wasGuarded;
            DidKill = didKill;
        }
    }

    public readonly struct ForcedMovementResolution
    {
        public ForcedMovementOutcome Outcome { get; }
        public int SourceEntityId { get; }
        public int TargetEntityId { get; }
        public RunPoint Destination { get; }
        public RunPoint FollowUpPoint { get; }

        internal ForcedMovementResolution(
            ForcedMovementOutcome outcome,
            int sourceEntityId,
            int targetEntityId,
            RunPoint destination,
            RunPoint followUpPoint)
        {
            Outcome = outcome;
            SourceEntityId = sourceEntityId;
            TargetEntityId = targetEntityId;
            Destination = destination;
            FollowUpPoint = followUpPoint;
        }
    }

    public readonly struct CombatDeathSnapshot
    {
        public int EntityId { get; }
        public int KillerEntityId { get; }
        public string AttackId { get; }
        public CombatDamageKind DamageKind { get; }
        public RunPoint Position { get; }

        internal CombatDeathSnapshot(
            int entityId,
            int killerEntityId,
            string attackId,
            CombatDamageKind damageKind,
            RunPoint position)
        {
            EntityId = entityId;
            KillerEntityId = killerEntityId;
            AttackId = attackId ?? string.Empty;
            DamageKind = damageKind;
            Position = position;
        }
    }

    public readonly struct CombatEntitySnapshot
    {
        private readonly StatusSnapshot[] _statuses;

        public int EntityId { get; }
        public CombatEntityKind Kind { get; }
        public int Health { get; }
        public int MaximumHealth { get; }
        public bool IsDead => Health <= 0;
        public RunPoint Position { get; }
        public int DirectGuardCharges { get; }
        public bool IsActionActive { get; }
        public int PendingUnspawnedAttackCount { get; }
        public int ActionCancelCount { get; }
        public bool IsForcedMovementLocked { get; }
        public RunPoint ForcedMovementDestination { get; }
        public float ForcedMovementStrength { get; }
        public ForcedMovementEndReason LastForcedMovementEndReason { get; }
        public int DeathReactionCount { get; }

        internal CombatEntitySnapshot(EntityState state)
        {
            EntityId = state.Definition.EntityId;
            Kind = state.Definition.Kind;
            Health = state.Health;
            MaximumHealth = state.Definition.MaximumHealth;
            Position = state.Position;
            DirectGuardCharges = state.DirectGuardCharges;
            IsActionActive = state.IsActionActive;
            PendingUnspawnedAttackCount = state.PendingUnspawnedAttackCount;
            ActionCancelCount = state.ActionCancelCount;
            IsForcedMovementLocked = state.IsForcedMovementLocked;
            ForcedMovementDestination = state.ForcedMovementDestination;
            ForcedMovementStrength = state.ForcedMovementStrength;
            LastForcedMovementEndReason = state.LastForcedMovementEndReason;
            DeathReactionCount = state.DeathReactionCount;
            _statuses = new StatusSnapshot[state.Statuses.Count];
            for (int index = 0; index < state.Statuses.Count; index++)
                _statuses[index] = new StatusSnapshot(state.Statuses[index]);
        }

        public int GetStatusCount(CombatStatusKind kind)
        {
            int count = 0;
            if (_statuses == null)
                return count;
            for (int index = 0; index < _statuses.Length; index++)
            {
                if (_statuses[index].Kind == kind)
                    count++;
            }
            return count;
        }

        public float GetStatusRemaining(CombatStatusKind kind, int sourceEntityId)
        {
            if (_statuses == null)
                return 0.0f;
            for (int index = 0; index < _statuses.Length; index++)
            {
                StatusSnapshot status = _statuses[index];
                if (status.Kind == kind && status.SourceEntityId == sourceEntityId)
                    return status.RemainingSeconds;
            }
            return 0.0f;
        }
    }

    public readonly struct CombatEffectsSnapshot
    {
        private readonly CombatEntitySnapshot[] _entities;

        public int DeathCount { get; }
        public CombatDeathSnapshot LastDeath { get; }
        public int ForcedMovementStartCount { get; }
        public int ForcedMovementEndCount { get; }
        public int EntityCount => _entities == null ? 0 : _entities.Length;
        internal ulong StateDigest { get; }

        internal CombatEffectsSnapshot(
            CombatEntitySnapshot[] entities,
            int deathCount,
            CombatDeathSnapshot lastDeath,
            int forcedMovementStartCount,
            int forcedMovementEndCount,
            ulong stateDigest)
        {
            _entities = entities;
            DeathCount = deathCount;
            LastDeath = lastDeath;
            ForcedMovementStartCount = forcedMovementStartCount;
            ForcedMovementEndCount = forcedMovementEndCount;
            StateDigest = stateDigest;
        }

        public CombatEntitySnapshot GetEntity(int entityId)
        {
            if (_entities != null)
            {
                for (int index = 0; index < _entities.Length; index++)
                {
                    if (_entities[index].EntityId == entityId)
                        return _entities[index];
                }
            }
            throw new ArgumentOutOfRangeException(nameof(entityId));
        }

        public CombatEntitySnapshot GetEntityAt(int index)
        {
            if (_entities == null || index < 0 || index >= _entities.Length)
                throw new ArgumentOutOfRangeException(nameof(index));
            return _entities[index];
        }
    }

    public sealed class CombatResolver
    {
        private readonly List<EntityState> _entities = new List<EntityState>(32);
        private CombatEntitySnapshot[] _snapshotEntities = Array.Empty<CombatEntitySnapshot>();
        private int _stateVersion;
        private int _snapshotVersion = -1;
        private int _deathCount;
        private CombatDeathSnapshot _lastDeath;
        private int _forcedMovementStartCount;
        private int _forcedMovementEndCount;

        public void Register(CombatEntityDefinition definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));
            if (FindEntity(definition.EntityId) != null)
                throw new InvalidOperationException($"Combat entity id is already registered: {definition.EntityId}");

            _entities.Add(new EntityState(definition));
            _stateVersion++;
        }

        public void SetPosition(int entityId, RunPoint position)
        {
            EntityState entity = FindEntity(entityId);
            if (entity == null || entity.Health <= 0 || entity.Position == position)
                return;
            entity.Position = position;
            _stateVersion++;
        }

        public void BeginAction(int entityId, int pendingUnspawnedAttackCount)
        {
            if (pendingUnspawnedAttackCount < 0)
                throw new ArgumentOutOfRangeException(nameof(pendingUnspawnedAttackCount));
            EntityState entity = FindEntity(entityId);
            if (entity == null || entity.Health <= 0)
                return;
            entity.IsActionActive = true;
            entity.PendingUnspawnedAttackCount = pendingUnspawnedAttackCount;
            _stateVersion++;
        }

        public CombatResolution ApplyDamage(DamageRequest request)
        {
            EntityState source = FindEntity(request.SourceEntityId);
            EntityState target = FindEntity(request.TargetEntityId);
            if ((source != null && source.Health <= 0) || target == null || target.Health <= 0)
                return default;

            float resolved = request.BaseDamage;
            resolved *= request.AttackerDamageMultiplier;
            resolved *= request.ReceivedDamageMultiplier;
            resolved *= GetStrongestMultiplier(target, CombatStatusKind.Vulnerable, 1.0f);
            resolved *= target.Definition.ReceivedDamageMultiplier;

            StatusState weaken = null;
            if (target.Definition.Kind == CombatEntityKind.Commander)
            {
                weaken = source == null ? null : FindStrongestWeaken(source);
                if (weaken != null)
                    resolved *= Math.Max(0.0f, 1.0f - weaken.Magnitude);
                resolved *= 1.0f - target.Definition.CommanderDamageReduction;
            }

            bool guarded = request.CanConsumeDirectGuard && target.DirectGuardCharges > 0;
            if (guarded)
            {
                target.DirectGuardCharges--;
                _stateVersion++;
                return new CombatResolution(0, true, false);
            }

            int rounded = (int)Math.Round(resolved, MidpointRounding.AwayFromZero);
            if (resolved > 0.0f && rounded < 1)
                rounded = 1;
            int applied = Math.Min(target.Health, rounded);
            if (applied <= 0)
                return default;

            target.Health -= applied;
            if (weaken != null)
                ConsumeStatus(source, weaken);
            bool killed = target.Health <= 0;
            if (killed)
                CommitDeath(target, request);
            _stateVersion++;
            return new CombatResolution(applied, false, killed);
        }

        public CombatResolution ApplyHealing(HealingRequest request)
        {
            EntityState source = FindEntity(request.SourceEntityId);
            EntityState target = FindEntity(request.TargetEntityId);
            if ((source != null && source.Health <= 0) || target == null || target.Health <= 0)
                return default;

            float resolved = request.BaseHealing * request.ReceivedHealingMultiplier;
            int rounded = (int)Math.Round(resolved, MidpointRounding.AwayFromZero);
            if (resolved > 0.0f && rounded < 1)
                rounded = 1;
            int applied = Math.Min(target.Definition.MaximumHealth - target.Health, rounded);
            if (applied <= 0)
                return default;

            target.Health += applied;
            _stateVersion++;
            return new CombatResolution(applied, false, false);
        }

        public bool ApplyStatus(StatusRequest request)
        {
            EntityState source = FindEntity(request.SourceEntityId);
            EntityState target = FindEntity(request.TargetEntityId);
            if ((source != null && source.Health <= 0) || target == null || target.Health <= 0)
                return false;

            for (int index = 0; index < target.Statuses.Count; index++)
            {
                StatusState status = target.Statuses[index];
                if (status.Kind != request.Kind || status.SourceEntityId != request.SourceEntityId)
                    continue;
                status.RemainingSeconds = request.DurationSeconds;
                status.Charges = request.Charges;
                _stateVersion++;
                return true;
            }

            target.Statuses.Add(new StatusState(request));
            _stateVersion++;
            return true;
        }

        public int RemoveStatuses(
            int targetEntityId,
            CombatStatusKind kind,
            int sourceEntityId = 0)
        {
            if (targetEntityId <= 0)
                throw new ArgumentOutOfRangeException(nameof(targetEntityId));
            if (sourceEntityId < 0)
                throw new ArgumentOutOfRangeException(nameof(sourceEntityId));

            EntityState target = FindEntity(targetEntityId);
            if (target == null)
                return 0;

            int removed = 0;
            for (int index = target.Statuses.Count - 1; index >= 0; index--)
            {
                StatusState status = target.Statuses[index];
                if (status.Kind != kind ||
                    (sourceEntityId > 0 && status.SourceEntityId != sourceEntityId))
                {
                    continue;
                }
                target.Statuses.RemoveAt(index);
                removed++;
            }
            if (removed > 0)
                _stateVersion++;
            return removed;
        }

        public ForcedMovementResolution ResolveForcedMovement(ForcedMovementRequest[] requests)
        {
            if (requests == null || requests.Length == 0)
                throw new ArgumentOutOfRangeException(nameof(requests));

            int targetId = requests[0].TargetEntityId;
            int selectedIndex = 0;
            for (int index = 1; index < requests.Length; index++)
            {
                if (requests[index].TargetEntityId != targetId)
                    throw new ArgumentException("A forced movement batch must target one entity.", nameof(requests));
                if (IsHigherPriority(requests[index], requests[selectedIndex]))
                    selectedIndex = index;
            }

            ForcedMovementRequest selected = requests[selectedIndex];
            EntityState target = FindEntity(targetId);
            if (target == null || target.Health <= 0)
                return new ForcedMovementResolution(ForcedMovementOutcome.Invalid, selected.SourceEntityId, targetId, default, default);
            if (target.Definition.Kind == CombatEntityKind.BossEnemy)
            {
                return new ForcedMovementResolution(
                    ForcedMovementOutcome.Immune,
                    selected.SourceEntityId,
                    targetId,
                    target.Position,
                    target.Position);
            }
            if (target.IsForcedMovementLocked)
            {
                return new ForcedMovementResolution(
                    ForcedMovementOutcome.Locked,
                    selected.SourceEntityId,
                    targetId,
                    target.ForcedMovementDestination,
                    target.Position);
            }

            float length = (float)Math.Sqrt(
                selected.Direction.X * selected.Direction.X +
                selected.Direction.Y * selected.Direction.Y);
            float directionX = selected.Direction.X / length;
            float directionY = selected.Direction.Y / length;
            RunPoint destination = new RunPoint(
                target.Position.X + directionX * selected.Distance,
                target.Position.Y + directionY * selected.Distance);
            target.IsForcedMovementLocked = true;
            target.ForcedMovementDestination = destination;
            target.ForcedMovementStrength = selected.Strength;
            target.ForcedMovementRemainingSeconds = selected.MaximumSeconds;
            target.LastForcedMovementEndReason = ForcedMovementEndReason.None;
            if (target.Definition.Kind == CombatEntityKind.NormalEnemy ||
                target.Definition.Kind == CombatEntityKind.EliteEnemy)
            {
                CancelAction(target);
            }
            _forcedMovementStartCount++;
            _stateVersion++;
            return new ForcedMovementResolution(
                ForcedMovementOutcome.Applied,
                selected.SourceEntityId,
                targetId,
                destination,
                destination);
        }

        public bool EndForcedMovement(
            int entityId,
            ForcedMovementEndReason reason,
            RunPoint finalPosition)
        {
            if (reason == ForcedMovementEndReason.None || reason == ForcedMovementEndReason.Death)
                throw new ArgumentOutOfRangeException(nameof(reason));
            EntityState target = FindEntity(entityId);
            if (target == null || target.IsForcedMovementLocked == false)
                return false;

            EndForcedMovement(target, reason, finalPosition);
            return true;
        }

        public void Advance(float deltaSeconds)
        {
            if (float.IsNaN(deltaSeconds) || float.IsInfinity(deltaSeconds) || deltaSeconds < 0.0f)
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
            if (deltaSeconds == 0.0f)
                return;

            bool changed = false;
            for (int entityIndex = 0; entityIndex < _entities.Count; entityIndex++)
            {
                EntityState entity = _entities[entityIndex];
                for (int statusIndex = entity.Statuses.Count - 1; statusIndex >= 0; statusIndex--)
                {
                    StatusState status = entity.Statuses[statusIndex];
                    status.RemainingSeconds -= deltaSeconds;
                    changed = true;
                    if (status.RemainingSeconds <= 0.0f)
                        entity.Statuses.RemoveAt(statusIndex);
                }

                if (entity.IsForcedMovementLocked)
                {
                    entity.ForcedMovementRemainingSeconds -= deltaSeconds;
                    changed = true;
                    if (entity.ForcedMovementRemainingSeconds <= 0.0f)
                        EndForcedMovement(entity, ForcedMovementEndReason.Timeout, entity.Position);
                }
            }

            if (changed)
                _stateVersion++;
        }

        public CombatEffectsSnapshot CreateSnapshot()
        {
            if (_snapshotVersion != _stateVersion)
            {
                CombatEntitySnapshot[] snapshots = new CombatEntitySnapshot[_entities.Count];
                for (int index = 0; index < _entities.Count; index++)
                    snapshots[index] = new CombatEntitySnapshot(_entities[index]);
                _snapshotEntities = snapshots;
                _snapshotVersion = _stateVersion;
            }

            ulong digest = 14695981039346656037UL;
            AddDigest(ref digest, (ulong)(uint)_deathCount);
            AddDigest(ref digest, (ulong)(uint)_forcedMovementStartCount);
            AddDigest(ref digest, (ulong)(uint)_forcedMovementEndCount);
            AddDigest(ref digest, (ulong)(uint)_lastDeath.EntityId);
            AddDigest(ref digest, (ulong)(uint)_lastDeath.KillerEntityId);
            AddStringDigest(ref digest, _lastDeath.AttackId);
            AddDigest(ref digest, (ulong)(uint)_lastDeath.DamageKind);
            AddPointDigest(ref digest, _lastDeath.Position);
            for (int index = 0; index < _entities.Count; index++)
            {
                EntityState entity = _entities[index];
                AddDigest(ref digest, (ulong)(uint)entity.Definition.EntityId);
                AddDigest(ref digest, (ulong)(uint)entity.Definition.Kind);
                AddDigest(ref digest, (ulong)(uint)entity.Definition.MaximumHealth);
                AddFloatDigest(ref digest, entity.Definition.ReceivedDamageMultiplier);
                AddFloatDigest(ref digest, entity.Definition.CommanderDamageReduction);
                AddDigest(ref digest, (ulong)(uint)entity.Health);
                AddPointDigest(ref digest, entity.Position);
                AddDigest(ref digest, (ulong)(uint)entity.DirectGuardCharges);
                AddDigest(ref digest, entity.IsActionActive ? 1UL : 0UL);
                AddDigest(ref digest, (ulong)(uint)entity.PendingUnspawnedAttackCount);
                AddDigest(ref digest, (ulong)(uint)entity.ActionCancelCount);
                AddDigest(ref digest, entity.IsForcedMovementLocked ? 1UL : 0UL);
                AddPointDigest(ref digest, entity.ForcedMovementDestination);
                AddFloatDigest(ref digest, entity.ForcedMovementStrength);
                AddFloatDigest(ref digest, entity.ForcedMovementRemainingSeconds);
                AddDigest(ref digest, (ulong)(uint)entity.LastForcedMovementEndReason);
                AddDigest(ref digest, (ulong)(uint)entity.DeathReactionCount);
                AddDigest(ref digest, (ulong)(uint)entity.Statuses.Count);
                for (int statusIndex = 0; statusIndex < entity.Statuses.Count; statusIndex++)
                {
                    StatusState status = entity.Statuses[statusIndex];
                    AddDigest(ref digest, (ulong)(uint)status.SourceEntityId);
                    AddDigest(ref digest, (ulong)(uint)status.Kind);
                    AddFloatDigest(ref digest, status.Magnitude);
                    AddFloatDigest(ref digest, status.RemainingSeconds);
                    AddDigest(ref digest, (ulong)(uint)status.Charges);
                }
            }

            return new CombatEffectsSnapshot(
                _snapshotEntities,
                _deathCount,
                _lastDeath,
                _forcedMovementStartCount,
                _forcedMovementEndCount,
                digest);
        }

        private void CommitDeath(EntityState target, DamageRequest request)
        {
            target.Health = 0;
            CancelAction(target);
            if (target.IsForcedMovementLocked)
                EndForcedMovement(target, ForcedMovementEndReason.Death, target.Position);
            target.DeathReactionCount++;
            _deathCount++;
            _lastDeath = new CombatDeathSnapshot(
                target.Definition.EntityId,
                request.SourceEntityId,
                request.AttackId,
                request.Kind,
                target.Position);
        }

        private static void CancelAction(EntityState target)
        {
            if (target.IsActionActive == false && target.PendingUnspawnedAttackCount == 0)
                return;
            target.IsActionActive = false;
            target.PendingUnspawnedAttackCount = 0;
            target.ActionCancelCount++;
        }

        private void EndForcedMovement(
            EntityState target,
            ForcedMovementEndReason reason,
            RunPoint finalPosition)
        {
            target.Position = finalPosition;
            target.IsForcedMovementLocked = false;
            target.ForcedMovementStrength = 0.0f;
            target.ForcedMovementRemainingSeconds = 0.0f;
            target.LastForcedMovementEndReason = reason;
            _forcedMovementEndCount++;
            _stateVersion++;
        }

        private static bool IsHigherPriority(ForcedMovementRequest candidate, ForcedMovementRequest current)
        {
            if (candidate.Priority != current.Priority)
                return candidate.Priority > current.Priority;
            if (candidate.Strength != current.Strength)
                return candidate.Strength > current.Strength;
            return candidate.SourceEntityId < current.SourceEntityId;
        }

        private static float GetStrongestMultiplier(
            EntityState entity,
            CombatStatusKind kind,
            float fallback)
        {
            float strongest = fallback;
            for (int index = 0; index < entity.Statuses.Count; index++)
            {
                StatusState status = entity.Statuses[index];
                if (status.Kind == kind && status.Magnitude > strongest)
                    strongest = status.Magnitude;
            }
            return strongest;
        }

        private static StatusState FindStrongestWeaken(EntityState entity)
        {
            StatusState strongest = null;
            for (int index = 0; index < entity.Statuses.Count; index++)
            {
                StatusState status = entity.Statuses[index];
                if (status.Kind != CombatStatusKind.Weakened || status.Charges <= 0)
                    continue;
                if (strongest == null || status.Magnitude > strongest.Magnitude ||
                    (status.Magnitude == strongest.Magnitude && status.SourceEntityId < strongest.SourceEntityId))
                {
                    strongest = status;
                }
            }
            return strongest;
        }

        private static void ConsumeStatus(EntityState entity, StatusState status)
        {
            status.Charges--;
            if (status.Charges <= 0)
                entity.Statuses.Remove(status);
        }

        private EntityState FindEntity(int entityId)
        {
            for (int index = 0; index < _entities.Count; index++)
            {
                if (_entities[index].Definition.EntityId == entityId)
                    return _entities[index];
            }
            return null;
        }

        private static void AddPointDigest(ref ulong value, RunPoint point)
        {
            AddDigest(ref value, unchecked((ulong)(uint)BitConverter.SingleToInt32Bits(point.X)));
            AddDigest(ref value, unchecked((ulong)(uint)BitConverter.SingleToInt32Bits(point.Y)));
        }

        private static void AddFloatDigest(ref ulong value, float part)
        {
            AddDigest(ref value, unchecked((ulong)(uint)BitConverter.SingleToInt32Bits(part)));
        }

        private static void AddStringDigest(ref ulong value, string part)
        {
            if (part == null)
            {
                AddDigest(ref value, 0UL);
                return;
            }
            AddDigest(ref value, (ulong)(uint)part.Length);
            for (int index = 0; index < part.Length; index++)
                AddDigest(ref value, part[index]);
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
    }

    internal sealed class EntityState
    {
        internal CombatEntityDefinition Definition { get; }
        internal List<StatusState> Statuses { get; } = new List<StatusState>(4);
        internal int Health { get; set; }
        internal RunPoint Position { get; set; }
        internal int DirectGuardCharges { get; set; }
        internal bool IsActionActive { get; set; }
        internal int PendingUnspawnedAttackCount { get; set; }
        internal int ActionCancelCount { get; set; }
        internal bool IsForcedMovementLocked { get; set; }
        internal RunPoint ForcedMovementDestination { get; set; }
        internal float ForcedMovementStrength { get; set; }
        internal float ForcedMovementRemainingSeconds { get; set; }
        internal ForcedMovementEndReason LastForcedMovementEndReason { get; set; }
        internal int DeathReactionCount { get; set; }

        internal EntityState(CombatEntityDefinition definition)
        {
            Definition = definition;
            Health = definition.MaximumHealth;
            Position = definition.InitialPosition;
            DirectGuardCharges = definition.DirectGuardCharges;
        }
    }

    internal sealed class StatusState
    {
        internal int SourceEntityId { get; }
        internal CombatStatusKind Kind { get; }
        internal float Magnitude { get; }
        internal float RemainingSeconds { get; set; }
        internal int Charges { get; set; }

        internal StatusState(StatusRequest request)
        {
            SourceEntityId = request.SourceEntityId;
            Kind = request.Kind;
            Magnitude = request.Magnitude;
            RemainingSeconds = request.DurationSeconds;
            Charges = request.Charges;
        }
    }

    internal readonly struct StatusSnapshot
    {
        internal int SourceEntityId { get; }
        internal CombatStatusKind Kind { get; }
        internal float RemainingSeconds { get; }

        internal StatusSnapshot(StatusState state)
        {
            SourceEntityId = state.SourceEntityId;
            Kind = state.Kind;
            RemainingSeconds = state.RemainingSeconds;
        }
    }
}
