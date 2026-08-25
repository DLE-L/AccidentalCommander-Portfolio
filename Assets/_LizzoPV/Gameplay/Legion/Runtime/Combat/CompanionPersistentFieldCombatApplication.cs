namespace Lizzo.PV.Legion
{
    public sealed partial class PartyService
    {
        internal bool ApplyCanonicalPersistentFieldCombat(AllyCombat combat, string baseUnitId)
        {
            if (combat == null || CanonicalPersistentFieldCombat.TryResolve(baseUnitId, AllyAttackMultiplierState, out CompanionPersistentFieldCombatSetup setup) == false)
                return false;

            CompanionGrowthScale growth = ResolveGrowthScale(baseUnitId);
            if (baseUnitId == "fire_mage" && growth.VisualUnitCount == 3)
                setup = setup.WithPromotedFireSageField();

            setup = setup.WithGrowthScale(growth).WithPassiveModifiers(ResolvePassiveCombatModifiers(baseUnitId));
            combat.BindParty(this);
            combat.SetCanonicalPersistentFieldInfo(setup);
            return true;
        }
    }
}
