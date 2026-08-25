using System.Collections.Generic;
using UnityEngine;

namespace Lizzo.PV.P0.Cards
{
    public static partial class CardEffectRuntime
    {
        public sealed class PassiveProgression
        {
                        private readonly List<CardKind> _acquisitionOrder = new List<CardKind>(MaxDistinctPassiveTypes);
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

            public bool TryRecordSuccess(string passiveId, CardKind kind)
            {
                if (IsEligible(passiveId) == false)
                    return false;

                int currentCount = GetCount(passiveId);
                if (currentCount == 0)
                    _acquisitionOrder.Add(kind);

                _acquisitionCounts[passiveId] = currentCount + 1;
                return true;
            }

            public bool TryRecordSuccess(string passiveId)
            {
                return TryRecordSuccess(passiveId, default);
            }


            public int FillDistinctKinds(CardKind[] kinds)
            {
                if (kinds == null)
                    return 0;

                int count = Mathf.Min(kinds.Length, _acquisitionOrder.Count);
                for (int i = 0; i < count; i++)
                    kinds[i] = _acquisitionOrder[i];

                return count;
            }


            public void Reset()
            {
                _acquisitionCounts.Clear();
                _acquisitionOrder.Clear();
            }
        }

    }
}
