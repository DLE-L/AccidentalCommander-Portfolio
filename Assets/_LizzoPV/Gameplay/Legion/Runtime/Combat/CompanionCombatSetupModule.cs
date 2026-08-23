using Lizzo.PV.Data;
using Lizzo.PV.P0.Debugging;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    internal static class CompanionCombatSetupModule
    {
        internal static void ApplyCombatFromData(
            this PartyService party,
            AllyCombat combat,
            UnitData unitData,
            AllyAttackStyle fallbackAttackStyle)
        {
            if (combat == null)
                return;

            SkillData skillData = unitData == null ? null : party.Data.GetSkill(unitData.SkillId);
            AllyAttackStyle attackStyle = party.ResolveAttackStyle(unitData, skillData?.SkillKind, fallbackAttackStyle);
            int power = skillData?.Power ?? Mathf.Max(unitData?.Attack ?? 0, unitData?.Heal ?? 0);
            float cooldown = skillData?.Cooldown ?? unitData?.Cooldown ?? 1.0f;
            float range = skillData?.Range ?? unitData?.Range ?? 1.0f;
            float knockback = skillData?.Knockback ?? unitData?.Knockback ?? 0.0f;

            if (party.IsShieldSoldierAreaPushTest(unitData, skillData?.SkillKind))
            {
                range = Mathf.Max(range, 1.8f);
                knockback = Mathf.Max(knockback, 0.9f);
            }

            float angle = skillData != null && skillData.Angle > 0.0f
                ? skillData.Angle
                : party.ResolveDefaultAttackAngle(attackStyle);

            if (attackStyle != AllyAttackStyle.HealCommander && party.AllyAttackMultiplierState > 1.0001f)
                power = Mathf.Max(1, Mathf.RoundToInt(power * party.AllyAttackMultiplierState));

            combat.BindParty(party);
            combat.SetInfo(attackStyle, power, cooldown, range, knockback, angle);
        }

        internal static bool ApplyCanonicalMeleeCombat(
            this PartyService party,
            AllyCombat combat,
            string baseUnitId)
        {
            if (combat == null || party.CanonicalMeleeCombat.TryResolve(baseUnitId, party.AllyAttackMultiplierState, out CompanionMeleeCombatSetup setup) == false)
                return false;

            if (party.IsShieldSoldierAreaPushTest(baseUnitId))
                setup = setup.WithShieldAreaPushCompatibilityOverride();

            CompanionGrowthScale growth = party.ResolveGrowthScale(baseUnitId);
            if (baseUnitId == "shield_guard" && growth.VisualUnitCount == 3)
                setup = setup.WithPromotedShieldCaptainGeometry();
            setup = setup.WithGrowthScale(growth).WithPassiveModifiers(party.ResolvePassiveCombatModifiers(baseUnitId));

            combat.BindParty(party);
            combat.SetCanonicalMeleeInfo(setup);
            if (baseUnitId == "sword_soldier" && growth.VisualUnitCount == 3)
                combat.SetPromotedMultiHitSequence(new PromotedMultiHitSequence(2, 0.70f));
            return true;
        }

        internal static bool ApplyCanonicalProjectileCombat(
            this PartyService party,
            AllyCombat combat,
            string baseUnitId)
        {
            if (combat == null || party.CanonicalProjectileCombat.TryResolve(baseUnitId, party.AllyAttackMultiplierState, out CompanionProjectileCombatSetup setup) == false)
                return false;

            combat.BindParty(party);
            CompanionGrowthScale growth = party.ResolveGrowthScale(baseUnitId);
            if (baseUnitId == "necromancer" && growth.VisualUnitCount == 3)
                setup = setup.WithPromotedDarkRitualistRange();
            setup = setup.WithGrowthScale(growth).WithPassiveModifiers(party.ResolvePassiveCombatModifiers(baseUnitId));
            if (party.CanonicalOwnedProxyCombat.TryResolve(baseUnitId, out CompanionOwnedProxyCombatSetup proxy))
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

        internal static bool ApplyCanonicalRangedSupportCombat(
            this PartyService party,
            AllyCombat combat,
            string baseUnitId)
        {
            if (combat == null || party.CanonicalRangedSupportCombat.TryResolve(baseUnitId, party.AllyAttackMultiplierState, out CompanionRangedSupportCombatSetup setup) == false)
                return false;

            combat.BindParty(party);
            CompanionGrowthScale growth = party.ResolveGrowthScale(baseUnitId);
            if (baseUnitId == "cleric" && growth.VisualUnitCount == 3)
                setup = setup.WithPromotedLightGuideHeal();
            else if (baseUnitId == "field_herbalist" && growth.VisualUnitCount == 3)
                setup = setup.WithPromotedBattleApothecaryHeal().WithPromotedBattleApothecaryBounce();

            setup = setup.WithGrowthScale(growth).WithPassiveModifiers(party.ResolvePassiveCombatModifiers(baseUnitId));

            combat.SetCanonicalRangedSupportInfo(setup);
            if (setup.HasPrimaryProjectileBounce)
                combat.SetPromotedProjectileBounce(setup.PrimaryProjectileBounce);
            return true;
        }

        internal static bool ApplyCanonicalTargetAreaCombat(
            this PartyService party,
            AllyCombat combat,
            string baseUnitId)
        {
            if (combat == null || party.CanonicalTargetAreaCombat.TryResolve(baseUnitId, party.AllyAttackMultiplierState, out CompanionTargetAreaCombatSetup setup) == false)
                return false;

            CompanionGrowthScale growth = party.ResolveGrowthScale(baseUnitId);
            bool promoted = growth.VisualUnitCount == 3;
            if (baseUnitId == "bombardier" && promoted)
                setup = setup.WithPromotedPowderCaptainImpact();

            setup = setup.WithGrowthScale(growth).WithPassiveModifiers(party.ResolvePassiveCombatModifiers(baseUnitId));
            combat.BindParty(party);
            combat.SetCanonicalTargetAreaInfo(setup);
            if (baseUnitId == "skeleton_bomber" && promoted)
                combat.SetPromotedTargetAreaFollowUp(setup.CreatePromotedBoneArtilleryFollowUp());
            return true;
        }

        internal static bool ApplyCanonicalPersistentFieldCombat(this PartyService party, AllyCombat combat, string baseUnitId)
        {
            if (combat == null || party.CanonicalPersistentFieldCombat.TryResolve(baseUnitId, party.AllyAttackMultiplierState, out CompanionPersistentFieldCombatSetup setup) == false)
                return false;

            CompanionGrowthScale growth = party.ResolveGrowthScale(baseUnitId);
            if (baseUnitId == "fire_mage" && growth.VisualUnitCount == 3)
                setup = setup.WithPromotedFireSageField();

            setup = setup.WithGrowthScale(growth).WithPassiveModifiers(party.ResolvePassiveCombatModifiers(baseUnitId));
            combat.BindParty(party);
            combat.SetCanonicalPersistentFieldInfo(setup);
            return true;
        }

        internal static bool ApplyCanonicalChainCombat(this PartyService party, AllyCombat combat, string baseUnitId)
        {
            if (combat == null || party.CanonicalChainCombat.TryResolve(baseUnitId, party.AllyAttackMultiplierState, out CompanionChainCombatSetup setup) == false)
                return false;

            CompanionGrowthScale growth = party.ResolveGrowthScale(baseUnitId);
            if (baseUnitId == "lightning_mage" && growth.VisualUnitCount == 3)
                setup = setup.WithPromotedStormMageChain();

            setup = setup.WithGrowthScale(growth).WithPassiveModifiers(party.ResolvePassiveCombatModifiers(baseUnitId));
            combat.BindParty(party);
            combat.SetCanonicalChainInfo(setup);
            return true;
        }

        internal static bool ApplyCanonicalWolfCombat(this PartyService party, AllyCombat combat, string baseUnitId)
        {
            if (combat == null || party.CanonicalWolfOwnedProxyCombat.TryResolve(baseUnitId, out CompanionWolfOwnedProxyCombatSetup setup) == false)
                return false;

            CompanionGrowthScale growth = party.ResolveGrowthScale(baseUnitId);
            if (growth.VisualUnitCount == 3)
                setup = setup.WithPromotedBeastCommanderHits();

            setup = setup.WithGrowthScale(growth).WithPassiveModifiers(party.ResolvePassiveCombatModifiers(baseUnitId));
            combat.BindParty(party);
            combat.SetCanonicalWolfOwnedProxyInfo(setup);
            return true;
        }

        internal static bool ApplyCanonicalWraithCombat(this PartyService party, AllyCombat combat, string baseUnitId)
        {
            if (combat == null
                || baseUnitId != "wraith_knight"
                || party.CanonicalMeleeCombat.TryResolveWraithMeleeDefense(
                    party.AllyAttackMultiplierState,
                    out CompanionWraithMeleeDefenseSetup setup) == false)
            {
                return false;
            }

            CompanionGrowthScale growth = party.ResolveGrowthScale(baseUnitId);
            if (growth.VisualUnitCount == 3)
                setup = setup.WithPromotedWraithGuardianDefense().WithPromotedWraithGuardianGeometry();

            CompanionMeleeCombatSetup scaled = setup.Melee
                .WithGrowthScale(growth)
                .WithPassiveModifiers(party.ResolvePassiveCombatModifiers(baseUnitId));
            combat.BindParty(party);
            combat.SetCanonicalWraithMeleeDefenseInfo(new CompanionWraithMeleeDefenseSetup(scaled, setup.PersonalDefense));
            return true;
        }

        internal static void RefreshShieldSoldierAreaPushTest(this PartyService party)
        {
            UnitData unitData = party.Data.GetUnit("shield_guard");
            if (unitData == null)
                return;

            for (int i = 0; i < party.Companions.Count; i++)
            {
                CompanionRuntime companion = party.Companions[i];
                if (companion == null || companion.UnitId != "shield_guard")
                    continue;

                AllyCombat combat = companion.GetComponent<AllyCombat>();
                party.ApplyCanonicalMeleeCombat(combat, "shield_guard");
            }
        }

        internal static AllyAttackStyle ResolveAttackStyle(
            this PartyService party,
            UnitData unitData,
            string skillKind,
            AllyAttackStyle fallbackAttackStyle)
        {
            if (party.IsShieldSoldierAreaPushTest(unitData, skillKind))
                return AllyAttackStyle.AreaPulse;

            return skillKind switch
            {
                "single_target_knockback" => AllyAttackStyle.ForwardPush,
                "area_knockback" => AllyAttackStyle.ForwardPush,
                "front_slash" => AllyAttackStyle.ForwardSlash,
                "heal_lowest" => AllyAttackStyle.HealCommander,
                "farthest_target" => AllyAttackStyle.FarthestTarget,
                _ => fallbackAttackStyle,
            };
        }

        internal static bool IsShieldSoldierAreaPushTest(this PartyService party, UnitData unitData, string skillKind)
        {
            return P0CombatDebugSettings.ShieldSoldierAreaPushTestEnabled
                && unitData != null
                && unitData.Id == "shield_guard"
                && skillKind == "single_target_knockback";
        }

        internal static bool IsShieldSoldierAreaPushTest(this PartyService party, string baseUnitId)
        {
            return P0CombatDebugSettings.ShieldSoldierAreaPushTestEnabled
                && baseUnitId == "shield_guard";
        }

        internal static float ResolveDefaultAttackAngle(this PartyService party, AllyAttackStyle attackStyle)
        {
            return attackStyle switch
            {
                AllyAttackStyle.ForwardPush => 85.0f,
                AllyAttackStyle.ForwardSlash => 60.0f,
                _ => 60.0f,
            };
        }

        internal static void RefreshAllCompanionCombat(this PartyService party)
        {
            for (int i = 0; i < party.Companions.Count; i++)
            {
                CompanionRuntime companion = party.Companions[i];
                if (companion == null)
                    continue;

                AllyCombat combat = companion.GetComponent<AllyCombat>();
                string canonicalBaseUnitId = companion.BaseUnitId switch
                {
                    "archer" => "falcon_archer",
                    "shield_captain" => "shield_guard",
                    _ => companion.BaseUnitId,
                };
                if (party.ApplyCanonicalWraithCombat(combat, canonicalBaseUnitId) == false
                    && party.ApplyCanonicalMeleeCombat(combat, canonicalBaseUnitId) == false
                    && party.ApplyCanonicalProjectileCombat(combat, canonicalBaseUnitId) == false
                    && party.ApplyCanonicalTargetAreaCombat(combat, canonicalBaseUnitId) == false
                    && party.ApplyCanonicalPersistentFieldCombat(combat, canonicalBaseUnitId) == false
                    && party.ApplyCanonicalChainCombat(combat, canonicalBaseUnitId) == false
                    && party.ApplyCanonicalWolfCombat(combat, canonicalBaseUnitId) == false
                    && party.ApplyCanonicalRangedSupportCombat(combat, canonicalBaseUnitId) == false)
                {
                    UnitData unitData = party.Data.GetUnit(companion.UnitId);
                    if (unitData != null)
                        party.ApplyCombatFromData(combat, unitData, AllyAttackStyle.SingleTarget);
                }
                CompanionGrowthScale scale = party.ResolveGrowthScale(canonicalBaseUnitId);
                companion.ApplyGrowthScale(scale);
            }
        }
    }
}
