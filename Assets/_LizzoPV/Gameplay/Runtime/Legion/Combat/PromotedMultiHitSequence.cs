namespace Lizzo.PV.Legion
{
    public sealed class PromotedMultiHitSequence
    {
        private int _completedPassCount;

        public PromotedMultiHitSequence(int passCount, float damageRatio)
        {
            PassCount = passCount;
            DamageRatio = damageRatio;
        }

        public int PassCount { get; }
        public float DamageRatio { get; }
        public int CompletedPassCount => _completedPassCount;
        public bool IsComplete => _completedPassCount == PassCount;

        public void BeginCast()
        {
            _completedPassCount = 0;
        }

        public bool TryRecordResolvedPass()
        {
            if (_completedPassCount >= PassCount)
                return false;

            _completedPassCount++;
            return true;
        }

        public void Reset()
        {
            _completedPassCount = 0;
        }
    }
}
