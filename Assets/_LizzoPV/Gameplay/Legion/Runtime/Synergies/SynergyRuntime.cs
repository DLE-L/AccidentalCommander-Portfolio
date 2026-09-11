using System;
using System.Collections.Generic;

namespace Lizzo.PV.Gameplay.Run
{
    public sealed class SynergyRuntimeDefinition
    {
        public static SynergyRuntimeDefinition Disabled { get; } = new SynergyRuntimeDefinition();

        private readonly SynergyDefinition[] _synergies;

        internal bool IsEnabled { get; }
        public int MaxConcurrentExecutions { get; }
        public int MaxConcurrentPairExecutions { get; }
        public int MaxConcurrentTrioExecutions { get; }
        public IReadOnlyList<SynergyDefinition> Synergies => _synergies;

        private SynergyRuntimeDefinition()
        {
            _synergies = Array.Empty<SynergyDefinition>();
            MaxConcurrentExecutions = 1;
            MaxConcurrentPairExecutions = 1;
            MaxConcurrentTrioExecutions = 1;
        }

        public SynergyRuntimeDefinition(int maxConcurrentExecutions, SynergyDefinition[] synergies)
            : this(maxConcurrentExecutions, maxConcurrentExecutions, synergies)
        {
        }

        public SynergyRuntimeDefinition(
            int maxConcurrentPairExecutions,
            int maxConcurrentTrioExecutions,
            SynergyDefinition[] synergies)
        {
            if (maxConcurrentPairExecutions <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxConcurrentPairExecutions));
            if (maxConcurrentTrioExecutions <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxConcurrentTrioExecutions));
            if (synergies == null || synergies.Length == 0 || synergies.Length > 20)
                throw new ArgumentOutOfRangeException(nameof(synergies));

            _synergies = new SynergyDefinition[synergies.Length];
            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < synergies.Length; index++)
            {
                SynergyDefinition synergy = synergies[index]
                    ?? throw new ArgumentException("Synergy definition cannot be null.", nameof(synergies));
                if (ids.Add(synergy.SynergyId) == false)
                    throw new ArgumentException($"Duplicate synergy id: {synergy.SynergyId}", nameof(synergies));
                _synergies[index] = synergy;
            }

            MaxConcurrentPairExecutions = maxConcurrentPairExecutions;
            MaxConcurrentTrioExecutions = maxConcurrentTrioExecutions;
            MaxConcurrentExecutions = maxConcurrentPairExecutions + maxConcurrentTrioExecutions;
            IsEnabled = true;
        }
    }

    public readonly struct SynergyStateSnapshot
    {
        public string SynergyId { get; }
        public SynergyTier Tier { get; }
        public bool IsActive { get; }
        public string CasterUnitId { get; }
        public SynergyCasterPresentation Presentation { get; }
        public SynergyTriggerPriority TriggerPriority { get; }
        public float NextReadyTime { get; }

        internal SynergyStateSnapshot(SynergyState state)
        {
            SynergyId = state.Definition.SynergyId;
            Tier = state.Definition.Tier;
            IsActive = state.IsActive;
            CasterUnitId = state.Definition.CasterUnitId;
            Presentation = state.Definition.Presentation;
            TriggerPriority = state.Definition.TriggerPriority;
            NextReadyTime = state.NextReadyTime;
        }
    }

    public readonly struct SynergyExecutionSnapshot
    {
        public long ExecutionId { get; }
        public string SynergyId { get; }
        public SynergyTier Tier { get; }
        public long TriggerId { get; }
        public string CasterUnitId { get; }
        public SynergyCasterPresentation Presentation { get; }
        public SynergyExecutionPhase Phase { get; }
        public int ControlledEntityId { get; }
        public SynergyControlResolution ControlResolution { get; }

        internal SynergyExecutionSnapshot(ExecutionState state)
        {
            ExecutionId = state.ExecutionId;
            SynergyId = state.SynergyId;
            Tier = state.Tier;
            TriggerId = state.TriggerId;
            CasterUnitId = state.CasterUnitId;
            Presentation = state.Presentation;
            Phase = state.Phase;
            ControlledEntityId = state.ControlledEntityId;
            ControlResolution = state.ControlResolution;
        }
    }

    public readonly struct SynergyRuntimeSnapshot
    {
        private readonly SynergyStateSnapshot[] _synergies;
        private readonly SynergyExecutionSnapshot[] _executions;

        public bool IsEnabled { get; }
        public float ElapsedSeconds { get; }
        public int PendingCount { get; }
        public int ActiveExecutionCount { get; }
        public int ActivePairExecutionCount { get; }
        public int ActiveTrioExecutionCount { get; }
        public int SynergyCount => _synergies == null ? 0 : _synergies.Length;
        public int ExecutionCount => _executions == null ? 0 : _executions.Length;
        internal ulong StateDigest { get; }

        internal SynergyRuntimeSnapshot(
            bool isEnabled,
            float elapsedSeconds,
            int pendingCount,
            int activeExecutionCount,
            int activePairExecutionCount,
            int activeTrioExecutionCount,
            SynergyStateSnapshot[] synergies,
            SynergyExecutionSnapshot[] executions,
            ulong stateDigest)
        {
            IsEnabled = isEnabled;
            ElapsedSeconds = elapsedSeconds;
            PendingCount = pendingCount;
            ActiveExecutionCount = activeExecutionCount;
            ActivePairExecutionCount = activePairExecutionCount;
            ActiveTrioExecutionCount = activeTrioExecutionCount;
            _synergies = synergies;
            _executions = executions;
            StateDigest = stateDigest;
        }

        public SynergyStateSnapshot GetSynergy(string synergyId)
        {
            if (_synergies != null)
            {
                for (int index = 0; index < _synergies.Length; index++)
                {
                    if (string.Equals(_synergies[index].SynergyId, synergyId, StringComparison.Ordinal))
                        return _synergies[index];
                }
            }
            throw new ArgumentOutOfRangeException(nameof(synergyId));
        }

        public SynergyStateSnapshot GetSynergyAt(int index)
        {
            if (_synergies == null || index < 0 || index >= _synergies.Length)
                throw new ArgumentOutOfRangeException(nameof(index));

            return _synergies[index];
        }

        public SynergyExecutionSnapshot GetExecution(long executionId)
        {
            if (_executions != null)
            {
                for (int index = 0; index < _executions.Length; index++)
                {
                    if (_executions[index].ExecutionId == executionId)
                        return _executions[index];
                }
            }
            throw new ArgumentOutOfRangeException(nameof(executionId));
        }
    }

    public sealed class SynergyRuntime
    {
        private readonly SynergyRuntimeDefinition _definition;
        private readonly SynergyState[] _synergies;
        private readonly Dictionary<string, int> _progressions = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly List<PendingTrigger> _pending = new List<PendingTrigger>(20);
        private readonly List<ExecutionState> _executions = new List<ExecutionState>(20);
        private readonly HashSet<TriggerKey> _seenTriggers = new HashSet<TriggerKey>();
        private float _elapsedSeconds;
        private int _activeExecutionCount;
        private int _activePairExecutionCount;
        private int _activeTrioExecutionCount;
        private long _nextExecutionId;

        public SynergyRuntime(SynergyRuntimeDefinition definition)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
            _synergies = new SynergyState[definition.Synergies.Count];
            for (int index = 0; index < _synergies.Length; index++)
                _synergies[index] = new SynergyState(definition.Synergies[index]);
        }

        public void SetLegionProgression(string legionId, int progression)
        {
            if (string.IsNullOrWhiteSpace(legionId))
                throw new ArgumentException("Legion id is required.", nameof(legionId));
            if (progression < 0 || progression > 3)
                throw new ArgumentOutOfRangeException(nameof(progression));

            _progressions[legionId] = progression;
            for (int index = 0; index < _synergies.Length; index++)
            {
                SynergyState state = _synergies[index];
                if (state.IsActive == false && Qualifies(state.Definition))
                {
                    state.IsActive = true;
                    _synergies[index] = state;
                }
            }
        }

        public void Advance(float deltaSeconds)
        {
            if (float.IsNaN(deltaSeconds) || float.IsInfinity(deltaSeconds) || deltaSeconds < 0.0f)
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
            _elapsedSeconds += deltaSeconds;
        }

        public bool TryQueue(string synergyId, long triggerId)
        {
            if (_definition.IsEnabled == false || triggerId <= 0L)
                return false;
            int index = FindSynergy(synergyId);
            if (index < 0 || _synergies[index].IsActive == false || _elapsedSeconds < _synergies[index].NextReadyTime)
                return false;
            for (int pendingIndex = 0; pendingIndex < _pending.Count; pendingIndex++)
            {
                if (_pending[pendingIndex].SynergyIndex == index)
                    return false;
            }

            TriggerKey key = new TriggerKey(index, triggerId);
            if (_seenTriggers.Add(key) == false)
                return false;
            _pending.Add(new PendingTrigger(index, triggerId));
            return true;
        }

        public bool TryStartNext(out SynergyExecutionSnapshot execution)
        {
            int pendingIndex = FindFirstStartablePending();
            if (pendingIndex < 0)
            {
                execution = default;
                return false;
            }

            return StartPending(pendingIndex, out execution);
        }

        public bool TryStartNext(SynergyTier tier, out SynergyExecutionSnapshot execution)
        {
            if (tier != SynergyTier.Pair && tier != SynergyTier.Trio)
                throw new ArgumentOutOfRangeException(nameof(tier));
            int pendingIndex = FindFirstStartablePending(tier);
            if (pendingIndex < 0)
            {
                execution = default;
                return false;
            }

            return StartPending(pendingIndex, out execution);
        }

        public bool TryStartQueued(string synergyId, out SynergyExecutionSnapshot execution)
        {
            int synergyIndex = FindSynergy(synergyId);
            if (synergyIndex < 0 || CanStart(_synergies[synergyIndex].Definition.Tier) == false)
            {
                execution = default;
                return false;
            }
            for (int pendingIndex = 0; pendingIndex < _pending.Count; pendingIndex++)
            {
                if (_pending[pendingIndex].SynergyIndex == synergyIndex)
                    return StartPending(pendingIndex, out execution);
            }
            execution = default;
            return false;
        }

        private bool StartPending(int pendingIndex, out SynergyExecutionSnapshot execution)
        {
            PendingTrigger pending = _pending[pendingIndex];
            _pending.RemoveAt(pendingIndex);
            SynergyState synergy = _synergies[pending.SynergyIndex];
            synergy.NextReadyTime = _elapsedSeconds + synergy.Definition.CooldownSeconds;
            _synergies[pending.SynergyIndex] = synergy;

            ExecutionState state = new ExecutionState(
                ++_nextExecutionId,
                synergy.Definition.SynergyId,
                synergy.Definition.Tier,
                pending.TriggerId,
                synergy.Definition.CasterUnitId,
                synergy.Definition.Presentation);
            _executions.Add(state);
            _activeExecutionCount++;
            if (state.Tier == SynergyTier.Pair)
                _activePairExecutionCount++;
            else
                _activeTrioExecutionCount++;
            execution = new SynergyExecutionSnapshot(state);
            return true;
        }

        public bool WaitForMovementEnd(long executionId, int entityId)
        {
            if (entityId <= 0)
                return false;
            int index = FindExecution(executionId);
            if (index < 0 || _executions[index].Phase != SynergyExecutionPhase.Executing)
                return false;

            ExecutionState state = _executions[index];
            state.ControlledEntityId = entityId;
            state.Phase = SynergyExecutionPhase.WaitingForMovement;
            _executions[index] = state;
            return true;
        }

        public bool ResolveMovementEnded(int entityId)
        {
            for (int index = 0; index < _executions.Count; index++)
            {
                ExecutionState state = _executions[index];
                if (state.Phase != SynergyExecutionPhase.WaitingForMovement || state.ControlledEntityId != entityId)
                    continue;
                state.Phase = SynergyExecutionPhase.FollowupReady;
                state.ControlResolution = SynergyControlResolution.MovementEnded;
                _executions[index] = state;
                return true;
            }
            return false;
        }

        public bool ResolveMovementImmune(long executionId, int entityId)
        {
            int index = FindExecution(executionId);
            if (index < 0)
                return false;
            ExecutionState state = _executions[index];
            if (state.Phase != SynergyExecutionPhase.WaitingForMovement || state.ControlledEntityId != entityId)
                return false;
            state.Phase = SynergyExecutionPhase.FollowupReady;
            state.ControlResolution = SynergyControlResolution.Immune;
            _executions[index] = state;
            return true;
        }

        public bool CompleteExecution(long executionId)
        {
            int index = FindExecution(executionId);
            if (index < 0 || _executions[index].Phase == SynergyExecutionPhase.Complete)
                return false;
            ExecutionState state = _executions[index];
            state.Phase = SynergyExecutionPhase.Complete;
            _executions[index] = state;
            _activeExecutionCount--;
            if (state.Tier == SynergyTier.Pair)
                _activePairExecutionCount--;
            else
                _activeTrioExecutionCount--;
            return true;
        }

        public SynergyRuntimeSnapshot CreateSnapshot()
        {
            SynergyStateSnapshot[] synergies = new SynergyStateSnapshot[_synergies.Length];
            for (int index = 0; index < synergies.Length; index++)
                synergies[index] = new SynergyStateSnapshot(_synergies[index]);
            SynergyExecutionSnapshot[] executions = new SynergyExecutionSnapshot[_executions.Count];
            for (int index = 0; index < executions.Length; index++)
                executions[index] = new SynergyExecutionSnapshot(_executions[index]);

            ulong digest = 14695981039346656037UL;
            AddDigest(ref digest, unchecked((ulong)(uint)BitConverter.SingleToInt32Bits(_elapsedSeconds)));
            AddDigest(ref digest, (ulong)(uint)_pending.Count);
            AddDigest(ref digest, (ulong)(uint)_activeExecutionCount);
            AddDigest(ref digest, (ulong)(uint)_activePairExecutionCount);
            AddDigest(ref digest, (ulong)(uint)_activeTrioExecutionCount);
            for (int index = 0; index < _synergies.Length; index++)
            {
                AddStringDigest(ref digest, _synergies[index].Definition.SynergyId);
                AddDigest(ref digest, _synergies[index].IsActive ? 1UL : 0UL);
                AddDigest(ref digest, unchecked((ulong)(uint)BitConverter.SingleToInt32Bits(_synergies[index].NextReadyTime)));
            }
            for (int index = 0; index < _executions.Count; index++)
            {
                AddDigest(ref digest, unchecked((ulong)_executions[index].ExecutionId));
                AddDigest(ref digest, (ulong)(uint)_executions[index].Phase);
                AddDigest(ref digest, (ulong)(uint)_executions[index].ControlResolution);
            }

            return new SynergyRuntimeSnapshot(
                _definition.IsEnabled,
                _elapsedSeconds,
                _pending.Count,
                _activeExecutionCount,
                _activePairExecutionCount,
                _activeTrioExecutionCount,
                synergies,
                executions,
                digest);
        }

        private bool Qualifies(SynergyDefinition definition)
        {
            for (int index = 0; index < definition.RequiredLegionIds.Count; index++)
            {
                if (_progressions.TryGetValue(definition.RequiredLegionIds[index], out int progression) == false || progression <= 0)
                    return false;
            }
            return true;
        }

        private int FindSynergy(string synergyId)
        {
            if (string.IsNullOrEmpty(synergyId))
                return -1;
            for (int index = 0; index < _synergies.Length; index++)
            {
                if (string.Equals(_synergies[index].Definition.SynergyId, synergyId, StringComparison.Ordinal))
                    return index;
            }
            return -1;
        }

        private int FindExecution(long executionId)
        {
            for (int index = 0; index < _executions.Count; index++)
            {
                if (_executions[index].ExecutionId == executionId)
                    return index;
            }
            return -1;
        }

        private int FindFirstStartablePending()
        {
            int selected = -1;
            for (int index = 0; index < _pending.Count; index++)
            {
                SynergyDefinition definition = _synergies[_pending[index].SynergyIndex].Definition;
                if (CanStart(definition.Tier) == false)
                    continue;
                if (selected < 0 || definition.TriggerPriority >
                    _synergies[_pending[selected].SynergyIndex].Definition.TriggerPriority)
                {
                    selected = index;
                }
            }
            return selected;
        }

        private int FindFirstStartablePending(SynergyTier tier)
        {
            if (CanStart(tier) == false)
                return -1;
            int selected = -1;
            for (int index = 0; index < _pending.Count; index++)
            {
                SynergyDefinition definition = _synergies[_pending[index].SynergyIndex].Definition;
                if (definition.Tier != tier)
                    continue;
                if (selected < 0 || definition.TriggerPriority >
                    _synergies[_pending[selected].SynergyIndex].Definition.TriggerPriority)
                {
                    selected = index;
                }
            }
            return selected;
        }

        private bool CanStart(SynergyTier tier)
        {
            if (tier == SynergyTier.Pair)
                return _activePairExecutionCount < _definition.MaxConcurrentPairExecutions;
            return _activeTrioExecutionCount < _definition.MaxConcurrentTrioExecutions;
        }

        private static void AddStringDigest(ref ulong value, string part)
        {
            if (part == null)
            {
                AddDigest(ref value, 0UL);
                return;
            }
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

    internal struct SynergyState
    {
        internal SynergyDefinition Definition { get; }
        internal bool IsActive { get; set; }
        internal float NextReadyTime { get; set; }

        internal SynergyState(SynergyDefinition definition)
        {
            Definition = definition;
            IsActive = false;
            NextReadyTime = 0.0f;
        }
    }

    internal struct ExecutionState
    {
        internal long ExecutionId { get; }
        internal string SynergyId { get; }
        internal SynergyTier Tier { get; }
        internal long TriggerId { get; }
        internal string CasterUnitId { get; }
        internal SynergyCasterPresentation Presentation { get; }
        internal SynergyExecutionPhase Phase { get; set; }
        internal int ControlledEntityId { get; set; }
        internal SynergyControlResolution ControlResolution { get; set; }

        internal ExecutionState(
            long executionId,
            string synergyId,
            SynergyTier tier,
            long triggerId,
            string casterUnitId,
            SynergyCasterPresentation presentation)
        {
            ExecutionId = executionId;
            SynergyId = synergyId;
            Tier = tier;
            TriggerId = triggerId;
            CasterUnitId = casterUnitId;
            Presentation = presentation;
            Phase = SynergyExecutionPhase.Executing;
            ControlledEntityId = 0;
            ControlResolution = SynergyControlResolution.None;
        }
    }

    internal readonly struct PendingTrigger
    {
        internal int SynergyIndex { get; }
        internal long TriggerId { get; }

        internal PendingTrigger(int synergyIndex, long triggerId)
        {
            SynergyIndex = synergyIndex;
            TriggerId = triggerId;
        }
    }

    internal readonly struct TriggerKey : IEquatable<TriggerKey>
    {
        private readonly int _synergyIndex;
        private readonly long _triggerId;

        internal TriggerKey(int synergyIndex, long triggerId)
        {
            _synergyIndex = synergyIndex;
            _triggerId = triggerId;
        }

        public bool Equals(TriggerKey other)
        {
            return _synergyIndex == other._synergyIndex && _triggerId == other._triggerId;
        }

        public override bool Equals(object obj)
        {
            return obj is TriggerKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (_synergyIndex * 397) ^ _triggerId.GetHashCode();
            }
        }
    }
}
