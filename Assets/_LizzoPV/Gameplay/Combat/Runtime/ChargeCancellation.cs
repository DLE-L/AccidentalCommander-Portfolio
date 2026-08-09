namespace Lizzo.PV.Combat
{
    public readonly struct ChargeCancellationResult
    {
        public ChargeCancellationResult(bool chargeCancelled, bool stunApplied)
        {
            ChargeCancelled = chargeCancelled;
            StunApplied = stunApplied;
        }

        public bool ChargeCancelled { get; }
        public bool StunApplied { get; }
    }

    public interface IChargeCancelable
    {
        bool IsChargeCancelable { get; }
        ChargeCancellationResult CancelChargeAndApplyStun(float stunDuration);
    }

    public static class ChargeCancellationRules
    {
        public static ChargeCancellationResult Resolve(bool chargeCancelable, bool stunImmune, float stunDuration)
        {
            return chargeCancelable
                ? new ChargeCancellationResult(true, stunImmune == false && stunDuration > 0.0f)
                : new ChargeCancellationResult(false, false);
        }
    }
}
