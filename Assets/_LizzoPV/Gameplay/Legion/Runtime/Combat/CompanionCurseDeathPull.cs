using Lizzo.PV.Combat;
using Lizzo.PV.Gameplay.Visuals;
using Lizzo.PV.Gameplay.Units;
using System;
using System.Collections.Generic;
using Lizzo.PV.Data;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    public readonly struct CompanionCurseDeathPullSetup
    {
        public CompanionCurseDeathPullSetup(float radius, int maxTargets)
        {
            Radius = Mathf.Max(0.0f, radius);
            MaxTargets = Mathf.Max(0, maxTargets);
        }

        public float Radius { get; }
        public int MaxTargets { get; }
        public bool IsConfigured => Radius > 0.0f && MaxTargets > 0;
    }

    public sealed class CompanionCurseDeathPullResolver
    {
        private const string NecromancerId = "necromancer";
        private const int MaxPullTargets = 4;

        private readonly IDataProvider _data;
        private readonly Func<float> _radiusMultiplier;

        public CompanionCurseDeathPullResolver(IDataProvider data, Func<float> radiusMultiplier = null)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _radiusMultiplier = radiusMultiplier;
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
                || effect.Radius <= 0.0f)
            {
                throw new InvalidOperationException("Canonical curse death pull data is invalid.");
            }

            setup = new CompanionCurseDeathPullSetup(effect.Radius * (_radiusMultiplier?.Invoke() ?? 1f), MaxPullTargets);
            return true;
        }
    }

    public sealed class CompanionEnemyDeathCombatEffects
    {
        private const float KnockbackSlideDuration = 0.16f;
        private readonly RuntimeObjectRegistry _registry;
        private readonly CompanionSecondPromotionCombatRunModule _secondPromotionCombatRunModule;
        private readonly CompanionThirdPromotionCombatRunModule _thirdPromotionCombatRunModule;
        private readonly CompanionCurseDeathPullResolver _canonicalCurseDeathPull;
        private readonly List<EnemyActor> _curseDeathPullCandidates = new List<EnemyActor>(32);

        public CompanionEnemyDeathCombatEffects(
            IDataProvider data,
            RuntimeObjectRegistry registry,
            CompanionSecondPromotionCombatRunModule secondPromotionCombatRunModule,
            CompanionThirdPromotionCombatRunModule thirdPromotionCombatRunModule, Func<float> radiusMultiplier = null)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _secondPromotionCombatRunModule = secondPromotionCombatRunModule
                ?? throw new ArgumentNullException(nameof(secondPromotionCombatRunModule));
            _thirdPromotionCombatRunModule = thirdPromotionCombatRunModule
                ?? throw new ArgumentNullException(nameof(thirdPromotionCombatRunModule));
            _canonicalCurseDeathPull = new CompanionCurseDeathPullResolver(data, radiusMultiplier);
        }

        public float CursePullRadius => _canonicalCurseDeathPull.TryResolve(out var setup) ? setup.Radius : 0f;

        internal bool ReportCompanionEnemyDeathStatus(
            in CompanionEnemyDeathStatusSnapshot snapshot,
            Vector3 deathPosition)
        {
            bool vulnerabilitySpread = _secondPromotionCombatRunModule?.TryResolveVulnerabilitySpread(
                snapshot,
                deathPosition,
                Time.time) ?? false;
            bool undeadRitual = _thirdPromotionCombatRunModule?.ReportCursedDeath(snapshot, deathPosition) ?? false;
            if (snapshot.WasCursed == false
                || snapshot.CurseSource.UnitId != "necromancer"
                || _canonicalCurseDeathPull.TryResolve(out CompanionCurseDeathPullSetup setup) == false)
            {
                return vulnerabilitySpread || undeadRitual;
            }

            _curseDeathPullCandidates.Clear();
            foreach (EnemyActor target in _registry.Enemies)
            {
                if (target == null || target.IsValid() == false || target.Hp <= 0
                    || target.IsBoss || target.IsForcedMovementActive)
                    continue;

                Vector3 point = CombatTargeting.ResolveTargetPoint(target, deathPosition);
                if ((point - deathPosition).sqrMagnitude <= setup.Radius * setup.Radius)
                    _curseDeathPullCandidates.Add(target);
            }

            int count = _curseDeathPullCandidates.Count;
            for (int index = 0; index < count; index += 1)
            {
                EnemyActor target = _curseDeathPullCandidates[index];
                target.ApplySmoothPullToPoint(deathPosition, KnockbackSlideDuration);
            }

            return vulnerabilitySpread || undeadRitual || count > 0;
        }
    }
}
