namespace Lizzo.PV.Combat.Fields
{
    public interface ICombatPersistentFieldModule
    {
        int ActiveFieldCount { get; }

        bool TrySpawn(in CombatPersistentFieldRequest request, float currentTime);
        void Tick(float currentTime);
        void Reset();
        void Dispose();
    }
}
