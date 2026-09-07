using System;
using System.Collections.Generic;
using System.Reflection;
using Lizzo.PV.Gameplay.Pause;
using Lizzo.PV.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lizzo.PV.EditorTests
{
    public sealed class GameplayPausePresentationTests
    {
        [Test]
        public void Present_BindsBuildSummaryAndClearsStaleSlotState()
        {
            using PauseFixture fixture = new PauseFixture();
            Sprite icon = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
            try
            {
                Assert.IsTrue(fixture.Controller.Present(
                    fromAppBackground: true,
                    CreateCompanions(icon, 7),
                    CreatePassives(icon, 5),
                    new[] { new PauseSynergyPresentation("guard_corps", "방패 군단") }));

                Assert.AreEqual("일시정지", fixture.Title.text);
                Assert.AreEqual("동료 7 / 7", fixture.CompanionCount.text);
                Assert.AreEqual("패시브 5 / 5", fixture.PassiveCount.text);
                Assert.IsTrue(fixture.CompanionSlots[0].Filled.activeSelf);
                Assert.AreEqual("×1", fixture.CompanionSlots[0].Count.text);
                Assert.IsTrue(fixture.PassiveSlots[0].Filled.activeSelf);
                Assert.AreEqual("Lv.1", fixture.PassiveSlots[0].Level.text);
                Assert.AreEqual(1, fixture.SynergyList.childCount);
                Assert.AreEqual("방패 군단", fixture.SynergyList.GetChild(0).GetComponentInChildren<TMP_Text>().text);

                Assert.IsTrue(fixture.Controller.Present(
                    fromAppBackground: false,
                    new[] { new PauseCompanionPresentation(null, 2) },
                    Array.Empty<PausePassivePresentation>(),
                    Array.Empty<PauseSynergyPresentation>()));

                Assert.AreEqual("일시정지", fixture.Title.text);
                Assert.AreEqual("동료 1 / 7", fixture.CompanionCount.text);
                Assert.AreEqual("패시브 0 / 5", fixture.PassiveCount.text);
                Assert.IsFalse(fixture.CompanionSlots[0].Icon.enabled);
                Assert.AreEqual("×2", fixture.CompanionSlots[0].Count.text);
                Assert.IsTrue(fixture.CompanionSlots[1].Empty.activeSelf);
                Assert.IsFalse(fixture.PassiveSlots[0].Filled.activeSelf);
                Assert.IsFalse(fixture.PassiveSlots[0].Icon.enabled);
                Assert.AreEqual(string.Empty, fixture.PassiveSlots[0].Level.text);
                Assert.IsTrue(fixture.EmptyState.activeSelf);
                Assert.AreEqual("활성 시너지 없음", fixture.EmptyStateText.text);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(icon);
            }
        }

        [Test]
        public void Present_CapsAndReusesOrderedSynergyItems()
        {
            using PauseFixture fixture = new PauseFixture();
            PauseSynergyPresentation[] many = new PauseSynergyPresentation[10];
            for (int index = 0; index < many.Length; index++)
                many[index] = new PauseSynergyPresentation("synergy_" + index, "시너지 " + index);

            Assert.IsTrue(fixture.Controller.Present(false, CreateCompanions(null, 9), CreatePassives(null, 7), many));
            Assert.AreEqual("동료 7 / 7", fixture.CompanionCount.text);
            Assert.AreEqual("패시브 5 / 5", fixture.PassiveCount.text);
            Assert.AreEqual(8, fixture.SynergyList.childCount);
            Assert.AreEqual("시너지 0", fixture.SynergyList.GetChild(0).GetComponentInChildren<TMP_Text>().text);
            Assert.AreEqual("시너지 7", fixture.SynergyList.GetChild(7).GetComponentInChildren<TMP_Text>().text);

            Transform first = fixture.SynergyList.GetChild(0);
            Assert.IsTrue(fixture.Controller.Present(false, Array.Empty<PauseCompanionPresentation>(), Array.Empty<PausePassivePresentation>(),
                new[] { new PauseSynergyPresentation("mixed_command", "혼성 지휘") }));
            Assert.AreEqual(8, fixture.SynergyList.childCount);
            Assert.AreSame(first, fixture.SynergyList.GetChild(0));
            Assert.AreEqual("혼성 지휘", first.GetComponentInChildren<TMP_Text>().text);
            Assert.IsTrue(first.gameObject.activeSelf);
            Assert.IsFalse(fixture.SynergyList.GetChild(1).gameObject.activeSelf);
        }

        [Test]
        public void PresentHideAndButtons_ForwardEventsOnce()
        {
            using PauseFixture fixture = new PauseFixture();
            int resumeCount = 0;
            int abandonCount = 0;
            fixture.Controller.ResumeRequested += () => resumeCount++;
            fixture.Controller.AbandonRequested += () => abandonCount++;

            Assert.IsTrue(fixture.Controller.Present(false, Array.Empty<PauseCompanionPresentation>(), Array.Empty<PausePassivePresentation>(), Array.Empty<PauseSynergyPresentation>()));
            Assert.IsTrue(fixture.Controller.Present(false, Array.Empty<PauseCompanionPresentation>(), Array.Empty<PausePassivePresentation>(), Array.Empty<PauseSynergyPresentation>()));
            Assert.AreEqual(1f, fixture.CanvasGroup.alpha);
            Assert.IsTrue(fixture.CanvasGroup.interactable);
            Assert.IsTrue(fixture.CanvasGroup.blocksRaycasts);

            fixture.ResumeButton.onClick.Invoke();
            fixture.AbandonButton.onClick.Invoke();
            Assert.AreEqual(1, resumeCount);
            Assert.AreEqual(1, abandonCount);

            fixture.Controller.Hide();
            Assert.AreEqual(0f, fixture.CanvasGroup.alpha);
            Assert.IsFalse(fixture.CanvasGroup.interactable);
            Assert.IsFalse(fixture.CanvasGroup.blocksRaycasts);
            Assert.IsTrue(fixture.Root.activeSelf);
        }

        private static PauseCompanionPresentation[] CreateCompanions(Sprite icon, int count)
        {
            PauseCompanionPresentation[] items = new PauseCompanionPresentation[count];
            for (int index = 0; index < items.Length; index++)
                items[index] = new PauseCompanionPresentation(icon, index + 1);
            return items;
        }

        private static PausePassivePresentation[] CreatePassives(Sprite icon, int count)
        {
            PausePassivePresentation[] items = new PausePassivePresentation[count];
            for (int index = 0; index < items.Length; index++)
                items[index] = new PausePassivePresentation(icon, index + 1);
            return items;
        }

        private sealed class PauseFixture : IDisposable
        {
            public readonly GameObject Root;
            public readonly GameplayPauseController Controller;
            public readonly CanvasGroup CanvasGroup;
            public readonly TMP_Text Title;
            public readonly TMP_Text CompanionCount;
            public readonly TMP_Text PassiveCount;
            public readonly SlotState[] CompanionSlots;
            public readonly SlotState[] PassiveSlots;
            public readonly Transform SynergyList;
            public readonly GameObject EmptyState;
            public readonly TMP_Text EmptyStateText;
            public readonly Button AbandonButton;
            public readonly Button ResumeButton;

            public PauseFixture()
            {
                Root = CreateRect("Pause").gameObject;
                CanvasGroup = Root.AddComponent<CanvasGroup>();
                GameplayPauseView view = Root.AddComponent<GameplayPauseView>();
                Controller = Root.AddComponent<GameplayPauseController>();

                RectTransform blocker = CreateRect("ModalInputBlocker", Root.transform);
                Image blockerImage = blocker.gameObject.AddComponent<Image>();
                blockerImage.raycastTarget = true;
                RectTransform header = CreateRect("Header", Root.transform);
                RectTransform headerContent = CreateRect("Content", header);
                Title = CreateText("TitleText", headerContent);
                RectTransform buildSummary = CreateRect("BuildSummary", Root.transform);
                RectTransform actions = CreateRect("Actions", Root.transform);

                RectTransform companions = CreateSection(buildSummary, "Companions", out CompanionCount);
                CompanionSlots = CreateCompanionSlots(companions);
                RectTransform passives = CreateSection(buildSummary, "Passives", out PassiveCount);
                PassiveSlots = CreatePassiveSlots(passives);
                RectTransform synergies = CreateSection(buildSummary, "CompletedSynergies", out TMP_Text ignoredTitle);
                SynergyList = CreateRect("CompletedSynergyList", synergies).transform;
                EmptyState = CreateRect("EmptyStateText", synergies).gameObject;
                EmptyStateText = EmptyState.AddComponent<TextMeshProUGUI>();
                EmptyStateText.raycastTarget = false;

                AbandonButton = CreateButton("AbandonButton", actions);
                ResumeButton = CreateButton("ResumeButton", actions);
                GameplayPauseSynergyItemView prefab = CreateSynergyItemPrefab();
                prefab.transform.SetParent(Root.transform, false);
                prefab.gameObject.SetActive(false);

                SetField(Controller, "_view", view);
                SetField(view, "_canvasGroup", CanvasGroup);
                SetField(view, "_modalInputBlocker", blocker);
                SetField(view, "_header", header);
                SetField(view, "_buildSummary", buildSummary);
                SetField(view, "_actions", actions);
                SetField(view, "_titleText", Title);
                SetField(view, "_companionCountText", CompanionCount);
                SetField(view, "_passiveCountText", PassiveCount);
                SetField(view, "_companionSlots", ExtractCompanionViews(CompanionSlots));
                SetField(view, "_passiveSlots", ExtractPassiveViews(PassiveSlots));
                SetField(view, "_completedSynergyList", SynergyList.GetComponent<RectTransform>());
                SetField(view, "_synergyItemPrefab", prefab);
                SetField(view, "_emptyState", EmptyState.GetComponent<RectTransform>());
                SetField(view, "_emptyStateText", EmptyStateText);
                SetField(view, "_abandonButton", AbandonButton);
                SetField(view, "_resumeButton", ResumeButton);
            }

            public void Dispose()
            {
                UnityEngine.Object.DestroyImmediate(Root);
            }

            private static RectTransform CreateSection(Transform parent, string name, out TMP_Text countText)
            {
                RectTransform section = CreateRect(name, parent);
                RectTransform content = CreateRect("Content", section);
                countText = CreateText("CountText", content);
                return content;
            }

            private static SlotState[] CreateCompanionSlots(Transform parent)
            {
                RectTransform slots = CreateRect("Slots", parent);
                SlotState[] states = new SlotState[7];
                for (int index = 0; index < states.Length; index++)
                {
                    RectTransform root = CreateRect("CompanionSlot" + (index + 1).ToString("00"), slots);
                    RectTransform visual = CreateRect("Visual", root);
                    RectTransform content = CreateRect("Content", root);
                    GameObject empty = CreateRect("EmptyState", content).gameObject;
                    GameObject filled = CreateRect("FilledState", content).gameObject;
                    Image icon = CreateRect("Icon", filled.transform).gameObject.AddComponent<Image>();
                    icon.raycastTarget = false;
                    TMP_Text count = CreateText("CountText", filled.transform);
                    GameplayPauseCompanionSlotView view = root.gameObject.AddComponent<GameplayPauseCompanionSlotView>();
                    SetField(view, "_visual", visual);
                    SetField(view, "_content", content);
                    SetField(view, "_emptyState", empty);
                    SetField(view, "_filledState", filled);
                    SetField(view, "_icon", icon);
                    SetField(view, "_countText", count);
                    states[index] = new SlotState(empty, filled, icon, count, view, null);
                }

                return states;
            }

            private static SlotState[] CreatePassiveSlots(Transform parent)
            {
                RectTransform slots = CreateRect("Slots", parent);
                SlotState[] states = new SlotState[5];
                for (int index = 0; index < states.Length; index++)
                {
                    RectTransform root = CreateRect("PassiveSlot" + (index + 1).ToString("00"), slots);
                    RectTransform visual = CreateRect("Visual", root);
                    RectTransform content = CreateRect("Content", root);
                    GameObject empty = CreateRect("EmptyState", content).gameObject;
                    GameObject filled = CreateRect("FilledState", content).gameObject;
                    Image icon = CreateRect("Icon", filled.transform).gameObject.AddComponent<Image>();
                    icon.raycastTarget = false;
                    TMP_Text level = CreateText("LevelText", filled.transform);
                    GameplayPausePassiveSlotView view = root.gameObject.AddComponent<GameplayPausePassiveSlotView>();
                    SetField(view, "_visual", visual);
                    SetField(view, "_content", content);
                    SetField(view, "_emptyState", empty);
                    SetField(view, "_filledState", filled);
                    SetField(view, "_icon", icon);
                    SetField(view, "_levelText", level);
                    states[index] = new SlotState(empty, filled, icon, level, null, view);
                }

                return states;
            }

            private static GameplayPauseSynergyItemView CreateSynergyItemPrefab()
            {
                RectTransform root = CreateRect("GameplayPauseSynergyItem");
                CreateRect("Visual", root);
                RectTransform content = CreateRect("Content", root);
                Image icon = CreateRect("Icon", content).gameObject.AddComponent<Image>();
                icon.raycastTarget = false;
                TMP_Text nameText = CreateText("NameText", content);
                GameplayPauseSynergyItemView view = root.gameObject.AddComponent<GameplayPauseSynergyItemView>();
                SetField(view, "_visual", root.Find("Visual").GetComponent<RectTransform>());
                SetField(view, "_content", content);
                SetField(view, "_icon", icon);
                SetField(view, "_nameText", nameText);
                return view;
            }

            private static Button CreateButton(string name, Transform parent)
            {
                RectTransform root = CreateRect(name, parent);
                Image image = root.gameObject.AddComponent<Image>();
                image.raycastTarget = true;
                Button button = root.gameObject.AddComponent<Button>();
                button.targetGraphic = image;
                return button;
            }

            private static TMP_Text CreateText(string name, Transform parent)
            {
                TextMeshProUGUI text = CreateRect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
                text.raycastTarget = false;
                return text;
            }

            private static RectTransform CreateRect(string name, Transform parent = null)
            {
                GameObject item = new GameObject(name, typeof(RectTransform));
                if (parent != null)
                    item.transform.SetParent(parent, false);
                return item.GetComponent<RectTransform>();
            }

            private static GameplayPauseCompanionSlotView[] ExtractCompanionViews(SlotState[] states)
            {
                GameplayPauseCompanionSlotView[] views = new GameplayPauseCompanionSlotView[states.Length];
                for (int index = 0; index < states.Length; index++)
                    views[index] = states[index].CompanionView;
                return views;
            }

            private static GameplayPausePassiveSlotView[] ExtractPassiveViews(SlotState[] states)
            {
                GameplayPausePassiveSlotView[] views = new GameplayPausePassiveSlotView[states.Length];
                for (int index = 0; index < states.Length; index++)
                    views[index] = states[index].PassiveView;
                return views;
            }

            private static void SetField(object target, string fieldName, object value)
            {
                FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
                if (field == null)
                    throw new MissingFieldException(target.GetType().Name, fieldName);
                field.SetValue(target, value);
            }
        }

        private readonly struct SlotState
        {
            public SlotState(GameObject empty, GameObject filled, Image icon, TMP_Text value, GameplayPauseCompanionSlotView companionView, GameplayPausePassiveSlotView passiveView)
            {
                Empty = empty;
                Filled = filled;
                Icon = icon;
                Value = value;
                CompanionView = companionView;
                PassiveView = passiveView;
            }

            public GameObject Empty { get; }
            public GameObject Filled { get; }
            public Image Icon { get; }
            public TMP_Text Value { get; }
            public TMP_Text Count => Value;
            public TMP_Text Level => Value;
            public GameplayPauseCompanionSlotView CompanionView { get; }
            public GameplayPausePassiveSlotView PassiveView { get; }
        }
    }
}
