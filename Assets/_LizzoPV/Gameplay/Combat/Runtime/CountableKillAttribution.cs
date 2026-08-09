using UnityEngine;

namespace Lizzo.PV.Combat
{
    public enum CombatKillSourceCategory
    {
        None = 0,
        CompanionOwnedAction,
        PersonalSummon,
        SynergySummon,
        SynergyAction,
        Commander,
        Enemy,
    }

    public readonly struct CountableKillAttribution
    {
        public int OwnerInstanceId { get; }
        public string SourceId { get; }
        public CombatKillSourceCategory Category { get; }
        public long LifeInstanceId { get; }
        public Vector3 LethalPosition { get; }
        public int FrameId { get; }

        public CountableKillAttribution(int ownerInstanceId, string sourceId, CombatKillSourceCategory category)
            : this(ownerInstanceId, sourceId, category, 0L, Vector3.zero, 0)
        {
        }

        CountableKillAttribution(int ownerInstanceId, string sourceId, CombatKillSourceCategory category, long lifeInstanceId, Vector3 lethalPosition, int frameId)
        {
            OwnerInstanceId = ownerInstanceId;
            SourceId = sourceId;
            Category = category;
            LifeInstanceId = lifeInstanceId;
            LethalPosition = lethalPosition;
            FrameId = frameId;
        }

        public CountableKillAttribution WithLethalContext(long lifeInstanceId, Vector3 lethalPosition, int frameId)
        {
            return new CountableKillAttribution(OwnerInstanceId, SourceId, Category, lifeInstanceId, lethalPosition, frameId);
        }

        public bool IsCountable => OwnerInstanceId != 0
            && string.IsNullOrEmpty(SourceId) == false
            && Category == CombatKillSourceCategory.CompanionOwnedAction;

        public bool IsAttributable => string.IsNullOrEmpty(SourceId) == false
            && (Category == CombatKillSourceCategory.CompanionOwnedAction || Category == CombatKillSourceCategory.SynergyAction);
    }
}
