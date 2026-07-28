using System;

namespace Lizzo.PV.Flow
{
    public enum RunMode
    {
        Normal,
        Tutorial,
    }

    public readonly struct RunContext : IEquatable<RunContext>
    {
        public static RunContext Normal => new RunContext(RunMode.Normal);
        public static RunContext Tutorial => new RunContext(RunMode.Tutorial);

        public RunMode Mode { get; }
        public bool IsTutorial => Mode == RunMode.Tutorial;
        public bool IsNormal => Mode == RunMode.Normal;

        public RunContext(RunMode mode)
        {
            if (mode != RunMode.Normal && mode != RunMode.Tutorial)
                throw new ArgumentOutOfRangeException(nameof(mode));

            Mode = mode;
        }

        public bool Equals(RunContext other) => Mode == other.Mode;

        public override bool Equals(object obj) => obj is RunContext other && Equals(other);

        public override int GetHashCode() => (int)Mode;

        public override string ToString() => Mode == RunMode.Tutorial ? "tutorial" : "normal";
    }

    public sealed class RunLaunchState
    {
        RunContext _currentContext = RunContext.Normal;
        bool _hasPreparedRequest;

        public RunContext CurrentContext => _currentContext;
        public bool HasPreparedRequest => _hasPreparedRequest;

        public void Prepare(RunContext context)
        {
            _currentContext = context;
            _hasPreparedRequest = true;
        }

        public RunContext ConsumeForLaunch()
        {
            if (_hasPreparedRequest == false)
            {
                _currentContext = RunContext.Normal;
                return _currentContext;
            }

            _hasPreparedRequest = false;
            return _currentContext;
        }

        public void PrepareRetry()
        {
            Prepare(_currentContext);
        }
    }
}
