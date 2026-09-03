namespace Lizzo.PV.Combat
{
    public interface ICombatImmediateHitTarget
    {
        CombatImmediateHitFaction Faction { get; }
        bool IsAlive { get; }
        bool TryReceiveImmediateHit(in CombatImmediateHitRequest request);
    }
}
