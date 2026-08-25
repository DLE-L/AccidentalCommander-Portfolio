namespace Lizzo.PV.Legion
{
    public sealed partial class PartyService
    {
        internal bool ApplyCanonicalTargetAreaCombat(AllyCombat combat, string baseUnitId)
        {
            if (combat == null || CanonicalTargetAreaCombat.TryResolve(baseUnitId, AllyAttackMultiplierState, out CompanionTargetAreaCombatSetup setup) == false)
                return false;

            CompanionGrowthScale growth = ResolveGrowthScale(baseUnitId);
            bool promoted = growth.VisualUnitCount == 3;
            if (baseUnitId == "bombardier" && promoted)
                setup = setup.WithPromotedPowderCaptainImpact();

            setup = setup.WithGrowthScale(growth).WithPassiveModifiers(ResolvePassiveCombatModifiers(baseUnitId));
            combat.BindParty(this);
            combat.SetCanonicalTargetAreaInfo(setup);
            if (baseUnitId == "skeleton_bomber" && promoted)
                combat.SetPromotedTargetAreaFollowUp(setup.CreatePromotedBoneArtilleryFollowUp());
            return true;
        }
    }
}
