using Lizzo.PV.Legion.Party.Roster;

namespace Lizzo.PV.Legion
{
    public interface ICanonicalCompanionRosterView
    {
        int ActiveCompanionSlotCount { get; }
        int ActiveCompanionSlotCap { get; }
        PartyRosterChangeResult PreviewCanonicalRecruit(string baseUnitId);
    }
}
