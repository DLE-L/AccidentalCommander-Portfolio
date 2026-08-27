namespace Lizzo.PV.Legion
{
    public sealed partial class PartyService
    {
        internal bool ApplyCanonicalMeleeCombat(AllyCombat combat, string baseUnitId)
        {
            if (combat == null || CanonicalMeleeCombat.TryResolve(baseUnitId, AllyAttackMultiplierState, out CompanionMeleeCombatSetup setup) == false)
                return false;

            if (this.IsShieldSoldierAreaPushTest(baseUnitId))
                setup = setup.WithShieldAreaPushCompatibilityOverride();

            CompanionGrowthScale growth = ResolveGrowthScale(baseUnitId);
            if (baseUnitId == "shield_guard" && growth.VisualUnitCount == 3)
                setup = setup.WithPromotedShieldCaptainGeometry();
            setup = setup.WithGrowthScale(growth).WithPassiveModifiers(ResolvePassiveCombatModifiers(baseUnitId));

            combat.BindParty(this);
            combat.SetCanonicalMeleeInfo(setup);
            if (baseUnitId == "sword_soldier" && growth.VisualUnitCount == 3)
                combat.SetPromotedMultiHitSequence(new PromotedMultiHitSequence(2, 0.70f));
            return true;
        }

        internal bool ApplyCanonicalWraithCombat(AllyCombat combat, string baseUnitId)
        {
            if (combat == null
                || baseUnitId != "wraith_knight"
                || CanonicalMeleeCombat.TryResolveWraithMeleeDefense(
                    AllyAttackMultiplierState,
                    out CompanionWraithMeleeDefenseSetup setup) == false)
            {
                return false;
            }

            CompanionGrowthScale growth = ResolveGrowthScale(baseUnitId);
            bool promoted = growth.VisualUnitCount == 3;
            if (promoted)
                setup = setup.WithPromotedWraithGuardianDefense().WithPromotedWraithGuardianGeometry();

            CompanionMeleeCombatSetup scaled = setup.Melee
                .WithGrowthScale(growth)
                .WithPassiveModifiers(ResolvePassiveCombatModifiers(baseUnitId));
            combat.BindParty(this);
            if (promoted)
                combat.SetCanonicalWraithMeleeDefenseInfo(new CompanionWraithMeleeDefenseSetup(scaled, setup.PersonalDefense));
            else
                combat.SetCanonicalMeleeInfo(scaled);
            return true;
        }
    }
}
