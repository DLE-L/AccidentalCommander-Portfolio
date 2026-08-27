using System;
using System.Collections.Generic;
using Lizzo.PV.Data;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    public readonly struct CompanionCurseDeathPullSetup
    {
        public CompanionCurseDeathPullSetup(float radius, int maxTargets, float pullDistance)
        {
            Radius = Mathf.Max(0.0f, radius);
            MaxTargets = Mathf.Max(0, maxTargets);
            PullDistance = Mathf.Max(0.0f, pullDistance);
        }

        public float Radius { get; }
        public int MaxTargets { get; }
        public float PullDistance { get; }
        public bool IsConfigured => Radius > 0.0f && MaxTargets > 0 && PullDistance > 0.0f;
    }

    public sealed class CompanionCurseDeathPullResolver
    {
        private const string NecromancerId = "necromancer";
        private const int PlaceholderMaxTargets = 4;

        private readonly IDataProvider _data;

        public CompanionCurseDeathPullResolver(IDataProvider data)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
        }

        public bool TryResolve(out CompanionCurseDeathPullSetup setup)
        {
            CompanionCombatProfileData profile = _data.GetCompanionCombatProfile(NecromancerId)
                ?? throw new InvalidOperationException("Canonical necromancer profile is missing.");
            CombatEffectData effect = _data.GetCombatEffect(profile.BasicEffectId)
                ?? throw new InvalidOperationException("Canonical necromancer effect is missing.");
            if (effect.OwnerUnitId != NecromancerId
                || effect.StatusKind != CompanionEnemyStatusKind.Curse
                || effect.StatusDuration <= 0.0f
                || effect.Radius <= 0.0f
                || effect.Push <= 0.0f)
            {
                throw new InvalidOperationException("Canonical curse death pull data is invalid.");
            }

            setup = new CompanionCurseDeathPullSetup(effect.Radius, PlaceholderMaxTargets, effect.Push);
            return true;
        }
    }

    public sealed partial class PartyService
    {
        private readonly List<TargetAreaImpactCandidate> _curseDeathPullCandidates = new List<TargetAreaImpactCandidate>(32);
        private readonly List<TargetAreaImpactCandidate> _curseDeathPullTargets = new List<TargetAreaImpactCandidate>(4);

        internal bool ReportCompanionEnemyDeathStatus(
            in CompanionEnemyDeathStatusSnapshot snapshot,
            Vector3 deathPosition)
        {
            if (snapshot.WasCursed == false
                || snapshot.CurseSource.UnitId != "necromancer"
                || _canonicalCurseDeathPull.TryResolve(out CompanionCurseDeathPullSetup setup) == false)
            {
                return false;
            }

            _curseDeathPullCandidates.Clear();
            foreach (MonsterController target in _registry.Enemies)
            {
                if (target == null || target.IsValid() == false)
                    continue;

                _curseDeathPullCandidates.Add(new TargetAreaImpactCandidate(
                    target,
                    AllyTargeting.ResolveTargetPoint(target, deathPosition),
                    target.GetInstanceID()));
            }

            TargetAreaImpactCollector.Collect(
                _curseDeathPullCandidates,
                deathPosition,
                setup.Radius,
                setup.MaxTargets,
                _curseDeathPullTargets);
            for (int index = 0; index < _curseDeathPullTargets.Count; index += 1)
            {
                TargetAreaImpactCandidate candidate = _curseDeathPullTargets[index];
                Vector3 direction = deathPosition - candidate.Point;
                if (direction.sqrMagnitude > 0.0001f)
                    candidate.Target.ApplySmoothKnockback(direction, setup.PullDistance, AllyCombat.KNOCKBACK_SLIDE_DURATION);
            }

            return _curseDeathPullTargets.Count > 0;
        }
    }
}
