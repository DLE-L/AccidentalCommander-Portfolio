using System.Collections.Generic;

namespace Lizzo.PV.Legion
{
    public enum ReturningAttackPass
    {
        Outbound,
        Return
    }

    public sealed class ReturningAttackHitLedger
    {
        private readonly HashSet<int> _outboundTargets = new HashSet<int>();
        private readonly HashSet<int> _returnTargets = new HashSet<int>();

        public int OutboundHitCount => _outboundTargets.Count;
        public int ReturnHitCount => _returnTargets.Count;

        public bool TryRecord(int targetKey, ReturningAttackPass pass)
        {
            if (targetKey == 0)
                return false;

            return pass == ReturningAttackPass.Outbound
                ? _outboundTargets.Add(targetKey)
                : _returnTargets.Add(targetKey);
        }

        public void Reset()
        {
            _outboundTargets.Clear();
            _returnTargets.Clear();
        }
    }
}
