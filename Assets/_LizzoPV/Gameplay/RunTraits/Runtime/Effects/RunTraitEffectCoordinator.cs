using System;
using System.Collections.Generic;
using Lizzo.PV.Combat;
using Lizzo.PV.Data;
using Lizzo.PV.Gameplay.Diagnostics;
using Lizzo.PV.Legion;
using UnityEngine;

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
        readonly FuseLinkCombatRuntime _fuseLink;
        readonly RuntimeObjectRegistry _registry;
        readonly CombatImmediateHitModule _immediateHits;
        bool _emergencyRallyActive;
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

        public RunTraitEffectCoordinator(RunTraitRunState runTraits, IDataProvider data, RuntimeObjectRegistry registry, ICombatImmediateHitModule immediateHits)
        {
            _runTraits = runTraits ?? throw new ArgumentNullException(nameof(runTraits));
            if (data == null) throw new ArgumentNullException(nameof(data));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _promotionShout = new PromotionShoutRunModule(_registry);
            _eliteFew = new EliteFewRunModule();
            _dangerousMarch = new DangerousMarchRunModule();
            _momentOfCompletion = new MomentOfCompletionRunModule();
            _emergencyRally = new EmergencyRallyRunModule();
            RunTuningData tuning = data.RunTuning;
            _fuseLink = new FuseLinkCombatRuntime(tuning, _registry, immediateHits);
            _immediateHits = immediateHits as CombatImmediateHitModule;
            if (_immediateHits != null)
                _immediateHits.Applied += OnImmediateHitApplied;
            _runTraits.TraitSelected += OnTraitSelected;
        }

        public bool ContainsSelectedTrait(string traitId)
        {
            return _disposed == false && _runTraits.Contains(traitId);
        }

        public bool TryGetActiveDurationRatio(string traitId, float now, out float remainingRatio)
        {
            remainingRatio = 0.0f;
            if (_disposed)
                return false;

            if (traitId == RunTraitIds.PromotionShout)
                return _promotionShout.TryGetActiveDurationRatio(now, out remainingRatio);

            if (traitId == RunTraitIds.EmergencyRally)
                return TryGetActiveDurationRatio(_emergencyRallyActive, _emergencyRally.ExpiresAt, EmergencyRallyRunModule.DurationSeconds, now, out remainingRatio);

            return false;
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
            if (ContainsSelectedTrait(RunTraitIds.EliteFew) == false)
                return 1.0f;

            return _eliteFew.GetAttackIntervalMultiplier(activeSlotCount, slotCapacity);
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
            if (ContainsSelectedTrait(RunTraitIds.MomentOfCompletion) == false)
                return 1;

            int creditCount = _momentOfCompletion.GetFirstActivationExecutionCreditCount(synergyId);
            Build1RuntimeDiagnostics.Log(creditCount > 1 ? "trait_effect_applied" : "trait_effect_blocked",
                Build1RuntimeDiagnostics.Text("trait_id", RunTraitIds.MomentOfCompletion),
                Build1RuntimeDiagnostics.Text("synergy_id", synergyId),
                Build1RuntimeDiagnostics.Int("execution_credit_count", creditCount),
                Build1RuntimeDiagnostics.Bool("extra_credit_granted", creditCount > 1),
                Build1RuntimeDiagnostics.Text("pending_count", "unavailable"),
                Build1RuntimeDiagnostics.Text("block_reason", creditCount > 1 ? "none" : "second_use_or_unsupported"));
            return creditCount;
        }

        public bool TryActivateEmergencyRally(int currentHp, int maxHp, IReadOnlyList<string> rosterSlotIds, float now)
        {
            if (ContainsSelectedTrait(RunTraitIds.EmergencyRally) == false || _emergencyRally.TryActivate(currentHp, maxHp, rosterSlotIds, now) == false)
                return false;

            _emergencyRallyActive = true;
            Build1RuntimeDiagnostics.Log("trait_effect_applied",
                Build1RuntimeDiagnostics.Text("trait_id", RunTraitIds.EmergencyRally),
                Build1RuntimeDiagnostics.Float("commander_hp_ratio", maxHp > 0 ? (float)currentHp / maxHp : 0.0f),
                Build1RuntimeDiagnostics.Int("target_count", _emergencyRally.RecipientCount),
                Build1RuntimeDiagnostics.Float("move_multiplier", EmergencyRallyRunModule.MoveSpeedMultiplier),
                Build1RuntimeDiagnostics.Int("shield", EmergencyRallyRunModule.DamageAbsorptionPerRosterSlot),
                Build1RuntimeDiagnostics.Float("duration", EmergencyRallyRunModule.DurationSeconds));
            return true;
        }

        public float GetEmergencyRallyMoveSpeedMultiplier(string rosterSlotId, float now)
        {
            ReportEmergencyRallyExpiry(now);
            return ContainsSelectedTrait(RunTraitIds.EmergencyRally)
                ? _emergencyRally.GetMoveSpeedMultiplier(rosterSlotId, now)
                : 1.0f;
        }

        public int ResolveEmergencyRallyPostMitigationDamage(string rosterSlotId, int damage, float now, out int absorbedDamage)
        {
            if (ContainsSelectedTrait(RunTraitIds.EmergencyRally))
            {
                ReportEmergencyRallyExpiry(now);
                int remainingDamage = _emergencyRally.ResolvePostMitigationDamage(rosterSlotId, damage, now, out absorbedDamage);
                if (absorbedDamage > 0)
                {
                    Build1RuntimeDiagnostics.Log("trait_effect_applied",
                        Build1RuntimeDiagnostics.Text("trait_id", RunTraitIds.EmergencyRally),
                        Build1RuntimeDiagnostics.Text("roster_slot_id", rosterSlotId),
                        Build1RuntimeDiagnostics.Int("absorbed_damage", absorbedDamage),
                        Build1RuntimeDiagnostics.Int("remaining_pool", _emergencyRally.GetRemainingAbsorption(rosterSlotId)));
                }
                return remainingDamage;
            }

            absorbedDamage = 0;
            return damage;
        }

        public void NotifyEmergencyRallyRecipientDown(string rosterSlotId)
        {
            if (_disposed == false)
            {
                _emergencyRally.RemoveRecipient(rosterSlotId);
                Build1RuntimeDiagnostics.Log("trait_effect_expired",
                    Build1RuntimeDiagnostics.Text("trait_id", RunTraitIds.EmergencyRally),
                    Build1RuntimeDiagnostics.Text("roster_slot_id", rosterSlotId),
                    Build1RuntimeDiagnostics.Text("reason", "recipient_down"));
            }
        }

        public void ResetRunState()
        {
            if (_disposed == false)
            {
                _promotionShout.Reset();
                _eliteFew.Reset();
                _dangerousMarch.Reset();
                _momentOfCompletion.Reset();
                _emergencyRally.Reset();
                _fuseLink?.Reset();
                _emergencyRallyActive = false;
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
            if (_immediateHits != null)
                _immediateHits.Applied -= OnImmediateHitApplied;
            _runTraits.TraitSelected -= OnTraitSelected;
            _fuseLink?.Dispose();
            _disposed = true;
        }

        void OnImmediateHitApplied(CombatImmediateHitRequest request)
        {
            if (_disposed || ContainsSelectedTrait(RunTraitIds.FuseLink) == false || _fuseLink == null)
                return;

            _fuseLink.Process(in request, Time.time);
        }

        void OnTraitSelected(string traitId)
        {
            Build1RuntimeDiagnostics.Log("trait_selected",
                Build1RuntimeDiagnostics.Text("trait_id", traitId),
                Build1RuntimeDiagnostics.Int("selected_count", _runTraits.SelectionCount));
            if (traitId == RunTraitIds.DangerousMarch)
            {
                Build1RuntimeDiagnostics.Log("trait_effect_applied",
                    Build1RuntimeDiagnostics.Text("trait_id", traitId),
                    Build1RuntimeDiagnostics.Float("spawn_density", _dangerousMarch.GetNormalSpawnDensityMultiplier()),
                    Build1RuntimeDiagnostics.Float("exp", _dangerousMarch.GetGameplayExperienceMultiplier()),
                    Build1RuntimeDiagnostics.Float("synergy_kill_counter", _dangerousMarch.KillCounterMultiplier));
            }
        }

        void ReportEmergencyRallyExpiry(float now)
        {
            if (_emergencyRallyActive && now >= _emergencyRally.ExpiresAt)
            {
                _emergencyRallyActive = false;
                Build1RuntimeDiagnostics.Log("trait_effect_expired",
                    Build1RuntimeDiagnostics.Text("trait_id", RunTraitIds.EmergencyRally),
                    Build1RuntimeDiagnostics.Text("reason", "duration"));
            }
        }

        static bool TryGetActiveDurationRatio(bool isActive, float expiresAt, float durationSeconds, float now, out float remainingRatio)
        {
            remainingRatio = 0.0f;
            if (isActive == false || now >= expiresAt || durationSeconds <= 0.0f)
                return false;

            remainingRatio = Mathf.Clamp01((expiresAt - now) / durationSeconds);
            return true;
        }

    }
}
