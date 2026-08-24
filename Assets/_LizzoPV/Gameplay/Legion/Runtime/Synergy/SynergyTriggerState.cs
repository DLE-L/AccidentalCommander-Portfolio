using System;
using Lizzo.PV.Gameplay.RunTraits;

namespace Lizzo.PV.Legion.Synergy
{
    public enum SynergyTriggerKind
    {
        Immediate,
        Timed,
        MagicCast,
        ExplosionKills,
        UndeadKills,
        Healing,
    }

    public enum SynergyDeathSourceCategory
    {
        Unknown,
        Commander,
        Companion,
        Synergy,
        PersonalSummon,
        SynergySummon,
        OwnedSupport,
        Enemy,
    }

    public readonly struct SynergyTriggerPayload
    {
        public SynergyTriggerPayload(string synergyId, SynergyTriggerKind kind, float scheduledTime, string originId, long resolutionScopeId, int frameId)
        {
            SynergyId = synergyId;
            Kind = kind;
            ScheduledTime = scheduledTime;
            OriginId = originId;
            ResolutionScopeId = resolutionScopeId;
            FrameId = frameId;
        }

        public string SynergyId { get; }
        public SynergyTriggerKind Kind { get; }
        public float ScheduledTime { get; }
        public string OriginId { get; }
        public long ResolutionScopeId { get; }
        public int FrameId { get; }
    }

    public readonly struct SynergyMagicCastEvent
    {
        public SynergyMagicCastEvent(string castId, bool isMagicFamilySquad, bool isBasicOrActiveSkill, bool isCastComplete, bool isExcludedAction)
        {
            CastId = castId;
            NumericCastId = 0L;
            HasNumericCastId = false;
            IsMagicFamilySquad = isMagicFamilySquad;
            IsBasicOrActiveSkill = isBasicOrActiveSkill;
            IsCastComplete = isCastComplete;
            IsExcludedAction = isExcludedAction;
        }

        public string CastId { get; }
        public long NumericCastId { get; }
        public bool HasNumericCastId { get; }
        public bool IsMagicFamilySquad { get; }
        public bool IsBasicOrActiveSkill { get; }
        public bool IsCastComplete { get; }
        public bool IsExcludedAction { get; }

        public SynergyMagicCastEvent(long castId, bool isMagicFamilySquad, bool isBasicOrActiveSkill, bool isCastComplete, bool isExcludedAction)
        {
            CastId = null;
            NumericCastId = castId;
            HasNumericCastId = castId > 0L;
            IsMagicFamilySquad = isMagicFamilySquad;
            IsBasicOrActiveSkill = isBasicOrActiveSkill;
            IsCastComplete = isCastComplete;
            IsExcludedAction = isExcludedAction;
        }
    }

    public readonly struct SynergyEnemyDeathEvent
    {
        public SynergyEnemyDeathEvent(
            string lifeInstanceId,
            SynergyDeathSourceCategory sourceCategory,
            bool isTrainingDummy,
            bool isSummonObject,
            bool isSynergyExplosion,
            long resolutionScopeId,
            int frameId)
        {
            LifeInstanceId = lifeInstanceId;
            SourceCategory = sourceCategory;
            IsTrainingDummy = isTrainingDummy;
            IsSummonObject = isSummonObject;
            IsSynergyExplosion = isSynergyExplosion;
            ResolutionScopeId = resolutionScopeId;
            FrameId = frameId;
            NumericLifeInstanceId = 0L;
            HasNumericLifeInstanceId = false;
        }

        public SynergyEnemyDeathEvent(
            long lifeInstanceId,
            SynergyDeathSourceCategory sourceCategory,
            bool isTrainingDummy,
            bool isSummonObject,
            bool isSynergyExplosion,
            long resolutionScopeId,
            int frameId)
        {
            LifeInstanceId = null;
            NumericLifeInstanceId = lifeInstanceId;
            HasNumericLifeInstanceId = lifeInstanceId > 0L;
            SourceCategory = sourceCategory;
            IsTrainingDummy = isTrainingDummy;
            IsSummonObject = isSummonObject;
            IsSynergyExplosion = isSynergyExplosion;
            ResolutionScopeId = resolutionScopeId;
            FrameId = frameId;
        }

        public string LifeInstanceId { get; }
        public long NumericLifeInstanceId { get; }
        public bool HasNumericLifeInstanceId { get; }
        public SynergyDeathSourceCategory SourceCategory { get; }
        public bool IsTrainingDummy { get; }
        public bool IsSummonObject { get; }
        public bool IsSynergyExplosion { get; }
        public long ResolutionScopeId { get; }
        public int FrameId { get; }
    }

    public readonly struct SynergyHealingEvent
    {
        public SynergyHealingEvent(bool isHealingSkillTag, int effectiveHealAmount, bool isOverheal, bool isShield, bool isReviveRestore)
        {
            IsHealingSkillTag = isHealingSkillTag;
            EffectiveHealAmount = effectiveHealAmount;
            IsOverheal = isOverheal;
            IsShield = isShield;
            IsReviveRestore = isReviveRestore;
        }

        public bool IsHealingSkillTag { get; }
        public int EffectiveHealAmount { get; }
        public bool IsOverheal { get; }
        public bool IsShield { get; }
        public bool IsReviveRestore { get; }
    }

    internal sealed class SynergyTriggerDeduplicationState
    {
        const int Capacity = 512;

        readonly string[] _magicCastIds = new string[Capacity];
        readonly long[] _magicNumericCastIds = new long[Capacity];
        readonly string[] _enemyLifeInstanceIds = new string[Capacity];
        readonly long[] _enemyNumericLifeInstanceIds = new long[Capacity];

        int _magicCastIdCount;
        int _magicNumericCastIdCount;
        int _enemyLifeInstanceIdCount;
        int _enemyNumericLifeInstanceIdCount;

        internal bool TryRegisterMagic(in SynergyMagicCastEvent castEvent)
        {
            return castEvent.HasNumericCastId
                ? TryRegisterUnique(_magicNumericCastIds, ref _magicNumericCastIdCount, castEvent.NumericCastId)
                : TryRegisterUnique(_magicCastIds, ref _magicCastIdCount, castEvent.CastId);
        }

        internal bool TryRegisterEnemy(in SynergyEnemyDeathEvent deathEvent)
        {
            return deathEvent.HasNumericLifeInstanceId
                ? TryRegisterUnique(_enemyNumericLifeInstanceIds, ref _enemyNumericLifeInstanceIdCount, deathEvent.NumericLifeInstanceId)
                : TryRegisterUnique(_enemyLifeInstanceIds, ref _enemyLifeInstanceIdCount, deathEvent.LifeInstanceId);
        }

        internal void Reset()
        {
            Array.Clear(_magicCastIds, 0, _magicCastIds.Length);
            Array.Clear(_magicNumericCastIds, 0, _magicNumericCastIds.Length);
            Array.Clear(_enemyLifeInstanceIds, 0, _enemyLifeInstanceIds.Length);
            Array.Clear(_enemyNumericLifeInstanceIds, 0, _enemyNumericLifeInstanceIds.Length);
            _magicCastIdCount = 0;
            _magicNumericCastIdCount = 0;
            _enemyLifeInstanceIdCount = 0;
            _enemyNumericLifeInstanceIdCount = 0;
        }

        static bool TryRegisterUnique(string[] values, ref int count, string id)
        {
            if (string.IsNullOrEmpty(id))
                return false;

            for (int index = 0; index < count; index++)
                if (values[index] == id)
                    return false;

            if (count >= values.Length)
                return false;

            values[count++] = id;
            return true;
        }

        static bool TryRegisterUnique(long[] values, ref int count, long id)
        {
            if (id <= 0L)
                return false;

            for (int index = 0; index < count; index++)
                if (values[index] == id)
                    return false;

            if (count >= values.Length)
                return false;

            values[count++] = id;
            return true;
        }
    }

    internal sealed class SynergyPendingTriggerQueue
    {
        readonly byte[] _executionCredits = new byte[SynergyTriggerCatalog.Count];
        readonly SynergyTriggerPayload[] _payloads = new SynergyTriggerPayload[SynergyTriggerCatalog.Count];
        readonly float[] _nextTimedDue = new float[SynergyTriggerCatalog.Count];

        long _nextResolutionScopeId;

        internal int PendingCount { get; private set; }

        internal bool HasPending(int index)
        {
            return _executionCredits[index] > 0;
        }

        internal bool IsTimedDue(int index, float clock)
        {
            return _nextTimedDue[index] > 0.0f && clock >= _nextTimedDue[index];
        }

        internal bool TryConsume(
            int index,
            float clock,
            float timedPeriod,
            out SynergyTriggerPayload payload)
        {
            if (_executionCredits[index] == 0)
            {
                payload = default;
                return false;
            }

            payload = _payloads[index];
            _executionCredits[index]--;
            if (_executionCredits[index] > 0)
                return true;

            PendingCount--;
            if (timedPeriod > 0.0f)
                _nextTimedDue[index] = clock + timedPeriod;
            return true;
        }

        internal bool TryQueue(
            int index,
            string synergyId,
            SynergyTriggerKind kind,
            float clock,
            string originId,
            int frameId,
            int executionCredits,
            out SynergyTriggerPayload payload)
        {
            if (_executionCredits[index] > 0)
            {
                payload = default;
                return false;
            }

            payload = new SynergyTriggerPayload(
                synergyId,
                kind,
                clock,
                originId,
                ++_nextResolutionScopeId,
                frameId);
            _executionCredits[index] = (byte)executionCredits;
            _payloads[index] = payload;
            PendingCount++;
            return true;
        }

        internal void Reset()
        {
            Array.Clear(_executionCredits, 0, _executionCredits.Length);
            Array.Clear(_payloads, 0, _payloads.Length);
            Array.Clear(_nextTimedDue, 0, _nextTimedDue.Length);
            _nextResolutionScopeId = 0L;
            PendingCount = 0;
        }
    }

    internal sealed class SynergyMagicTriggerCounter
    {
        const int Threshold = 3;

        int _count;

        internal int Count => _count;

        internal bool TryAdvance(
            in SynergyMagicCastEvent castEvent,
            bool isActive,
            bool hasPending,
            SynergyTriggerDeduplicationState deduplication)
        {
            if (isActive == false
                || castEvent.IsMagicFamilySquad == false
                || castEvent.IsBasicOrActiveSkill == false
                || castEvent.IsCastComplete == false
                || castEvent.IsExcludedAction
                || deduplication.TryRegisterMagic(in castEvent) == false)
            {
                return false;
            }

            _count++;
            if (_count < Threshold || hasPending)
                return false;

            _count = 0;
            return true;
        }

        internal void Reset()
        {
            _count = 0;
        }
    }

    internal static class SynergyTriggerCatalog
    {
        internal const int GuardIndex = 0;
        internal const int ArcherIndex = 1;
        internal const int MagicIndex = 2;
        internal const int ExplosionIndex = 3;
        internal const int BeastIndex = 4;
        internal const int UndeadIndex = 5;
        internal const int HealingIndex = 6;
        internal const int MixedIndex = 7;
        internal const int Count = 8;

        static readonly string[] SynergyIds =
        {
            SynergyActivationIds.GuardShockwave,
            SynergyActivationIds.ArcherRain,
            SynergyActivationIds.MagicChain,
            SynergyActivationIds.ExplosionChain,
            SynergyActivationIds.BeastHunt,
            SynergyActivationIds.UndeadSummon,
            SynergyActivationIds.HealingBond,
            SynergyActivationIds.MixedCommand,
        };

        static readonly float[] TimedPeriods =
        {
            12.0f,
            8.0f,
            0.0f,
            0.0f,
            10.0f,
            0.0f,
            0.0f,
            15.0f,
        };

        internal static string GetId(int index)
        {
            return SynergyIds[index];
        }

        internal static float GetTimedPeriod(int index)
        {
            return TimedPeriods[index];
        }

        internal static int FindIndex(string synergyId)
        {
            for (int index = 0; index < SynergyIds.Length; index++)
                if (SynergyIds[index] == synergyId)
                    return index;
            return -1;
        }
    }

    /// <summary>
    /// Run-owned common trigger lifecycle. It schedules typed rounds only; P10D owns all effect execution.
    /// </summary>
    public sealed class SynergyTriggerState : IDisposable
    {
        const int ExplosionThreshold = 8;
        const int UndeadThreshold = 15;

        readonly SynergyActivationState _activations;
        readonly SynergyTriggerDeduplicationState _deduplication = new SynergyTriggerDeduplicationState();
        readonly SynergyPendingTriggerQueue _pending = new SynergyPendingTriggerQueue();
        readonly SynergyMagicTriggerCounter _magic = new SynergyMagicTriggerCounter();
        readonly bool[] _activationSeen = new bool[SynergyTriggerCatalog.Count];
        readonly int[] _counters = new int[SynergyTriggerCatalog.Count];

        RunTraitEffectCoordinator _runTraitEffects;

        int _lastExplosionTriggerFrame = int.MinValue;
        int _lastUndeadTriggerFrame = int.MinValue;
        long _lastExplosionResolutionScopeId;
        float _clock;
        bool _undeadAliveCapFull;
        bool _disposed;

        public SynergyTriggerState(SynergyActivationState activations)
        {
            _activations = activations ?? throw new ArgumentNullException(nameof(activations));
            _activations.Activated += OnActivated;
            PrimeExistingActivations();
        }

        public event Action<SynergyTriggerPayload> TriggerReady;
        public int PendingCount => _pending.PendingCount;

        internal void BindRunTraitEffectCoordinator(RunTraitEffectCoordinator coordinator)
        {
            _runTraitEffects = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
        }

        internal void UnbindRunTraitEffectCoordinator(RunTraitEffectCoordinator coordinator)
        {
            if (ReferenceEquals(_runTraitEffects, coordinator))
                _runTraitEffects = null;
        }

        public bool HasPending(string synergyId)
        {
            int index = SynergyTriggerCatalog.FindIndex(synergyId);
            return index >= 0 && _pending.HasPending(index);
        }

        public int GetCounter(string synergyId)
        {
            int index = SynergyTriggerCatalog.FindIndex(synergyId);
            if (index < 0)
                return 0;
            return index == SynergyTriggerCatalog.MagicIndex ? _magic.Count : _counters[index];
        }

        public bool TryConsumePending(string synergyId, out SynergyTriggerPayload payload)
        {
            int index = SynergyTriggerCatalog.FindIndex(synergyId);
            if (index < 0
                || _pending.TryConsume(
                    index,
                    _clock,
                    SynergyTriggerCatalog.GetTimedPeriod(index),
                    out payload) == false)
            {
                payload = default;
                return false;
            }

            if (index == SynergyTriggerCatalog.ExplosionIndex)
                _lastExplosionResolutionScopeId = payload.ResolutionScopeId;
            return true;
        }

        public void Tick(float deltaSeconds, bool runReady, bool paused, int frameId)
        {
            if (_disposed || runReady == false || paused || deltaSeconds <= 0.0f)
                return;

            _clock += deltaSeconds;
            ScheduleTimed(SynergyTriggerCatalog.GuardIndex, frameId);
            ScheduleTimed(SynergyTriggerCatalog.ArcherIndex, frameId);
            ScheduleTimed(SynergyTriggerCatalog.BeastIndex, frameId);
            ScheduleTimed(SynergyTriggerCatalog.MixedIndex, frameId);
            ScheduleUndeadOverflow(frameId);
        }

        public bool ReportMagicCast(in SynergyMagicCastEvent castEvent)
        {
            if (_magic.TryAdvance(
                in castEvent,
                IsActive(SynergyTriggerCatalog.MagicIndex),
                _pending.HasPending(SynergyTriggerCatalog.MagicIndex),
                _deduplication) == false)
            {
                return false;
            }

            return Queue(SynergyTriggerCatalog.MagicIndex, SynergyTriggerKind.MagicCast, castEvent.CastId, -1);
        }

        public bool ReportEnemyDeath(in SynergyEnemyDeathEvent deathEvent)
        {
            bool uniqueLifeInstance = _deduplication.TryRegisterEnemy(in deathEvent);
            if (IsEligibleCountableDeath(deathEvent) == false || uniqueLifeInstance == false)
            {
                return false;
            }

            bool queued = false;
            if (IsActive(SynergyTriggerCatalog.ExplosionIndex))
            {
                bool blockedByResolutionScope = deathEvent.IsSynergyExplosion
                    && deathEvent.ResolutionScopeId != 0L
                    && deathEvent.ResolutionScopeId == _lastExplosionResolutionScopeId;
                if (blockedByResolutionScope == false)
                {
                    _counters[SynergyTriggerCatalog.ExplosionIndex] += _runTraitEffects == null
                        ? 1
                        : _runTraitEffects.GetExplosionKillCounterIncrement();
                    queued |= ScheduleExplosion(deathEvent);
                }
            }

            if (IsActive(SynergyTriggerCatalog.UndeadIndex))
            {
                if (_undeadAliveCapFull)
                {
                    int increment = _runTraitEffects == null
                        ? 1
                        : _runTraitEffects.GetUndeadKillCounterIncrement();
                    _counters[SynergyTriggerCatalog.UndeadIndex] = Math.Min(UndeadThreshold - 1, _counters[SynergyTriggerCatalog.UndeadIndex] + increment);
                }
                else
                {
                    _counters[SynergyTriggerCatalog.UndeadIndex] += _runTraitEffects == null
                        ? 1
                        : _runTraitEffects.GetUndeadKillCounterIncrement();
                    queued |= ScheduleUndead(deathEvent);
                }
            }

            return queued;
        }

        public bool ReportHealing(in SynergyHealingEvent healingEvent)
        {
            if (IsActive(SynergyTriggerCatalog.HealingIndex) == false
                || healingEvent.IsHealingSkillTag == false
                || healingEvent.EffectiveHealAmount < 1
                || healingEvent.IsOverheal
                || healingEvent.IsShield
                || healingEvent.IsReviveRestore)
            {
                return false;
            }

            return Queue(SynergyTriggerCatalog.HealingIndex, SynergyTriggerKind.Healing, null, -1);
        }

        public void SetUndeadAliveCapFull(bool isFull)
        {
            _undeadAliveCapFull = isFull;
            if (isFull && _counters[SynergyTriggerCatalog.UndeadIndex] >= UndeadThreshold)
                _counters[SynergyTriggerCatalog.UndeadIndex] = UndeadThreshold - 1;
        }

        public void Reset()
        {
            Array.Clear(_activationSeen, 0, _activationSeen.Length);
            _pending.Reset();
            Array.Clear(_counters, 0, _counters.Length);
            _deduplication.Reset();
            _magic.Reset();
            _lastExplosionTriggerFrame = int.MinValue;
            _lastUndeadTriggerFrame = int.MinValue;
            _lastExplosionResolutionScopeId = 0L;
            _clock = 0.0f;
            _undeadAliveCapFull = false;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _activations.Activated -= OnActivated;
            TriggerReady = null;
            Reset();
        }

        void OnActivated(SynergyActivationSnapshot snapshot)
        {
            int index = SynergyTriggerCatalog.FindIndex(snapshot.SynergyId);
            if (index >= 0 && snapshot.IsActive)
                ObserveActivation(index);
        }

        void PrimeExistingActivations()
        {
            for (int i = 0; i < _activations.Snapshot.Count; i++)
            {
                SynergyActivationSnapshot snapshot = _activations.Snapshot[i];
                if (snapshot.IsActive)
                    ObserveActivation(i);
            }
        }

        void ObserveActivation(int index)
        {
            if (_activationSeen[index])
                return;

            _activationSeen[index] = true;
            int executionCredits = _runTraitEffects == null
                ? 1
                : _runTraitEffects.GetFirstSynergyActivationExecutionCreditCount(SynergyTriggerCatalog.GetId(index));
            Queue(index, SynergyTriggerKind.Immediate, null, -1, executionCredits);
        }

        void ScheduleTimed(int index, int frameId)
        {
            if (IsActive(index) == false || _pending.HasPending(index) || _pending.IsTimedDue(index, _clock) == false)
                return;

            Queue(index, SynergyTriggerKind.Timed, null, frameId);
        }

        bool ScheduleExplosion(in SynergyEnemyDeathEvent deathEvent)
        {
            if (_counters[SynergyTriggerCatalog.ExplosionIndex] < ExplosionThreshold || _pending.HasPending(SynergyTriggerCatalog.ExplosionIndex) || _lastExplosionTriggerFrame == deathEvent.FrameId)
                return false;

            _counters[SynergyTriggerCatalog.ExplosionIndex] -= ExplosionThreshold;
            _lastExplosionTriggerFrame = deathEvent.FrameId;
            return Queue(SynergyTriggerCatalog.ExplosionIndex, SynergyTriggerKind.ExplosionKills, deathEvent.LifeInstanceId, deathEvent.FrameId);
        }

        bool ScheduleUndead(in SynergyEnemyDeathEvent deathEvent)
        {
            if (_counters[SynergyTriggerCatalog.UndeadIndex] < UndeadThreshold || _pending.HasPending(SynergyTriggerCatalog.UndeadIndex) || _lastUndeadTriggerFrame == deathEvent.FrameId)
                return false;

            _counters[SynergyTriggerCatalog.UndeadIndex] -= UndeadThreshold;
            _lastUndeadTriggerFrame = deathEvent.FrameId;
            return Queue(SynergyTriggerCatalog.UndeadIndex, SynergyTriggerKind.UndeadKills, deathEvent.LifeInstanceId, deathEvent.FrameId);
        }

        void ScheduleUndeadOverflow(int frameId)
        {
            if (IsActive(SynergyTriggerCatalog.UndeadIndex) == false
                || _undeadAliveCapFull
                || _counters[SynergyTriggerCatalog.UndeadIndex] < UndeadThreshold
                || _pending.HasPending(SynergyTriggerCatalog.UndeadIndex)
                || _lastUndeadTriggerFrame == frameId)
            {
                return;
            }

            _counters[SynergyTriggerCatalog.UndeadIndex] -= UndeadThreshold;
            _lastUndeadTriggerFrame = frameId;
            Queue(SynergyTriggerCatalog.UndeadIndex, SynergyTriggerKind.UndeadKills, null, frameId);
        }

        bool Queue(int index, SynergyTriggerKind kind, string originId, int frameId, int executionCredits = 1)
        {
            if (_pending.TryQueue(
                index,
                SynergyTriggerCatalog.GetId(index),
                kind,
                _clock,
                originId,
                frameId,
                executionCredits,
                out SynergyTriggerPayload payload) == false)
            {
                return false;
            }

            TriggerReady?.Invoke(payload);
            return true;
        }

        bool IsActive(int index)
        {
            return _activations.IsActive(SynergyTriggerCatalog.GetId(index));
        }

        static bool IsEligibleCountableDeath(in SynergyEnemyDeathEvent deathEvent)
        {
            if ((deathEvent.HasNumericLifeInstanceId == false && string.IsNullOrEmpty(deathEvent.LifeInstanceId))
                || deathEvent.IsTrainingDummy
                || deathEvent.IsSummonObject)
                return false;

            return deathEvent.SourceCategory == SynergyDeathSourceCategory.Commander
                || deathEvent.SourceCategory == SynergyDeathSourceCategory.Companion
                || deathEvent.SourceCategory == SynergyDeathSourceCategory.Synergy;
        }

    }
}
