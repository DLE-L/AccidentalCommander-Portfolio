using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Lizzo.PV.EditorTests;
using Lizzo.PV.UI;
using Lizzo.PV.UI.Theming.Draft;

namespace Lizzo.PV.EditorTests
{
    public sealed class LobbySceneLayoutTests
    {
        static readonly string[] NavigationOrder = {
            "LegionNavButton", "BattleNavButton", "TraitNavButton", "RelicNavButton", "ShopNavButton" }
        ;
        static readonly string[] NavigationLabels = {
            "군단", "출정", "특성", "유물", "상점" }
        ;
        static readonly string[] SectionNames =
        {
            "ShopTitleSection", "DailyClaimSection", "FeaturedOfferSection", "PassOfferRow",
            "DailyShopSection", "GrowthOfferSection", "CurrencyShopSection",
        }
        ;
        static readonly string[] UnitNames =
        {
            "방패병", "검병", "성직자", "매사냥꾼", "전투 약초사", "폭탄병",
            "화염술사", "번개술사", "늑대 조련사", "망령 기사", "사령술사", "해골 폭탄병",
        }
        ;
        static readonly string[] SlotNames = {
            "ProfileSlot", "GoldCurrencySlot", "GemCurrencySlot", "SettingsSlot" }
        ;

        [Test]
        public void LobbyHome_UsesSemanticSafeAreaAndDefaultState()
        {
            Scene lobby = LobbySceneTestContext.OpenLobby();
            Transform safe = Find(lobby, "@HomeLobby/SafeArea");
            Transform home = Find(lobby, "@HomeLobby/SafeArea/ContentViewport/BattleHomePanel");
            Transform header = Find(lobby, "@HomeLobby/SafeArea/PersistentHeader");
            Transform bottom = Find(lobby, "@HomeLobby/SafeArea/BottomNavigation");
            Assert.That(safe, Is.Not.Null);
            Assert.That(home, Is.Not.Null);
            Assert.That(header, Is.Not.Null);
            Assert.That(bottom, Is.Not.Null);
            Assert.That(home.gameObject.activeSelf, Is.True);
            Assert.That(Find(lobby, "@HomeLobby/SafeArea/LegionUnitDetailPopup").gameObject.activeSelf, Is.False);
            Assert.That(bottom.GetComponentsInChildren<Button>(true), Has.Length.EqualTo(5));
            for (int i = 0;
            i < NavigationOrder.Length;
            i++)
            {
                Transform button = bottom.Find(NavigationOrder[i]);
                Assert.That(button, Is.Not.Null, NavigationOrder[i]);
                Assert.That(button.Find("Label").GetComponent<TMP_Text>().text, Is.EqualTo(NavigationLabels[i]));
                Assert.That(button.GetComponents<Button>(), Has.Length.EqualTo(1));
            }
        }

        [Test]
        public void LobbyHome_PersistentHeaderRendersAboveContentViewportAndBelowBottomNavigation()
        {
            Scene lobby = OpenLobby();
            try
            {
                RectTransform safe = Find(lobby, "@HomeLobby/SafeArea").GetComponent<RectTransform>();
                Transform contentViewport = safe.Find("ContentViewport");
                Transform persistentHeader = safe.Find("PersistentHeader");
                Transform bottomNavigation = safe.Find("BottomNavigation");
                Assert.That(contentViewport.GetSiblingIndex(), Is.LessThan(persistentHeader.GetSiblingIndex()));
                Assert.That(persistentHeader.GetSiblingIndex(), Is.LessThan(bottomNavigation.GetSiblingIndex()));

                using (TemporarySafeAreaFixture fixture = TemporarySafeAreaFixture.Create(safe, 1968f, 2184f))
                {
                    Transform profile = fixture.SafeArea.Find("PersistentHeader/ProfileSlot");
                    RectTransform legionPass = fixture.SafeArea.Find("ContentViewport/LegionPassEntry").GetComponent<RectTransform>();
                    Assert.That(DescendantBounds(profile).Intersects(WorldBounds(legionPass)), Is.True,
                        "The Fold profile composite must retain the approved LegionPass overlap while rendering above ContentViewport.");
                }
            }
            finally
            {
                LobbySceneTestContext.RestoreLobbyWithoutSaving();
            }
        }
[Test]
        public void Shop_UsesExactScrollableSemanticContract()
        {
            Scene lobby = OpenLobby();
            try
            {
                LobbyNavigationShell shell = Find(lobby, "@HomeLobby")?.GetComponent<LobbyNavigationShell>();
                Assert.That(shell, Is.Not.Null);
                Assert.That(shell.Configure(), Is.True);
                shell.Show(null);
                Find(lobby, "@HomeLobby/SafeArea/BottomNavigation/ShopNavButton").GetComponent<Button>().onClick.Invoke();

                Transform shop = Find(lobby, "@HomeLobby/SafeArea/ContentViewport/DestinationPanel/DestinationScrollViewport/ScrollContent/ShopContent");
                Assert.That(shop, Is.Not.Null);
                Assert.That(shop.GetComponent<RectTransform>().rect.size, Is.EqualTo(new Vector2(984f, 2288f)));

            for (int i = 0;
            i < SectionNames.Length;
            i++)
            {
                Transform section = shop.Find(SectionNames[i]);
                Assert.That(section, Is.Not.Null, SectionNames[i]);
                Assert.That(section.Find("Visual"), Is.Not.Null, SectionNames[i]);
                Assert.That(section.Find("Content"), Is.Not.Null, SectionNames[i]);
                Assert.That(section.GetComponentsInChildren<Button>(true), Is.Empty, SectionNames[i]);
                foreach (Graphic graphic in section.GetComponentsInChildren<Graphic>(true))
                {
                    Assert.That(graphic.raycastTarget, Is.False, section.name + "/" + graphic.name);
                }
            }

            AssertSection(shop, "ShopTitleSection", new Vector2(984f, 88f), "상점");
            AssertSection(shop, "DailyClaimSection", new Vector2(984f, 120f), "오늘의 무료 보상");
            AssertSection(shop, "FeaturedOfferSection", new Vector2(984f, 280f), "오늘의 추천 상품");
            AssertSection(shop, "PassOfferRow", new Vector2(984f, 176f), null);
            AssertSection(shop, "DailyShopSection", new Vector2(984f, 676f), "일일 상점");
            AssertSection(shop, "GrowthOfferSection", new Vector2(984f, 272f), "성장 상품");
            AssertSection(shop, "CurrencyShopSection", new Vector2(984f, 436f), "재화 상점");

            Assert.That(shop.Find("PassOfferRow/Content/LegionPassCard"), Is.Not.Null);
            Assert.That(shop.Find("PassOfferRow/Content/MonthlyBenefitCard"), Is.Not.Null);
            AssertGrid(shop.Find("DailyShopSection/Content/ItemGrid"), "DailyItem_", 6, new Vector2(480f, 180f));
            Assert.That(shop.Find("GrowthOfferSection/Content/StarterGrowthCard"), Is.Not.Null);
            Assert.That(shop.Find("GrowthOfferSection/Content/ChapterGrowthCard"), Is.Not.Null);
            AssertGrid(shop.Find("CurrencyShopSection/Content/ItemGrid"), "CurrencyItem_", 4, new Vector2(480f, 160f));

            Transform viewport = Find(lobby, "@HomeLobby/SafeArea/ContentViewport/DestinationPanel/DestinationScrollViewport");
            Assert.That(viewport, Is.Not.Null);
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            Assert.That(viewportRect.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(viewportRect.anchorMax, Is.EqualTo(Vector2.one));
            Assert.That(viewportRect.offsetMin, Is.EqualTo(new Vector2(48f, 24f)));
            Assert.That(viewportRect.offsetMax, Is.EqualTo(new Vector2(-48f, -24f)));
            ScrollRect scroll = viewport.GetComponent<ScrollRect>();
            Assert.That(scroll, Is.Not.Null);
            Assert.That(scroll.horizontal, Is.False);
            Assert.That(scroll.vertical, Is.True);
                Assert.That(scroll.content, Is.SameAs(Find(lobby, "@HomeLobby/SafeArea/ContentViewport/DestinationPanel/DestinationScrollViewport/ScrollCon"
                    + "tent").GetComponent<RectTransform>()));
            }
            finally
            {
                LobbySceneTestContext.RestoreLobbyWithoutSaving();
            }
        }

[Test]
        public void ShopSelection_UsesShopContentAndHidesDestinationShell()
        {
            Scene lobby = OpenLobby();
            try
            {
                LobbyNavigationShell shell = Find(lobby, "@HomeLobby")?.GetComponent<LobbyNavigationShell>();
                Assert.That(shell, Is.Not.Null);
                Assert.That(shell.Configure(), Is.True);
                shell.Show(null);
                Find(lobby, "@HomeLobby/SafeArea/BottomNavigation/ShopNavButton").GetComponent<Button>().onClick.Invoke();

                Assert.That(Find(lobby, "@HomeLobby/SafeArea/ContentViewport/DestinationPanel/DestinationScrollViewport/ScrollCon"
                    + "tent/ShopContent").gameObject.activeSelf, Is.True);
                Assert.That(Find(lobby, "@HomeLobby/SafeArea/ContentViewport/DestinationPanel/DestinationTitle").gameObject.activeSelf, Is.False);
                Assert.That(Find(lobby, "@HomeLobby/SafeArea/ContentViewport/DestinationPanel/BackButton").gameObject.activeSelf, Is.False);
                Assert.That(Find(lobby, "@HomeLobby/SafeArea/ContentViewport/DestinationPanel/ComingSoonIcon").gameObject.activeSelf, Is.False);
                Assert.That(Find(lobby, "@HomeLobby/SafeArea/ContentViewport/DestinationPanel/LegionTabs").gameObject.activeSelf, Is.False);
                Assert.That(Find(lobby, "@HomeLobby/SafeArea/ContentViewport/LegionPassEntry").gameObject.activeSelf, Is.False);
                ScrollRect destinationScroll = Find(
                    lobby,
                    "@HomeLobby/SafeArea/ContentViewport/DestinationPanel/DestinationScrollViewport")
                    .GetComponent<ScrollRect>();
                Assert.That(destinationScroll.verticalNormalizedPosition, Is.EqualTo(1f).Within(0.001f));
            }
            finally
            {
                LobbySceneTestContext.RestoreLobbyWithoutSaving();
            }
        }

[Test]
        public void CollectionDestinationHasExactRosterGeometryAndStates()
        {
            Scene lobby = OpenLobby();
            Transform legion = Find(lobby, "@HomeLobby/SafeArea/ContentViewport/DestinationPanel/DestinationScrollViewport/ScrollContent/LegionContent");
            Transform grid = legion?.Find("UnitRosterGrid");
            Assert.That(legion, Is.Not.Null);
            Assert.That(legion.gameObject.activeSelf, Is.False);
            Assert.That(legion.GetComponent<RectTransform>().rect.size, Is.EqualTo(new Vector2(984f, 1240f)));
            Assert.That(grid, Is.Not.Null);
            Assert.That(grid.GetComponent<RectTransform>().rect.size, Is.EqualTo(new Vector2(984f, 1008f)));
            Assert.That(grid.childCount, Is.EqualTo(12));

            for (int i = 0;
            i < UnitNames.Length;
            i++)
            {
                Transform card = grid.Find("UnitCard_" + i.ToString("00"));
                Assert.That(card, Is.Not.Null, "UnitCard_" + i.ToString("00"));
                Assert.That(card.GetComponent<RectTransform>().rect.size, Is.EqualTo(new Vector2(228f, 320f)));
                Assert.That(card.GetComponentsInChildren<Button>(true), Is.Empty);
                AssertPopupVisual(card);
                Assert.That(card.Find("Content/PortraitPlaceholder").GetComponent<Image>().sprite, Is.Null);
                Assert.That(card.Find("Content/TitleText").GetComponent<TMP_Text>().text, Is.EqualTo(UnitNames[i]));
                Assert.That(card.Find("Content/LevelText").GetComponent<TMP_Text>().text, Is.EqualTo("Lv. N/A"));
                Assert.That(card.Find("Content/CardProgress").GetComponent<TMP_Text>().text, Is.EqualTo("N/A / N/A"));
                AssertWithin(card, card.Find("Content/PortraitPlaceholder"));
                AssertWithin(card, card.Find("Content/TitleText"));
                AssertWithin(card, card.Find("Content/LevelText"));
                AssertWithin(card, card.Find("Content/CardProgress"));
                AssertNoOverlap(card.Find("Content/PortraitPlaceholder"), card.Find("Content/TitleText"));
                AssertNoOverlap(card.Find("Content/TitleText"), card.Find("Content/LevelText"));
                AssertNoOverlap(card.Find("Content/LevelText"), card.Find("Content/CardProgress"));
            }

            Assert.That(grid.Find("UnitCard_00/Content/StatusIndicator").GetComponent<TMP_Text>().text, Is.EqualTo("선택"));
            Assert.That(grid.Find("UnitCard_01/Content/StatusIndicator").GetComponent<TMP_Text>().text, Is.EqualTo("강화 가능"));
            Assert.That(grid.Find("UnitCard_04/Content/StatusIndicator").GetComponent<TMP_Text>().text, Is.EqualTo("강화 가능"));
            Assert.That(grid.Find("UnitCard_09/Content/StatusIndicator").GetComponent<TMP_Text>().text, Is.EqualTo("잠금"));
            Assert.That(grid.Find("UnitCard_10/Content/StatusIndicator").GetComponent<TMP_Text>().text, Is.EqualTo("잠금"));
            Assert.That(grid.Find("UnitCard_11/Content/StatusIndicator").GetComponent<TMP_Text>().text, Is.EqualTo("MAX"));
        }

[Test]
        public void DetailPopupHasExactGeometryTwoControlsAndSavedInactiveState()
        {
            Scene lobby = OpenLobby();
            Transform popup = Find(lobby, "@HomeLobby/SafeArea/LegionUnitDetailPopup");
            Transform dialog = popup?.Find("Dialog");
            Transform content = dialog?.Find("Content");
            Assert.That(popup, Is.Not.Null);
            Assert.That(popup.gameObject.activeSelf, Is.False);
            Assert.That(dialog, Is.Not.Null);
            Assert.That(dialog.GetComponent<RectTransform>().rect.size, Is.EqualTo(new Vector2(936f, 1280f)));
            Assert.That(content.GetComponent<RectTransform>().rect.size, Is.EqualTo(new Vector2(824f, 1184f)));
            AssertPopupVisual(dialog);
            Assert.That(popup.Find("Backdrop").GetComponent<Image>().raycastTarget, Is.True);
            Assert.That(dialog.GetComponentsInChildren<Button>(true), Has.Length.EqualTo(2));
            Assert.That(content.Find("CloseButton").GetComponent<Button>().interactable, Is.True);
            Assert.That(content.Find("UpgradeButton").GetComponent<Button>().interactable, Is.False);
            Assert.That(content.Find("RoleBadge/RoleText").GetComponent<TMP_Text>().text, Is.EqualTo("전방 방어"));
            Assert.That(content.Find("LevelProgression/CurrentLevelText").GetComponent<TMP_Text>().text, Is.EqualTo("Lv. N/A"));
            Assert.That(content.Find("LevelProgression/NextLevelText").GetComponent<TMP_Text>().text, Is.EqualTo("Lv. N/A"));
            Assert.That(content.Find("StatsPanel/AttackRow").GetComponent<TMP_Text>().text, Is.EqualTo("공격력 N/A → N/A"));
            Assert.That(content.Find("StatsPanel/HealthRow").GetComponent<TMP_Text>().text, Is.EqualTo("체력 N/A → N/A"));
            Assert.That(content.Find("CardProgressRow").GetComponent<TMP_Text>().text, Is.EqualTo("보유 카드   N/A / N/A"));
            Assert.That(content.Find("GoldCostRow").GetComponent<TMP_Text>().text, Is.EqualTo("Gold   N/A"));
            Assert.That(content.Find("UpgradeButton/Label").GetComponent<TMP_Text>().text, Is.EqualTo("강화"));
            AssertWithin(dialog, content.Find("PortraitPlaceholder"));
            AssertWithin(dialog, content.Find("StatsPanel"));
            AssertWithin(dialog, content.Find("UpgradeButton"));
            AssertNoOverlap(content.Find("PortraitPlaceholder"), content.Find("RoleBadge"));
            AssertNoOverlap(content.Find("RoleBadge"), content.Find("LevelProgression"));
            AssertNoOverlap(content.Find("LevelProgression"), content.Find("StatsPanel"));
            AssertNoOverlap(content.Find("StatsPanel"), content.Find("CardProgressRow"));
            AssertNoOverlap(content.Find("CardProgressRow"), content.Find("GoldCostRow"));
            AssertNoOverlap(content.Find("GoldCostRow"), content.Find("UpgradeButton"));
        }

[Test]
        public void NavigationBindingAndPriorShopContractRemainPresent()
        {
            Scene lobby = OpenLobby();
            Transform home = Find(lobby, "@HomeLobby");
            LobbyNavigationShell shell = home.GetComponent<LobbyNavigationShell>();
            SerializedObject serialized = new SerializedObject(shell);
            Assert.That(shell.Configure(), Is.True);
            Transform legionContent = Find(
                lobby,
                "@HomeLobby/SafeArea/ContentViewport/DestinationPanel/DestinationScrollViewport/ScrollContent/LegionContent");
            Assert.That(serialized.FindProperty("_legionContent").objectReferenceValue, Is.SameAs(legionContent.gameObject));
            Assert.That(
                Find(lobby, "@HomeLobby/SafeArea/ContentViewport/DestinationPanel/DestinationScrollViewport/ScrollContent/ShopContent"),
                Is.Not.Null);
            Assert.That(
                Find(lobby, "@HomeLobby/SafeArea/ContentViewport/DestinationPanel/DestinationScrollViewport")
                    .GetComponent<ScrollRect>().vertical,
                Is.True);
            Assert.That(Find(lobby, "@HomeLobby/SafeArea/BottomNavigation/ShopNavButton").GetComponent<Button>(), Is.Not.Null);
            Assert.That(Find(lobby, "@HomeLobby/SafeArea/BottomNavigation/LegionNavButton").GetComponent<Button>(), Is.Not.Null);
            Assert.That(Find(lobby, "@HomeLobby/SafeArea/BottomNavigation/BattleNavButton").GetComponent<Button>(), Is.Not.Null);
            Assert.That(Find(lobby, "@HomeLobby/SafeArea/BottomNavigation/RelicNavButton").GetComponent<Button>(), Is.Not.Null);
            Assert.That(Find(lobby, "@HomeLobby/SafeArea/BottomNavigation/TraitNavButton").GetComponent<Button>(), Is.Not.Null);
        }

[Test]
        public void PersistentHeader_UsesProfileCurrencySettingsSemanticLayout()
        {
            Scene lobby = LobbySceneTestContext.OpenLobby();

            Transform header = Find(lobby, "@HomeLobby/SafeArea/PersistentHeader");
            Assert.That(header, Is.Not.Null);
            RectTransform headerRect = header.GetComponent<RectTransform>();
            LobbyHomeReferenceLayout layout = header.GetComponentInParent<LobbyHomeReferenceLayout>();
            Assert.That(layout, Is.Not.Null);
            layout.RefreshLayout();
            RectTransform headerSafe = GetLayoutSafeArea(headerRect);
            Rect headerFrame = GetExpectedContentFrame(headerSafe);
            AssertContentFrameRect(header, headerSafe, headerFrame.xMin, headerFrame.width, 0f, 0f, 865f, 192f);

            for (int i = 0;
            i < SlotNames.Length;
            i++)
            {
                Transform slot = header.Find(SlotNames[i]);
                Assert.That(slot, Is.Not.Null, SlotNames[i]);
                RectTransform slotRect = slot.GetComponent<RectTransform>();
                Assert.That(slotRect.localPosition.z, Is.EqualTo(0f).Within(0.001f), SlotNames[i]);
                if (slot.name == "ProfileSlot")
                    AssertProfileCompositeScale(slot, headerFrame.width);
                else
                    Assert.That(slotRect.localScale, Is.EqualTo(Vector3.one), SlotNames[i]);
                Assert.That(slot.Find("Visual"), Is.Not.Null, SlotNames[i]);
                Assert.That(slot.Find("Content"), Is.Not.Null, SlotNames[i]);
                Assert.That(slot.GetComponentsInChildren<Button>(true), Is.Empty, SlotNames[i]);
                foreach (Graphic graphic in slot.GetComponentsInChildren<Graphic>(true))
                {
                    Assert.That(graphic.raycastTarget, Is.False, $"Decorative raycast on {graphic.name}");
                }
            }

            RectTransform gold = header.Find("GoldCurrencySlot").GetComponent<RectTransform>();
            RectTransform gem = header.Find("GemCurrencySlot").GetComponent<RectTransform>();
            AssertReferencePercentRect(gold, 187f / 865f, 78f / 1819f, 264f / 865f, 56f / 1819f);
            AssertReferencePercentRect(gem, 497f / 865f, 78f / 1819f, 245f / 865f, 56f / 1819f);

            RectTransform profile = header.Find("ProfileSlot").GetComponent<RectTransform>();
            RectTransform settings = header.Find("SettingsSlot").GetComponent<RectTransform>();
            Image profileFabricBorder = profile.Find("Visual/ProfileChassisFace/Border").GetComponent<Image>();
            Bounds profileFabricBounds = RenderedImageBounds(profileFabricBorder);
            Assert.That(profileFabricBounds.size.x / profileFabricBounds.size.y, Is.InRange(0.84f, 0.88f));
            AssertReferenceSquare(settings, new Vector2(799.5f / 865f, 106.5f / 1819f), 65f / 1819f);

            RectTransform canvasRect = header.GetComponentInParent<Canvas>()?.GetComponent<RectTransform>();
            Assert.That(canvasRect, Is.Not.Null, "Header must be under a Canvas.");
            Bounds canvasBounds = WorldBounds(canvasRect);
            headerSafe = GetLayoutSafeArea(profile);
            headerFrame = GetExpectedContentFrame(headerSafe);
            Bounds[] slotBounds =
            {
                WorldBounds(profile),
                WorldBounds(gold),
                WorldBounds(gem),
                WorldBounds(settings)
            }
            ;
            for (int i = 0;
            i < slotBounds.Length;
            i++)
            {
                Bounds bounds = slotBounds[i];
                Vector4 slotScreen = ScreenRect(new[] { profile, gold, gem, settings }[i], headerSafe);
                Assert.That(slotScreen.x, Is.GreaterThanOrEqualTo(headerFrame.xMin - 0.5f), SlotNames[i]);
                Assert.That(slotScreen.x + slotScreen.z, Is.LessThanOrEqualTo(headerFrame.xMax + 0.5f), SlotNames[i]);
                Assert.That(slotScreen.y, Is.GreaterThanOrEqualTo(-0.5f), SlotNames[i]);
                Assert.That(slotScreen.y + slotScreen.w, Is.LessThanOrEqualTo(headerSafe.rect.height + 0.5f), SlotNames[i]);
                Assert.That(bounds.min.x, Is.GreaterThanOrEqualTo(canvasBounds.min.x - 0.5f), SlotNames[i]);
                Assert.That(bounds.max.x, Is.LessThanOrEqualTo(canvasBounds.max.x + 0.5f), SlotNames[i]);
                Assert.That(bounds.min.y, Is.GreaterThanOrEqualTo(canvasBounds.min.y - 0.5f), SlotNames[i]);
                Assert.That(bounds.max.y, Is.LessThanOrEqualTo(canvasBounds.max.y + 0.5f), SlotNames[i]);
                Assert.That(bounds.max.x, Is.GreaterThan(bounds.min.x), SlotNames[i]);
                Assert.That(bounds.max.y, Is.GreaterThan(bounds.min.y), SlotNames[i]);
            }
            Assert.That(slotBounds[0].center.x, Is.LessThan(slotBounds[1].center.x));
            Assert.That(slotBounds[1].center.x, Is.LessThan(slotBounds[2].center.x));
            Assert.That(slotBounds[2].center.x, Is.LessThan(slotBounds[3].center.x));
            AssertProfileVisibleBounds(profile, gold, headerSafe, headerFrame.xMin, headerFrame.width, 0f, 5f);
            Assert.That(slotBounds[1].max.x, Is.LessThanOrEqualTo(slotBounds[2].min.x + 0.5f));
            Assert.That(slotBounds[2].max.x, Is.LessThanOrEqualTo(slotBounds[3].min.x + 0.5f));

            Transform headerVisual = header.Find("Visual");
            Transform navigationVisual = Find(lobby, "@HomeLobby/SafeArea/BottomNavigation/Visual/BottomNavigationVisual");
            Transform sourceChassis = navigationVisual?.Find("Chassis");
            Assert.That(headerVisual, Is.Not.Null);
            Assert.That(navigationVisual, Is.Not.Null);
            Assert.That(navigationVisual.GetComponent<LobbyBottomNavigationView>(), Is.Not.Null);
            Assert.That(sourceChassis, Is.Not.Null);
            AssertBottomNavigationChassis(headerVisual.Find("BottomNavigationChassis"), sourceChassis);
            AssertBottomNavigationChassis(header.Find("SettingsSlot/Visual/NativeSettingsFrame/BottomNavigationChassis"), sourceChassis);

            Transform profileFrame = header.Find("ProfileSlot/Visual/ProfileChassisFace");
            Assert.That(profileFrame, Is.Not.Null);

            Image portrait = header.Find("ProfileSlot/Content/Portrait").GetComponent<Image>();
            Assert.That(AssetDatabase.GetAssetPath(portrait.sprite), Does.Contain("Sample_Cha01_1.png"));
            Image goldIcon = header.Find("GoldCurrencySlot/Content/Icon").GetComponent<Image>();
            Image gemIcon = header.Find("GemCurrencySlot/Content/Icon").GetComponent<Image>();
            Assert.That(AssetDatabase.GetAssetPath(goldIcon.sprite), Does.Contain("ResourceBar_Icon_Gold.png"));
            Assert.That(AssetDatabase.GetAssetPath(gemIcon.sprite), Does.Contain("ResourceBar_Icon_Gem.png"));
            Image settingsIcon = header.Find("SettingsSlot/Content/Icon").GetComponent<Image>();
            Assert.That(AssetDatabase.GetAssetPath(settingsIcon.sprite), Does.Contain("UI_System_Setting_01.png"));

            Assert.That(header.Find("ProfileSlot/Content/LevelText").GetComponent<TMP_Text>().text, Is.EqualTo("Lv.1"));
            Assert.That(header.Find("GoldCurrencySlot/Content/ValueText").GetComponent<TMP_Text>().text, Is.EqualTo("0"));
            Assert.That(header.Find("GoldCurrencySlot/Content/AddSlot").GetComponent<TMP_Text>().text, Is.EqualTo("+"));
            Assert.That(header.Find("GemCurrencySlot/Content/ValueText").GetComponent<TMP_Text>().text, Is.EqualTo("0"));

            Assert.That(header.Find("Title").gameObject.activeSelf, Is.False);
            Assert.That(header.Find("HeaderCaption").gameObject.activeSelf, Is.False);
            Assert.That(header.Find("HeaderStatus").gameObject.activeSelf, Is.False);
        }

        [Test]
        public void LobbyHome_ProfileSlotUsesFacetedProfileFrame01ChassisAndIntegratedDiamondPeak()
        {
            const string chassisBackgroundPath = "Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Sprite_Common/Flag/GuildFlag_01_Fabric_1_Bg.png";
            const string chassisBorderPath = "Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Sprite_Common/Flag/GuildFlag_01_Fabric_1_Border.png";
            const string crestBackgroundPath = "Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Sprite_Common/Frame/BasicFrame/BasicFrame_Diamond_03_White_Bg.png";
            const string crestBorderPath = "Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Sprite_Common/Frame/BasicFrame/BasicFrame_Diamond_03_White_Border.png";
            Scene lobby = OpenLobby();
            try
            {
                Transform profile = Find(lobby, "@HomeLobby/SafeArea/PersistentHeader/ProfileSlot");
                Transform visual = profile.Find("Visual");
                Transform depth = visual.Find("ProfileChassisDepth");
                Transform rim = visual.Find("ProfileChassisCyanRim");
                Transform face = visual.Find("ProfileChassisFace");
                Transform crest = visual.Find("ProfileTopCrest");
                Transform background = face?.Find("Bg");
                Transform border = face?.Find("Border");
                Transform crestBackground = crest?.Find("Bg");
                Transform crestBorder = crest?.Find("Border");
                Transform portrait = profile.Find("Content/Portrait");
                Transform level = profile.Find("Content/LevelText");
                Image depthImage = depth?.GetComponent<Image>();
                Image rimImage = rim?.GetComponent<Image>();
                Image backgroundImage = background?.GetComponent<Image>();
                Image borderImage = border?.GetComponent<Image>();
                Image crestBackgroundImage = crestBackground?.GetComponent<Image>();
                Image crestBorderImage = crestBorder?.GetComponent<Image>();
                Image portraitImage = portrait?.GetComponent<Image>();
                TMP_Text levelText = level?.GetComponent<TMP_Text>();
                Sprite expectedBackground = AssetDatabase.LoadAssetAtPath<Sprite>(chassisBackgroundPath);
                Sprite expectedBorder = AssetDatabase.LoadAssetAtPath<Sprite>(chassisBorderPath);
                Sprite expectedCrestBackground = AssetDatabase.LoadAssetAtPath<Sprite>(crestBackgroundPath);
                Sprite expectedCrestBorder = AssetDatabase.LoadAssetAtPath<Sprite>(crestBorderPath);

                Assert.That(expectedBackground, Is.Not.Null);
                Assert.That(expectedBorder, Is.Not.Null);
                Assert.That(expectedCrestBackground, Is.Not.Null);
                Assert.That(expectedCrestBorder, Is.Not.Null);
                Assert.That(depthImage, Is.Not.Null);
                Assert.That(rimImage, Is.Not.Null);
                Assert.That(backgroundImage, Is.Not.Null);
                Assert.That(borderImage, Is.Not.Null);
                Assert.That(crestBackgroundImage, Is.Not.Null);
                Assert.That(crestBorderImage, Is.Not.Null);
                foreach (Image image in new[] { depthImage, rimImage, backgroundImage, borderImage })
                {
                    Assert.That(image.gameObject.activeInHierarchy, Is.True);
                    Assert.That(image.enabled, Is.True);
                    Assert.That(image.type, Is.EqualTo(Image.Type.Simple));
                    Assert.That(image.preserveAspect, Is.True);
                    Assert.That(image.raycastTarget, Is.False);
                    Assert.That(image.GetComponentsInChildren<Button>(true), Is.Empty);
                    Assert.That(image.GetComponentsInChildren<Selectable>(true), Is.Empty);
                }
                Assert.That(crest.gameObject.activeSelf, Is.False);
                Assert.That(depthImage.sprite, Is.SameAs(expectedBackground));
                Assert.That(rimImage.sprite, Is.SameAs(expectedBorder));
                Assert.That(backgroundImage.sprite, Is.SameAs(expectedBackground));
                Assert.That(borderImage.sprite, Is.SameAs(expectedBorder));
                Assert.That(crestBackgroundImage.sprite, Is.SameAs(expectedCrestBackground));
                Assert.That(crestBorderImage.sprite, Is.SameAs(expectedCrestBorder));

                foreach (Image image in profile.GetComponentsInChildren<Image>(true))
                {
                    string spritePath = image.sprite == null ? string.Empty : AssetDatabase.GetAssetPath(image.sprite);
                    Assert.That(image.name, Does.Not.Contain("Staff"));
                    Assert.That(image.name, Does.Not.Contain("Symbol"));
                    if (image.gameObject.activeInHierarchy)
                    {
                        Assert.That(spritePath, Does.Not.Contain("ProfileFrame_01_"));
                        Assert.That(spritePath, Does.Not.Contain("ProfileFrame_02_"));
                        Assert.That(spritePath, Does.Not.Contain("Badge_Crimped_"));
                        Assert.That(spritePath, Does.Not.Contain("BasicFrame_Diamond_"));
                    }
                }
                Assert.That(profile.GetComponentsInChildren<Mask>(true), Is.Empty);

                RectTransform safe = GetLayoutSafeArea(profile);
                LobbyHomeReferenceLayout layout = safe.GetComponent<LobbyHomeReferenceLayout>();
                Assert.That(layout, Is.Not.Null);
                layout.RefreshLayout();
                Bounds borderBounds = RenderedImageBounds(borderImage);
                Assert.That(borderBounds.size.x / borderBounds.size.y, Is.InRange(0.84f, 0.88f));
                Assert.That(borderBounds.Contains(portrait.position), Is.True);
                AssertLevelTextWithinBadge(profile, levelText, safe);
                Assert.That(portrait.gameObject.activeInHierarchy, Is.True);
                Assert.That(portraitImage.sprite, Is.Not.Null);
                Assert.That(level.gameObject.activeInHierarchy, Is.True);
                Assert.That(levelText.text, Is.EqualTo("Lv.1"));

                SerializedObject serialized = new SerializedObject(layout);
                Assert.That(serialized.FindProperty("_profileSlot").objectReferenceValue, Is.SameAs(profile.GetComponent<RectTransform>()));
                Rect frame = GetExpectedContentFrame(safe);
                AssertProfileVisibleBounds(profile, Find(lobby, "@HomeLobby/SafeArea/PersistentHeader/GoldCurrencySlot"), safe, frame.xMin, frame.width, 0f, 5f);
            }
            finally
            {
                LobbySceneTestContext.RestoreLobbyWithoutSaving();
            }
        }

        [Test]
        public void LobbyHome_ProfileSlotUsesLayeredReferenceChassis()
        {
            const string backgroundPath = "Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Sprite_Common/Flag/GuildFlag_01_Fabric_1_Bg.png";
            const string borderPath = "Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Sprite_Common/Flag/GuildFlag_01_Fabric_1_Border.png";
            const string crestBackgroundPath = "Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Sprite_Common/Frame/BasicFrame/BasicFrame_Diamond_03_White_Bg.png";
            const string crestBorderPath = "Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Sprite_Common/Frame/BasicFrame/BasicFrame_Diamond_03_White_Border.png";
            const string crestInnerBorderPath = "Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Sprite_Common/Frame/BasicFrame/BasicFrame_Diamond_03_White_InnerBorder.png";
            const string badgeBackgroundPath = "Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Sprite_Common/Label/Label_Flag_01_White_Bg.png";
            const string badgeBorderPath = "Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Sprite_Common/Label/Label_Flag_01_White_Border.png";
            Scene lobby = OpenLobby();
            try
            {
                Transform header = Find(lobby, "@HomeLobby/SafeArea/PersistentHeader");
                Transform profile = header.Find("ProfileSlot");
                Transform visual = profile.Find("Visual");
                Transform depth = visual.Find("ProfileChassisDepth");
                Transform crest = visual.Find("ProfileTopCrest");
                Transform rim = visual.Find("ProfileChassisCyanRim");
                Transform front = visual.Find("ProfileChassisFace");
                Transform frontBackground = front.Find("Bg");
                Transform frontBorder = front.Find("Border");
                Transform badge = visual.Find("LevelBadge");
                Transform portrait = profile.Find("Content/Portrait");
                TMP_Text levelText = profile.Find("Content/LevelText").GetComponent<TMP_Text>();
                RectTransform level = levelText.rectTransform;
                Image depthImage = depth?.GetComponent<Image>();
                Image rimImage = rim?.GetComponent<Image>();
                Image frontBackgroundImage = frontBackground.GetComponent<Image>();
                Image frontBorderImage = frontBorder.GetComponent<Image>();
                Image navyDonor = header.Find("Visual/BottomNavigationChassis/Border").GetComponent<Image>();
                Image cyanDonor = header.Find("Visual/BottomNavigationChassis/InnerBorder").GetComponent<Image>();
                Sprite expectedBackground = AssetDatabase.LoadAssetAtPath<Sprite>(backgroundPath);
                Sprite expectedBorder = AssetDatabase.LoadAssetAtPath<Sprite>(borderPath);
                Sprite expectedCrestBackground = AssetDatabase.LoadAssetAtPath<Sprite>(crestBackgroundPath);
                Sprite expectedCrestBorder = AssetDatabase.LoadAssetAtPath<Sprite>(crestBorderPath);
                Sprite expectedCrestInnerBorder = AssetDatabase.LoadAssetAtPath<Sprite>(crestInnerBorderPath);
                Sprite expectedBadgeBackground = AssetDatabase.LoadAssetAtPath<Sprite>(badgeBackgroundPath);
                Sprite expectedBadgeBorder = AssetDatabase.LoadAssetAtPath<Sprite>(badgeBorderPath);

                Assert.That(depthImage, Is.Not.Null);
                Assert.That(rimImage, Is.Not.Null);
                Assert.That(depth.gameObject.activeInHierarchy, Is.True);
                Assert.That(rim.gameObject.activeInHierarchy, Is.True);
                Assert.That(depthImage.enabled, Is.True);
                Assert.That(rimImage.enabled, Is.True);
                Assert.That(depthImage.sprite, Is.SameAs(expectedBackground));
                Assert.That(rimImage.sprite, Is.SameAs(expectedBorder));
                Assert.That(frontBackgroundImage.sprite, Is.SameAs(expectedBackground));
                Assert.That(frontBorderImage.sprite, Is.SameAs(expectedBorder));
                foreach (Image image in new[] { depthImage, rimImage, frontBackgroundImage, frontBorderImage })
                {
                    Assert.That(image.type, Is.EqualTo(Image.Type.Simple));
                    Assert.That(image.preserveAspect, Is.True);
                    Assert.That(image.raycastTarget, Is.False);
                }
                Assert.That(depthImage.raycastTarget, Is.False);
                Assert.That(rimImage.raycastTarget, Is.False);
                Assert.That(depth.GetComponentsInChildren<Selectable>(true), Is.Empty);
                Assert.That(rim.GetComponentsInChildren<Selectable>(true), Is.Empty);
                Assert.That(depth.GetComponentsInChildren<Button>(true), Is.Empty);
                Assert.That(rim.GetComponentsInChildren<Button>(true), Is.Empty);
                Assert.That(depthImage.color, Is.EqualTo(navyDonor.color));
                Assert.That(rimImage.color, Is.EqualTo(cyanDonor.color));
                Assert.That(depthImage.color, Is.EqualTo(new Color(0.0706f, 0.2275f, 0.5098f, 1f)));
                Assert.That(rimImage.color, Is.EqualTo(new Color(0.42f, 0.863f, 0.969f, 1f)));

                Assert.That(depth.GetSiblingIndex(), Is.LessThan(rim.GetSiblingIndex()));
                Assert.That(rim.GetSiblingIndex(), Is.LessThan(crest.GetSiblingIndex()));
                Assert.That(crest.GetSiblingIndex(), Is.LessThan(front.GetSiblingIndex()));
                Assert.That(crest.gameObject.activeSelf, Is.False);
                Assert.That(visual.GetSiblingIndex(), Is.LessThan(profile.Find("Content").GetSiblingIndex()));
                RectTransform depthRect = depth.GetComponent<RectTransform>();
                RectTransform rimRect = rim.GetComponent<RectTransform>();
                RectTransform frontRect = front.GetComponent<RectTransform>();
                Assert.That(depthRect.anchorMin, Is.EqualTo(Vector2.zero));
                Assert.That(depthRect.anchorMax, Is.EqualTo(Vector2.one));
                Assert.That(depthRect.offsetMin, Is.EqualTo(new Vector2(4f, -6f)));
                Assert.That(depthRect.offsetMax, Is.EqualTo(new Vector2(4f, -6f)));
                Assert.That(depthRect.localScale, Is.EqualTo(Vector3.one));
                Assert.That(rimRect.anchorMin, Is.EqualTo(Vector2.zero));
                Assert.That(rimRect.anchorMax, Is.EqualTo(Vector2.one));
                Assert.That(rimRect.offsetMin, Is.EqualTo(Vector2.zero));
                Assert.That(rimRect.offsetMax, Is.EqualTo(Vector2.zero));
                Assert.That(rimRect.localScale, Is.EqualTo(new Vector3(1.04f, 1.04f, 1f)));
                Assert.That(frontRect.anchorMin, Is.EqualTo(Vector2.zero));
                Assert.That(frontRect.anchorMax, Is.EqualTo(Vector2.one));
                Assert.That(frontRect.offsetMin, Is.EqualTo(Vector2.zero));
                Assert.That(frontRect.offsetMax, Is.EqualTo(Vector2.zero));
                Assert.That(frontBackground.GetComponent<RectTransform>().anchorMin, Is.EqualTo(Vector2.zero));
                Assert.That(frontBackground.GetComponent<RectTransform>().anchorMax, Is.EqualTo(Vector2.one));
                Assert.That(frontBorder.GetComponent<RectTransform>().anchorMin, Is.EqualTo(Vector2.zero));
                Assert.That(frontBorder.GetComponent<RectTransform>().anchorMax, Is.EqualTo(Vector2.one));
                Image crestBackgroundImage = crest.Find("Bg").GetComponent<Image>();
                Image crestInnerBorderImage = crest.Find("InnerBorder").GetComponent<Image>();
                Image crestBorderImage = crest.Find("Border").GetComponent<Image>();
                Assert.That(crestBackgroundImage.sprite, Is.SameAs(expectedCrestBackground));
                Assert.That(crestInnerBorderImage.sprite, Is.SameAs(expectedCrestInnerBorder));
                Assert.That(crestBorderImage.sprite, Is.SameAs(expectedCrestBorder));
                Assert.That(crestBackgroundImage.transform.GetSiblingIndex(), Is.LessThan(crestInnerBorderImage.transform.GetSiblingIndex()));
                Assert.That(crestInnerBorderImage.transform.GetSiblingIndex(), Is.LessThan(crestBorderImage.transform.GetSiblingIndex()));
                foreach (Image image in new[] { crestBackgroundImage, crestInnerBorderImage, crestBorderImage })
                {
                    Assert.That(image.enabled, Is.True);
                    Assert.That(image.type, Is.EqualTo(Image.Type.Simple));
                    Assert.That(image.preserveAspect, Is.True);
                    Assert.That(image.raycastTarget, Is.False);
                    Assert.That(image.GetComponentsInChildren<Selectable>(true), Is.Empty);
                }
                RectTransform crestRect = crest.GetComponent<RectTransform>();
                Assert.That(crestRect.anchorMin, Is.EqualTo(new Vector2(0.5f, 1f)));
                Assert.That(crestRect.anchorMax, Is.EqualTo(new Vector2(0.5f, 1f)));
                Assert.That(crestRect.pivot, Is.EqualTo(new Vector2(0.5f, 1f)));
                Assert.That(crestRect.anchoredPosition, Is.EqualTo(new Vector2(0f, 21f)));
                RectTransform visualRect = visual.GetComponent<RectTransform>();
                Assert.That(visualRect.anchorMin, Is.EqualTo(new Vector2(0.5f, 1f)));
                Assert.That(visualRect.anchorMax, Is.EqualTo(new Vector2(0.5f, 1f)));
                Assert.That(visualRect.pivot, Is.EqualTo(new Vector2(0.5f, 1f)));
                Assert.That(visualRect.sizeDelta, Is.EqualTo(new Vector2(170f, 198f)));
                Assert.That(visualRect.anchoredPosition, Is.EqualTo(new Vector2(0f, -4f)));
                Assert.That(level.anchoredPosition.x, Is.EqualTo(0f).Within(0.01f));
                Assert.That(level.sizeDelta, Is.EqualTo(new Vector2(70f, 28f)));

                Assert.That(badge, Is.Not.Null);
                Transform badgeDepth = badge.Find("LevelBadgeDepth");
                Transform badgeRim = badge.Find("LevelBadgeCyanRim");
                Transform badgeFace = badge.Find("LevelBadgeFace");
                Image badgeDepthImage = badgeDepth.GetComponent<Image>();
                Image badgeRimImage = badgeRim.GetComponent<Image>();
                Image badgeBackgroundImage = badgeFace.Find("Bg").GetComponent<Image>();
                Image badgeBorderImage = badgeFace.Find("Border").GetComponent<Image>();
                Assert.That(expectedBadgeBackground, Is.Not.Null);
                Assert.That(expectedBadgeBorder, Is.Not.Null);
                Assert.That(badgeDepthImage.sprite, Is.SameAs(expectedBadgeBackground));
                Assert.That(badgeRimImage.sprite, Is.SameAs(expectedBadgeBorder));
                Assert.That(badgeBackgroundImage.sprite, Is.SameAs(expectedBadgeBackground));
                Assert.That(badgeBorderImage.sprite, Is.SameAs(expectedBadgeBorder));
                foreach (Image image in new[] { badgeDepthImage, badgeRimImage, badgeBackgroundImage, badgeBorderImage })
                {
                    Assert.That(image.gameObject.activeInHierarchy, Is.True);
                    Assert.That(image.enabled, Is.True);
                    Assert.That(image.preserveAspect, Is.False);
                    Assert.That(image.raycastTarget, Is.False);
                    Assert.That(image.GetComponents<Button>(), Is.Empty);
                    Assert.That(image.GetComponents<Selectable>(), Is.Empty);
                }
                Assert.That(badgeDepthImage.color, Is.EqualTo(navyDonor.color));
                Assert.That(badgeRimImage.color, Is.EqualTo(cyanDonor.color));
                Assert.That(badgeDepth.GetSiblingIndex(), Is.LessThan(badgeRim.GetSiblingIndex()));
                Assert.That(badgeRim.GetSiblingIndex(), Is.LessThan(badgeFace.GetSiblingIndex()));
                RectTransform badgeRect = badge.GetComponent<RectTransform>();
                Assert.That(badgeRect.anchorMin, Is.EqualTo(new Vector2(0.5f, 0f)));
                Assert.That(badgeRect.anchorMax, Is.EqualTo(new Vector2(0.5f, 0f)));
                Assert.That(badgeRect.pivot, Is.EqualTo(new Vector2(0.5f, 1f)));
                Assert.That(badgeRect.sizeDelta, Is.EqualTo(new Vector2(77f, 72f)));
                Assert.That(badgeRect.anchoredPosition.x, Is.EqualTo(0f).Within(0.01f));

                RectTransform safe = GetLayoutSafeArea(profile);
                LobbyHomeReferenceLayout layout = safe.GetComponent<LobbyHomeReferenceLayout>();
                layout.RefreshLayout();
                Assert.That(level.anchoredPosition.x, Is.EqualTo(0f).Within(0.01f));
                Assert.That(badgeRect.anchoredPosition.x, Is.EqualTo(0f).Within(0.01f));
                Bounds frontBounds = RenderedImageBounds(frontBorderImage);
                Assert.That(frontBounds.size.x / frontBounds.size.y, Is.InRange(0.84f, 0.88f));
                Assert.That(frontBounds.Contains(portrait.position), Is.True);
                AssertLevelTextWithinBadge(profile, levelText, safe);
                TMP_Text startTitle = header.parent.Find("ContentViewport/BattleHomePanel/StartBattleButton/TitleText").GetComponent<TMP_Text>();
                Assert.That(levelText.color, Is.EqualTo(startTitle.color));
                AssertWithin(safe, depth, rim, front, badge);
                Rect frame = GetExpectedContentFrame(safe);
                AssertProfileVisibleBounds(profile, header.Find("GoldCurrencySlot"), safe, frame.xMin, frame.width, 0f, 5f);
            }
            finally
            {
                LobbySceneTestContext.RestoreLobbyWithoutSaving();
            }
        }

        [Test]
        public void LobbyHome_UsesBottomNavigationChassisProvenance()
        {
            Scene lobby = OpenLobby();
            try
            {
                Find(lobby, "@HomeLobby/SafeArea").GetComponent<LobbyHomeReferenceLayout>().RefreshLayout();
                Canvas.ForceUpdateCanvases();
                Transform navigationVisual = Find(lobby, "@HomeLobby/SafeArea/BottomNavigation/Visual/BottomNavigationVisual");
                Assert.That(navigationVisual, Is.Not.Null);
                Assert.That(navigationVisual.GetComponent<LobbyBottomNavigationView>(), Is.Not.Null);
                Assert.That(navigationVisual.childCount, Is.EqualTo(3));
                Assert.That(navigationVisual.GetChild(0).name, Is.EqualTo("Chassis"));
                Assert.That(navigationVisual.GetChild(1).name, Is.EqualTo("Dividers"));
                Assert.That(navigationVisual.GetChild(2).name, Is.EqualTo("Cells"));
                Transform sourceChassis = navigationVisual.Find("Chassis");
                string[] targetPaths =
                {
                    "@HomeLobby/SafeArea/PersistentHeader/Visual/BottomNavigationChassis",
                    "@HomeLobby/SafeArea/PersistentHeader/SettingsSlot/Visual/NativeSettingsFrame/BottomNavigationChassis",
                    "@HomeLobby/SafeArea/ContentViewport/LegionPassEntry/Visual/BottomNavigationChassis",
                    "@HomeLobby/SafeArea/ContentViewport/BattleHomePanel/EventShortcut/Visual/BottomNavigationChassis",
                    "@HomeLobby/SafeArea/ContentViewport/BattleHomePanel/MiniPassShortcut/Visual/BottomNavigationChassis",
                    "@HomeLobby/SafeArea/ContentViewport/BattleHomePanel/StartBattleButton/Visual/NativeStartBattleButton/BottomNavigationChassis",
                };
                for (int i = 0; i < targetPaths.Length; i++) AssertBottomNavigationChassis(Find(lobby, targetPaths[i]), sourceChassis);

                AssertReferencePercentRect(Find(lobby, "@HomeLobby/SafeArea/PersistentHeader/Visual/BottomNavigationChassis"), 151f / 865f, 49f / 1819f, 703f / 865f, 116f / 1819f);
                AssertReferencePercentRect(Find(lobby, "@HomeLobby/SafeArea/PersistentHeader/GoldCurrencySlot"), 187f / 865f, 78f / 1819f, 264f / 865f, 56f / 1819f);
                AssertReferencePercentRect(Find(lobby, "@HomeLobby/SafeArea/PersistentHeader/GoldCurrencySlot/Content/NativeAddButton"), 401f / 865f, 80f / 1819f, 50f / 865f, 53f / 1819f);
                AssertReferencePercentRect(Find(lobby, "@HomeLobby/SafeArea/PersistentHeader/GemCurrencySlot"), 497f / 865f, 78f / 1819f, 245f / 865f, 56f / 1819f);
                AssertReferencePercentRect(Find(lobby, "@HomeLobby/SafeArea/PersistentHeader/GemCurrencySlot/Content/NativeAddButton"), 692f / 865f, 80f / 1819f, 50f / 865f, 53f / 1819f);
                AssertReferenceSquare(Find(lobby, "@HomeLobby/SafeArea/PersistentHeader/SettingsSlot"), new Vector2(799.5f / 865f, 106.5f / 1819f), 65f / 1819f);
                AssertReferencePercentRect(Find(lobby, "@HomeLobby/SafeArea/ContentViewport/LegionPassEntry"), 114f / 865f, 209f / 1819f, 646f / 865f, 162f / 1819f);
                AssertReferenceCircle(Find(lobby, "@HomeLobby/SafeArea/ContentViewport/BattleHomePanel/EventShortcut/Content/Icon"), new Vector2(104f / 865f, 492f / 1819f), 148f / 1819f);
                AssertReferencePercentRect(Find(lobby, "@HomeLobby/SafeArea/ContentViewport/BattleHomePanel/EventShortcut"), 17f / 865f, 555f / 1819f, 167f / 865f, 113f / 1819f);
                AssertReferenceCircle(Find(lobby, "@HomeLobby/SafeArea/ContentViewport/BattleHomePanel/MiniPassShortcut/Content/Icon"), new Vector2(760f / 865f, 492f / 1819f), 148f / 1819f);
                AssertReferencePercentRect(Find(lobby, "@HomeLobby/SafeArea/ContentViewport/BattleHomePanel/MiniPassShortcut"), 676f / 865f, 555f / 1819f, 166f / 865f, 113f / 1819f);
                Transform startBattle = Find(lobby, "@HomeLobby/SafeArea/ContentViewport/BattleHomePanel/StartBattleButton");
                RectTransform safe = GetLayoutSafeArea(startBattle);
                Rect frame = GetExpectedContentFrame(safe);
                AssertReferenceFrameRect(startBattle, safe, frame.xMin + frame.width * (145f / 865f), -1f, frame.width * (573f / 865f), safe.rect.height * (254f / 1819f));
            }
            finally { LobbySceneTestContext.RestoreLobbyWithoutSaving(); }
        }

        [Test]
        public void LobbyHome_ResourceBarUsesReferenceRailAndGearHierarchy()
        {
            const string resourceBarPath = "Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Blue/Sprites/HUD/ResourceBar_Bg.png";
            const string gearPath = "Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/UniqueIcon/512/UI_System_Setting_01.png";
            Scene lobby = OpenLobby();
            try
            {
                Transform safe = Find(lobby, "@HomeLobby/SafeArea");
                LobbyHomeReferenceLayout layout = safe.GetComponent<LobbyHomeReferenceLayout>();
                Assert.That(layout, Is.Not.Null);
                layout.RefreshLayout();
                Canvas.ForceUpdateCanvases();

                Sprite resourceBar = AssetDatabase.LoadAssetAtPath<Sprite>(resourceBarPath);
                Sprite gear = AssetDatabase.LoadAssetAtPath<Sprite>(gearPath);
                Assert.That(resourceBar, Is.Not.Null);
                Assert.That(gear, Is.Not.Null);

                RectTransform gold = safe.Find("PersistentHeader/GoldCurrencySlot").GetComponent<RectTransform>();
                RectTransform gem = safe.Find("PersistentHeader/GemCurrencySlot").GetComponent<RectTransform>();
                AssertReferenceResourceRail(gold, resourceBar, "GoldCurrencySlot");
                AssertReferenceResourceRail(gem, resourceBar, "GemCurrencySlot");
                AssertResourceRailEndCapsAreOccluded(gold, "GoldCurrencySlot");
                AssertResourceRailEndCapsAreOccluded(gem, "GemCurrencySlot");
                Vector4 goldRail = ScreenBounds(RenderedImageBounds(gold.Find("Visual/BottomNavigationChassis/Bg").GetComponent<Image>()), safe.GetComponent<RectTransform>());
                Vector4 gemRail = ScreenBounds(RenderedImageBounds(gem.Find("Visual/BottomNavigationChassis/Bg").GetComponent<Image>()), safe.GetComponent<RectTransform>());
                Assert.That(goldRail.w, Is.EqualTo(gemRail.w).Within(1f), "Currency rails share the reference height.");
                Assert.That(goldRail.y + goldRail.w * 0.5f, Is.EqualTo(gemRail.y + gemRail.w * 0.5f).Within(1f), "Currency rails share the reference baseline.");

                Image settingsIcon = safe.Find("PersistentHeader/SettingsSlot/Content/Icon").GetComponent<Image>();
                RectTransform settings = safe.Find("PersistentHeader/SettingsSlot").GetComponent<RectTransform>();
                TMP_Text settingsVendorLabel = safe.Find("PersistentHeader/SettingsSlot/Visual/NativeSettingsFrame/Text (TMP)").GetComponent<TMP_Text>();
                Assert.That(settingsIcon.sprite, Is.SameAs(gear));
                Assert.That(settingsIcon.enabled, Is.True);
                Assert.That(settingsIcon.raycastTarget, Is.False);
                Assert.That(settingsVendorLabel.text, Is.EqualTo("Button"));
                Assert.That(settingsVendorLabel.gameObject.activeSelf, Is.False, "Surplus Settings vendor text must not render.");
                Assert.That(settingsVendorLabel.raycastTarget, Is.False);
                Assert.That(settingsVendorLabel.GetComponent<Selectable>(), Is.Null);
                Assert.That(settingsIcon.GetComponentsInChildren<Button>(true), Is.Empty);
                Assert.That(settingsIcon.GetComponentsInChildren<Selectable>(true), Is.Empty);
                AssertReferenceSquare(settings, new Vector2(799.5f / 865f, 106.5f / 1819f), 65f / 1819f);
                Assert.That(RenderedImageBounds(settingsIcon).Contains(settings.position), Is.True);

                RectTransform authoredSafe = safe.GetComponent<RectTransform>();
                foreach ((float width, float height) in new[] { (1080f, 2340f), (1080f, 2520f), (1968f, 2184f) })
                {
                    float outputScale = Mathf.Min(width / 1080f, height / 1920f);
                    using (TemporaryCanvasFixture fixture = TemporaryCanvasFixture.Create(authoredSafe, width, height, outputScale))
                    {
                        RectTransform fixtureSafe = fixture.SafeArea;
                        fixtureSafe.GetComponent<LobbyHomeReferenceLayout>().RefreshLayout();
                        Canvas.ForceUpdateCanvases();
                        AssertResourceRailEndCapsAreOccluded(fixtureSafe.Find("PersistentHeader/GoldCurrencySlot").GetComponent<RectTransform>(), "GoldCurrencySlot.fixture");
                        AssertResourceRailEndCapsAreOccluded(fixtureSafe.Find("PersistentHeader/GemCurrencySlot").GetComponent<RectTransform>(), "GemCurrencySlot.fixture");
                        AssertResourceTextStress(fixture.Canvas, fixtureSafe, "GoldCurrencySlot", "999,999");
                        AssertResourceTextStress(fixture.Canvas, fixtureSafe, "GemCurrencySlot", "99,999");
                    }
                }
            }
            finally { LobbySceneTestContext.RestoreLobbyWithoutSaving(); }
        }

        [Test]
        public void LobbyHome_UsesNormalizedReferencePercentContract()
        {
            Scene lobby = OpenLobby();
            try
            {
                LobbyHomeReferenceLayout layout = Find(lobby, "@HomeLobby/SafeArea").GetComponent<LobbyHomeReferenceLayout>();
                Assert.That(layout, Is.Not.Null);
                Assert.That(Find(lobby, "@HomeLobby/SafeArea").GetComponents<LobbyHomeReferenceLayout>(), Has.Length.EqualTo(1));
                SerializedObject layoutSerialized = new SerializedObject(layout);
                Assert.That(layoutSerialized.FindProperty("_headerChassis").objectReferenceValue, Is.SameAs(Find(lobby, "@HomeLobby/SafeArea/PersistentHeader/Visual/BottomNavigationChassis").GetComponent<RectTransform>()));
                Assert.That(layoutSerialized.FindProperty("_profileSlot").objectReferenceValue, Is.SameAs(Find(lobby, "@HomeLobby/SafeArea/PersistentHeader/ProfileSlot").GetComponent<RectTransform>()));
                Assert.That(layoutSerialized.FindProperty("_profileLevelBadgeExtent").objectReferenceValue, Is.SameAs(Find(lobby, "@HomeLobby/SafeArea/PersistentHeader/ProfileSlot/Visual/LevelBadge/LevelBadgeDepth").GetComponent<RectTransform>()));
                Assert.That(layoutSerialized.FindProperty("_gold").objectReferenceValue, Is.SameAs(Find(lobby, "@HomeLobby/SafeArea/PersistentHeader/GoldCurrencySlot").GetComponent<RectTransform>()));
                Assert.That(layoutSerialized.FindProperty("_goldAdd").objectReferenceValue, Is.SameAs(Find(lobby, "@HomeLobby/SafeArea/PersistentHeader/GoldCurrencySlot/Content/NativeAddButton").GetComponent<RectTransform>()));
                Assert.That(layoutSerialized.FindProperty("_gem").objectReferenceValue, Is.SameAs(Find(lobby, "@HomeLobby/SafeArea/PersistentHeader/GemCurrencySlot").GetComponent<RectTransform>()));
                Assert.That(layoutSerialized.FindProperty("_gemAdd").objectReferenceValue, Is.SameAs(Find(lobby, "@HomeLobby/SafeArea/PersistentHeader/GemCurrencySlot/Content/NativeAddButton").GetComponent<RectTransform>()));
                Assert.That(layoutSerialized.FindProperty("_settings").objectReferenceValue, Is.SameAs(Find(lobby, "@HomeLobby/SafeArea/PersistentHeader/SettingsSlot").GetComponent<RectTransform>()));
                Assert.That(layoutSerialized.FindProperty("_legionPass").objectReferenceValue, Is.SameAs(Find(lobby, "@HomeLobby/SafeArea/ContentViewport/LegionPassEntry").GetComponent<RectTransform>()));
                Assert.That(layoutSerialized.FindProperty("_eventPanel").objectReferenceValue, Is.SameAs(Find(lobby, "@HomeLobby/SafeArea/ContentViewport/BattleHomePanel/EventShortcut").GetComponent<RectTransform>()));
                Assert.That(layoutSerialized.FindProperty("_eventIcon").objectReferenceValue, Is.SameAs(Find(lobby, "@HomeLobby/SafeArea/ContentViewport/BattleHomePanel/EventShortcut/Content/Icon").GetComponent<RectTransform>()));
                Assert.That(layoutSerialized.FindProperty("_miniPanel").objectReferenceValue, Is.SameAs(Find(lobby, "@HomeLobby/SafeArea/ContentViewport/BattleHomePanel/MiniPassShortcut").GetComponent<RectTransform>()));
                Assert.That(layoutSerialized.FindProperty("_miniIcon").objectReferenceValue, Is.SameAs(Find(lobby, "@HomeLobby/SafeArea/ContentViewport/BattleHomePanel/MiniPassShortcut/Content/Icon").GetComponent<RectTransform>()));
                Assert.That(layoutSerialized.FindProperty("_startBattle").objectReferenceValue, Is.SameAs(Find(lobby, "@HomeLobby/SafeArea/ContentViewport/BattleHomePanel/StartBattleButton").GetComponent<RectTransform>()));
                Image rootImage = Find(lobby, "@HomeLobby/SafeArea/PersistentHeader").GetComponent<Image>();
                Assert.That(rootImage.enabled, Is.True);
                Assert.That(rootImage.raycastTarget, Is.False);
                Assert.That(rootImage.sprite, Is.Null);
                layout.RefreshLayout();
                AssertReferencePercentRect(Find(lobby, "@HomeLobby/SafeArea/PersistentHeader/Visual/BottomNavigationChassis"), 151f / 865f, 49f / 1819f, 703f / 865f, 116f / 1819f);
                AssertReferencePercentRect(Find(lobby, "@HomeLobby/SafeArea/PersistentHeader/GoldCurrencySlot"), 187f / 865f, 78f / 1819f, 264f / 865f, 56f / 1819f);
                AssertReferencePercentRect(Find(lobby, "@HomeLobby/SafeArea/PersistentHeader/GoldCurrencySlot/Content/NativeAddButton"), 401f / 865f, 80f / 1819f, 50f / 865f, 53f / 1819f);
                AssertReferencePercentRect(Find(lobby, "@HomeLobby/SafeArea/PersistentHeader/GemCurrencySlot"), 497f / 865f, 78f / 1819f, 245f / 865f, 56f / 1819f);
                AssertReferencePercentRect(Find(lobby, "@HomeLobby/SafeArea/PersistentHeader/GemCurrencySlot/Content/NativeAddButton"), 692f / 865f, 80f / 1819f, 50f / 865f, 53f / 1819f);
                AssertReferenceSquare(Find(lobby, "@HomeLobby/SafeArea/PersistentHeader/SettingsSlot"), new Vector2(799.5f / 865f, 106.5f / 1819f), 65f / 1819f);
                AssertReferencePercentRect(Find(lobby, "@HomeLobby/SafeArea/ContentViewport/LegionPassEntry"), 114f / 865f, 209f / 1819f, 646f / 865f, 162f / 1819f);
                Transform eventPanel = Find(lobby, "@HomeLobby/SafeArea/ContentViewport/BattleHomePanel/EventShortcut");
                Transform miniPanel = Find(lobby, "@HomeLobby/SafeArea/ContentViewport/BattleHomePanel/MiniPassShortcut");
                Transform eventIcon = eventPanel.Find("Content/Icon");
                Transform miniIcon = miniPanel.Find("Content/Icon");
                AssertReferenceCircle(eventIcon, new Vector2(104f / 865f, 492f / 1819f), 148f / 1819f);
                AssertReferencePercentRect(eventPanel, 17f / 865f, 555f / 1819f, 167f / 865f, 113f / 1819f);
                AssertReferenceCircle(miniIcon, new Vector2(760f / 865f, 492f / 1819f), 148f / 1819f);
                AssertReferencePercentRect(miniPanel, 676f / 865f, 555f / 1819f, 166f / 865f, 113f / 1819f);
                AssertStartBattleFrameClearance(Find(lobby, "@HomeLobby/SafeArea/ContentViewport/BattleHomePanel/StartBattleButton"), Find(lobby, "@HomeLobby/SafeArea/BottomNavigation/Visual/BottomNavigationVisual/Cells/Cell_01"));
                AssertCirclePanelOverlap(eventIcon, eventPanel, 11.6f);
                AssertCirclePanelOverlap(miniIcon, miniPanel, 11.6f);
                AssertPanelText(eventPanel, "Content/TitleText", 0.32f);
                AssertPanelText(eventPanel, "Content/StatusText", 0.71f);
                AssertPanelText(miniPanel, "Content/TitleText", 0.32f);
                AssertPanelText(miniPanel, "Content/StatusText", 0.71f);
            }
            finally { LobbySceneTestContext.RestoreLobbyWithoutSaving(); }
        }

        [Test]
        public void LobbyHome_OnlyRendersSemanticCtaAndCurrencyAddLabels()
        {
            Scene lobby = OpenLobby();
            try
            {
                Transform safe = Find(lobby, "@HomeLobby/SafeArea");
                Transform startBattle = safe.Find("ContentViewport/BattleHomePanel/StartBattleButton");
                Assert.That(startBattle.Find("TitleText").GetComponent<TMP_Text>().text, Is.EqualTo("\uCD9C\uC815"));
                Assert.That(startBattle.Find("Visual/NativeStartBattleButton/Text (TMP)").gameObject.activeSelf, Is.False);
                Assert.That(startBattle.Find("StageText").gameObject.activeSelf, Is.False);
                Assert.That(startBattle.Find("Label").gameObject.activeSelf, Is.False);

                foreach (string currency in new[] { "GoldCurrencySlot", "GemCurrencySlot" })
                {
                    Transform slot = safe.Find("PersistentHeader/" + currency);
                    Assert.That(slot.Find("Content/AddSlot").GetComponent<TMP_Text>().text, Is.EqualTo("+"));
                    Assert.That(slot.Find("Content/AddSlot").gameObject.activeSelf, Is.True);
                    Assert.That(slot.Find("Content/NativeAddButton/Text (TMP)").gameObject.activeSelf, Is.False);
                }

                LobbyHomeReferenceLayout layout = safe.GetComponent<LobbyHomeReferenceLayout>();
                SerializedObject layoutSerialized = new SerializedObject(layout);
                Assert.That(layoutSerialized.FindProperty("_persistentHeaderRoot").objectReferenceValue,
                    Is.SameAs(safe.Find("PersistentHeader").GetComponent<RectTransform>()));

                Image headerImage = safe.Find("PersistentHeader").GetComponent<Image>();
                LobbyDraftThemeBinder binder = Find(lobby, "@HomeLobby").GetComponent<LobbyDraftThemeBinder>();
                Assert.That(headerImage.enabled, Is.True);
                Assert.That(headerImage.raycastTarget, Is.False);
                Assert.That(new SerializedObject(binder).FindProperty("_persistentHeaderImage").objectReferenceValue,
                    Is.SameAs(headerImage));
            }
            finally { LobbySceneTestContext.RestoreLobbyWithoutSaving(); }
        }

        [Test]
        public void LobbyHome_CurrencyAddSlotsRenderAboveNativeAddChassis()
        {
            Scene lobby = OpenLobby();
            try
            {
                Transform safe = Find(lobby, "@HomeLobby/SafeArea");
                safe.GetComponent<LobbyHomeReferenceLayout>().RefreshLayout();
                Canvas.ForceUpdateCanvases();
                foreach (string currency in new[] { "GoldCurrencySlot", "GemCurrencySlot" })
                {
                    Transform content = safe.Find("PersistentHeader/" + currency + "/Content");
                    RectTransform addSlot = content.Find("AddSlot").GetComponent<RectTransform>();
                    TMP_Text addText = addSlot.GetComponent<TMP_Text>();
                    RectTransform nativeAdd = content.Find("NativeAddButton").GetComponent<RectTransform>();
                    Image addChassis = nativeAdd.Find("BottomNavigationChassis/Bg").GetComponent<Image>();
                    Bounds glyphBounds = RenderedTextBounds(addText);

                    Assert.That(addSlot.gameObject.activeInHierarchy, Is.True, currency + ".active");
                    Assert.That(addText.enabled, Is.True, currency + ".enabled");
                    Assert.That(addText.text, Is.EqualTo("+"), currency + ".text");
                    Assert.That(addText.color.a, Is.GreaterThan(0f), currency + ".graphicAlpha");
                    Assert.That(addText.alpha, Is.GreaterThan(0f), currency + ".tmpAlpha");
                    Assert.That(RenderedImageBounds(addChassis).Contains(glyphBounds.center), Is.True, currency + ".glyphCenter");
                    Assert.That(addSlot.GetSiblingIndex(), Is.GreaterThan(nativeAdd.GetSiblingIndex()), currency + ".renderOrder");
                    Assert.That(nativeAdd.Find("BottomNavigationChassis").gameObject.activeInHierarchy, Is.True, currency + ".chassis");
                }
            }
            finally { LobbySceneTestContext.RestoreLobbyWithoutSaving(); }
        }

        [Test]
        public void LobbyHome_LegionPremiumEntryStaysInsidePanel()
        {
            Scene lobby = OpenLobby();
            try
            {
                Transform pass = Find(lobby, "@HomeLobby/SafeArea/ContentViewport/LegionPassEntry");
                RectTransform premium = pass.Find("Content/PremiumEntry").GetComponent<RectTransform>();
                Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(pass, premium);
                Rect panel = pass.GetComponent<RectTransform>().rect;
                Assert.That(bounds.min.x, Is.GreaterThan(panel.xMin + 2f));
                Assert.That(bounds.max.x, Is.LessThan(panel.xMax - 2f));
                Assert.That(bounds.min.y, Is.GreaterThan(panel.yMin + 2f));
                Assert.That(bounds.max.y, Is.LessThan(panel.yMax - 2f));
            }
            finally { LobbySceneTestContext.RestoreLobbyWithoutSaving(); }
        }

        [TestCase(1080f, 2340f)]
        [TestCase(1080f, 2520f)]
        [TestCase(1968f, 2184f)]
        public void LobbyHome_CtaClearsVisibleSelectedBattleFrameAcrossResponsiveMatrix(float width, float height)
        {
            Scene lobby = OpenLobby();
            RectTransform authoredSafe = Find(lobby, "@HomeLobby/SafeArea").GetComponent<RectTransform>();
            RectTransformState authoredSafeBefore = new RectTransformState(authoredSafe);
            RectTransformState authoredHeaderBefore = new RectTransformState(
                Find(lobby, "@HomeLobby/SafeArea/PersistentHeader/Visual/BottomNavigationChassis").GetComponent<RectTransform>());
            RectTransformState authoredPassBefore = new RectTransformState(
                Find(lobby, "@HomeLobby/SafeArea/ContentViewport/LegionPassEntry").GetComponent<RectTransform>());
            try
            {
                AssertAuthoredSafeAreaStretch(authoredSafe);
                using (TemporarySafeAreaFixture fixture = TemporarySafeAreaFixture.Create(authoredSafe, width, height))
                {
                    RectTransform safe = fixture.SafeArea;
                    safe.GetComponent<LobbyHomeReferenceLayout>().RefreshLayout();
                    UnityEngine.Canvas.ForceUpdateCanvases();
                    Transform header = safe.Find("PersistentHeader/Visual/BottomNavigationChassis");
                    Transform legionPass = safe.Find("ContentViewport/LegionPassEntry");
                    Transform eventPanel = safe.Find("ContentViewport/BattleHomePanel/EventShortcut");
                    Transform miniPanel = safe.Find("ContentViewport/BattleHomePanel/MiniPassShortcut");
                    Transform eventIcon = eventPanel.Find("Content/Icon");
                    Transform miniIcon = miniPanel.Find("Content/Icon");
                    Transform cta = safe.Find("ContentViewport/BattleHomePanel/StartBattleButton");
                    Transform profile = safe.Find("PersistentHeader/ProfileSlot");
                    Transform gold = safe.Find("PersistentHeader/GoldCurrencySlot");
                    Transform headerRoot = safe.Find("PersistentHeader");
                    Transform premium = legionPass.Find("Content/PremiumEntry");
                    Transform battleFrame = safe.Find("BottomNavigation/Visual/BottomNavigationVisual/Cells/Cell_01");
                    Assert.That(battleFrame.Find("Selected").gameObject.activeSelf, Is.True);

                    float referenceAspect = 865f / 1819f;
                    float frameWidth = width / height <= referenceAspect ? width : height * referenceAspect;
                    float frameLeft = (width - frameWidth) * 0.5f;
                    Assert.That(safe.rect.width, Is.EqualTo(width).Within(0.1f));
                    Assert.That(safe.rect.height, Is.EqualTo(height).Within(0.1f));
                    AssertContentFrameRect(header, safe, frameLeft, frameWidth, 151f, 49f, 703f, 116f);
                    AssertContentFrameRect(legionPass, safe, frameLeft, frameWidth, 114f, 209f, 646f, 162f);
                    AssertReferenceFrameSquare(safe.Find("PersistentHeader/SettingsSlot"), safe, frameLeft + frameWidth * (799.5f / 865f), height * (106.5f / 1819f), height * (65f / 1819f));
                    AssertTallProfileRoot(profile, safe, frameLeft, frameWidth);
                    AssertProfileCompositeScale(profile, frameWidth);
                    AssertProfileRelationOffsetPreservesX(profile);
                    AssertFabricProfileRelations(profile, header, legionPass, gold, safe, frameLeft, frameWidth);
                    AssertReferenceFrameSquare(eventIcon, safe, frameLeft + frameWidth * (104f / 865f), height * (492f / 1819f), height * (148f / 1819f));
                    AssertReferenceFrameSquare(miniIcon, safe, frameLeft + frameWidth * (760f / 865f), height * (492f / 1819f), height * (148f / 1819f));
                    AssertReferenceFrameRect(eventPanel, safe, frameLeft + frameWidth * (17f / 865f), 555f / 1819f, frameWidth * (167f / 865f), height * (113f / 1819f));
                    AssertReferenceFrameRect(miniPanel, safe, frameLeft + frameWidth * (676f / 865f), 555f / 1819f, frameWidth * (166f / 865f), height * (113f / 1819f));
                    AssertReferenceFrameRect(cta, safe, frameLeft + frameWidth * (145f / 865f), -1f, frameWidth * (573f / 865f), height * (254f / 1819f));
                    float clearance = SafeAreaVerticalGap(safe, cta.GetComponent<RectTransform>(), battleFrame.GetComponent<RectTransform>());
                    Assert.That(clearance, Is.EqualTo(height * (19f / 1819f)).Within(2f), "CTA visual bottom must clear the selected Battle frame.");
                    AssertCirclePanelOverlap(eventIcon, eventPanel, height * (11f / 1819f));
                    AssertCirclePanelOverlap(miniIcon, miniPanel, height * (11f / 1819f));
                    AssertPanelText(eventPanel, "Content/TitleText", 0.32f);
                    AssertPanelText(eventPanel, "Content/StatusText", 0.71f);
                    AssertPanelText(miniPanel, "Content/TitleText", 0.32f);
                    AssertPanelText(miniPanel, "Content/StatusText", 0.71f);
                    AssertProfileVisibleBounds(profile, gold, safe, frameLeft, frameWidth, 12f, 1f);
                    AssertContentFrameRect(headerRoot, safe, frameLeft, frameWidth, 0f, 0f, 865f, 192f);
                    AssertContainedByPanel(legionPass, premium, 2f);
                    AssertWithin(safe, header, profile, legionPass, eventPanel, miniPanel, eventIcon, miniIcon, cta);
                }

                if (width == 1968f)
                    AssertFoldOutputPixelProfileContainment(authoredSafe, width, height);
            }
            finally
            {
                AssertAuthoredSafeAreaStretch(authoredSafe);
                authoredSafeBefore.AssertUnchanged(authoredSafe, "SafeArea");
                authoredHeaderBefore.AssertUnchanged(Find(lobby, "@HomeLobby/SafeArea/PersistentHeader/Visual/BottomNavigationChassis").GetComponent<RectTransform>(), "Header");
                authoredPassBefore.AssertUnchanged(Find(lobby, "@HomeLobby/SafeArea/ContentViewport/LegionPassEntry").GetComponent<RectTransform>(), "LegionPass");
            }
        }

        static void AssertReferencePercentRect(Transform target, float left, float top, float width, float height)
        {
            RectTransform rect = target.GetComponent<RectTransform>();
            RectTransform safe = GetLayoutSafeArea(target);
            Rect frame = GetExpectedContentFrame(safe);
            Vector4 actual = ScreenRect(rect, safe);
            Assert.That(actual.x, Is.EqualTo(frame.xMin + frame.width * left).Within(2f), target.name + ".left");
            Assert.That(actual.y, Is.EqualTo(safe.rect.height * top).Within(2f), target.name + ".top");
            Assert.That(actual.z, Is.EqualTo(frame.width * width).Within(2f), target.name + ".width");
            Assert.That(actual.w, Is.EqualTo(safe.rect.height * height).Within(2f), target.name + ".height");
        }

        static void AssertContainedByPanel(Transform panel, Transform child, float padding)
        {
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            Bounds childBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(panel, child);
            Assert.That(childBounds.min.x, Is.GreaterThan(panelRect.rect.xMin + padding), child.name + ".left");
            Assert.That(childBounds.max.x, Is.LessThan(panelRect.rect.xMax - padding), child.name + ".right");
            Assert.That(childBounds.min.y, Is.GreaterThan(panelRect.rect.yMin + padding), child.name + ".bottom");
            Assert.That(childBounds.max.y, Is.LessThan(panelRect.rect.yMax - padding), child.name + ".top");
        }

        static RectTransform GetLayoutSafeArea(Transform target)
        {
            LobbyHomeReferenceLayout layout = target.GetComponentInParent<LobbyHomeReferenceLayout>();
            Assert.That(layout, Is.Not.Null, target.name + ".layout");
            return layout.transform as RectTransform;
        }

        static Rect GetExpectedContentFrame(RectTransform safe)
        {
            const float referenceAspect = 865f / 1819f;
            float width = safe.rect.width / safe.rect.height <= referenceAspect
                ? safe.rect.width
                : safe.rect.height * referenceAspect;
            float screenLeft = safe.rect.center.x - width * 0.5f + safe.rect.width * 0.5f;
            return new Rect(screenLeft, safe.rect.yMin, width, safe.rect.height);
        }

        static void AssertReferenceFrameRect(Transform target, RectTransform safe, float left, float top, float width, float height)
        {
            Vector4 actual = ScreenRect(target.GetComponent<RectTransform>(), safe);
            Assert.That(actual.x, Is.EqualTo(left).Within(2f), target.name + ".left");
            if (top >= 0f) Assert.That(actual.y, Is.EqualTo(safe.rect.height * top).Within(2f), target.name + ".top");
            Assert.That(actual.z, Is.EqualTo(width).Within(2f), target.name + ".width");
            Assert.That(actual.w, Is.EqualTo(height).Within(2f), target.name + ".height");
        }

        static void AssertContentFrameRect(Transform target, RectTransform safe, float frameLeft, float frameWidth, float left, float top, float width, float height)
        {
            float scale = frameWidth / 865f;
            Vector4 actual = ScreenRect(target.GetComponent<RectTransform>(), safe);
            Assert.That(actual.x, Is.EqualTo(frameLeft + left * scale).Within(2f), target.name + ".left");
            Assert.That(actual.y, Is.EqualTo(top * scale).Within(2f), target.name + ".top");
            Assert.That(actual.z, Is.EqualTo(width * scale).Within(2f), target.name + ".width");
            Assert.That(actual.w, Is.EqualTo(height * scale).Within(2f), target.name + ".height");
        }

        static void AssertTallProfileRoot(Transform profile, RectTransform safe, float frameLeft, float frameWidth)
        {
            Vector4 actual = ScreenBounds(WorldBounds(profile.GetComponent<RectTransform>()), safe);
            float scale = frameWidth / 865f;
            float referenceLeft = safe.rect.width / safe.rect.height > 865f / 1819f ? 13.13f : 13f;
            Assert.That(actual.x, Is.EqualTo(frameLeft + referenceLeft * scale).Within(2f), "Profile left");
            Assert.That(actual.y, Is.EqualTo(21f * scale).Within(2f), "Profile top");
            Assert.That(actual.z, Is.EqualTo(142f * scale).Within(2f), "Profile width");
            Assert.That(actual.w, Is.EqualTo(193f * scale).Within(2f), "Profile height");
        }

        static void AssertProfileCompositeScale(Transform profile, float contentFrameWidth)
        {
            float expected = contentFrameWidth / 1080f;
            Vector3 actual = profile.localScale;
            Assert.That(actual.x, Is.EqualTo(expected).Within(0.001f), "Profile composite X scale");
            Assert.That(actual.y, Is.EqualTo(expected).Within(0.001f), "Profile composite Y scale");
            Assert.That(actual.x, Is.EqualTo(actual.y).Within(0.0001f), "Profile composite must remain uniformly scaled.");
        }

        static void AssertFabricProfileRelations(Transform profile, Transform header, Transform legionPass, Transform gold, RectTransform safe, float frameLeft, float frameWidth)
        {
            Vector4 profileBounds = ScreenBounds(RenderedVisibleBounds(profile), safe);
            Assert.That(profileBounds.x, Is.GreaterThanOrEqualTo(frameLeft - 1f), "Profile left frame containment");
            Assert.That(profileBounds.x + profileBounds.z, Is.LessThanOrEqualTo(frameLeft + frameWidth + 1f), "Profile right frame containment");
            AssertFabricProfileHeaderPassRelations(profile, header, legionPass, safe, frameWidth);
            AssertProfileVisibleBounds(profile, gold, safe, frameLeft, frameWidth, 12f, 1f);
        }

        static void AssertProfileRelationOffsetPreservesX(Transform profile)
        {
            RectTransform badge = profile.Find("Visual/LevelBadge").GetComponent<RectTransform>();
            RectTransform levelText = profile.Find("Content/LevelText").GetComponent<RectTransform>();
            Assert.That(badge.anchoredPosition.x, Is.EqualTo(0f).Within(0.01f), "Profile LevelBadge relation offset must preserve authored X.");
            Assert.That(levelText.anchoredPosition.x, Is.EqualTo(0f).Within(0.01f), "Profile LevelText relation offset must preserve authored X.");
        }

        static void AssertFabricProfileHeaderPassRelations(Transform profile, Transform header, Transform legionPass, RectTransform safe, float frameWidth)
        {
            float scale = frameWidth / 865f;
            Vector4 profileBounds = ScreenBounds(RenderedVisibleBounds(profile), safe);
            Vector4 headerBounds = ScreenRect(header.GetComponent<RectTransform>(), safe);
            Vector4 passBounds = ScreenRect(legionPass.GetComponent<RectTransform>(), safe);
            Assert.That(profileBounds.x + profileBounds.z - headerBounds.x, Is.EqualTo(4f * scale).Within(3f), "Profile/Header horizontal overlap");
            Assert.That(passBounds.y - (profileBounds.y + profileBounds.w), Is.EqualTo(-5f * scale).Within(3f), "Profile/Pass shallow corner overlap");
            Assert.That(passBounds.y - (headerBounds.y + headerBounds.w), Is.EqualTo(44f * scale).Within(3f), "Header/Pass floating gap");
            Assert.That(safe.Find("ContentViewport").GetSiblingIndex(), Is.LessThan(safe.Find("PersistentHeader").GetSiblingIndex()), "Profile header must render above the pass.");
        }

        static void AssertTallProfileComposite(Transform profile, Transform gold, RectTransform safe, float frameLeft, float frameWidth, bool phoneFixture)
        {
            Transform visual = profile.Find("Visual");
            Image depth = visual.Find("ProfileChassisDepth").GetComponent<Image>();
            Image rim = visual.Find("ProfileChassisCyanRim").GetComponent<Image>();
            Image background = visual.Find("ProfileChassisFace/Bg").GetComponent<Image>();
            Image border = visual.Find("ProfileChassisFace/Border").GetComponent<Image>();
            Image crestBackground = visual.Find("ProfileTopCrest/Bg").GetComponent<Image>();
            Image crestInnerBorder = visual.Find("ProfileTopCrest/InnerBorder").GetComponent<Image>();
            Image crestBorder = visual.Find("ProfileTopCrest/Border").GetComponent<Image>();
            Transform badge = visual.Find("LevelBadge");
            Image badgeDepth = badge.Find("LevelBadgeDepth").GetComponent<Image>();
            Image badgeRim = badge.Find("LevelBadgeCyanRim").GetComponent<Image>();
            Image badgeBackground = badge.Find("LevelBadgeFace/Bg").GetComponent<Image>();
            Image badgeBorder = badge.Find("LevelBadgeFace/Border").GetComponent<Image>();
            Bounds body = RenderedImageBounds(depth);
            body.Encapsulate(RenderedImageBounds(rim));
            body.Encapsulate(RenderedImageBounds(background));
            body.Encapsulate(RenderedImageBounds(border));
            Bounds crestBounds = RenderedImageBounds(crestBackground);
            crestBounds.Encapsulate(RenderedImageBounds(crestInnerBorder));
            crestBounds.Encapsulate(RenderedImageBounds(crestBorder));
            Bounds badgeBounds = RenderedImageBounds(badgeDepth);
            badgeBounds.Encapsulate(RenderedImageBounds(badgeRim));
            badgeBounds.Encapsulate(RenderedImageBounds(badgeBackground));
            badgeBounds.Encapsulate(RenderedImageBounds(badgeBorder));
            Bounds outer = body;
            outer.Encapsulate(badgeBounds);
            Bounds fullVisible = outer;
            fullVisible.Encapsulate(crestBounds);
            Vector4 bodyScreen = ScreenBounds(body, safe);
            Vector4 badgeScreen = ScreenBounds(badgeBounds, safe);
            Vector4 outerScreen = ScreenBounds(outer, safe);
            Vector4 fullVisibleScreen = ScreenBounds(fullVisible, safe);
            Vector4 goldScreen = ScreenRect(gold.GetComponent<RectTransform>(), safe);
            Assert.That(bodyScreen.z / bodyScreen.w, Is.InRange(0.78f, 0.81f), "Profile chassis body aspect");
            Assert.That(badgeScreen.z / badgeScreen.w, Is.InRange(0.78f, 0.88f), "Profile chassis badge aspect");
            Assert.That(bodyScreen.y + bodyScreen.w - badgeScreen.y, Is.InRange(45f, 60f), "Profile chassis badge overlap");
            Assert.That(goldScreen.x - (bodyScreen.x + bodyScreen.z), Is.GreaterThanOrEqualTo(phoneFixture ? 30f : 25f), "Profile chassis body-to-Gold gap");
            Assert.That(outerScreen.x, Is.GreaterThanOrEqualTo(frameLeft - 1f));
            Assert.That(outerScreen.x + outerScreen.z, Is.LessThanOrEqualTo(frameLeft + frameWidth + 1f));
            Assert.That(outerScreen.y, Is.GreaterThanOrEqualTo(-1f));
            Assert.That(outerScreen.y + outerScreen.w, Is.LessThanOrEqualTo(safe.rect.height + 1f));
            Assert.That(fullVisibleScreen.x, Is.GreaterThanOrEqualTo(frameLeft - 1f));
            Assert.That(fullVisibleScreen.x + fullVisibleScreen.z, Is.LessThanOrEqualTo(frameLeft + frameWidth + 1f));
            Assert.That(fullVisibleScreen.y, Is.GreaterThanOrEqualTo(-1f));
            Assert.That(fullVisibleScreen.y + fullVisibleScreen.w, Is.LessThanOrEqualTo(safe.rect.height + 1f));
            if (phoneFixture)
            {
                Vector4 crestScreen = ScreenBounds(crestBounds, safe);
                Vector4 borderScreen = ScreenBounds(RenderedImageBounds(border), safe);
                Assert.That(crestScreen.x + crestScreen.z * 0.5f, Is.EqualTo(borderScreen.x + borderScreen.z * 0.5f).Within(1f), "Profile crest center");
                float crestProtrusion = Mathf.Max(0f, borderScreen.y - crestScreen.y);
                Assert.That(crestProtrusion, Is.InRange(18f, 24f), "Profile crest protrusion");
                float crestOverlap = Mathf.Max(0f, Mathf.Min(crestScreen.y + crestScreen.w, bodyScreen.y + bodyScreen.w) - Mathf.Max(crestScreen.y, bodyScreen.y));
                Assert.That(crestOverlap / crestScreen.w, Is.GreaterThanOrEqualTo(0.4f), "Profile crest body occlusion");
                Assert.That(outerScreen.x, Is.EqualTo(16f).Within(3f), "Profile chassis phone outer left");
                Assert.That(outerScreen.y, Is.EqualTo(33f).Within(3f), "Profile chassis phone outer top");
                Assert.That(outerScreen.z, Is.EqualTo(177f).Within(3f), "Profile chassis phone outer width");
                Assert.That(outerScreen.w, Is.EqualTo(265f).Within(3f), "Profile chassis phone outer height");
                Assert.That(bodyScreen.z, Is.InRange(174f, 180f), "Profile chassis phone body width");
                Assert.That(bodyScreen.w, Is.InRange(208f, 224f), "Profile chassis phone body height");
                Assert.That(badgeScreen.z, Is.InRange(78f, 84f), "Profile chassis phone badge width");
                Assert.That(badgeScreen.w, Is.InRange(92f, 100f), "Profile chassis phone badge height");
            }

            TMP_Text level = profile.Find("Content/LevelText").GetComponent<TMP_Text>();
            Vector4 levelScreen = ScreenRect(level.rectTransform, safe);
            Assert.That(levelScreen.x, Is.GreaterThan(badgeScreen.x + 2f));
            Assert.That(levelScreen.x + levelScreen.z, Is.LessThan(badgeScreen.x + badgeScreen.z - 2f));
            Assert.That(levelScreen.y, Is.GreaterThan(badgeScreen.y + 2f));
            Assert.That(levelScreen.y + levelScreen.w, Is.LessThan(badgeScreen.y + badgeScreen.w - 2f));
            Vector4 portraitScreen = ScreenRect(profile.Find("Content/Portrait").GetComponent<RectTransform>(), safe);
            Assert.That(portraitScreen.x, Is.GreaterThan(bodyScreen.x + 2f));
            Assert.That(portraitScreen.x + portraitScreen.z, Is.LessThan(bodyScreen.x + bodyScreen.z - 2f));
            Assert.That(portraitScreen.y, Is.GreaterThan(bodyScreen.y + 2f));
            Assert.That(portraitScreen.y + portraitScreen.w, Is.LessThan(bodyScreen.y + bodyScreen.w - 2f));
        }

        static Vector4 ScreenBounds(Bounds bounds, RectTransform safe)
        {
            Vector3 min = safe.InverseTransformPoint(bounds.min);
            Vector3 max = safe.InverseTransformPoint(bounds.max);
            return new Vector4(min.x + safe.rect.width * 0.5f, safe.rect.height * 0.5f - max.y, max.x - min.x, max.y - min.y);
        }

        static void AssertFoldOutputPixelProfileContainment(RectTransform authoredSafe, float outputWidth, float outputHeight)
        {
            float scale = Mathf.Min(outputWidth / 1080f, outputHeight / 1920f);
            using (TemporaryCanvasFixture fixture = TemporaryCanvasFixture.Create(authoredSafe, outputWidth, outputHeight, scale))
            {
                RectTransform safe = fixture.SafeArea;
                Transform profile = safe.Find("PersistentHeader/ProfileSlot");
                Transform header = safe.Find("PersistentHeader/Visual/BottomNavigationChassis");
                Transform legionPass = safe.Find("ContentViewport/LegionPassEntry");
                Transform gold = safe.Find("PersistentHeader/GoldCurrencySlot");
                safe.GetComponent<LobbyHomeReferenceLayout>().RefreshLayout();
                Canvas.ForceUpdateCanvases();
                AssertProfileRelationOffsetPreservesX(profile);
                Bounds visibleBounds = RenderedVisibleBounds(profile);
                Vector4 visible = OutputPixelBounds(visibleBounds, safe, scale);
                float frameWidth = outputHeight * (865f / 1819f);
                float frameLeft = (outputWidth - frameWidth) * 0.5f;
                const float profileBoundaryTolerance = 4f;

                Assert.That(fixture.Canvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceCamera));
                Assert.That(fixture.Canvas.worldCamera, Is.Not.Null);
                Assert.That(fixture.CanvasScaler.enabled, Is.False);
                Assert.That(fixture.Canvas.scaleFactor, Is.EqualTo(scale).Within(0.0001f));
                AssertProfileCompositeScale(profile, frameWidth / scale);
                AssertFabricProfileHeaderPassRelationsInOutputPixels(profile, header, legionPass, gold, safe, frameWidth, scale);

                Assert.That(visible.x, Is.GreaterThanOrEqualTo(frameLeft - profileBoundaryTolerance), "Fold output profile left");
                Assert.That(visible.x + visible.z, Is.LessThanOrEqualTo(frameLeft + frameWidth + profileBoundaryTolerance), "Fold output profile right");
                Assert.That(visible.y, Is.GreaterThanOrEqualTo(-profileBoundaryTolerance), "Fold output profile top");
                Assert.That(visible.y + visible.w, Is.LessThanOrEqualTo(outputHeight + profileBoundaryTolerance), "Fold output profile bottom");
            }
        }

        static void AssertFabricProfileHeaderPassRelationsInOutputPixels(Transform profile, Transform header, Transform legionPass, Transform gold, RectTransform safe, float outputFrameWidth, float outputScale)
        {
            Vector4 profileBounds = OutputPixelBounds(RenderedVisibleBounds(profile), safe, outputScale);
            Vector4 headerBounds = OutputPixelRect(header.GetComponent<RectTransform>(), safe, outputScale);
            Vector4 passBounds = OutputPixelRect(legionPass.GetComponent<RectTransform>(), safe, outputScale);
            Vector4 goldBounds = OutputPixelBounds(RenderedVisibleBounds(gold), safe, outputScale);
            float relationScale = outputFrameWidth / 865f;
            Assert.That(profileBounds.x + profileBounds.z - headerBounds.x, Is.EqualTo(4f * relationScale).Within(2f), "Fold output Profile/Header overlap");
            Assert.That(passBounds.y - (profileBounds.y + profileBounds.w), Is.EqualTo(-5f * relationScale).Within(2f), "Fold output Profile/Pass overlap");
            Assert.That(passBounds.y - (headerBounds.y + headerBounds.w), Is.EqualTo(44f * relationScale).Within(2f), "Fold output Header/Pass gap");
            Assert.That(goldBounds.x - (profileBounds.x + profileBounds.z), Is.GreaterThanOrEqualTo(12f), "Fold output Profile-Gold gap");
        }

        static Vector4 OutputPixelBounds(Bounds bounds, RectTransform safe, float scale)
        {
            Vector3 min = safe.InverseTransformPoint(bounds.min);
            Vector3 max = safe.InverseTransformPoint(bounds.max);
            return new Vector4(
                (min.x + safe.rect.width * 0.5f) * scale,
                (safe.rect.height * 0.5f - max.y) * scale,
                (max.x - min.x) * scale,
                (max.y - min.y) * scale);
        }

        static Vector4 OutputPixelRect(RectTransform target, RectTransform safe, float scale)
        {
            Vector4 rect = ScreenRect(target, safe);
            return new Vector4(rect.x * scale, rect.y * scale, rect.z * scale, rect.w * scale);
        }

        static float SafeAreaVerticalGap(RectTransform safe, RectTransform upper, RectTransform lower)
        {
            Vector3[] upperCorners = new Vector3[4];
            Vector3[] lowerCorners = new Vector3[4];
            upper.GetWorldCorners(upperCorners);
            lower.GetWorldCorners(lowerCorners);
            return safe.InverseTransformPoint(upperCorners[0]).y - safe.InverseTransformPoint(lowerCorners[1]).y;
        }

        static void AssertStartBattleFrameClearance(Transform cta, Transform battleFrame)
        {
            RectTransform safe = GetLayoutSafeArea(cta);
            Rect frame = GetExpectedContentFrame(safe);
            AssertReferenceFrameRect(cta, safe, frame.xMin + frame.width * (145f / 865f), -1f, frame.width * (573f / 865f), safe.rect.height * (254f / 1819f));
            Assert.That(SafeAreaVerticalGap(safe, cta.GetComponent<RectTransform>(), battleFrame.GetComponent<RectTransform>()), Is.EqualTo(safe.rect.height * (19f / 1819f)).Within(2f));
        }

        static void AssertProfileVisibleBounds(Transform profile, Transform gold, RectTransform safe, float frameLeft, float frameWidth, float minimumGoldGap, float frameBoundaryTolerance)
        {
            Bounds profileBounds = RenderedVisibleBounds(profile);
            Bounds goldBounds = RenderedVisibleBounds(gold);
            float profileLeft = safe.InverseTransformPoint(profileBounds.min).x + safe.rect.width * 0.5f;
            float profileRight = safe.InverseTransformPoint(profileBounds.max).x + safe.rect.width * 0.5f;
            float profileTop = safe.rect.height * 0.5f - safe.InverseTransformPoint(profileBounds.max).y;
            float profileBottom = safe.rect.height * 0.5f - safe.InverseTransformPoint(profileBounds.min).y;
            float goldLeft = safe.InverseTransformPoint(goldBounds.min).x + safe.rect.width * 0.5f;
            Assert.That(profileLeft, Is.GreaterThanOrEqualTo(frameLeft - frameBoundaryTolerance));
            Assert.That(profileRight, Is.LessThanOrEqualTo(frameLeft + frameWidth + frameBoundaryTolerance));
            Assert.That(profileTop, Is.GreaterThanOrEqualTo(-frameBoundaryTolerance));
            Assert.That(profileBottom, Is.LessThanOrEqualTo(safe.rect.height + frameBoundaryTolerance));
            Assert.That(goldLeft - profileRight, Is.GreaterThanOrEqualTo(minimumGoldGap), "Profile visible bounds must not overlap Gold.");
        }

        static Bounds DescendantBounds(Transform root)
        {
            RectTransform[] rects = root.GetComponentsInChildren<RectTransform>(true);
            Bounds bounds = WorldBounds(rects[0]);
            for (int i = 1; i < rects.Length; i++) bounds.Encapsulate(WorldBounds(rects[i]));
            return bounds;
        }

        static Bounds RenderedVisibleBounds(Transform root)
        {
            Canvas.ForceUpdateCanvases();
            bool hasBounds = false;
            Bounds bounds = default;

            foreach (Image image in root.GetComponentsInChildren<Image>(true))
            {
                if (!image.isActiveAndEnabled || image.sprite == null || image.color.a <= 0f) continue;
                Encapsulate(RenderedImageBounds(image));
            }

            foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (!text.isActiveAndEnabled || string.IsNullOrEmpty(text.text) || text.color.a <= 0f) continue;
                Encapsulate(WorldBounds(text.rectTransform));
            }

            Assert.That(hasBounds, Is.True, $"{root.name} requires an active rendered Graphic.");
            return bounds;

            void Encapsulate(Bounds renderedBounds)
            {
                if (hasBounds) bounds.Encapsulate(renderedBounds);
                else
                {
                    bounds = renderedBounds;
                    hasBounds = true;
                }
            }
        }

        static Bounds RenderedImageBounds(Image image)
        {
            Rect rect = image.rectTransform.rect;
            float spriteAspect = image.sprite.rect.width / image.sprite.rect.height;
            float width = rect.width;
            float height = rect.height;
            if (image.preserveAspect)
            {
                if (width / height > spriteAspect)
                    width = height * spriteAspect;
                else
                    height = width / spriteAspect;
            }

            Vector2 center = rect.center;
            Transform transform = image.rectTransform;
            Bounds bounds = new Bounds(transform.TransformPoint(new Vector3(center.x - width * 0.5f, center.y - height * 0.5f)), Vector3.zero);
            bounds.Encapsulate(transform.TransformPoint(new Vector3(center.x + width * 0.5f, center.y + height * 0.5f)));
            return bounds;
        }

        static Bounds RenderedTextBounds(TMP_Text text)
        {
            text.ForceMeshUpdate();
            Bounds localBounds = text.textBounds;
            Transform transform = text.rectTransform;
            Bounds bounds = new Bounds(transform.TransformPoint(localBounds.min), Vector3.zero);
            bounds.Encapsulate(transform.TransformPoint(localBounds.max));
            return bounds;
        }

        static void AssertLevelTextWithinBadge(Transform profile, TMP_Text levelText, RectTransform safe)
        {
            Image badgeBorder = profile.Find("Visual/LevelBadge/LevelBadgeFace/Border").GetComponent<Image>();
            Vector4 badge = ScreenBounds(RenderedImageBounds(badgeBorder), safe);
            Vector4 text = ScreenBounds(RenderedTextBounds(levelText), safe);
            Assert.That(text.x, Is.GreaterThanOrEqualTo(badge.x + 2f), "LevelText left padding");
            Assert.That(text.x + text.z, Is.LessThanOrEqualTo(badge.x + badge.z - 2f), "LevelText right padding");
            Assert.That(text.y, Is.GreaterThanOrEqualTo(badge.y + 2f), "LevelText top padding");
            Assert.That(text.y + text.w, Is.LessThanOrEqualTo(badge.y + badge.w - 2f), "LevelText bottom padding");
        }

        static void AssertReferenceFrameSquare(Transform target, RectTransform safe, float centerX, float centerY, float side)
        {
            Vector4 actual = ScreenRect(target.GetComponent<RectTransform>(), safe);
            Assert.That(actual.z, Is.EqualTo(side).Within(2f), target.name + ".width");
            Assert.That(actual.w, Is.EqualTo(side).Within(2f), target.name + ".height");
            Assert.That(Mathf.Abs(actual.z - actual.w), Is.LessThan(0.5f), target.name + ".square");
            Assert.That(actual.x + actual.z * 0.5f, Is.EqualTo(centerX).Within(2f), target.name + ".centerX");
            Assert.That(actual.y + actual.w * 0.5f, Is.EqualTo(centerY).Within(2f), target.name + ".centerY");
        }

        static void AssertWithin(RectTransform safe, params Transform[] targets)
        {
            Bounds safeBounds = WorldBounds(safe);
            foreach (Transform target in targets)
            {
                Bounds bounds = WorldBounds(target.GetComponent<RectTransform>());
                Assert.That(bounds.min.x, Is.GreaterThanOrEqualTo(safeBounds.min.x - 1f), target.name + ".left");
                Assert.That(bounds.max.x, Is.LessThanOrEqualTo(safeBounds.max.x + 1f), target.name + ".right");
                Assert.That(bounds.min.y, Is.GreaterThanOrEqualTo(safeBounds.min.y - 1f), target.name + ".bottom");
                Assert.That(bounds.max.y, Is.LessThanOrEqualTo(safeBounds.max.y + 1f), target.name + ".top");
            }
        }

        static void AssertAuthoredSafeAreaStretch(RectTransform safe)
        {
            Assert.That(safe.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(safe.anchorMax, Is.EqualTo(Vector2.one));
            Assert.That(safe.offsetMin, Is.EqualTo(Vector2.zero));
            Assert.That(safe.offsetMax, Is.EqualTo(Vector2.zero));
        }

        sealed class TemporarySafeAreaFixture : System.IDisposable
        {
            readonly Scene _temporaryScene;

            public RectTransform SafeArea { get; }

            TemporarySafeAreaFixture(Scene temporaryScene, RectTransform safeArea)
            {
                _temporaryScene = temporaryScene;
                SafeArea = safeArea;
            }

            public static TemporarySafeAreaFixture Create(RectTransform authoredSafe, float width, float height)
            {
                Scene authoredScene = authoredSafe.gameObject.scene;
                bool authoredSceneWasDirty = authoredScene.isDirty;
                Scene temporaryScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                try
                {
                    GameObject clone = Object.Instantiate(authoredSafe.gameObject);
                    SceneManager.MoveGameObjectToScene(clone, temporaryScene);
                    Assert.That(authoredScene.isDirty, Is.EqualTo(authoredSceneWasDirty), "Authored Lobby scene dirtied by fixture clone.");
                    clone.name = "LobbyResponsiveFixture";
                    RectTransform safe = clone.GetComponent<RectTransform>();
                    safe.anchorMin = safe.anchorMax = safe.pivot = Vector2.one * 0.5f;
                    safe.anchoredPosition = Vector2.zero;
                    safe.sizeDelta = new Vector2(width, height);
                    LobbyHomeReferenceLayout layout = safe.GetComponent<LobbyHomeReferenceLayout>();
                    Assert.That(layout, Is.Not.Null);
                    layout.RefreshLayout();
                    Canvas.ForceUpdateCanvases();
                    return new TemporarySafeAreaFixture(temporaryScene, safe);
                }
                catch
                {
                    EditorSceneManager.CloseScene(temporaryScene, true);
                    throw;
                }
            }

            public void Dispose()
            {
                if (_temporaryScene.IsValid())
                    EditorSceneManager.CloseScene(_temporaryScene, true);
            }
        }

        sealed class TemporaryCanvasFixture : System.IDisposable
        {
            readonly Scene _temporaryScene;

            public Canvas Canvas { get; }
            public CanvasScaler CanvasScaler { get; }
            public RectTransform SafeArea { get; }

            TemporaryCanvasFixture(Scene temporaryScene, Canvas canvas, CanvasScaler canvasScaler, RectTransform safeArea)
            {
                _temporaryScene = temporaryScene;
                Canvas = canvas;
                CanvasScaler = canvasScaler;
                SafeArea = safeArea;
            }

            public static TemporaryCanvasFixture Create(RectTransform authoredSafe, float outputWidth, float outputHeight, float outputScale)
            {
                Canvas authoredCanvas = authoredSafe.GetComponentInParent<Canvas>()?.rootCanvas;
                Assert.That(authoredCanvas, Is.Not.Null);
                Scene authoredScene = authoredSafe.gameObject.scene;
                bool authoredSceneWasDirty = authoredScene.isDirty;
                Scene temporaryScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                try
                {
                    GameObject clone = Object.Instantiate(authoredCanvas.gameObject);
                    SceneManager.MoveGameObjectToScene(clone, temporaryScene);
                    Assert.That(authoredScene.isDirty, Is.EqualTo(authoredSceneWasDirty), "Authored Lobby scene dirtied by canvas fixture clone.");
                    clone.name = "LobbyCanvasResponsiveFixture";
                    Canvas canvas = clone.GetComponent<Canvas>();
                    CanvasScaler canvasScaler = clone.GetComponent<CanvasScaler>();
                    Assert.That(canvas, Is.Not.Null);
                    Assert.That(canvasScaler, Is.Not.Null);
                    canvasScaler.enabled = false;
                    canvas.scaleFactor = outputScale;

                    RectTransform safe = clone.transform.Find("SafeArea").GetComponent<RectTransform>();
                    safe.anchorMin = safe.anchorMax = safe.pivot = Vector2.one * 0.5f;
                    safe.anchoredPosition = Vector2.zero;
                    safe.sizeDelta = new Vector2(outputWidth / outputScale, outputHeight / outputScale);
                    LobbyHomeReferenceLayout layout = safe.GetComponent<LobbyHomeReferenceLayout>();
                    Assert.That(layout, Is.Not.Null);
                    layout.RefreshLayout();
                    UnityEngine.Canvas.ForceUpdateCanvases();
                    return new TemporaryCanvasFixture(temporaryScene, canvas, canvasScaler, safe);
                }
                catch
                {
                    EditorSceneManager.CloseScene(temporaryScene, true);
                    throw;
                }
            }

            public void Dispose()
            {
                if (_temporaryScene.IsValid())
                    EditorSceneManager.CloseScene(_temporaryScene, true);
            }
        }

        readonly struct RectTransformState
        {
            readonly Vector2 _anchorMin;
            readonly Vector2 _anchorMax;
            readonly Vector2 _pivot;
            readonly Vector2 _sizeDelta;
            readonly Vector2 _anchoredPosition;

            public RectTransformState(RectTransform rect)
            {
                _anchorMin = rect.anchorMin;
                _anchorMax = rect.anchorMax;
                _pivot = rect.pivot;
                _sizeDelta = rect.sizeDelta;
                _anchoredPosition = rect.anchoredPosition;
            }

            public void AssertUnchanged(RectTransform rect, string label)
            {
                Assert.That(rect.anchorMin, Is.EqualTo(_anchorMin), label + ".anchorMin");
                Assert.That(rect.anchorMax, Is.EqualTo(_anchorMax), label + ".anchorMax");
                Assert.That(rect.pivot, Is.EqualTo(_pivot), label + ".pivot");
                Assert.That(rect.sizeDelta, Is.EqualTo(_sizeDelta), label + ".sizeDelta");
                Assert.That(rect.anchoredPosition, Is.EqualTo(_anchoredPosition), label + ".anchoredPosition");
            }
        }

        static void AssertReferenceSquare(Transform target, Vector2 normalizedCenter, float normalizedHeightSide)
        {
            RectTransform rect = target.GetComponent<RectTransform>();
            RectTransform safe = GetLayoutSafeArea(target);
            Rect frame = GetExpectedContentFrame(safe);
            Vector4 actual = ScreenRect(rect, safe);
            float side = safe.rect.height * normalizedHeightSide;
            Assert.That(actual.z, Is.EqualTo(side).Within(2f), target.name + ".width");
            Assert.That(actual.w, Is.EqualTo(side).Within(2f), target.name + ".height");
            Assert.That(Mathf.Abs(actual.z - actual.w), Is.LessThan(0.5f), target.name + ".square");
            Assert.That(actual.x + actual.z * 0.5f, Is.EqualTo(frame.xMin + frame.width * normalizedCenter.x).Within(2f), target.name + ".centerX");
            Assert.That(actual.y + actual.w * 0.5f, Is.EqualTo(safe.rect.height * normalizedCenter.y).Within(2f), target.name + ".centerY");
        }

        static void AssertReferenceCircle(Transform target, Vector2 normalizedCenter, float normalizedHeightDiameter) => AssertReferenceSquare(target, normalizedCenter, normalizedHeightDiameter);

        static void AssertCirclePanelOverlap(Transform circle, Transform panel, float expectedOverlap)
        {
            RectTransform safe = GetLayoutSafeArea(panel);
            Vector4 circleScreen = ScreenRect(circle.GetComponent<RectTransform>(), safe);
            Vector4 panelScreen = ScreenRect(panel.GetComponent<RectTransform>(), safe);
            Assert.That(circleScreen.y + circleScreen.w - panelScreen.y, Is.EqualTo(expectedOverlap).Within(2f), panel.name + ".overlap");
        }

        static void AssertPanelText(Transform panel, string textPath, float topFraction)
        {
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            TMP_Text text = panel.Find(textPath).GetComponent<TMP_Text>();
            RectTransform textRect = text.GetComponent<RectTransform>();
            Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(panel, textRect);
            Assert.That(bounds.min.x, Is.GreaterThan(panelRect.rect.xMin + 2f));
            Assert.That(bounds.max.x, Is.LessThan(panelRect.rect.xMax - 2f));
            Assert.That(bounds.min.y, Is.GreaterThan(panelRect.rect.yMin + 2f));
            Assert.That(bounds.max.y, Is.LessThan(panelRect.rect.yMax - 2f));
            Assert.That(text.preferredHeight, Is.LessThanOrEqualTo(textRect.rect.height));
            Assert.That((panelRect.rect.yMax - bounds.center.y) / panelRect.rect.height, Is.EqualTo(topFraction).Within(0.03f));
        }

        static void AssertReferenceResourceRail(RectTransform slot, Sprite expectedRailSprite, string label)
        {
            Transform visual = slot.Find("Visual/BottomNavigationChassis");
            Image rail = visual.Find("Bg").GetComponent<Image>();
            RectTransform icon = slot.Find("Content/Icon").GetComponent<RectTransform>();
            TMP_Text value = slot.Find("Content/ValueText").GetComponent<TMP_Text>();
            RectTransform add = slot.Find("Content/NativeAddButton").GetComponent<RectTransform>();
            Image addRail = add.Find("BottomNavigationChassis/Bg").GetComponent<Image>();
            TMP_Text addLabel = slot.Find("Content/AddSlot").GetComponent<TMP_Text>();
            RectTransform safe = GetLayoutSafeArea(slot);

            Assert.That(rail.sprite, Is.SameAs(expectedRailSprite), label + ".railSprite");
            Assert.That(rail.type, Is.EqualTo(Image.Type.Sliced), label + ".railType");
            Assert.That(rail.color, Is.EqualTo(new Color(0f, 0.2f, 0.4f, 0.8f)), label + ".railColor");
            Assert.That(rail.raycastTarget, Is.False, label + ".railRaycast");
            Assert.That(addRail.sprite, Is.SameAs(expectedRailSprite), label + ".addRailSprite");
            Assert.That(addRail.type, Is.EqualTo(Image.Type.Sliced), label + ".addRailType");
            Assert.That(addRail.color, Is.EqualTo(new Color(0f, 0.2f, 0.4f, 0.8f)), label + ".addRailColor");
            Assert.That(addRail.raycastTarget, Is.False, label + ".addRailRaycast");
            Assert.That(value.color.a, Is.GreaterThan(0.99f), label + ".valueAlpha");
            Assert.That(addLabel.text, Is.EqualTo("+"), label + ".addText");
            Assert.That(addLabel.gameObject.activeInHierarchy, Is.True, label + ".addActive");
            Assert.That(addLabel.rectTransform.GetSiblingIndex(), Is.GreaterThan(add.GetSiblingIndex()), label + ".addRenderOrder");

            Vector4 railBounds = ScreenBounds(RenderedImageBounds(rail), safe);
            Vector4 iconBounds = ScreenRect(icon, safe);
            Vector4 valueBounds = ScreenBounds(RenderedTextBounds(value), safe);
            Vector4 addBounds = ScreenRect(add, safe);
            Assert.That(railBounds.y + railBounds.w * 0.5f, Is.EqualTo(iconBounds.y + iconBounds.w * 0.5f).Within(1f), label + ".baseline");
            Assert.That(iconBounds.x + iconBounds.z, Is.LessThanOrEqualTo(valueBounds.x + 1f), label + ".iconValueOrder");
            Assert.That(valueBounds.x + valueBounds.z, Is.LessThanOrEqualTo(addBounds.x + 1f), label + ".valueAddOrder");
        }

        static void AssertResourceRailEndCapsAreOccluded(RectTransform slot, string label)
        {
            Transform visual = slot.Find("Visual");
            Transform content = slot.Find("Content");
            Image rail = visual.Find("BottomNavigationChassis/Bg").GetComponent<Image>();
            Image icon = content.Find("Icon").GetComponent<Image>();
            Image addChassis = content.Find("NativeAddButton/BottomNavigationChassis/Bg").GetComponent<Image>();
            TMP_Text value = content.Find("ValueText").GetComponent<TMP_Text>();
            RectTransform safe = GetLayoutSafeArea(slot);

            Assert.That(visual.GetSiblingIndex(), Is.LessThan(content.GetSiblingIndex()), label + ".railBeforeForeground");

            Vector4 railBounds = ScreenBounds(RenderedImageBounds(rail), safe);
            Vector4 iconBounds = ScreenBounds(RenderedImageBounds(icon), safe);
            Vector4 addBounds = ScreenBounds(RenderedImageBounds(addChassis), safe);
            Vector4 valueBounds = ScreenBounds(RenderedTextBounds(value), safe);

            Assert.That(railBounds.x, Is.GreaterThanOrEqualTo(iconBounds.x).And.LessThanOrEqualTo(iconBounds.x + iconBounds.z), label + ".leftEndUnderIcon");
            Assert.That(railBounds.x + railBounds.z, Is.GreaterThanOrEqualTo(addBounds.x).And.LessThanOrEqualTo(addBounds.x + addBounds.z), label + ".rightEndUnderAdd");
            Assert.That(railBounds.y, Is.GreaterThanOrEqualTo(iconBounds.y).And.GreaterThanOrEqualTo(addBounds.y), label + ".endCapsTopCovered");
            Assert.That(railBounds.y + railBounds.w, Is.LessThanOrEqualTo(iconBounds.y + iconBounds.w).And.LessThanOrEqualTo(addBounds.y + addBounds.w), label + ".endCapsBottomCovered");
            Assert.That(valueBounds.x, Is.GreaterThanOrEqualTo(iconBounds.x + iconBounds.z - 1f), label + ".valueClearsIcon");
            Assert.That(valueBounds.x + valueBounds.z, Is.LessThanOrEqualTo(addBounds.x + 1f), label + ".valueClearsAdd");
        }

        static void AssertResourceTextStress(Canvas canvas, RectTransform safe, string slotName, string value)
        {
            Transform slot = safe.Find("PersistentHeader/" + slotName);
            TMP_Text valueText = slot.Find("Content/ValueText").GetComponent<TMP_Text>();
            Assert.That(canvas, Is.Not.Null, slotName + ".stressCanvas");
            Assert.That(canvas.isActiveAndEnabled, Is.True, slotName + ".stressCanvasActive");
            valueText.text = value;
            Canvas.ForceUpdateCanvases();
            valueText.ForceMeshUpdate();
            Canvas.ForceUpdateCanvases();

            Vector4 iconBounds = ScreenRect(slot.Find("Content/Icon").GetComponent<RectTransform>(), safe);
            Vector4 valueBounds = ScreenBounds(RenderedVisibleBounds(valueText.transform), safe);
            Vector4 addBounds = ScreenRect(slot.Find("Content/NativeAddButton").GetComponent<RectTransform>(), safe);
            Assert.That(valueText.preferredWidth, Is.LessThanOrEqualTo(valueText.rectTransform.rect.width + 0.1f), slotName + ".stressPreferredWidth");
            Assert.That(valueBounds.x, Is.GreaterThanOrEqualTo(iconBounds.x + iconBounds.z - 1f), slotName + ".stressIconGap");
            Assert.That(valueBounds.x + valueBounds.z, Is.LessThanOrEqualTo(addBounds.x + 1f), slotName + ".stressAddGap");
        }

        static Vector4 ScreenRect(RectTransform rect, RectTransform canvas)
        {
            Vector3 center = canvas.InverseTransformPoint(rect.TransformPoint(rect.rect.center));
            return new Vector4(center.x + canvas.rect.width * 0.5f - rect.rect.width * 0.5f, canvas.rect.height * 0.5f - center.y - rect.rect.height * 0.5f, rect.rect.width, rect.rect.height);
        }

        static void AssertReferenceRect(Transform target, Vector2 expectedSize, Vector2 expectedCenter)
        {
            Assert.That(target, Is.Not.Null);
            RectTransform rect = target.GetComponent<RectTransform>();
            Assert.That(rect, Is.Not.Null);
            Assert.That(rect.rect.width, Is.EqualTo(expectedSize.x).Within(2f), target.name + ".width");
            Assert.That(rect.rect.height, Is.EqualTo(expectedSize.y).Within(2f), target.name + ".height");
            RectTransform canvas = target.GetComponentInParent<Canvas>()?.GetComponent<RectTransform>();
            Assert.That(canvas, Is.Not.Null, target.name);
            Vector3 canvasLocalCenter = canvas.InverseTransformPoint(rect.TransformPoint(rect.rect.center));
            Vector2 screenCenter = new Vector2(canvasLocalCenter.x + canvas.rect.width * 0.5f, canvas.rect.height * 0.5f - canvasLocalCenter.y);
            Assert.That(screenCenter.x, Is.EqualTo(expectedCenter.x).Within(3f), target.name + ".centerX");
            Assert.That(screenCenter.y, Is.EqualTo(expectedCenter.y).Within(3f), target.name + ".centerY");
        }

        static void AssertBottomNavigationChassis(Transform target, Transform source)
        {
            Assert.That(target, Is.Not.Null);
            Assert.That(source, Is.Not.Null);
            Assert.That(target.name, Is.EqualTo("BottomNavigationChassis"));
            Assert.That(target.GetComponent<LobbyBottomNavigationView>(), Is.Null);
            Assert.That(target.GetComponentsInChildren<Button>(true), Is.Empty);
            Assert.That(target.childCount, Is.EqualTo(source.childCount));
            Image[] targetImages = target.GetComponentsInChildren<Image>(true);
            Image[] sourceImages = source.GetComponentsInChildren<Image>(true);
            Assert.That(targetImages, Has.Length.EqualTo(sourceImages.Length));
            for (int i = 0; i < sourceImages.Length; i++)
            {
                Assert.That(targetImages[i].sprite, Is.SameAs(sourceImages[i].sprite), sourceImages[i].name);
                Assert.That(targetImages[i].type, Is.EqualTo(sourceImages[i].type), sourceImages[i].name);
                Assert.That(targetImages[i].raycastTarget, Is.False, target.name + "/" + targetImages[i].name);
            }
            for (int i = 0; i < source.childCount; i++) Assert.That(target.Find(source.GetChild(i).name), Is.Not.Null, source.GetChild(i).name);
            foreach (Graphic graphic in target.GetComponentsInChildren<Graphic>(true)) Assert.That(graphic.raycastTarget, Is.False, target.name + "/" + graphic.name);
        }
        static Scene OpenLobby() => LobbySceneTestContext.OpenLobby();

        static Transform Find(Scene scene, string path) => LobbySceneTestContext.Find(scene, path);
        static void AssertPopupVisual(Transform root) => LobbySceneTestContext.AssertPopupVisual(root, false);
        static void AssertWithin(Transform parent, Transform child) => LobbySceneTestContext.AssertWithin(parent, child);
        static void AssertNoOverlap(Transform first, Transform second) => LobbySceneTestContext.AssertNoOverlap(first, second);
        static Bounds WorldBounds(RectTransform rect) => LobbySceneTestContext.WorldBounds(rect);
        static void AssertSection(Transform shop, string name, Vector2 size, string title)
        {
            Transform section = shop.Find(name);
            Assert.That(section, Is.Not.Null, name);
            Assert.That(section.GetComponent<RectTransform>().rect.size, Is.EqualTo(size).Within(0.01f), name);
            Assert.That(section.Find("Visual"), Is.Not.Null, name);
            Assert.That(section.Find("Content"), Is.Not.Null, name);
            if (title != null)
            {
                Assert.That(section.Find("Content/TitleText").GetComponent<TMP_Text>().text, Is.EqualTo(title), name);
            }
        }
        static void AssertCard(Transform card, Vector2 size)
        {
            Assert.That(card, Is.Not.Null);
            Assert.That(card.GetComponent<RectTransform>().rect.size, Is.EqualTo(size).Within(0.01f));
            Assert.That(card.Find("Visual"), Is.Not.Null);
            Assert.That(card.Find("Content"), Is.Not.Null);
        }
        static void AssertGrid(Transform section, string prefix, int count, Vector2 size)
        {
            for (int i = 0;
            i < count;
            i++)
            {
                Transform card = section.Find(prefix + i.ToString("00"));
                AssertCard(card, size);
                Assert.That(card.Find("Content/IconPlaceholder"), Is.Not.Null, card.name);
                Assert.That(card.Find("Content/TitleText"), Is.Not.Null, card.name);
                Assert.That(card.Find("Content/ValueText"), Is.Not.Null, card.name);
                Assert.That(card.Find("Content/PriceText"), Is.Not.Null, card.name);
            }
        }
    }
}
