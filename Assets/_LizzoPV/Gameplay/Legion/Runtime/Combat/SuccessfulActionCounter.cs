namespace Lizzo.PV.Legion
{
    public sealed class SuccessfulActionCounter
    {
        private int _triggerCount;
        private int _currentCount;

        public int TriggerCount => _triggerCount;
        public int CurrentCount => _currentCount;

        public void Configure(int triggerCount)
        {
            _triggerCount = triggerCount < 1 ? 1 : triggerCount;
            _currentCount = 0;
        }

        public bool RecordSuccess()
        {
            if (_triggerCount < 1)
                return false;

            _currentCount++;
            if (_currentCount < _triggerCount)
                return false;

            _currentCount = 0;
            return true;
        }

        public void Reset() => _currentCount = 0;
    }
}
