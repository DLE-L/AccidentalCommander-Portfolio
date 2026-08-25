namespace Lizzo.PV.Legion
{
    public sealed partial class PartyService
    {
        internal bool ApplyCanonicalWolfCombat(AllyCombat combat, string baseUnitId)
        {
            if (combat == null || CanonicalWolfOwnedProxyCombat.TryResolve(baseUnitId, out CompanionWolfOwnedProxyCombatSetup setup) == false)
                return false;

            CompanionGrowthScale growth = ResolveGrowthScale(baseUnitId);
            if (growth.VisualUnitCount == 3)
                setup = setup.WithPromotedBeastCommanderHits();

            setup = setup.WithGrowthScale(growth).WithPassiveModifiers(ResolvePassiveCombatModifiers(baseUnitId));
            combat.BindParty(this);
            combat.SetCanonicalWolfOwnedProxyInfo(setup);
            return true;
        }
    }
}
