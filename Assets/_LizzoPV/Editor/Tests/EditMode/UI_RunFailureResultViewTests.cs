using NUnit.Framework;
using Lizzo.PV.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class UI_RunFailureResultViewTests
    {
        [Test]
        public void PresentFailure_PopulatesStageKpisAndRoutesActions()
        {
            using Fixture fixture = new Fixture();
            RunResultViewData view = new RunResultViewData(
                false,
                "쓰러졌습니다",
                "이번 전투 기록",
                "1-1",
                string.Empty,
                "다시 도전",
                false,
                string.Empty,
                123.0f,
                17,
                1,
                string.Empty,
                string.Empty,
                string.Empty,
                120,
                false,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                System.Array.Empty<int>(),
                System.Array.Empty<RunResultSquadSlotView>());
            int retryCount = 0;
            int lobbyCount = 0;

            Assert.IsTrue(fixture.View.Present(view, () => retryCount++, () => lobbyCount++));
            Assert.IsTrue(fixture.View.IsShowing);
            Assert.AreEqual("쓰러졌습니다", fixture.Title.text);
            Assert.AreEqual("이번 전투 기록", fixture.Subtitle.text);
            Assert.AreEqual("스테이지", fixture.KpiLabels[0].text);
            Assert.AreEqual("1-1", fixture.KpiValues[0].text);
            Assert.AreEqual("시간", fixture.KpiLabels[1].text);
            Assert.AreEqual("02:03", fixture.KpiValues[1].text);
            Assert.AreEqual("처치", fixture.KpiLabels[2].text);
            Assert.AreEqual("17", fixture.KpiValues[2].text);
            Assert.AreEqual("골드", fixture.KpiLabels[3].text);
            Assert.AreEqual("120", fixture.KpiValues[3].text);

            fixture.RetryButton.onClick.Invoke();
            fixture.LobbyButton.onClick.Invoke();

            Assert.AreEqual(1, retryCount);
            Assert.AreEqual(1, lobbyCount);
        }

        private sealed class Fixture : System.IDisposable
        {
            private readonly GameObject _root;
            public readonly UI_RunFailureResultView View;
            public readonly TMP_Text Title;
            public readonly TMP_Text Subtitle;
            public readonly TMP_Text[] KpiLabels = new TMP_Text[4];
            public readonly TMP_Text[] KpiValues = new TMP_Text[4];
            public readonly Button RetryButton;
            public readonly Button LobbyButton;

            public Fixture()
            {
                _root = new GameObject("FailureResultTestRoot");
                View = _root.AddComponent<UI_RunFailureResultView>();
                GameObject header = CreateChild(_root, "Header");
                Title = CreateText(header, "TitleText");
                Subtitle = CreateText(header, "SubtitleText");
                UI_RunFailureResultView.KpiBinding[] kpis = new UI_RunFailureResultView.KpiBinding[4];
                for (int i = 0; i < kpis.Length; i++)
                {
                    GameObject stat = CreateChild(_root, $"Stat_{i:00}");
                    TMP_Text label = CreateText(stat, "LabelText");
                    TMP_Text value = CreateText(stat, "ValueText");
                    kpis[i] = new UI_RunFailureResultView.KpiBinding();
                    SetBindingField(kpis[i], "_labelText", label);
                    SetBindingField(kpis[i], "_valueText", value);
                    KpiLabels[i] = label;
                    KpiValues[i] = value;
                }
                LobbyButton = CreateButton(_root, "LobbyButton");
                RetryButton = CreateButton(_root, "RetryButton");
                TMP_Text lobbyText = CreateText(LobbyButton.gameObject, "LabelText");
                TMP_Text retryText = CreateText(RetryButton.gameObject, "LabelText");

                SetField("_titleText", Title);
                SetField("_subtitleText", Subtitle);
                SetField("_kpiItems", kpis);
                SetField("_lobbyButton", LobbyButton);
                SetField("_lobbyButtonText", lobbyText);
                SetField("_retryButton", RetryButton);
                SetField("_retryButtonText", retryText);
            }

            private void SetField(string name, object value)
            {
                typeof(UI_RunFailureResultView)
                    .GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .SetValue(View, value);
            }

            private static void SetBindingField(
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
