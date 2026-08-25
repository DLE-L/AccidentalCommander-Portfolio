namespace Lizzo.PV.Legion
{
    public sealed partial class PartyService
    {
        internal bool ApplyCanonicalChainCombat(AllyCombat combat, string baseUnitId)
        {
            if (combat == null || CanonicalChainCombat.TryResolve(baseUnitId, AllyAttackMultiplierState, out CompanionChainCombatSetup setup) == false)
                return false;

            CompanionGrowthScale growth = ResolveGrowthScale(baseUnitId);
            if (baseUnitId == "lightning_mage" && growth.VisualUnitCount == 3)
                setup = setup.WithPromotedStormMageChain();

            setup = setup.WithGrowthScale(growth).WithPassiveModifiers(ResolvePassiveCombatModifiers(baseUnitId));
            combat.BindParty(this);
            combat.SetCanonicalChainInfo(setup);
            return true;
        }
    }
}
