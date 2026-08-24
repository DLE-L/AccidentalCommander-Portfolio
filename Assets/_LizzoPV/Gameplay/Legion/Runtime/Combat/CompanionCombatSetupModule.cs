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

    public sealed partial class AllyCombat
    {
        public void SetInfo(AllyAttackStyle attackStyle, int damage, float period, float range, float knockback, float angle = 60.0f)
        {
            ClearCanonicalAbilitySchedules();
            _attackStyle = attackStyle;
            _damage = damage;
            _period = period;
            _range = Mathf.Max(range, MIN_ATTACK_RANGE);
            _knockback = knockback;
            _angle = angle;
            _maxForwardTargetCount = int.MaxValue;
            _maxProjectileTargetCount = 1;
            _noTargetRetrySeconds = NO_TARGET_RETRY_DELAY;
            _sourceIdOverride = null;
            _projectileSpeedMultiplier = 1.0f;
            _nextAttackTime = Time.time + Random.Range(0.1f, 0.35f);
        }

        public void SetCanonicalWolfOwnedProxyInfo(CompanionWolfOwnedProxyCombatSetup setup)
        {
            ClearCanonicalAbilitySchedules();
            _wolfSetup = setup;
            _wolfState = new WolfOwnedProxyState();
            _attackStyle = AllyAttackStyle.SingleTarget;
            _damage = setup.Damage;
            _period = setup.Period;
            _range = setup.SearchRange;
            _sourceIdOverride = setup.SourceId;
            _noTargetRetrySeconds = setup.NoTargetRetrySeconds;
            _nextAttackTime = Time.time;
        }

        public void SetCanonicalWraithMeleeDefenseInfo(CompanionWraithMeleeDefenseSetup setup)
        {
            SetCanonicalMeleeInfo(setup.Melee);
            _personalMitigation = new PersonalDamageMitigationState();
            _personalMitigation.Configure(setup.PersonalDefense, Time.time);
        }

        public void SetPromotedMultiHitSequence(PromotedMultiHitSequence sequence)
        {
            _promotedMultiHitSequence = sequence;
        }

        public void SetPromotedProjectileBurst(PromotedProjectileBurst burst)
        {
            _promotedProjectileBurst = burst ?? throw new System.ArgumentNullException(nameof(burst));
        }

        public void SetPromotedProjectileBounce(CompanionProjectileBounceSetup bounce)
        {
            if (bounce.IsConfigured == false || _sourceIdOverride != bounce.SourceId)
                throw new System.InvalidOperationException("Projectile bounce requires the active canonical projectile source.");

            _promotedProjectileBounce = bounce;
        }

        public void ApplyGrowthScale(CompanionGrowthScale scale)
        {
            if (_chainAbilitySchedule != null)
            {
                _chainSetup = _chainSetup.WithGrowthScale(scale);
                _damage = _chainSetup.Damage;
                _period = _chainSetup.Period;
                _chainAbilitySchedule.ApplyIntervalMultiplier(scale.IntervalMultiplier);
                return;
            }

            if (_persistentFieldAbilitySchedule != null)
            {
                _persistentFieldSetup = _persistentFieldSetup.WithGrowthScale(scale);
                _damage = _persistentFieldSetup.Damage;
                _period = _persistentFieldSetup.Period;
                _persistentFieldAbilitySchedule.ApplyIntervalMultiplier(scale.IntervalMultiplier);
                return;
            }

            _damage = Mathf.Max(1, Mathf.RoundToInt(_damage * scale.EffectMultiplier));
            _period = Mathf.Max(0.01f, _period * scale.IntervalMultiplier);
            _secondaryHealAmount = _secondaryHealAmount > 0
                ? Mathf.Max(1, Mathf.RoundToInt(_secondaryHealAmount * scale.EffectMultiplier))
                : 0;
            if (_secondaryHealPeriodScalesWithGrowth)
            {
                _secondaryHealPeriod = Mathf.Max(0.01f, _secondaryHealPeriod * scale.IntervalMultiplier);
                _secondaryAbilitySchedule?.ApplyIntervalMultiplier(scale.IntervalMultiplier);
            }
        }

        public void SetCanonicalMeleeInfo(CompanionMeleeCombatSetup setup)
        {
            ClearCanonicalAbilitySchedules();
            _attackStyle = setup.AttackStyle;
            _damage = setup.Damage;
            _period = setup.Period;
            _range = Mathf.Max(setup.Range, MIN_ATTACK_RANGE);
            _knockback = setup.Knockback;
            _angle = setup.Angle;
            _maxForwardTargetCount = Mathf.Max(1, setup.MaxTargets);
            _maxProjectileTargetCount = 1;
            _noTargetRetrySeconds = Mathf.Max(0.0f, setup.NoTargetRetrySeconds);
            _sourceIdOverride = null;
            _projectileSpeedMultiplier = 1.0f;
            _nextAttackTime = Time.time + Random.Range(0.1f, 0.35f);
        }

        public void SetCanonicalProjectileInfo(CompanionProjectileCombatSetup setup)
        {
            ClearCanonicalAbilitySchedules();
            _attackStyle = setup.AttackStyle;
            _damage = setup.Damage;
            _period = setup.Period;
            _range = Mathf.Max(setup.Range, MIN_ATTACK_RANGE);
            _knockback = 0.0f;
            _angle = 0.0f;
            _maxForwardTargetCount = int.MaxValue;
            _maxProjectileTargetCount = Mathf.Max(1, setup.MaxTargets);
            _noTargetRetrySeconds = Mathf.Max(0.0f, setup.NoTargetRetrySeconds);
            _sourceIdOverride = setup.SourceId;
            _projectileSpeedMultiplier = setup.ProjectileSpeedMultiplier;
            _nextAttackTime = Time.time + Random.Range(0.1f, 0.35f);
        }

        public void SetCanonicalProjectileWithProxyInfo(
            CompanionProjectileCombatSetup setup,
            CompanionOwnedProxyCombatSetup proxy)
        {
            SetCanonicalProjectileInfo(setup);
            _ownedProxySetup = proxy;
            _ownedProxyCounter = new SuccessfulActionCounter();
            _ownedProxyCounter.Configure(proxy.TriggerCount);
        }

        public void SetCanonicalRangedSupportInfo(CompanionRangedSupportCombatSetup setup)
        {
            _promotedProjectileBounce = default;
            _attackStyle = setup.Primary.AttackStyle;
            _damage = setup.Primary.Damage;
            _period = setup.Primary.Period;
            _range = Mathf.Max(setup.Primary.Range, MIN_ATTACK_RANGE);
            _knockback = 0.0f;
            _angle = 0.0f;
            _maxForwardTargetCount = int.MaxValue;
            _maxProjectileTargetCount = Mathf.Max(1, setup.Primary.MaxTargets);
            _noTargetRetrySeconds = Mathf.Max(0.0f, setup.Primary.NoTargetRetrySeconds);
            _sourceIdOverride = setup.Primary.SourceId;
            _projectileSpeedMultiplier = setup.Primary.ProjectileSpeedMultiplier;
            _secondaryHealAmount = setup.SecondaryHealAmount;
            _secondaryHealPeriod = setup.SecondaryPeriod;
            _secondaryHealRange = Mathf.Max(setup.SecondaryRange, MIN_ATTACK_RANGE);
            _secondaryHealMaxTargets = Mathf.Max(1, setup.SecondaryMaxTargets);
            _secondaryHealSecondTargetRatio = Mathf.Clamp01(setup.SecondarySecondTargetRatio);
            _secondaryHealPeriodScalesWithGrowth = setup.SecondaryPeriodScalesWithGrowth;
            float now = Time.time;
            _primaryAbilitySchedule = new CombatAbilitySchedule();
            _secondaryAbilitySchedule = new CombatAbilitySchedule();
            _primaryAbilitySchedule.Configure(_period, _noTargetRetrySeconds, now, Random.Range(0.1f, 0.35f));
            _secondaryAbilitySchedule.Configure(
                setup.SecondaryPeriod,
                setup.SecondaryNoTargetRetrySeconds,
                now,
                Random.Range(0.1f, 0.35f));
            _nextAttackTime = float.PositiveInfinity;
        }

        public void SetCanonicalTargetAreaInfo(CompanionTargetAreaCombatSetup setup)
        {
            ClearCanonicalAbilitySchedules();
            _attackStyle = AllyAttackStyle.TargetedArea;
            _damage = setup.Damage;
            _period = setup.Period;
            _range = Mathf.Max(setup.Range, MIN_ATTACK_RANGE);
            _knockback = 0.0f;
            _angle = 0.0f;
            _maxForwardTargetCount = int.MaxValue;
            _maxProjectileTargetCount = 1;
            _noTargetRetrySeconds = Mathf.Max(0.0f, setup.NoTargetRetrySeconds);
            _sourceIdOverride = setup.SourceId;
            _projectileSpeedMultiplier = 1.0f;
            _targetAreaRadius = Mathf.Max(setup.Radius, MIN_ATTACK_RANGE);
            _targetAreaMaxTargets = Mathf.Max(1, setup.MaxTargets);
            _targetAreaNormalPush = setup.NormalPush;
            _targetAreaEliteBossPush = setup.EliteBossPush;
            _hasPromotedTargetAreaFollowUp = false;
            _targetAreaCastState = new TargetAreaCastState();
            _targetAreaCastState.Configure(setup, Time.time, Random.Range(0.1f, 0.35f));
            _nextAttackTime = float.PositiveInfinity;
        }

        public void SetPromotedTargetAreaFollowUp(PromotedTargetAreaFollowUpSetup setup)
        {
            if (_sourceIdOverride != setup.SourceId || setup.SourceId != "skeleton_bomber")
            {
                throw new System.InvalidOperationException(
                    "Bone Artillery follow-up requires the active skeleton_bomber target-area setup.");
            }

            _promotedTargetAreaFollowUp = setup;
            _hasPromotedTargetAreaFollowUp = true;
        }

        public void SetCanonicalPersistentFieldInfo(CompanionPersistentFieldCombatSetup setup)
        {
            ClearCanonicalAbilitySchedules();
            _attackStyle = AllyAttackStyle.TargetedField;
            _damage = setup.Damage;
            _period = setup.Period;
            _range = Mathf.Max(setup.Range, MIN_ATTACK_RANGE);
            _knockback = 0.0f;
            _angle = 0.0f;
            _maxForwardTargetCount = int.MaxValue;
            _maxProjectileTargetCount = 1;
            _noTargetRetrySeconds = Mathf.Max(0.0f, setup.NoTargetRetrySeconds);
            _sourceIdOverride = setup.SourceId;
            _projectileSpeedMultiplier = 1.0f;
            _persistentFieldSetup = setup;
            _persistentFieldAbilitySchedule = new CombatAbilitySchedule();
            _persistentFieldAbilitySchedule.Configure(
                _period,
                _noTargetRetrySeconds,
                Time.time,
                Random.Range(0.1f, 0.35f));
            _nextAttackTime = float.PositiveInfinity;
        }

        public void SetCanonicalChainInfo(CompanionChainCombatSetup setup)
        {
            ClearCanonicalAbilitySchedules();
            _attackStyle = AllyAttackStyle.TargetedChain;
            _damage = setup.Damage;
            _period = setup.Period;
            _range = Mathf.Max(setup.InitialRange, MIN_ATTACK_RANGE);
            _knockback = 0.0f;
            _angle = 0.0f;
            _maxForwardTargetCount = int.MaxValue;
            _maxProjectileTargetCount = 1;
            _noTargetRetrySeconds = setup.NoTargetRetrySeconds;
            _sourceIdOverride = setup.SourceId;
            _chainSetup = setup;
            _projectileSpeedMultiplier = 1.0f;
            _chainAbilitySchedule = new CombatAbilitySchedule();
            _chainAbilitySchedule.Configure(
                setup.Period,
                setup.NoTargetRetrySeconds,
                Time.time,
                Random.Range(0.1f, 0.35f));
            _nextAttackTime = float.PositiveInfinity;
        }

        private void ClearCanonicalAbilitySchedules()
        {
            _primaryAbilitySchedule = null;
            _secondaryAbilitySchedule = null;
            _targetAreaCastState = null;
            _persistentFieldAbilitySchedule = null;
            _chainAbilitySchedule = null;
            _persistentFieldSetup = default;
            _chainSetup = default;
            _ownedProxyCounter = null;
            _ownedProxySetup = default;
            _secondaryHealAmount = 0;
            _secondaryHealPeriod = 0.0f;
            _secondaryHealRange = 0.0f;
            _secondaryHealMaxTargets = 0;
            _secondaryHealSecondTargetRatio = 0.0f;
            _secondaryHealPeriodScalesWithGrowth = true;
            _supportHealTargets.Clear();
            _targetAreaRadius = 0.0f;
            _targetAreaMaxTargets = 0;
            _targetAreaNormalPush = 0.0f;
            _targetAreaEliteBossPush = 0.0f;
            _promotedMultiHitSequence = null;
            _promotedProjectileBurst = null;
            _promotedProjectileBounce = default;
        }
    }
}
