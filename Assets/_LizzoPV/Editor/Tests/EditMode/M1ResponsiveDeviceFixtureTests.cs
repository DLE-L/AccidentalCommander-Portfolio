using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Lizzo.PV.Flow;
using Lizzo.PV.Presentation;
using Lizzo.PV.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Lizzo.PV.EditorTests
{
    public sealed class M1ResponsiveDeviceFixtureTests
    {
        private static readonly MethodInfo NormalizeSafeAreaMethod = typeof(SafeAreaLayout).GetMethod(
            "NormalizeSafeArea", BindingFlags.Static | BindingFlags.NonPublic);

        [Test]
        public void AndroidPlayer_DisablesRenderOutsideSafeArea()
        {
            Assert.That(PlayerSettings.Android.renderOutsideSafeArea, Is.False);
        }

        [TestCase("PV_LGV50_1080x2340", 1080, 2340, 0, 0, 1080, 2280)]
        [TestCase("PV_FoldInner_1768x2208", 1768, 2208, 0, 0, 1768, 2120)]
        [TestCase("PV_FoldCover_Narrow", 832, 2268, 0, 0, 832, 2196)]
        [TestCase("PV_Portrait_1080x1920", 1080, 1920, 0, 0, 1080, 1920)]
        public void DeviceFixture_NormalizesSafeAreaInsideViewport(
            string fixture, int width, int height, int x, int y, int safeWidth, int safeHeight)
        {
            Assert.That(NormalizeSafeAreaMethod, Is.Not.Null);
            Rect safeArea = new Rect(x, y, safeWidth, safeHeight);
            Rect normalized = (Rect)NormalizeSafeAreaMethod.Invoke(null, new object[] { safeArea, width, height });
            Assert.That(normalized.xMin, Is.GreaterThanOrEqualTo(0f), fixture);
            Assert.That(normalized.yMin, Is.GreaterThanOrEqualTo(0f), fixture);
            Assert.That(normalized.xMax, Is.LessThanOrEqualTo(1f), fixture);
            Assert.That(normalized.yMax, Is.LessThanOrEqualTo(1f), fixture);
            Assert.That(normalized.width, Is.EqualTo((float)safeWidth / width).Within(0.0001f), fixture);
            Assert.That(normalized.height, Is.EqualTo((float)safeHeight / height).Within(0.0001f), fixture);
        }

        [Test]
        public void ProductionScenes_UseHeightMatchedLogicalFrameAndAuthoredSafeAreaOwners()
        {
            string[] paths =
            {
                "Assets/_LizzoPV/Scenes/Loading.unity",
                "Assets/_LizzoPV/Scenes/Lobby.unity",
                "Assets/_LizzoPV/Scenes/Gameplay.unity",
            };

            var opened = new List<Scene>();
            try
            {
                foreach (string path in paths)
                {
                    Scene scene = SceneManager.GetSceneByPath(path);
                    if (!scene.IsValid() || !scene.isLoaded)
                    {
                        scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                        opened.Add(scene);
                    }

                    GameObject[] roots = scene.GetRootGameObjects();
                    CanvasScaler[] scalers = roots.SelectMany(root => root.GetComponentsInChildren<CanvasScaler>(true)).ToArray();
                    Assert.That(scalers, Is.Not.Empty, path);
                    foreach (CanvasScaler scaler in scalers)
                    {
                        Assert.That(scaler.uiScaleMode, Is.EqualTo(CanvasScaler.ScaleMode.ScaleWithScreenSize), path);
                        Assert.That(scaler.referenceResolution, Is.EqualTo(new Vector2(1080f, 2340f)), path);
                        Assert.That(scaler.screenMatchMode, Is.EqualTo(CanvasScaler.ScreenMatchMode.MatchWidthOrHeight), path);
                        Assert.That(scaler.matchWidthOrHeight, Is.EqualTo(1f), path);
                    }

                    SafeAreaLayout[] safeAreas = roots.SelectMany(root => root.GetComponentsInChildren<SafeAreaLayout>(true)).ToArray();
                    Assert.That(safeAreas, Is.Not.Empty, path);
                    foreach (SafeAreaLayout safeArea in safeAreas)
                    {
                        SerializedProperty target = new SerializedObject(safeArea).FindProperty("_target");
                        Assert.That(target, Is.Not.Null, path);
                        Assert.That(target.objectReferenceValue, Is.SameAs(safeArea.transform), safeArea.name);
                    }
                    Assert.That(scene.isDirty, Is.False, path);
                }
            }
            finally
            {
                for (int index = opened.Count - 1; index >= 0; index--)
                    EditorSceneManager.CloseScene(opened[index], true);
            }
        }

        [Test]
        public void FoldInner_KeepsVerticalWorldSpanAndExpandsHorizontalWorldSpan()
        {
            const float orthographicSize = 6f;
            float referenceAspect = 1080f / 2340f;
            float foldAspect = 1768f / 2208f;
            float referenceHeight = orthographicSize * 2f;
            float foldHeight = orthographicSize * 2f;
            float referenceWidth = referenceHeight * referenceAspect;
            float foldWidth = foldHeight * foldAspect;

            Assert.That(foldHeight, Is.EqualTo(referenceHeight));
            Assert.That(foldWidth, Is.GreaterThan(referenceWidth));
            Assert.That(CameraController.InitialOrthographicSize, Is.EqualTo(orthographicSize));
        }

        [Test]
        public void LoadingScene_HasRenderedCanvasMotionAndSingleInputBlockerContract()
        {
            const string path = "Assets/_LizzoPV/Scenes/Loading.unity";
            Scene scene = SceneManager.GetSceneByPath(path);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened)
                scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                GameObject[] roots = scene.GetRootGameObjects();
                LoadingPresentationBinder binder = roots.SelectMany(root => root.GetComponentsInChildren<LoadingPresentationBinder>(true)).Single();
                SceneTransitionOverlay overlay = roots.SelectMany(root => root.GetComponentsInChildren<SceneTransitionOverlay>(true)).Single();
                Canvas canvas = binder.GetComponentInParent<Canvas>();
                Assert.That(canvas, Is.Not.Null);
                Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));
                Assert.That(canvas.GetComponent<CanvasScaler>(), Is.Not.Null);
                Assert.That(canvas.GetComponent<GraphicRaycaster>(), Is.Not.Null);

                SerializedObject serializedBinder = new SerializedObject(binder);
                foreach (string field in new[] { "_presentationSet", "_background", "_barTrack", "_barFill", "_errorPanel", "_motionPlayer", "_audioSource" })
                    Assert.That(serializedBinder.FindProperty(field).objectReferenceValue, Is.Not.Null, field);
                Assert.That(binder.GetComponent<Animation>(), Is.Not.Null);
                Assert.That(binder.GetComponent<UiMotionPlayer>(), Is.Not.Null);

                SerializedObject serializedOverlay = new SerializedObject(overlay);
                GameObject visualRoot = serializedOverlay.FindProperty("_visualRoot").objectReferenceValue as GameObject;
                Assert.That(visualRoot, Is.SameAs(binder.gameObject));
                Image blocker = visualRoot.transform.Find("InputBlocker")?.GetComponent<Image>();
                Assert.That(blocker, Is.Not.Null);
                Assert.That(blocker.raycastTarget, Is.True);
                Assert.That(scene.isDirty, Is.False);
            }
            finally
            {
                if (opened)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
