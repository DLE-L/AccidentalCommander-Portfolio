using System.Linq;
using Lizzo.PV.UI;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class UI_PauseOverlayLiveSceneAuthoringTests
    {
        private const string GameplayScenePath = "Assets/_LizzoPV/Scenes/Gameplay.unity";
        private const string SemiBoldFontPath = "Assets/_LizzoPV/Fonts/Pretendard/TMP/Pretendard-SemiBold SDF.asset";
        private const string InfoContentPath = "GameplayUIRoot/HUD/PauseOverlay/Panel/InfoContent";
        private const string SynergySectionPath = InfoContentPath + "/SynergySection";
        private const string SynergyBodyPath = SynergySectionPath + "/SynergyBody";
        private const string SynergyListPath = SynergyBodyPath + "/SynergyList";

        [Test]
        public void GameplayPauseOverlay_HasBoundSemanticSynergyEmptyState()
        {
            Scene gameplayScene = EditorSceneManager.GetSceneByPath(GameplayScenePath);
            bool openedGameplayScene = false;
            if (!gameplayScene.isLoaded)
            {
                gameplayScene = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Additive);
                openedGameplayScene = true;
            }

            try
            {
                Transform gameplayUiRoot = gameplayScene.GetRootGameObjects()
                    .Single(root => root.name == "GameplayUIRoot")
                    .transform;
                Transform infoContent = FindTransform(gameplayUiRoot, InfoContentPath.Substring("GameplayUIRoot/".Length));
                Transform synergySection = FindTransform(gameplayUiRoot, SynergySectionPath.Substring("GameplayUIRoot/".Length));
                Transform synergyBody = FindTransform(gameplayUiRoot, SynergyBodyPath.Substring("GameplayUIRoot/".Length));
                Transform synergyList = FindTransform(gameplayUiRoot, SynergyListPath.Substring("GameplayUIRoot/".Length));
                UI_PauseOverlay overlay = FindTransform(gameplayUiRoot, "HUD/PauseOverlay").GetComponent<UI_PauseOverlay>();

                Assert.That(overlay, Is.Not.Null);
                SerializedObject overlaySerialized = new SerializedObject(overlay);
                Assert.That(overlaySerialized.FindProperty("_infoScroll"), Is.Null);

                Assert.That(overlay.Validate(), Is.True);
                Canvas.ForceUpdateCanvases();
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(infoContent.GetComponent<RectTransform>());
                Assert.That(infoContent.GetComponent<Image>().raycastTarget, Is.False);

                CanvasScaler scaler = gameplayUiRoot.Find("HUD").GetComponent<CanvasScaler>();
                Assert.That(scaler.referenceResolution, Is.EqualTo(new Vector2(1080f, 1920f)));
                Assert.That(scaler.referenceResolution.x / scaler.referenceResolution.y, Is.EqualTo(9f / 16f).Within(0.0001f));

                Transform emptyState = synergyBody.Find("SynergyEmptyStateText");
                Assert.That(emptyState, Is.Not.Null);
                TextMeshProUGUI emptyStateText = emptyState.GetComponent<TextMeshProUGUI>();
                Assert.That(emptyStateText, Is.Not.Null);
                Assert.That(emptyStateText.text, Is.EqualTo("활성 시너지 없음"));
                Assert.That(emptyStateText.alignment, Is.EqualTo(TextAlignmentOptions.Center));
                Assert.That(emptyStateText.raycastTarget, Is.False);
                Assert.That(AssetDatabase.GetAssetPath(emptyStateText.font), Is.EqualTo(SemiBoldFontPath));

                RectTransform rectTransform = emptyState.GetComponent<RectTransform>();
                Assert.That(rectTransform.anchorMin, Is.EqualTo(Vector2.zero));
                Assert.That(rectTransform.anchorMax, Is.EqualTo(Vector2.one));
                Assert.That(rectTransform.pivot, Is.EqualTo(new Vector2(0.5f, 0.5f)));
                Assert.That(rectTransform.anchoredPosition, Is.EqualTo(Vector2.zero));
                Assert.That(rectTransform.sizeDelta, Is.EqualTo(Vector2.zero));

                TMP_Text boundEmptyState = new SerializedObject(overlay)
                    .FindProperty("_synergyEmptyStateText")
                    .objectReferenceValue as TMP_Text;
                Assert.That(boundEmptyState, Is.SameAs(emptyStateText));
                Assert.That(synergyList.parent, Is.SameAs(synergyBody));
                Assert.That(emptyState.parent, Is.SameAs(synergyBody));
                Assert.That(infoContent.GetComponentsInChildren<ScrollRect>(true), Is.Empty);
                Assert.That(infoContent.GetComponentsInChildren<Scrollbar>(true), Is.Empty);

                Transform companionRow = infoContent.Find("CompanionSection/CompanionSlotRow");
                Transform passiveRow = infoContent.Find("PassiveSection/PassiveSlotRow");
                Assert.That(companionRow, Is.Not.Null);
                Assert.That(passiveRow, Is.Not.Null);
                Assert.That(companionRow.childCount, Is.EqualTo(7));
                Assert.That(passiveRow.childCount, Is.EqualTo(5));
                AssertSlotRowIsLeftAligned(companionRow, 12f);
                AssertSlotRowIsLeftAligned(passiveRow, 20f);
                Assert.That(synergySection.GetComponent<RectTransform>().rect.height, Is.GreaterThan(150f));

                GridLayoutGroup synergyGrid = synergyList.GetComponent<GridLayoutGroup>();
                Assert.That(synergyGrid.childAlignment, Is.EqualTo(TextAnchor.UpperLeft));
                Assert.That(synergyGrid.constraint, Is.EqualTo(GridLayoutGroup.Constraint.FixedColumnCount));
                Assert.That(synergyGrid.constraintCount, Is.EqualTo(2));
                Assert.That(synergyGrid.cellSize.x * 2f + synergyGrid.spacing.x + synergyGrid.padding.horizontal, Is.LessThanOrEqualTo(synergyList.GetComponent<RectTransform>().rect.width + 0.5f));
                Assert.That(synergyGrid.cellSize.y * 4f + synergyGrid.spacing.y * 3f + synergyGrid.padding.vertical, Is.LessThanOrEqualTo(synergyBody.GetComponent<RectTransform>().rect.height + 0.5f));

                SerializedProperty levelTexts = overlaySerialized.FindProperty("_passiveLevelTexts");
                Assert.That(levelTexts, Is.Not.Null);
                Assert.That(levelTexts.arraySize, Is.EqualTo(5));
                SerializedProperty companionIcons = overlaySerialized.FindProperty("_companionIconImages");
                SerializedProperty companionCounts = overlaySerialized.FindProperty("_companionSlotCountTexts");
                SerializedProperty passiveIcons = overlaySerialized.FindProperty("_passiveIconImages");
                Assert.That(companionIcons.arraySize, Is.EqualTo(7));
                Assert.That(companionCounts.arraySize, Is.EqualTo(7));
                Assert.That(passiveIcons.arraySize, Is.EqualTo(5));
                for (int index = 0; index < companionIcons.arraySize; index++)
                {
                    Image icon = companionIcons.GetArrayElementAtIndex(index).objectReferenceValue as Image;
                    TMP_Text count = companionCounts.GetArrayElementAtIndex(index).objectReferenceValue as TMP_Text;
                    Assert.That(icon, Is.Not.Null);
                    Assert.That(icon.rectTransform.anchorMin, Is.EqualTo(new Vector2(0.5f, 0.5f)));
                    Assert.That(icon.rectTransform.anchorMax, Is.EqualTo(new Vector2(0.5f, 0.5f)));
                    Assert.That(icon.rectTransform.pivot, Is.EqualTo(new Vector2(0.5f, 0.5f)));
                    Assert.That(icon.rectTransform.anchoredPosition, Is.EqualTo(Vector2.zero));
                    Assert.That(icon.rectTransform.sizeDelta, Is.EqualTo(new Vector2(104f, 104f)));
                    Assert.That(Vector3.Distance(icon.transform.localScale, new Vector3(0.83f, 0.83f, 0.83f)), Is.LessThan(0.005f));
                    Assert.That(count, Is.Not.Null);
                    Assert.That(count.text, Is.EqualTo("×1"));
                }
                for (int index = 0; index < passiveIcons.arraySize; index++)
                {
                    Image icon = passiveIcons.GetArrayElementAtIndex(index).objectReferenceValue as Image;
                    Assert.That(icon, Is.Not.Null);
                    Assert.That(icon.rectTransform.anchorMin, Is.EqualTo(new Vector2(0.5f, 0.5f)));
                    Assert.That(icon.rectTransform.anchorMax, Is.EqualTo(new Vector2(0.5f, 0.5f)));
                    Assert.That(icon.rectTransform.pivot, Is.EqualTo(new Vector2(0.5f, 0.5f)));
                    Assert.That(icon.rectTransform.anchoredPosition, Is.EqualTo(Vector2.zero));
                    Assert.That(icon.rectTransform.sizeDelta, Is.EqualTo(new Vector2(80f, 80f)));
                    Assert.That(icon.transform.localScale, Is.EqualTo(Vector3.one));
                    Assert.That(icon.preserveAspect, Is.True);
                }
                for (int index = 0; index < levelTexts.arraySize; index++)
                {
                    TMP_Text levelText = levelTexts.GetArrayElementAtIndex(index).objectReferenceValue as TMP_Text;
                    Assert.That(levelText, Is.Not.Null);
                    Assert.That(levelText.name, Is.EqualTo("LevelText"));
                    Assert.That(levelText.transform.parent.name, Is.EqualTo("PassiveSlot_0" + index));
                    Assert.That(levelText.text, Is.EqualTo("Lv.3"));
                    Assert.That(levelText.raycastTarget, Is.False);
                    Assert.That(AssetDatabase.GetAssetPath(levelText.font), Is.EqualTo(SemiBoldFontPath));
                    RectTransform levelRect = levelText.rectTransform;
                    Assert.That(levelRect.anchorMin, Is.EqualTo(new Vector2(1f, 0f)));
                    Assert.That(levelRect.anchorMax, Is.EqualTo(new Vector2(1f, 0f)));
                    Assert.That(levelRect.pivot, Is.EqualTo(new Vector2(1f, 0f)));
                    Assert.That(levelRect.anchoredPosition, Is.EqualTo(new Vector2(-4f, 4f)));
                    Assert.That(levelRect.sizeDelta, Is.EqualTo(new Vector2(48f, 24f)));
                }
            }
            finally
            {
                if (openedGameplayScene)
                    EditorSceneManager.CloseScene(gameplayScene, true);
            }
        }

        [Test]
        public void GameplayPauseOverlay_HasFiveBlankPassiveEmptyVisuals()
        {
            Scene gameplayScene = EditorSceneManager.GetSceneByPath(GameplayScenePath);
            bool openedGameplayScene = false;
            if (!gameplayScene.isLoaded)
            {
                gameplayScene = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Additive);
                openedGameplayScene = true;
            }

            try
            {
                Transform gameplayUiRoot = gameplayScene.GetRootGameObjects()
                    .Single(root => root.name == "GameplayUIRoot")
                    .transform;
                Transform passiveRow = FindTransform(gameplayUiRoot, "HUD/PauseOverlay/Panel/InfoContent/PassiveSection/PassiveSlotRow");
                UI_PauseOverlay overlay = FindTransform(gameplayUiRoot, "HUD/PauseOverlay").GetComponent<UI_PauseOverlay>();
                SerializedProperty emptyRoots = new SerializedObject(overlay).FindProperty("_passiveEmptySlotRoots");

                Assert.That(passiveRow.childCount, Is.EqualTo(5));
                Assert.That(emptyRoots, Is.Not.Null);
                Assert.That(emptyRoots.arraySize, Is.EqualTo(5));

                string[] firstVisualDescendants = null;
                bool[] firstVisualStates = null;
                for (int index = 0; index < 5; index++)
                {
                    Transform slot = passiveRow.GetChild(index);
                    Assert.That(slot.name, Is.EqualTo("PassiveSlot_0" + index));
                    Transform emptyVisual = slot.Find("EmptyVisual");
                    Assert.That(emptyVisual, Is.Not.Null);
                    Assert.That(emptyRoots.GetArrayElementAtIndex(index).objectReferenceValue, Is.SameAs(emptyVisual.gameObject));

                    Transform[] descendants = emptyVisual.GetComponentsInChildren<Transform>(true);
                    Assert.That(descendants.Any(child => child != emptyVisual && child.name.IndexOf("lock", System.StringComparison.OrdinalIgnoreCase) >= 0), Is.False);
                    Assert.That(descendants.Any(child => child.name == "Icon" && child.gameObject.activeSelf && child.GetComponent<Image>()?.sprite != null), Is.False);

                    string[] descendantNames = descendants.Select(child => child.name).ToArray();
                    bool[] descendantStates = descendants.Select(child => child.gameObject.activeSelf).ToArray();
                    if (firstVisualDescendants == null)
                    {
                        firstVisualDescendants = descendantNames;
                        firstVisualStates = descendantStates;
                    }
                    else
                    {
                        Assert.That(descendantNames, Is.EqualTo(firstVisualDescendants));
                        Assert.That(descendantStates, Is.EqualTo(firstVisualStates));
                    }
                }
            }
            finally
            {
                if (openedGameplayScene)
                    EditorSceneManager.CloseScene(gameplayScene, true);
            }
        }

        private static Transform FindTransform(Transform root, string path)
        {
            Transform target = root.Find(path);
            Assert.That(target, Is.Not.Null, root.name + "/" + path);
            return target;
        }

        private static void AssertSlotRowIsLeftAligned(Transform row, float expectedSpacing)
        {
            RectTransform rowRect = row.GetComponent<RectTransform>();
            HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
            Assert.That(layout.childAlignment, Is.EqualTo(TextAnchor.MiddleLeft));
            Assert.That(layout.spacing, Is.EqualTo(expectedSpacing).Within(0.01f));
            Assert.That(layout.childForceExpandWidth, Is.False);
            Assert.That(layout.childForceExpandHeight, Is.False);

            float previousMax = rowRect.rect.xMin + layout.padding.left;
            for (int index = 0; index < row.childCount; index++)
            {
                RectTransform slot = row.GetChild(index).GetComponent<RectTransform>();
                Assert.That(slot.rect.width, Is.EqualTo(104f).Within(0.5f));
                Assert.That(slot.rect.height, Is.EqualTo(104f).Within(0.5f));
                Assert.That(IsContained(rowRect, slot), Is.True, row.name + " slot " + index);

                float minX = GetLocalBounds(rowRect, slot).min.x;
                Assert.That(minX, Is.EqualTo(previousMax).Within(0.5f), row.name + " slot " + index + " left alignment/gap");
                previousMax = minX + slot.rect.width + layout.spacing;
            }
        }

        private static Bounds GetLocalBounds(RectTransform parent, RectTransform child)
        {
            Vector3[] corners = new Vector3[4];
            child.GetWorldCorners(corners);
            Bounds bounds = new Bounds(parent.InverseTransformPoint(corners[0]), Vector3.zero);
            for (int index = 1; index < corners.Length; index++)
                bounds.Encapsulate(parent.InverseTransformPoint(corners[index]));
            return bounds;
        }

        private static bool IsContained(RectTransform parent, RectTransform child)
        {
            Vector3[] corners = new Vector3[4];
            child.GetWorldCorners(corners);
            for (int index = 0; index < corners.Length; index++)
            {
                Vector3 local = parent.InverseTransformPoint(corners[index]);
                if (local.x < parent.rect.xMin - 0.5f
                    || local.x > parent.rect.xMax + 0.5f
                    || local.y < parent.rect.yMin - 0.5f
                    || local.y > parent.rect.yMax + 0.5f)
                    return false;
            }

            return true;
        }
    }
}
