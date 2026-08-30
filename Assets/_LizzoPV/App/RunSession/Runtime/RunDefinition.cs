using System;
using Lizzo.PV.Data;
using UnityEngine;

namespace Lizzo.PV.Flow
{
    public enum RunSpawnPattern
    {
        StageBudget,
        SequentialEdges,
    }

    public enum RunCardOfferPattern
    {
        Standard,
        GuidedSequence,
    }

    public enum RunCombatProfile
    {
        Canonical,
        TutorialBaselineV0,
    }

    public static class RunCardPoolProfileIds
    {
        public const string Standard = "standard";
    }

    public readonly struct RunSpawnRateStep
    {
        public float StartSeconds { get; }
        public float RatePerSecond { get; }

        public RunSpawnRateStep(float startSeconds, float ratePerSecond)
        {
            StartSeconds = Math.Max(0.0f, startSeconds);
            RatePerSecond = Math.Max(0.0f, ratePerSecond);
        }
    }

    public readonly struct RunSpawnEdgeStep
    {
        public float StartSeconds { get; }
        public int ActiveEdgeCount { get; }

        public RunSpawnEdgeStep(float startSeconds, int activeEdgeCount)
        {
            StartSeconds = Math.Max(0.0f, startSeconds);
            ActiveEdgeCount = Mathf.Clamp(activeEdgeCount, 0, 4);
        }
    }

    public readonly struct RunEnemyMixStep
    {
        public float StartSeconds { get; }
        public float EndSeconds { get; }
        public int MediumEnemyInterval { get; }

        public RunEnemyMixStep(float startSeconds, float endSeconds, int mediumEnemyInterval)
        {
            StartSeconds = Math.Max(0.0f, startSeconds);
            EndSeconds = Math.Max(StartSeconds, endSeconds);
            MediumEnemyInterval = Math.Max(0, mediumEnemyInterval);
        }
    }

    public sealed class RunSequentialSpawnSchedule
    {
        readonly RunSpawnRateStep[] _rates;
        readonly RunSpawnEdgeStep[] _edges;
        readonly RunEnemyMixStep[] _enemyMixes;

        public float TickSeconds { get; }
        public float FirstGroupStartSeconds { get; }
        public int FirstGroupCount { get; }
        public int FirstGroupEdgeCount { get; }
        public float FirstGroupTangentLimit { get; }
        public float FirstGroupCameraMargin { get; }
        public int SmallEnemyTemplateId { get; }
        public int MediumEnemyTemplateId { get; }

        public RunSequentialSpawnSchedule(
            float tickSeconds,
            float firstGroupStartSeconds,
            int firstGroupCount,
            int firstGroupEdgeCount,
            float firstGroupTangentLimit,
            float firstGroupCameraMargin,
            int smallEnemyTemplateId,
            int mediumEnemyTemplateId,
            RunSpawnRateStep[] rates,
            RunSpawnEdgeStep[] edges,
            RunEnemyMixStep[] enemyMixes)
        {
            TickSeconds = Math.Max(0.01f, tickSeconds);
            FirstGroupStartSeconds = Math.Max(0.0f, firstGroupStartSeconds);
            FirstGroupCount = Math.Max(0, firstGroupCount);
            FirstGroupEdgeCount = FirstGroupCount > 0
                ? Mathf.Clamp(firstGroupEdgeCount, 1, 4)
                : 0;
            FirstGroupTangentLimit = Math.Max(0.0f, firstGroupTangentLimit);
            FirstGroupCameraMargin = Math.Max(0.0f, firstGroupCameraMargin);
            if (smallEnemyTemplateId <= 0)
                throw new ArgumentOutOfRangeException(nameof(smallEnemyTemplateId));
            if (mediumEnemyTemplateId <= 0)
                throw new ArgumentOutOfRangeException(nameof(mediumEnemyTemplateId));
            SmallEnemyTemplateId = smallEnemyTemplateId;
            MediumEnemyTemplateId = mediumEnemyTemplateId;
            _rates = rates == null ? Array.Empty<RunSpawnRateStep>() : (RunSpawnRateStep[])rates.Clone();
            _edges = edges == null ? Array.Empty<RunSpawnEdgeStep>() : (RunSpawnEdgeStep[])edges.Clone();
            _enemyMixes = enemyMixes == null ? Array.Empty<RunEnemyMixStep>() : (RunEnemyMixStep[])enemyMixes.Clone();
        }

        public float ResolveRate(float elapsedSeconds)
        {
            float value = 0.0f;
            for (int index = 0; index < _rates.Length; index++)
            {
                if (elapsedSeconds < _rates[index].StartSeconds)
                    break;
                value = _rates[index].RatePerSecond;
            }
            return value;
        }

        public int ResolveActiveEdgeCount(float elapsedSeconds)
        {
            int value = 0;
            for (int index = 0; index < _edges.Length; index++)
            {
                if (elapsedSeconds < _edges[index].StartSeconds)
                    break;
                value = _edges[index].ActiveEdgeCount;
            }
            return value;
        }

        public bool ShouldUseMediumEnemy(float elapsedSeconds, int sequence)
        {
            for (int index = 0; index < _enemyMixes.Length; index++)
            {
                RunEnemyMixStep step = _enemyMixes[index];
                if (elapsedSeconds >= step.StartSeconds
                    && elapsedSeconds < step.EndSeconds
                    && step.MediumEnemyInterval > 0)
                {
                    return sequence % step.MediumEnemyInterval == 0;
                }
            }
            return false;
        }
    }

    public readonly struct RunConditionalEnemySpawn
    {
        public int TemplateId { get; }
        public float StartSeconds { get; }
        public float Chance { get; }

        public RunConditionalEnemySpawn(int templateId, float startSeconds, float chance)
        {
            if (templateId <= 0)
                throw new ArgumentOutOfRangeException(nameof(templateId));

            TemplateId = templateId;
            StartSeconds = Math.Max(0.0f, startSeconds);
            Chance = Mathf.Clamp01(chance);
        }
    }

    public sealed class RunRingSurgeDefinition
    {
        public float StartSeconds { get; }
        public int SpawnCount { get; }
        public float CameraMargin { get; }

        public RunRingSurgeDefinition(float startSeconds, int spawnCount, float cameraMargin)
        {
            StartSeconds = Math.Max(0.0f, startSeconds);
            SpawnCount = Math.Max(0, spawnCount);
            CameraMargin = Math.Max(0.0f, cameraMargin);
        }
    }

    public sealed class RunBossPreludeSpawnDefinition
    {
        public float SlowdownSeconds { get; }
        public float ReadySeconds { get; }
        public float MinimumMultiplier { get; }
        public float MaximumMultiplier { get; }

        public RunBossPreludeSpawnDefinition(
            float slowdownSeconds,
            float readySeconds,
            float minimumMultiplier,
            float maximumMultiplier)
        {
            SlowdownSeconds = Math.Max(0.0f, slowdownSeconds);
            ReadySeconds = Mathf.Clamp(readySeconds, 0.0f, SlowdownSeconds);
            MinimumMultiplier = Math.Max(0.0f, minimumMultiplier);
            MaximumMultiplier = Math.Max(MinimumMultiplier, maximumMultiplier);
        }

        public float ResolveMultiplier(float remainingSeconds)
        {
            if (remainingSeconds <= 0.0f)
                return 0.0f;
            if (remainingSeconds <= ReadySeconds)
                return MinimumMultiplier;
            if (remainingSeconds > SlowdownSeconds)
                return 1.0f;

            float ratio = Mathf.InverseLerp(ReadySeconds, SlowdownSeconds, remainingSeconds);
            return Mathf.Lerp(MinimumMultiplier, MaximumMultiplier, ratio);
        }
    }

    public sealed class RunEliteSpawnSchedule
    {
        public int TemplateId { get; }
        public string ContentId { get; }
        public string DisplayName { get; }
        public float FirstSpawnSeconds { get; }
        public int SpawnCount { get; }
        public float RespawnIntervalSeconds { get; }
        public float MinimumCameraMargin { get; }
        public float MaximumCameraMargin { get; }

        public RunEliteSpawnSchedule(
            int templateId,
            string contentId,
            string displayName,
            float firstSpawnSeconds,
            int spawnCount,
            float respawnIntervalSeconds,
            float minimumCameraMargin,
            float maximumCameraMargin)
        {
            if (templateId <= 0)
                throw new ArgumentOutOfRangeException(nameof(templateId));
            if (string.IsNullOrWhiteSpace(contentId))
                throw new ArgumentException("Elite content id is required.", nameof(contentId));
            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("Elite display name is required.", nameof(displayName));
            if (minimumCameraMargin < 0.0f || maximumCameraMargin < minimumCameraMargin)
                throw new ArgumentOutOfRangeException(nameof(maximumCameraMargin));

            TemplateId = templateId;
            ContentId = contentId.Trim();
            DisplayName = displayName.Trim();
            FirstSpawnSeconds = Math.Max(0.0f, firstSpawnSeconds);
            SpawnCount = Math.Max(0, spawnCount);
            RespawnIntervalSeconds = Math.Max(0.01f, respawnIntervalSeconds);
            MinimumCameraMargin = minimumCameraMargin;
            MaximumCameraMargin = maximumCameraMargin;
        }
    }

    public sealed class RunStandardSpawnSchedule
    {
        readonly RunSpawnRateStep[] _rates;
        readonly RunConditionalEnemySpawn[] _conditionalEnemies;

        public int BaseEnemyTemplateId { get; }
        public float MinimumCameraMargin { get; }
        public float MaximumCameraMargin { get; }
        public RunRingSurgeDefinition RingSurge { get; }
        public RunBossPreludeSpawnDefinition BossPrelude { get; }

        public RunStandardSpawnSchedule(
            int baseEnemyTemplateId,
            float minimumCameraMargin,
            float maximumCameraMargin,
            RunRingSurgeDefinition ringSurge,
            RunBossPreludeSpawnDefinition bossPrelude,
            RunSpawnRateStep[] rates,
            RunConditionalEnemySpawn[] conditionalEnemies)
        {
            if (baseEnemyTemplateId <= 0)
                throw new ArgumentOutOfRangeException(nameof(baseEnemyTemplateId));
            if (minimumCameraMargin < 0.0f || maximumCameraMargin < minimumCameraMargin)
                throw new ArgumentOutOfRangeException(nameof(maximumCameraMargin));

            BaseEnemyTemplateId = baseEnemyTemplateId;
            MinimumCameraMargin = minimumCameraMargin;
            MaximumCameraMargin = maximumCameraMargin;
            RingSurge = ringSurge;
            BossPrelude = bossPrelude;
            _rates = rates == null ? Array.Empty<RunSpawnRateStep>() : (RunSpawnRateStep[])rates.Clone();
            _conditionalEnemies = conditionalEnemies == null
                ? Array.Empty<RunConditionalEnemySpawn>()
                : (RunConditionalEnemySpawn[])conditionalEnemies.Clone();
        }

        public float ResolveRate(float elapsedSeconds)
        {
            float value = 0.0f;
            for (int index = 0; index < _rates.Length; index++)
            {
                if (elapsedSeconds < _rates[index].StartSeconds)
                    break;
                value = _rates[index].RatePerSecond;
            }
            return value;
        }

        public int ResolveEnemyTemplateId(float elapsedSeconds, Func<float> randomValue)
        {
            if (randomValue == null)
                throw new ArgumentNullException(nameof(randomValue));

            for (int index = 0; index < _conditionalEnemies.Length; index++)
            {
                RunConditionalEnemySpawn entry = _conditionalEnemies[index];
                if (elapsedSeconds >= entry.StartSeconds && randomValue() < entry.Chance)
                    return entry.TemplateId;
            }

            return BaseEnemyTemplateId;
        }
    }

    public sealed class RunBossDefinition
    {
        public int TemplateId { get; }
        public string ContentId { get; }
        public string DisplayName { get; }

        public RunBossDefinition(int templateId, string contentId, string displayName)
        {
            if (templateId <= 0)
                throw new ArgumentOutOfRangeException(nameof(templateId));
            if (string.IsNullOrWhiteSpace(contentId))
                throw new ArgumentException("Boss content id is required.", nameof(contentId));
            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("Boss display name is required.", nameof(displayName));

            TemplateId = templateId;
            ContentId = contentId.Trim();
            DisplayName = displayName.Trim();
        }
    }

    public sealed class RunDefinition
    {
        public string Id { get; }
        public string MapAddress { get; }
        public string CardPoolProfileId { get; }
        public Vector2 ArenaSize { get; }
        public float DurationSeconds { get; }
        public float BossSpawnSeconds { get; }
        public int BossMinimumSquadCount { get; }
        public int BossMinimumCompanionCount { get; }
        public int CommanderMaxHp { get; }
        public float CommanderMoveSpeed { get; }
        public float ExperienceMultiplier { get; }
        public int TargetCardCount { get; }
        public int InitialExperienceCharge { get; }
        public float InitialExperienceChargeSeconds { get; }
        public bool EnableEliteSpawns => EliteSpawnSchedule != null && EliteSpawnSchedule.SpawnCount > 0;
        public bool EnableAdvancedCombatSystems { get; }
        public bool IndependentCompanionActions { get; }
        public bool UseGuidedVictoryTransition { get; }
        public bool CollapseExperienceDrops { get; }
        public float ExperienceRewardCutoffSeconds { get; }
        public int BossContactDamage { get; }
        public float BossContactCooldownSeconds { get; }
        public RunSpawnPattern SpawnPattern { get; }
        public int MaxEnemyCount { get; }
        public RunStandardSpawnSchedule StandardSpawnSchedule { get; }
        public RunSequentialSpawnSchedule SequentialSpawnSchedule { get; }
        public RunEliteSpawnSchedule EliteSpawnSchedule { get; }
        public RunCardOfferPattern CardOfferPattern { get; }
        public RunCombatProfile CombatProfile { get; }
        public RunBossDefinition Boss { get; }
        public RunResultPresentation ResultPresentation { get; }

        public RunDefinition(
            string id,
            string mapAddress,
            string cardPoolProfileId,
            Vector2 arenaSize,
            float durationSeconds,
            float bossSpawnSeconds,
            int bossMinimumSquadCount,
            int bossMinimumCompanionCount,
            int commanderMaxHp,
            float commanderMoveSpeed,
            float experienceMultiplier,
            int targetCardCount,
            int initialExperienceCharge,
            float initialExperienceChargeSeconds,
            RunEliteSpawnSchedule eliteSpawnSchedule,
            bool enableAdvancedCombatSystems,
            bool independentCompanionActions,
            bool useGuidedVictoryTransition,
            bool collapseExperienceDrops,
            float experienceRewardCutoffSeconds,
            int bossContactDamage,
            float bossContactCooldownSeconds,
            RunSpawnPattern spawnPattern,
            int maxEnemyCount,
            RunStandardSpawnSchedule standardSpawnSchedule,
            RunSequentialSpawnSchedule sequentialSpawnSchedule,
            RunCardOfferPattern cardOfferPattern,
            RunCombatProfile combatProfile,
            RunBossDefinition boss,
            RunResultPresentation resultPresentation)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Run definition id is required.", nameof(id));
            if (string.IsNullOrWhiteSpace(mapAddress))
                throw new ArgumentException("Run map address is required.", nameof(mapAddress));
            if (string.IsNullOrWhiteSpace(cardPoolProfileId))
                throw new ArgumentException("Run card pool profile id is required.", nameof(cardPoolProfileId));
            if (arenaSize.x <= 0.0f || arenaSize.y <= 0.0f)
                throw new ArgumentOutOfRangeException(nameof(arenaSize));
            if (durationSeconds <= 0.0f)
                throw new ArgumentOutOfRangeException(nameof(durationSeconds));
            if (bossSpawnSeconds < 0.0f || bossSpawnSeconds > durationSeconds)
                throw new ArgumentOutOfRangeException(nameof(bossSpawnSeconds));
            if (commanderMaxHp <= 0)
                throw new ArgumentOutOfRangeException(nameof(commanderMaxHp));
            if (commanderMoveSpeed <= 0.0f)
                throw new ArgumentOutOfRangeException(nameof(commanderMoveSpeed));
            if (experienceMultiplier <= 0.0f)
                throw new ArgumentOutOfRangeException(nameof(experienceMultiplier));
            if (maxEnemyCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxEnemyCount));
            if (spawnPattern == RunSpawnPattern.StageBudget && standardSpawnSchedule == null)
                throw new ArgumentNullException(nameof(standardSpawnSchedule));
            if (spawnPattern == RunSpawnPattern.SequentialEdges && sequentialSpawnSchedule == null)
                throw new ArgumentNullException(nameof(sequentialSpawnSchedule));
            if (boss == null)
                throw new ArgumentNullException(nameof(boss));
            if (resultPresentation == null)
                throw new ArgumentNullException(nameof(resultPresentation));

            Id = id.Trim();
            MapAddress = mapAddress.Trim();
            CardPoolProfileId = cardPoolProfileId.Trim();
            ArenaSize = arenaSize;
            DurationSeconds = durationSeconds;
            BossSpawnSeconds = bossSpawnSeconds;
            BossMinimumSquadCount = Math.Max(0, bossMinimumSquadCount);
            BossMinimumCompanionCount = Math.Max(0, bossMinimumCompanionCount);
            CommanderMaxHp = commanderMaxHp;
            CommanderMoveSpeed = commanderMoveSpeed;
            ExperienceMultiplier = experienceMultiplier;
            TargetCardCount = Math.Max(0, targetCardCount);
            InitialExperienceCharge = Math.Max(0, initialExperienceCharge);
            InitialExperienceChargeSeconds = Math.Max(0.0f, initialExperienceChargeSeconds);
            EliteSpawnSchedule = eliteSpawnSchedule;
            EnableAdvancedCombatSystems = enableAdvancedCombatSystems;
            IndependentCompanionActions = independentCompanionActions;
            UseGuidedVictoryTransition = useGuidedVictoryTransition;
            CollapseExperienceDrops = collapseExperienceDrops;
            ExperienceRewardCutoffSeconds = Math.Max(0.0f, experienceRewardCutoffSeconds);
            BossContactDamage = Math.Max(0, bossContactDamage);
            BossContactCooldownSeconds = Math.Max(0.0f, bossContactCooldownSeconds);
            SpawnPattern = spawnPattern;
            MaxEnemyCount = maxEnemyCount;
            StandardSpawnSchedule = standardSpawnSchedule;
            SequentialSpawnSchedule = sequentialSpawnSchedule;
            CardOfferPattern = cardOfferPattern;
            CombatProfile = combatProfile;
            Boss = boss;
            ResultPresentation = resultPresentation;
        }

        public bool HasCardLimit => TargetCardCount > 0;
        public bool HasBossContactOverride => BossContactDamage > 0 && BossContactCooldownSeconds > 0.0f;
        public bool UsesGuidedCardOffers => CardOfferPattern == RunCardOfferPattern.GuidedSequence;
        public bool UsesBaselineCombatProfile => CombatProfile == RunCombatProfile.TutorialBaselineV0;

        public EnemyData ResolveEnemy(EnemyData source, float elapsedSeconds)
        {
            return TutorialCombatBaseline.ResolveEnemy(
                source,
                UsesBaselineCombatProfile,
                elapsedSeconds,
                ExperienceRewardCutoffSeconds);
        }

        public int ResolveRequiredExperience(IDataProvider data, int cardNumber)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            return UsesBaselineCombatProfile
                ? TutorialCombatBaseline.RequiredExperienceForCard(cardNumber)
                : Math.Max(1, data.GetLevelExp(cardNumber));
        }
    }

    public sealed class RunResultPresentation
    {
        public string ClearTitle { get; }
        public string StageLabel { get; }
        public string ClearPrimaryLabel { get; }

        public RunResultPresentation(string clearTitle, string stageLabel, string clearPrimaryLabel)
        {
            ClearTitle = clearTitle ?? string.Empty;
            StageLabel = stageLabel ?? string.Empty;
            ClearPrimaryLabel = clearPrimaryLabel ?? string.Empty;
        }
    }

    public static class RunDefinitionResolver
    {
        private static readonly Vector2 DefaultArenaSize = new Vector2(100.0f, 100.0f);
        public const float OpeningCardDelaySeconds = 1.0f;

        public static RunDefinition Resolve(RunContext context, IDataProvider data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            UnitData commander = data.GetUnit("commander_01")
                ?? throw new InvalidOperationException("[RunDefinitionResolver] Commander data is missing.");
            EnemyData boss = data.GetEnemy("boss_hungry_giant")
                ?? throw new InvalidOperationException("[RunDefinitionResolver] Boss data is missing.");
            RunTuningData tuning = data.RunTuning
                ?? throw new InvalidOperationException("[RunDefinitionResolver] Run tuning data is missing.");

            return context.IsTutorial
                ? CreateTutorial(data, boss, tuning)
                : CreateStandard(context, commander, boss, tuning, data);
        }

        private static RunDefinition CreateStandard(
            RunContext context,
            UnitData commander,
            EnemyData boss,
            RunTuningData tuning,
            IDataProvider data)
        {
            float timelineScale = tuning.TimelineScale;
            EnemyData goblin = data.GetEnemy("small_goblin")
                ?? throw new InvalidOperationException("[RunDefinitionResolver] Small enemy data is missing.");
            EnemyData orc = data.GetEnemy("shield_orc")
                ?? throw new InvalidOperationException("[RunDefinitionResolver] Medium enemy data is missing.");
            EnemyData wolf = data.GetEnemy("hungry_wolf")
                ?? throw new InvalidOperationException("[RunDefinitionResolver] Wolf enemy data is missing.");
            EnemyData elite = data.GetEnemy("elite_red_charger")
                ?? throw new InvalidOperationException("[RunDefinitionResolver] Elite enemy data is missing.");
            return new RunDefinition(
                "campaign-stage-" + (int)context.StageId,
                "Map_01.prefab",
                RunCardPoolProfileIds.Standard,
                DefaultArenaSize,
                Mathf.Max(1.0f, tuning.StageDurationSeconds),
                Mathf.Clamp(tuning.BossSpawnSeconds, 0.0f, Mathf.Max(1.0f, tuning.StageDurationSeconds)),
                0,
                0,
                commander.Hp,
                commander.MoveSpeed,
                1.0f,
                0,
                Mathf.Max(1, data.GetLevelExp(1)),
                OpeningCardDelaySeconds,
                new RunEliteSpawnSchedule(
                    elite.TemplateId,
                    elite.Id,
                    elite.DisplayName,
                    tuning.RedChargerSpawnSeconds,
                    3,
                    60.0f,
                    1.2f,
                    2.4f),
                true,
                false,
                false,
                false,
                0.0f,
                0,
                0.0f,
                RunSpawnPattern.StageBudget,
                tuning.MaxEnemyStage1,
                new RunStandardSpawnSchedule(
                    goblin.TemplateId,
                    0.8f,
                    1.8f,
                    new RunRingSurgeDefinition(60.0f * timelineScale, 24, 0.9f),
                    new RunBossPreludeSpawnDefinition(10.0f, 5.0f, 0.15f, 0.65f),
                    new[]
                    {
                        new RunSpawnRateStep(0.0f, 1.6f),
                        new RunSpawnRateStep(25.0f * timelineScale, 2.2f),
                        new RunSpawnRateStep(60.0f * timelineScale, 2.8f),
                        new RunSpawnRateStep(150.0f * timelineScale, 3.2f),
                    },
                    new[]
                    {
                        new RunConditionalEnemySpawn(orc.TemplateId, data.GetEffectiveSpawnSeconds(orc), 0.15f),
                        new RunConditionalEnemySpawn(wolf.TemplateId, data.GetEffectiveSpawnSeconds(wolf), 0.45f),
                    }),
                null,
                RunCardOfferPattern.Standard,
                RunCombatProfile.Canonical,
                new RunBossDefinition(boss.TemplateId, boss.Id, boss.DisplayName),
                new RunResultPresentation("승리", "1-" + (int)context.StageId, "다시 출정"));
        }

        private static RunDefinition CreateTutorial(IDataProvider data, EnemyData boss, RunTuningData tuning)
        {
            return new RunDefinition(
                "tutorial-baseline-v0",
                "Map_01.prefab",
                RunCardPoolProfileIds.Standard,
                Vector2.one * TutorialCombatBaseline.ArenaSize,
                TutorialRunTimeline.CompletionTargetSeconds,
                TutorialRunTimeline.BossTargetSeconds,
                BossSpawnReadiness.TutorialTargetSquadCount,
                BossSpawnReadiness.TutorialTargetCompanionCount,
                TutorialCombatBaseline.CommanderMaxHp,
                TutorialCombatBaseline.CommanderMoveSpeed,
                TutorialCombatBaseline.ExperienceMultiplier,
                TutorialCombatBaseline.TargetCardCount,
                Mathf.Max(1, data.GetLevelExp(1)),
                OpeningCardDelaySeconds,
                null,
                false,
                true,
                true,
                true,
                TutorialRunTimeline.ShowcaseStartSeconds,
                20,
                1.0f,
                RunSpawnPattern.SequentialEdges,
                tuning.MaxEnemyStage1,
                null,
                CreateTutorialSpawnSchedule(data),
                RunCardOfferPattern.GuidedSequence,
                RunCombatProfile.TutorialBaselineV0,
                new RunBossDefinition(boss.TemplateId, boss.Id, boss.DisplayName),
                new RunResultPresentation("튜토리얼 완료", "튜토리얼", "로비로"));
        }

        private static RunSequentialSpawnSchedule CreateTutorialSpawnSchedule(IDataProvider data)
        {
            EnemyData smallEnemy = data.GetEnemy("small_goblin")
                ?? throw new InvalidOperationException("[RunDefinitionResolver] Tutorial small enemy data is missing.");
            EnemyData mediumEnemy = data.GetEnemy("shield_orc")
                ?? throw new InvalidOperationException("[RunDefinitionResolver] Tutorial medium enemy data is missing.");
            return new RunSequentialSpawnSchedule(
                0.25f,
                0.0f,
                9,
                4,
                2.5f,
                0.5f,
                smallEnemy.TemplateId,
                mediumEnemy.TemplateId,
                new[]
                {
                    new RunSpawnRateStep(0.0f, 0.0f),
                    new RunSpawnRateStep(TutorialCombatBaseline.InitialSpawnSeconds, 1.0f),
                    new RunSpawnRateStep(30.0f, 5.2f),
                    new RunSpawnRateStep(60.0f, 8.9f),
                    new RunSpawnRateStep(90.0f, 17.5f),
                    new RunSpawnRateStep(150.0f, 8.8f),
                    new RunSpawnRateStep(180.0f, 0.0f),
                },
                new[]
                {
                    new RunSpawnEdgeStep(0.0f, 0),
                    new RunSpawnEdgeStep(TutorialCombatBaseline.InitialSpawnSeconds, 4),
                },
                new[]
                {
                    new RunEnemyMixStep(60.0f, 90.0f, 8),
                    new RunEnemyMixStep(90.0f, 135.0f, 5),
                });
        }
    }
}
