using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Lizzo.PV.UI;
using Lizzo.PV.Legion;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class UI_PauseOverlayDynamicSynergyTests
    {
        [Test]
        public void PauseOverlayHasNoScrollRectDependency()
        {
            Assert.IsNull(typeof(UI_PauseOverlay).GetField("_infoScroll", BindingFlags.Instance | BindingFlags.NonPublic));
        }

        [Test]
        public void CompletedSynergyIdsExposeGuardSquadWithoutParsingSummary()
        {
            using (ServiceTestFixture fixture = new ServiceTestFixture())
            {
                FieldInfo state = typeof(PartyService).GetField(
                    "GuardSquadActivatedState",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(state);
                state.SetValue(fixture.Run.Party, true);

                List<string> ids = new List<string>();
                fixture.Run.Party.FillCompletedSynergyIds(ids);

                CollectionAssert.AreEqual(new[] { "guard_squad" }, ids);
                Assert.AreEqual("근위대", fixture.Run.Party.GetCompletedSynergySummary());
            }
        }

        [Test]
        public void CompletedSynergyIdsFillCallerOwnedListWithoutExposingInternalStorage()
        {
            using (ServiceTestFixture fixture = new ServiceTestFixture())
            {
                FieldInfo state = typeof(PartyService).GetField(
                    "GuardSquadActivatedState",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(state);
                state.SetValue(fixture.Run.Party, true);

                List<string> ids = new List<string> { "stale" };
                fixture.Run.Party.FillCompletedSynergyIds(ids);
                CollectionAssert.AreEqual(new[] { "guard_squad" }, ids);

                ids.Add("caller_owned");
                fixture.Run.Party.FillCompletedSynergyIds(ids);
                CollectionAssert.AreEqual(new[] { "guard_squad" }, ids);
            }
        }

        [Test]
        public void PauseSynergyItemRejectsRaycastOnNameText()
        {
            GameObject root = new GameObject("SynergyItemTestRoot");
            GameObject textObject = new GameObject(
                "NameText",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            textObject.transform.SetParent(root.transform, false);
            UI_PauseSynergyItem item = root.AddComponent<UI_PauseSynergyItem>();
            TMP_Text nameText = textObject.GetComponent<TMP_Text>();
            SetField(item, "_nameText", nameText);
            nameText.raycastTarget = true;

            try
            {
                LogAssert.Expect(
                    LogType.Error,
                    new Regex("\\[UI_PauseSynergyItem\\] Decorative graphics must not raycast\\."));
                Assert.IsFalse(item.Validate());
                nameText.raycastTarget = false;
                Assert.IsTrue(item.Validate());
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void PauseOverlayReusesSynergyItemsAndKeepsSevenFiveRosterContract()
        {
            GameObject root = new GameObject("PauseOverlayTestRoot");
            GameObject infoContentObject = new GameObject("InfoContent", typeof(RectTransform));
            GameObject synergySectionObject = new GameObject("SynergySection", typeof(RectTransform));
            GameObject synergyBodyObject = new GameObject("SynergyBody", typeof(RectTransform));
            GameObject synergyListObject = new GameObject("SynergyList", typeof(RectTransform));
            infoContentObject.transform.SetParent(root.transform, false);
            synergySectionObject.transform.SetParent(infoContentObject.transform, false);
            synergyBodyObject.transform.SetParent(synergySectionObject.transform, false);
            synergyListObject.transform.SetParent(synergyBodyObject.transform, false);

            UI_PauseOverlay overlay = root.AddComponent<UI_PauseOverlay>();
            CanvasGroup canvasGroup = root.AddComponent<CanvasGroup>();
            SetField(overlay, "_root", root);
            SetField(overlay, "_rootCanvasGroup", canvasGroup);
            SetField(overlay, "_titleText", CreateText(root.transform, "TitleText"));
            SetField(overlay, "_companionCountText", CreateText(root.transform, "CompanionCountText"));
            SetField(overlay, "_passiveCountText", CreateText(root.transform, "PassiveCountText"));
            GameObject[] companionRoots = CreateRoots(root.transform, 7, "CompanionSlot_");
            GameObject[] companionFilledRoots = CreateChildRoots(companionRoots, "Normal");
            GameObject[] companionEmptyRoots = CreateChildRoots(companionRoots, "Disable");
            SetField(overlay, "_companionSlotRoots", companionRoots);
            SetField(overlay, "_companionFilledSlotRoots", companionFilledRoots);
            SetField(overlay, "_companionEmptySlotRoots", companionEmptyRoots);
            SetField(overlay, "_companionSlotCountTexts", CreateTexts(root.transform, 7, "CompanionCount_"));
            SetField(overlay, "_companionIconImages", CreateImages(root.transform, 7, "CompanionIcon_"));
            GameObject[] passiveRoots = CreateRoots(root.transform, 5, "PassiveSlot_");
            GameObject[] passiveFilledRoots = CreateChildRoots(passiveRoots, "Nomal");
            GameObject[] passiveEmptyRoots = CreateChildRoots(passiveRoots, "Lock");
            SetField(overlay, "_passiveSlotRoots", passiveRoots);
            SetField(overlay, "_passiveFilledSlotRoots", passiveFilledRoots);
            SetField(overlay, "_passiveEmptySlotRoots", passiveEmptyRoots);
            SetField(overlay, "_passiveIconImages", CreateImages(root.transform, 5, "PassiveIcon_"));
            SetField(overlay, "_passiveLevelTexts", CreateTexts(root.transform, 5, "PassiveLevel_"));
            SetField(overlay, "_synergyEmptyStateText", CreateText(root.transform, "EmptyStateText"));
            SetField(overlay, "_synergyList", synergyListObject.GetComponent<RectTransform>());
            SetField(overlay, "_synergyItemPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_LizzoPV/Prefabs/UI/UI_PauseSynergyItem.prefab"));
            SetField(overlay, "_lobbyButton", CreateButton(root.transform, "LobbyButton"));
            SetField(overlay, "_resumeButton", CreateButton(root.transform, "ResumeButton"));

            Assert.IsTrue(overlay.Configure(() => { }, () => { }));
            Assert.IsTrue(overlay.Init());

            Sprite rosterSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
            try
            {
                overlay.Present(
                    false,
                    CreateCompanionPresentations(rosterSprite, 7),
                    CreatePassivePresentations(rosterSprite, 5),
                    new[]
                    {
                        new PauseSynergyPresentation("guard_squad", "근위대"),
                        new PauseSynergyPresentation("future_one", "미래 시너지 1"),
                        new PauseSynergyPresentation("future_two", "미래 시너지 2"),
                        new PauseSynergyPresentation("future_three", "미래 시너지 3"),
                        new PauseSynergyPresentation("future_four", "미래 시너지 4"),
                        new PauseSynergyPresentation("future_five", "미래 시너지 5"),
                        new PauseSynergyPresentation("future_six", "미래 시너지 6"),
                        new PauseSynergyPresentation("future_seven", "미래 시너지 7"),
                    });

                Assert.AreEqual(8, synergyListObject.transform.childCount);
                Assert.IsTrue(synergyListObject.transform.Cast<Transform>().All(child => child.gameObject.activeSelf));
                Assert.AreEqual("근위대", synergyListObject.transform.GetChild(0).GetComponentInChildren<TMP_Text>().text);
                Assert.AreEqual("미래 시너지 7", synergyListObject.transform.GetChild(7).GetComponentInChildren<TMP_Text>().text);
                Assert.IsFalse(GetField<TMP_Text>(overlay, "_synergyEmptyStateText").gameObject.activeSelf);

                overlay.Present(
                    false,
                    CreateCompanionPresentations(rosterSprite, 7),
                    CreatePassivePresentations(rosterSprite, 5),
                    new[] { new PauseSynergyPresentation("guard_squad", "근위대") });

                Assert.AreEqual(8, synergyListObject.transform.childCount);
                Assert.IsTrue(synergyListObject.transform.GetChild(0).gameObject.activeSelf);
                Assert.IsFalse(synergyListObject.transform.GetChild(1).gameObject.activeSelf);
                Assert.IsFalse(synergyListObject.transform.GetChild(2).gameObject.activeSelf);
                Assert.IsFalse(GetField<TMP_Text>(overlay, "_synergyEmptyStateText").gameObject.activeSelf);
                Assert.AreEqual("동료 7 / 7", GetField<TMP_Text>(overlay, "_companionCountText").text);
                Assert.AreEqual("패시브 5 / 5", GetField<TMP_Text>(overlay, "_passiveCountText").text);
                Assert.AreEqual("Lv.1", GetField<TMP_Text[]>(overlay, "_passiveLevelTexts")[0].text);
            }
            finally
            {
                Object.DestroyImmediate(rosterSprite);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void PauseOverlayKeepsCapacityRootsAndBindsCompanionCountsAndEmptySynergyState()
        {
            GameObject root = new GameObject("PauseOverlayCapacityTestRoot");
            GameObject infoContentObject = new GameObject("InfoContent", typeof(RectTransform));
            GameObject synergySectionObject = new GameObject("SynergySection", typeof(RectTransform));
            GameObject synergyBodyObject = new GameObject("SynergyBody", typeof(RectTransform));
            GameObject synergyListObject = new GameObject("SynergyList", typeof(RectTransform));
            infoContentObject.transform.SetParent(root.transform, false);
            synergySectionObject.transform.SetParent(infoContentObject.transform, false);
            synergyBodyObject.transform.SetParent(synergySectionObject.transform, false);
            synergyListObject.transform.SetParent(synergyBodyObject.transform, false);

            UI_PauseOverlay overlay = root.AddComponent<UI_PauseOverlay>();
            CanvasGroup canvasGroup = root.AddComponent<CanvasGroup>();
            SetField(overlay, "_root", root);
            SetField(overlay, "_rootCanvasGroup", canvasGroup);
            SetField(overlay, "_titleText", CreateText(root.transform, "TitleText"));
            SetField(overlay, "_companionCountText", CreateText(root.transform, "CompanionCountText"));
            SetField(overlay, "_passiveCountText", CreateText(root.transform, "PassiveCountText"));
            GameObject[] companionRoots = CreateRoots(root.transform, 7, "CompanionSlot_");
            GameObject[] companionFilledRoots = CreateChildRoots(companionRoots, "Normal");
            GameObject[] companionEmptyRoots = CreateChildRoots(companionRoots, "Disable");
            SetField(overlay, "_companionSlotRoots", companionRoots);
            SetField(overlay, "_companionFilledSlotRoots", companionFilledRoots);
            SetField(overlay, "_companionEmptySlotRoots", companionEmptyRoots);
            SetField(overlay, "_companionSlotCountTexts", CreateTexts(root.transform, 7, "CompanionCount_"));
            SetField(overlay, "_companionIconImages", CreateImages(root.transform, 7, "CompanionIcon_"));
            GameObject[] passiveRoots = CreateRoots(root.transform, 5, "PassiveSlot_");
            GameObject[] passiveFilledRoots = CreateChildRoots(passiveRoots, "Nomal");
            GameObject[] passiveEmptyRoots = CreateChildRoots(passiveRoots, "Lock");
            SetField(overlay, "_passiveSlotRoots", passiveRoots);
            SetField(overlay, "_passiveFilledSlotRoots", passiveFilledRoots);
            SetField(overlay, "_passiveEmptySlotRoots", passiveEmptyRoots);
            SetField(overlay, "_passiveIconImages", CreateImages(root.transform, 5, "PassiveIcon_"));
            SetField(overlay, "_passiveLevelTexts", CreateTexts(root.transform, 5, "PassiveLevel_"));
            SetField(overlay, "_synergyEmptyStateText", CreateText(root.transform, "EmptyStateText"));
            SetField(overlay, "_synergyList", synergyListObject.GetComponent<RectTransform>());
            SetField(overlay, "_synergyItemPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_LizzoPV/Prefabs/UI/UI_PauseSynergyItem.prefab"));
            SetField(overlay, "_lobbyButton", CreateButton(root.transform, "LobbyButton"));
            SetField(overlay, "_resumeButton", CreateButton(root.transform, "ResumeButton"));

            Assert.IsTrue(overlay.Configure(() => { }, () => { }));
            Assert.IsTrue(overlay.Init());

            Sprite rosterSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
            try
            {
                overlay.Present(
                    false,
                    new[]
                    {
                        new PauseCompanionPresentation(rosterSprite, 2),
                        new PauseCompanionPresentation(rosterSprite, 1),
                        new PauseCompanionPresentation(null, 0),
                        new PauseCompanionPresentation(null, 0),
                        new PauseCompanionPresentation(null, 0),
                        new PauseCompanionPresentation(null, 0),
                        new PauseCompanionPresentation(null, 0),
                    },
                    CreatePassivePresentations(rosterSprite, 2),
                    System.Array.Empty<PauseSynergyPresentation>());

                Assert.IsTrue(companionRoots.All(r => r.activeSelf));
                Assert.IsTrue(companionEmptyRoots[0].activeSelf == false);
                Assert.IsTrue(companionEmptyRoots[2].activeSelf);
                Assert.AreEqual("동료 2 / 7", GetField<TMP_Text>(overlay, "_companionCountText").text);
                Assert.AreEqual("×2", GetField<TMP_Text[]>(overlay, "_companionSlotCountTexts")[0].text);
                Assert.AreEqual("×1", GetField<TMP_Text[]>(overlay, "_companionSlotCountTexts")[1].text);
                Assert.IsFalse(GetField<TMP_Text[]>(overlay, "_companionSlotCountTexts")[2].gameObject.activeSelf);
                Assert.IsTrue(passiveRoots.All(r => r.activeSelf));
                Assert.IsFalse(passiveEmptyRoots[0].activeSelf);
                Assert.IsTrue(passiveEmptyRoots[2].activeSelf);
                Assert.AreEqual("패시브 2 / 5", GetField<TMP_Text>(overlay, "_passiveCountText").text);
                Assert.IsTrue(GetField<TMP_Text>(overlay, "_synergyEmptyStateText").gameObject.activeSelf);
                Assert.AreEqual("활성 시너지 없음", GetField<TMP_Text>(overlay, "_synergyEmptyStateText").text);

                overlay.Present(
                    false,
                    new[]
                    {
                        new PauseCompanionPresentation(null, 3),
                    },
                    new[] { new PausePassivePresentation(null, 2) },
                    System.Array.Empty<PauseSynergyPresentation>());

                Assert.AreEqual("동료 1 / 7", GetField<TMP_Text>(overlay, "_companionCountText").text);
                Assert.IsTrue(companionFilledRoots[0].activeSelf);
                Assert.IsFalse(GetField<Image[]>(overlay, "_companionIconImages")[0].enabled);
                Assert.AreEqual("패시브 1 / 5", GetField<TMP_Text>(overlay, "_passiveCountText").text);
                Assert.IsTrue(passiveFilledRoots[0].activeSelf);
                Assert.IsFalse(GetField<Image[]>(overlay, "_passiveIconImages")[0].enabled);
                Assert.IsFalse(passiveEmptyRoots[0].activeSelf);
                Assert.IsTrue(GetField<TMP_Text[]>(overlay, "_passiveLevelTexts")[0].gameObject.activeSelf);
                Assert.AreEqual("Lv.2", GetField<TMP_Text[]>(overlay, "_passiveLevelTexts")[0].text);
            }
            finally
            {
                Object.DestroyImmediate(rosterSprite);
                Object.DestroyImmediate(root);
            }
        }

        private static TMP_Text CreateText(Transform parent, string name)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            return textObject.GetComponent<TMP_Text>();
        }

        private static PauseCompanionPresentation[] CreateCompanionPresentations(Sprite icon, int count)
        {
            PauseCompanionPresentation[] presentations = new PauseCompanionPresentation[count];
            for (int i = 0; i < count; i++)
                presentations[i] = new PauseCompanionPresentation(icon, 1);
            return presentations;
        }

        private static PausePassivePresentation[] CreatePassivePresentations(Sprite icon, int count)
        {
            PausePassivePresentation[] presentations = new PausePassivePresentation[count];
            for (int i = 0; i < count; i++)
                presentations[i] = new PausePassivePresentation(icon, 1);
            return presentations;
        }

        private static TMP_Text[] CreateTexts(Transform parent, int count, string prefix)
        {
            TMP_Text[] texts = new TMP_Text[count];
            for (int i = 0; i < count; i++)
                texts[i] = CreateText(parent, prefix + i);
            return texts;
        }

        private static GameObject[] CreateRoots(Transform parent, int count, string prefix)
        {
            GameObject[] roots = new GameObject[count];
            for (int i = 0; i < count; i++)
            {
                roots[i] = new GameObject(prefix + i, typeof(RectTransform));
                roots[i].transform.SetParent(parent, false);
            }

            return roots;
        }

        private static GameObject[] CreateChildRoots(GameObject[] parents, string name)
        {
            GameObject[] roots = new GameObject[parents.Length];
            for (int i = 0; i < parents.Length; i++)
            {
                roots[i] = new GameObject(name, typeof(RectTransform));
                roots[i].transform.SetParent(parents[i].transform, false);
            }

            return roots;
        }

        private static Image[] CreateImages(Transform parent, int count, string prefix)
        {
            Image[] images = new Image[count];
            for (int i = 0; i < count; i++)
            {
                GameObject imageObject = new GameObject(prefix + i, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                imageObject.transform.SetParent(parent, false);
                images[i] = imageObject.GetComponent<Image>();
            }

            return images;
        }

        private static Button CreateButton(Transform parent, string name)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            return buttonObject.GetComponent<Button>();
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, "Missing field: " + name);
            field.SetValue(target, value);
        }

        private static T GetField<T>(object target, string name) where T : class
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, "Missing field: " + name);
            return field.GetValue(target) as T;
        }
    }
}
