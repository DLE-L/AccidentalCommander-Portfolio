using System;
using System.Collections.Generic;
using Lizzo.PV.Flow;
using Lizzo.PV.Gameplay.RunTraits;
using Lizzo.PV.Legion;
using Lizzo.PV.P0.Combat;
using Lizzo.PV.P0.Telemetry;
using UnityEngine;

namespace Lizzo.PV.Gameplay.Commander
{
    public sealed class CommanderGemCollector
    {
        const float DefaultCollectDistance = 1.0f;
        const float GatherRangePadding = 0.5f;
        const float EliteRedChargerAbsorbScale = 1.45f;

        readonly List<GemController> _collectBuffer = new List<GemController>(64);
        readonly RunState _runState;
        readonly RuntimeObjectRegistry _registry;
        readonly RunTraitEffectCoordinator _runTraitEffects;
        readonly bool _isTutorial;
        GridController _grid;
        float _collectDistance = DefaultCollectDistance;
        float _experienceMultiplier = 1.0f;
        double _experienceBonusRemainder;

        public CommanderGemCollector(RunState runState, RuntimeObjectRegistry registry)
            : this(runState, registry, null, false)
        {
        }

        public CommanderGemCollector(
            RunState runState,
            RuntimeObjectRegistry registry,
            RunTraitEffectCoordinator runTraitEffects,
            bool isTutorial = false)
        {
            _runState = runState ?? throw new ArgumentNullException(nameof(runState));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _runTraitEffects = runTraitEffects;
            _isTutorial = isTutorial;
        }

        public void BindGrid(GridController grid)
        {
            _grid = grid;
        }

        public void SetCollectDistance(float collectDistance)
        {
            _collectDistance = collectDistance;
        }

        public double ExperienceBonusRemainder => _experienceBonusRemainder;

        public void SetExperienceMultiplier(float multiplier)
        {
            _experienceMultiplier = Mathf.Max(1.0f, multiplier);
        }

        public void ResetExperienceBonusRemainder()
        {
            _experienceBonusRemainder = 0.0d;
        }

        public int AwardGameplayExperience(int baseExperience)
        {
            if (baseExperience <= 0)
                return 0;

            // Tab97 P10B2B reconciliation: grant authored base EXP immediately and carry only
            // the fractional bonus through this run. Rounding source values avoids float storage
            // noise without changing the award rule or applying a reward-level rounding policy.
            float traitMultiplier = _isTutorial || _runTraitEffects == null
                ? 1.0f
                : _runTraitEffects.GetGameplayExperienceMultiplier();
            double bonusMultiplier = Math.Round(
                (_experienceMultiplier * traitMultiplier) - 1.0f,
                4,
                MidpointRounding.AwayFromZero);
            _experienceBonusRemainder += baseExperience * bonusMultiplier;
            int bonusGrant = (int)Math.Floor(_experienceBonusRemainder + 0.0000001d);
            _experienceBonusRemainder -= bonusGrant;
            int totalGrant = baseExperience + bonusGrant;
            if (_runState.IsLoaded == false)
                return 0;

            _runState.AddExperience(totalGrant);
            return totalGrant;
        }

        public int Collect(Vector3 position)
        {
            if (_grid == null)
                return 0;

            float sqrCollectDistance = _collectDistance * _collectDistance;
            _grid.GatherGems(position, _collectDistance + GatherRangePadding, _collectBuffer);

            int collectedCount = 0;
            for (int i = 0; i < _collectBuffer.Count; i++)
            {
                GemController gem = _collectBuffer[i];
                if (gem == null || gem.IsValid() == false || gem.CanPickup == false)
                    continue;

                Vector3 direction = gem.transform.position - position;
                if (direction.sqrMagnitude > sqrCollectDistance)
                    continue;

                float absorbScale = gem.SourceEnemyId == CombatIds.EliteRedCharger
                    ? EliteRedChargerAbsorbScale
                    : 1.0f;
                RetroVfx.Spawn(RetroVfxKind.XpAbsorb, gem.transform.position, Vector3.zero, absorbScale);
                int sourceExperience = _isTutorial
                    ? Mathf.Max(1, gem.SourceRewardTotal)
                    : 1;
                int awardedExperience = AwardGameplayExperience(sourceExperience);
                P0Telemetry.Log(
                    P0Telemetry.ExpOrbAbsorb,
                    $"enemy_id={gem.SourceEnemyId}",
                    $"actual_exp_reward={awardedExperience}",
                    $"source_reward_total={gem.SourceRewardTotal}",
                    "visual_only=false");

                if (_registry.ReleaseGem(gem))
                    collectedCount++;
            }

            return collectedCount;
        }
    }
}
