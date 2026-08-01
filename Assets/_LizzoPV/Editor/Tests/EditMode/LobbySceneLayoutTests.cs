using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Lizzo.PV.EditorTests;
using Lizzo.PV.UI;

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
            Assert.That(headerRect.anchorMin.x, Is.EqualTo(0f).Within(0.001f));
            Assert.That(headerRect.anchorMin.y, Is.EqualTo(1f).Within(0.001f));
            Assert.That(headerRect.anchorMax.x, Is.EqualTo(1f).Within(0.001f));
            Assert.That(headerRect.anchorMax.y, Is.EqualTo(1f).Within(0.001f));
            Assert.That(headerRect.rect.height, Is.EqualTo(192f).Within(0.01f));
            Assert.That(headerRect.anchoredPosition.y, Is.EqualTo(-96f).Within(0.01f));

            for (int i = 0;
            i < SlotNames.Length;
            i++)
            {
                Transform slot = header.Find(SlotNames[i]);
                Assert.That(slot, Is.Not.Null, SlotNames[i]);
                RectTransform slotRect = slot.GetComponent<RectTransform>();
                Assert.That(slotRect.localPosition.z, Is.EqualTo(0f).Within(0.001f), SlotNames[i]);
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
            Assert.That(gold.rect.width, Is.EqualTo(gem.rect.width).Within(0.001f));
            Assert.That(gold.rect.height, Is.EqualTo(gem.rect.height).Within(0.001f));

            RectTransform profile = header.Find("ProfileSlot").GetComponent<RectTransform>();
            RectTransform settings = header.Find("SettingsSlot").GetComponent<RectTransform>();
            Assert.That(Mathf.Abs(profile.rect.width - profile.rect.height), Is.LessThan(1f));
            Assert.That(settings.rect.width, Is.EqualTo(128f).Within(0.01f));
            Assert.That(settings.rect.height, Is.EqualTo(112f).Within(0.01f));

            RectTransform canvasRect = header.GetComponentInParent<Canvas>()?.GetComponent<RectTransform>();
            Assert.That(canvasRect, Is.Not.Null, "Header must be under a Canvas.");
            Bounds headerBounds = WorldBounds(headerRect);
            Bounds canvasBounds = WorldBounds(canvasRect);
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
                Assert.That(bounds.min.x, Is.GreaterThanOrEqualTo(headerBounds.min.x - 0.5f), SlotNames[i]);
                Assert.That(bounds.max.x, Is.LessThanOrEqualTo(headerBounds.max.x + 0.5f), SlotNames[i]);
                Assert.That(bounds.min.y, Is.GreaterThanOrEqualTo(headerBounds.min.y - 0.5f), SlotNames[i]);
                Assert.That(bounds.max.y, Is.LessThanOrEqualTo(headerBounds.max.y + 0.5f), SlotNames[i]);
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
            Assert.That(slotBounds[0].max.x, Is.LessThanOrEqualTo(slotBounds[1].min.x + 0.5f));
            Assert.That(slotBounds[1].max.x, Is.LessThanOrEqualTo(slotBounds[2].min.x + 0.5f));
            Assert.That(slotBounds[2].max.x, Is.LessThanOrEqualTo(slotBounds[3].min.x + 0.5f));

            Assert.That(header.Find("ProfileSlot/Content/Portrait").GetComponent<Image>().sprite, Is.Null);
            Assert.That(header.Find("ProfileSlot/Content/LevelText").GetComponent<TMP_Text>().text, Is.EqualTo("Lv.1"));
            Assert.That(header.Find("GoldCurrencySlot/Content/Icon").GetComponent<Image>().sprite, Is.Null);
            Assert.That(header.Find("GoldCurrencySlot/Content/ValueText").GetComponent<TMP_Text>().text, Is.EqualTo("0"));
            Assert.That(header.Find("GoldCurrencySlot/Content/AddSlot").GetComponent<TMP_Text>().text, Is.EqualTo("+"));
            Assert.That(header.Find("GemCurrencySlot/Content/Icon").GetComponent<Image>().sprite, Is.Null);
            Assert.That(header.Find("GemCurrencySlot/Content/ValueText").GetComponent<TMP_Text>().text, Is.EqualTo("0"));
            Assert.That(header.Find("SettingsSlot/Content/Icon").GetComponent<Image>().sprite, Is.Null);

            Assert.That(header.Find("Title").gameObject.activeSelf, Is.False);
            Assert.That(header.Find("HeaderCaption").gameObject.activeSelf, Is.False);
            Assert.That(header.Find("HeaderStatus").gameObject.activeSelf, Is.False);
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
