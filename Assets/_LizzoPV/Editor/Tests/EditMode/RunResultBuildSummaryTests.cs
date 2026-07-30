using NUnit.Framework;
using Lizzo.PV.UI;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class RunResultBuildSummaryTests
    {
        [Test]
        public void ResultSnapshot_ExposesImmutableCanonicalSummaryContract()
        {
            Assert.IsNotNull(typeof(RunResultViewData).GetProperty("CompanionPresentations"));
            Assert.IsNotNull(typeof(RunResultViewData).GetProperty("PassivePresentations"));
            Assert.IsNotNull(typeof(RunResultViewData).GetProperty("SynergyPresentations"));
            Assert.IsNotNull(typeof(RunResultViewData).GetProperty("BestActiveSynergy"));

            System.Type viewType = System.Type.GetType(
                "Lizzo.PV.UI.UI_RunBuildSummaryView, Assembly-CSharp");
            Assert.IsNotNull(viewType);
        }

        [Test]
        public void ResultSnapshot_PreservesCanonicalEntriesAfterInputMutation()
        {
            var companions = new System.Collections.Generic.List<PauseCompanionPresentation>
            {
                new PauseCompanionPresentation(null, 2)
            };
            var passives = new System.Collections.Generic.List<PausePassivePresentation>
            {
                new PausePassivePresentation(null, 3)
            };
            var synergies = new System.Collections.Generic.List<PauseSynergyPresentation>
            {
                new PauseSynergyPresentation("future_one", "미래 시너지")
            };
            RunResultBestSynergyPresentation best = new RunResultBestSynergyPresentation("future_one", "미래 시너지", 9);

            RunResultViewData view = new RunResultViewData(
                false, "실패", "", "1-1", "", "재도전", false, "", 1f, 1, 1,
                "", "", "", 0, false, "", "", "", "",
                System.Array.Empty<int>(), System.Array.Empty<RunResultSquadSlotView>(),
                companions, passives, synergies, best);

            companions[0] = new PauseCompanionPresentation(null, 0);
            passives[0] = new PausePassivePresentation(null, 0);
            synergies[0] = new PauseSynergyPresentation("changed", "변경");

            Assert.AreEqual(2, view.CompanionPresentations[0].CurrentCount);
            Assert.AreEqual(3, view.PassivePresentations[0].Level);
            Assert.AreEqual("future_one", view.SynergyPresentations[0].Id);
            Assert.AreEqual("미래 시너지 · 기여 9", view.BestActiveSynergy.SummaryText);
        }

        [Test]
        public void BuildSummary_PresentsCanonicalIconsCountsLevelsAndAllSynergies()
        {
            using Fixture fixture = new Fixture();
            Sprite companionIcon = fixture.CreateSprite();
            Sprite passiveIcon = fixture.CreateSprite();
            RunResultViewData view = fixture.CreateView(
                new[] { new PauseCompanionPresentation(companionIcon, 2) },
                new[] { new PausePassivePresentation(passiveIcon, 3) },
                new[]
                {
                    new PauseSynergyPresentation("one", "시너지 1"),
                    new PauseSynergyPresentation("two", "시너지 2")
                });

            Assert.IsTrue(fixture.View.Present(view));
            Assert.AreEqual("x2", fixture.CompanionCountTexts[0].text);
            Assert.AreEqual("Lv.3", fixture.PassiveLevelTexts[0].text);
            Assert.AreEqual("시너지 1 · 시너지 2", fixture.Synergy.text);
            Assert.IsTrue(fixture.CompanionIcons[0].gameObject.activeSelf);
            Assert.IsTrue(fixture.PassiveIcons[0].gameObject.activeSelf);
            Assert.IsTrue(fixture.CompanionActiveVisuals[0].activeSelf);
            Assert.IsFalse(fixture.CompanionEmptyVisuals[0].activeSelf);
            Assert.IsTrue(fixture.CompanionRoots[1].activeSelf);
            Assert.IsFalse(fixture.CompanionActiveVisuals[1].activeSelf);
            Assert.IsTrue(fixture.CompanionEmptyVisuals[1].activeSelf);
            Assert.IsTrue(fixture.PassiveActiveVisuals[0].activeSelf);
            Assert.IsFalse(fixture.PassiveEmptyVisuals[0].activeSelf);
            Assert.IsFalse(fixture.PassiveActiveVisuals[1].activeSelf);
            Assert.IsTrue(fixture.PassiveEmptyVisuals[1].activeSelf);
        }

        [Test]
        public void BuildSummary_ClearUsesBestSynergyCompactOutput()
        {
            using Fixture fixture = new Fixture();
            RunResultViewData view = fixture.CreateView(
                new[] { new PauseCompanionPresentation(fixture.CreateSprite(), 1) },
                System.Array.Empty<PausePassivePresentation>(),
                new[]
                {
                    new PauseSynergyPresentation("one", "시너지 1"),
                    new PauseSynergyPresentation("two", "시너지 2")
                },
                true,
                new RunResultBestSynergyPresentation("one", "시너지 1", 12));

            Assert.IsTrue(fixture.View.Present(view));
            Assert.AreEqual("시너지 1", fixture.Synergy.text);
        }

        [Test]
        public void BuildSummary_ClearAllowsUnboundPassiveAuthoring()
        {
            using Fixture fixture = new Fixture();
            fixture.ClearPassiveBindings();
            RunResultViewData view = fixture.CreateView(
                System.Array.Empty<PauseCompanionPresentation>(),
                System.Array.Empty<PausePassivePresentation>(),
                System.Array.Empty<PauseSynergyPresentation>(),
                true,
                new RunResultBestSynergyPresentation("one", "시너지 1", 0));

            Assert.IsTrue(fixture.View.Present(view));
            Assert.AreEqual("시너지 1", fixture.Synergy.text);
        }

        [Test]
        public void BuildSummary_MissingIconLeavesOnlyThatItemBlank()
        {
            using Fixture fixture = new Fixture();
            LogAssert.Expect(LogType.Error, "[Result] Missing companion icon for summary slot 0");
            RunResultViewData view = fixture.CreateView(
                new[] { new PauseCompanionPresentation(null, 2) },
                System.Array.Empty<PausePassivePresentation>(),
                System.Array.Empty<PauseSynergyPresentation>());

            Assert.IsTrue(fixture.View.Present(view));
            Assert.IsFalse(fixture.CompanionIcons[0].gameObject.activeSelf);
            Assert.IsFalse(fixture.CompanionCountTexts[0].gameObject.activeSelf);
        }

        private sealed class Fixture : System.IDisposable
        {
            private readonly GameObject _root;
            public readonly UI_RunBuildSummaryView View;
            public readonly GameObject[] CompanionRoots = new GameObject[7];
            public readonly GameObject[] CompanionActiveVisuals = new GameObject[7];
            public readonly GameObject[] CompanionEmptyVisuals = new GameObject[7];
            public readonly Image[] CompanionIcons = new Image[7];
            public readonly TMP_Text[] CompanionCountTexts = new TMP_Text[7];
            public readonly GameObject[] PassiveActiveVisuals = new GameObject[5];
            public readonly GameObject[] PassiveEmptyVisuals = new GameObject[5];
            public readonly Image[] PassiveIcons = new Image[5];
            public readonly TMP_Text[] PassiveLevelTexts = new TMP_Text[5];
            public readonly TMP_Text Synergy;

            public Fixture()
            {
                _root = new GameObject("BuildSummaryFixture");
                View = _root.AddComponent<UI_RunBuildSummaryView>();
                UI_RunBuildSummaryView.CompanionSlotBinding[] companions =
                    new UI_RunBuildSummaryView.CompanionSlotBinding[7];
                for (int i = 0; i < companions.Length; i++)
                {
                    GameObject root = CreateChild(_root, $"Companion_{i:00}");
                    GameObject active = CreateChild(root, "ActiveVisual");
                    GameObject empty = CreateChild(root, "EmptyVisual");
                    Image icon = CreateChild(active, "Icon").AddComponent<Image>();
                    TMP_Text count = CreateText(root, "CountText");
                    companions[i] = new UI_RunBuildSummaryView.CompanionSlotBinding();
                    SetField(companions[i], "_root", root);
                    SetField(companions[i], "_activeVisual", active);
                    SetField(companions[i], "_emptyVisual", empty);
                    SetField(companions[i], "_icon", icon);
                    SetField(companions[i], "_countText", count);
                    CompanionRoots[i] = root;
                    CompanionActiveVisuals[i] = active;
                    CompanionEmptyVisuals[i] = empty;
                    CompanionIcons[i] = icon;
                    CompanionCountTexts[i] = count;
                }

                UI_RunBuildSummaryView.PassiveSlotBinding[] passives =
                    new UI_RunBuildSummaryView.PassiveSlotBinding[5];
                for (int i = 0; i < passives.Length; i++)
                {
                    GameObject root = CreateChild(_root, $"Passive_{i:00}");
                    GameObject active = CreateChild(root, "ActiveVisual");
                    GameObject empty = CreateChild(root, "EmptyVisual");
                    Image icon = CreateChild(active, "Icon").AddComponent<Image>();
                    TMP_Text level = CreateText(root, "LevelText");
                    passives[i] = new UI_RunBuildSummaryView.PassiveSlotBinding();
                    SetField(passives[i], "_root", root);
                    SetField(passives[i], "_activeVisual", active);
                    SetField(passives[i], "_emptyVisual", empty);
                    SetField(passives[i], "_icon", icon);
                    SetField(passives[i], "_levelText", level);
                    PassiveActiveVisuals[i] = active;
                    PassiveEmptyVisuals[i] = empty;
                    PassiveIcons[i] = icon;
                    PassiveLevelTexts[i] = level;
                }

                Synergy = CreateText(_root, "SynergySummaryText");
                SetField(View, "_companionSlots", companions);
                SetField(View, "_passiveSlots", passives);
                SetField(View, "_synergySummaryText", Synergy);
            }

            public RunResultViewData CreateView(
                System.Collections.Generic.IReadOnlyList<PauseCompanionPresentation> companions,
                System.Collections.Generic.IReadOnlyList<PausePassivePresentation> passives,
                System.Collections.Generic.IReadOnlyList<PauseSynergyPresentation> synergies,
                bool isClear = false,
                RunResultBestSynergyPresentation best = null)
            {
                return new RunResultViewData(
                    isClear, isClear ? "승리" : "실패", "", "1-1", "", isClear ? "전투 준비 계속" : "재도전", false, "", 1f, 1, 1,
                    "", "", "", 0, false, "", "", "", "",
                    System.Array.Empty<int>(), System.Array.Empty<RunResultSquadSlotView>(),
                    companions, passives, synergies, best);
            }

            public Sprite CreateSprite()
            {
                Texture2D texture = new Texture2D(2, 2);
                Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.one * 0.5f);
                return sprite;
            }

            public void ClearPassiveBindings()
            {
                SetField(View, "_passiveSlots", new UI_RunBuildSummaryView.PassiveSlotBinding[5]);
            }

            public void Dispose() => Object.DestroyImmediate(_root);

            private static void SetField(object target, string name, object value)
            {
                target.GetType().GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .SetValue(target, value);
            }

            private static GameObject CreateChild(GameObject parent, string name)
            {
                GameObject child = new GameObject(name);
                child.transform.SetParent(parent.transform, false);
                return child;
            }

            private static TMP_Text CreateText(GameObject parent, string name)
            {
                return CreateChild(parent, name).AddComponent<TextMeshProUGUI>();
            }
        }
    }
}
