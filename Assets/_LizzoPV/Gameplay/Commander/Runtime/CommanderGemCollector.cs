using System;
using System.Collections.Generic;
using Lizzo.PV.Flow;
using Lizzo.PV.Legion;
using Lizzo.PV.Gameplay.Combat;
using Lizzo.PV.Gameplay.Telemetry;
using Lizzo.PV.Presentation;
using UnityEngine;

namespace Lizzo.PV.Gameplay.Commander
{
    public sealed class CommanderGemCollector
    {
        const float EliteRedChargerAbsorbScale = 1.45f;

        readonly List<GemController> _collectBuffer = new List<GemController>(64);
        readonly RunState _runState;
        readonly RuntimeObjectRegistry _registry;
        float _experienceMultiplier = 1.0f;
        double _experienceBonusRemainder;

        public CommanderGemCollector(RunState runState, RuntimeObjectRegistry registry)
        {
            _runState = runState ?? throw new ArgumentNullException(nameof(runState));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
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

            // Grant authored base EXP immediately and carry only the fractional bonus through the run.
            double bonusMultiplier = Math.Round(
                _experienceMultiplier - 1.0f,
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
            return Collect(position, Time.unscaledDeltaTime);
        }

        public int Collect(Vector3 position, float deltaTime)
        {
            _collectBuffer.Clear();
            foreach (GemController gem in _registry.Gems)
                _collectBuffer.Add(gem);

            int collectedCount = 0;
            for (int i = 0; i < _collectBuffer.Count; i++)
            {
                GemController gem = _collectBuffer[i];
                if (gem == null || gem.IsValid() == false)
                    continue;

                if (gem.AdvanceToward(position, deltaTime) == false)
                    continue;

                float absorbScale = gem.SourceEnemyId == CombatIds.EliteRedCharger
                    ? EliteRedChargerAbsorbScale
                    : 1.0f;
                RetroVfx.Spawn(RetroVfxKind.XpAbsorb, gem.transform.position, Vector3.zero, absorbScale);
                int awardedExperience = AwardGameplayExperience(gem.RewardAmount);
                gem.Services?.WorldFeedback?.TryPresentExperience(
                    OrbVisualTier.Small,
                    ExperienceFeedbackEventKind.AbsorbComplete,
                    gem.transform.position,
                    gem.GetInstanceID(),
                    awardedExperience);
                RunTelemetry.Log(
                    RunTelemetry.ExpOrbAbsorb,
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
