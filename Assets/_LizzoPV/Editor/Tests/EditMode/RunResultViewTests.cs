using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Lizzo.PV.UI;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Lizzo.PV.Tests.EditMode
{
    /// <summary>
    /// Current public Clear/Failure result presentation and live Scene binding contracts.
    /// </summary>
    public sealed class RunResultViewTests
    {
        const string GameplayScenePath = "Assets/_LizzoPV/Scenes/Gameplay.unity";

        [Test]
        public void ResultSnapshot_CopiesCanonicalCollectionsAndExposesCurrentShape()
        {
            Assert.That(typeof(RunResultViewData).GetProperty("CompanionPresentations"), Is.Not.Null);
            Assert.That(typeof(RunResultViewData).GetProperty("PassivePresentations"), Is.Not.Null);
            Assert.That(typeof(RunResultViewData).GetProperty("SynergyPresentations"), Is.Not.Null);
            Assert.That(typeof(RunResultViewData).GetProperty("BestActiveSynergy"), Is.Not.Null);
            Assert.That(typeof(RunResultViewData).GetProperty("HasCompletedSynergy"), Is.Not.Null);
            Assert.That(typeof(RunResultViewData).GetProperty("SquadSlots"), Is.Not.Null);

            List<PauseCompanionPresentation> companions = new List<PauseCompanionPresentation>
            {
                new PauseCompanionPresentation(null, 2)
            };
            List<PausePassivePresentation> passives = new List<PausePassivePresentation>
            {
                new PausePassivePresentation(null, 3)
            };
            List<PauseSynergyPresentation> synergies = new List<PauseSynergyPresentation>
            {
                new PauseSynergyPresentation("future_one", "미래 시너지")
            };
            RunResultViewData view = new RunResultViewData(
                false, "실패", "", "1-1", "", "재도전", false, "", 1f, 1, 1,
                "", "", "", 0, false, "", "", "", "",
                Array.Empty<int>(), Array.Empty<RunResultSquadSlotView>(), companions, passives, synergies,
                new RunResultBestSynergyPresentation("future_one", "미래 시너지", 9));

            companions[0] = new PauseCompanionPresentation(null, 0);
            passives[0] = new PausePassivePresentation(null, 0);
            synergies[0] = new PauseSynergyPresentation("changed", "변경");

            Assert.That(view.CompanionPresentations[0].CurrentCount, Is.EqualTo(2));
            Assert.That(view.PassivePresentations[0].Level, Is.EqualTo(3));
            Assert.That(view.SynergyPresentations[0].Id, Is.EqualTo("future_one"));
            Assert.That(view.BestActiveSynergy.SummaryText, Is.EqualTo("미래 시너지 · 기여 9"));
        }

        [Test]
        public void Clear_PresentsSevenSlotsBestSynergyAndOwnedActions()
        {
            using Fixture fixture = new Fixture();
            RunResultViewData view = fixture.CreateView(true, true);
            int primaryCount = 0;
            int lobbyCount = 0;

            Assert.That(fixture.Popup.Present(view, () => primaryCount++, () => lobbyCount++), Is.True);
            Assert.That(fixture.Popup.IsShowingMainResult, Is.True);
            Assert.That(fixture.Popup.IsShowingReviveChoice, Is.False);
            Assert.That(fixture.Title.text, Is.EqualTo("승리"));
            Assert.That(fixture.Stage.text, Is.EqualTo("1-1"));
            Assert.That(fixture.SynergySection.text, Is.EqualTo("이번 클리어 우수 시너지"));
            Assert.That(fixture.SynergyName.text, Is.EqualTo("근위대"));
            Assert.That(fixture.MainSummarySynergy.text, Is.EqualTo("근위대"));
            Assert.That(fixture.LegionHeader.text, Is.EqualTo("최종 군단  3/7"));
            Assert.That(fixture.KpiLabels.Count(label => label.text == "레벨"), Is.EqualTo(0));
            Assert.That(fixture.DamageButton.interactable, Is.False);

            fixture.PrimaryButton.onClick.Invoke();
            fixture.LobbyButton.onClick.Invoke();
            Assert.That(primaryCount, Is.EqualTo(1));
            Assert.That(lobbyCount, Is.EqualTo(1));
        }

        [Test]
        public void ClearWithoutBestSynergyUsesEmptyStateAndLeavesPassiveSectionUnbound()
        {
            using Fixture fixture = new Fixture();
            fixture.ClearPassiveBindings();
            RunResultViewData view = fixture.CreateView(true, false);

            Assert.That(fixture.Popup.Present(view, null, null), Is.True);
            Assert.That(fixture.SynergyName.text, Is.EqualTo("우수 시너지 없음"));
            Assert.That(fixture.MainSummarySynergy.text, Is.EqualTo("우수 시너지 없음"));
            Assert.That(new SerializedObject(fixture.MainSummaryView).FindProperty("_passiveSlots").arraySize, Is.EqualTo(0));
        }

        [Test]
        public void ClearMissingIconLeavesOnlyThatCompanionSlotBlank()
        {
            using Fixture fixture = new Fixture();
            LogAssert.Expect(UnityEngine.LogType.Error, "[Result] Missing companion icon for summary slot 0");
            RunResultViewData view = fixture.CreateView(
                true,
                true,
                new[] { new PauseCompanionPresentation(null, 2) });

            Assert.That(fixture.Popup.Present(view, null, null), Is.True);
            Assert.That(fixture.CompanionIcons[0].gameObject.activeSelf, Is.False);
            Assert.That(fixture.CompanionCountTexts[0].gameObject.activeSelf, Is.False);
        }

        [Test]
        public void Failure_PresentsKpisAndRoutesRetryAndLobby()
        {
            using Fixture fixture = new Fixture();
            RunResultViewData view = fixture.CreateView(false, true);
            int retryCount = 0;
            int lobbyCount = 0;

            Assert.That(fixture.Popup.Present(view, () => retryCount++, () => lobbyCount++), Is.True);
            Assert.That(fixture.Popup.IsShowingFailureResult, Is.True);
            Assert.That(fixture.Popup.IsShowingMainResult, Is.False);
            Assert.That(fixture.Popup.IsShowingReviveChoice, Is.False);
            Assert.That(fixture.FailureTitle.text, Is.EqualTo("쓰러졌습니다"));
            Assert.That(fixture.FailureKpiValues[0].text, Is.EqualTo("1-1"));
            Assert.That(fixture.FailureKpiValues[1].text, Is.EqualTo("02:03"));
            Assert.That(fixture.FailureKpiValues[2].text, Is.EqualTo("17"));
            Assert.That(fixture.FailureKpiValues[3].text, Is.EqualTo("120"));
            Assert.That(fixture.FailureSummarySynergy.text, Is.EqualTo("근위대"));

            fixture.FailureRetryButton.onClick.Invoke();
            fixture.FailureLobbyButton.onClick.Invoke();
            Assert.That(retryCount, Is.EqualTo(1));
            Assert.That(lobbyCount, Is.EqualTo(1));
        }

        [Test]
        public void FailureReviveChoice_IsSingleUseAndCurrencyInert()
        {
            using Fixture fixture = new Fixture();
            int reviveCount = 0;
            Assert.That(fixture.Popup.PresentFailureReviveChoice(fixture.CreateView(false, false), null, () => reviveCount++, null), Is.True);
            Assert.That(fixture.Popup.IsShowingReviveChoice, Is.True);
            Assert.That(fixture.ReviveButton.interactable, Is.True);
            Assert.That(fixture.CurrencyButton.interactable, Is.False);
            Assert.That(fixture.CurrencyButton.gameObject.activeSelf, Is.False);

            fixture.ReviveButton.onClick.Invoke();
            fixture.ReviveButton.onClick.Invoke();
            Assert.That(reviveCount, Is.EqualTo(1));
            Assert.That(fixture.Popup.IsShowingReviveChoice, Is.False);
        }

        [Test]
        public void GameplayScene_BindsCurrentClearAndFailureViewsWithoutLegacyScrollContract()
        {
            Scene gameplay = EditorSceneManager.GetSceneByPath(GameplayScenePath);
            bool opened = false;
            if (!gameplay.isLoaded)
            {
                gameplay = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Additive);
                opened = true;
            }

            try
            {
                Transform root = gameplay.GetRootGameObjects().Single(item => item.name == "GameplayUIRoot").transform;
                UI_RunResultPopup popup = root.Find("ResultPopup").GetComponent<UI_RunResultPopup>();
                Assert.That(popup, Is.Not.Null);
                Assert.That(popup.ConfigureForClear(), Is.True);
                Assert.That(popup.ConfigureForFailure(), Is.True);
                Assert.That(popup.Configure(), Is.True);

                AssertSummary(root.Find("ResultPopup/SafeAreaContent/MainResult"), false);
                AssertSummary(root.Find("ResultPopup/SafeAreaContent/FailureResult"), true);
            }
            finally
            {
                if (opened)
                    EditorSceneManager.CloseScene(gameplay, true);
            }
        }

        static void AssertSummary(Transform result, bool requirePassiveBindings)
        {
            UI_RunBuildSummaryView summary = result.GetComponent<UI_RunBuildSummaryView>();
            Assert.That(summary, Is.Not.Null);
            SerializedObject serialized = new SerializedObject(summary);
            SerializedProperty companions = serialized.FindProperty("_companionSlots");
            SerializedProperty passives = serialized.FindProperty("_passiveSlots");
            Assert.That(companions.arraySize, Is.EqualTo(7));
            Assert.That(passives.arraySize, Is.EqualTo(5));
            TMP_Text synergy = serialized.FindProperty("_synergySummaryText").objectReferenceValue as TMP_Text;
            Assert.That(synergy, Is.Not.Null);
            Assert.That(synergy.raycastTarget, Is.False);

            for (int i = 0; i < companions.arraySize; i++)
            {
                Assert.That(companions.GetArrayElementAtIndex(i).FindPropertyRelative("_root").objectReferenceValue, Is.Not.Null);
                Assert.That(companions.GetArrayElementAtIndex(i).FindPropertyRelative("_icon").objectReferenceValue, Is.Not.Null);
                Assert.That(companions.GetArrayElementAtIndex(i).FindPropertyRelative("_countText").objectReferenceValue, Is.Not.Null);
            }

            for (int i = 0; i < passives.arraySize; i++)
            {
                TMP_Text level = passives.GetArrayElementAtIndex(i).FindPropertyRelative("_levelText").objectReferenceValue as TMP_Text;
                if (requirePassiveBindings)
                {
                    Assert.That(level, Is.Not.Null);
                    Assert.That(level.name, Is.EqualTo("LevelText"));
                    Assert.That(level.raycastTarget, Is.False);
                }
                else
                    Assert.That(level, Is.Null);
            }
        }

        sealed class Fixture : IDisposable
        {
            readonly GameObject _root;
            readonly Sprite _placeholderSprite;
            public readonly UI_RunResultPopup Popup;
            public readonly UI_RunBuildSummaryView MainSummaryView;
            public readonly TMP_Text Title;
            public readonly TMP_Text Stage;
            public readonly TMP_Text[] KpiLabels;
            public readonly TMP_Text[] KpiValues;
            public readonly TMP_Text SynergySection;
            public readonly TMP_Text SynergyName;
            public readonly TMP_Text MainSummarySynergy;
            public readonly TMP_Text FailureSummarySynergy;
            public readonly TMP_Text LegionHeader;
            public readonly TMP_Text FailureTitle;
            public readonly TMP_Text[] FailureKpiValues = new TMP_Text[4];
            public readonly Image[] CompanionIcons = new Image[7];
            public readonly TMP_Text[] CompanionCountTexts = new TMP_Text[7];
            public readonly Button DamageButton;
            public readonly Button PrimaryButton;
            public readonly Button LobbyButton;
            public readonly Button ReviveButton;
            public readonly Button CurrencyButton;
            public readonly Button FailureRetryButton;
            public readonly Button FailureLobbyButton;

            public Fixture()
            {
                _root = new GameObject("ResultViewTestRoot");
                _placeholderSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f);
                Popup = _root.AddComponent<UI_RunResultPopup>();

                GameObject main = CreateChild(_root, "MainResult");
                GameObject revive = CreateChild(_root, "ReviveChoice");
                UI_RunMainResultView mainView = main.AddComponent<UI_RunMainResultView>();
                UI_RunReviveChoiceView reviveView = revive.AddComponent<UI_RunReviveChoiceView>();
                UI_RunBuildSummaryView mainSummary = main.AddComponent<UI_RunBuildSummaryView>();
                MainSummaryView = mainSummary;
                Title = CreateText(main, "TitleText");
                GameObject header = CreateChild(main, "Header");
                Stage = CreateText(header, "StageText");
                GameObject kpiRow = CreateChild(main, "KpiRow");
                KpiLabels = new TMP_Text[4];
                KpiValues = new TMP_Text[4];
                UI_RunMainResultView.KpiBinding[] kpiItems = new UI_RunMainResultView.KpiBinding[4];
                string[] kpiNames = { "StageStat", "TimeStat", "KillStat", "GoldStat" };
                for (int i = 0; i < kpiItems.Length; i++)
                {
                    GameObject kpi = CreateChild(kpiRow, kpiNames[i]);
                    TMP_Text label = CreateText(kpi, "LabelText");
                    TMP_Text value = CreateText(kpi, "ValueText");
                    kpiItems[i] = new UI_RunMainResultView.KpiBinding();
                    SetField(kpiItems[i], "_labelText", label);
                    SetField(kpiItems[i], "_valueText", value);
                    KpiLabels[i] = label;
                    KpiValues[i] = value;
                }

                GameObject synergy = CreateChild(main, "SynergyHero");
                SynergySection = CreateText(synergy, "SectionLabelText");
                SynergyName = CreateText(synergy, "SynergyNameText");
                GameObject synergyMembersRoot = CreateChild(synergy, "Members");
                TMP_Text synergyMembers = CreateText(synergyMembersRoot, "MembersText");
                GameObject synergyEffectRoot = CreateChild(synergy, "Effect");
                TMP_Text synergyEffect = CreateText(synergyEffectRoot, "EffectText");
                Image[] synergyIcons = new Image[3];
                for (int i = 0; i < synergyIcons.Length; i++)
                    synergyIcons[i] = CreateChild(synergy, $"MemberIcon_{i:00}").AddComponent<Image>();

                LegionHeader = CreateText(main, "FinalLegionHeaderText");
                UI_RunMainResultView.LegionSlotBinding[] legionSlots = new UI_RunMainResultView.LegionSlotBinding[7];
                for (int i = 0; i < legionSlots.Length; i++)
                {
                    GameObject slotRoot = CreateChild(main, $"Slot_{i:00}");
                    GameObject visual = CreateChild(slotRoot, "Visual");
                    Image highlight = CreateChild(slotRoot, "Highlight").AddComponent<Image>();
                    Image icon = CreateChild(slotRoot, "Icon").AddComponent<Image>();
                    GameObject[] gems = new GameObject[3];
                    for (int gemIndex = 0; gemIndex < gems.Length; gemIndex++)
                        gems[gemIndex] = CreateChild(slotRoot, $"GradeGem_{gemIndex:00}");
                    legionSlots[i] = new UI_RunMainResultView.LegionSlotBinding();
                    SetField(legionSlots[i], "_root", slotRoot);
                    SetField(legionSlots[i], "_visual", visual);
                    SetField(legionSlots[i], "_highlight", highlight);
                    SetField(legionSlots[i], "_icon", icon);
                    SetField(legionSlots[i], "_gradeGems", gems);
                }
                DamageButton = CreateButton(main, "DamageStatisticsButton");
                TMP_Text damageText = CreateText(DamageButton.gameObject, "LabelText");
                PrimaryButton = CreateButton(main, "PrimaryButton");
                LobbyButton = CreateButton(main, "LobbyButton");
                TMP_Text primaryButtonText = CreateText(PrimaryButton.gameObject, "Text");
                TMP_Text lobbyButtonText = CreateText(LobbyButton.gameObject, "Text");

                GameObject failure = CreateChild(_root, "FailureResult");
                UI_RunFailureResultView failureView = failure.AddComponent<UI_RunFailureResultView>();
                UI_RunBuildSummaryView failureSummary = failure.AddComponent<UI_RunBuildSummaryView>();
                GameObject failureHeader = CreateChild(failure, "Header");
                FailureTitle = CreateText(failureHeader, "TitleText");
                TMP_Text failureSubtitle = CreateText(failureHeader, "SubtitleText");
                UI_RunFailureResultView.KpiBinding[] failureKpis = new UI_RunFailureResultView.KpiBinding[4];
                for (int i = 0; i < failureKpis.Length; i++)
                {
                    GameObject stat = CreateChild(failure, $"FailureStat_{i:00}");
                    TMP_Text label = CreateText(stat, "LabelText");
                    TMP_Text value = CreateText(stat, "ValueText");
                    failureKpis[i] = new UI_RunFailureResultView.KpiBinding();
                    SetField(failureKpis[i], "_labelText", label);
                    SetField(failureKpis[i], "_valueText", value);
                    FailureKpiValues[i] = value;
                }
                FailureLobbyButton = CreateButton(failure, "LobbyButton");
                FailureRetryButton = CreateButton(failure, "RetryButton");
                TMP_Text failureLobbyText = CreateText(FailureLobbyButton.gameObject, "LabelText");
                TMP_Text failureRetryText = CreateText(FailureRetryButton.gameObject, "LabelText");
                TMP_Text reviveTitle = CreateText(revive, "TitleText");
                TMP_Text reviveTimeout = CreateText(revive, "TimeoutText");
                TMP_Text reviveBody = CreateText(revive, "BodyText");
                ReviveButton = CreateButton(revive, "ReviveButton");
                CurrencyButton = CreateButton(revive, "CurrencyButton");
                Button closeButton = CreateButton(revive, "GiveUpButton");

                SetField(mainView, "_titleText", Title);
                SetField(mainView, "_stageText", Stage);
                SetField(mainView, "_kpiItems", kpiItems);
                SetField(mainView, "_synergySectionLabelText", SynergySection);
                SetField(mainView, "_synergyNameText", SynergyName);
                SetField(mainView, "_synergyMemberIcons", synergyIcons);
                SetField(mainView, "_synergyMembersRoot", synergyMembersRoot);
                SetField(mainView, "_synergyMembersText", synergyMembers);
                SetField(mainView, "_synergyEffectRoot", synergyEffectRoot);
                SetField(mainView, "_synergyEffectText", synergyEffect);
                SetField(mainView, "_finalLegionHeaderText", LegionHeader);
                SetField(mainView, "_legionSlots", legionSlots);
                SetField(mainView, "_legionIconSprites", new Sprite[7]);
                SetField(mainView, "_damageStatisticsButton", DamageButton);
                SetField(mainView, "_damageStatisticsButtonText", damageText);
                SetField(mainView, "_primaryButton", PrimaryButton);
                SetField(mainView, "_primaryButtonText", primaryButtonText);
                SetField(mainView, "_lobbyButton", LobbyButton);
                SetField(mainView, "_lobbyButtonText", lobbyButtonText);
                SetField(Popup, "_failureResultView", failureView);
                SetField(failureView, "_titleText", FailureTitle);
                SetField(failureView, "_subtitleText", failureSubtitle);
                SetField(failureView, "_kpiItems", failureKpis);
                SetField(failureView, "_lobbyButton", FailureLobbyButton);
                SetField(failureView, "_lobbyButtonText", failureLobbyText);
                SetField(failureView, "_retryButton", FailureRetryButton);
                SetField(failureView, "_retryButtonText", failureRetryText);
                SetField(Popup, "_mainResultView", mainView);
                SetField(Popup, "_reviveChoiceView", reviveView);
                SetField(Popup, "_mainBuildSummaryView", mainSummary);
                SetField(Popup, "_failureBuildSummaryView", failureSummary);
                MainSummarySynergy = BindSummary(mainSummary, main, false);
                FailureSummarySynergy = BindSummary(failureSummary, failure, true);
                SetField(reviveView, "_titleText", reviveTitle);
                SetField(reviveView, "_timeoutText", reviveTimeout);
                SetField(reviveView, "_bodyText", reviveBody);
                SetField(reviveView, "_reviveButton", ReviveButton);
                SetField(reviveView, "_currencyButton", CurrencyButton);
                SetField(reviveView, "_closeButton", closeButton);
            }

            public RunResultViewData CreateView(bool isClear, bool includeBest, IReadOnlyList<PauseCompanionPresentation> companions = null)
            {
                RunResultSquadSlotView[] slots = new RunResultSquadSlotView[7];
                for (int i = 0; i < slots.Length; i++)
                    slots[i] = new RunResultSquadSlotView($"slot_{i}", $"slot {i}", i, isClear && i < 3 ? 1 : 0, isClear && i < 3);

                return new RunResultViewData(
                    isClear, isClear ? "승리" : "쓰러졌습니다", string.Empty, "1-1", "다시 전장에 들어가 준비를 이어가세요.",
                    isClear ? "전투 준비 계속" : "다시 도전", false, string.Empty, 123.0f, 17, 4,
                    "편성 군단", "사령관이 전투 중 쓰러졌습니다.", "동료를 모아 강화하세요.", 120, isClear,
                    string.Empty, "근위대", "방패 계열 + 검병 + 성직자", "지휘관 중심 방어 밀치기", new[] { 0, 1, 2 }, slots,
                    companions ?? new[]
                    {
                        new PauseCompanionPresentation(_placeholderSprite, isClear ? 1 : 0),
                        new PauseCompanionPresentation(_placeholderSprite, isClear ? 1 : 0),
                        new PauseCompanionPresentation(_placeholderSprite, isClear ? 1 : 0)
                    },
                    Array.Empty<PausePassivePresentation>(),
                    isClear
                        ? new[] { new PauseSynergyPresentation("synergy_guard_shockwave", "근위대"), new PauseSynergyPresentation("synergy_archer_rain", "사격대") }
                        : new[] { new PauseSynergyPresentation("synergy_guard_shockwave", "근위대") },
                    includeBest && isClear ? new RunResultBestSynergyPresentation("synergy_guard_shockwave", "근위대", 0) : null);
            }

            public void ClearPassiveBindings()
            {
                SetField(MainSummaryView, "_passiveSlots", new UI_RunBuildSummaryView.PassiveSlotBinding[0]);
            }

            public void Dispose()
            {
                if (_root != null)
                    UnityEngine.Object.DestroyImmediate(_root);
                if (_placeholderSprite != null)
                    UnityEngine.Object.DestroyImmediate(_placeholderSprite);
            }

            TMP_Text BindSummary(UI_RunBuildSummaryView view, GameObject parent, bool failure)
            {
                UI_RunBuildSummaryView.CompanionSlotBinding[] companions = new UI_RunBuildSummaryView.CompanionSlotBinding[7];
                for (int i = 0; i < companions.Length; i++)
                {
                    GameObject root = CreateChild(parent, $"SummaryCompanion_{i:00}");
                    GameObject active = CreateChild(root, "ActiveVisual");
                    GameObject empty = CreateChild(root, "EmptyVisual");
                    Image icon = CreateChild(root, "Icon").AddComponent<Image>();
                    TMP_Text count = CreateText(root, "CountText");
                    companions[i] = new UI_RunBuildSummaryView.CompanionSlotBinding();
                    SetField(companions[i], "_root", root);
                    SetField(companions[i], "_activeVisual", active);
                    SetField(companions[i], "_emptyVisual", empty);
                    SetField(companions[i], "_icon", icon);
                    SetField(companions[i], "_countText", count);
                    if (!failure)
                    {
                        CompanionIcons[i] = icon;
                        CompanionCountTexts[i] = count;
                    }
                }

                UI_RunBuildSummaryView.PassiveSlotBinding[] passives = new UI_RunBuildSummaryView.PassiveSlotBinding[5];
                for (int i = 0; i < passives.Length; i++)
                {
                    GameObject root = CreateChild(parent, $"SummaryPassive_{i:00}");
                    GameObject active = CreateChild(root, "ActiveVisual");
                    GameObject empty = CreateChild(root, "EmptyVisual");
                    Image icon = CreateChild(root, "Icon").AddComponent<Image>();
                    TMP_Text level = CreateText(root, "LevelText");
                    passives[i] = new UI_RunBuildSummaryView.PassiveSlotBinding();
                    SetField(passives[i], "_root", root);
                    SetField(passives[i], "_activeVisual", active);
                    SetField(passives[i], "_emptyVisual", empty);
                    SetField(passives[i], "_icon", icon);
                    SetField(passives[i], "_levelText", level);
                }
                SetField(view, "_companionSlots", companions);
                SetField(view, "_passiveSlots", passives);
                TMP_Text synergy = CreateText(parent, "SummarySynergyText");
                SetField(view, "_synergySummaryText", synergy);
                return synergy;
            }

            static GameObject CreateChild(GameObject parent, string name)
            {
                GameObject child = new GameObject(name);
                child.transform.SetParent(parent.transform, false);
                return child;
            }

            static TMP_Text CreateText(GameObject parent, string name) => CreateChild(parent, name).AddComponent<TextMeshProUGUI>();
            static Button CreateButton(GameObject parent, string name) => CreateChild(parent, name).AddComponent<Button>();

            static void SetField(object target, string name, object value)
            {
                target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
            }
        }
    }
}
