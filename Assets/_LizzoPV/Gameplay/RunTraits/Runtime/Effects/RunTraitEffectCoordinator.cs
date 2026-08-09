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
        readonly FuseLinkRunModule _fuseLink;
        readonly RuntimeObjectRegistry _registry;
        readonly CombatImmediateHitModule _immediateHits;
        readonly string[] _fuseLinkPrimaryEffectIds;
        readonly float _fuseLinkSecondaryRadius;
        readonly float _fuseLinkSecondaryDamageRatio;
        readonly int _fuseLinkSecondaryMaxTargets;
        readonly List<FuseLinkTarget> _fuseLinkTargets = new List<FuseLinkTarget>(6);
        int _lastEliteFewEmptySlots = int.MinValue;
        float _lastEliteFewIntervalMultiplier = float.NaN;
        bool _promotionShoutActive;
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
            _fuseLinkPrimaryEffectIds = Array.Empty<string>();
        }

        public RunTraitEffectCoordinator(RunTraitRunState runTraits, IDataProvider data, RuntimeObjectRegistry registry, ICombatImmediateHitModule immediateHits)
        {
            _runTraits = runTraits ?? throw new ArgumentNullException(nameof(runTraits));
            if (data == null) throw new ArgumentNullException(nameof(data));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _promotionShout = new PromotionShoutRunModule();
            _eliteFew = new EliteFewRunModule();
            _dangerousMarch = new DangerousMarchRunModule();
            _momentOfCompletion = new MomentOfCompletionRunModule();
            _emergencyRally = new EmergencyRallyRunModule();
            RunTuningData tuning = data.RunTuning;
            _fuseLink = new FuseLinkRunModule(tuning.FuseLinkFuseSeconds, tuning.FuseLinkSecondaryDamageRatio);
            _fuseLinkPrimaryEffectIds = tuning.FuseLinkPrimaryEffectIds.Split(',');
            _fuseLinkSecondaryRadius = tuning.FuseLinkSecondaryRadius;
            _fuseLinkSecondaryDamageRatio = tuning.FuseLinkSecondaryDamageRatio;
            _fuseLinkSecondaryMaxTargets = tuning.FuseLinkSecondaryMaxTargets;
            _immediateHits = immediateHits as CombatImmediateHitModule;
            if (_immediateHits != null)
                _immediateHits.Applied += OnImmediateHitApplied;
            _runTraits.TraitSelected += OnTraitSelected;
        }

        public bool ContainsSelectedTrait(string traitId)
        {
            return _disposed == false && _runTraits.Contains(traitId);
        }

        public void ReportPromotionCommitted(float now)
        {
            if (ContainsSelectedTrait(RunTraitIds.PromotionShout))
            {
                _promotionShout.OnPromotionCommitted(now);
                _promotionShoutActive = true;
                Build1RuntimeDiagnostics.Log("trait_effect_applied",
                    Build1RuntimeDiagnostics.Text("trait_id", RunTraitIds.PromotionShout),
                    Build1RuntimeDiagnostics.Text("promotion_slot_id", "unavailable"),
                    Build1RuntimeDiagnostics.Text("affected_living_count", "unavailable"),
                    Build1RuntimeDiagnostics.Float("attack_speed_multiplier", PromotionShoutRunModule.AttackIntervalDivisor),
                    Build1RuntimeDiagnostics.Float("duration", PromotionShoutRunModule.DurationSeconds));
            }
        }

        public float GetCompanionAttackIntervalDivisor(float now)
        {
            if (_promotionShoutActive && now >= _promotionShout.ExpiresAt)
            {
                _promotionShoutActive = false;
                Build1RuntimeDiagnostics.Log("trait_effect_expired",
                    Build1RuntimeDiagnostics.Text("trait_id", RunTraitIds.PromotionShout),
                    Build1RuntimeDiagnostics.Text("reason", "duration"));
            }
            return ContainsSelectedTrait(RunTraitIds.PromotionShout)
                ? _promotionShout.GetAttackIntervalDivisor(now)
                : 1.0f;
        }

        public float GetCommanderAttackIntervalMultiplier(int activeSlotCount, int slotCapacity)
        {
            if (ContainsSelectedTrait(RunTraitIds.EliteFew) == false)
                return 1.0f;

            float multiplier = _eliteFew.GetAttackIntervalMultiplier(activeSlotCount, slotCapacity);
            int emptySlotCount = Math.Max(0, slotCapacity - activeSlotCount);
            if (_lastEliteFewEmptySlots != emptySlotCount || Mathf.Approximately(_lastEliteFewIntervalMultiplier, multiplier) == false)
            {
                _lastEliteFewEmptySlots = emptySlotCount;
                _lastEliteFewIntervalMultiplier = multiplier;
                Build1RuntimeDiagnostics.Log("trait_effect_applied",
                    Build1RuntimeDiagnostics.Text("trait_id", RunTraitIds.EliteFew),
                    Build1RuntimeDiagnostics.Int("empty_slot_count", emptySlotCount),
                    Build1RuntimeDiagnostics.Float("attack_interval_multiplier", multiplier));
            }
            return multiplier;
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
                _dangerousMarch.Reset();
                _momentOfCompletion.Reset();
                _emergencyRally.Reset();
                _fuseLink?.Reset();
                _fuseLinkTargets.Clear();
                _promotionShoutActive = false;
                _emergencyRallyActive = false;
                _lastEliteFewEmptySlots = int.MinValue;
                _lastEliteFewIntervalMultiplier = float.NaN;
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
            if (_disposed || ContainsSelectedTrait(RunTraitIds.FuseLink) == false || request.IsFuseSecondary || IsFuseLinkPrimaryEffect(request.EffectId) == false)
                return;

            MonsterController trigger = request.Target as MonsterController;
            if (trigger == null || trigger.SpawnSequence <= 0L)
                return;

            if (_fuseLink == null)
                return;

            FuseLinkProcessOutcome outcome = _fuseLink.TryProcess(new FuseLinkPrimaryHit(trigger.SpawnSequence, request.Damage, true, false), Time.time, out FuseLinkSecondaryPlan plan, out float expiresAt);
            if (outcome == FuseLinkProcessOutcome.Detonated)
            {
                ResolveFuseSecondary(request, plan.Damage);
            }
            else if (outcome == FuseLinkProcessOutcome.FuseSet || outcome == FuseLinkProcessOutcome.FuseExpiredAndReset)
            {
                if (outcome == FuseLinkProcessOutcome.FuseExpiredAndReset)
                {
                    Build1RuntimeDiagnostics.Log("trait_effect_expired",
                        Build1RuntimeDiagnostics.Text("trait_id", RunTraitIds.FuseLink),
                        Build1RuntimeDiagnostics.Long("spawn_sequence", trigger.SpawnSequence),
                        Build1RuntimeDiagnostics.Text("reason", "natural_expiry_observed_on_primary"));
                }
                Build1RuntimeDiagnostics.Log("trait_effect_applied",
                    Build1RuntimeDiagnostics.Text("trait_id", RunTraitIds.FuseLink),
                    Build1RuntimeDiagnostics.Long("spawn_sequence", trigger.SpawnSequence),
                    Build1RuntimeDiagnostics.Text("source_id", request.SourceId),
                    Build1RuntimeDiagnostics.Int("authored_damage", request.Damage),
                    Build1RuntimeDiagnostics.Float("expires_at", expiresAt),
                    Build1RuntimeDiagnostics.Text("outcome", outcome.ToString()));
            }
        }

        bool IsFuseLinkPrimaryEffect(string effectId)
        {
            if (string.IsNullOrEmpty(effectId))
                return false;
            for (int i = 0; i < _fuseLinkPrimaryEffectIds.Length; i++)
            {
                if (_fuseLinkPrimaryEffectIds[i] == effectId)
                    return true;
            }
            return false;
        }

        void ResolveFuseSecondary(in CombatImmediateHitRequest trigger, int damage)
        {
            _fuseLinkTargets.Clear();
            float radiusSquared = _fuseLinkSecondaryRadius * _fuseLinkSecondaryRadius;
            Vector3 center = trigger.FeedbackPosition;
            foreach (MonsterController target in _registry.Enemies)
            {
                if (target == null || target.IsValid() == false || target.Hp <= 0 || target.SpawnSequence <= 0L)
                    continue;
                Vector3 point = target.transform.position;
                float distanceSquared = (point - center).sqrMagnitude;
                if (distanceSquared > radiusSquared)
                    continue;
                int index = 0;
                while (index < _fuseLinkTargets.Count && (_fuseLinkTargets[index].DistanceSquared < distanceSquared || (_fuseLinkTargets[index].DistanceSquared == distanceSquared && _fuseLinkTargets[index].SpawnSequence < target.SpawnSequence)))
                    index++;
                if (index < _fuseLinkSecondaryMaxTargets)
                    _fuseLinkTargets.Insert(index, new FuseLinkTarget(target, point, distanceSquared));
                if (_fuseLinkTargets.Count > _fuseLinkSecondaryMaxTargets)
                    _fuseLinkTargets.RemoveAt(_fuseLinkSecondaryMaxTargets);
            }
            int appliedTargetCount = 0;
            for (int i = 0; i < _fuseLinkTargets.Count; i++)
            {
                FuseLinkTarget candidate = _fuseLinkTargets[i];
                if (_immediateHits.TryApply(CombatImmediateHitRequest.CreateAllyDirectTarget(trigger.SourceId, candidate.Target, trigger.Origin, candidate.Point, damage, AttackVisualKind.AreaHit, false, default, null, true)))
                    appliedTargetCount++;
            }
            Build1RuntimeDiagnostics.Log("trait_effect_applied",
                Build1RuntimeDiagnostics.Text("trait_id", RunTraitIds.FuseLink),
                Build1RuntimeDiagnostics.Long("trigger_spawn_sequence", trigger.Target is MonsterController monster ? monster.SpawnSequence : 0L),
                Build1RuntimeDiagnostics.Text("source_id", trigger.SourceId),
                Build1RuntimeDiagnostics.Int("authored_damage", trigger.Damage),
                Build1RuntimeDiagnostics.Float("secondary_ratio", _fuseLinkSecondaryDamageRatio),
                Build1RuntimeDiagnostics.Float("radius", _fuseLinkSecondaryRadius),
                Build1RuntimeDiagnostics.Int("secondary_target_count", appliedTargetCount),
                Build1RuntimeDiagnostics.Int("secondary_damage", damage),
                Build1RuntimeDiagnostics.Bool("trigger_is_fuse_secondary", trigger.IsFuseSecondary),
                Build1RuntimeDiagnostics.Bool("countable_attribution", trigger.KillAttribution.IsCountable));
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

        readonly struct FuseLinkTarget
        {
            public FuseLinkTarget(MonsterController target, Vector3 point, float distanceSquared) { Target = target; Point = point; DistanceSquared = distanceSquared; SpawnSequence = target.SpawnSequence; }
            public MonsterController Target { get; }
            public Vector3 Point { get; }
            public float DistanceSquared { get; }
            public long SpawnSequence { get; }
        }
    }
}
