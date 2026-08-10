using System;
using System.Collections.Generic;
using Lizzo.PV.Combat.Projectiles;
using Lizzo.PV.Combat;
using Lizzo.PV.Data;
using Lizzo.PV.Legion.Combat;
using UnityEngine;

namespace Lizzo.PV.Legion.Synergy
{
    public readonly struct MagicChainCandidate
    {
        public MagicChainCandidate(MonsterController target, Vector3 point, long spawnSequence, bool isValid)
        {
            Target = target;
            Point = point;
            SpawnSequence = spawnSequence;
            IsValid = isValid;
        }

        public MonsterController Target { get; }
        public Vector3 Point { get; }
        public long SpawnSequence { get; }
        public bool IsValid { get; }
    }

    public static class MagicChainAssignmentRules
    {
        public static void SelectAssignments(IReadOnlyList<MagicChainCandidate> source, Vector3 anchor, float range, int count, List<MagicChainCandidate> results)
        {
            results.Clear();
            if (source == null || count <= 0 || range < 0.0f)
                return;

            float rangeSquared = range * range;
            for (int sourceIndex = 0; sourceIndex < source.Count; sourceIndex++)
            {
                MagicChainCandidate candidate = source[sourceIndex];
                if (candidate.IsValid == false || candidate.SpawnSequence <= 0L || (candidate.Point - anchor).sqrMagnitude > rangeSquared)
                    continue;

                int insertIndex = results.Count;
                float candidateDistance = (candidate.Point - anchor).sqrMagnitude;
                for (int resultIndex = 0; resultIndex < results.Count; resultIndex++)
                {
                    MagicChainCandidate existing = results[resultIndex];
                    float existingDistance = (existing.Point - anchor).sqrMagnitude;
                    if (candidateDistance < existingDistance || (Mathf.Approximately(candidateDistance, existingDistance) && candidate.SpawnSequence < existing.SpawnSequence))
                    {
                        insertIndex = resultIndex;
                        break;
                    }
                }

                if (insertIndex >= count)
                    continue;

                results.Insert(insertIndex, candidate);
                if (results.Count > count)
                    results.RemoveAt(results.Count - 1);
            }

            int validCount = results.Count;
            if (validCount == 0)
                return;

            MagicChainCandidate nearest = results[0];
            while (results.Count < count)
                results.Add(nearest);
        }
    }

    /// <summary>Run-owned P10D magic-chain executor; trigger cadence remains in SynergyTriggerState.</summary>
    public sealed class MagicChainSynergy : IDisposable
    {
        const string DamageId = "DMG_SYNERGY_MAGIC_01";
        const float SharedHomingSpeed = 22.0f;
        const float ArrivalDistance = 0.08f;

        readonly SynergyActivationState _activations;
        readonly SynergyTriggerState _triggers;
        readonly PartyService _party;
        readonly RuntimeObjectRegistry _registry;
        readonly ICombatProjectileModule _projectiles;
        readonly CanonicalCompanionCastStream _casts;
        readonly SynergyDamageData _data;
        readonly List<MagicChainCandidate> _candidates = new(32);
        readonly List<MagicChainCandidate> _assignments = new(5);
        bool _disposed;

        public MagicChainSynergy(IDataProvider data, SynergyActivationState activations, SynergyTriggerState triggers, PartyService party, RuntimeObjectRegistry registry, ICombatProjectileModule projectiles, CanonicalCompanionCastStream casts)
        {
            _activations = activations ?? throw new ArgumentNullException(nameof(activations));
            _triggers = triggers ?? throw new ArgumentNullException(nameof(triggers));
            _party = party ?? throw new ArgumentNullException(nameof(party));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _projectiles = projectiles ?? throw new ArgumentNullException(nameof(projectiles));
            _casts = casts ?? throw new ArgumentNullException(nameof(casts));
            _data = data?.GetSynergyDamage(DamageId) ?? throw new InvalidOperationException("Canonical magic synergy damage data is missing.");
            _casts.Completed += OnCanonicalCastCompleted;
        }

        public int LastSpawnedProjectileCount { get; private set; }

        public bool TryResolvePending()
        {
            if (_triggers.TryConsumePending(SynergyActivationIds.MagicChain, out _) == false)
                return false;

            LastSpawnedProjectileCount = 0;
            string representative = _activations.GetRepresentativeRosterSlotId(SynergyActivationIds.MagicChain);
            if (_party.TryResolveSynergyAnchorAndRange(representative, out Vector3 anchor, out float range) == false)
                return true;

            GatherCandidates();
            MagicChainAssignmentRules.SelectAssignments(_candidates, anchor, range, _data.ProjectileCount, _assignments);
            for (int index = 0; index < _assignments.Count; index++)
            {
                MagicChainCandidate candidate = _assignments[index];
                MonsterController target = candidate.Target;
                if (target == null || target.IsValid() == false)
                    continue;

                int damage = ResolveDamage(target);
                CombatProjectileRequest request = CombatProjectileRequest.CreateHoming(
                    _data.SynergyId, null, null, anchor, target, damage, SharedHomingSpeed,
                    _data.ProjectileLifetimeSeconds, ArrivalDistance, AttackVisualKind.SingleHit,
                    killAttribution: new CountableKillAttribution(0, _data.SynergyId, CombatKillSourceCategory.SynergyAction),
                    presentationId: CombatProjectilePresentationIds.MagicChain);
                if (_projectiles.TrySpawn(request))
                    LastSpawnedProjectileCount++;
            }

            return true;
        }

        public void Reset()
        {
            LastSpawnedProjectileCount = 0;
            _candidates.Clear();
            _assignments.Clear();
            _registry.ReleaseProjectilesBySourceId(_data.SynergyId);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _casts.Completed -= OnCanonicalCastCompleted;
            Reset();
        }

        void OnCanonicalCastCompleted(CanonicalCompanionCastCompleted cast)
        {
            bool isMagicFamily = HasExactFamilyTag(cast.FamilyTags, "magic_family");
            bool isAcceptedAction = cast.ActionKind == CanonicalCompanionActionKind.BasicAttack || cast.ActionKind == CanonicalCompanionActionKind.ActiveSkill;
            _triggers.ReportMagicCast(new SynergyMagicCastEvent(cast.CastId, isMagicFamily, isAcceptedAction, true, false));
        }

        void GatherCandidates()
        {
            _candidates.Clear();
            foreach (MonsterController target in _registry.Enemies)
            {
                if (target == null || target.IsValid() == false || target.Hp <= 0 || target.SpawnSequence <= 0L)
                    continue;

                _candidates.Add(new MagicChainCandidate(target, target.transform.position, target.SpawnSequence, true));
            }
        }

        int ResolveDamage(MonsterController target)
        {
            int damage = Mathf.RoundToInt(_data.BaseValue);
            if (target.IsBoss == false)
                return damage;

            return Mathf.Max(1, Mathf.Min(damage, Mathf.FloorToInt(target.MaxHp * _data.BossMaxHpPercent)));
        }

        static bool HasExactFamilyTag(string familyTags, string required)
        {
            if (string.IsNullOrEmpty(familyTags) || string.IsNullOrEmpty(required))
                return false;

            int start = 0;
            for (int index = 0; index <= familyTags.Length; index++)
            {
                if (index != familyTags.Length && familyTags[index] != ',')
                    continue;

                int length = index - start;
                if (length == required.Length && string.CompareOrdinal(familyTags, start, required, 0, length) == 0)
                    return true;

                start = index + 1;
            }

            return false;
        }
    }
}
