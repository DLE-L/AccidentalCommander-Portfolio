namespace Lizzo.PV.Combat
{
    public interface ICombatImmediateHitTarget
    {
        CombatImmediateHitFaction Faction { get; }
        bool IsAlive { get; }
        void ReceiveImmediateHit(in CombatImmediateHitRequest request);
    }
}
