using System;
using System.Linq;
using Lizzo.PV.Flow;
using Lizzo.PV.Gameplay.Result;
using Lizzo.PV.UI;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class GameplayRunResultPopupContractTests
    {
        private const string ScenePath = "Assets/_LizzoPV/Scenes/Gameplay.unity";

        [TestCase("승리", true)]
        [TestCase("패배", false)]
        [TestCase("전투 종료", false)]
        public void CommonPopup_PresentsEveryOutcomeWithOneMainAction(
            string title,
            bool isClear)
        {
            using Fixture fixture = new Fixture();
            int mainCount = 0;
            fixture.Controller.MainRequested += () => mainCount++;

            Assert.That(fixture.Controller.Present(CreateView(title, isClear)), Is.True);
            Assert.That(fixture.Text("Sections/StageHeader/Visual/Ribbon/TitleText"), Is.EqualTo(title));
            Assert.That(fixture.Text("Sections/StageHeader/Visual/Ribbon/StageText"), Is.EqualTo("챕터 1"));
            Assert.That(fixture.Text("Sections/StageHeader/Visual/Ribbon/StageNameText"), Is.EqualTo("경계선의 망꾼"));
            Assert.That(fixture.Text("Sections/RewardSection/RewardList/RewardItem_00/LabelText"), Is.EqualTo("골드"));
            Assert.That(fixture.Text("Sections/RewardSection/RewardList/RewardItem_00/ValueText"), Is.EqualTo("x100"));
            Assert.That(fixture.Text("Sections/RewardSection/RewardList/RewardItem_01/LabelText"), Is.EqualTo("군단 스크롤"));
            Assert.That(fixture.Text("Sections/RewardSection/RewardList/RewardItem_01/ValueText"), Is.EqualTo("x1"));
            Assert.That(fixture.Text("Sections/Actions/MainButton/LabelText"), Is.EqualTo("메인으로"));

            Button button = fixture.Button("Sections/Actions/MainButton");
            button.onClick.Invoke();
            button.onClick.Invoke();
            Assert.That(mainCount, Is.EqualTo(1));
            Assert.That(button.interactable, Is.False);
        }

        [Test]
        public void AuthoredHierarchy_HasNoLegacyResultOrReviveSurfaces()
        {
            using Fixture fixture = new Fixture();

            Transform safeArea = fixture.Controller.transform.Find("Content/SafeAreaContent");
            Assert.That(safeArea.Find("RunResultPopup"), Is.Not.Null);
            Assert.That(safeArea.Find("MainResult"), Is.Null);
            Assert.That(safeArea.Find("FailureResult"), Is.Null);
            Assert.That(safeArea.Find("ReviveChoice"), Is.Null);
            Assert.That(
                fixture.Controller.GetComponentsInChildren<Button>(true),
                Has.Exactly(1).Matches<Button>(button => button.name == "MainButton"));
            Assert.That(
                fixture.Controller.GetComponentsInChildren<Component>(true)
                    .Any(component => component != null && component.GetType().Name.StartsWith("GameplayResult")),
                Is.False);
        }

        private static RunResultViewData CreateView(string title, bool isClear)
        {
            return new RunResultViewData(
                isClear,
                title,
                "챕터 1",
                "경계선의 망꾼",
                new[]
                {
                    new RunResultRewardPresentation(AccountResourceKind.Gold, "골드", 100),
                    new RunResultRewardPresentation(AccountResourceKind.LegionScroll, "군단 스크롤", 1),
                });
        }

        private sealed class Fixture : IDisposable
        {
            private readonly Scene _scene;
            private readonly bool _opened;
            private readonly GameObject _clone;

            public GameplayRunResultPopupController Controller { get; }

            public Fixture()
            {
                _scene = EditorSceneManager.GetSceneByPath(ScenePath);
                if (!_scene.isLoaded)
                {
                    _scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
                    _opened = true;
                }

                GameplayRunResultPopupController source = FindInScene(_scene);
                Assert.That(source, Is.Not.Null);
                _clone = UnityEngine.Object.Instantiate(source.gameObject);
                _clone.name = "RunResultPopupTestClone";
                Controller = _clone.GetComponent<GameplayRunResultPopupController>();
                Controller.Hide();
            }

            public Button Button(string relativePath)
            {
                Transform transform = Controller.transform.Find("Content/SafeAreaContent/RunResultPopup/" + relativePath);
                Assert.That(transform, Is.Not.Null, relativePath);
                return transform.GetComponent<Button>();
            }

            public string Text(string relativePath)
            {
                return ButtonOrTextTransform(relativePath).GetComponent<TMP_Text>().text;
            }

            private Transform ButtonOrTextTransform(string relativePath)
            {
                Transform transform = Controller.transform.Find("Content/SafeAreaContent/RunResultPopup/" + relativePath);
                Assert.That(transform, Is.Not.Null, relativePath);
                return transform;
            }

            public void Dispose()
            {
                if (_clone != null)
                    UnityEngine.Object.DestroyImmediate(_clone);
                if (_opened)
                    EditorSceneManager.CloseScene(_scene, false);
            }

            private static GameplayRunResultPopupController FindInScene(Scene scene)
            {
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    GameplayRunResultPopupController value =
                        root.GetComponentInChildren<GameplayRunResultPopupController>(true);
                    if (value != null)
                        return value;
                }

                return null;
            }
        }
    }
}
