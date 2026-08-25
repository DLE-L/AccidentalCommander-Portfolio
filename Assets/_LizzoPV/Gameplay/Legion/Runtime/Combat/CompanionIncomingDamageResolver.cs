using System.Collections.Generic;
using Lizzo.PV.Combat;
using Lizzo.PV.Gameplay.RunTraits;
using Lizzo.PV.Legion.Party.Roster;
using Lizzo.PV.Legion.Synergy;
using Lizzo.PV.P0.Skills.Guard;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    internal readonly struct CompanionIncomingDamageResolution
    {
        internal CompanionIncomingDamageResolution(
            int appliedDamage,
            int guardShockwavePreventedDamage,
            int healingBondPreventedDamage)
        {
            AppliedDamage = appliedDamage;
            GuardShockwavePreventedDamage = guardShockwavePreventedDamage;
            HealingBondPreventedDamage = healingBondPreventedDamage;
        }

        internal int AppliedDamage { get; }
        internal int GuardShockwavePreventedDamage { get; }
        internal int HealingBondPreventedDamage { get; }
    }

    internal sealed class CompanionIncomingDamageResolver
    {
        private const string SHIELD_CAPTAIN_PROMOTION_PROTECTION_SOURCE = "shield_captain_promotion_protection";
        private const float SHIELD_CAPTAIN_PROMOTION_DAMAGE_MULTIPLIER = 0.90f;
        private const float SHIELD_CAPTAIN_PROMOTION_PROTECTION_DURATION = 1.5f;
        private const float GUARD_SHOCKWAVE_DAMAGE_MULTIPLIER = 0.75f;
        private const float MINIMUM_INCOMING_DAMAGE_MULTIPLIER = 0.40f;

        private readonly CompanionProtectionWindow _shieldCaptainPromotionProtection;
        private readonly GuardShockwaveProtectionWindow _guardShockwaveProtection = new GuardShockwaveProtectionWindow();

        internal CompanionIncomingDamageResolver()
        {
            _shieldCaptainPromotionProtection = new CompanionProtectionWindow(
                new CompanionProtectionWindowSetup(
                    SHIELD_CAPTAIN_PROMOTION_PROTECTION_SOURCE,
                    SHIELD_CAPTAIN_PROMOTION_DAMAGE_MULTIPLIER,
                    SHIELD_CAPTAIN_PROMOTION_PROTECTION_DURATION));
        }

        internal bool TryActivateShieldCaptainPromotionProtection(
            PartyRosterChangeResult rosterCommit,
            string baseUnitId,
            float currentTime)
        {
            if (rosterCommit != PartyRosterChangeResult.Promote || baseUnitId != "shield_guard")
                return false;

            return _shieldCaptainPromotionProtection.TryActivateOnce(currentTime);
        }

        internal void ApplyGuardShockwaveProtection(
            IReadOnlyList<CompanionRuntime> companions,
            float duration,
            float currentTime)
        {
            if (companions == null)
                return;

            for (int i = 0; i < companions.Count; i++)
            {
                CompanionRuntime companion = companions[i];
                if (companion != null && companion.IsDown == false)
                    _guardShockwaveProtection.Refresh(companion.GetInstanceID(), duration, currentTime);
            }
        }

        internal float ResolveIncomingDamageMultiplier(
            CompanionRuntime companion,
            float currentTime,
            HealingBondRunModule healingBond)
        {
            return ResolveIncomingDamageMultiplier(
                companion,
                currentTime,
                healingBond,
                includeGuardShockwave: true,
                includeHealingBond: true);
        }

        internal CompanionIncomingDamageResolution Resolve(
            CompanionRuntime companion,
            int originalDamage,
            int currentHp,
            float currentTime,
            HealingBondRunModule healingBond,
            RunTraitEffectCoordinator runTraitEffects)
        {
            if (companion == null || originalDamage <= 0 || currentHp <= 0)
                return new CompanionIncomingDamageResolution(0, 0, 0);

            int noSynergyApplied = ResolveAppliedDamage(
                companion,
                originalDamage,
                currentHp,
                currentTime,
                healingBond,
                includeGuardShockwave: false,
                includeHealingBond: false);
            int guardOnlyApplied = ResolveAppliedDamage(
                companion,
                originalDamage,
                currentHp,
                currentTime,
                healingBond,
                includeGuardShockwave: true,
                includeHealingBond: false);
            int healingOnlyApplied = ResolveAppliedDamage(
                companion,
                originalDamage,
                currentHp,
                currentTime,
                healingBond,
                includeGuardShockwave: false,
                includeHealingBond: true);
            int bothApplied = ResolveAppliedDamage(
                companion,
                originalDamage,
                currentHp,
                currentTime,
                healingBond,
                includeGuardShockwave: true,
                includeHealingBond: true);
            DamagePreventionAllocation allocation = DamageContributionLedger.CalculatePreventionAllocation(
                noSynergyApplied,
                guardOnlyApplied,
                healingOnlyApplied,
                bothApplied);
            int appliedDamage = bothApplied;
            if (companion.IsDown == false && runTraitEffects != null)
            {
                int postMitigationDamage = ResolvePostMitigationDamage(
                    companion,
                    originalDamage,
                    currentTime,
                    healingBond,
                    includeGuardShockwave: true,
                    includeHealingBond: true);
                appliedDamage = runTraitEffects.ResolveEmergencyRallyPostMitigationDamage(
                    companion.RosterSlotId,
                    postMitigationDamage,
                    currentTime,
                    out _);
                appliedDamage = Mathf.Min(currentHp, appliedDamage);
            }

            return new CompanionIncomingDamageResolution(
                appliedDamage,
                allocation.GuardShockwave,
                allocation.HealingBond);
        }

        internal bool HasGuardShockwaveProtection(CompanionRuntime companion, float currentTime)
        {
            return companion != null
                && _guardShockwaveProtection.IsActive(companion.GetInstanceID(), currentTime);
        }

        internal bool HasHealingBondKnockdownImmunity(
            CompanionRuntime companion,
            HealingBondRunModule healingBond)
        {
            return healingBond != null && healingBond.HasKnockdownImmunity(companion);
        }

        internal void RemoveGuardShockwaveProtection(CompanionRuntime companion)
        {
            if (companion != null)
                _guardShockwaveProtection.Remove(companion.GetInstanceID());
        }

        internal void Reset()
        {
            _shieldCaptainPromotionProtection.Reset();
            _guardShockwaveProtection.Reset();
        }

        private int ResolveAppliedDamage(
            CompanionRuntime companion,
            int originalDamage,
            int currentHp,
            float currentTime,
            HealingBondRunModule healingBond,
            bool includeGuardShockwave,
            bool includeHealingBond)
        {
            int resolvedDamage = ResolvePostMitigationDamage(
                companion,
                originalDamage,
                currentTime,
                healingBond,
                includeGuardShockwave,
                includeHealingBond);
            return Mathf.Min(currentHp, resolvedDamage);
        }

        private int ResolvePostMitigationDamage(
            CompanionRuntime companion,
            int originalDamage,
            float currentTime,
            HealingBondRunModule healingBond,
            bool includeGuardShockwave,
            bool includeHealingBond)
        {
            float multiplier = ResolveIncomingDamageMultiplier(
                companion,
                currentTime,
                healingBond,
                includeGuardShockwave,
                includeHealingBond);
            return Mathf.Max(0, Mathf.RoundToInt(originalDamage * multiplier));
        }

        private float ResolveIncomingDamageMultiplier(
            CompanionRuntime companion,
            float currentTime,
            HealingBondRunModule healingBond,
            bool includeGuardShockwave,
            bool includeHealingBond)
        {
            if (companion == null)
                return 1.0f;

            float multiplier = Mathf.Clamp(companion.IncomingDamageMultiplier, 0.0f, 1.0f);
            if (_shieldCaptainPromotionProtection.IsActive(currentTime))
                multiplier *= SHIELD_CAPTAIN_PROMOTION_DAMAGE_MULTIPLIER;
            if (includeGuardShockwave && HasGuardShockwaveProtection(companion, currentTime))
                multiplier *= GUARD_SHOCKWAVE_DAMAGE_MULTIPLIER;

            multiplier *= Mathf.Clamp(GuardSquadSkillBehaviour.CompanionDamageMultiplier, 0.0f, 1.0f);
            if (includeHealingBond)
                multiplier *= healingBond?.GetDamageTakenMultiplier(companion) ?? 1.0f;
            return Mathf.Max(MINIMUM_INCOMING_DAMAGE_MULTIPLIER, multiplier);
        }
    }
}
