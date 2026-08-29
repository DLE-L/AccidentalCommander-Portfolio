using Lizzo.PV.Data;
using UnityEngine;

namespace Lizzo.PV.Flow
{
    public enum TutorialRunPhase
    {
        MeleeFoundation,
        RangedExpansion,
        FinalAssembly,
        Showcase,
        BossWindow,
        Complete,
    }

    public static class TutorialRunTimeline
    {
        public const float RangedExpansionStartSeconds = 30.0f;
        public const float FinalAssemblyStartSeconds = 90.0f;
        public const float ShowcaseStartSeconds = 135.0f;
        public const float BossTargetSeconds = 150.0f;
        public const float CompletionTargetSeconds = 180.0f;

        public static TutorialRunPhase Resolve(float elapsedSeconds)
        {
            if (elapsedSeconds >= CompletionTargetSeconds)
                return TutorialRunPhase.Complete;
            if (elapsedSeconds >= BossTargetSeconds)
                return TutorialRunPhase.BossWindow;
            if (elapsedSeconds >= ShowcaseStartSeconds)
                return TutorialRunPhase.Showcase;
            if (elapsedSeconds >= FinalAssemblyStartSeconds)
                return TutorialRunPhase.FinalAssembly;
            if (elapsedSeconds >= RangedExpansionStartSeconds)
                return TutorialRunPhase.RangedExpansion;

            return TutorialRunPhase.MeleeFoundation;
        }
    }

    public static class TutorialCombatBaseline
    {
        public const int TargetCardCount = 21;
        public const float ArenaSize = 20.0f;
        public const int CommanderMaxHp = 1000;
        public const float CommanderMoveSpeed = 3.2f;
        public const float ExperienceMultiplier = 2.0f;
        public const int SmallEnemyHp = 60;
        public const int MediumEnemyHp = 240;
        public const int BossHp = 6000;
        public const float InitialSpawnSeconds = 3.0f;

        public static int RequiredExperienceForCard(int cardNumber)
        {
            int normalized = Mathf.Max(1, cardNumber);
            return 5 * normalized * normalized;
        }

        public static float ResolveSpawnRate(float elapsedSeconds)
        {
            if (elapsedSeconds < InitialSpawnSeconds)
                return 0.0f;
            if (elapsedSeconds < 30.0f)
                return 1.0f;
            if (elapsedSeconds < 60.0f)
                return 5.2f;
            if (elapsedSeconds < 90.0f)
                return 8.9f;
            if (elapsedSeconds < 135.0f)
                return 17.5f;
            if (elapsedSeconds < 150.0f)
                return 17.5f;
            if (elapsedSeconds < TutorialRunTimeline.CompletionTargetSeconds)
                return 8.8f;
            return 0.0f;
        }

        public static int ResolveActiveSpawnEdgeCount(float elapsedSeconds)
        {
            if (elapsedSeconds < InitialSpawnSeconds)
                return 0;
            return 4;
        }

        public static EnemyData ResolveEnemy(
            EnemyData source,
            bool applyBaseline,
            float elapsedSeconds,
            float experienceRewardCutoffSeconds = TutorialRunTimeline.ShowcaseStartSeconds)
        {
            if (!applyBaseline || source == null)
                return source;

            EnemyData clone = CloneEnemy(source);
            switch (source.Id)
            {
                case "small_goblin":
                case "hungry_wolf":
                    clone.Hp = SmallEnemyHp;
                    clone.Attack = 5;
                    clone.AttackCooldown = 1.0f;
                    clone.MoveSpeed = 0.8f;
                    clone.ExpReward = 5;
                    break;
                case "shield_orc":
                    clone.Hp = MediumEnemyHp;
                    clone.Attack = 10;
                    clone.AttackCooldown = 1.0f;
                    clone.MoveSpeed = 0.68f;
                    clone.ExpReward = 20;
                    break;
                case "boss_hungry_giant":
                    clone.Hp = BossHp;
                    clone.MoveSpeed = 0.6f;
                    clone.ExpReward = 0;
                    break;
            }

            if (elapsedSeconds >= experienceRewardCutoffSeconds)
                clone.ExpReward = 0;

            return clone;
        }

        private static EnemyData CloneEnemy(EnemyData source)
        {
            return new EnemyData
            {
                TemplateId = source.TemplateId,
                Id = source.Id,
                DisplayName = source.DisplayName,
                Prefab = source.Prefab,
                Type = source.Type,
                Hp = source.Hp,
                Attack = source.Attack,
                AttackCooldown = source.AttackCooldown,
                ContactRange = source.ContactRange,
                MoveSpeed = source.MoveSpeed,
                ExpReward = source.ExpReward,
                SpawnSeconds = source.SpawnSeconds,
                ChargeCooldown = source.ChargeCooldown,
                ChargeDuration = source.ChargeDuration,
                PatternCooldown = source.PatternCooldown,
                Range = source.Range,
                Width = source.Width,
                Color = source.Color,
                SortingOrder = source.SortingOrder,
            };
        }
    }
}
