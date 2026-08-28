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
        public float FirstGroupTangentLimit { get; }
        public float FirstGroupCameraMargin { get; }

        public RunSequentialSpawnSchedule(
            float tickSeconds,
            float firstGroupStartSeconds,
            int firstGroupCount,
            float firstGroupTangentLimit,
            float firstGroupCameraMargin,
            RunSpawnRateStep[] rates,
            RunSpawnEdgeStep[] edges,
            RunEnemyMixStep[] enemyMixes)
        {
            TickSeconds = Math.Max(0.01f, tickSeconds);
            FirstGroupStartSeconds = Math.Max(0.0f, firstGroupStartSeconds);
            FirstGroupCount = Math.Max(0, firstGroupCount);
            FirstGroupTangentLimit = Math.Max(0.0f, firstGroupTangentLimit);
            FirstGroupCameraMargin = Math.Max(0.0f, firstGroupCameraMargin);
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

    public sealed class RunDefinition
    {
        public string Id { get; }
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
        public bool ShowInitialCardOffer { get; }
        public bool EnableEliteSpawns { get; }
        public bool EnableAdvancedCombatSystems { get; }
        public bool IndependentCompanionActions { get; }
        public bool UseGuidedVictoryTransition { get; }
        public bool CollapseExperienceDrops { get; }
        public float ExperienceRewardCutoffSeconds { get; }
        public int BossContactDamage { get; }
        public float BossContactCooldownSeconds { get; }
        public RunSpawnPattern SpawnPattern { get; }
        public RunSequentialSpawnSchedule SequentialSpawnSchedule { get; }
        public RunCardOfferPattern CardOfferPattern { get; }
        public RunCombatProfile CombatProfile { get; }
        public RunResultPresentation ResultPresentation { get; }

        public RunDefinition(
            string id,
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
            bool showInitialCardOffer,
            bool enableEliteSpawns,
            bool enableAdvancedCombatSystems,
            bool independentCompanionActions,
            bool useGuidedVictoryTransition,
            bool collapseExperienceDrops,
            float experienceRewardCutoffSeconds,
            int bossContactDamage,
            float bossContactCooldownSeconds,
            RunSpawnPattern spawnPattern,
            RunSequentialSpawnSchedule sequentialSpawnSchedule,
            RunCardOfferPattern cardOfferPattern,
            RunCombatProfile combatProfile,
            RunResultPresentation resultPresentation)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Run definition id is required.", nameof(id));
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
            if (resultPresentation == null)
                throw new ArgumentNullException(nameof(resultPresentation));

            Id = id.Trim();
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
            ShowInitialCardOffer = showInitialCardOffer;
            EnableEliteSpawns = enableEliteSpawns;
            EnableAdvancedCombatSystems = enableAdvancedCombatSystems;
            IndependentCompanionActions = independentCompanionActions;
            UseGuidedVictoryTransition = useGuidedVictoryTransition;
            CollapseExperienceDrops = collapseExperienceDrops;
            ExperienceRewardCutoffSeconds = Math.Max(0.0f, experienceRewardCutoffSeconds);
            BossContactDamage = Math.Max(0, bossContactDamage);
            BossContactCooldownSeconds = Math.Max(0.0f, bossContactCooldownSeconds);
            SpawnPattern = spawnPattern;
            SequentialSpawnSchedule = sequentialSpawnSchedule;
            CardOfferPattern = cardOfferPattern;
            CombatProfile = combatProfile;
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

        public static RunDefinition Resolve(RunContext context, IDataProvider data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            UnitData commander = data.GetUnit("commander_01")
                ?? throw new InvalidOperationException("[RunDefinitionResolver] Commander data is missing.");
            RunTuningData tuning = data.RunTuning
                ?? throw new InvalidOperationException("[RunDefinitionResolver] Run tuning data is missing.");

            return context.IsTutorial
                ? CreateTutorial(data)
                : CreateStandard(context, commander, tuning);
        }

        private static RunDefinition CreateStandard(
            RunContext context,
            UnitData commander,
            RunTuningData tuning)
        {
            return new RunDefinition(
                "campaign-stage-" + (int)context.StageId,
                DefaultArenaSize,
                Mathf.Max(1.0f, tuning.StageDurationSeconds),
                Mathf.Clamp(tuning.BossSpawnSeconds, 0.0f, Mathf.Max(1.0f, tuning.StageDurationSeconds)),
                0,
                0,
                commander.Hp,
                commander.MoveSpeed,
                1.0f,
                0,
                0,
                0.0f,
                true,
                true,
                true,
                false,
                false,
                false,
                0.0f,
                0,
                0.0f,
                RunSpawnPattern.StageBudget,
                null,
                RunCardOfferPattern.Standard,
                RunCombatProfile.Canonical,
                new RunResultPresentation("승리", "1-1", "다시 출정"));
        }

        private static RunDefinition CreateTutorial(IDataProvider data)
        {
            return new RunDefinition(
                "tutorial-baseline-v0",
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
                7.0f,
                false,
                false,
                false,
                true,
                true,
                true,
                TutorialRunTimeline.ShowcaseStartSeconds,
                20,
                1.0f,
                RunSpawnPattern.SequentialEdges,
                CreateTutorialSpawnSchedule(),
                RunCardOfferPattern.GuidedSequence,
                RunCombatProfile.TutorialBaselineV0,
                new RunResultPresentation("튜토리얼 완료", "튜토리얼", "로비로"));
        }

        private static RunSequentialSpawnSchedule CreateTutorialSpawnSchedule()
        {
            return new RunSequentialSpawnSchedule(
                0.25f,
                7.0f,
                9,
                2.5f,
                0.5f,
                new[]
                {
                    new RunSpawnRateStep(0.0f, 0.0f),
                    new RunSpawnRateStep(7.0f, 1.0f),
                    new RunSpawnRateStep(30.0f, 5.2f),
                    new RunSpawnRateStep(60.0f, 8.9f),
                    new RunSpawnRateStep(90.0f, 17.5f),
                    new RunSpawnRateStep(150.0f, 8.8f),
                    new RunSpawnRateStep(180.0f, 0.0f),
                },
                new[]
                {
                    new RunSpawnEdgeStep(0.0f, 0),
                    new RunSpawnEdgeStep(7.0f, 1),
                    new RunSpawnEdgeStep(19.0f, 2),
                    new RunSpawnEdgeStep(50.0f, 3),
                    new RunSpawnEdgeStep(122.0f, 4),
                },
                new[]
                {
                    new RunEnemyMixStep(60.0f, 90.0f, 8),
                    new RunEnemyMixStep(90.0f, 135.0f, 5),
                });
        }
    }
}
