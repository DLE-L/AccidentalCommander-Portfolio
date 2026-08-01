using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Lizzo.PV.UI;
using Lizzo.PV.EditorTests;

namespace Lizzo.PV.EditorTests
{
    public sealed class LobbyVisualTypographyTests
    {
        const string PretendardRoot = "Assets/_LizzoPV/Fonts/Pretendard/TMP/";
        static readonly Color MainBlue = new(0.08627451f, 0.5294118f, 0.972549f, 1f);
        static readonly Color RaisedPanel = new(0.972549f, 0.9882353f, 1f, 1f);
        static readonly Color SoftPanel = new(0.8666667f, 0.9372549f, 1f, 1f);
        static readonly Color SlotInterior = new(0.8313726f, 0.9137255f, 0.972549f, 1f);
        static readonly Color Navy = new(0.0627451f, 0.172549f, 0.3843137f, 1f);
[Test]
        public void HomeAndShopUseVisibleRaisedPanelsAndPrimaryCta()
        {
            Scene lobby = OpenLobby();
            AssertPrimaryButton(Find(lobby, "@HomeLobby/SafeArea/ContentViewport/BattleHomePanel/StartBattleButton"));

            AssertSurface(Find(lobby, "@HomeLobby/SafeArea/ContentViewport/BattleHomePanel/EventShortcut"), RaisedPanel);
            AssertSurface(Find(lobby, "@HomeLobby/SafeArea/ContentViewport/BattleHomePanel/MiniPassShortcut"), RaisedPanel);
            AssertSurface(Find(lobby, "@HomeLobby/SafeArea/ContentViewport/LegionPassEntry"), RaisedPanel);

            Transform shop = Find(lobby, "@HomeLobby/SafeArea/ContentViewport/DestinationPanel/DestinationScrollViewport/ScrollContent/ShopContent");
            Assert.That(shop, Is.Not.Null);
            foreach (string name in new[] {
                "ShopTitleSection", "DailyClaimSection", "FeaturedOfferSection", "PassOfferRow",
                "DailyShopSection", "GrowthOfferSection", "CurrencyShopSection" }
            )
            {
                AssertSurface(
                    shop.Find(name),
                    name == "DailyClaimSection" || name == "FeaturedOfferSection" ? SoftPanel : RaisedPanel);
            }

            foreach (Transform card in shop.GetComponentsInChildren<Transform>(true))
            {
                if (card.name.EndsWith("Card") || card.name.StartsWith("DailyItem_") || card.name.StartsWith("CurrencyItem_"))
                {
                    AssertSurface(card, RaisedPanel);
                }
            }
        }

[Test]
        public void HomeAndShopPlaceholdersAndTextUseApprovedVisibleTokens()
        {
            Scene lobby = OpenLobby();
            Transform home = Find(lobby, "@HomeLobby/SafeArea/ContentViewport/BattleHomePanel");
            Transform shop = Find(lobby, "@HomeLobby/SafeArea/ContentViewport/DestinationPanel/DestinationScrollViewport/ScrollContent/ShopContent");
            foreach (Transform root in new[] {
                home, shop }
            )
            {
                Assert.That(root, Is.Not.Null);
                foreach (Image image in root.GetComponentsInChildren<Image>(true))
                {
                    if (image.isActiveAndEnabled)
                    {
                        if (image.sprite == null && image.color.a > 0f)
                        {
                            AssertColorClose(image.color, SlotInterior);
                        }
                        Transform startButton = root.Find("StartBattleButton");
                        bool isStartButtonInteractionOwner = startButton != null && image.transform == startButton;
                        if (image.gameObject != root.gameObject && !isStartButtonInteractionOwner)
                        {
                            Assert.That(image.raycastTarget, Is.False, image.name);
                        }
                        if (image.color.a > 0.5f)
                        {
                            Assert.That(image.color.r + image.color.g + image.color.b, Is.GreaterThan(0.25f), image.name);
                        }
                    }
                }
                foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (text.isActiveAndEnabled)
                    {
                        Assert.That(text.color.r + text.color.g + text.color.b, Is.GreaterThan(0.25f), text.name);
                    }
                }
            }
        }

[Test]
        public void PublicShopNavigationPath_RendersShopContentImmediately()
        {
            Scene lobby = OpenLobby();
            try
            {
                Transform shellRoot = Find(lobby, "@HomeLobby");
                LobbyNavigationShell shell = shellRoot.GetComponent<LobbyNavigationShell>();
                Assert.That(shell.Configure(), Is.True);
                shell.Show(null);

                Transform shopButton = Find(lobby, "@HomeLobby/SafeArea/BottomNavigation/ShopNavButton");
                shopButton.GetComponent<Button>().onClick.Invoke();

                Transform placeholderPanel = Find(lobby, "@HomeLobby/SafeArea/ContentViewport/DestinationPanel");
                Transform battlePanel = Find(lobby, "@HomeLobby/SafeArea/ContentViewport/BattleHomePanel");
                Transform shop = Find(lobby, "@HomeLobby/SafeArea/ContentViewport/DestinationPanel/DestinationScrollViewport/ScrollContent/ShopContent");
                Assert.That(placeholderPanel.gameObject.activeInHierarchy, Is.True);
                Assert.That(battlePanel.gameObject.activeInHierarchy, Is.False);
                Assert.That(placeholderPanel.GetComponent<CanvasGroup>().alpha, Is.GreaterThan(0.99f));
                foreach (string name in new[] {
                    "ShopTitleSection", "DailyClaimSection", "FeaturedOfferSection", "PassOfferRow", "DailyShopSection", "DailyItem_00" }
                )
                    Assert.That(FindDescendant(shop, name).gameObject.activeInHierarchy, Is.True, name);
                foreach (string name in new[] {
                    "ShopTitleSection", "DailyClaimSection", "FeaturedOfferSection", "PassOfferRow", "DailyShopSection" }
                )
                    Assert.That(
                        FindDescendant(shop, name).Find("Visual/Bg_light_shadow").GetComponent<Image>().isActiveAndEnabled,
                        Is.True,
                        name + " visible shadow");
            }
            finally
            {
                LobbySceneTestContext.RestoreLobbyWithoutSaving();
            }
        }
[Test]
        public void LobbyTypography_UsesApprovedRoleFontsForAllProjectOwnedText()
        {
            var scene = LobbySceneTestContext.OpenLobby();
            Assert.That(scene.IsValid() && scene.isLoaded, Is.True, "Lobby must be loaded for the live typography contract.");

            int projectOwnedCount = 0;
            int nanumCount = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
                {
                    projectOwnedCount++;
                    string fontPath = AssetDatabase.GetAssetPath(text.font);
                    if (fontPath.Contains("NanumGothic"))
                    {
                        nanumCount++;
                    }

                    Assert.That(fontPath, Does.StartWith(PretendardRoot), text.name + " must use a project-owned Pretendard role font.");
                    Assert.That(text.fontSharedMaterial, Is.Not.Null, text.name + " must have a role material.");
                    Assert.That(text.fontStyle, Is.EqualTo(FontStyles.Normal), text.name + " must not stack synthetic Bold.");
                    AssertNoNanumSerializedReferences(text);
                }
            }

            Assert.That(projectOwnedCount, Is.EqualTo(138));
            Assert.That(nanumCount, Is.EqualTo(0), "Nanum must not remain on project-owned Lobby text.");
        }
        [Test]
        public void SharedLobbyVisualsRemainReadableAndProjectOwned()
        {
            Scene lobby = LobbySceneTestContext.OpenLobby();
            Transform header = Find(lobby, "@HomeLobby/SafeArea/PersistentHeader");
            Assert.That(header, Is.Not.Null);
            foreach (TMP_Text text in header.GetComponentsInChildren<TMP_Text>(true))
            {
                Assert.That(AssetDatabase.GetAssetPath(text.font), Does.StartWith(PretendardRoot), text.name);
            }
        }

        [Test]
        public void LegionStatesUseDistinctReadableVisualsWithoutNewInteractionOwners()
        {
            Scene lobby = OpenLobby();
            Transform grid = Find(lobby, "@HomeLobby/SafeArea/ContentViewport/DestinationPanel/DestinationScrollViewport/ScrollCon"
                + "tent/LegionContent/UnitRosterGrid");
            Assert.That(grid, Is.Not.Null);
            Color selected = grid.Find("UnitCard_00/Visual/Bg").GetComponent<Image>().color;
            Color normal = grid.Find("UnitCard_02/Visual/Bg").GetComponent<Image>().color;
            Assert.That(selected, Is.Not.EqualTo(normal));
            Assert.That(grid.GetComponentsInChildren<Button>(true), Is.Empty);
            Transform popup = Find(lobby, "@HomeLobby/SafeArea/LegionUnitDetailPopup");
            Assert.That(popup.GetComponentsInChildren<Button>(true), Has.Length.EqualTo(2));
            Assert.That(popup.Find("Dialog/Content/UpgradeButton").GetComponent<Button>().interactable, Is.False);
        }

        [Test]
        public void SharedGeometryAndSavedStateRemainUnchanged()
        {
            Scene lobby = OpenLobby();
            Assert.That(Find(lobby, "@HomeLobby/SafeArea/ContentViewport/BattleHomePanel").gameObject.activeSelf, Is.True);
            Assert.That(Find(lobby, "@HomeLobby/SafeArea/ContentViewport/DestinationPanel/DestinationScrollViewport/ScrollCon"
                + "tent/LegionContent").gameObject.activeSelf, Is.False);
            Assert.That(Find(lobby, "@HomeLobby/SafeArea/ContentViewport/DestinationPanel/DestinationScrollViewport/ScrollCon"
                + "tent/ShopContent").gameObject.activeSelf, Is.False);
            Assert.That(Find(lobby, "@HomeLobby/SafeArea/LegionUnitDetailPopup").gameObject.activeSelf, Is.False);
            Assert.That(Find(lobby, "@HomeLobby/SafeArea/ContentViewport/LegionPassEntry").gameObject.activeSelf, Is.True);
            Assert.That(Find(lobby, "@HomeLobby/SafeArea/BottomNavigation").GetComponentsInChildren<Button>(true), Has.Length.EqualTo(5));
        }

        [Test]
        public void VisibleRenderedLayersUseLightLegionSurfaceAndTrueNavContrast()
        {
            Scene lobby = OpenLobby();
            Image viewport = Find(lobby, "@HomeLobby/SafeArea/ContentViewport/DestinationPanel/DestinationScrollViewport").GetComponent<Image>();
            Image page = Find(lobby, "@HomeLobby/SafeArea/ContentViewport/DestinationPanel/PageBackground").GetComponent<Image>();
            Assert.That(viewport.sprite, Is.Null);
            Assert.That(page.sprite, Is.Null);
            Assert.That(viewport.color.a, Is.GreaterThan(0.9f));
            Assert.That(page.color.a, Is.GreaterThan(0.9f));
        }

        [Test]
        public void DraftThemeDoesNotOwnAuthoredPageBackground()
        {
            Scene lobby = OpenLobby();
            Image page = Find(lobby, "@HomeLobby/SafeArea/ContentViewport/DestinationPanel/PageBackground").GetComponent<Image>();
            Assert.That(page.sprite, Is.Null);
            Assert.That(page.color.a, Is.GreaterThan(0.9f));
        }
        static void AssertPrimaryButton(Transform root)
        {
            Assert.That(root, Is.Not.Null);
            Assert.That(root.GetComponent<Button>(), Is.Not.Null);
            Assert.That(root.Find("Visual"), Is.Not.Null);
        }
        static void AssertSurface(Transform root, Color expected)
        {
            Assert.That(root, Is.Not.Null);
            Transform visual = root.Find("Visual");
            Assert.That(visual, Is.Not.Null, root.name);
            Assert.That(visual.Find("Bg"), Is.Not.Null, root.name);
            Assert.That(visual.Find("Border"), Is.Not.Null, root.name);
        }
        static void AssertColorClose(Color actual, Color expected)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(0.002f));
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(0.002f));
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(0.002f));
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(0.002f));
        }
        static Scene OpenLobby() => LobbySceneTestContext.OpenLobby();
        static Transform Find(Scene scene, string path) => LobbySceneTestContext.Find(scene, path);
        static Transform FindDescendant(Transform root, string name)
        {
            if (root == null)
            {
                return null;
            }
            if (root.name == name)
            {
                return root;
            }
            for (int i = 0;
            i < root.childCount;
            i++)
            {
                Transform t = FindDescendant(root.GetChild(i), name);
                if (t != null)
                {
                    return t;
                }
            }
            return null;
        }
        static void AssertNoNanumSerializedReferences(TMP_Text text)
        {
            string fontPath = AssetDatabase.GetAssetPath(text.font);
            Assert.That(fontPath, Does.Not.Contain("NanumGothic"), text.name);
            Assert.That(text.font != null ? text.font.name : string.Empty, Does.Not.Contain("NanumGothic"), text.name);
            AssertNoNanumMaterial(text.fontSharedMaterial, text.name);
            SerializedObject serialized = new SerializedObject(text);
            AssertNoNanumMaterial(serialized.FindProperty("m_fontMaterial")?.objectReferenceValue, text.name);
            AssertNoNanumMaterialArray(serialized.FindProperty("m_fontSharedMaterials"), text.name);
            AssertNoNanumMaterialArray(serialized.FindProperty("m_fontMaterials"), text.name);
        }
        static void AssertNoNanumMaterialArray(SerializedProperty property, string path)
        {
            if (property == null || !property.isArray)
            {
                return;
            }
            for (int i = 0;
            i < property.arraySize;
            i++)
            {
                AssertNoNanumMaterial(property.GetArrayElementAtIndex(i).objectReferenceValue, path);
            }
        }
        static void AssertNoNanumMaterial(Object material, string path)
        {
            if (material == null)
            {
                return;
            }
            Assert.That(material.name, Does.Not.Contain("NanumGothic"), path);
            Assert.That(AssetDatabase.GetAssetPath(material), Does.Not.Contain("NanumGothic"), path);
        }
    }
}
