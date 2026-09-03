using System.Reflection;
using Lizzo.PV.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Lizzo.PV.EditorTests
{
    public sealed class GameplaySafeAreaLayoutTests
    {
        private const string GameplayScenePath = "Assets/_LizzoPV/Scenes/Gameplay.unity";
        private static readonly MethodInfo NormalizeSafeAreaMethod = typeof(SafeAreaLayout).GetMethod(
            "NormalizeSafeArea",
            BindingFlags.Static | BindingFlags.NonPublic);

        [Test]
        public void GameplayCanvas_UsesPortraitHeightContractAndSafeInteractiveLayers()
        {
            Scene scene = SceneManager.GetSceneByPath(GameplayScenePath);
            bool openedForTest = !scene.IsValid() || !scene.isLoaded;
            if (openedForTest)
                scene = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Additive);

            try
            {
                Transform root = FindPath(scene, "GameplayUIRoot");
                Assert.IsNotNull(root);

                CanvasScaler scaler = root.GetComponent<CanvasScaler>();
                Assert.IsNotNull(scaler);
                Assert.AreEqual(CanvasScaler.ScaleMode.ScaleWithScreenSize, scaler.uiScaleMode);
                Assert.AreEqual(new Vector2(1080f, 2340f), scaler.referenceResolution);
                Assert.AreEqual(CanvasScaler.ScreenMatchMode.MatchWidthOrHeight, scaler.screenMatchMode);
                Assert.AreEqual(1f, scaler.matchWidthOrHeight);

                AssertSafeAreaOwns(root.Find("HUDLayer"));
                AssertSafeAreaOwns(root.Find("InputLayer"));
                Assert.IsNull(root.Find("DecisionLayer").GetComponent<SafeAreaLayout>());
                Assert.IsNull(root.Find("FeedbackLayer").GetComponent<SafeAreaLayout>());
            }
            finally
            {
                if (openedForTest && scene.IsValid() && scene.isLoaded)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void LgV50PortraitFixture_NormalizesTopInset()
        {
            Rect normalized = Normalize(new Rect(0f, 0f, 1440f, 3040f), 1440, 3120);

            Assert.AreEqual(0f, normalized.xMin);
            Assert.AreEqual(0f, normalized.yMin);
            Assert.AreEqual(1f, normalized.xMax);
            Assert.AreEqual(3040f / 3120f, normalized.yMax, 0.0001f);
        }

        [Test]
        public void GalaxyZFold2InnerFixture_PreservesExtraWidthAndTopInset()
        {
            const int screenWidth = 1768;
            const int screenHeight = 2208;
            Rect normalized = Normalize(new Rect(0f, 0f, 1768f, 2120f), screenWidth, screenHeight);

            Assert.AreEqual(0f, normalized.xMin);
            Assert.AreEqual(0f, normalized.yMin);
            Assert.AreEqual(1f, normalized.xMax);
            Assert.AreEqual(2120f / 2208f, normalized.yMax, 0.0001f);

            float heightMatchedLogicalWidth = screenWidth * (2340f / screenHeight);
            Assert.Greater(heightMatchedLogicalWidth, 1080f);
        }

        private static Rect Normalize(Rect safeArea, int screenWidth, int screenHeight)
        {
            Assert.IsNotNull(NormalizeSafeAreaMethod);
            return (Rect)NormalizeSafeAreaMethod.Invoke(null, new object[] { safeArea, screenWidth, screenHeight });
        }

        private static void AssertSafeAreaOwns(Transform transform)
        {
            Assert.IsNotNull(transform);
            SafeAreaLayout layout = transform.GetComponent<SafeAreaLayout>();
            Assert.IsNotNull(layout, transform.name);

            var serialized = new SerializedObject(layout);
            SerializedProperty target = serialized.FindProperty("_target");
            Assert.IsNotNull(target);
            Assert.AreSame(transform, target.objectReferenceValue);
        }

        private static Transform FindPath(Scene scene, string path)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == path)
                    return root.transform;
            }

            return null;
        }
    }
}
