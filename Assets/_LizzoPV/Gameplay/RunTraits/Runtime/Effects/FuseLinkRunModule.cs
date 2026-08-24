using System;
using System.Collections.Generic;
using Lizzo.PV.Combat;
using Lizzo.PV.Data;
using Lizzo.PV.Gameplay.Diagnostics;
using Lizzo.PV.Legion;
using UnityEngine;

namespace Lizzo.PV.Gameplay.RunTraits
{
    public enum FuseLinkProcessOutcome
    {
        Blocked,
        FuseSet,
        FuseExpiredAndReset,
        Detonated,
    }

    public readonly struct FuseLinkPrimaryHit
    {
        public FuseLinkPrimaryHit(long spawnSequence, int authoredDamage, bool isPrimaryExplosion, bool isFuseSecondary)
        {
            SpawnSequence = spawnSequence;
            AuthoredDamage = authoredDamage;
            IsPrimaryExplosion = isPrimaryExplosion;
            IsFuseSecondary = isFuseSecondary;
        }

        public long SpawnSequence { get; }
        public int AuthoredDamage { get; }
        public bool IsPrimaryExplosion { get; }
        public bool IsFuseSecondary { get; }
    }

    public readonly struct FuseLinkSecondaryPlan
    {
        public FuseLinkSecondaryPlan(long triggerSpawnSequence, int damage)
        {
            TriggerSpawnSequence = triggerSpawnSequence;
            Damage = damage;
        }

        public long TriggerSpawnSequence { get; }
        public int Damage { get; }
    }

    public sealed class FuseLinkRunModule : IDisposable
    {
        readonly Dictionary<long, float> _expiresAtBySpawnSequence = new Dictionary<long, float>(32);
        readonly float _fuseSeconds;
        readonly float _secondaryDamageRatio;
        bool _disposed;

        public FuseLinkRunModule(float fuseSeconds, float secondaryDamageRatio)
        {
            _fuseSeconds = fuseSeconds;
            _secondaryDamageRatio = secondaryDamageRatio;
        }

        public bool TryProcess(in FuseLinkPrimaryHit hit, float now, out FuseLinkSecondaryPlan plan)
        {
            return TryProcess(hit, now, out plan, out _) == FuseLinkProcessOutcome.Detonated;
        }

        public FuseLinkProcessOutcome TryProcess(in FuseLinkPrimaryHit hit, float now, out FuseLinkSecondaryPlan plan, out float expiresAt)
        {
            plan = default;
            expiresAt = 0.0f;
            if (_disposed || hit.IsPrimaryExplosion == false || hit.IsFuseSecondary || hit.SpawnSequence <= 0L || hit.AuthoredDamage <= 0)
                return FuseLinkProcessOutcome.Blocked;

            if (_expiresAtBySpawnSequence.TryGetValue(hit.SpawnSequence, out float existingExpiresAt))
            {
                if (now <= existingExpiresAt)
                {
                    _expiresAtBySpawnSequence.Remove(hit.SpawnSequence);
                    int damage = Math.Max(1, (int)MathF.Floor(hit.AuthoredDamage * _secondaryDamageRatio + 0.5f));
                    plan = new FuseLinkSecondaryPlan(hit.SpawnSequence, damage);
                    return FuseLinkProcessOutcome.Detonated;
                }

                _expiresAtBySpawnSequence.Remove(hit.SpawnSequence);
                float newExpiry = now + _fuseSeconds;
                _expiresAtBySpawnSequence.Add(hit.SpawnSequence, newExpiry);
                expiresAt = newExpiry;
                return FuseLinkProcessOutcome.FuseExpiredAndReset;
            }

            expiresAt = now + _fuseSeconds;
            _expiresAtBySpawnSequence.Add(hit.SpawnSequence, expiresAt);
            return FuseLinkProcessOutcome.FuseSet;
        }

        public void Reset()
        {
            _expiresAtBySpawnSequence.Clear();
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            Reset();
            _disposed = true;
        }
    }

    internal sealed class FuseLinkCombatRuntime : IDisposable
    {
        private readonly FuseLinkRunModule _core;
        private readonly RuntimeObjectRegistry _registry;
        private readonly ICombatImmediateHitModule _immediateHits;
        private readonly string[] _primaryEffectIds;
        private readonly float _secondaryRadius;
        private readonly float _secondaryDamageRatio;
        private readonly int _secondaryMaxTargets;
        private readonly List<FuseLinkTarget> _targets = new List<FuseLinkTarget>(6);
        private bool _disposed;

        internal FuseLinkCombatRuntime(
            RunTuningData tuning,
            RuntimeObjectRegistry registry,
            ICombatImmediateHitModule immediateHits)
        {
            if (tuning == null)
                throw new ArgumentNullException(nameof(tuning));

            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _immediateHits = immediateHits;
            _core = new FuseLinkRunModule(tuning.FuseLinkFuseSeconds, tuning.FuseLinkSecondaryDamageRatio);
            _primaryEffectIds = tuning.FuseLinkPrimaryEffectIds.Split(',');
            _secondaryRadius = tuning.FuseLinkSecondaryRadius;
            _secondaryDamageRatio = tuning.FuseLinkSecondaryDamageRatio;
            _secondaryMaxTargets = tuning.FuseLinkSecondaryMaxTargets;
        }

        internal void Process(in CombatImmediateHitRequest request, float now)
        {
            if (_disposed || request.IsFuseSecondary || IsPrimaryEffect(request.EffectId) == false)
                return;

            MonsterController trigger = request.Target as MonsterController;
            if (trigger == null || trigger.SpawnSequence <= 0L)
                return;

            FuseLinkProcessOutcome outcome = _core.TryProcess(
                new FuseLinkPrimaryHit(trigger.SpawnSequence, request.Damage, true, false),
                now,
                out FuseLinkSecondaryPlan plan,
                out float expiresAt);
            if (outcome == FuseLinkProcessOutcome.Detonated)
            {
                ResolveSecondary(in request, plan.Damage);
                return;
            }

            if (outcome != FuseLinkProcessOutcome.FuseSet
                && outcome != FuseLinkProcessOutcome.FuseExpiredAndReset)
            {
                return;
            }

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

        internal void Reset()
        {
            if (_disposed)
                return;

            _core.Reset();
            _targets.Clear();
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _core.Dispose();
            _targets.Clear();
            _disposed = true;
        }

        private bool IsPrimaryEffect(string effectId)
        {
            if (string.IsNullOrEmpty(effectId))
                return false;
            for (int index = 0; index < _primaryEffectIds.Length; index++)
            {
                if (_primaryEffectIds[index] == effectId)
                    return true;
            }
            return false;
        }

        private void ResolveSecondary(in CombatImmediateHitRequest trigger, int damage)
        {
            _targets.Clear();
            float radiusSquared = _secondaryRadius * _secondaryRadius;
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
                while (index < _targets.Count
                    && (_targets[index].DistanceSquared < distanceSquared
                        || (_targets[index].DistanceSquared == distanceSquared
                            && _targets[index].SpawnSequence < target.SpawnSequence)))
                {
                    index++;
                }

                if (index < _secondaryMaxTargets)
                    _targets.Insert(index, new FuseLinkTarget(target, point, distanceSquared));
                if (_targets.Count > _secondaryMaxTargets)
                    _targets.RemoveAt(_secondaryMaxTargets);
            }

            int appliedTargetCount = 0;
            for (int index = 0; index < _targets.Count; index++)
            {
                FuseLinkTarget candidate = _targets[index];
                if (_immediateHits != null
                    && _immediateHits.TryApply(CombatImmediateHitRequest.CreateAllyDirectTarget(
                        trigger.SourceId,
                        candidate.Target,
                        trigger.Origin,
                        candidate.Point,
                        damage,
                        AttackVisualKind.AreaHit,
                        false,
                        default,
                        null,
                        true)))
                {
                    appliedTargetCount++;
                }
            }

            Build1RuntimeDiagnostics.Log("trait_effect_applied",
                Build1RuntimeDiagnostics.Text("trait_id", RunTraitIds.FuseLink),
                Build1RuntimeDiagnostics.Long(
                    "trigger_spawn_sequence",
                    trigger.Target is MonsterController monster ? monster.SpawnSequence : 0L),
                Build1RuntimeDiagnostics.Text("source_id", trigger.SourceId),
                Build1RuntimeDiagnostics.Int("authored_damage", trigger.Damage),
                Build1RuntimeDiagnostics.Float("secondary_ratio", _secondaryDamageRatio),
                Build1RuntimeDiagnostics.Float("radius", _secondaryRadius),
                Build1RuntimeDiagnostics.Int("secondary_target_count", appliedTargetCount),
                Build1RuntimeDiagnostics.Int("secondary_damage", damage),
                Build1RuntimeDiagnostics.Bool("trigger_is_fuse_secondary", trigger.IsFuseSecondary),
                Build1RuntimeDiagnostics.Bool("countable_attribution", trigger.KillAttribution.IsCountable));
        }

        private readonly struct FuseLinkTarget
        {
            internal FuseLinkTarget(MonsterController target, Vector3 point, float distanceSquared)
            {
                Target = target;
                Point = point;
                DistanceSquared = distanceSquared;
                SpawnSequence = target.SpawnSequence;
            }

            internal MonsterController Target { get; }
            internal Vector3 Point { get; }
            internal float DistanceSquared { get; }
            internal long SpawnSequence { get; }
        }
    }
}
