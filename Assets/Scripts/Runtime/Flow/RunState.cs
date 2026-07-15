using System;

namespace Lizzo.PV.Flow
{
    public enum RunOutcome
    {
        Clear,
        Failure
    }

    public readonly struct RunResult
    {
        public RunOutcome Outcome { get; }
        public int BossHpPercent { get; }
        public float ElapsedSeconds { get; }
        public int KillCount { get; }

        public RunResult(RunOutcome outcome, int bossHpPercent, float elapsedSeconds, int killCount)
        {
            Outcome = outcome;
            BossHpPercent = bossHpPercent;
            ElapsedSeconds = elapsedSeconds;
            KillCount = killCount;
        }
    }

    public sealed class RunState : IDisposable
    {
        bool _disposed;

        public bool IsLoaded { get; private set; }
        public int Level { get; private set; } = 1;
        public int Experience { get; private set; }
        public int RequiredExperience { get; private set; } = 1;
        public int KillCount { get; private set; }
        public float ElapsedSeconds { get; private set; }

        public event Action<int, int> ExperienceChanged;
        public event Action<int> KillCountChanged;        public event Action<RunResult> RunEnded;


        public void Reset(int requiredExperience)
        {
            EnsureNotDisposed();
            IsLoaded = false;
            Level = 1;
            Experience = 0;
            RequiredExperience = Math.Max(1, requiredExperience);
            KillCount = 0;
            ElapsedSeconds = 0.0f;
        }

        public void MarkLoaded()
        {
            EnsureNotDisposed();
            IsLoaded = true;
        }

        public void MarkStopped()
        {
            if (_disposed)
                return;

            IsLoaded = false;
        }

        public bool TryEnd(RunOutcome outcome, int bossHpPercent)
        {
            EnsureNotDisposed();
            if (!IsLoaded)
                return false;

            int normalizedBossHp = outcome == RunOutcome.Clear
                ? 0
                : Math.Clamp(bossHpPercent, -1, 100);
            IsLoaded = false;
            RunEnded?.Invoke(new RunResult(outcome, normalizedBossHp, ElapsedSeconds, KillCount));
            return true;
        }


        public void AdvanceTime(float deltaSeconds)
        {
            EnsureNotDisposed();
            if (!IsLoaded || deltaSeconds <= 0.0f)
                return;

            ElapsedSeconds += deltaSeconds;
        }

        public bool AddExperience(int amount)
        {
            EnsureNotDisposed();
            if (!IsLoaded || amount <= 0)
                return false;

            Experience += amount;
            ExperienceChanged?.Invoke(Experience, RequiredExperience);
            return Experience >= RequiredExperience;
        }

        public void AdvanceLevel(int requiredExperience)
        {
            EnsureNotDisposed();
            Level = Math.Max(1, Level + 1);
            Experience = 0;
            RequiredExperience = Math.Max(1, requiredExperience);
            ExperienceChanged?.Invoke(Experience, RequiredExperience);
        }

        public void RegisterKill()
        {
            EnsureNotDisposed();
            if (!IsLoaded)
                return;

            KillCount++;
            KillCountChanged?.Invoke(KillCount);
        }

        internal void SetLevelForDebug(int level, int requiredExperience)
        {
            EnsureNotDisposed();
            Level = Math.Max(1, level);
            Experience = 0;
            RequiredExperience = Math.Max(1, requiredExperience);
            ExperienceChanged?.Invoke(Experience, RequiredExperience);
        }

        internal void SetElapsedSecondsForDebug(float seconds)
        {
            EnsureNotDisposed();
            ElapsedSeconds = Math.Max(0.0f, seconds);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            IsLoaded = false;
            ExperienceChanged = null;
            KillCountChanged = null;            RunEnded = null;

            _disposed = true;
        }

        void EnsureNotDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(RunState));
        }
    }
}
