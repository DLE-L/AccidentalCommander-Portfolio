namespace Lizzo.PV.Combat
{
    public interface ICombatImmediateHitModule
    {
        bool TryApply(in CombatImmediateHitRequest request);
    }
}
