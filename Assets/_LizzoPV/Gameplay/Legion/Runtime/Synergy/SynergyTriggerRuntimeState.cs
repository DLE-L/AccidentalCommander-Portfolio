using System;
using Lizzo.PV.Gameplay.RunTraits;

namespace Lizzo.PV.Legion.Synergy
{
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

    internal readonly struct SynergyDeathTriggerResult
    {
        internal SynergyDeathTriggerResult(bool queueExplosion, bool queueUndead)
        {
            QueueExplosion = queueExplosion;
            QueueUndead = queueUndead;
        }

        internal bool QueueExplosion { get; }
        internal bool QueueUndead { get; }
    }

    internal sealed class SynergyDeathTriggerCounter
    {
        const int ExplosionThreshold = 8;
        const int UndeadThreshold = 15;

        int _explosionCount;
        int _undeadCount;
        int _lastExplosionTriggerFrame = int.MinValue;
        int _lastUndeadTriggerFrame = int.MinValue;
        long _lastExplosionResolutionScopeId;
        bool _undeadAliveCapFull;

        internal int GetCount(int synergyIndex)
        {
            if (synergyIndex == SynergyTriggerCatalog.ExplosionIndex)
                return _explosionCount;
            return synergyIndex == SynergyTriggerCatalog.UndeadIndex ? _undeadCount : 0;
        }

        internal SynergyDeathTriggerResult Report(
            in SynergyEnemyDeathEvent deathEvent,
            bool explosionActive,
            bool undeadActive,
            bool explosionPending,
            bool undeadPending,
            SynergyTriggerDeduplicationState deduplication,
            RunTraitEffectCoordinator runTraitEffects)
        {
            bool uniqueLifeInstance = deduplication.TryRegisterEnemy(in deathEvent);
            if (IsEligibleCountableDeath(deathEvent) == false || uniqueLifeInstance == false)
                return default;

            bool queueExplosion = false;
            if (explosionActive)
            {
                bool blockedByResolutionScope = deathEvent.IsSynergyExplosion
                    && deathEvent.ResolutionScopeId != 0L
                    && deathEvent.ResolutionScopeId == _lastExplosionResolutionScopeId;
                if (blockedByResolutionScope == false)
                {
                    _explosionCount += runTraitEffects == null
                        ? 1
                        : runTraitEffects.GetExplosionKillCounterIncrement();
                    queueExplosion = TryScheduleExplosion(deathEvent.FrameId, explosionPending);
                }
            }

            bool queueUndead = false;
            if (undeadActive)
            {
                int increment = runTraitEffects == null
                    ? 1
                    : runTraitEffects.GetUndeadKillCounterIncrement();
                if (_undeadAliveCapFull)
                    _undeadCount = Math.Min(UndeadThreshold - 1, _undeadCount + increment);
                else
                {
                    _undeadCount += increment;
                    queueUndead = TryScheduleUndead(deathEvent.FrameId, undeadPending);
                }
            }

            return new SynergyDeathTriggerResult(queueExplosion, queueUndead);
        }

        internal bool TryScheduleUndeadOverflow(int frameId, bool isActive, bool hasPending)
        {
            if (isActive == false
                || _undeadAliveCapFull
                || _undeadCount < UndeadThreshold
                || hasPending
                || _lastUndeadTriggerFrame == frameId)
            {
                return false;
            }

            _undeadCount -= UndeadThreshold;
            _lastUndeadTriggerFrame = frameId;
            return true;
        }

        internal void ObserveExplosionConsumed(long resolutionScopeId)
        {
            _lastExplosionResolutionScopeId = resolutionScopeId;
        }

        internal void SetUndeadAliveCapFull(bool isFull)
        {
            _undeadAliveCapFull = isFull;
            if (isFull && _undeadCount >= UndeadThreshold)
                _undeadCount = UndeadThreshold - 1;
        }

        internal void Reset()
        {
            _explosionCount = 0;
            _undeadCount = 0;
            _lastExplosionTriggerFrame = int.MinValue;
            _lastUndeadTriggerFrame = int.MinValue;
            _lastExplosionResolutionScopeId = 0L;
            _undeadAliveCapFull = false;
        }

        bool TryScheduleExplosion(int frameId, bool hasPending)
        {
            if (_explosionCount < ExplosionThreshold || hasPending || _lastExplosionTriggerFrame == frameId)
                return false;

            _explosionCount -= ExplosionThreshold;
            _lastExplosionTriggerFrame = frameId;
            return true;
        }

        bool TryScheduleUndead(int frameId, bool hasPending)
        {
            if (_undeadCount < UndeadThreshold || hasPending || _lastUndeadTriggerFrame == frameId)
                return false;

            _undeadCount -= UndeadThreshold;
            _lastUndeadTriggerFrame = frameId;
            return true;
        }

        static bool IsEligibleCountableDeath(in SynergyEnemyDeathEvent deathEvent)
        {
            if ((deathEvent.HasNumericLifeInstanceId == false && string.IsNullOrEmpty(deathEvent.LifeInstanceId))
                || deathEvent.IsTrainingDummy
                || deathEvent.IsSummonObject)
            {
                return false;
            }

            return deathEvent.SourceCategory == SynergyDeathSourceCategory.Commander
                || deathEvent.SourceCategory == SynergyDeathSourceCategory.Companion
                || deathEvent.SourceCategory == SynergyDeathSourceCategory.Synergy;
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
}
