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
        const float AbsorbMoveSpeed = 7.5f;
        const float EliteRedChargerAbsorbScale = 1.45f;

        readonly List<GemController> _collectBuffer = new List<GemController>(64);
        readonly List<GemController> _attractingGems = new List<GemController>(64);
        readonly HashSet<GemController> _attractingGemSet = new HashSet<GemController>();
        readonly RunState _runState;
        readonly RuntimeObjectRegistry _registry;
        readonly RunTraitEffectCoordinator _runTraitEffects;
        GridController _grid;
        CircleCollider2D _absorbCollider;
        float _collectDistance = DefaultCollectDistance;
        float _experienceMultiplier = 1.0f;
        double _experienceBonusRemainder;

        public CommanderGemCollector(RunState runState, RuntimeObjectRegistry registry)
            : this(runState, registry, null)
        {
        }

        public CommanderGemCollector(
            RunState runState,
            RuntimeObjectRegistry registry,
            RunTraitEffectCoordinator runTraitEffects)
        {
            _runState = runState ?? throw new ArgumentNullException(nameof(runState));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _runTraitEffects = runTraitEffects;
        }

        public void BindGrid(GridController grid)
        {
            _grid = grid;
            _attractingGems.Clear();
            _attractingGemSet.Clear();
        }

        public void BindAbsorbCollider(CircleCollider2D absorbCollider)
        {
            _absorbCollider = absorbCollider;
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
            float traitMultiplier = _runTraitEffects == null
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
            return Collect(position, Time.deltaTime);
        }

        public int Collect(Vector3 position, float deltaTime)
        {
            if (_grid == null || _absorbCollider == null || _absorbCollider.enabled == false)
                return 0;

            float sqrCollectDistance = _collectDistance * _collectDistance;
            _grid.GatherGems(position, _collectDistance + GatherRangePadding, _collectBuffer);

            for (int i = 0; i < _collectBuffer.Count; i++)
            {
                GemController gem = _collectBuffer[i];
                if (gem == null || gem.IsValid() == false || gem.CanPickup == false)
                    continue;

                Vector3 direction = gem.transform.position - position;
                if (direction.sqrMagnitude > sqrCollectDistance)
                    continue;

                if (_attractingGemSet.Add(gem))
                    _attractingGems.Add(gem);
            }

            int collectedCount = 0;
            Vector3 absorbCenter = _absorbCollider.bounds.center;
            float moveDistance = AbsorbMoveSpeed * Mathf.Max(0.0f, deltaTime);
            for (int i = _attractingGems.Count - 1; i >= 0; i--)
            {
                GemController gem = _attractingGems[i];
                if (gem == null || gem.IsValid() == false)
                {
                    RemoveAttractingGemAt(i, gem);
                    continue;
                }

                Vector3 nextPosition = Vector3.MoveTowards(gem.transform.position, absorbCenter, moveDistance);
                gem.transform.position = nextPosition;
                if (_absorbCollider.OverlapPoint(nextPosition) == false)
                    continue;

                float absorbScale = gem.SourceEnemyId == CombatIds.EliteRedCharger
                    ? EliteRedChargerAbsorbScale
                    : 1.0f;
                RetroVfx.Spawn(RetroVfxKind.XpAbsorb, gem.transform.position, Vector3.zero, absorbScale);
                int awardedExperience = AwardGameplayExperience(1);
                P0Telemetry.Log(
                    P0Telemetry.ExpOrbAbsorb,
                    $"enemy_id={gem.SourceEnemyId}",
                    $"actual_exp_reward={awardedExperience}",
                    $"source_reward_total={gem.SourceRewardTotal}",
                    "visual_only=false");

                if (_registry.ReleaseGem(gem))
                    collectedCount++;

                RemoveAttractingGemAt(i, gem);
            }

            return collectedCount;
        }

        void RemoveAttractingGemAt(int index, GemController gem)
        {
            _attractingGems.RemoveAt(index);
            if (gem != null)
                _attractingGemSet.Remove(gem);
        }
    }
}
