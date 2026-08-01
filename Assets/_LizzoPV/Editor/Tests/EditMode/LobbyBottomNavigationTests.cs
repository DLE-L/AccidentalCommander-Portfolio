using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Lizzo.PV.EditorTests;
using Lizzo.PV.UI;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class LobbyBottomNavigationTests
    {
        const string FrameDonorPath = "Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Blue/Prefabs/Prefabs_Frame/ItemFrame/ItemFrame_01_Normal_Blue.prefab";
        static readonly string[] ButtonNames = { "LegionNavButton", "BattleNavButton", "TraitNavButton", "RelicNavButton", "ShopNavButton" };
        static readonly string[] Labels = { "군단", "출정", "특성", "유물", "상점" };
        static readonly string[] ExpectedLabels = { "군단", "출정", "특성", "유물", "상점" };

[Test]
        public void RuntimeSelectionView_RequiresExplicitSerializedTabBindings()
        {
            GameObject host = new GameObject("UnconfiguredLobbyBottomNavigationView");
            try
            {
                LobbyBottomNavigationView view = host.AddComponent<LobbyBottomNavigationView>();
                SerializedObject serialized = new SerializedObject(view);
                SerializedProperty bindings = serialized.FindProperty("_tabBindings");
                Assert.That(bindings, Is.Not.Null,
                    "The runtime view must own an explicit serialized tab-binding collection.");
                LogAssert.Expect(LogType.Error,
                    "[LobbyBottomNavigationView] Exactly five explicit serialized tab bindings are required.");
                Assert.That(view.Configure(), Is.False,
                    "An unconfigured runtime view must fail explicitly before authoring supplies bindings.");
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

[Test]
        public void RuntimeSelectionView_ExposesProjectOwnedPublicSeam()
        {
            Scene lobby = LobbySceneTestContext.OpenLobby();
            Transform bottom = LobbySceneTestContext.Find(lobby, "@HomeLobby/SafeArea/BottomNavigation");
            Transform candidate = bottom.Find("Visual/BottomNavigationVisual");
            Assert.That(candidate, Is.Not.Null);
            Assert.That(candidate.GetComponent("Lizzo.PV.UI.LobbyBottomNavigationView"), Is.Not.Null,
                "The accepted candidate must be owned by the project runtime selection view.");
        }

[Test]
        public void RuntimeSelectionView_UpdatesPublicVisualStateWithoutMovingButtonHitboxes()
        {
            Scene lobby = LobbySceneTestContext.OpenLobby();
            Transform bottom = LobbySceneTestContext.Find(lobby, "@HomeLobby/SafeArea/BottomNavigation");
            Transform candidate = bottom.Find("Visual/BottomNavigationVisual");
            LobbyBottomNavigationView view = candidate.GetComponent<LobbyBottomNavigationView>();
            Assert.That(view, Is.Not.Null);
            Assert.That(view.Configure(), Is.True);

            Button[] buttons = new Button[ButtonNames.Length];
            Vector2[] anchorMins = new Vector2[ButtonNames.Length];
            Vector2[] anchorMaxs = new Vector2[ButtonNames.Length];
            Vector2[] sizeDeltas = new Vector2[ButtonNames.Length];
            Vector3[] positions = new Vector3[ButtonNames.Length];
            Vector3[] scales = new Vector3[ButtonNames.Length];
            for (int i = 0; i < ButtonNames.Length; i++)
            {
                buttons[i] = bottom.Find(ButtonNames[i]).GetComponent<Button>();
                RectTransform rect = buttons[i].GetComponent<RectTransform>();
                anchorMins[i] = rect.anchorMin;
                anchorMaxs[i] = rect.anchorMax;
                sizeDeltas[i] = rect.sizeDelta;
                positions[i] = rect.anchoredPosition3D;
                scales[i] = rect.localScale;
            }

            view.SetSelected(buttons[0]);
            AssertRuntimeVisualState(candidate, bottom, buttons, 0);
            view.SetSelected(buttons[4]);
            AssertRuntimeVisualState(candidate, bottom, buttons, 4);

            for (int i = 0; i < ButtonNames.Length; i++)
            {
                RectTransform current = buttons[i].GetComponent<RectTransform>();
                Assert.That(current.anchorMin, Is.EqualTo(anchorMins[i]), ButtonNames[i]);
                Assert.That(current.anchorMax, Is.EqualTo(anchorMaxs[i]), ButtonNames[i]);
                Assert.That(current.sizeDelta, Is.EqualTo(sizeDeltas[i]), ButtonNames[i]);
                Assert.That(current.anchoredPosition3D, Is.EqualTo(positions[i]), ButtonNames[i]);
                Assert.That(current.localScale, Is.EqualTo(scales[i]), ButtonNames[i]);
            }
        }

[Test]
        public void RuntimeSelectionView_FollowsPublicLobbyNavigationSelection()
        {
            Scene lobby = LobbySceneTestContext.OpenLobby();
            LobbyNavigationShell shell = LobbySceneTestContext.Find(lobby, "@HomeLobby").GetComponent<LobbyNavigationShell>();
            Transform bottom = LobbySceneTestContext.Find(lobby, "@HomeLobby/SafeArea/BottomNavigation");
            Transform candidate = bottom.Find("Visual/BottomNavigationVisual");
            LobbyBottomNavigationView view = candidate.GetComponent<LobbyBottomNavigationView>();
            Button shop = bottom.Find("ShopNavButton").GetComponent<Button>();
            Assert.That(shell, Is.Not.Null);
            Assert.That(view, Is.Not.Null);
            Assert.That(shell.Configure(), Is.True);

            shell.Show(null);
            shop.onClick.Invoke();
            AssertRuntimeVisualState(candidate, bottom,
                new[]
                {
                    bottom.Find("LegionNavButton").GetComponent<Button>(),
                    bottom.Find("BattleNavButton").GetComponent<Button>(),
                    bottom.Find("TraitNavButton").GetComponent<Button>(),
                    bottom.Find("RelicNavButton").GetComponent<Button>(),
                    shop,
                }, 4);
            shell.Hide();
        }

[Test]
        public void Authoring_UsesContinuousChassisAndSelectedRise()
        {
            Scene lobby = LobbySceneTestContext.OpenLobby();
            Transform bottom = LobbySceneTestContext.Find(lobby, "@HomeLobby/SafeArea/BottomNavigation");
            Assert.That(bottom, Is.Not.Null);
            GameObject frameDonor = AssetDatabase.LoadAssetAtPath<GameObject>(FrameDonorPath);
            Assert.That(frameDonor, Is.Not.Null);
            Transform candidate = bottom.Find("Visual/BottomNavigationVisual");
            Assert.That(candidate, Is.Not.Null, "The production bottom-navigation visual must exist.");
            Assert.That(candidate.gameObject.activeSelf, Is.True);
            LobbyBottomNavigationView view = candidate.GetComponent<LobbyBottomNavigationView>();
            Assert.That(view, Is.Not.Null);
            view.SetSelected(bottom.Find("BattleNavButton").GetComponent<Button>());

            RectTransform candidateRect = candidate.GetComponent<RectTransform>();
            Assert.That(candidateRect.sizeDelta.y, Is.EqualTo(200f).Within(0.01f));
            Assert.That(candidateRect.localScale, Is.EqualTo(Vector3.one));
            AssertLayer(candidate.Find("Chassis/Bg"), Image.Type.Sliced);
            Color chassisFace = candidate.Find("Chassis/Bg").GetComponent<Image>().color;
            Assert.That(chassisFace.r, Is.GreaterThan(0.95f));
            Assert.That(chassisFace.g, Is.GreaterThan(0.95f));
            Assert.That(chassisFace.b, Is.GreaterThan(0.95f));
            AssertLayer(candidate.Find("Chassis/InnerBorder"), Image.Type.Sliced);
            AssertLayer(candidate.Find("Chassis/Border"), Image.Type.Sliced);
            AssertLayer(candidate.Find("Chassis/Shadow"), Image.Type.Sliced);
            Assert.That(candidate.Find("Chassis/Bg").GetComponent<Image>().sprite,
                Is.SameAs(frameDonor.transform.Find("Bg").GetComponent<Image>().sprite));
            Assert.That(candidate.Find("Chassis/InnerBorder").GetComponent<Image>().sprite,
                Is.SameAs(frameDonor.transform.Find("InnerBorder1").GetComponent<Image>().sprite));
            Assert.That(candidate.Find("Chassis/Border").GetComponent<Image>().sprite,
                Is.SameAs(frameDonor.transform.Find("Border").GetComponent<Image>().sprite));
            Assert.That(candidate.Find("Chassis/Shadow").GetComponent<Image>().sprite,
                Is.SameAs(frameDonor.transform.Find("Bg").GetComponent<Image>().sprite));

            Transform cells = candidate.Find("Cells");
            Assert.That(cells, Is.Not.Null);
            Transform dividers = candidate.Find("Dividers");
            Assert.That(dividers, Is.Not.Null);
            Assert.That(dividers.GetComponentsInChildren<Image>(true).Length, Is.EqualTo(4));
            Assert.That(dividers.GetSiblingIndex(), Is.LessThan(cells.GetSiblingIndex()));
            Color expectedDividerColor = new Color(0.0627f, 0.1725f, 0.3843f, 0.70f);
            for (int i = 0; i < 4; i++)
            {
                Transform divider = dividers.Find("Divider_0" + i);
                Assert.That(divider, Is.Not.Null);
                Image dividerImage = divider.GetComponent<Image>();
                Assert.That(dividerImage, Is.Not.Null);
                Assert.That(dividerImage.sprite, Is.Null);
                Assert.That(dividerImage.type, Is.EqualTo(Image.Type.Simple));
                Assert.That(dividerImage.color.r, Is.EqualTo(expectedDividerColor.r).Within(0.01f));
                Assert.That(dividerImage.color.g, Is.EqualTo(expectedDividerColor.g).Within(0.01f));
                Assert.That(dividerImage.color.b, Is.EqualTo(expectedDividerColor.b).Within(0.01f));
                Assert.That(dividerImage.color.a, Is.EqualTo(expectedDividerColor.a).Within(0.01f));
                Assert.That(dividerImage.raycastTarget, Is.False);
                RectTransform dividerRect = divider.GetComponent<RectTransform>();
                Assert.That(dividerRect.anchorMin.x, Is.EqualTo((i + 1) * 0.2f).Within(0.001f));
                Assert.That(dividerRect.anchorMax.x, Is.EqualTo((i + 1) * 0.2f).Within(0.001f));
                Assert.That(dividerRect.sizeDelta.x, Is.EqualTo(3f).Within(0.01f));
                Assert.That(dividerRect.sizeDelta.y, Is.EqualTo(176f).Within(0.01f));
                Assert.That(dividerRect.anchoredPosition.x, Is.EqualTo(0f).Within(0.01f));
                Assert.That(dividerRect.anchoredPosition.y, Is.EqualTo(12f).Within(0.01f));
                Assert.That(dividerRect.localPosition.z, Is.EqualTo(0f).Within(0.001f));
                Assert.That(dividerRect.localScale, Is.EqualTo(Vector3.one));
            }
            for (int i = 0; i < ButtonNames.Length; i++)
            {
                Transform cell = cells.Find("Cell_0" + i);
                Assert.That(cell, Is.Not.Null);
                RectTransform rect = cell.GetComponent<RectTransform>();
                Assert.That(rect.pivot, Is.EqualTo(new Vector2(0.5f, 0f)), ButtonNames[i]);
                Assert.That(rect.anchorMin.x, Is.EqualTo(i * 0.2f).Within(0.001f));
                Assert.That(rect.anchorMax.x, Is.EqualTo((i + 1) * 0.2f).Within(0.001f));
                Assert.That(rect.rect.width, Is.EqualTo(206.4f).Within(0.1f));
                Assert.That(rect.rect.height, Is.EqualTo(i == 1 ? 218f : 200f).Within(0.01f));
                Assert.That(cell.Find("IconZone"), Is.Null, ButtonNames[i]);
                Assert.That(cell.Find("LabelZone"), Is.Null, ButtonNames[i]);
                Transform semanticButton = bottom.Find(ButtonNames[i]);
                Image semanticIcon = semanticButton.Find("Icon").GetComponent<Image>();
                TMP_Text semanticLabel = semanticButton.Find("Label").GetComponent<TMP_Text>();
                Assert.That(semanticIcon.gameObject.activeSelf, Is.True, ButtonNames[i]);
                Assert.That(semanticIcon.enabled, Is.True, ButtonNames[i]);
                Assert.That(semanticIcon.preserveAspect, Is.True, ButtonNames[i]);
                Assert.That(semanticLabel.gameObject.activeSelf, Is.True, ButtonNames[i]);
                Assert.That(semanticLabel.enabled, Is.True, ButtonNames[i]);
                Assert.That(semanticLabel.text, Is.EqualTo(Labels[i]), ButtonNames[i]);
                Assert.That(cell.Find("Divider"), Is.Null, ButtonNames[i]);
            }

            Assert.That(cells.Find("Cell_01/Selected"), Is.Not.Null);
            Assert.That(cells.Find("Cell_01/Selected").GetComponent<RectTransform>().rect.height, Is.EqualTo(218f).Within(0.01f));
            foreach (Graphic graphic in candidate.GetComponentsInChildren<Graphic>(true))
            {
                Assert.That(graphic.raycastTarget, Is.False, graphic.name);
            }
        }

[Test]
        public void Authoring_PreservesFiveSemanticButtonOwners()
        {
            Scene lobby = LobbySceneTestContext.OpenLobby();
            Transform bottom = LobbySceneTestContext.Find(lobby, "@HomeLobby/SafeArea/BottomNavigation");
            Transform candidate = bottom.Find("Visual/BottomNavigationVisual");
            Assert.That(candidate, Is.Not.Null);
            for (int i = 0; i < ButtonNames.Length; i++)
            {
                Transform button = bottom.Find(ButtonNames[i]);
                Assert.That(button, Is.Not.Null, ButtonNames[i]);
                Assert.That(button.GetComponent<Button>(), Is.Not.Null, ButtonNames[i]);
                Assert.That(button.GetComponent<Image>().raycastTarget, Is.True, ButtonNames[i]);
                Assert.That(button.Find("Label").GetComponent<TMP_Text>().text, Is.EqualTo(Labels[i]));
                Assert.That(button.GetComponentsInChildren<Button>(true).Length, Is.EqualTo(1), ButtonNames[i]);
            }
        }
        [Test]
        public void SemanticNames_UseProductionHierarchyAndSerializedShellKeys()
        {
            Scene lobby = LobbySceneTestContext.OpenLobby();
            Transform bottom = Find(lobby, "@HomeLobby/SafeArea/BottomNavigation");
            Assert.That(bottom.Find("LegionNavButton"), Is.Not.Null);
            Assert.That(bottom.Find("TraitNavButton"), Is.Not.Null);
            Assert.That(bottom.Find("RelicNavButton"), Is.Not.Null);
            Assert.That(bottom.Find("LegionNavButton/Icon"), Is.Not.Null);
            Transform destination = Find(lobby, "@HomeLobby/SafeArea/ContentViewport/DestinationPanel");
            Assert.That(destination, Is.Not.Null);
            Assert.That(destination.Find("DestinationTitle"), Is.Not.Null);
            Assert.That(destination.Find("ComingSoonIcon"), Is.Not.Null);
            Assert.That(destination.Find("LegionTabs"), Is.Not.Null);
            Assert.That(destination.Find("DestinationScrollViewport/ScrollContent/ShopContent"), Is.Not.Null);
        }

        [Test]
        public void BottomNavigation_UsesPopupVisualLayerAndOneInteractionOwner()
        {
            Scene lobby = LobbySceneTestContext.OpenLobby();
            Transform bottom = Find(lobby, "@HomeLobby/SafeArea/BottomNavigation");
            LobbyNavigationShell shell = Find(lobby, "@HomeLobby").GetComponent<LobbyNavigationShell>();
            Assert.That(shell.Configure(), Is.True);
            shell.Show(null);
            Transform candidate = bottom.Find("Visual/BottomNavigationVisual");
            Assert.That(candidate, Is.Not.Null);
            Assert.That(bottom.GetComponentsInChildren<Button>(true), Has.Length.EqualTo(5));
            for (int i = 0; i < ButtonNames.Length; i++)
            {
                Transform button = bottom.Find(ButtonNames[i]);
                Assert.That(button.GetComponent<Button>(), Is.Not.Null, ButtonNames[i]);
                Assert.That(button.GetComponent<Image>().raycastTarget, Is.True, ButtonNames[i]);
                Assert.That(button.Find("Icon").GetComponent<Graphic>().raycastTarget, Is.False, ButtonNames[i]);
                Assert.That(button.Find("Label").GetComponent<TMP_Text>().raycastTarget, Is.False, ButtonNames[i]);
                Assert.That(button.Find("Icon").gameObject.activeSelf, Is.True, ButtonNames[i]);
                Assert.That(button.Find("Label").gameObject.activeSelf, Is.True, ButtonNames[i]);
                Assert.That(candidate.Find("Cells/Cell_0"+i+"/Normal"), Is.Not.Null, ButtonNames[i]);
                Assert.That(candidate.Find("Cells/Cell_0"+i+"/Selected"), Is.Not.Null, ButtonNames[i]);
            }
        }

        [Test]
        public void PublicNavigation_UsesTruthfulLegionRelicTraitDestinationLabels()
        {
            Scene lobby = LobbySceneTestContext.OpenLobby();
            LobbyNavigationShell shell = Find(lobby, "@HomeLobby").GetComponent<LobbyNavigationShell>();
            Transform bottom = Find(lobby, "@HomeLobby/SafeArea/BottomNavigation");
            TMP_Text title = Find(lobby, "@HomeLobby/SafeArea/ContentViewport/DestinationPanel/DestinationTitle").GetComponent<TMP_Text>();
            Assert.That(shell.Configure(), Is.True);
            shell.Show(null);
            bottom.Find("LegionNavButton").GetComponent<Button>().onClick.Invoke();
            Assert.That(title.text, Is.EqualTo("LEGION"));
            shell.Show(null);
            bottom.Find("RelicNavButton").GetComponent<Button>().onClick.Invoke();
            Assert.That(title.text, Is.EqualTo("RELIC"));
            shell.Show(null);
            bottom.Find("TraitNavButton").GetComponent<Button>().onClick.Invoke();
            Assert.That(title.text, Is.EqualTo("TRAIT"));
        }

        [Test]
        public void PublicNavigationSeam_AppliesSelectedStateAndPreservesSemanticIcons()
        {
            Scene lobby = LobbySceneTestContext.OpenLobby();
            LobbyNavigationShell shell = Find(lobby, "@HomeLobby").GetComponent<LobbyNavigationShell>();
            Transform bottom = Find(lobby, "@HomeLobby/SafeArea/BottomNavigation");
            Assert.That(shell.Configure(), Is.True);
            shell.Show(null);
            bottom.Find("ShopNavButton").GetComponent<Button>().onClick.Invoke();
            Assert.That(bottom.Find("ShopNavButton/Icon").GetComponent<Image>().enabled, Is.True);
            Assert.That(bottom.Find("ShopNavButton/Label").GetComponent<TMP_Text>().text, Is.EqualTo("상점"));
        }

        [Test]
        public void PublicNavigation_TransitionsBetweenDestinationsWithoutResetting()
        {
            Scene lobby = LobbySceneTestContext.OpenLobby();
            LobbyNavigationShell shell = Find(lobby, "@HomeLobby").GetComponent<LobbyNavigationShell>();
            Transform bottom = Find(lobby, "@HomeLobby/SafeArea/BottomNavigation");
            Transform destination = Find(lobby, "@HomeLobby/SafeArea/ContentViewport/DestinationPanel/DestinationScrollViewport/ScrollContent");
            TMP_Text title = Find(lobby, "@HomeLobby/SafeArea/ContentViewport/DestinationPanel/DestinationTitle").GetComponent<TMP_Text>();
            Assert.That(shell.Configure(), Is.True);

            shell.Show(null);
            bottom.Find("ShopNavButton").GetComponent<Button>().onClick.Invoke();
            Transform shop = destination.Find("ShopContent");
            Transform legion = destination.Find("LegionContent");
            Assert.That(shop.gameObject.activeSelf, Is.True);
            Assert.That(legion.gameObject.activeSelf, Is.False);

            bottom.Find("LegionNavButton").GetComponent<Button>().onClick.Invoke();

            Assert.That(shop.gameObject.activeSelf, Is.False);
            Assert.That(legion.gameObject.activeSelf, Is.True);
            Assert.That(title.text, Is.EqualTo("LEGION"));
            Transform cells = bottom.Find("Visual/BottomNavigationVisual/Cells");
            Assert.That(cells.Find("Cell_00/Selected").gameObject.activeSelf, Is.True);
            Assert.That(cells.Find("Cell_04/Normal").gameObject.activeSelf, Is.True);
        }

        [Test]
        public void BottomNavigation_UsesSingleContinuousCommercialBarContract()
        {
            Scene lobby = LobbySceneTestContext.OpenLobby();
            RectTransform bar = Find(lobby, "@HomeLobby/SafeArea/BottomNavigation").GetComponent<RectTransform>();
            Assert.That(bar.rect.size, Is.EqualTo(new Vector2(1032f, 188f)).Within(0.01f));
            Assert.That(bar.anchoredPosition, Is.EqualTo(new Vector2(0f, 24f)).Within(0.01f));
            Transform bottom = bar.transform;
            for (int i = 0; i < ButtonNames.Length; i++)
            {
                RectTransform button = bottom.Find(ButtonNames[i]).GetComponent<RectTransform>();
                Assert.That(button.rect.width, Is.EqualTo(206.4f).Within(0.1f), ButtonNames[i]);
                Assert.That(button.rect.height, Is.EqualTo(188f).Within(0.1f), ButtonNames[i]);
            }
        }

        [Test]
        public void BottomNavigation_UsesProductionNormalAndFocusLayers()
        {
            Scene lobby = LobbySceneTestContext.OpenLobby();
            Transform candidate = Find(lobby, "@HomeLobby/SafeArea/BottomNavigation/Visual/BottomNavigationVisual");
            Assert.That(candidate, Is.Not.Null);
            for (int i = 0; i < ButtonNames.Length; i++)
            {
                Transform cell = candidate.Find("Cells/Cell_0"+i);
                Assert.That(cell.Find("Normal"), Is.Not.Null, ButtonNames[i]);
                Assert.That(cell.Find("Selected"), Is.Not.Null, ButtonNames[i]);
                foreach (Graphic graphic in cell.GetComponentsInChildren<Graphic>(true))
                {
                    Assert.That(graphic.raycastTarget, Is.False, graphic.name);
                }
            }
        }

        [Test]
        public void BottomNavigation_UsesMappedProjectDonorIconsAndNativeIconContract()
        {
            Scene lobby = LobbySceneTestContext.OpenLobby();
            Transform bottom = Find(lobby, "@HomeLobby/SafeArea/BottomNavigation");
            for (int i = 0; i < ButtonNames.Length; i++)
            {
                Image icon = bottom.Find(ButtonNames[i] + "/Icon").GetComponent<Image>();
                Assert.That(icon.sprite, Is.Not.Null, ButtonNames[i]);
                Assert.That(icon.preserveAspect, Is.True, ButtonNames[i]);
                Assert.That(bottom.Find(ButtonNames[i]+"/Label").GetComponent<TMP_Text>().text, Is.EqualTo(ExpectedLabels[i]));
            }
        }

        [Test]
        public void BottomNavigation_UsesRaisedLightFocusDepthContract()
        {
            Scene lobby = LobbySceneTestContext.OpenLobby();
            Transform candidate = Find(lobby, "@HomeLobby/SafeArea/BottomNavigation/Visual/BottomNavigationVisual");
            Transform cell = candidate.Find("Cells/Cell_01");
            Assert.That(cell.GetComponent<RectTransform>().rect.height, Is.EqualTo(218f).Within(0.01f));
            Assert.That(cell.localScale, Is.EqualTo(Vector3.one * 1.08f));
            Assert.That(cell.Find("Selected/InnerBorder").gameObject.activeInHierarchy, Is.True);
        }

        [Test]
        public void BottomNavigation_UsesSingleProductionVisualRoot()
        {
            Transform bottom = Find(LobbySceneTestContext.OpenLobby(),"@HomeLobby/SafeArea/BottomNavigation");
            Assert.That(bottom.Find("Visual/BottomNavigationVisual"), Is.Not.Null);
            Assert.That(bottom.Find("Visual/Face"), Is.Null);
        }
        static void AssertLayer(Transform layer, Image.Type expectedType)
        {
            Assert.That(layer, Is.Not.Null);
            Image image = layer.GetComponent<Image>();
            Assert.That(image, Is.Not.Null);
            Assert.That(image.sprite, Is.Not.Null);
            Assert.That(image.type, Is.EqualTo(expectedType));
            Assert.That(image.raycastTarget, Is.False);
        }

        static void AssertRuntimeVisualState(Transform candidate, Transform bottom, Button[] buttons, int selectedIndex)
        {
            Transform cells = candidate.Find("Cells");
            for (int i = 0; i < ButtonNames.Length; i++)
            {
                bool selected = i == selectedIndex;
                Transform cell = cells.Find("Cell_0" + i);
                Assert.That(cell.Find("Normal").gameObject.activeSelf, Is.EqualTo(!selected), ButtonNames[i]);
                Assert.That(cell.Find("Selected").gameObject.activeSelf, Is.EqualTo(selected), ButtonNames[i]);
                RectTransform cellRect = cell.GetComponent<RectTransform>();
                Assert.That(cellRect.sizeDelta.y, Is.EqualTo(selected ? 218f : 200f).Within(0.01f), ButtonNames[i]);
                Assert.That(cellRect.localScale, Is.EqualTo(selected ? Vector3.one * 1.08f : Vector3.one), ButtonNames[i]);
                Image icon = buttons[i].transform.Find("Icon").GetComponent<Image>();
                Assert.That(icon.preserveAspect, Is.True, ButtonNames[i]);
                Assert.That(icon.rectTransform.sizeDelta, Is.EqualTo(Vector2.one * (selected ? 128f : 116f)), ButtonNames[i]);
                TMP_Text label = buttons[i].transform.Find("Label").GetComponent<TMP_Text>();
                Assert.That(label.fontSize, Is.EqualTo(selected ? 40f : 36f).Within(0.01f), ButtonNames[i]);
                if (selected)
                    Assert.That(cell.GetSiblingIndex(), Is.EqualTo(cells.childCount - 1), ButtonNames[i]);
            }
        }

        static Transform Find(Scene scene, string path)
        {
            return LobbySceneTestContext.Find(scene, path);
        }
    }
}
