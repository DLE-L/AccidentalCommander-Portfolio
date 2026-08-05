using System;
using System.Linq;
using System.Reflection;
using Lizzo.PV.Gameplay;
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
            SerializedField(_presentation, "_experienceValueText", CreateText("Experience"));
            SerializedField(_presentation, "_levelValueText", CreateText("Level"));
            SerializedField(_presentation, "_bossHealthValueText", CreateText("Boss"));
            SerializedField(_presentation, "_pauseIcon", CreateImage("PauseIcon"));
            SerializedField(_presentation, "_speedIcon", CreateImage("SpeedIcon"));
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
            Assert.AreEqual("3/10", Text("Experience").text);
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
}
