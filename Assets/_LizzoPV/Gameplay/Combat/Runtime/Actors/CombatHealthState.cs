namespace Lizzo.PV.Combat
{
    // Owns health values and damage arithmetic. Hit protection, healing policy and death effects belong to callers.
    public sealed class CombatHealthState
    {
        public int Current { get; private set; } = 100;
        public int Maximum { get; private set; } = 100;

        // Snapshot restoration intentionally preserves raw values used by spawn, tutorial and diagnostics.
        public void Restore(int current, int maximum)
        {
            Current = current;
            Maximum = maximum;
        }

        public void Reset(int maximum) => Restore(maximum, maximum);

        public int Heal(int amount)
        {
            int previous = Current;
            Current = System.Math.Min(Maximum, Current + amount);
            return Current - previous;
        }

        // Preserve the existing raw assignment/signed-damage contract during actor migration.
        // Returns true only when this call changes a living actor to zero HP.
        public bool ApplyDamageAndCheckDeath(int damage)
        {
            if (Current <= 0)
                return false;

            Current -= damage;
            if (Current > 0)
                return false;

            Current = 0;
            return true;
        }
    }
}
