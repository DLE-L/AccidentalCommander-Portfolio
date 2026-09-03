using System;
using System.Reflection;
using Lizzo.PV.Flow;
using Lizzo.PV.Gameplay.Route;
using Lizzo.PV.Gameplay.RunTraits;
using Lizzo.PV.Tests.Support;
using Lizzo.PV.UI;
using NUnit.Framework;
using UnityEngine;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class RunLevelProgressionCoordinatorTests
    {
        [Test]
        public void HandleExperienceChanged_BelowThresholdRefreshesCurrentStatus()
        {
            using ServiceTestFixture fixture = new ServiceTestFixture();
            fixture.Run.State.Reset(fixture.Data.GetLevelExp(1));
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
            FakeGameplayRunUi ui = new FakeGameplayRunUi();
            object coordinator = CreateCoordinator(fixture.Run, ui);

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
                return false;
            }

            public bool ShowResult(RunResultViewData data, Action mainRequested) => true;
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
