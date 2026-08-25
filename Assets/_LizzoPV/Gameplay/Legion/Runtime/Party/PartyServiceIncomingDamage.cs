using Lizzo.PV.Combat;
using Lizzo.PV.Legion.Party.Roster;
using Lizzo.PV.Legion.Synergy;

namespace Lizzo.PV.Legion
{
    public sealed partial class PartyService
    {
        private readonly CompanionIncomingDamageResolver _incomingDamage;
        private DamageContributionLedger _damageContributions;

        internal bool TryActivateShieldCaptainPromotionProtection(
            PartyRosterChangeResult rosterCommit,
            string baseUnitId,
            float currentTime)
        {
            return _incomingDamage.TryActivateShieldCaptainPromotionProtection(
                rosterCommit,
                baseUnitId,
                currentTime);
        }

        internal void ApplyGuardShockwaveProtection(float duration, float currentTime)
        {
            _incomingDamage.ApplyGuardShockwaveProtection(Companions, duration, currentTime);
        }

        internal void BindDamageContributionLedger(DamageContributionLedger ledger)
        {
            _damageContributions = ledger ?? throw new System.ArgumentNullException(nameof(ledger));
        }

        internal void UnbindDamageContributionLedger(DamageContributionLedger ledger)
        {
            if (ReferenceEquals(_damageContributions, ledger))
                _damageContributions = null;
        }

        internal void RecordCompanionDamagePrevention(in CompanionIncomingDamageResolution resolution)
        {
            _damageContributions?.RecordPreventedDamage(SynergyActivationIds.GuardShockwave, resolution.GuardShockwavePreventedDamage);
            _damageContributions?.RecordPreventedDamage(SynergyActivationIds.HealingBond, resolution.HealingBondPreventedDamage);
        }

        internal CompanionIncomingDamageResolution ResolveCompanionIncomingDamage(
            CompanionRuntime companion,
            int originalDamage,
            int currentHp,
            float currentTime)
        {
            return _incomingDamage.Resolve(
                companion,
                originalDamage,
                currentHp,
                currentTime,
                _healingBondRunModule,
                _runTraitEffects);
        }

    }
}
