using System.Collections.Generic;

namespace Lizzo.PV.P0.Cards
{
    public static partial class CardEffectRuntime
    {
        public sealed class PassiveProgression
        {
            private readonly Dictionary<string, int> _acquisitionCounts = new Dictionary<string, int>(MaxDistinctPassiveTypes);

            public int DistinctCount => _acquisitionCounts.Count;

            public int GetCount(string passiveId)
            {
                return string.IsNullOrWhiteSpace(passiveId) == false
                    && _acquisitionCounts.TryGetValue(passiveId, out int count)
                    ? count
                    : 0;
            }

            public bool IsEligible(string passiveId)
            {
                if (string.IsNullOrWhiteSpace(passiveId))
                    return false;

                int currentCount = GetCount(passiveId);
                return currentCount < MaxPassiveAcquisitions
                    && (currentCount > 0 || _acquisitionCounts.Count < MaxDistinctPassiveTypes);
            }

            public bool TryRecordSuccess(string passiveId)
            {
                if (IsEligible(passiveId) == false)
                    return false;

                int currentCount = GetCount(passiveId);
                _acquisitionCounts[passiveId] = currentCount + 1;
                return true;
            }

            public void Reset()
            {
                _acquisitionCounts.Clear();
            }
        }
    }
}
