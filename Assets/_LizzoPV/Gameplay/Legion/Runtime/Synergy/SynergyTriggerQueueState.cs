using System;

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

}
