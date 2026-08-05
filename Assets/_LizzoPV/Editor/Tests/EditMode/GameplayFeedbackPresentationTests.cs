using System;
using System.Collections;
using System.Reflection;
using Lizzo.PV.Gameplay;
using Lizzo.PV.Gameplay.Feedback;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class GameplayFeedbackPresentationTests
    {
        [Test]
        public void BossWarning_RepeatedShowsPreserveAuthoredAlphaAndReplaceRgbAndText()
        {
            using FeedbackFixture fixture = new FeedbackFixture();
            Color first = new Color(0.85f, 0.20f, 0.10f, 1f);
            Color second = new Color(0.15f, 0.70f, 0.95f, 1f);

            Assert.That(fixture.Controller.ShowBossWarning("First", first, 1f, true), Is.True);
            Assert.That(fixture.WarningText.text, Is.EqualTo("First"));
            AssertAccents(fixture.Accents, first, fixture.AuthoredAlphas);

            Assert.That(fixture.Controller.ShowBossWarning("Second", second, 1f, true), Is.True);
            Assert.That(fixture.WarningText.text, Is.EqualTo("Second"));
            AssertAccents(fixture.Accents, second, fixture.AuthoredAlphas);
        }

        [Test]
        public void BossWarning_ZeroDurationAndExplicitHideLeaveNoVisibleWarning()
        {
            using FeedbackFixture fixture = new FeedbackFixture();

            Assert.That(fixture.Controller.ShowBossWarning("Zero", Color.red, 0f, true), Is.True);
            Assert.That(fixture.BossRoot.activeSelf, Is.False);

            Assert.That(fixture.Controller.ShowBossWarning("Shown", Color.red, 1f, true), Is.True);
            fixture.Controller.HideBossWarning();
            Assert.That(fixture.BossRoot.activeSelf, Is.False);
            Assert.That(fixture.CanvasGroup.alpha, Is.EqualTo(0f));
        }

        [Test]
        public void ThreatDirection_OffScreenClampsAndOnScreenSuppresses()
        {
            using FeedbackFixture fixture = new FeedbackFixture();
            fixture.Target.transform.position = Vector3.zero;

            Assert.That(fixture.Controller.ShowThreatDirection(fixture.Target.transform, null, null, Color.white), Is.True);
            Assert.That(fixture.Controller.IsThreatDirectionVisible, Is.False);

            fixture.Target.transform.position = new Vector3(100f, 0f, 0f);
            Assert.That(fixture.Controller.ShowThreatDirection(fixture.Target.transform, null, null, Color.white), Is.True);
            Assert.That(fixture.Controller.IsThreatDirectionVisible, Is.True);
            Assert.That(fixture.ThreatRoot.anchoredPosition.x, Is.InRange(508f, 540f));
            Assert.That(Mathf.Abs(fixture.Arrow.localEulerAngles.z), Is.GreaterThan(0.01f));
        }

        [Test]
        public void ThreatDirection_BehindCameraInvertsToOppositeEdge()
        {
            using FeedbackFixture fixture = new FeedbackFixture();
            fixture.Target.transform.position = new Vector3(100f, 0f, -20f);

            Assert.That(fixture.Controller.ShowThreatDirection(fixture.Target.transform, null, null, Color.white), Is.True);
            Assert.That(fixture.Controller.IsThreatDirectionVisible, Is.True);
            Assert.That(fixture.ThreatRoot.anchoredPosition.x, Is.LessThanOrEqualTo(-508f));
        }

        [Test]
        public void Controller_PresentsBossWarningAndThreatDirectionWithoutCrossHiding()
        {
            using FeedbackFixture fixture = new FeedbackFixture();
            fixture.Target.transform.position = new Vector3(100f, 0f, 0f);

            Assert.That(fixture.Controller.ShowBossWarning("Warning", Color.cyan, 1f, true), Is.True);
            Assert.That(fixture.Controller.ShowThreatDirection(fixture.Target.transform, null, null, Color.white), Is.True);
            Assert.That(fixture.BossRoot.activeSelf, Is.True);
            Assert.That(fixture.Controller.IsThreatDirectionVisible, Is.True);
        }

        [UnityTest]
        public IEnumerator ThreatDirection_FiniteDurationHidesButZeroDurationPersistsUntilExplicitHide()
        {
            using FeedbackFixture fixture = new FeedbackFixture();
            fixture.Target.transform.position = new Vector3(100f, 0f, 0f);

            Assert.That(fixture.Controller.ShowThreatDirection(fixture.Target.transform, null, null, Color.white, 0.02f), Is.True);
            yield return new WaitForSecondsRealtime(0.08f);
            Assert.That(fixture.Controller.IsThreatDirectionVisible, Is.False);

            Assert.That(fixture.Controller.ShowThreatDirection(fixture.Target.transform, null, null, Color.white, 0f), Is.True);
            yield return null;
            Assert.That(fixture.Controller.IsThreatDirectionVisible, Is.True);
            fixture.Controller.HideThreatDirection();
            Assert.That(fixture.Controller.IsThreatDirectionVisible, Is.False);
        }

        [UnityTest]
        public IEnumerator ThreatDirection_DestroyedTrackedTargetClearsVisibleStateOnNextTick()
        {
            using FeedbackFixture fixture = new FeedbackFixture();
            fixture.Target.transform.position = new Vector3(100f, 0f, 0f);

            Assert.That(fixture.Controller.ShowThreatDirection(fixture.Target.transform, null, null, Color.white), Is.True);
            Assert.That(fixture.Controller.IsThreatDirectionVisible, Is.True);

            UnityEngine.Object.DestroyImmediate(fixture.Target);
            yield return null;

            Assert.That(fixture.Controller.IsThreatDirectionVisible, Is.False);
            Assert.That(fixture.ThreatRoot.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void MissingTargetOrConfigurationFailsSafelyAndFeedbackHasNoRaycastOwners()
        {
            using FeedbackFixture fixture = new FeedbackFixture();
            Assert.That(fixture.Controller.ShowThreatDirection(null, null, null, Color.white), Is.False);

            GameObject incompleteRoot = new GameObject("IncompleteFeedbackController");
            try
            {
                GameplayFeedbackController incomplete = incompleteRoot.AddComponent<GameplayFeedbackController>();
                LogAssert.Expect(LogType.Error, "[GameplayFeedbackController] Authored feedback references are required.");
                Assert.That(incomplete.Configure(), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(incompleteRoot);
            }

            foreach (Graphic graphic in fixture.Root.GetComponentsInChildren<Graphic>(true))
                Assert.That(graphic.raycastTarget, Is.False, graphic.name);
        }

        private static void AssertAccents(Image[] accents, Color expected, float[] alphas)
        {
            for (int index = 0; index < accents.Length; index++)
            {
                Color actual = accents[index].color;
                Assert.That(actual.r, Is.EqualTo(expected.r).Within(0.0001f));
                Assert.That(actual.g, Is.EqualTo(expected.g).Within(0.0001f));
                Assert.That(actual.b, Is.EqualTo(expected.b).Within(0.0001f));
                Assert.That(actual.a, Is.EqualTo(alphas[index]).Within(0.0001f));
            }
        }

        private sealed class FeedbackFixture : IDisposable
        {
            public readonly GameObject Root;
            public readonly GameplayFeedbackController Controller;
            public readonly GameObject BossRoot;
            public readonly CanvasGroup CanvasGroup;
            public readonly TMP_Text WarningText;
            public readonly Image[] Accents;
            public readonly float[] AuthoredAlphas;
            public readonly RectTransform ThreatRoot;
            public readonly RectTransform Arrow;
            public readonly GameObject Target;
            private readonly Camera _camera;

            public FeedbackFixture()
            {
                Root = new GameObject("FeedbackFixture", typeof(RectTransform));
                Controller = Root.AddComponent<GameplayFeedbackController>();

                RectTransform viewport = CreateRect("Viewport", Root.transform);
                viewport.sizeDelta = new Vector2(1080f, 2340f);
                _camera = CreateCamera();
                BossRoot = CreateRect("BossWarning", Root.transform).gameObject;
                CanvasGroup = BossRoot.AddComponent<CanvasGroup>();
                WarningText = CreateText("WarningText", BossRoot.transform);
                Accents = CreateAccents(BossRoot.transform, out AuthoredAlphas);
                GameplayBossWarningView bossView = BossRoot.AddComponent<GameplayBossWarningView>();
                SetField(bossView, "_canvasGroup", CanvasGroup);
                SetField(bossView, "_warningText", WarningText);
                SetField(bossView, "_accentImages", Accents);

                ThreatRoot = CreateRect("ThreatDirection", Root.transform);
                ThreatRoot.anchorMin = ThreatRoot.anchorMax = new Vector2(0.5f, 0.5f);
                ThreatRoot.sizeDelta = new Vector2(97f, 92f);
                RectTransform visual = CreateRect("Visual", ThreatRoot);
                RectTransform content = CreateRect("Content", ThreatRoot);
                Image background = CreateRect("Background", visual).gameObject.AddComponent<Image>();
                background.raycastTarget = false;
                Arrow = CreateRect("Icon", visual);
                Image icon = Arrow.gameObject.AddComponent<Image>();
                icon.raycastTarget = false;
                GameplayThreatDirectionView threatView = ThreatRoot.gameObject.AddComponent<GameplayThreatDirectionView>();
                SetField(threatView, "_root", ThreatRoot);
                SetField(threatView, "_visualRoot", visual);
                SetField(threatView, "_contentRoot", content);
                SetField(threatView, "_arrowIconRoot", Arrow);
                SetField(threatView, "_backgroundImage", background);
                SetField(threatView, "_iconImage", icon);

                Target = new GameObject("ThreatTarget");
                SetField(Controller, "_bossWarning", bossView);
                SetField(Controller, "_threatDirection", threatView);
                SetField(Controller, "_viewport", viewport);
                SetField(Controller, "_worldCamera", _camera);
                Assert.That(Controller.Configure(), Is.True);
            }

            public void Dispose()
            {
                if (Target != null)
                    UnityEngine.Object.DestroyImmediate(Target);
                UnityEngine.Object.DestroyImmediate(Root);
                UnityEngine.Object.DestroyImmediate(_camera.gameObject);
            }

            private static Camera CreateCamera()
            {
                GameObject cameraObject = new GameObject("FeedbackFixtureCamera");
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 5f;
                camera.aspect = 1f;
                camera.transform.position = new Vector3(0f, 0f, -10f);
                return camera;
            }

            private static Image[] CreateAccents(Transform parent, out float[] alphas)
            {
                alphas = new[] { 0.2f, 0.6f, 0.9f };
                Image[] accents = new Image[alphas.Length];
                for (int index = 0; index < accents.Length; index++)
                {
                    accents[index] = CreateRect("Accent" + index, parent).gameObject.AddComponent<Image>();
                    accents[index].color = new Color(0.1f, 0.2f, 0.3f, alphas[index]);
                    accents[index].raycastTarget = false;
                }

                return accents;
            }

            private static TMP_Text CreateText(string name, Transform parent)
            {
                TextMeshProUGUI text = CreateRect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
                text.raycastTarget = false;
                return text;
            }

            private static RectTransform CreateRect(string name, Transform parent)
            {
                GameObject item = new GameObject(name, typeof(RectTransform));
                item.transform.SetParent(parent, false);
                return item.GetComponent<RectTransform>();
            }

            private static void SetField(object target, string fieldName, object value)
            {
                FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
                if (field == null)
                    throw new MissingFieldException(target.GetType().Name, fieldName);

                field.SetValue(target, value);
            }
        }
    }
}
