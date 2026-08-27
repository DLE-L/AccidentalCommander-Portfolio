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
            setup = setup.WithGrowthScale(growth).WithPassiveModifiers(ResolvePassiveCombatModifiers(baseUnitId));

            combat.BindParty(this);
            combat.SetCanonicalMeleeInfo(setup);
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
            CompanionMeleeCombatSetup scaled = setup.Melee
                .WithGrowthScale(growth)
                .WithPassiveModifiers(ResolvePassiveCombatModifiers(baseUnitId));
            combat.BindParty(this);
            combat.SetCanonicalMeleeInfo(scaled);
            return true;
        }
    }
}
