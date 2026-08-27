namespace Lizzo.PV.Legion
{
    public sealed partial class PartyService
    {
        internal bool ApplyCanonicalReturningAttackCombat(AllyCombat combat, string baseUnitId)
        {
            if (combat == null
                || CanonicalReturningAttackCombat.TryResolve(
                    baseUnitId,
                    AllyAttackMultiplierState,
                    out CompanionReturningAttackCombatSetup setup) == false)
            {
                return false;
            }

            setup = setup
                .WithGrowthScale(ResolveGrowthScale(baseUnitId))
                .WithPassiveModifiers(ResolvePassiveCombatModifiers(baseUnitId));
            combat.BindParty(this);
            combat.SetCanonicalReturningAttackInfo(setup);
            return true;
        }
    }
}
