namespace Lizzo.PV.Legion
{
    public sealed class SuccessfulActionCounter
    {
        private readonly CompanionLineageTriggerCounter _counter = new CompanionLineageTriggerCounter();

        public int TriggerCount => _counter.TriggerThreshold;
        public int CurrentCount => _counter.CurrentCount;

        public void Configure(int triggerCount)
        {
            _counter.Configure(CompanionLineageEventKind.Action, triggerCount);
        }

        public bool RecordSuccess()
        {
            return _counter.Record(CompanionLineageEventKind.Action) > 0;
        }

        public void Reset() => _counter.Reset();
    }
}
