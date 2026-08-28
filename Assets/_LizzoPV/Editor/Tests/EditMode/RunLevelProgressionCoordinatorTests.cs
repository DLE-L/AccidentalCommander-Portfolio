using System;
using System.Reflection;
using Lizzo.PV.Flow;
using Lizzo.PV.Gameplay.Route;
using Lizzo.PV.Gameplay.RunTraits;
using Lizzo.PV.Tests.Support;
using Lizzo.PV.UI;
using NUnit.Framework;
using UnityEngine;
using Lizzo.PV.Data;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class RunLevelProgressionCoordinatorTests
    {
        [Test]
        public void HandleExperienceChanged_BelowThresholdRefreshesCurrentStatus()
        {
            using ServiceTestFixture fixture = new ServiceTestFixture();
            fixture.Run.State.Reset(fixture.Data.GetLevelExp(1));
            fixture.Run.State.MarkLoaded();
            FakeGameplayRunUi ui = new FakeGameplayRunUi();
            object coordinator = CreateCoordinator(fixture.Run, ui);

            HandleExperienceChanged(
                coordinator,
                fixture.Run.State.Experience,
                fixture.Run.State.RequiredExperience);

            Assert.That(fixture.Run.State.Level, Is.EqualTo(1));
            Assert.That(ui.ShowSkillSelectionCount, Is.Zero);
            Assert.That(ui.ExperienceStatusCount, Is.EqualTo(1));
            Assert.That(ui.Level, Is.EqualTo(1));
            Assert.That(ui.CurrentExperience, Is.EqualTo(0.0f));
            Assert.That(ui.RequiredExperience, Is.EqualTo(fixture.Run.State.RequiredExperience));
        }

        [Test]
        public void HandleExperienceChanged_AtThresholdAdvancesAndPresentsSelection()
        {
            using ServiceTestFixture fixture = new ServiceTestFixture();
            fixture.Run.State.Reset(fixture.Data.GetLevelExp(1));
            fixture.Run.State.MarkLoaded();
            FakeGameplayRunUi ui = new FakeGameplayRunUi();
            object coordinator = CreateCoordinator(fixture.Run, ui);

            fixture.Run.State.AddExperience(fixture.Run.State.RequiredExperience);

            HandleExperienceChanged(
                coordinator,
                fixture.Run.State.RequiredExperience,
                fixture.Run.State.RequiredExperience);

            Assert.That(fixture.Run.State.Level, Is.EqualTo(2));
            Assert.That(fixture.Run.State.Experience, Is.Zero);
            Assert.That(fixture.Run.State.RequiredExperience, Is.EqualTo(fixture.Data.GetLevelExp(2)));
            Assert.That(ui.ShowSkillSelectionCount, Is.EqualTo(1));
            Assert.That(ui.ExperienceStatusCount, Is.EqualTo(1));
            Assert.That(ui.Level, Is.EqualTo(2));
            Assert.That(ui.CurrentExperience, Is.EqualTo(0.0f));
            Assert.That(ui.RequiredExperience, Is.EqualTo(fixture.Data.GetLevelExp(2)));
        }

        [Test]
        public void TutorialOverflow_QueuesNextCardAfterUnscaledTransition()
        {
            using ServiceTestFixture fixture = new ServiceTestFixture(RunContext.Tutorial);
            fixture.Run.State.Reset(TutorialCombatBaseline.RequiredExperienceForCard(1));
            fixture.Run.State.MarkLoaded();
            FakeGameplayRunUi ui = new FakeGameplayRunUi();
            object coordinator = CreateCoordinator(fixture.Run, ui);

            fixture.Run.State.AddExperience(25);
            HandleExperienceChanged(coordinator, 25, 5);

            Assert.That(ui.ShowSkillSelectionCount, Is.EqualTo(1));
            Assert.That(fixture.Run.State.Level, Is.EqualTo(2));
            Assert.That(fixture.Run.State.Experience, Is.EqualTo(20));
            Assert.That(fixture.Run.State.RequiredExperience, Is.EqualTo(20));

            ui.CloseSelection();
            Tick(coordinator, 0.29f);
            Assert.That(ui.ShowSkillSelectionCount, Is.EqualTo(1));
            Tick(coordinator, 0.02f);

            Assert.That(ui.ShowSkillSelectionCount, Is.EqualTo(2));
            Assert.That(fixture.Run.State.Level, Is.EqualTo(3));
            Assert.That(fixture.Run.State.Experience, Is.Zero);
            Assert.That(fixture.Run.State.RequiredExperience, Is.EqualTo(45));
        }

        [Test]
        public void TutorialBaseline_ExposesApprovedCurveSpawnAndEnemyValues()
        {
            int[] expected =
            {
                5, 20, 45, 80, 125, 180, 245, 320, 405, 500, 605,
                720, 845, 980, 1125, 1280, 1445, 1620, 1805, 2000, 2205,
            };
            for (int index = 0; index < expected.Length; index++)
                Assert.That(TutorialCombatBaseline.RequiredExperienceForCard(index + 1), Is.EqualTo(expected[index]));

            Assert.That(TutorialCombatBaseline.ResolveSpawnRate(7.0f), Is.EqualTo(1.0f));
            Assert.That(TutorialCombatBaseline.ResolveSpawnRate(30.0f), Is.EqualTo(5.2f));
            Assert.That(TutorialCombatBaseline.ResolveSpawnRate(60.0f), Is.EqualTo(8.9f));
            Assert.That(TutorialCombatBaseline.ResolveSpawnRate(90.0f), Is.EqualTo(17.5f));
            Assert.That(TutorialCombatBaseline.ResolveSpawnRate(150.0f), Is.EqualTo(8.8f));
            Assert.That(TutorialCombatBaseline.ResolveActiveSpawnEdgeCount(7.0f), Is.EqualTo(1));
            Assert.That(TutorialCombatBaseline.ResolveActiveSpawnEdgeCount(19.0f), Is.EqualTo(2));
            Assert.That(TutorialCombatBaseline.ResolveActiveSpawnEdgeCount(50.0f), Is.EqualTo(3));
            Assert.That(TutorialCombatBaseline.ResolveActiveSpawnEdgeCount(122.0f), Is.EqualTo(4));

            EnemyData small = TutorialCombatBaseline.ResolveEnemy(
                new EnemyData { Id = "small_goblin", Hp = 7, ExpReward = 1 },
                true,
                60.0f);
            Assert.That(small.Hp, Is.EqualTo(100));
            Assert.That(small.Attack, Is.EqualTo(5));
            Assert.That(small.AttackCooldown, Is.EqualTo(1.0f));
            Assert.That(small.MoveSpeed, Is.EqualTo(0.8f));
            Assert.That(small.ExpReward, Is.EqualTo(5));

            EnemyData boss = TutorialCombatBaseline.ResolveEnemy(
                new EnemyData { Id = "boss_hungry_giant", Attack = 25 },
                true,
                150.0f);
            Assert.That(boss.Hp, Is.EqualTo(8000));
            Assert.That(boss.MoveSpeed, Is.EqualTo(0.6f));
            Assert.That(boss.Attack, Is.EqualTo(25), "Excluded boss special damage must remain untouched.");
        }

        private static object CreateCoordinator(RunServices services, IGameplayRunUi ui)
        {
            Type type = typeof(RunServices).Assembly.GetType("Lizzo.PV.Gameplay.Run.RunLevelProgressionCoordinator");
            Assert.IsNotNull(type, "Missing RunLevelProgressionCoordinator test type.");
            ConstructorInfo constructor = type.GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[] { typeof(RunServices), typeof(IGameplayRunUi) },
                null);
            Assert.IsNotNull(constructor, "Missing level progression coordinator constructor.");
            return constructor.Invoke(new object[] { services, ui });
        }

        private static void HandleExperienceChanged(object coordinator, int currentExperience, int requiredExperience)
        {
            MethodInfo method = coordinator.GetType().GetMethod(
                "HandleExperienceChanged",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, "Missing level progression handler.");
            method.Invoke(coordinator, new object[] { currentExperience, requiredExperience });
        }

        private static void Tick(object coordinator, float unscaledDeltaTime)
        {
            MethodInfo method = coordinator.GetType().GetMethod(
                "Tick",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, "Missing level progression tick.");
            method.Invoke(coordinator, new object[] { unscaledDeltaTime });
        }

        private sealed class FakeGameplayRunUi : IGameplayRunUi
        {
            public event Action<bool> ModalChanged;
            public event Action MaxBuildCompleteBannerRequested;

            public bool IsThreatDirectionVisible => false;
            public int ShowSkillSelectionCount { get; private set; }
            public int ExperienceStatusCount { get; private set; }
            public int Level { get; private set; }
            public float CurrentExperience { get; private set; }
            public float RequiredExperience { get; private set; }

            public bool Initialize(RunServices services, Camera worldCamera, RunPauseController pauseController) => true;
            public void ShowGameplay() { }
            public void BindPlayer(PlayerController player) { }

            public bool ShowSkillSelection()
            {
                ShowSkillSelectionCount++;
                ModalChanged?.Invoke(true);
                return true;
            }

            public void CloseSelection() => ModalChanged?.Invoke(false);

            public bool ShowResult(RunResultViewData data, Action primaryRequested, Action optionalRequested, Action lobbyRequested) => true;
            public void CloseModal() { }
            public void SetPauseOverlay(bool visible, bool fromAppBackground) { }
            public void SetGameplaySpeed(float speed) { }
            public void SetRunStatus(int kills, float survivalSeconds) { }

            public void SetExperienceStatus(int level, float currentExperience, float requiredExperience)
            {
                ExperienceStatusCount++;
                Level = level;
                CurrentExperience = currentExperience;
                RequiredExperience = requiredExperience;
            }

            public void ShowBoss(string name, int hp, int maxHp) { }
            public void HideBoss() { }
            public void HideGameplay() { }
            public void ShowBossPreWarning(string text, Color accentColor, float durationSeconds, bool showEdges) { }
            public void HideBossPreWarning() { }
            public void ShowThreatDirection(Transform target, string label, Color accentColor, float durationSeconds = 0.0f) { }
            public void HideThreatDirection() { }
        }
    }
}
