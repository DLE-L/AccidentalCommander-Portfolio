namespace Lizzo.PV.Legion
{
    public sealed partial class PartyService
    {
        internal bool ApplyCanonicalTargetAreaCombat(AllyCombat combat, string baseUnitId)
        {
            if (combat == null || CanonicalTargetAreaCombat.TryResolve(baseUnitId, AllyAttackMultiplierState, out CompanionTargetAreaCombatSetup setup) == false)
                return false;

            CompanionGrowthScale growth = ResolveGrowthScale(baseUnitId);
            setup = setup.WithGrowthScale(growth).WithPassiveModifiers(ResolvePassiveCombatModifiers(baseUnitId));
            combat.BindParty(this);
            combat.SetCanonicalTargetAreaInfo(setup);
            return true;
        }
    }
}
