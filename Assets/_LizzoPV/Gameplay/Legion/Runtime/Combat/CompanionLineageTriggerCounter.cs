namespace Lizzo.PV.Legion
{
    public enum CompanionLineageEventKind
    {
        Action,
        Kill,
        Hit
    }

    public sealed class CompanionLineageTriggerCounter
    {
        private CompanionLineageEventKind _eventKind;
        private int _triggerThreshold;
        private int _currentCount;
        private int _totalTriggerCount;

        public CompanionLineageEventKind EventKind => _eventKind;
        public int TriggerThreshold => _triggerThreshold;
        public int CurrentCount => _currentCount;
        public int TotalTriggerCount => _totalTriggerCount;

        public void Configure(CompanionLineageEventKind eventKind, int triggerThreshold)
        {
            _eventKind = eventKind;
            _triggerThreshold = triggerThreshold < 1 ? 1 : triggerThreshold;
            Reset();
        }

        public int Record(CompanionLineageEventKind eventKind, int count = 1)
        {
            if (_triggerThreshold < 1 || eventKind != _eventKind || count < 1)
                return 0;

            _currentCount += count;
            int triggered = _currentCount / _triggerThreshold;
            _currentCount %= _triggerThreshold;
            _totalTriggerCount += triggered;
            return triggered;
        }

        public void Reset()
        {
            _currentCount = 0;
            _totalTriggerCount = 0;
        }
    }
}
