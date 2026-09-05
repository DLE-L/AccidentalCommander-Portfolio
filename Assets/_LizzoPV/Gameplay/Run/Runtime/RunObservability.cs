using System;

namespace Lizzo.PV.Gameplay.Run
{
    public enum RunEventKind
    {
        RunStarted, EnemySpawned, SynergyQueued, SynergyStarted, SynergyCompleted,
        ExperienceAbsorbed, BossSpawned, RunFinished,
    }

    public readonly struct RunEventSnapshot
    {
        public long Sequence { get; }
        public float TimeSeconds { get; }
        public RunEventKind Kind { get; }
        public string SubjectId { get; }
        public int Value { get; }

        public RunEventSnapshot(long sequence, float timeSeconds, RunEventKind kind, string subjectId, int value)
        {
            Sequence = sequence;
            TimeSeconds = timeSeconds;
            Kind = kind;
            SubjectId = subjectId ?? string.Empty;
            Value = value;
        }
    }

    public sealed class RunEventBuffer
    {
        private readonly RunEventSnapshot[] _events;
        private int _count;
        private int _next;
        private long _sequence;

        public int Count => _count;

        public RunEventBuffer(int capacity)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));
            _events = new RunEventSnapshot[capacity];
        }

        public void Record(float timeSeconds, RunEventKind kind, string subjectId = null, int value = 0)
        {
            if (float.IsNaN(timeSeconds) || float.IsInfinity(timeSeconds) || timeSeconds < 0.0f)
                throw new ArgumentOutOfRangeException(nameof(timeSeconds));
            _events[_next] = new RunEventSnapshot(++_sequence, timeSeconds, kind, subjectId, value);
            _next = (_next + 1) % _events.Length;
            if (_count < _events.Length)
                _count++;
        }

        public RunEventSnapshot GetOldest(int index)
        {
            if ((uint)index >= (uint)_count)
                throw new ArgumentOutOfRangeException(nameof(index));
            int first = _count == _events.Length ? _next : 0;
            return _events[(first + index) % _events.Length];
        }
    }
}
