using System;

namespace Lizzo.PV.Combat
{
    public readonly struct CompanionStatusSource
    {
        public CompanionStatusSource(string unitId, int ownerInstanceId, int reactionDepth = 0)
        {
            UnitId = unitId;
            OwnerInstanceId = ownerInstanceId;
            ReactionDepth = Math.Max(0, reactionDepth);
        }

        public string UnitId { get; }
        public int OwnerInstanceId { get; }
        public int ReactionDepth { get; }
        public bool IsValid => string.IsNullOrEmpty(UnitId) == false && OwnerInstanceId != 0;
    }
}
