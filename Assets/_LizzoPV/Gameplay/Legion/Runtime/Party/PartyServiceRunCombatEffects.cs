using System;
using Lizzo.PV.Gameplay.RunTraits;
using Lizzo.PV.Legion.Party.Roster;
using Lizzo.PV.Legion.Synergy;
using Lizzo.PV.P0.Cards;
using Lizzo.PV.P0.Units;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    public sealed partial class PartyService
    {
        private PassiveRosterState _passiveRoster;
        private CompanionPassiveCombatResolver _passiveCombat;
        private SynergyActivationState _synergies;
        private HealingBondRunModule _healingBondRunModule;
        private MixedCommandRunModule _mixedCommandRunModule;
        private RunTraitEffectCoordinator _runTraitEffects;
        private SynergyTriggerState _synergyTriggers;
        private Build1SynergyProgression _build1SynergyProgression;

        internal void BindPassiveRoster(PassiveRosterState passiveRoster, CompanionPassiveCombatResolver passiveEffects = null)
        {
            if (ReferenceEquals(_passiveRoster, passiveRoster)) return;
            if (_passiveRoster != null) _passiveRoster.Changed -= RefreshAllCompanionCombat;
            _passiveRoster = passiveRoster ?? throw new ArgumentNullException(nameof(passiveRoster));
            _passiveCombat = passiveEffects ?? new CompanionPassiveCombatResolver(_data, _passiveRoster);
            _passiveRoster.Changed += RefreshAllCompanionCombat;
            RefreshAllCompanionCombat();
        }

        internal void BindSynergyActivationState(SynergyActivationState synergies)
        {
            _synergies = synergies ?? throw new ArgumentNullException(nameof(synergies));
            RefreshSynergyActivations();
        }

        internal void BindBuild1SynergyProgression(Build1SynergyProgression progression)
        {
            _build1SynergyProgression = progression ?? throw new ArgumentNullException(nameof(progression));
            RefreshSynergyActivations();
        }

        internal void UnbindBuild1SynergyProgression(Build1SynergyProgression progression)
        {
            if (ReferenceEquals(_build1SynergyProgression, progression))
                _build1SynergyProgression = null;
        }

        internal void BindHealingBondRunModule(HealingBondRunModule module)
        {
            _healingBondRunModule = module ?? throw new ArgumentNullException(nameof(module));
        }

        internal void UnbindHealingBondRunModule(HealingBondRunModule module)
        {
            if (ReferenceEquals(_healingBondRunModule, module))
                _healingBondRunModule = null;
        }

        internal void BindMixedCommandRunModule(MixedCommandRunModule module)
        {
            _mixedCommandRunModule = module ?? throw new ArgumentNullException(nameof(module));
        }

        internal void UnbindMixedCommandRunModule(MixedCommandRunModule module)
        {
            if (ReferenceEquals(_mixedCommandRunModule, module))
                _mixedCommandRunModule = null;
        }

        internal void BindRunTraitEffectCoordinator(
            RunTraitEffectCoordinator coordinator,
            SynergyTriggerState synergyTriggers)
        {
            _runTraitEffects = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
            _synergyTriggers = synergyTriggers ?? throw new ArgumentNullException(nameof(synergyTriggers));
            _synergyTriggers.BindRunTraitEffectCoordinator(_runTraitEffects);
        }

        internal void UnbindRunTraitEffectCoordinator(RunTraitEffectCoordinator coordinator)
        {
            if (ReferenceEquals(_runTraitEffects, coordinator))
            {
                _synergyTriggers?.UnbindRunTraitEffectCoordinator(coordinator);
                _synergyTriggers = null;
                _runTraitEffects = null;
            }
        }

        internal void HandlePromotionCommitted(PartyRosterChangeResult rosterCommit, float now)
        {
            if (rosterCommit == PartyRosterChangeResult.Promote)
                _runTraitEffects?.ReportPromotionCommitted(now);
        }

        internal bool ReportHealingBond(CompanionRuntime companion, in SynergyHealingEvent healingEvent)
        {
            return _healingBondRunModule != null && _healingBondRunModule.ReportHealing(companion, healingEvent);
        }

        internal bool ReportHealingBond(PlayerController player, in SynergyHealingEvent healingEvent)
        {
            return _healingBondRunModule != null && _healingBondRunModule.ReportHealing(player, healingEvent);
        }

        internal CompanionPassiveCombatModifiers ResolvePassiveCombatModifiers(string baseUnitId)
        {
            return _passiveCombat == null ? CompanionPassiveCombatModifiers.Identity : _passiveCombat.Resolve(baseUnitId);
        }

        internal CommanderPassiveModifiers ResolveCommanderPassiveModifiers()
        {
            return _passiveCombat == null ? CommanderPassiveModifiers.Identity : _passiveCombat.ResolveCommander();
        }

        internal float ResolveCompanionAttackIntervalDivisorForSource(string sourceId)
        {
            if (TryResolveCompanionSource(sourceId, out CompanionRuntime companion) == false)
                return 1.0f;

            return ResolveCompanionAttackIntervalDivisor(companion);
        }

        internal bool TryResolveCompanionSource(string sourceId, out CompanionRuntime result)
        {
            result = null;
            if (string.IsNullOrEmpty(sourceId))
                return false;

            for (int index = 0; index < Companions.Count; index++)
            {
                CompanionRuntime companion = Companions[index];
                if (companion != null
                    && companion.IsDown == false
                    && (companion.UnitId == sourceId || companion.BaseUnitId == sourceId))
                {
                    result = companion;
                    return true;
                }
            }

            return false;
        }

        internal bool TryResolveActiveCompanionWithFamilyTag(string familyTag, out CompanionRuntime companion)
        {
            for (int index = 0; index < Companions.Count; index++)
            {
                CompanionRuntime candidate = Companions[index];
                if (candidate != null && candidate.IsDown == false && HasFamilyTag(candidate.FamilyTags, familyTag))
                {
                    companion = candidate;
                    return true;
                }
            }

            companion = null;
            return false;
        }

        internal float ResolveCompanionAttackIntervalDivisor(CompanionRuntime companion)
        {
            float mixedCommandDivisor = _mixedCommandRunModule?.GetAttackIntervalDivisor(companion) ?? 1.0f;
            float promotionShoutDivisor = companion == null || companion.IsDown
                ? 1.0f
                : _runTraitEffects?.GetCompanionAttackIntervalDivisor(Time.time) ?? 1.0f;
            return mixedCommandDivisor * promotionShoutDivisor;
        }

        internal float ResolveCompanionMoveSpeedMultiplier(CompanionRuntime companion)
        {
            float mixedCommandMultiplier = _mixedCommandRunModule?.GetMoveSpeedMultiplier(companion) ?? 1.0f;
            float build1ReadyMultiplier = _build1SynergyProgression?.GetMoveSpeedMultiplier(companion) ?? 1.0f;
            float emergencyRallyMultiplier = companion == null || companion.IsDown
                ? 1.0f
                : _runTraitEffects?.GetEmergencyRallyMoveSpeedMultiplier(companion.RosterSlotId, Time.time) ?? 1.0f;
            return mixedCommandMultiplier * build1ReadyMultiplier * emergencyRallyMultiplier;
        }

        public float AddAllyAttackBonus(float bonusRatio)
        {
            AllyAttackMultiplierState = Mathf.Max(1.0f, AllyAttackMultiplierState + Mathf.Max(0.0f, bonusRatio));
            RefreshAllCompanionCombat();
            return AllyAttackMultiplierState;
        }

        public float AddGuardWallBonus(float bonusRatio)
        {
            GuardWallBonusMultiplierState = Mathf.Max(1.0f, GuardWallBonusMultiplierState + Mathf.Max(0.0f, bonusRatio));
            return GuardWallBonusMultiplierState;
        }

        internal void ResetCardModifiers()
        {
            AllyAttackMultiplierState = 1.0f;
            GuardWallBonusMultiplierState = 1.0f;
        }

        internal void RefreshSynergyActivations()
        {
            _synergies?.Refresh(_roster.Snapshot);
            _build1SynergyProgression?.Refresh(_roster.Snapshot);
        }

        internal void RefreshAllCompanionCombat() => CompanionCombatSetupModule.RefreshAllCompanionCombat(this);
    }
}
