namespace Lizzo.PV.Legion
{
    public sealed partial class PartyService
    {
        public void Dispose()
        {
            ResetRunState();
        }

        public void ResetRunState()
        {
            WasSlotFullState = false;
        }

    }
}
