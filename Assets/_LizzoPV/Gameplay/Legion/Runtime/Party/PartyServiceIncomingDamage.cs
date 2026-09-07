using Lizzo.PV.Legion.Party.Roster;

namespace Lizzo.PV.Legion
{
    public sealed partial class PartyService
    {
        private readonly CompanionIncomingDamageResolver _incomingDamage;

        internal CompanionIncomingDamageResolution ResolveCompanionIncomingDamage(
            CompanionRuntime companion,
            int originalDamage,
            int currentHp)
        {
            return _incomingDamage.Resolve(companion, originalDamage, currentHp);
        }

    }
}
