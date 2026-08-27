namespace Lizzo.PV.Combat.Fields
{
    public interface ICombatPersistentFieldModule
    {
        int ActiveFieldCount { get; }

        bool TrySpawn(in CombatPersistentFieldRequest request, float currentTime);
        bool TryIgnite(in CombatPersistentFieldIgnitionRequest request, float currentTime, out int ignitedFieldCount);
        void Tick(float currentTime);
        void Reset();
        void Dispose();
    }
}
