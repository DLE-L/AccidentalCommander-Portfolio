using System;
using System.Reflection;
using Lizzo.PV.Lobby;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Lizzo.PV.EditorTests
{
    public sealed class LobbyNavigationControllerTests
    {
        private const string LobbyScenePath = "Assets/_LizzoPV/Scenes/Lobby.unity";

        [Test]
        public void Configure_UsesDepartureAsTheOnlySelectableM1Section()
        {
            using Fixture fixture = new Fixture();
            LobbySection? lockedRequest = null;
            fixture.Controller.LockedSectionRequested += section => lockedRequest = section;

            Assert.IsTrue(fixture.Controller.Configure());
            Assert.AreEqual(LobbySection.Departure, fixture.Controller.CurrentSection);
            Assert.IsTrue(fixture.Shop.Button.interactable);
            Assert.IsTrue(fixture.Legion.Button.interactable);
            Assert.IsTrue(fixture.Departure.Button.interactable);
            Assert.IsTrue(fixture.Commander.Button.interactable);
            Assert.IsTrue(fixture.Challenge.Button.interactable);
            Assert.IsTrue(fixture.Shop.IsLocked);
            Assert.IsTrue(fixture.Legion.IsLocked);
            Assert.IsFalse(fixture.Departure.IsLocked);
            Assert.IsTrue(fixture.Commander.IsLocked);
            Assert.IsTrue(fixture.Challenge.IsLocked);
            Assert.IsTrue(fixture.DepartureScreen.activeSelf);
            Assert.IsFalse(fixture.LegionScreen.activeSelf);

            Assert.IsFalse(fixture.Controller.Select(LobbySection.Shop));
            Assert.AreEqual(LobbySection.Shop, lockedRequest);
            fixture.Legion.Button.onClick.Invoke();
            Assert.AreEqual(LobbySection.Legion, lockedRequest);
            Assert.IsFalse(fixture.Controller.Select(LobbySection.Commander));
            Assert.AreEqual(LobbySection.Commander, lockedRequest);
            Assert.IsFalse(fixture.Controller.Select(LobbySection.Challenge));
            Assert.AreEqual(LobbySection.Challenge, lockedRequest);
            Assert.AreEqual(LobbySection.Departure, fixture.Controller.CurrentSection);
            Assert.AreEqual(0f, fixture.Overlay.anchoredPosition.x);
            Assert.IsTrue(fixture.DepartureScreen.activeSelf);
            Assert.IsFalse(fixture.LegionScreen.activeSelf);
        }

        [Test]
        public void LobbyScene_BindsApprovedFiveSectionIdentityInProductionOrder()
        {
            Scene scene = EditorSceneManager.OpenScene(LobbyScenePath, OpenSceneMode.Additive);
            try
            {
                LobbyNavigationController controller = FindController(scene);
                AssertItem(controller.ShopButton, "ShopButton", "상점", "UI_Shop_01_Red", true);
                AssertItem(controller.LegionButton, "LegionButton", "군단", "Gear_Helmet_04", true);
                AssertItem(controller.DepartureButton, "DepartureButton", "출정", "UI_Play_Battle_01_Color", false);
                AssertItem(
                    controller.CommanderButton,
                    "CommanderButton",
                    "군단장",
                    "Item_Book_03_Red",
                    true);
                AssertItem(controller.ChallengeButton, "ChallengeButton", "도전", "UI_Play_Dungeon_01", true);

                float shopX = ((RectTransform)controller.ShopButton.transform).anchoredPosition.x;
                float legionX = ((RectTransform)controller.LegionButton.transform).anchoredPosition.x;
                float lobbyX = ((RectTransform)controller.DepartureButton.transform).anchoredPosition.x;
                float commanderStatsX = ((RectTransform)controller.CommanderButton.transform).anchoredPosition.x;
                float challengeX = ((RectTransform)controller.ChallengeButton.transform).anchoredPosition.x;
                Assert.That(shopX, Is.LessThan(legionX));
                Assert.That(legionX, Is.LessThan(lobbyX));
                Assert.That(lobbyX, Is.LessThan(commanderStatsX));
                Assert.That(commanderStatsX, Is.LessThan(challengeX));

                Assert.IsTrue(controller.Configure());
                Assert.AreEqual(LobbySection.Departure, controller.CurrentSection);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static LobbyNavigationController FindController(Scene scene)
        {
            LobbyNavigationController found = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (LobbyNavigationController candidate in
                         root.GetComponentsInChildren<LobbyNavigationController>(true))
                {
                    Assert.IsNull(found, "Lobby scene must contain exactly one navigation controller.");
                    found = candidate;
                }
            }

            Assert.IsNotNull(found, "Lobby scene must contain a navigation controller.");
            return found;
        }

        private static void AssertItem(
            LobbyNavigationItemView item,
            string objectName,
            string label,
            string spriteName,
            bool locked)
        {
            Assert.IsNotNull(item);
            Assert.AreEqual(objectName, item.gameObject.name);
            Assert.AreEqual(locked, item.IsLocked);

            TMP_Text labelText = item.transform.Find("Content/Label").GetComponent<TMP_Text>();
            Image icon = item.transform.Find("Content/Icon").GetComponent<Image>();
            Assert.AreEqual(label, labelText.text);
            Assert.IsNotNull(icon.sprite);
            Assert.AreEqual(spriteName, icon.sprite.name);
        }

        private sealed class Fixture : IDisposable
        {
            public readonly GameObject Root;
            public readonly LobbyNavigationController Controller;
            public readonly LobbyNavigationItemView Shop;
            public readonly LobbyNavigationItemView Legion;
            public readonly LobbyNavigationItemView Departure;
            public readonly LobbyNavigationItemView Commander;
            public readonly LobbyNavigationItemView Challenge;
            public readonly RectTransform Overlay;
            public readonly GameObject LegionScreen;
            public readonly GameObject DepartureScreen;

            public Fixture()
            {
                Root = new GameObject("LobbyNavigationFixture", typeof(RectTransform));
                Root.SetActive(false);
                Controller = Root.AddComponent<LobbyNavigationController>();
                Shop = CreateItem("ShopButton", Root.transform, true);
                Legion = CreateItem("LegionButton", Root.transform, false);
                Departure = CreateItem("DepartureButton", Root.transform, false);
                Commander = CreateItem("CommanderButton", Root.transform, true);
                Challenge = CreateItem("ChallengeButton", Root.transform, true);
                Overlay = CreateRect("ActiveTabOverlay", Root.transform);
                LegionScreen = new GameObject("LegionScreen");
                DepartureScreen = new GameObject("LobbyScreen");
                LegionScreen.transform.SetParent(Root.transform, false);
                DepartureScreen.transform.SetParent(Root.transform, false);

                SetField(Controller, "_shopButton", Shop);
                SetField(Controller, "_legionButton", Legion);
                SetField(Controller, "_departureButton", Departure);
                SetField(Controller, "_commanderButton", Commander);
                SetField(Controller, "_challengeButton", Challenge);
                SetField(Controller, "_activeTabOverlay", Overlay);
                SetField(Controller, "_legionScreen", LegionScreen);
                SetField(Controller, "_departureScreen", DepartureScreen);
                Root.SetActive(true);
            }

            public void Dispose()
            {
                UnityEngine.Object.DestroyImmediate(Root);
            }

            private static LobbyNavigationItemView CreateItem(string name, Transform parent, bool locked)
            {
                RectTransform root = CreateRect(name, parent);
                Image interactionTarget = root.gameObject.AddComponent<Image>();
                Button button = root.gameObject.AddComponent<Button>();
                button.targetGraphic = interactionTarget;
                RectTransform content = CreateRect("Content", root);
                Image icon = CreateRect("Icon", content).gameObject.AddComponent<Image>();
                TMP_Text label = CreateRect("Label", content).gameObject.AddComponent<TextMeshProUGUI>();
                GameObject lockBadge = CreateRect("LockBadge", content).gameObject;
                LobbyNavigationItemView view = root.gameObject.AddComponent<LobbyNavigationItemView>();
                SetField(view, "_button", button);
                SetField(view, "_interactionTarget", interactionTarget);
                SetField(view, "_contentRoot", content);
                SetField(view, "_icon", icon);
                SetField(view, "_label", label);
                SetField(view, "_lockBadge", lockBadge);
                SetField(view, "_locked", locked);
                return view;
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
