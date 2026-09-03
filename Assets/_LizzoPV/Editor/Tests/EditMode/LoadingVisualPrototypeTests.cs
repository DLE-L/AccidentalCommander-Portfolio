using Lizzo.PV.UI;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Lizzo.PV.EditorTests
{
    public sealed class LoadingVisualPrototypeTests
    {
        private const string PrefabPath = "Assets/_LizzoPV/Loading/Presentation/Prefabs/LoadingScreenRoot.prefab";
        private const string PreviewScenePath = "Assets/_LizzoPV/Prototypes/Loading/LoadingScreenPrototypePreview.unity";

        [Test]
        public void Prefab_PreservesSemanticLayersAndOptionalDefaultStates()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                Assert.AreEqual("LoadingScreenRoot", root.name);
                Assert.IsNotNull(root.GetComponent<CanvasGroup>());
                Assert.IsNull(root.GetComponent<Canvas>());
                Assert.IsNull(root.GetComponent<GraphicRaycaster>());

                Transform visual = Require(root.transform, "Visual");
                Transform content = Require(root.transform, "Content");
                Transform safeArea = Require(content, "SafeAreaRoot");
                Transform errorPanel = Require(safeArea, "LoadErrorPanel");
                Transform transitionEffect = Require(visual, "TransitionEffect");
                Transform inputBlocker = Require(root.transform, "InputBlocker");

                Assert.AreSame(visual.parent, content.parent);
                Assert.IsFalse(errorPanel.gameObject.activeSelf);
                Assert.IsFalse(transitionEffect.gameObject.activeSelf);
                Assert.IsFalse(inputBlocker.gameObject.activeSelf);

                SafeAreaLayout safeAreaLayout = safeArea.GetComponent<SafeAreaLayout>();
                Assert.IsNotNull(safeAreaLayout);
                SerializedObject serialized = new SerializedObject(safeAreaLayout);
                Assert.AreSame(safeArea, serialized.FindProperty("_target").objectReferenceValue);

                Image background = Require(visual, "FullScreenVisual/LoadingBackground").GetComponent<Image>();
                Assert.IsNotNull(background);
                Assert.IsTrue(background.sprite == null, "Loading prototype must not retain removed generated lobby art.");
                Assert.IsFalse(background.raycastTarget);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        [Test]
        public void Prefab_UsesOneRaycastOwnerPerOptionalControl()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                Transform loadingBar = Require(root.transform, "Content/SafeAreaRoot/LoadingBar");
                Slider slider = loadingBar.GetComponent<Slider>();
                Assert.IsNotNull(slider);
                Assert.IsFalse(slider.interactable);
                Assert.AreEqual(Selectable.Transition.None, slider.transition);
                Assert.IsNotNull(slider.fillRect);
                Assert.AreEqual("LoadingBarFill", slider.fillRect.name);

                foreach (Graphic graphic in loadingBar.GetComponentsInChildren<Graphic>(true))
                    Assert.IsFalse(graphic.raycastTarget, graphic.name);

                Transform retry = Require(root.transform, "Content/SafeAreaRoot/LoadErrorPanel/RetryButton");
                Button button = retry.GetComponent<Button>();
                Assert.IsNotNull(button);
                Assert.AreEqual(0, button.onClick.GetPersistentEventCount());
                Assert.AreSame(retry.GetComponent<Image>(), button.targetGraphic);
                Assert.AreEqual(1, CountRaycastOwners(retry));
                Assert.IsTrue(retry.GetComponent<Image>().raycastTarget);
                Assert.IsFalse(Require(retry, "Visual").GetComponent<Image>().raycastTarget);
                Assert.IsFalse(Require(retry, "RetryButtonLabel").GetComponent<TMP_Text>().raycastTarget);

                Transform inputBlocker = Require(root.transform, "InputBlocker");
                Assert.AreEqual(1, CountRaycastOwners(inputBlocker));
                Assert.IsTrue(inputBlocker.GetComponent<Image>().raycastTarget);

                Assert.IsFalse(Require(root.transform, "Content/SafeAreaRoot/ProgressText").GetComponent<TMP_Text>().raycastTarget);
                Assert.IsFalse(Require(root.transform, "Content/SafeAreaRoot/LoadErrorPanel/ErrorMessageText").GetComponent<TMP_Text>().raycastTarget);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        [Test]
        public void PreviewHarness_ReproducesPortraitCanvasContract()
        {
            Scene scene = SceneManager.GetSceneByPath(PreviewScenePath);
            bool openedForTest = !scene.IsValid() || !scene.isLoaded;
            if (openedForTest)
                scene = EditorSceneManager.OpenScene(PreviewScenePath, OpenSceneMode.Additive);

            try
            {
                Transform canvasRoot = FindRoot(scene, "@PrototypeCanvas");
                Assert.IsNotNull(canvasRoot);
                Canvas canvas = canvasRoot.GetComponent<Canvas>();
                CanvasScaler scaler = canvasRoot.GetComponent<CanvasScaler>();
                Assert.IsNotNull(canvas);
                Assert.IsNotNull(scaler);
                Assert.AreEqual(RenderMode.ScreenSpaceCamera, canvas.renderMode);
                Assert.IsNotNull(canvas.worldCamera);
                Assert.AreEqual(CanvasScaler.ScaleMode.ScaleWithScreenSize, scaler.uiScaleMode);
                Assert.AreEqual(new Vector2(1080f, 2340f), scaler.referenceResolution);
                Assert.AreEqual(CanvasScaler.ScreenMatchMode.MatchWidthOrHeight, scaler.screenMatchMode);
                Assert.AreEqual(0.5f, scaler.matchWidthOrHeight);

                Transform instance = Require(canvasRoot, "LoadingScreenRoot");
                RectTransform rect = instance as RectTransform;
                Assert.IsNotNull(rect);
                Assert.AreEqual(Vector2.zero, rect.anchorMin);
                Assert.AreEqual(Vector2.one, rect.anchorMax);
                Assert.AreEqual(Vector2.zero, rect.offsetMin);
                Assert.AreEqual(Vector2.zero, rect.offsetMax);
            }
            finally
            {
                if (openedForTest && scene.IsValid() && scene.isLoaded)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static int CountRaycastOwners(Transform root)
        {
            int count = 0;
            foreach (Graphic graphic in root.GetComponentsInChildren<Graphic>(true))
            {
                if (graphic.raycastTarget)
                    count++;
            }

            return count;
        }

        private static Transform Require(Transform root, string path)
        {
            Transform found = root.Find(path);
            Assert.IsNotNull(found, path);
            return found;
        }

        private static Transform FindRoot(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == name)
                    return root.transform;
            }

            return null;
        }
    }
}
