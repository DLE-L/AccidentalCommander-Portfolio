using NUnit.Framework;
using Lizzo.PV.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class UI_RunResultPopupTests
    {
        [Test]
        public void PresentClear_PopulatesResultAndRoutesActions()
        {
            using Fixture fixture = new Fixture();
            RunResultViewData view = fixture.CreateView(true);
            int primaryCount = 0;
            int lobbyCount = 0;

            Assert.IsTrue(fixture.Popup.Present(view, () => primaryCount++, () => lobbyCount++));
            Assert.IsTrue(fixture.Popup.IsShowingMainResult);
            Assert.IsFalse(fixture.Popup.IsShowingReviveChoice);
            Assert.AreEqual("승리", fixture.Title.text);
            Assert.AreEqual("1-1", fixture.Stage.text);
            Assert.AreEqual("스테이지", fixture.KpiLabels[0].text);
            Assert.AreEqual("1-1", fixture.KpiValues[0].text);
            Assert.AreEqual("시간", fixture.KpiLabels[1].text);
            Assert.AreEqual("02:03", fixture.KpiValues[1].text);
            Assert.AreEqual("처치", fixture.KpiLabels[2].text);
            Assert.AreEqual("17", fixture.KpiValues[2].text);
            Assert.AreEqual("골드", fixture.KpiLabels[3].text);
            Assert.AreEqual("120", fixture.KpiValues[3].text);
            Assert.IsFalse(System.Array.Exists(fixture.KpiLabels, label => label.text == "레벨"));
            Assert.AreEqual("근위대", fixture.SynergyName.text);
            Assert.AreEqual("최종 군단  3/7", fixture.LegionHeader.text);
            Assert.IsFalse(fixture.DamageButton.interactable);

            fixture.PrimaryButton.onClick.Invoke();
            fixture.LobbyButton.onClick.Invoke();

            Assert.AreEqual(1, primaryCount);
            Assert.AreEqual(1, lobbyCount);
        }

        [Test]
        public void FailureReviveChoice_WiresFreeReviveOnceAndKeepsCurrencyInert()
        {
            using Fixture fixture = new Fixture();
            RunResultViewData view = fixture.CreateView(false);
            int reviveCount = 0;

            Assert.IsTrue(fixture.Popup.PresentFailureReviveChoice(view, null, () => reviveCount++, null));
            Assert.IsTrue(fixture.Popup.IsShowingReviveChoice);
            Assert.IsFalse(fixture.Popup.IsShowingMainResult);
            Assert.IsTrue(fixture.ReviveButton.interactable);
            Assert.IsFalse(fixture.CurrencyButton.interactable);
            Assert.IsFalse(fixture.CurrencyButton.gameObject.activeSelf);

            fixture.ReviveButton.onClick.Invoke();
            fixture.ReviveButton.onClick.Invoke();
            Assert.AreEqual(1, reviveCount);
            Assert.IsFalse(fixture.Popup.IsShowingReviveChoice);
        }

        [Test]
        public void ReviveChoiceClose_ReportsDeclineAndRoutesToFailureOnce()
        {
            using Fixture fixture = new Fixture();
            RunResultViewData view = fixture.CreateView(false);

            Assert.IsTrue(fixture.Popup.PresentFailureReviveChoice(view, null, () => { }, null));
            Assert.IsTrue(fixture.Popup.CloseReviveChoice());
            Assert.IsFalse(fixture.Popup.IsShowingReviveChoice);
            Assert.IsTrue(fixture.Popup.IsShowingFailureResult);
            Assert.IsFalse(fixture.Popup.CloseReviveChoice());
        }

        [Test]
        public void FailureWithoutReviveCallback_PresentsNormalFailureResult()
        {
            using Fixture fixture = new Fixture();
            RunResultViewData view = fixture.CreateView(false);

            Assert.IsTrue(fixture.Popup.Present(view, null, null));
            Assert.IsFalse(fixture.Popup.IsShowingReviveChoice);
            Assert.IsFalse(fixture.Popup.IsShowingMainResult);
            Assert.IsTrue(fixture.Popup.IsShowingFailureResult);
            Assert.AreEqual("쓰러졌습니다", fixture.FailureTitle.text);
            Assert.AreEqual("1-1", fixture.FailureKpiValues[0].text);
        }

        [Test]
        public void FailurePresent_ShowsFailureResultOnlyAndRoutesActions()
        {
            using Fixture fixture = new Fixture();
            RunResultViewData view = fixture.CreateView(false);
            int retryCount = 0;
            int lobbyCount = 0;

            Assert.IsTrue(fixture.Popup.Present(view, () => retryCount++, () => lobbyCount++));
            Assert.IsFalse(fixture.Popup.IsShowingMainResult);
            Assert.IsFalse(fixture.Popup.IsShowingReviveChoice);
            Assert.IsTrue(fixture.Popup.IsShowingFailureResult);
            Assert.AreEqual("쓰러졌습니다", fixture.FailureTitle.text);
            Assert.AreEqual("1-1", fixture.FailureKpiValues[0].text);

            fixture.FailureRetryButton.onClick.Invoke();
            fixture.FailureLobbyButton.onClick.Invoke();

            Assert.AreEqual(1, retryCount);
            Assert.AreEqual(1, lobbyCount);
        }

        [Test]
        public void ClearResult_ExposesSynergyKpiAndSevenSlotContract()
        {
            Assert.IsNotNull(typeof(UI_RunMainResultView));
            Assert.IsNotNull(typeof(UI_RunReviveChoiceView));
            Assert.IsNotNull(
                typeof(UI_RunResultPopup).GetField(
                    "_mainResultView",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic));
            Assert.IsNotNull(
                typeof(UI_RunResultPopup).GetField(
                    "_reviveChoiceView",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic));
            Assert.IsNull(
                typeof(UI_RunResultPopup).GetField(
                    "_kpiItems",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic));
            Assert.IsNull(
                typeof(UI_RunResultPopup).GetField(
                    "_reviveChoiceTimeoutCancellation",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic));
            Assert.IsNotNull(
                typeof(RunResultViewData).GetProperty("HasCompletedSynergy"));
            Assert.IsNotNull(
                typeof(RunResultViewData).GetProperty("SquadSlots"));
        }

        private sealed class Fixture : System.IDisposable
        {
            private readonly GameObject _root;
            public readonly UI_RunResultPopup Popup;
            public readonly TMP_Text Title;
            public readonly TMP_Text Stage;
            public readonly TMP_Text[] KpiLabels;
            public readonly TMP_Text[] KpiValues;
            public readonly TMP_Text SynergyName;
            public readonly TMP_Text LegionHeader;
            public readonly TMP_Text FailureTitle;
            public readonly TMP_Text[] FailureKpiValues = new TMP_Text[4];
            public readonly Button DamageButton;
            public readonly Button PrimaryButton;
            public readonly Button LobbyButton;
            public readonly Button ReviveButton;
            public readonly Button CurrencyButton;
            public readonly Button FailureRetryButton;
            public readonly Button FailureLobbyButton;

            public Fixture()
            {
                _root = new GameObject("ResultPopupTestRoot");
                Popup = _root.AddComponent<UI_RunResultPopup>();
                GameObject main = CreateChild(_root, "MainResult");
                GameObject revive = CreateChild(_root, "ReviveChoice");
                UI_RunMainResultView mainView = main.AddComponent<UI_RunMainResultView>();
                UI_RunReviveChoiceView reviveView = revive.AddComponent<UI_RunReviveChoiceView>();
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
                    SetMainBindingField(kpiItems[i], "_labelText", label);
                    SetMainBindingField(kpiItems[i], "_valueText", value);
                    KpiLabels[i] = label;
                    KpiValues[i] = value;
                }
                GameObject synergy = CreateChild(main, "SynergyHero");
                TMP_Text synergyLabel = CreateText(synergy, "SectionLabelText");
                SynergyName = CreateText(synergy, "SynergyNameText");
                GameObject synergyMembersRoot = CreateChild(synergy, "Members");
                TMP_Text synergyMembers = CreateText(synergyMembersRoot, "MembersText");
                GameObject synergyEffectRoot = CreateChild(synergy, "Effect");
                TMP_Text synergyEffect = CreateText(synergyEffectRoot, "EffectText");
                Image[] synergyIcons = new Image[3];
                for (int i = 0; i < synergyIcons.Length; i++)
                {
                    GameObject iconRoot = CreateChild(synergy, $"MemberIcon_{i:00}");
                    synergyIcons[i] = iconRoot.AddComponent<Image>();
                }
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
                    SetMainBindingField(legionSlots[i], "_root", slotRoot);
                    SetMainBindingField(legionSlots[i], "_visual", visual);
                    SetMainBindingField(legionSlots[i], "_highlight", highlight);
                    SetMainBindingField(legionSlots[i], "_icon", icon);
                    SetMainBindingField(legionSlots[i], "_gradeGems", gems);
                }
                GameObject damage = CreateChild(main, "DamageStatisticsButton");
                DamageButton = damage.AddComponent<Button>();
                TMP_Text damageText = CreateText(damage, "LabelText");
                PrimaryButton = CreateButton(main, "PrimaryButton");
                LobbyButton = CreateButton(main, "LobbyButton");
                TMP_Text primaryButtonText = CreateText(PrimaryButton.gameObject, "Text");
                TMP_Text lobbyButtonText = CreateText(LobbyButton.gameObject, "Text");

                GameObject failure = CreateChild(_root, "FailureResult");
                UI_RunFailureResultView failureView = failure.AddComponent<UI_RunFailureResultView>();
                GameObject failureHeader = CreateChild(failure, "Header");
                FailureTitle = CreateText(failureHeader, "TitleText");
                TMP_Text failureSubtitle = CreateText(failureHeader, "SubtitleText");
                UI_RunFailureResultView.KpiBinding[] failureKpis = new UI_RunFailureResultView.KpiBinding[4];
                for (int i = 0; i < failureKpis.Length; i++)
                {
                    GameObject failureStat = CreateChild(failure, $"FailureStat_{i:00}");
                    TMP_Text label = CreateText(failureStat, "LabelText");
                    TMP_Text value = CreateText(failureStat, "ValueText");
                    failureKpis[i] = new UI_RunFailureResultView.KpiBinding();
                    SetFailureBindingField(failureKpis[i], "_labelText", label);
                    SetFailureBindingField(failureKpis[i], "_valueText", value);
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

                SetMainField(mainView, "_titleText", Title);
                SetMainField(mainView, "_stageText", Stage);
                SetMainField(mainView, "_kpiItems", kpiItems);
                SetMainField(mainView, "_synergySectionLabelText", synergyLabel);
                SetMainField(mainView, "_synergyNameText", SynergyName);
                SetMainField(mainView, "_synergyMemberIcons", synergyIcons);
                SetMainField(mainView, "_synergyMembersRoot", synergyMembersRoot);
                SetMainField(mainView, "_synergyMembersText", synergyMembers);
                SetMainField(mainView, "_synergyEffectRoot", synergyEffectRoot);
                SetMainField(mainView, "_synergyEffectText", synergyEffect);
                SetMainField(mainView, "_finalLegionHeaderText", LegionHeader);
                SetMainField(mainView, "_legionSlots", legionSlots);
                SetMainField(mainView, "_legionIconSprites", new Sprite[7]);
                SetMainField(mainView, "_damageStatisticsButton", DamageButton);
                SetMainField(mainView, "_damageStatisticsButtonText", damageText);
                SetMainField(mainView, "_primaryButton", PrimaryButton);
                SetMainField(mainView, "_primaryButtonText", primaryButtonText);
                SetMainField(mainView, "_lobbyButton", LobbyButton);
                SetMainField(mainView, "_lobbyButtonText", lobbyButtonText);
                SetField("_failureResultView", failureView);
                SetFailureField(failureView, "_titleText", FailureTitle);
                SetFailureField(failureView, "_subtitleText", failureSubtitle);
                SetFailureField(failureView, "_kpiItems", failureKpis);
                SetFailureField(failureView, "_lobbyButton", FailureLobbyButton);
                SetFailureField(failureView, "_lobbyButtonText", failureLobbyText);
                SetFailureField(failureView, "_retryButton", FailureRetryButton);
                SetFailureField(failureView, "_retryButtonText", failureRetryText);
                SetField("_mainResultView", mainView);
                SetField("_reviveChoiceView", reviveView);
                SetMainField(reviveView, "_titleText", reviveTitle);
                SetMainField(reviveView, "_timeoutText", reviveTimeout);
                SetMainField(reviveView, "_bodyText", reviveBody);
                SetMainField(reviveView, "_reviveButton", ReviveButton);
                SetMainField(reviveView, "_currencyButton", CurrencyButton);
                SetMainField(reviveView, "_closeButton", closeButton);
            }

            public RunResultViewData CreateView(bool isClear)
            {
                RunResultSquadSlotView[] slots = new RunResultSquadSlotView[7];
                for (int i = 0; i < slots.Length; i++)
                {
                    slots[i] = new RunResultSquadSlotView(
                        $"slot_{i}",
                        $"slot {i}",
                        i,
                        isClear && i < 3 ? 1 : 0,
                        isClear && i < 3);
                }

                return new RunResultViewData(
                    isClear,
                    isClear ? "승리" : "쓰러졌습니다",
                    string.Empty,
                    "1-1",
                    "다시 전장에 들어가 준비를 이어가세요.",
                    isClear ? "전투 준비 계속" : "다시 도전",
                    false,
                    string.Empty,
                    123.0f,
                    17,
                    4,
                    "편성 군단",
                    "사령관이 전투 중 쓰러졌습니다.",
                    "동료를 모아 강화하세요.",
                    120,
                    isClear,
                    string.Empty,
                    "근위대",
                    "방패 계열 + 검병 + 성직자",
                    "지휘관 중심 방어 밀치기",
                    new[] { 0, 1, 2 },
                    slots);
            }

            private void SetField(string name, object value)
            {
                typeof(UI_RunResultPopup)
                    .GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .SetValue(Popup, value);
            }

            private static void SetMainField(object target, string name, object value)
            {
                target.GetType()
                    .GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .SetValue(target, value);
            }

            private static void SetMainBindingField(object target, string name, object value)
            {
                target.GetType()
                    .GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .SetValue(target, value);
            }

            private static void SetFailureField(UI_RunFailureResultView view, string name, object value)
            {
                typeof(UI_RunFailureResultView)
                    .GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .SetValue(view, value);
            }

            private static void SetFailureBindingField(
                UI_RunFailureResultView.KpiBinding binding,
                string name,
                object value)
            {
                typeof(UI_RunFailureResultView.KpiBinding)
                    .GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .SetValue(binding, value);
            }

            public void Dispose()
            {
                if (_root != null)
                    Object.DestroyImmediate(_root);
            }

            private static GameObject CreateChild(GameObject parent, string name)
            {
                GameObject child = new GameObject(name);
                child.transform.SetParent(parent.transform, false);
                return child;
            }

            private static TMP_Text CreateText(GameObject parent, string name)
            {
                GameObject child = CreateChild(parent, name);
                return child.AddComponent<TextMeshProUGUI>();
            }

            private static Button CreateButton(GameObject parent, string name)
            {
                GameObject child = CreateChild(parent, name);
                return child.AddComponent<Button>();
            }
        }
    }
}
