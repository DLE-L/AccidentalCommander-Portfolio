#if false // Superseded by GameplayRunResultPopupContractTests.
using System;
using System.Linq;
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
    public sealed class GameplayResultPresentationTests
    {
        private const string CleanScenePath = "Assets/_LizzoPV/Scenes/Gameplay.unity";

        [Test]
        public void Victory_UsesExclusiveView_AndDispatchesEachActionOnce()
        {
            using SceneFixture fixture = new SceneFixture();
            int primary = 0;
            int lobby = 0;
            fixture.Controller.PrimaryRequested += () => primary++;
            fixture.Controller.LobbyRequested += () => lobby++;

            Assert.That(fixture.Controller.PresentResult(CreateView(true)), Is.True);
            Assert.That(fixture.Controller.IsShowingMainResult, Is.True);
            Assert.That(fixture.Controller.IsShowingFailureResult, Is.False);
            Assert.That(fixture.Controller.IsShowingReviveChoice, Is.False);
            fixture.Button("MainResult/Sections/ActionRow/PrimaryButton").onClick.Invoke();
            fixture.Button("MainResult/Sections/ActionRow/LobbyButton").onClick.Invoke();
            Assert.That(primary, Is.EqualTo(1));
            Assert.That(lobby, Is.EqualTo(1));
        }

        [Test]
        public void Failure_UsesExclusiveView_AndShowsFinalBuildState()
        {
            using SceneFixture fixture = new SceneFixture();
            Assert.That(fixture.Controller.PresentResult(CreateView(false, true)), Is.True);
            Assert.That(fixture.Controller.IsShowingFailureResult, Is.True);
            Assert.That(fixture.Controller.IsShowingMainResult, Is.False);
            Assert.That(fixture.Controller.IsShowingReviveChoice, Is.False);
            Assert.That(fixture.AllText().Any(text => text == "완성"), Is.True);
            Assert.That(fixture.AllText().Any(text => text == "Gold"), Is.False);
        }

        [Test]
        public void ReviveClose_DeclinesOnce_AndReturnsToFailure()
        {
            using SceneFixture fixture = new SceneFixture();
            int revive = 0;
            fixture.Controller.ReviveRequested += () => revive++;
            Assert.That(fixture.Controller.PresentReviveChoice(CreateView(false)), Is.True);
            Assert.That(fixture.Controller.IsShowingReviveChoice, Is.True);
            fixture.Button("ReviveChoice/Content/ChoiceGroup/GiveUpButton").onClick.Invoke();
            Assert.That(fixture.Controller.IsShowingFailureResult, Is.True);
            fixture.Button("ReviveChoice/Content/ChoiceGroup/GiveUpButton").onClick.Invoke();
            Assert.That(revive, Is.Zero);
        }

        [Test]
        public void ReviveAccept_IsSingleDispatch_AndHideClearsVisibleState()
        {
            using SceneFixture fixture = new SceneFixture();
            int revive = 0;
            fixture.Controller.ReviveRequested += () => revive++;
            Assert.That(fixture.Controller.PresentReviveChoice(CreateView(false)), Is.True);
            Button button = fixture.Button("ReviveChoice/Content/ChoiceGroup/ReviveButton");
            button.onClick.Invoke();
            button.onClick.Invoke();
            Assert.That(revive, Is.EqualTo(1));
            fixture.Controller.Hide();
            Assert.That(fixture.Controller.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void BuildSummary_ClearsStaleCompanionState_AtMinimumAndMaximumCapacity()
        {
            using SceneFixture fixture = new SceneFixture();
            Sprite icon = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * .5f);
            try
            {
                PauseCompanionPresentation[] full = Enumerable.Range(0, 7).Select(_ => new PauseCompanionPresentation(icon, 3)).ToArray();
                Assert.That(fixture.Controller.PresentResult(CreateView(true, false, full)), Is.True);
                Assert.That(fixture.AllText().Any(text => text == "x3"), Is.True);
                Assert.That(fixture.Controller.PresentResult(CreateView(true)), Is.True);
                Assert.That(fixture.AllText().Any(text => text == "x3"), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(icon);
            }
        }

        [Test]
        public void PublicSeam_HasRequiredMethods()
        {
            Assert.That(typeof(GameplayResultController).GetMethod("PresentResult"), Is.Not.Null);
            Assert.That(typeof(GameplayResultController).GetMethod("PresentReviveChoice"), Is.Not.Null);
            Assert.That(typeof(GameplayResultController).GetMethod("Hide"), Is.Not.Null);
        }

        private static RunResultViewData CreateView(bool clear, bool complete = false, PauseCompanionPresentation[] companions = null)
        {
            return new RunResultViewData(clear, clear ? "승리" : "쓰러졌습니다", string.Empty, "1-1", "테스트", clear ? "계속" : "재도전", false, string.Empty, 123f, 17, 4, string.Empty, string.Empty, string.Empty, 0, false, string.Empty, string.Empty, string.Empty, string.Empty, Array.Empty<int>(), Array.Empty<RunResultSquadSlotView>(), companions ?? Array.Empty<PauseCompanionPresentation>(), Array.Empty<PausePassivePresentation>(), Array.Empty<PauseSynergyPresentation>(), null, complete);
        }

        private sealed class SceneFixture : IDisposable
        {
            private readonly Scene _scene;
            private readonly bool _opened;
            public GameplayResultController Controller { get; }

            public SceneFixture()
            {
                _scene = EditorSceneManager.GetSceneByPath(CleanScenePath);
                if (!_scene.isLoaded)
                {
                    _scene = EditorSceneManager.OpenScene(CleanScenePath, OpenSceneMode.Additive);
                    _opened = true;
                }
                Controller = FindInScene<GameplayResultController>(_scene);
                Assert.That(Controller, Is.Not.Null);
                Controller.Hide();
            }

            public Button Button(string relativePath)
            {
                Transform transform = Controller.transform.Find("Content/SafeAreaContent/" + relativePath);
                Assert.That(transform, Is.Not.Null, relativePath);
                return transform.GetComponent<Button>();
            }

            public string[] AllText() => Controller.GetComponentsInChildren<TMP_Text>(true).Select(item => item.text).ToArray();

            public void Dispose()
            {
                if (Controller != null)
                    Controller.Hide();
                if (_opened)
                    EditorSceneManager.CloseScene(_scene, true);
            }

            private static T FindInScene<T>(Scene scene) where T : Component
            {
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    T value = root.GetComponentInChildren<T>(true);
                    if (value != null)
                        return value;
                }
                return null;
            }
        }
    }
}
#endif
