using Lizzo.PV.Gameplay.Pause;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Lizzo.PV.EditorTests
{
    public sealed class GameplayPauseVisualLayoutTests
    {
        private const string CleanScenePath = "Assets/_LizzoPV/Scenes/Gameplay.unity";
        private const string CleanSynergyPrefabPath = "Assets/_LizzoPV/Gameplay/UI/Prefabs/Pause/GameplayPauseSynergyItem.prefab";

        [Test]
        public void CleanPause_PreservesDonorLayoutAndSemanticInputOwnership()
        {
            Scene scene = SceneManager.GetSceneByPath(CleanScenePath);
            bool openedForTest = !scene.IsValid() || !scene.isLoaded;
            if (openedForTest)
                scene = EditorSceneManager.OpenScene(CleanScenePath, OpenSceneMode.Additive);

            try
            {
                Transform pause = FindPath(scene, "GameplayUIRoot/DecisionLayer/Pause");
                Assert.IsNotNull(pause);

                RectTransform panelVisual = RequireRect(pause, "Visual/Panel");
                RectTransform content = RequireRect(pause, "Content");
                Assert.AreEqual(new Vector2(920f, 860f), panelVisual.sizeDelta);
                Assert.AreEqual(panelVisual.anchorMin, content.anchorMin);
                Assert.AreEqual(panelVisual.anchorMax, content.anchorMax);
                Assert.AreEqual(panelVisual.anchoredPosition, content.anchoredPosition);
                Assert.AreEqual(panelVisual.sizeDelta, content.sizeDelta);

                RectTransform companionSlots = RequireRect(pause, "Content/BuildSummary/Companions/Content/Slots");
                RectTransform passiveSlots = RequireRect(pause, "Content/BuildSummary/Passives/Content/Slots");
                Assert.AreEqual(7, companionSlots.childCount);
                Assert.AreEqual(5, passiveSlots.childCount);
                AssertPauseSummaryLayout(pause, companionSlots, passiveSlots);

                GridLayoutGroup grid = RequireRect(
                    pause,
                    "Content/BuildSummary/CompletedSynergies/Content/CompletedSynergyList")
                    .GetComponent<GridLayoutGroup>();
                Assert.IsNotNull(grid);
                Assert.AreEqual(new Vector2(415f, 32f), grid.cellSize);
                Assert.AreEqual(new Vector2(8f, 4f), grid.spacing);
                Assert.AreEqual(GridLayoutGroup.Constraint.FixedColumnCount, grid.constraint);
                Assert.AreEqual(2, grid.constraintCount);

                Graphic[] graphics = pause.GetComponentsInChildren<Graphic>(true);
                int raycastOwners = 0;
                foreach (Graphic graphic in graphics)
                {
                    if (graphic.raycastTarget)
                        raycastOwners++;
                }

                Assert.AreEqual(3, raycastOwners);
                Assert.IsTrue(RequireRect(pause, "ModalInputBlocker").GetComponent<Image>().raycastTarget);
                Assert.IsTrue(RequireRect(pause, "Content/Actions/LobbyButton").GetComponent<Image>().raycastTarget);
                Assert.IsTrue(RequireRect(pause, "Content/Actions/ResumeButton").GetComponent<Image>().raycastTarget);
                Assert.AreEqual(0, pause.GetComponentsInChildren<Canvas>(true).Length);
                Assert.AreEqual(0, pause.GetComponentsInChildren<GraphicRaycaster>(true).Length);

                CanvasGroup canvasGroup = pause.GetComponent<CanvasGroup>();
                Assert.IsNotNull(canvasGroup);
                Assert.AreEqual(0f, canvasGroup.alpha);
                Assert.IsFalse(canvasGroup.interactable);
                Assert.IsFalse(canvasGroup.blocksRaycasts);
                Assert.IsTrue(pause.gameObject.activeSelf);
            }
            finally
            {
                if (openedForTest && scene.IsValid() && scene.isLoaded)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void CleanSynergyItem_UsesAuthoredTwoColumnVisualContract()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(CleanSynergyPrefabPath);
            try
            {
                RectTransform rect = root.GetComponent<RectTransform>();
                Assert.AreEqual(new Vector2(415f, 32f), rect.sizeDelta);
                Assert.IsNotNull(root.GetComponent<GameplayPauseSynergyItemView>());

                Transform visual = root.transform.Find("Visual");
                Assert.IsNotNull(visual);
                Assert.AreEqual(2, visual.childCount);

                TMP_Text nameText = root.transform.Find("Content/NameText").GetComponent<TMP_Text>();
                Assert.IsNotNull(nameText);
                Assert.IsTrue(nameText.enableAutoSizing);
                Assert.AreEqual(14f, nameText.fontSizeMin);
                Assert.AreEqual(24f, nameText.fontSizeMax);
                Assert.AreEqual(TextOverflowModes.Ellipsis, nameText.overflowMode);

                foreach (Graphic graphic in root.GetComponentsInChildren<Graphic>(true))
                    Assert.IsFalse(graphic.raycastTarget, graphic.name);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static Transform FindPath(Scene scene, string path)
        {
            string[] parts = path.Split('/');
            Transform current = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name != parts[0])
                    continue;
                current = root.transform;
                break;
            }

            for (int index = 1; current != null && index < parts.Length; index++)
                current = current.Find(parts[index]);
            return current;
        }

        private static RectTransform RequireRect(Transform root, string path)
        {
            Transform found = root.Find(path);
            Assert.IsNotNull(found, path);
            RectTransform rect = found as RectTransform;
            Assert.IsNotNull(rect, path);
            return rect;
        }

        private static void AssertPauseSummaryLayout(Transform pause, RectTransform companionSlots, RectTransform passiveSlots)
        {
            RectTransform buildSummary = RequireRect(pause, "Content/BuildSummary");
            VerticalLayoutGroup buildLayout = buildSummary.GetComponent<VerticalLayoutGroup>();
            Assert.IsNotNull(buildLayout);
            Assert.AreEqual(18f, buildLayout.spacing);
            Assert.AreEqual(8, buildLayout.padding.left);
            Assert.AreEqual(8, buildLayout.padding.right);
            Assert.IsTrue(buildLayout.childControlWidth);
            Assert.IsTrue(buildLayout.childControlHeight);
            Assert.IsFalse(buildLayout.childForceExpandWidth);
            Assert.IsFalse(buildLayout.childForceExpandHeight);
            Assert.AreEqual(TextAnchor.UpperCenter, buildLayout.childAlignment);

            AssertSectionLayout(pause, "Companions", 156f);
            AssertSectionLayout(pause, "Passives", 156f);
            AssertSectionLayout(pause, "CompletedSynergies", 210f);
            AssertSlotRowLayout(companionSlots, 7, 12f, 4, 42);
            AssertSlotRowLayout(passiveSlots, 5, 20f, 4, 242);

            RectTransform synergyContent = RequireRect(pause, "Content/BuildSummary/CompletedSynergies/Content");
            VerticalLayoutGroup synergyLayout = synergyContent.GetComponent<VerticalLayoutGroup>();
            Assert.IsNotNull(synergyLayout);
            Assert.AreEqual(12f, synergyLayout.spacing);
            Assert.IsFalse(synergyLayout.childControlWidth);
            Assert.IsFalse(synergyLayout.childControlHeight);
            Assert.IsFalse(synergyLayout.childForceExpandWidth);
            Assert.IsFalse(synergyLayout.childForceExpandHeight);
            LayoutElement emptyStateLayout = RequireRect(pause, "Content/BuildSummary/CompletedSynergies/Content/EmptyStateText")
                .GetComponent<LayoutElement>();
            Assert.IsNotNull(emptyStateLayout);
            Assert.IsTrue(emptyStateLayout.ignoreLayout);
        }

        private static void AssertSectionLayout(Transform pause, string sectionName, float preferredHeight)
        {
            RectTransform section = RequireRect(pause, "Content/BuildSummary/" + sectionName);
            LayoutElement element = section.GetComponent<LayoutElement>();
            Assert.IsNotNull(element);
            Assert.AreEqual(854f, element.minWidth);
            Assert.AreEqual(854f, element.preferredWidth);
            Assert.AreEqual(preferredHeight, element.minHeight);
            Assert.AreEqual(preferredHeight, element.preferredHeight);

            RectTransform content = RequireRect(section, "Content");
            VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
            Assert.IsNotNull(layout);
            Assert.AreEqual(12f, layout.spacing);
            Assert.IsFalse(layout.childControlWidth);
            Assert.IsFalse(layout.childControlHeight);
            Assert.IsFalse(layout.childForceExpandWidth);
            Assert.IsFalse(layout.childForceExpandHeight);
            Assert.AreEqual(TextAnchor.UpperCenter, layout.childAlignment);
        }

        private static void AssertSlotRowLayout(RectTransform row, int expectedCount, float spacing, int leftPadding, int rightPadding)
        {
            HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
            Assert.IsNotNull(layout);
            Assert.AreEqual(spacing, layout.spacing);
            Assert.AreEqual(leftPadding, layout.padding.left);
            Assert.AreEqual(rightPadding, layout.padding.right);
            Assert.IsFalse(layout.childControlWidth);
            Assert.IsFalse(layout.childControlHeight);
            Assert.IsFalse(layout.childForceExpandWidth);
            Assert.IsFalse(layout.childForceExpandHeight);
            Assert.AreEqual(TextAnchor.MiddleLeft, layout.childAlignment);

            Assert.AreEqual(expectedCount, row.childCount);
            for (int index = 0; index < row.childCount; index++)
            {
                RectTransform slot = row.GetChild(index) as RectTransform;
                Assert.IsNotNull(slot);
                Assert.AreEqual(new Vector2(104f, 104f), slot.sizeDelta);
                LayoutElement element = slot.GetComponent<LayoutElement>();
                Assert.IsNotNull(element);
                Assert.AreEqual(104f, element.minWidth);
                Assert.AreEqual(104f, element.minHeight);
                Assert.AreEqual(104f, element.preferredWidth);
                Assert.AreEqual(104f, element.preferredHeight);
            }
        }
    }
}
