using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Lizzo.PV.Flow;
using Lizzo.PV.Gameplay;
using Lizzo.PV.Gameplay.UI.HUD;
using Lizzo.PV.Legion.Synergy;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TestTools;

namespace Lizzo.PV.EditorTests
{
    public sealed class GameplayHudControllerTests
    {
        GameObject _root;
        GameplayHudController _controller;
        GameplayHudPresentationController _presentation;
        Button _pauseButton;
        Button _speedButton;
        Slider _experienceSlider;
        Slider _bossHealthSlider;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("HudRoot");
            RectTransform rootRect = _root.AddComponent<RectTransform>();
            _controller = _root.AddComponent<GameplayHudController>();
            _presentation = _root.AddComponent<GameplayHudPresentationController>();
            _pauseButton = CreateButton("PauseEntry");
            _speedButton = CreateButton("SpeedEntry");

            SerializedField(_presentation, "_killValueText", CreateText("Kills"));
            SerializedField(_presentation, "_survivalTimerValueText", CreateText("Timer"));
            SerializedField(_presentation, "_levelValueText", CreateText("Level"));
            SerializedField(_presentation, "_bossHealthValueText", CreateText("Boss"));
            SerializedField(_presentation, "_pauseIcon", CreateImage("PauseIcon"));
            SerializedField(_presentation, "_speedIcon", CreateImage("SpeedIcon"));
            SerializedField(_presentation, "_speedValueText", CreateText("SpeedValue"));
            _experienceSlider = CreateSlider("ExperienceBar");
            _bossHealthSlider = CreateSlider("BossHealthBar");
            SerializedField(_presentation, "_experienceSlider", _experienceSlider);
            SerializedField(_presentation, "_bossHealthSlider", _bossHealthSlider);

            SerializedField(_controller, "_presentation", _presentation);
            SerializedField(_controller, "_hud", rootRect);
            SerializedField(_controller, "_pauseEntry", _pauseButton);
            SerializedField(_controller, "_speedEntry", _speedButton);
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_root);
        }

        [Test]
        public void Configure_RequiredReferencesFailExplicitly()
        {
            SerializedField(_controller, "_presentation", null);
            LogAssert.Expect(LogType.Error, "[GameplayHudController] Authored HUD references are required.");
            Assert.IsFalse(_controller.Configure());
        }

        [Test]
        public void Presentations_FormatRunExperienceAndBossValues()
        {
            Assert.IsTrue(_controller.Configure());

            _controller.SetRunStatus(12, 65.9f);
            _controller.SetExperience(2, 3, 10);
            _controller.ShowBoss(25, 100);

            Assert.AreEqual("12", Text("Kills").text);
            Assert.AreEqual("01:05", Text("Timer").text);
            Assert.AreEqual("2", Text("Level").text);
            Assert.AreEqual("25/100", Text("Boss").text);
            Assert.IsTrue(_root.transform.Find("Boss") == null || _root.transform.Find("Boss").gameObject.activeSelf);
        }

        [Test]
        public void BossAndExperienceVisibilityAreMutuallyExclusive()
        {
            Assert.IsTrue(_controller.Configure());
            _controller.ShowBoss(1, 2);
            Assert.IsFalse(_experienceSlider.gameObject.activeSelf);
            Assert.IsTrue(_bossHealthSlider.gameObject.activeSelf);

            _controller.HideBoss();
            Assert.IsTrue(_experienceSlider.gameObject.activeSelf);
            Assert.IsFalse(_bossHealthSlider.gameObject.activeSelf);
        }

        [Test]
        public void PauseAndSpeedClicksForwardExactlyOnceAfterRepeatedConfiguration()
        {
            Assert.IsTrue(_controller.Configure());
            Assert.IsTrue(_controller.Configure());
            int pauseCount = 0;
            int speedCount = 0;
            _controller.PauseRequested += () => pauseCount++;
            _controller.SpeedToggleRequested += () => speedCount++;

            _pauseButton.onClick.Invoke();
            _speedButton.onClick.Invoke();

            Assert.AreEqual(1, pauseCount);
            Assert.AreEqual(1, speedCount);
        }

        [Test]
        public void SpeedPresentationAlwaysShowsTheActualTestMultiplier()
        {
            Assert.IsTrue(_controller.Configure());

            _controller.SetGameplaySpeed(1.0f);
            Assert.AreEqual("x1", Text("SpeedValue").text);
            Assert.IsFalse(_root.transform.Find("SpeedIcon").GetComponent<Image>().enabled);

            _controller.SetGameplaySpeed(5.0f);
            Assert.AreEqual("x5", Text("SpeedValue").text);
            Assert.IsFalse(_root.transform.Find("SpeedIcon").GetComponent<Image>().enabled);
        }

        [Test]
        public void HudContractHasNoGoldSurface()
        {
            string[] names = typeof(GameplayHudController)
                .GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .Select(field => field.Name)
                .Concat(typeof(GameplayHudPresentationController)
                    .GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                    .Select(field => field.Name))
                .ToArray();
            Assert.IsFalse(names.Any(name => name.IndexOf("gold", StringComparison.OrdinalIgnoreCase) >= 0));
        }

        [Test]
        public void HudFixtureHasExactlyTwoRaycastOwners()
        {
            Assert.IsTrue(_controller.Configure());
            Image[] images = _root.GetComponentsInChildren<Image>(true);
            Assert.AreEqual(2, images.Count(image => image.raycastTarget));
        }

        TMP_Text Text(string name) => _root.transform.Find(name).GetComponent<TMP_Text>();

        TMP_Text CreateText(string name)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(_root.transform);
            return child.AddComponent<TextMeshProUGUI>();
        }

        Image CreateImage(string name)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(_root.transform);
            Image image = child.AddComponent<Image>();
            image.raycastTarget = false;
            return image;
        }

        Button CreateButton(string name)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(_root.transform);
            Button button = child.AddComponent<Button>();
            Image image = child.AddComponent<Image>();
            image.raycastTarget = true;
            button.targetGraphic = image;
            return button;
        }

        Slider CreateSlider(string name)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(_root.transform);
            return child.AddComponent<Slider>();
        }

        static void SerializedField(UnityEngine.Object target, string fieldName, UnityEngine.Object value)
        {
            UnityEditor.SerializedObject serialized = new UnityEditor.SerializedObject(target);
            serialized.FindProperty(fieldName).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

    }

    public sealed class SynergyNotificationBannerControllerTests
    {
        [Test]
        public void GameSceneConfiguresBannerAfterUiInitializationAndBeforeGameplayShows()
        {
            string sourcePath = Path.Combine(Application.dataPath, "_LizzoPV/Gameplay/Run/Runtime/GameScene.cs");
            string source = File.ReadAllText(sourcePath);
            int uiInitialization = source.IndexOf(
                "if (!_uiController.Initialize(_services, mainCamera, _pauseController))",
                StringComparison.Ordinal);
            int bannerConfiguration = source.IndexOf(
                "_synergyNotificationBanner.Configure(_services.Build1SynergyProgression, _pauseController)",
                StringComparison.Ordinal);
            int showGameplay = source.IndexOf("_uiController.ShowGameplay();", StringComparison.Ordinal);

            Assert.That(uiInitialization, Is.GreaterThanOrEqualTo(0));
            Assert.That(bannerConfiguration, Is.GreaterThan(uiInitialization));
            Assert.That(showGameplay, Is.GreaterThan(bannerConfiguration));
        }

        [Test]
        public void SameRefreshShowsEachSynergyNameOnlyOnItsFirstVisibleTransition()
        {
            using SynergyBannerFixture fixture = new SynergyBannerFixture();
            fixture.SetStages(Build1SynergyStage.Ready, Build1SynergyStage.None, Build1SynergyStage.None);
            Assert.IsTrue(fixture.Configure());

            fixture.SetStages(Build1SynergyStage.Complete, Build1SynergyStage.None, Build1SynergyStage.Ready);
            fixture.Refresh();

            Assert.AreEqual("혼성 지휘", fixture.Message);
            Assert.AreEqual(1.2f, fixture.RemainingSeconds);

            fixture.ExpireActiveMessage();
            Assert.IsFalse(fixture.IsVisible);
        }

        [Test]
        public void UnchangedRefreshDoesNotRepeatOrQueue()
        {
            using SynergyBannerFixture fixture = new SynergyBannerFixture();
            Assert.IsTrue(fixture.Configure());
            fixture.SetStages(Build1SynergyStage.Ready, Build1SynergyStage.None, Build1SynergyStage.None);
            fixture.Refresh();
            Assert.AreEqual("근위대", fixture.Message);

            fixture.ExpireActiveMessage();
            Assert.IsFalse(fixture.IsVisible);

            fixture.Refresh();
            Assert.IsFalse(fixture.IsVisible);
        }

        [Test]
        public void PauseFreezesActiveMessageAndQueueProgression()
        {
            using SynergyBannerFixture fixture = new SynergyBannerFixture();
            fixture.SetStages(Build1SynergyStage.Ready, Build1SynergyStage.None, Build1SynergyStage.None);
            Assert.IsTrue(fixture.Configure());
            fixture.SetStages(Build1SynergyStage.Complete, Build1SynergyStage.None, Build1SynergyStage.Ready);
            fixture.Refresh();
            Assert.AreEqual("혼성 지휘", fixture.Message);

            fixture.Pause();
            fixture.SetRemainingSeconds(0.0f);
            fixture.Refresh();

            Assert.IsTrue(fixture.IsVisible);
            Assert.AreEqual("혼성 지휘", fixture.Message);

            fixture.Unpause();
            fixture.Refresh();
            Assert.AreEqual("혼성 지휘", fixture.Message);
        }

        [Test]
        public void SingleStageNotificationKeepsExistingCopyAndDuration()
        {
            using SynergyBannerFixture fixture = new SynergyBannerFixture();
            Assert.IsTrue(fixture.Configure());
            fixture.SetStages(Build1SynergyStage.None, Build1SynergyStage.Ready, Build1SynergyStage.None);

            fixture.Refresh();

            Assert.AreEqual("폭발단", fixture.Message);
            Assert.AreEqual(1.2f, fixture.RemainingSeconds);
        }

        [Test]
        public void InitializationAndDisableClearQueuedNotificationState()
        {
            using SynergyBannerFixture fixture = new SynergyBannerFixture();
            fixture.SetStages(Build1SynergyStage.Ready, Build1SynergyStage.None, Build1SynergyStage.None);
            Assert.IsTrue(fixture.Configure());
            fixture.SetStages(Build1SynergyStage.Complete, Build1SynergyStage.None, Build1SynergyStage.Ready);
            fixture.Refresh();
            Assert.IsTrue(fixture.IsVisible);

            fixture.Disable();
            Assert.IsFalse(fixture.IsVisible);

            fixture.Enable();
            Assert.IsTrue(fixture.Configure());
            fixture.ExpireActiveMessage();
            Assert.IsFalse(fixture.IsVisible);
        }

        sealed class SynergyBannerFixture : IDisposable
        {
            readonly GameObject _root;
            readonly GameObject _banner;
            readonly TMP_Text _message;
            readonly SynergyNotificationBannerController _controller;
            readonly RunPauseController _pause;
            readonly Build1SynergyProgression _progression;
            readonly Build1SynergyStage[] _stages = new Build1SynergyStage[3];
            readonly int[] _conditionCounts = new int[3];

            public SynergyBannerFixture()
            {
                _root = new GameObject("SynergyBannerFixture");
                _controller = _root.AddComponent<SynergyNotificationBannerController>();
                _banner = new GameObject("Banner");
                _banner.transform.SetParent(_root.transform);
                _message = _banner.AddComponent<TextMeshProUGUI>();
                SetSerializedField(_controller, "_notificationBanner", _banner);
                SetSerializedField(_controller, "_notificationMessageText", _message);

                GameObject pauseRoot = new GameObject("SynergyBannerPause");
                _pause = pauseRoot.AddComponent<RunPauseController>();
                _pause.Initialize();
                _progression = (Build1SynergyProgression)FormatterServices.GetUninitializedObject(typeof(Build1SynergyProgression));
                SetField(_progression, "_stages", _stages);
                SetField(_progression, "_conditionCounts", _conditionCounts);
            }

            public string Message => _message.text;
            public bool IsVisible => _banner.activeSelf;
            public float RemainingSeconds => (float)GetField(_controller, "_bannerRemainingSeconds");

            public bool Configure() => _controller.Configure(_progression, _pause);

            public void SetStages(Build1SynergyStage guard, Build1SynergyStage explosive, Build1SynergyStage mixed)
            {
                _stages[0] = guard;
                _stages[1] = explosive;
                _stages[2] = mixed;
            }

            public void Refresh()
            {
                typeof(SynergyNotificationBannerController)
                    .GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(_controller, null);
            }
            public void SetRemainingSeconds(float value) => SetField(_controller, "_bannerRemainingSeconds", value);
            public void ExpireActiveMessage()
            {
                SetRemainingSeconds(0.0f);
                Refresh();
            }

            public void Pause() => _pause.ToggleUserPause();
            public void Unpause() => _pause.ToggleUserPause();
            public void Disable()
            {
                _controller.enabled = false;
                typeof(SynergyNotificationBannerController)
                    .GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(_controller, null);
            }
            public void Enable() => _controller.enabled = true;

            public void Dispose()
            {
                UnityEngine.Object.DestroyImmediate(_pause.gameObject);
                UnityEngine.Object.DestroyImmediate(_root);
            }

            static object GetField(object target, string name)
            {
                return target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
            }

            static void SetField(object target, string name, object value)
            {
                target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
            }

            static void SetSerializedField(UnityEngine.Object target, string fieldName, UnityEngine.Object value)
            {
                UnityEditor.SerializedObject serialized = new UnityEditor.SerializedObject(target);
                serialized.FindProperty(fieldName).objectReferenceValue = value;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
