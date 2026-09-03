using System;

namespace Lizzo.PV.Flow
{
    public enum SceneTransitionKind
    {
        Start = 0,
        Standard = 1,
    }

    public readonly struct SceneTransitionRequest : IEquatable<SceneTransitionRequest>
    {
        public SceneTransitionRequest(string targetScenePath, SceneTransitionKind kind, bool canReturnToSource)
        {
            TargetScenePath = targetScenePath ?? string.Empty;
            Kind = kind;
            CanReturnToSource = canReturnToSource;
        }

        public string TargetScenePath { get; }
        public SceneTransitionKind Kind { get; }
        public bool CanReturnToSource { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(TargetScenePath);

        public bool Equals(SceneTransitionRequest other)
        {
            return string.Equals(TargetScenePath, other.TargetScenePath, StringComparison.Ordinal)
                   && Kind == other.Kind
                   && CanReturnToSource == other.CanReturnToSource;
        }

        public override bool Equals(object obj)
        {
            return obj is SceneTransitionRequest other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = StringComparer.Ordinal.GetHashCode(TargetScenePath ?? string.Empty);
                hash = (hash * 397) ^ (int)Kind;
                hash = (hash * 397) ^ CanReturnToSource.GetHashCode();
                return hash;
            }
        }
    }
}
