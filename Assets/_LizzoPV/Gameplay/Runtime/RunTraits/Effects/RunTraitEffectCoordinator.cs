using System;
using System.Collections.Generic;

namespace Lizzo.PV.Gameplay.RunTraits
{
    public sealed class RunTraitEffectCoordinator : IDisposable
    {
        readonly RunTraitRunState _runTraits;
        readonly PromotionShoutRunModule _promotionShout;
        readonly EliteFewRunModule _eliteFew;
        readonly DangerousMarchRunModule _dangerousMarch;
        readonly MomentOfCompletionRunModule _momentOfCompletion;
        readonly EmergencyRallyRunModule _emergencyRally;
        bool _disposed;

        public RunTraitEffectCoordinator(RunTraitRunState runTraits)
        {
            _runTraits = runTraits ?? throw new ArgumentNullException(nameof(runTraits));
            _promotionShout = new PromotionShoutRunModule();
            _eliteFew = new EliteFewRunModule();
            _dangerousMarch = new DangerousMarchRunModule();
            _momentOfCompletion = new MomentOfCompletionRunModule();
            _emergencyRally = new EmergencyRallyRunModule();
        }

        public bool ContainsSelectedTrait(string traitId)
        {
            return _disposed == false && _runTraits.Contains(traitId);
        }

        public void ReportPromotionCommitted(float now)
        {
            if (ContainsSelectedTrait(RunTraitIds.PromotionShout))
                _promotionShout.OnPromotionCommitted(now);
        }

        public float GetCompanionAttackIntervalDivisor(float now)
        {
            return ContainsSelectedTrait(RunTraitIds.PromotionShout)
                ? _promotionShout.GetAttackIntervalDivisor(now)
                : 1.0f;
        }

        public float GetCommanderAttackIntervalMultiplier(int activeSlotCount, int slotCapacity)
        {
            return ContainsSelectedTrait(RunTraitIds.EliteFew)
                ? _eliteFew.GetAttackIntervalMultiplier(activeSlotCount, slotCapacity)
                : 1.0f;
        }

        public float GetNormalSpawnDensityMultiplier()
        {
            return ContainsSelectedTrait(RunTraitIds.DangerousMarch)
                ? _dangerousMarch.GetNormalSpawnDensityMultiplier()
                : 1.0f;
        }

        public float GetGameplayExperienceMultiplier()
        {
            return ContainsSelectedTrait(RunTraitIds.DangerousMarch)
                ? _dangerousMarch.GetGameplayExperienceMultiplier()
                : 1.0f;
        }

        public int GetExplosionKillCounterIncrement()
        {
            return ContainsSelectedTrait(RunTraitIds.DangerousMarch)
                ? _dangerousMarch.GetExplosionKillCounterIncrement()
                : 1;
        }

        public int GetUndeadKillCounterIncrement()
        {
            return ContainsSelectedTrait(RunTraitIds.DangerousMarch)
                ? _dangerousMarch.GetUndeadKillCounterIncrement()
                : 1;
        }

        public int GetFirstSynergyActivationExecutionCreditCount(string synergyId)
        {
            return ContainsSelectedTrait(RunTraitIds.MomentOfCompletion)
                ? _momentOfCompletion.GetFirstActivationExecutionCreditCount(synergyId)
                : 1;
        }

        public bool TryActivateEmergencyRally(int currentHp, int maxHp, IReadOnlyList<string> rosterSlotIds, float now)
        {
            return ContainsSelectedTrait(RunTraitIds.EmergencyRally)
                && _emergencyRally.TryActivate(currentHp, maxHp, rosterSlotIds, now);
        }

        public float GetEmergencyRallyMoveSpeedMultiplier(string rosterSlotId, float now)
        {
            return ContainsSelectedTrait(RunTraitIds.EmergencyRally)
                ? _emergencyRally.GetMoveSpeedMultiplier(rosterSlotId, now)
                : 1.0f;
        }

        public int ResolveEmergencyRallyPostMitigationDamage(string rosterSlotId, int damage, float now, out int absorbedDamage)
        {
            if (ContainsSelectedTrait(RunTraitIds.EmergencyRally))
                return _emergencyRally.ResolvePostMitigationDamage(rosterSlotId, damage, now, out absorbedDamage);

            absorbedDamage = 0;
            return damage;
        }

        public void NotifyEmergencyRallyRecipientDown(string rosterSlotId)
        {
            if (_disposed == false)
                _emergencyRally.RemoveRecipient(rosterSlotId);
        }

        public void ResetRunState()
        {
            if (_disposed == false)
            {
                _promotionShout.Reset();
                _dangerousMarch.Reset();
                _momentOfCompletion.Reset();
                _emergencyRally.Reset();
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _promotionShout.Dispose();
            _dangerousMarch.Dispose();
            _momentOfCompletion.Dispose();
            _emergencyRally.Dispose();
            _disposed = true;
        }
    }
}
