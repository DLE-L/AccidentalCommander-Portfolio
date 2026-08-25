namespace Lizzo.PV.Legion
{
    public sealed partial class PartyService
    {
        internal bool ApplyCanonicalRangedSupportCombat(AllyCombat combat, string baseUnitId)
        {
            if (combat == null || CanonicalRangedSupportCombat.TryResolve(baseUnitId, AllyAttackMultiplierState, out CompanionRangedSupportCombatSetup setup) == false)
                return false;

            combat.BindParty(this);
            CompanionGrowthScale growth = ResolveGrowthScale(baseUnitId);
            if (baseUnitId == "cleric" && growth.VisualUnitCount == 3)
                setup = setup.WithPromotedLightGuideHeal();
            else if (baseUnitId == "field_herbalist" && growth.VisualUnitCount == 3)
                setup = setup.WithPromotedBattleApothecaryHeal().WithPromotedBattleApothecaryBounce();

            setup = setup.WithGrowthScale(growth).WithPassiveModifiers(ResolvePassiveCombatModifiers(baseUnitId));

            combat.SetCanonicalRangedSupportInfo(setup);
            if (setup.HasPrimaryProjectileBounce)
                combat.SetPromotedProjectileBounce(setup.PrimaryProjectileBounce);
            return true;
        }
    }
}
