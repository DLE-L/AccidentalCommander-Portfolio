namespace Lizzo.PV.Legion
{
    public sealed partial class PartyService
    {
        internal bool ApplyCanonicalProjectileCombat(AllyCombat combat, string baseUnitId)
        {
            if (combat == null || CanonicalProjectileCombat.TryResolve(baseUnitId, AllyAttackMultiplierState, out CompanionProjectileCombatSetup setup) == false)
                return false;

            combat.BindParty(this);
            CompanionGrowthScale growth = ResolveGrowthScale(baseUnitId);
            if (baseUnitId == "necromancer" && growth.VisualUnitCount == 3)
                setup = setup.WithPromotedDarkRitualistRange();
            setup = setup.WithGrowthScale(growth).WithPassiveModifiers(ResolvePassiveCombatModifiers(baseUnitId));
            if (CanonicalOwnedProxyCombat.TryResolve(baseUnitId, out CompanionOwnedProxyCombatSetup proxy))
            {
                if (baseUnitId == "falcon_archer" && growth.VisualUnitCount == 3)
                    proxy = proxy.WithTriggerCount(3);
                combat.SetCanonicalProjectileWithProxyInfo(setup, proxy);
            }
            else
                combat.SetCanonicalProjectileInfo(setup);
            if (baseUnitId == "falcon_archer" && growth.VisualUnitCount == 3)
                combat.SetPromotedProjectileBurst(new PromotedProjectileBurst(2, 0.65f));
            return true;
        }
    }
}
