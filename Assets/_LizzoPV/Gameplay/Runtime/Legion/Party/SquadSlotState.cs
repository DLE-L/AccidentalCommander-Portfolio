namespace Lizzo.PV.Legion
{
    public readonly struct SquadSlotState
    {
        public SquadSlotState(
            string slotId,
            string baseUnitId,
            string displayName,
            int currentCount,
            int maxCount,
            bool isPromoted,
            string leaderUnitId)
        {
            SlotId = slotId;
            BaseUnitId = baseUnitId;
            DisplayName = displayName;
            CurrentCount = currentCount;
            MaxCount = maxCount;
            IsPromoted = isPromoted;
            LeaderUnitId = leaderUnitId;
        }

        public string SlotId { get; }
        public string BaseUnitId { get; }
        public string DisplayName { get; }
        public int CurrentCount { get; }
        public int MaxCount { get; }
        public bool IsPromoted { get; }
        public string LeaderUnitId { get; }
        public bool IsActive => CurrentCount > 0;
        public bool IsComplete => MaxCount > 0 && CurrentCount >= MaxCount;
    }
}
