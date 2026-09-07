using System;
using Lizzo.PV.Legion.Party.Roster;
using Lizzo.PV.P0.Cards;
using Lizzo.PV.P0.Units;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    public sealed partial class PartyService
    {
        private PassiveRosterState _passiveRoster;
        private CompanionPassiveCombatResolver _passiveCombat;
        private CompanionFirstPromotionCombatRunModule _firstPromotionCombatRunModule;
        private CompanionSecondPromotionCombatRunModule _secondPromotionCombatRunModule;
        private CompanionThirdPromotionCombatRunModule _thirdPromotionCombatRunModule;

        internal void BindPassiveRoster(PassiveRosterState passiveRoster, CompanionPassiveCombatResolver passiveEffects = null)
        {
            if (ReferenceEquals(_passiveRoster, passiveRoster)) return;
            if (_passiveRoster != null) _passiveRoster.Changed -= RefreshAllCompanionCombat;
            _passiveRoster = passiveRoster ?? throw new ArgumentNullException(nameof(passiveRoster));
            _passiveCombat = passiveEffects ?? new CompanionPassiveCombatResolver(_data, _passiveRoster);
            _passiveRoster.Changed += RefreshAllCompanionCombat;
            RefreshAllCompanionCombat();
        }

        internal void BindFirstPromotionCombatRunModule(CompanionFirstPromotionCombatRunModule module)
        {
            _firstPromotionCombatRunModule = module ?? throw new ArgumentNullException(nameof(module));
        }

        internal void UnbindFirstPromotionCombatRunModule(CompanionFirstPromotionCombatRunModule module)
        {
            if (ReferenceEquals(_firstPromotionCombatRunModule, module))
                _firstPromotionCombatRunModule = null;
        }

        internal void BindSecondPromotionCombatRunModule(CompanionSecondPromotionCombatRunModule module)
        {
            _secondPromotionCombatRunModule = module ?? throw new ArgumentNullException(nameof(module));
        }

        internal void UnbindSecondPromotionCombatRunModule(CompanionSecondPromotionCombatRunModule module)
        {
            if (ReferenceEquals(_secondPromotionCombatRunModule, module))
                _secondPromotionCombatRunModule = null;
        }

        internal void BindThirdPromotionCombatRunModule(CompanionThirdPromotionCombatRunModule module)
        {
            _thirdPromotionCombatRunModule = module ?? throw new ArgumentNullException(nameof(module));
        }

        internal void UnbindThirdPromotionCombatRunModule(CompanionThirdPromotionCombatRunModule module)
        {
            if (ReferenceEquals(_thirdPromotionCombatRunModule, module))
                _thirdPromotionCombatRunModule = null;
        }

        internal void ReportReturningAttackHit(CompanionRuntime runtime)
        {
            _thirdPromotionCombatRunModule?.ReportReturningAttackHit(runtime);
        }

        internal CompanionPassiveCombatModifiers ResolvePassiveCombatModifiers(string baseUnitId)
        {
            return _passiveCombat == null ? CompanionPassiveCombatModifiers.Identity : _passiveCombat.Resolve(baseUnitId);
        }

        internal CommanderPassiveModifiers ResolveCommanderPassiveModifiers()
        {
            return _passiveCombat == null ? CommanderPassiveModifiers.Identity : _passiveCombat.ResolveCommander();
        }

        internal float ResolveCompanionAttackIntervalDivisor(CompanionRuntime companion)
        {
            return _firstPromotionCombatRunModule?.GetAttackIntervalDivisor(companion, Time.time) ?? 1.0f;
        }

        public float AddAllyAttackBonus(float bonusRatio)
        {
            AllyAttackMultiplierState = Mathf.Max(1.0f, AllyAttackMultiplierState + Mathf.Max(0.0f, bonusRatio));
            RefreshAllCompanionCombat();
            return AllyAttackMultiplierState;
        }

        internal void ResetCardModifiers()
        {
            AllyAttackMultiplierState = 1.0f;
        }

        internal void RefreshAllCompanionCombat() => CompanionCombatSetupModule.RefreshAllCompanionCombat(this);
    }
}
