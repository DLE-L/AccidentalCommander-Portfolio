using System;

namespace Lizzo.PV.Legion
{
    public readonly struct CompanionConditionProgress
    {
        public CompanionConditionProgress(float current, float required, int pending)
        { Current = current; Required = required; Pending = pending; }
        public float Current { get; }
        public float Required { get; }
        public int Pending { get; }
    }

    public interface ICompanionConditionSource
    {
        event Action<string, CompanionConditionProgress> Changed;
        bool TryGetProgress(string companionId, out CompanionConditionProgress progress);
    }
}
