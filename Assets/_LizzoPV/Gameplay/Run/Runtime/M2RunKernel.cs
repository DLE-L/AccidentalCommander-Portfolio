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

        public RunDefinitionSnapshot(int seed)
        {
            Seed = seed;
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

        internal RunRuntimeSnapshot(
            bool isStarted,
            float elapsedSeconds,
            SimulationBlocker activeBlockers,
            RunSessionOutcome outcome,
            long resolutionStamp,
            int resultCommitCount,
            ulong stateDigest)
        {
            IsStarted = isStarted;
            ElapsedSeconds = elapsedSeconds;
            ActiveBlockers = activeBlockers;
            Outcome = outcome;
            ResolutionStamp = resolutionStamp;
            ResultCommitCount = resultCommitCount;
            StateDigest = stateDigest;
        }
    }

    public readonly struct RunCommand
    {
        internal RunCommandType Type { get; }
        internal SimulationBlocker Blocker { get; }

        private RunCommand(RunCommandType type, SimulationBlocker blocker)
        {
            Type = type;
            Blocker = blocker;
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
    }

    public static class RunCompositionRoot
    {
        public static RunRuntimeHost Build(RunDefinitionSnapshot definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            SimulationClock clock = new SimulationClock();
            RunCombatSession session = new RunCombatSession();
            return new RunRuntimeHost(definition, clock, session);
        }
    }

    public sealed class RunRuntimeHost : IDisposable
    {
        private readonly RunDefinitionSnapshot _definition;
        private readonly SimulationClock _clock;
        private readonly RunCombatSession _session;
        private readonly List<RunCommand> _commands = new List<RunCommand>(8);
        private RunRuntimeSnapshot _snapshot;
        private bool _disposed;

        internal RunRuntimeHost(
            RunDefinitionSnapshot definition,
            SimulationClock clock,
            RunCombatSession session)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _session = session ?? throw new ArgumentNullException(nameof(session));
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
                _clock.Advance(deltaSeconds);
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
            ulong digest = RunStateDigest.Calculate(
                _definition.Seed,
                _session.IsStarted,
                _clock.ElapsedSeconds,
                _session.ActiveBlockers,
                _session.Outcome,
                _session.ResolutionStamp,
                _session.ResultCommitCount);
            _snapshot = new RunRuntimeSnapshot(
                _session.IsStarted,
                _clock.ElapsedSeconds,
                _session.ActiveBlockers,
                _session.Outcome,
                _session.ResolutionStamp,
                _session.ResultCommitCount,
                digest);
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
            int resultCommitCount)
        {
            ulong value = Offset;
            Add(ref value, unchecked((ulong)(uint)seed));
            Add(ref value, isStarted ? 1UL : 0UL);
            Add(ref value, unchecked((ulong)(uint)BitConverter.SingleToInt32Bits(elapsedSeconds)));
            Add(ref value, unchecked((ulong)(uint)blockers));
            Add(ref value, unchecked((ulong)(uint)outcome));
            Add(ref value, unchecked((ulong)resolutionStamp));
            Add(ref value, unchecked((ulong)(uint)resultCommitCount));
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
