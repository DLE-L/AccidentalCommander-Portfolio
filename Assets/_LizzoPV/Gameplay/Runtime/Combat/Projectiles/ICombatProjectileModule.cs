namespace Lizzo.PV.Combat.Projectiles
{
    public interface ICombatProjectileModule
    {
        bool TrySpawn(in CombatProjectileRequest request);
    }
}
