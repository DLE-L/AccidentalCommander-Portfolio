using System;
using System.Reflection;
using Lizzo.PV.Flow;
using Lizzo.PV.Gameplay.Route;
using Lizzo.PV.Gameplay.Run;
using Lizzo.PV.Gameplay.RunTraits;
using Lizzo.PV.Tests.Support;
using Lizzo.PV.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class RunGameplayUpdateCoordinatorTests
    {
        [Test]
        public void Tick_UnloadedRunDoesNotAdvanceOrTouchUi()
        {
            using ServiceTestFixture fixture = new ServiceTestFixture();
            fixture.Run.State.Reset(fixture.Data.GetLevelExp(1));
            FakeGameplayRunUi ui = new FakeGameplayRunUi();
            int traitOfferPresentationCount = 0;
            object coordinator = CreateCoordinator(
                fixture.Run,
                ui,
                NoBossHealth,
                () => traitOfferPresentationCount++);

            Tick(coordinator, 60.0f, 0.016f);

            Assert.That(fixture.Run.State.ElapsedSeconds, Is.Zero);
            Assert.That(ui.RunStatusCount, Is.Zero);
            Assert.That(ui.HideBossCount, Is.Zero);
            Assert.That(ui.TraitOffer, Is.Null);
            Assert.That(traitOfferPresentationCount, Is.Zero);
        }

        [Test]
        public void Tick_LoadedRunUpdatesHudAndInvokesTraitOfferPresentation()
        {
            using ServiceTestFixture fixture = new ServiceTestFixture();
            fixture.Run.State.Reset(fixture.Data.GetLevelExp(1));
            fixture.Run.State.MarkLoaded();
            FakeGameplayRunUi ui = new FakeGameplayRunUi();
            int traitOfferPresentationCount = 0;
            object coordinator = CreateCoordinator(
                fixture.Run,
                ui,
                NoBossHealth,
                () => traitOfferPresentationCount++);
            Tick(coordinator, 60.0f, 0.016f);

            Assert.That(fixture.Run.State.ElapsedSeconds, Is.EqualTo(60.0f));
            Assert.That(ui.RunStatusCount, Is.EqualTo(1));
            Assert.That(ui.KillCount, Is.Zero);
            Assert.That(ui.ElapsedSeconds, Is.EqualTo(60.0f));
            Assert.That(ui.HideBossCount, Is.EqualTo(1));
            Assert.That(traitOfferPresentationCount, Is.EqualTo(1));
        }

        [Test]
        public void Tick_LoadedRunInvokesTutorialCompletionCorrection()
        {
            using ServiceTestFixture fixture = new ServiceTestFixture();
            fixture.Run.State.Reset(fixture.Data.GetLevelExp(1));
            fixture.Run.State.MarkLoaded();
            int correctionCount = 0;
            object coordinator = CreateCoordinator(
                fixture.Run,
                new FakeGameplayRunUi(),
                NoBossHealth,
                () => { },
                () => correctionCount++);

            Tick(coordinator, 0.0f, 0.016f);

            Assert.That(correctionCount, Is.EqualTo(1));
        }

        [Test]
        public void TraitOfferPresentation_PresentsOfferAndUsesLiveBossPhaseGateForSelection()
        {
            using ServiceTestFixture fixture = new ServiceTestFixture();
            fixture.Run.State.Reset(fixture.Data.GetLevelExp(1));
            fixture.Run.State.MarkLoaded();
            Assert.That(GetRunTraitOffers(fixture.Run).ReportEliteDefeated(), Is.True);
            FakeGameplayRunUi ui = new FakeGameplayRunUi();
            bool isBossPhaseActive = false;
            object coordinator = CreateTraitOfferPresentationCoordinator(
                fixture.Run,
                ui,
                () => isBossPhaseActive);

            LogAssert.Expect(
                LogType.Error,
                "CardCatalogProvider requires an active catalog provider before P0 cards are generated.");
            TickTraitOfferPresentation(coordinator);

            Assert.That(ui.TraitOffer, Is.Not.Null);
            Assert.That(ui.SelectionRequested, Is.Not.Null);

            RunTraitOfferSlot selected = ui.TraitOffer.Slots[0];
            isBossPhaseActive = true;
            Assert.That(ui.SelectionRequested(ui.TraitOffer.OfferIdentity, 0, selected.TraitId), Is.False);
            Assert.That(fixture.Run.RunTraits.SelectionCount, Is.Zero);

            isBossPhaseActive = false;
            Assert.That(ui.SelectionRequested(ui.TraitOffer.OfferIdentity, 0, selected.TraitId), Is.True);
            Assert.That(fixture.Run.RunTraits.SelectionCount, Is.EqualTo(1));
        }

        [Test]
        public void TraitOfferPresentation_RunTuningBossDeadlineBlocksTraitOffer()
        {
            using ServiceTestFixture fixture = new ServiceTestFixture();
            fixture.Data.SetRunTuning(tuning => tuning.BossSpawnSeconds = 45.0f);
            fixture.Run.State.Reset(fixture.Data.GetLevelExp(1));
            fixture.Run.State.MarkLoaded();
            Assert.That(GetRunTraitOffers(fixture.Run).ReportEliteDefeated(), Is.True);
            fixture.Run.State.AdvanceTime(45.0f);
            FakeGameplayRunUi ui = new FakeGameplayRunUi();
            object coordinator = CreateTraitOfferPresentationCoordinator(fixture.Run, ui, () => false);

            TickTraitOfferPresentation(coordinator);

            Assert.That(fixture.Run.State.ElapsedSeconds, Is.EqualTo(45.0f));
            Assert.That(ui.TraitOffer, Is.Null);
        }

        [Test]
        public void Tick_InjectedBossHealthSnapshotUpdatesHud()
        {
            using ServiceTestFixture fixture = new ServiceTestFixture();
            fixture.Run.State.Reset(fixture.Data.GetLevelExp(1));
            fixture.Run.State.MarkLoaded();
            FakeGameplayRunUi ui = new FakeGameplayRunUi();
            BossHealthSnapshotProvider bossHealth = (out int hp, out int maxHp) =>
            {
                hp = 25;
                maxHp = 100;
                return true;
            };
            object coordinator = CreateCoordinator(fixture.Run, ui, bossHealth, () => { });

            Tick(coordinator, 0.0f, 0.016f);

            Assert.That(ui.ShowBossCount, Is.EqualTo(1));
            Assert.That(ui.BossName, Is.EqualTo("BOSS Hungry Giant"));
            Assert.That(ui.BossHp, Is.EqualTo(25));
            Assert.That(ui.BossMaxHp, Is.EqualTo(100));
            Assert.That(ui.HideBossCount, Is.Zero);
        }

        private static object CreateCoordinator(
            RunServices services,
            IGameplayRunUi ui,
            BossHealthSnapshotProvider bossHealthSnapshotProvider,
            Action updateTraitOfferPresentation)
        {
            Type type = typeof(RunServices).Assembly.GetType("Lizzo.PV.Gameplay.Run.RunGameplayUpdateCoordinator");
            Assert.IsNotNull(type, "Missing RunGameplayUpdateCoordinator test type.");
            ConstructorInfo constructor = type.GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[]
                {
                    typeof(RunServices),
                    typeof(IGameplayRunUi),
                    typeof(BossHealthSnapshotProvider),
                    typeof(Action),
                },
                null);
            Assert.IsNotNull(constructor, "Missing gameplay update coordinator constructor.");
            return constructor.Invoke(new object[]
            {
                services,
                ui,
                bossHealthSnapshotProvider,
                updateTraitOfferPresentation,
            });
        }

        private static object CreateCoordinator(
            RunServices services,
            IGameplayRunUi ui,
            BossHealthSnapshotProvider bossHealthSnapshotProvider,
            Action updateTraitOfferPresentation,
            Action requestTutorialCompletionCorrection)
        {
            Type type = typeof(RunServices).Assembly.GetType("Lizzo.PV.Gameplay.Run.RunGameplayUpdateCoordinator");
            Assert.IsNotNull(type, "Missing RunGameplayUpdateCoordinator test type.");
            ConstructorInfo constructor = type.GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[]
                {
                    typeof(RunServices),
                    typeof(IGameplayRunUi),
                    typeof(BossHealthSnapshotProvider),
                    typeof(Action),
                    typeof(Action),
                },
                null);
            Assert.IsNotNull(constructor, "Missing tutorial correction gameplay update constructor.");
            return constructor.Invoke(new object[]
            {
                services,
                ui,
                bossHealthSnapshotProvider,
                updateTraitOfferPresentation,
                requestTutorialCompletionCorrection,
            });
        }

        private static object CreateTraitOfferPresentationCoordinator(
            RunServices services,
            IGameplayRunUi ui,
            Func<bool> isBossPhaseActive)
        {
            Type type = typeof(RunServices).Assembly.GetType(
                "Lizzo.PV.Gameplay.Run.RunTraitOfferPresentationCoordinator");
            Assert.IsNotNull(type, "Missing trait offer presentation coordinator test type.");
            ConstructorInfo constructor = type.GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[]
                {
                    typeof(RunServices),
                    typeof(IGameplayRunUi),
                    typeof(Func<bool>),
                },
                null);
            Assert.IsNotNull(constructor, "Missing trait offer presentation coordinator constructor.");
            return constructor.Invoke(new object[] { services, ui, isBossPhaseActive });
        }

        private static RunTraitOfferCoordinator GetRunTraitOffers(RunServices services)
        {
            PropertyInfo property = typeof(RunServices).GetProperty(
                "RunTraitOffers",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(property, "Missing RunServices.RunTraitOffers test seam.");
            return (RunTraitOfferCoordinator)property.GetValue(services);
        }

        private static bool NoBossHealth(out int hp, out int maxHp)
        {
            hp = 0;
            maxHp = 0;
            return false;
        }

        private static void Tick(object coordinator, float deltaTime, float unscaledDeltaTime)
        {
            MethodInfo method = coordinator.GetType().GetMethod("Tick", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, "Missing gameplay update tick method.");
            method.Invoke(coordinator, new object[] { deltaTime, unscaledDeltaTime });
        }

        private static void TickTraitOfferPresentation(object coordinator)
        {
            MethodInfo method = coordinator.GetType().GetMethod(
                "Tick",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, "Missing trait offer presentation tick method.");
            method.Invoke(coordinator, null);
        }

        private sealed class FakeGameplayRunUi : IGameplayRunUi, IRunTraitOfferUi
        {
            public event Action<bool> ModalChanged;
            public event Action MaxBuildCompleteBannerRequested;

            public bool IsThreatDirectionVisible => false;
            public bool IsModalOpen { get; set; }
            public bool IsPauseOverlayVisible { get; set; }
            public int RunStatusCount { get; private set; }
            public int KillCount { get; private set; }
            public float ElapsedSeconds { get; private set; }
            public int ShowBossCount { get; private set; }
            public string BossName { get; private set; }
            public int BossHp { get; private set; }
            public int BossMaxHp { get; private set; }
            public int HideBossCount { get; private set; }
            public RunTraitOfferSnapshot TraitOffer { get; private set; }
            public Func<string, int, string, bool> SelectionRequested { get; private set; }

            public bool Initialize(RunServices services, Camera worldCamera, RunPauseController pauseController) => true;
            public void ShowGameplay() { }
            public void BindPlayer(PlayerController player) { }
            public bool ShowSkillSelection() => true;
            public bool ShowResult(RunResultViewData data, Action primaryRequested, Action optionalRequested, Action lobbyRequested) => true;
            public void CloseModal() { }
            public void SetPauseOverlay(bool visible, bool fromAppBackground) { }
            public void SetGameplaySpeed(float speed) { }

            public void SetRunStatus(int kills, float survivalSeconds)
            {
                RunStatusCount++;
                KillCount = kills;
                ElapsedSeconds = survivalSeconds;
            }

            public void SetExperienceStatus(int level, float currentExperience, float requiredExperience) { }
            public void ShowBoss(string name, int hp, int maxHp)
            {
                ShowBossCount++;
                BossName = name;
                BossHp = hp;
                BossMaxHp = maxHp;
            }
            public void HideBoss() => HideBossCount++;
            public void HideGameplay() { }
            public void ShowBossPreWarning(string text, Color accentColor, float durationSeconds, bool showEdges) { }
            public void HideBossPreWarning() { }
            public void ShowThreatDirection(Transform target, string label, Color accentColor, float durationSeconds = 0.0f) { }
            public void HideThreatDirection() { }

            public bool ShowRunTraitOffer(
                RunTraitOfferSnapshot snapshot,
                Func<string, int, string, bool> selectionRequested)
            {
                TraitOffer = snapshot;
                SelectionRequested = selectionRequested;
                return true;
            }
        }
    }
}
