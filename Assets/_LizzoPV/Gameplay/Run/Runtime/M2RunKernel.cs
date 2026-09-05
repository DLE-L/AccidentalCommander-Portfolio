using System;
using System.Collections.Generic;

namespace Lizzo.PV.Gameplay.Run.M2
{
    [Flags]
    public enum SimulationBlocker
    {
        None = 0,
        InitialRecruit = 1 << 0,
        GrowthSelection = 1 << 1,
        UserPause = 1 << 2,
        BackgroundPause = 1 << 3,
        BossIntroduction = 1 << 4,
        ResultLock = 1 << 5,
    }

    public enum RunSessionOutcome
    {
        None,
        Victory,
        Defeat,
        Abandoned,
    }

    public sealed class RunDefinitionSnapshot
    {
        public int Seed { get; }
        public SwordVerticalDefinition SwordVertical { get; }
        public FormationGrowthDefinition FormationGrowth { get; }

        public RunDefinitionSnapshot(int seed)
            : this(seed, SwordVerticalDefinition.Disabled, FormationGrowthDefinition.Disabled)
        {
        }

        public RunDefinitionSnapshot(int seed, SwordVerticalDefinition swordVertical)
            : this(seed, swordVertical, FormationGrowthDefinition.Disabled)
        {
        }

        public RunDefinitionSnapshot(
            int seed,
            SwordVerticalDefinition swordVertical,
            FormationGrowthDefinition formationGrowth)
        {
            Seed = seed;
            SwordVertical = swordVertical ?? throw new ArgumentNullException(nameof(swordVertical));
            FormationGrowth = formationGrowth ?? throw new ArgumentNullException(nameof(formationGrowth));
        }
    }

    public readonly struct RunRuntimeSnapshot
    {
        public bool IsStarted { get; }
        public float ElapsedSeconds { get; }
        public SimulationBlocker ActiveBlockers { get; }
        public RunSessionOutcome Outcome { get; }
        public long ResolutionStamp { get; }
        public int ResultCommitCount { get; }
        public ulong StateDigest { get; }
        public SwordVerticalSnapshot SwordVertical { get; }
        public FormationGrowthSnapshot FormationGrowth { get; }
        public CombatEffectsSnapshot CombatEffects { get; }

        internal RunRuntimeSnapshot(
            bool isStarted,
            float elapsedSeconds,
            SimulationBlocker activeBlockers,
            RunSessionOutcome outcome,
            long resolutionStamp,
            int resultCommitCount,
            ulong stateDigest,
            SwordVerticalSnapshot swordVertical,
            FormationGrowthSnapshot formationGrowth,
            CombatEffectsSnapshot combatEffects)
        {
            IsStarted = isStarted;
            ElapsedSeconds = elapsedSeconds;
            ActiveBlockers = activeBlockers;
            Outcome = outcome;
            ResolutionStamp = resolutionStamp;
            ResultCommitCount = resultCommitCount;
            StateDigest = stateDigest;
            SwordVertical = swordVertical;
            FormationGrowth = formationGrowth;
            CombatEffects = combatEffects;
        }
    }

    public readonly struct RunCommand
    {
        internal RunCommandType Type { get; }
        internal SimulationBlocker Blocker { get; }
        internal int EntityId { get; }
        internal int ValueA { get; }
        internal int ValueB { get; }
        internal int ValueC { get; }
        internal RunPoint Point { get; }
        internal CombatEntityDefinition CombatEntityDefinition { get; }
        internal DamageRequest DamageRequest { get; }
        internal HealingRequest HealingRequest { get; }
        internal StatusRequest StatusRequest { get; }
        internal ForcedMovementRequest[] ForcedMovementRequests { get; }
        internal ForcedMovementEndReason ForcedMovementEndReason { get; }

        private RunCommand(RunCommandType type, SimulationBlocker blocker)
            : this(type, blocker, 0, default, 0, 0, 0)
        {
        }

        private RunCommand(
            RunCommandType type,
            SimulationBlocker blocker,
            int entityId,
            RunPoint point,
            int valueA,
            int valueB,
            int valueC,
            CombatEntityDefinition combatEntityDefinition = null,
            DamageRequest damageRequest = default,
            HealingRequest healingRequest = default,
            StatusRequest statusRequest = default,
            ForcedMovementRequest[] forcedMovementRequests = null,
            ForcedMovementEndReason forcedMovementEndReason = default)
        {
            Type = type;
            Blocker = blocker;
            EntityId = entityId;
            Point = point;
            ValueA = valueA;
            ValueB = valueB;
            ValueC = valueC;
            CombatEntityDefinition = combatEntityDefinition;
            DamageRequest = damageRequest;
            HealingRequest = healingRequest;
            StatusRequest = statusRequest;
            ForcedMovementRequests = forcedMovementRequests;
            ForcedMovementEndReason = forcedMovementEndReason;
        }

        public static RunCommand AddBlocker(SimulationBlocker blocker)
        {
            ValidateMutableBlocker(blocker);
            return new RunCommand(RunCommandType.AddBlocker, blocker);
        }

        public static RunCommand ClearBlocker(SimulationBlocker blocker)
        {
            if (blocker == SimulationBlocker.None)
                throw new ArgumentOutOfRangeException(nameof(blocker));

            return new RunCommand(RunCommandType.ClearBlocker, blocker);
        }

        public static RunCommand BossDefeated()
        {
            return new RunCommand(RunCommandType.BossDefeated, SimulationBlocker.None);
        }

        public static RunCommand CommanderDefeated()
        {
            return new RunCommand(RunCommandType.CommanderDefeated, SimulationBlocker.None);
        }

        public static RunCommand Abandon()
        {
            return new RunCommand(RunCommandType.Abandon, SimulationBlocker.None);
        }

        public static RunCommand ChooseSwordGrowthCard()
        {
            return new RunCommand(RunCommandType.ChooseSwordGrowthCard, SimulationBlocker.None);
        }

        public static RunCommand ChooseGrowthOffer(int slotIndex)
        {
            if (slotIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(slotIndex));
            return new RunCommand(
                RunCommandType.ChooseGrowthOffer,
                SimulationBlocker.None,
                0,
                default,
                slotIndex,
                0,
                0);
        }

        public static RunCommand ExperienceAbsorbed(int amount)
        {
            if (amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount));
            return new RunCommand(
                RunCommandType.ExperienceAbsorbed,
                SimulationBlocker.None,
                0,
                default,
                amount,
                0,
                0);
        }

        public static RunCommand RegisterCombatEntity(CombatEntityDefinition definition)
        {
            return new RunCommand(
                RunCommandType.RegisterCombatEntity,
                SimulationBlocker.None,
                0,
                default,
                0,
                0,
                0,
                combatEntityDefinition: definition ?? throw new ArgumentNullException(nameof(definition)));
        }

        public static RunCommand SetCombatEntityPosition(int entityId, RunPoint position)
        {
            if (entityId <= 0)
                throw new ArgumentOutOfRangeException(nameof(entityId));
            return new RunCommand(
                RunCommandType.SetCombatEntityPosition,
                SimulationBlocker.None,
                entityId,
                position,
                0,
                0,
                0);
        }

        public static RunCommand BeginCombatAction(int entityId, int pendingUnspawnedAttackCount)
        {
            if (entityId <= 0)
                throw new ArgumentOutOfRangeException(nameof(entityId));
            if (pendingUnspawnedAttackCount < 0)
                throw new ArgumentOutOfRangeException(nameof(pendingUnspawnedAttackCount));
            return new RunCommand(
                RunCommandType.BeginCombatAction,
                SimulationBlocker.None,
                entityId,
                default,
                pendingUnspawnedAttackCount,
                0,
                0);
        }

        public static RunCommand ResolveCombatDamage(DamageRequest request)
        {
            return new RunCommand(
                RunCommandType.ResolveCombatDamage,
                SimulationBlocker.None,
                0,
                default,
                0,
                0,
                0,
                damageRequest: request);
        }

        public static RunCommand ResolveCombatHealing(HealingRequest request)
        {
            return new RunCommand(
                RunCommandType.ResolveCombatHealing,
                SimulationBlocker.None,
                0,
                default,
                0,
                0,
                0,
                healingRequest: request);
        }

        public static RunCommand ApplyCombatStatus(StatusRequest request)
        {
            return new RunCommand(
                RunCommandType.ApplyCombatStatus,
                SimulationBlocker.None,
                0,
                default,
                0,
                0,
                0,
                statusRequest: request);
        }

        public static RunCommand ResolveForcedMovement(ForcedMovementRequest[] requests)
        {
            if (requests == null || requests.Length == 0)
                throw new ArgumentOutOfRangeException(nameof(requests));
            ForcedMovementRequest[] copy = new ForcedMovementRequest[requests.Length];
            Array.Copy(requests, copy, requests.Length);
            return new RunCommand(
                RunCommandType.ResolveForcedMovement,
                SimulationBlocker.None,
                0,
                default,
                0,
                0,
                0,
                forcedMovementRequests: copy);
        }

        public static RunCommand EndForcedMovement(
            int entityId,
            ForcedMovementEndReason reason,
            RunPoint finalPosition)
        {
            if (entityId <= 0)
                throw new ArgumentOutOfRangeException(nameof(entityId));
            if (reason == ForcedMovementEndReason.None || reason == ForcedMovementEndReason.Death)
                throw new ArgumentOutOfRangeException(nameof(reason));
            return new RunCommand(
                RunCommandType.EndForcedMovement,
                SimulationBlocker.None,
                entityId,
                finalPosition,
                0,
                0,
                0,
                forcedMovementEndReason: reason);
        }

        public static RunCommand SpawnVerticalEnemy(
            int enemyId,
            RunPoint position,
            int health,
            int contactDamage,
            int experience)
        {
            return new RunCommand(
                RunCommandType.SpawnVerticalEnemy,
                SimulationBlocker.None,
                enemyId,
                position,
                health,
                contactDamage,
                experience);
        }

        public static RunCommand MoveVerticalEnemy(int enemyId, RunPoint position)
        {
            return new RunCommand(
                RunCommandType.MoveVerticalEnemy,
                SimulationBlocker.None,
                enemyId,
                position,
                0,
                0,
                0);
        }

        public static RunCommand MoveCommander(RunPoint position)
        {
            return new RunCommand(
                RunCommandType.MoveCommander,
                SimulationBlocker.None,
                0,
                position,
                0,
                0,
                0);
        }

        public static RunCommand SetSwordSlot(RunPoint position)
        {
            return new RunCommand(
                RunCommandType.SetSwordSlot,
                SimulationBlocker.None,
                0,
                position,
                0,
                0,
                0);
        }

        public static RunCommand VerticalEnemyContact(int enemyId)
        {
            return new RunCommand(
                RunCommandType.VerticalEnemyContact,
                SimulationBlocker.None,
                enemyId,
                default,
                0,
                0,
                0);
        }

        public static RunCommand SwordReachedActionPoint()
        {
            return new RunCommand(RunCommandType.SwordReachedActionPoint, SimulationBlocker.None);
        }

        public static RunCommand SwordReachedSlot()
        {
            return new RunCommand(RunCommandType.SwordReachedSlot, SimulationBlocker.None);
        }

        private static void ValidateMutableBlocker(SimulationBlocker blocker)
        {
            if (blocker == SimulationBlocker.None ||
                (blocker & SimulationBlocker.ResultLock) != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(blocker));
            }
        }
    }

    internal enum RunCommandType
    {
        AddBlocker,
        ClearBlocker,
        BossDefeated,
        CommanderDefeated,
        Abandon,
        ChooseSwordGrowthCard,
        ChooseGrowthOffer,
        ExperienceAbsorbed,
        RegisterCombatEntity,
        SetCombatEntityPosition,
        BeginCombatAction,
        ResolveCombatDamage,
        ResolveCombatHealing,
        ApplyCombatStatus,
        ResolveForcedMovement,
        EndForcedMovement,
        SpawnVerticalEnemy,
        MoveVerticalEnemy,
        MoveCommander,
        SetSwordSlot,
        VerticalEnemyContact,
        SwordReachedActionPoint,
        SwordReachedSlot,
    }

    public static class RunCompositionRoot
    {
        public static RunRuntimeHost Build(RunDefinitionSnapshot definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            SimulationClock clock = new SimulationClock();
            RunCombatSession session = new RunCombatSession();
            FormationGrowthRuntime formationGrowth = new FormationGrowthRuntime(definition.FormationGrowth);
            CombatResolver combatResolver = new CombatResolver();
            SwordVerticalRuntime swordVertical = new SwordVerticalRuntime(
                definition.SwordVertical,
                formationGrowth.IsEnabled);
            return new RunRuntimeHost(
                definition,
                clock,
                session,
                swordVertical,
                formationGrowth,
                combatResolver);
        }
    }

    public sealed class RunRuntimeHost : IDisposable
    {
        private readonly RunDefinitionSnapshot _definition;
        private readonly SimulationClock _clock;
        private readonly RunCombatSession _session;
        private readonly SwordVerticalRuntime _swordVertical;
        private readonly FormationGrowthRuntime _formationGrowth;
        private readonly CombatResolver _combatResolver;
        private readonly List<RunCommand> _commands = new List<RunCommand>(8);
        private RunRuntimeSnapshot _snapshot;
        private bool _disposed;

        internal RunRuntimeHost(
            RunDefinitionSnapshot definition,
            SimulationClock clock,
            RunCombatSession session,
            SwordVerticalRuntime swordVertical,
            FormationGrowthRuntime formationGrowth,
            CombatResolver combatResolver)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _swordVertical = swordVertical ?? throw new ArgumentNullException(nameof(swordVertical));
            _formationGrowth = formationGrowth ?? throw new ArgumentNullException(nameof(formationGrowth));
            _combatResolver = combatResolver ?? throw new ArgumentNullException(nameof(combatResolver));
            RefreshSnapshot();
        }

        public RunRuntimeSnapshot CurrentSnapshot
        {
            get
            {
                EnsureNotDisposed();
                return _snapshot;
            }
        }

        public bool Start()
        {
            EnsureNotDisposed();
            if (!_session.TryStart())
                return false;

            _formationGrowth.BeginInitialOffer(_definition.Seed);
            RefreshSnapshot();
            return true;
        }

        public void Submit(RunCommand command)
        {
            EnsureNotDisposed();
            if (!_session.IsStarted)
                throw new InvalidOperationException("Run must be started before commands are submitted.");

            _commands.Add(command);
        }

        public void Advance(float deltaSeconds)
        {
            EnsureNotDisposed();
            if (!_session.IsStarted)
                throw new InvalidOperationException("Run must be started before it advances.");
            if (float.IsNaN(deltaSeconds) || float.IsInfinity(deltaSeconds) || deltaSeconds < 0.0f)
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds));

            _session.BeginResolution();
            ProcessCommands();
            if (!_session.IsBlocked)
            {
                _clock.Advance(deltaSeconds);
                _combatResolver.Advance(deltaSeconds);
                _swordVertical.Advance(deltaSeconds);
                if (_formationGrowth.IsEnabled)
                {
                    int absorbedExperience = _swordVertical.ConsumeNewlyAbsorbedExperience();
                    if (absorbedExperience > 0)
                        _formationGrowth.AddExperience(absorbedExperience);
                    SyncGrowthBlocker();
                }
                else if (_swordVertical.ConsumeGrowthSelectionRequest())
                {
                    _session.AddBlocker(SimulationBlocker.GrowthSelection);
                }
                if (_swordVertical.CommanderDefeated)
                    _session.ResolveOutcome(false, true, false);
            }
            RefreshSnapshot();
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _commands.Clear();
            _disposed = true;
        }

        private void ProcessCommands()
        {
            bool bossDefeated = false;
            bool commanderDefeated = false;
            bool abandoned = false;

            if (!_session.HasResult)
            {
                for (int i = 0; i < _commands.Count; i++)
                {
                    RunCommand command = _commands[i];
                    switch (command.Type)
                    {
                        case RunCommandType.AddBlocker:
                            _session.AddBlocker(command.Blocker);
                            break;
                        case RunCommandType.ClearBlocker:
                            _session.ClearBlocker(command.Blocker);
                            break;
                        case RunCommandType.BossDefeated:
                            bossDefeated = true;
                            break;
                        case RunCommandType.CommanderDefeated:
                            commanderDefeated = true;
                            break;
                        case RunCommandType.Abandon:
                            abandoned = true;
                            break;
                        case RunCommandType.ChooseSwordGrowthCard:
                            if (_formationGrowth.IsEnabled)
                                break;
                            bool recruited = _swordVertical.TryChooseGrowthCard(
                                (_session.ActiveBlockers & SimulationBlocker.InitialRecruit) != 0,
                                (_session.ActiveBlockers & SimulationBlocker.GrowthSelection) != 0);
                            if (recruited)
                            {
                                _session.ClearBlocker(SimulationBlocker.InitialRecruit);
                                if (_swordVertical.HasGrowthChoice == false)
                                    _session.ClearBlocker(SimulationBlocker.GrowthSelection);
                            }
                            break;
                        case RunCommandType.ChooseGrowthOffer:
                            bool growthAllowed = _formationGrowth.ActiveOfferIsInitial
                                ? (_session.ActiveBlockers & SimulationBlocker.InitialRecruit) != 0
                                : (_session.ActiveBlockers & SimulationBlocker.GrowthSelection) != 0;
                            if (growthAllowed && _formationGrowth.TryChoose(command.ValueA, out LegionGrowthApplication application))
                            {
                                _swordVertical.TryApplyExternalGrowth(application.BaseUnitId);
                                SyncSwordSlot();
                                _session.ClearBlocker(SimulationBlocker.InitialRecruit);
                                SyncGrowthBlocker();
                            }
                            break;
                        case RunCommandType.ExperienceAbsorbed:
                            _formationGrowth.AddExperience(command.ValueA);
                            SyncGrowthBlocker();
                            break;
                        case RunCommandType.RegisterCombatEntity:
                            _combatResolver.Register(command.CombatEntityDefinition);
                            break;
                        case RunCommandType.SetCombatEntityPosition:
                            _combatResolver.SetPosition(command.EntityId, command.Point);
                            break;
                        case RunCommandType.BeginCombatAction:
                            _combatResolver.BeginAction(command.EntityId, command.ValueA);
                            break;
                        case RunCommandType.ResolveCombatDamage:
                            _combatResolver.ApplyDamage(command.DamageRequest);
                            break;
                        case RunCommandType.ResolveCombatHealing:
                            _combatResolver.ApplyHealing(command.HealingRequest);
                            break;
                        case RunCommandType.ApplyCombatStatus:
                            _combatResolver.ApplyStatus(command.StatusRequest);
                            break;
                        case RunCommandType.ResolveForcedMovement:
                            _combatResolver.ResolveForcedMovement(command.ForcedMovementRequests);
                            break;
                        case RunCommandType.EndForcedMovement:
                            _combatResolver.EndForcedMovement(
                                command.EntityId,
                                command.ForcedMovementEndReason,
                                command.Point);
                            break;
                        case RunCommandType.SpawnVerticalEnemy:
                            _swordVertical.SpawnEnemy(
                                command.EntityId,
                                command.Point,
                                command.ValueA,
                                command.ValueB,
                                command.ValueC);
                            break;
                        case RunCommandType.MoveVerticalEnemy:
                            _swordVertical.MoveEnemy(command.EntityId, command.Point);
                            break;
                        case RunCommandType.MoveCommander:
                            _swordVertical.MoveCommander(command.Point);
                            break;
                        case RunCommandType.SetSwordSlot:
                            _swordVertical.SetSwordSlot(command.Point);
                            break;
                        case RunCommandType.VerticalEnemyContact:
                            _swordVertical.ApplyEnemyContact(command.EntityId);
                            commanderDefeated |= _swordVertical.CommanderDefeated;
                            break;
                        case RunCommandType.SwordReachedActionPoint:
                            _swordVertical.ReachActionPoint();
                            break;
                        case RunCommandType.SwordReachedSlot:
                            _swordVertical.ReachSlot();
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                _session.ResolveOutcome(bossDefeated, commanderDefeated, abandoned);
            }

            _commands.Clear();
        }

        private void RefreshSnapshot()
        {
            SwordVerticalSnapshot swordVertical = _swordVertical.CreateSnapshot();
            FormationGrowthSnapshot formationGrowth = _formationGrowth.CreateSnapshot();
            CombatEffectsSnapshot combatEffects = _combatResolver.CreateSnapshot();
            ulong digest = RunStateDigest.Calculate(
                _definition.Seed,
                _session.IsStarted,
                _clock.ElapsedSeconds,
                _session.ActiveBlockers,
                _session.Outcome,
                _session.ResolutionStamp,
                _session.ResultCommitCount,
                swordVertical.StateDigest,
                formationGrowth.StateDigest,
                combatEffects.StateDigest);
            _snapshot = new RunRuntimeSnapshot(
                _session.IsStarted,
                _clock.ElapsedSeconds,
                _session.ActiveBlockers,
                _session.Outcome,
                _session.ResolutionStamp,
                _session.ResultCommitCount,
                digest,
                swordVertical,
                formationGrowth,
                combatEffects);
        }

        private void SyncGrowthBlocker()
        {
            if (_formationGrowth.IsEnabled == false)
                return;

            if (_formationGrowth.HasActiveOffer && _formationGrowth.ActiveOfferIsInitial == false)
                _session.AddBlocker(SimulationBlocker.GrowthSelection);
            else
                _session.ClearBlocker(SimulationBlocker.GrowthSelection);
        }

        private void SyncSwordSlot()
        {
            if (_formationGrowth.TryGetPrimarySlot("sword_soldier", out RunPoint slotPosition))
                _swordVertical.SetSwordSlot(slotPosition);
        }

        private void EnsureNotDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(RunRuntimeHost));
        }
    }

    internal sealed class SimulationClock
    {
        internal float ElapsedSeconds { get; private set; }

        internal void Advance(float deltaSeconds)
        {
            ElapsedSeconds += deltaSeconds;
        }
    }

    internal sealed class RunCombatSession
    {
        internal bool IsStarted { get; private set; }
        internal SimulationBlocker ActiveBlockers { get; private set; } = SimulationBlocker.InitialRecruit;
        internal RunSessionOutcome Outcome { get; private set; }
        internal long ResolutionStamp { get; private set; }
        internal int ResultCommitCount { get; private set; }
        internal bool HasResult => Outcome != RunSessionOutcome.None;
        internal bool IsBlocked => ActiveBlockers != SimulationBlocker.None;

        internal bool TryStart()
        {
            if (IsStarted)
                return false;

            IsStarted = true;
            return true;
        }

        internal void BeginResolution()
        {
            ResolutionStamp++;
        }

        internal void AddBlocker(SimulationBlocker blocker)
        {
            if (HasResult)
                return;

            ActiveBlockers |= blocker;
        }

        internal void ClearBlocker(SimulationBlocker blocker)
        {
            if (HasResult)
                return;

            ActiveBlockers &= ~blocker;
        }

        internal void ResolveOutcome(bool bossDefeated, bool commanderDefeated, bool abandoned)
        {
            if (HasResult)
                return;

            RunSessionOutcome outcome = RunSessionOutcome.None;
            if (bossDefeated)
                outcome = RunSessionOutcome.Victory;
            else if (commanderDefeated)
                outcome = RunSessionOutcome.Defeat;
            else if (abandoned)
                outcome = RunSessionOutcome.Abandoned;

            if (outcome == RunSessionOutcome.None)
                return;

            Outcome = outcome;
            ResultCommitCount = 1;
            ActiveBlockers |= SimulationBlocker.ResultLock;
        }
    }

    internal static class RunStateDigest
    {
        private const ulong Offset = 14695981039346656037UL;
        private const ulong Prime = 1099511628211UL;

        internal static ulong Calculate(
            int seed,
            bool isStarted,
            float elapsedSeconds,
            SimulationBlocker blockers,
            RunSessionOutcome outcome,
            long resolutionStamp,
            int resultCommitCount,
            ulong swordVerticalDigest,
            ulong formationGrowthDigest,
            ulong combatEffectsDigest)
        {
            ulong value = Offset;
            Add(ref value, unchecked((ulong)(uint)seed));
            Add(ref value, isStarted ? 1UL : 0UL);
            Add(ref value, unchecked((ulong)(uint)BitConverter.SingleToInt32Bits(elapsedSeconds)));
            Add(ref value, unchecked((ulong)(uint)blockers));
            Add(ref value, unchecked((ulong)(uint)outcome));
            Add(ref value, unchecked((ulong)resolutionStamp));
            Add(ref value, unchecked((ulong)(uint)resultCommitCount));
            Add(ref value, swordVerticalDigest);
            Add(ref value, formationGrowthDigest);
            Add(ref value, combatEffectsDigest);
            return value;
        }

        private static void Add(ref ulong value, ulong part)
        {
            for (int shift = 0; shift < 64; shift += 8)
            {
                value ^= (byte)(part >> shift);
                value *= Prime;
            }
        }
    }
}
