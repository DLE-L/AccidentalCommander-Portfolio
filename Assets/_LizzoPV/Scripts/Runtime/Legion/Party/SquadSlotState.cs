namespace Lizzo.PV.Legion
{
    public readonly struct SquadSlotState
    {
        public SquadSlotState(
            string slotId,
            string familyId,
            string displayName,
            int currentCount,
            int maxCount,
            bool isPromoted,
            string leaderUnitId)
        {
            SlotId = slotId;
            FamilyId = familyId;
            DisplayName = displayName;
            CurrentCount = currentCount;
            MaxCount = maxCount;
            IsPromoted = isPromoted;
            LeaderUnitId = leaderUnitId;
        }

        public string SlotId { get; }
        public string FamilyId { get; }
        public string DisplayName { get; }
        public int CurrentCount { get; }
        public int MaxCount { get; }
        public bool IsPromoted { get; }
        public string LeaderUnitId { get; }
        public bool IsActive => CurrentCount > 0;
        public bool IsComplete => MaxCount > 0 && CurrentCount >= MaxCount;
    }
}
