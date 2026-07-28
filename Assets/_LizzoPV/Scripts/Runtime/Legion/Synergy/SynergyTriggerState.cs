using System;

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

    /// <summary>
    /// Run-owned common trigger lifecycle. It schedules typed rounds only; P10D owns all effect execution.
    /// </summary>
    public sealed class SynergyTriggerState : IDisposable
    {
        const int GuardIndex = 0;
        const int ArcherIndex = 1;
        const int MagicIndex = 2;
        const int ExplosionIndex = 3;
        const int BeastIndex = 4;
        const int UndeadIndex = 5;
        const int HealingIndex = 6;
        const int MixedIndex = 7;
        const int MagicThreshold = 3;
        const int ExplosionThreshold = 8;
        const int UndeadThreshold = 15;
        const int DeduplicationCapacity = 512;

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

        readonly SynergyActivationState _activations;
        readonly bool[] _activationSeen = new bool[SynergyIds.Length];
        readonly bool[] _pending = new bool[SynergyIds.Length];
        readonly SynergyTriggerPayload[] _pendingPayloads = new SynergyTriggerPayload[SynergyIds.Length];
        readonly float[] _nextTimedDue = new float[SynergyIds.Length];
        readonly int[] _counters = new int[SynergyIds.Length];
        readonly string[] _magicCastIds = new string[DeduplicationCapacity];
        readonly long[] _magicNumericCastIds = new long[DeduplicationCapacity];
        readonly string[] _enemyLifeInstanceIds = new string[DeduplicationCapacity];
        readonly long[] _enemyNumericLifeInstanceIds = new long[DeduplicationCapacity];

        int _magicCastIdCount;
        int _magicNumericCastIdCount;
        int _enemyLifeInstanceIdCount;
        int _enemyNumericLifeInstanceIdCount;
        int _lastExplosionTriggerFrame = int.MinValue;
        int _lastUndeadTriggerFrame = int.MinValue;
        long _lastExplosionResolutionScopeId;
        long _nextResolutionScopeId;
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
        public int PendingCount { get; private set; }

        public bool HasPending(string synergyId)
        {
            int index = FindSynergyIndex(synergyId);
            return index >= 0 && _pending[index];
        }

        public int GetCounter(string synergyId)
        {
            int index = FindSynergyIndex(synergyId);
            return index >= 0 ? _counters[index] : 0;
        }

        public bool TryConsumePending(string synergyId, out SynergyTriggerPayload payload)
        {
            int index = FindSynergyIndex(synergyId);
            if (index < 0 || _pending[index] == false)
            {
                payload = default;
                return false;
            }

            payload = _pendingPayloads[index];
            _pending[index] = false;
            PendingCount--;
            if (TimedPeriods[index] > 0.0f)
                _nextTimedDue[index] = _clock + TimedPeriods[index];

            if (index == ExplosionIndex)
                _lastExplosionResolutionScopeId = payload.ResolutionScopeId;

            return true;
        }

        public void Tick(float deltaSeconds, bool runReady, bool paused, int frameId)
        {
            if (_disposed || runReady == false || paused || deltaSeconds <= 0.0f)
                return;

            _clock += deltaSeconds;
            ScheduleTimed(GuardIndex, frameId);
            ScheduleTimed(ArcherIndex, frameId);
            ScheduleTimed(BeastIndex, frameId);
            ScheduleTimed(MixedIndex, frameId);
            ScheduleUndeadOverflow(frameId);
        }

        public bool ReportMagicCast(in SynergyMagicCastEvent castEvent)
        {
            if (IsActive(MagicIndex) == false
                || castEvent.IsMagicFamilySquad == false
                || castEvent.IsBasicOrActiveSkill == false
                || castEvent.IsCastComplete == false
                || castEvent.IsExcludedAction
                || (castEvent.HasNumericCastId
                    ? TryRegisterUnique(_magicNumericCastIds, ref _magicNumericCastIdCount, castEvent.NumericCastId)
                    : TryRegisterUnique(_magicCastIds, ref _magicCastIdCount, castEvent.CastId)) == false)
            {
                return false;
            }

            _counters[MagicIndex]++;
            if (_counters[MagicIndex] < MagicThreshold || _pending[MagicIndex])
                return false;

            _counters[MagicIndex] = 0;
            return Queue(MagicIndex, SynergyTriggerKind.MagicCast, castEvent.CastId, -1);
        }

        public bool ReportEnemyDeath(in SynergyEnemyDeathEvent deathEvent)
        {
            bool uniqueLifeInstance = deathEvent.HasNumericLifeInstanceId
                ? TryRegisterUnique(_enemyNumericLifeInstanceIds, ref _enemyNumericLifeInstanceIdCount, deathEvent.NumericLifeInstanceId)
                : TryRegisterUnique(_enemyLifeInstanceIds, ref _enemyLifeInstanceIdCount, deathEvent.LifeInstanceId);
            if (IsEligibleCountableDeath(deathEvent) == false || uniqueLifeInstance == false)
            {
                return false;
            }

            bool queued = false;
            if (IsActive(ExplosionIndex))
            {
                bool blockedByResolutionScope = deathEvent.IsSynergyExplosion
                    && deathEvent.ResolutionScopeId != 0L
                    && deathEvent.ResolutionScopeId == _lastExplosionResolutionScopeId;
                if (blockedByResolutionScope == false)
                {
                    _counters[ExplosionIndex]++;
                    queued |= ScheduleExplosion(deathEvent);
                }
            }

            if (IsActive(UndeadIndex))
            {
                if (_undeadAliveCapFull)
                {
                    _counters[UndeadIndex] = Math.Min(UndeadThreshold - 1, _counters[UndeadIndex] + 1);
                }
                else
                {
                    _counters[UndeadIndex]++;
                    queued |= ScheduleUndead(deathEvent);
                }
            }

            return queued;
        }

        public bool ReportHealing(in SynergyHealingEvent healingEvent)
        {
            if (IsActive(HealingIndex) == false
                || healingEvent.IsHealingSkillTag == false
                || healingEvent.EffectiveHealAmount < 1
                || healingEvent.IsOverheal
                || healingEvent.IsShield
                || healingEvent.IsReviveRestore)
            {
                return false;
            }

            return Queue(HealingIndex, SynergyTriggerKind.Healing, null, -1);
        }

        public void SetUndeadAliveCapFull(bool isFull)
        {
            _undeadAliveCapFull = isFull;
            if (isFull && _counters[UndeadIndex] >= UndeadThreshold)
                _counters[UndeadIndex] = UndeadThreshold - 1;
        }

        public void Reset()
        {
            Array.Clear(_activationSeen, 0, _activationSeen.Length);
            Array.Clear(_pending, 0, _pending.Length);
            Array.Clear(_pendingPayloads, 0, _pendingPayloads.Length);
            Array.Clear(_nextTimedDue, 0, _nextTimedDue.Length);
            Array.Clear(_counters, 0, _counters.Length);
            Array.Clear(_magicCastIds, 0, _magicCastIds.Length);
            Array.Clear(_magicNumericCastIds, 0, _magicNumericCastIds.Length);
            Array.Clear(_enemyLifeInstanceIds, 0, _enemyLifeInstanceIds.Length);
            Array.Clear(_enemyNumericLifeInstanceIds, 0, _enemyNumericLifeInstanceIds.Length);
            _magicCastIdCount = 0;
            _magicNumericCastIdCount = 0;
            _enemyLifeInstanceIdCount = 0;
            _enemyNumericLifeInstanceIdCount = 0;
            _lastExplosionTriggerFrame = int.MinValue;
            _lastUndeadTriggerFrame = int.MinValue;
            _lastExplosionResolutionScopeId = 0L;
            _nextResolutionScopeId = 0L;
            _clock = 0.0f;
            _undeadAliveCapFull = false;
            PendingCount = 0;
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
            int index = FindSynergyIndex(snapshot.SynergyId);
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
            Queue(index, SynergyTriggerKind.Immediate, null, -1);
        }

        void ScheduleTimed(int index, int frameId)
        {
            if (IsActive(index) == false || _pending[index] || _nextTimedDue[index] <= 0.0f || _clock < _nextTimedDue[index])
                return;

            Queue(index, SynergyTriggerKind.Timed, null, frameId);
        }

        bool ScheduleExplosion(in SynergyEnemyDeathEvent deathEvent)
        {
            if (_counters[ExplosionIndex] < ExplosionThreshold || _pending[ExplosionIndex] || _lastExplosionTriggerFrame == deathEvent.FrameId)
                return false;

            _counters[ExplosionIndex] -= ExplosionThreshold;
            _lastExplosionTriggerFrame = deathEvent.FrameId;
            return Queue(ExplosionIndex, SynergyTriggerKind.ExplosionKills, deathEvent.LifeInstanceId, deathEvent.FrameId);
        }

        bool ScheduleUndead(in SynergyEnemyDeathEvent deathEvent)
        {
            if (_counters[UndeadIndex] < UndeadThreshold || _pending[UndeadIndex] || _lastUndeadTriggerFrame == deathEvent.FrameId)
                return false;

            _counters[UndeadIndex] -= UndeadThreshold;
            _lastUndeadTriggerFrame = deathEvent.FrameId;
            return Queue(UndeadIndex, SynergyTriggerKind.UndeadKills, deathEvent.LifeInstanceId, deathEvent.FrameId);
        }

        void ScheduleUndeadOverflow(int frameId)
        {
            if (IsActive(UndeadIndex) == false
                || _undeadAliveCapFull
                || _counters[UndeadIndex] < UndeadThreshold
                || _pending[UndeadIndex]
                || _lastUndeadTriggerFrame == frameId)
            {
                return;
            }

            _counters[UndeadIndex] -= UndeadThreshold;
            _lastUndeadTriggerFrame = frameId;
            Queue(UndeadIndex, SynergyTriggerKind.UndeadKills, null, frameId);
        }

        bool Queue(int index, SynergyTriggerKind kind, string originId, int frameId)
        {
            if (_pending[index])
                return false;

            SynergyTriggerPayload payload = new SynergyTriggerPayload(
                SynergyIds[index],
                kind,
                _clock,
                originId,
                ++_nextResolutionScopeId,
                frameId);
            _pending[index] = true;
            _pendingPayloads[index] = payload;
            PendingCount++;
            TriggerReady?.Invoke(payload);
            return true;
        }

        bool IsActive(int index)
        {
            return _activations.IsActive(SynergyIds[index]);
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

        static bool TryRegisterUnique(string[] values, ref int count, string id)
        {
            if (string.IsNullOrEmpty(id))
                return false;

            for (int i = 0; i < count; i++)
            {
                if (values[i] == id)
                    return false;
            }

            if (count >= values.Length)
                return false;

            values[count++] = id;
            return true;
        }

        static bool TryRegisterUnique(long[] values, ref int count, long id)
        {
            if (id <= 0L)
                return false;

            for (int i = 0; i < count; i++)
            {
                if (values[i] == id)
                    return false;
            }

            if (count >= values.Length)
                return false;

            values[count++] = id;
            return true;
        }

        static int FindSynergyIndex(string synergyId)
        {
            for (int i = 0; i < SynergyIds.Length; i++)
            {
                if (SynergyIds[i] == synergyId)
                    return i;
            }

            return -1;
        }
    }
}
