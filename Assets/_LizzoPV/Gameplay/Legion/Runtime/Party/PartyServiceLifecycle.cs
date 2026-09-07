namespace Lizzo.PV.Legion
{
    public sealed partial class PartyService
    {
        public void Dispose()
        {
            if (_passiveRoster != null)
                _passiveRoster.Changed -= RefreshAllCompanionCombat;
            this.ResetRunState();
        }

        public void ResetRunState()
        {
            for (int i = Allies.Count - 1; i >= 0; i--)
            {
                if (Allies[i] != null)
                    _factory.Release(Allies[i].gameObject);
            }

            Allies.Clear();
            ShieldSoldiers.Clear();
            Companions.Clear();
            WasSlotFullState = false;
            ResetCardModifiers();
            Formation.ResetRunState();
        }

    }
}
