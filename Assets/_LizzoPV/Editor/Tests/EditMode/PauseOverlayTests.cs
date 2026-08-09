using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Lizzo.PV.Data;
using Lizzo.PV.Flow;
using Lizzo.PV.Legion;
using Lizzo.PV.Legion.Synergy;
using Lizzo.PV.P0.Cards;
using Lizzo.PV.P0.Presentation;
using Lizzo.PV.Tests.Support;
using Lizzo.PV.UI;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Lizzo.PV.EditorTests
{
    /// <summary>
    /// Current Pause UI resolver, overlay, Gameplay-scene, and pause-state contracts.
    /// </summary>
    public sealed class PauseOverlayTests
    {
        const string GameplayScenePath = "Assets/_LizzoPV/Scenes/Gameplay.unity";
        const string SemiBoldFontPath = "Assets/_LizzoPV/Fonts/Pretendard/TMP/Pretendard-SemiBold SDF.asset";
        const string InfoContentPath = "GameplayUIRoot/HUD/PauseOverlay/Panel/InfoContent";

        static readonly FieldInfo ActiveProvider = typeof(PresentationCatalogProvider)
            .GetField("_active", BindingFlags.Static | BindingFlags.NonPublic);

        PresentationCatalogProvider _previousProvider;
        PresentationCatalog _catalog;
        GameObject _providerRoot;
        Sprite _icon;
        CardPresentationSet _cardSet;

        [SetUp]
        public void SetUp()
        {
            _previousProvider = ActiveProvider.GetValue(null) as PresentationCatalogProvider;
            UnitPresentationSet units = AssetDatabase.LoadAssetAtPath<UnitPresentationSet>(
                "Assets/_LizzoPV/Data/Presentation/UnitPresentationSet.asset");
            Assert.That(units, Is.Not.Null);

            _catalog = ScriptableObject.CreateInstance<PresentationCatalog>();
            _catalog.SetPresentationSetsForEditor(null, null, null, null, units);
            _providerRoot = new GameObject("PauseOverlayPresentationCatalog");
            _providerRoot.SetActive(false);
            PresentationCatalogProvider provider = _providerRoot.AddComponent<PresentationCatalogProvider>();
            SerializedObject serialized = new SerializedObject(provider);
            serialized.FindProperty("_catalog").objectReferenceValue = _catalog;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            ActiveProvider.SetValue(null, provider);
        }

        [TearDown]
        public void TearDown()
        {
            if (_cardSet != null)
                UnityEngine.Object.DestroyImmediate(_cardSet);
            if (_providerRoot != null)
                UnityEngine.Object.DestroyImmediate(_providerRoot);
            if (_catalog != null)
                UnityEngine.Object.DestroyImmediate(_catalog);
            if (_icon != null)
                UnityEngine.Object.DestroyImmediate(_icon);
            ActiveProvider.SetValue(null, _previousProvider);
        }

        [Test]
        public void BuildSummaryResolver_FillsFivePassivesAndHandlesMissingData()
        {
            FakeDataProvider data = new FakeDataProvider();
            data.InitializeAsync().GetAwaiter().GetResult();
            string[] passiveIds =
            {
                "passive_melee_training", "passive_frontline_tempo", "passive_ranged_training",
                "passive_projectile_speed", "passive_long_range",
            };
            for (int i = 0; i < passiveIds.Length; i++)
                data.SetPassive(CreatePassive(passiveIds[i]));

            _icon = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
            _cardSet = CreateCardPresentationSet(passiveIds.Length);
            _catalog.SetPresentationSetsForEditor(null, null, _cardSet, null, null);
            PassiveRosterState roster = new PassiveRosterState();
            for (int i = 0; i < passiveIds.Length; i++)
                Assert.IsTrue(roster.TryApply(data.GetPassive(passiveIds[i]), out _));

            List<PauseCompanionPresentation> companions = new List<PauseCompanionPresentation>();
            List<PausePassivePresentation> passives = new List<PausePassivePresentation>();
            List<PauseSynergyPresentation> synergies = new List<PauseSynergyPresentation>();
            PauseBuildSummaryPresentationResolver.Fill(
                Array.Empty<SquadSlotState>(), roster, null, data, companions, passives, synergies, 7, 5, null);

            Assert.That(passives, Has.Count.EqualTo(5));
            for (int i = 0; i < passives.Count; i++)
            {
                Assert.That(passives[i].Icon, Is.SameAs(_icon));
                Assert.That(passives[i].Level, Is.EqualTo(1));
            }

            PassiveData missingPresentationData = CreatePassive("missing_presentation_passive");
            data.SetPassive(missingPresentationData);
            PassiveRosterState missingPresentationRoster = new PassiveRosterState();
            Assert.IsTrue(missingPresentationRoster.TryApply(missingPresentationData, out _));
            passives.Clear();
            PauseBuildSummaryPresentationResolver.Fill(
                Array.Empty<SquadSlotState>(), missingPresentationRoster, null, data, companions, passives, synergies, 7, 5, null);
            Assert.That(passives, Has.Count.EqualTo(1));
            Assert.That(passives[0].Icon, Is.Null);
            Assert.That(passives[0].Level, Is.EqualTo(1));

            FakeDataProvider missingDataProvider = new FakeDataProvider();
            missingDataProvider.InitializeAsync().GetAwaiter().GetResult();
            PassiveData unknownPassive = CreatePassive("unknown_passive");
            PassiveRosterState missingDataRoster = new PassiveRosterState();
            Assert.IsTrue(missingDataRoster.TryApply(unknownPassive, out _));
            LogAssert.Expect(LogType.Error, "[PauseBuildSummaryPresentationResolver] Missing passive data: unknown_passive");
            passives.Clear();
            PauseBuildSummaryPresentationResolver.Fill(
                Array.Empty<SquadSlotState>(), missingDataRoster, null, missingDataProvider, companions, passives, synergies, 7, 5, null);
            Assert.That(passives, Has.Count.EqualTo(1));
            Assert.That(passives[0].Icon, Is.Null);
            Assert.That(passives[0].Level, Is.EqualTo(0));
        }

        [Test]
        public void SynergyResolver_UsesCanonicalLocalizedNamesAndGuardPresentation()
        {
            LocalDataProvider data = CreateProjectProvider();
            string[] ids =
            {
                SynergyActivationIds.GuardShockwave, SynergyActivationIds.ArcherRain,
                SynergyActivationIds.MagicChain, SynergyActivationIds.ExplosionChain,
                SynergyActivationIds.BeastHunt, SynergyActivationIds.UndeadSummon,
                SynergyActivationIds.HealingBond, SynergyActivationIds.MixedCommand,
            };
            string[] displayNames = { "근위대", "사격대", "마법단", "폭발단", "야수단", "망자단", "치유 결속", "연합 지휘" };
            for (int i = 0; i < ids.Length; i++)
                Assert.That(data.GetSynergy(ids[i]).DisplayName, Is.EqualTo(displayNames[i]));

            SynergyActivationState activations = new SynergyActivationState(data);
            activations.Refresh(new[]
            {
                new SquadSlotState("shield_slot", "shield_guard", string.Empty, 1, 3, false, "shield_guard"),
                new SquadSlotState("sword_slot", "sword_soldier", string.Empty, 1, 3, false, "sword_soldier"),
                new SquadSlotState("cleric_slot", "cleric", string.Empty, 1, 3, false, "cleric"),
            });
            List<PauseSynergyPresentation> synergies = new List<PauseSynergyPresentation>();
            PauseBuildSummaryPresentationResolver.Fill(
                Array.Empty<SquadSlotState>(), null, activations, data,
                new List<PauseCompanionPresentation>(), new List<PausePassivePresentation>(), synergies, 7, 5, null);
            Assert.AreEqual(1, activations.ActiveCount);
            Assert.That(synergies, Has.Count.EqualTo(1));
            Assert.AreEqual(SynergyActivationIds.GuardShockwave, synergies[0].Id);
            Assert.AreEqual("근위대", synergies[0].DisplayName);

            LogAssert.Expect(LogType.Error, "[LocalDataProvider] Local data asset was not available. address=PlayerData.xml");
            LocalDataProvider fallback = new LocalDataProvider(new TestAssetService());
            Assert.IsTrue(fallback.InitializeAsync().GetAwaiter().GetResult().Succeeded);
            Assert.AreEqual("근위대", fallback.GetSynergy("guard_squad").DisplayName);
            Assert.AreEqual("사격대", fallback.GetSynergy(SynergyActivationIds.ArcherRain).DisplayName);
            Assert.AreEqual("연합 지휘", fallback.GetSynergy(SynergyActivationIds.MixedCommand).DisplayName);
        }

        [Test]
        public void CompanionResolver_FillsCanonicalPortraitsAndReportsMissingUnits()
        {
            Assert.IsTrue(PresentationCatalogProvider.TryGetUnit("shield_guard", out UnitPresentationSet.Entry shieldGuard));
            Assert.That(shieldGuard.Portrait, Is.Not.Null);

            List<PauseCompanionPresentation> presentations = new List<PauseCompanionPresentation>();
            SquadSlotState[] states = CreateCanonicalSquadSnapshot();
            PauseCompanionPresentationResolver.Fill(states, presentations, 7, null);
            Assert.That(presentations, Has.Count.EqualTo(7));
            Assert.That(presentations[0].Icon, Is.SameAs(shieldGuard.Portrait));
            Assert.AreEqual(2, presentations[0].CurrentCount);
            for (int i = 1; i < presentations.Count; i++)
            {
                Assert.That(presentations[i].Icon, Is.Null);
                Assert.AreEqual(0, presentations[i].CurrentCount);
            }

            states[0] = new SquadSlotState("squad_00", "missing_unit", string.Empty, 1, 3, false, "missing_unit");
            LogAssert.Expect(LogType.Error,
                "[GameplayUIController] Missing UnitPresentationSet entry for active companion: missing_unit (roster slot: squad_00)");
            presentations.Clear();
            PauseCompanionPresentationResolver.Fill(states, presentations, 7, null);
            Assert.IsNull(presentations[0].Icon);
            Assert.AreEqual(1, presentations[0].CurrentCount);
        }

        [Test]
        public void PauseOverlay_PresentsZeroOneManySynergiesAndSevenFiveSlots()
        {
            using OverlayFixture fixture = new OverlayFixture();
            Sprite rosterSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
            try
            {
                fixture.Overlay.Present(
                    false, CreateCompanionPresentations(rosterSprite, 7), CreatePassivePresentations(rosterSprite, 5),
                    Enumerable.Range(0, 8).Select(index => new PauseSynergyPresentation("synergy_" + index, "시너지 " + index)).ToArray());
                Assert.AreEqual(8, fixture.SynergyList.childCount);
                Assert.IsTrue(fixture.SynergyList.Cast<Transform>().All(child => child.gameObject.activeSelf));
                Assert.AreEqual("시너지 7", fixture.SynergyList.GetChild(7).GetComponentInChildren<TMP_Text>().text);
                Assert.IsFalse(fixture.SynergyEmptyState.gameObject.activeSelf);
                Assert.AreEqual("동료 7 / 7", fixture.CompanionCount.text);
                Assert.AreEqual("패시브 5 / 5", fixture.PassiveCount.text);

                fixture.Overlay.Present(
                    false, new[] { new PauseCompanionPresentation(rosterSprite, 2), new PauseCompanionPresentation(rosterSprite, 1) },
                    CreatePassivePresentations(rosterSprite, 2), new[] { new PauseSynergyPresentation("guard_squad", "근위대") });
                Assert.IsTrue(fixture.CompanionRoots.All(root => root.activeSelf));
                Assert.IsTrue(fixture.CompanionEmptyRoots[2].activeSelf);
                Assert.IsTrue(fixture.PassiveRoots.All(root => root.activeSelf));
                Assert.IsTrue(fixture.PassiveEmptyRoots[2].activeSelf);
                Assert.AreEqual("동료 2 / 7", fixture.CompanionCount.text);
                Assert.AreEqual("패시브 2 / 5", fixture.PassiveCount.text);
                Assert.IsTrue(fixture.SynergyList.GetChild(0).gameObject.activeSelf);
                Assert.IsFalse(fixture.SynergyList.GetChild(1).gameObject.activeSelf);

                fixture.Overlay.Present(
                    false, new[] { new PauseCompanionPresentation(null, 3) }, new[] { new PausePassivePresentation(null, 2) },
                    Array.Empty<PauseSynergyPresentation>());
                Assert.AreEqual("동료 1 / 7", fixture.CompanionCount.text);
                Assert.AreEqual("패시브 1 / 5", fixture.PassiveCount.text);
                Assert.IsTrue(fixture.SynergyEmptyState.gameObject.activeSelf);
                Assert.AreEqual("활성 시너지 없음", fixture.SynergyEmptyState.text);
                Assert.IsFalse(fixture.CompanionIcons[0].enabled);
                Assert.IsFalse(fixture.PassiveIcons[0].enabled);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(rosterSprite);
            }
        }

        [Test]
        public void PauseSynergyItem_RejectsRaycastOnDecorativeNameText()
        {
            GameObject root = new GameObject("SynergyItemTestRoot");
            GameObject textObject = new GameObject("NameText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(root.transform, false);
            UI_PauseSynergyItem item = root.AddComponent<UI_PauseSynergyItem>();
            TMP_Text nameText = textObject.GetComponent<TMP_Text>();
            SetField(item, "_nameText", nameText);
            nameText.raycastTarget = true;
            try
            {
                LogAssert.Expect(LogType.Error, new Regex("\\[UI_PauseSynergyItem\\] Decorative graphics must not raycast\\."));
                Assert.IsFalse(item.Validate());
                nameText.raycastTarget = false;
                Assert.IsTrue(item.Validate());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void GameplayPauseOverlay_UsesSemanticResponsiveSceneContract()
        {
            Scene gameplayScene = EditorSceneManager.GetSceneByPath(GameplayScenePath);
            bool openedGameplayScene = false;
            if (!gameplayScene.isLoaded)
            {
                gameplayScene = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Additive);
                openedGameplayScene = true;
            }
            try
            {
                Transform gameplayUiRoot = gameplayScene.GetRootGameObjects().Single(root => root.name == "GameplayUIRoot").transform;
                Transform infoContent = FindTransform(gameplayUiRoot, "HUD/PauseOverlay/Panel/InfoContent");
                Transform synergyBody = FindTransform(gameplayUiRoot, "HUD/PauseOverlay/Panel/InfoContent/SynergySection/SynergyBody");
                Transform synergyList = FindTransform(gameplayUiRoot, "HUD/PauseOverlay/Panel/InfoContent/SynergySection/SynergyBody/SynergyList");
                UI_PauseOverlay overlay = FindTransform(gameplayUiRoot, "HUD/PauseOverlay").GetComponent<UI_PauseOverlay>();
                Assert.IsNotNull(overlay);
                Assert.IsTrue(overlay.Validate());
                Assert.IsEmpty(infoContent.GetComponentsInChildren<ScrollRect>(true));
                Assert.IsEmpty(infoContent.GetComponentsInChildren<Scrollbar>(true));
                Assert.IsFalse(infoContent.GetComponent<Image>().raycastTarget);

                CanvasScaler scaler = gameplayUiRoot.Find("HUD").GetComponent<CanvasScaler>();
                Assert.AreEqual(new Vector2(1080f, 1920f), scaler.referenceResolution);

                TextMeshProUGUI emptyState = FindTransform(synergyBody, "SynergyEmptyStateText").GetComponent<TextMeshProUGUI>();
                Assert.AreEqual("활성 시너지 없음", emptyState.text);
                Assert.AreEqual(TextAlignmentOptions.Center, emptyState.alignment);
                Assert.IsFalse(emptyState.raycastTarget);
                Assert.AreEqual(SemiBoldFontPath, AssetDatabase.GetAssetPath(emptyState.font));
                Assert.IsTrue(new SerializedObject(overlay).FindProperty("_synergyEmptyStateText").objectReferenceValue == emptyState);

                Transform companionRow = FindTransform(infoContent, "CompanionSection/CompanionSlotRow");
                Transform passiveRow = FindTransform(infoContent, "PassiveSection/PassiveSlotRow");
                Assert.AreEqual(7, companionRow.childCount);
                Assert.AreEqual(5, passiveRow.childCount);
                AssertSlotRowIsLeftAligned(companionRow);
                AssertSlotRowIsLeftAligned(passiveRow);

                GridLayoutGroup synergyGrid = synergyList.GetComponent<GridLayoutGroup>();
                Assert.AreEqual(TextAnchor.UpperLeft, synergyGrid.childAlignment);
                Assert.AreEqual(GridLayoutGroup.Constraint.FixedColumnCount, synergyGrid.constraint);
                Assert.AreEqual(2, synergyGrid.constraintCount);
                Assert.LessOrEqual(synergyGrid.cellSize.x * 2f + synergyGrid.spacing.x + synergyGrid.padding.horizontal,
                    synergyList.GetComponent<RectTransform>().rect.width + 0.5f);

                SerializedObject overlaySerialized = new SerializedObject(overlay);
                Assert.AreEqual(7, overlaySerialized.FindProperty("_companionIconImages").arraySize);
                Assert.AreEqual(7, overlaySerialized.FindProperty("_companionSlotCountTexts").arraySize);
                Assert.AreEqual(5, overlaySerialized.FindProperty("_passiveIconImages").arraySize);
                Assert.AreEqual(5, overlaySerialized.FindProperty("_passiveLevelTexts").arraySize);
                AssertPassiveEmptyVisuals(passiveRow, overlaySerialized.FindProperty("_passiveEmptySlotRoots"));
            }
            finally
            {
                if (openedGameplayScene)
                    EditorSceneManager.CloseScene(gameplayScene, true);
            }
        }

        [Test]
        [Category("FtueRunPause")]
        public void RunPauseController_PreservesModalSpeedAndRunEndState()
        {
            GameObject gameObject = new GameObject(nameof(PauseOverlayTests));
            try
            {
                RunPauseController controller = gameObject.AddComponent<RunPauseController>();
                controller.Initialize();
                Assert.AreEqual(1.0f, controller.SelectedGameplaySpeed);
                Assert.IsTrue(controller.ToggleGameplaySpeed());
                Assert.AreEqual(5.0f, controller.SelectedGameplaySpeed);
                controller.SetModalOpen(true);
                Assert.IsFalse(controller.ToggleGameplaySpeed());
                Assert.AreEqual(0.0f, Time.timeScale);
                controller.SetModalOpen(false);
                Assert.AreEqual(5.0f, Time.timeScale);
                controller.MarkRunEnded();
                Assert.IsTrue(controller.IsPaused);
                Assert.AreEqual(1.0f, controller.SelectedGameplaySpeed);
                Assert.AreEqual(0.0f, Time.timeScale);
            }
            finally
            {
                Time.timeScale = 1.0f;
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        static PassiveData CreatePassive(string id) => new PassiveData
        {
            Id = id, Category = "test", EligibleTarget = "test", EffectId = "test_effect", ValueType = "test",
            Level1Value = 1f, Level2Value = 2f, Level3Value = 3f, StackRule = "test", TitleKo = id, TitleEn = id,
            DescriptionTemplateKo = id, OfferWeightRule = "test", Prohibition = "test",
        };

        CardPresentationSet CreateCardPresentationSet(int count)
        {
            CardPresentationSet cards = ScriptableObject.CreateInstance<CardPresentationSet>();
            SerializedObject serialized = new SerializedObject(cards);
            SerializedProperty entries = serialized.FindProperty("_entries");
            entries.arraySize = count;
            for (int i = 0; i < count; i++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("_id").stringValue = ((CardKind)((int)CardKind.PassiveMeleeTraining + i)).ToString();
                entry.FindPropertyRelative("_icon").objectReferenceValue = _icon;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return cards;
        }

        static LocalDataProvider CreateProjectProvider()
        {
            TestAssetService assets = new TestAssetService();
            assets.Register("PlayerData.xml", AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_LizzoPV/Gameplay/RunData/Data/GameData.xml"));
            LocalDataProvider data = new LocalDataProvider(assets);
            Assert.IsTrue(data.InitializeAsync().GetAwaiter().GetResult().Succeeded);
            return data;
        }

        static SquadSlotState[] CreateCanonicalSquadSnapshot()
        {
            SquadSlotState[] states = new SquadSlotState[7];
            states[0] = new SquadSlotState("squad_00", "shield_guard", string.Empty, 2, 3, false, "shield_guard");
            for (int i = 1; i < states.Length; i++)
                states[i] = new SquadSlotState("squad_" + i.ToString("00"), string.Empty, string.Empty, 0, 3, false, string.Empty);
            return states;
        }

        static PauseCompanionPresentation[] CreateCompanionPresentations(Sprite icon, int count)
        {
            PauseCompanionPresentation[] presentations = new PauseCompanionPresentation[count];
            for (int i = 0; i < count; i++)
                presentations[i] = new PauseCompanionPresentation(icon, 1);
            return presentations;
        }

        static PausePassivePresentation[] CreatePassivePresentations(Sprite icon, int count)
        {
            PausePassivePresentation[] presentations = new PausePassivePresentation[count];
            for (int i = 0; i < count; i++)
                presentations[i] = new PausePassivePresentation(icon, 1);
            return presentations;
        }

        static void AssertPassiveEmptyVisuals(Transform passiveRow, SerializedProperty emptyRoots)
        {
            Assert.AreEqual(5, emptyRoots.arraySize);
            for (int i = 0; i < 5; i++)
            {
                Transform slot = passiveRow.GetChild(i);
                Assert.AreEqual("PassiveSlot_0" + i, slot.name);
                Transform emptyVisual = slot.Find("EmptyVisual");
                Assert.IsNotNull(emptyVisual);
                Assert.AreSame(emptyVisual.gameObject, emptyRoots.GetArrayElementAtIndex(i).objectReferenceValue);
                Transform[] descendants = emptyVisual.GetComponentsInChildren<Transform>(true);
                Assert.IsFalse(descendants.Any(child => child != emptyVisual && child.name.IndexOf("lock", StringComparison.OrdinalIgnoreCase) >= 0));
                Assert.IsFalse(descendants.Any(child => child.name == "Icon" && child.gameObject.activeSelf && child.GetComponent<Image>()?.sprite != null));
            }
        }

        static void AssertSlotRowIsLeftAligned(Transform row)
        {
            RectTransform rowRect = row.GetComponent<RectTransform>();
            HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
            Assert.AreEqual(TextAnchor.MiddleLeft, layout.childAlignment);
            Assert.IsFalse(layout.childForceExpandWidth);
            Assert.IsFalse(layout.childForceExpandHeight);
            float previousMax = rowRect.rect.xMin + layout.padding.left;
            for (int i = 0; i < row.childCount; i++)
            {
                RectTransform slot = row.GetChild(i).GetComponent<RectTransform>();
                Bounds bounds = GetLocalBounds(rowRect, slot);
                Assert.IsTrue(IsContained(rowRect, slot));
                Assert.GreaterOrEqual(bounds.min.x, previousMax - 0.5f);
                previousMax = bounds.max.x + layout.spacing;
            }
        }

        static Bounds GetLocalBounds(RectTransform parent, RectTransform child)
        {
            Vector3[] corners = new Vector3[4];
            child.GetWorldCorners(corners);
            Bounds bounds = new Bounds(parent.InverseTransformPoint(corners[0]), Vector3.zero);
            for (int i = 1; i < corners.Length; i++)
                bounds.Encapsulate(parent.InverseTransformPoint(corners[i]));
            return bounds;
        }

        static bool IsContained(RectTransform parent, RectTransform child)
        {
            Vector3[] corners = new Vector3[4];
            child.GetWorldCorners(corners);
            for (int i = 0; i < corners.Length; i++)
            {
                Vector3 local = parent.InverseTransformPoint(corners[i]);
                if (local.x < parent.rect.xMin - 0.5f || local.x > parent.rect.xMax + 0.5f
                    || local.y < parent.rect.yMin - 0.5f || local.y > parent.rect.yMax + 0.5f)
                    return false;
            }
            return true;
        }

    static Transform FindTransform(Transform root, string path)
    {
        Transform target = root.Find(path);
        Assert.That(target, Is.Not.Null, root.name + "/" + path);
        return target;
    }

    static void SetField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, "Missing field: " + name);
            field.SetValue(target, value);
        }

        sealed class OverlayFixture : IDisposable
        {
            public readonly GameObject Root;
            public readonly UI_PauseOverlay Overlay;
            public readonly Transform SynergyList;
            public readonly TMP_Text SynergyEmptyState;
            public readonly TMP_Text CompanionCount;
            public readonly TMP_Text PassiveCount;
            public readonly GameObject[] CompanionRoots;
            public readonly GameObject[] CompanionEmptyRoots;
            public readonly GameObject[] PassiveRoots;
            public readonly GameObject[] PassiveEmptyRoots;
            public readonly Image[] CompanionIcons;
            public readonly Image[] PassiveIcons;

            public OverlayFixture()
            {
                Root = new GameObject("PauseOverlayTestRoot");
                GameObject infoContent = new GameObject("InfoContent", typeof(RectTransform));
                GameObject synergySection = new GameObject("SynergySection", typeof(RectTransform));
                GameObject synergyBody = new GameObject("SynergyBody", typeof(RectTransform));
                GameObject synergyList = new GameObject("SynergyList", typeof(RectTransform));
                infoContent.transform.SetParent(Root.transform, false);
                synergySection.transform.SetParent(infoContent.transform, false);
                synergyBody.transform.SetParent(synergySection.transform, false);
                synergyList.transform.SetParent(synergyBody.transform, false);
                SynergyList = synergyList.transform;

                Overlay = Root.AddComponent<UI_PauseOverlay>();
                SetField(Overlay, "_root", Root);
                SetField(Overlay, "_rootCanvasGroup", Root.AddComponent<CanvasGroup>());
                SetField(Overlay, "_titleText", CreateText(Root.transform, "TitleText"));
                CompanionCount = CreateText(Root.transform, "CompanionCountText");
                PassiveCount = CreateText(Root.transform, "PassiveCountText");
                SetField(Overlay, "_companionCountText", CompanionCount);
                SetField(Overlay, "_passiveCountText", PassiveCount);

                CompanionRoots = CreateRoots(Root.transform, 7, "CompanionSlot_");
                GameObject[] companionFilled = CreateChildRoots(CompanionRoots, "Normal");
                CompanionEmptyRoots = CreateChildRoots(CompanionRoots, "Disable");
                SetField(Overlay, "_companionSlotRoots", CompanionRoots);
                SetField(Overlay, "_companionFilledSlotRoots", companionFilled);
                SetField(Overlay, "_companionEmptySlotRoots", CompanionEmptyRoots);
                SetField(Overlay, "_companionSlotCountTexts", CreateTexts(Root.transform, 7, "CompanionCount_"));
                CompanionIcons = CreateImages(Root.transform, 7, "CompanionIcon_");
                SetField(Overlay, "_companionIconImages", CompanionIcons);

                PassiveRoots = CreateRoots(Root.transform, 5, "PassiveSlot_");
                GameObject[] passiveFilled = CreateChildRoots(PassiveRoots, "Nomal");
                PassiveEmptyRoots = CreateChildRoots(PassiveRoots, "Lock");
                SetField(Overlay, "_passiveSlotRoots", PassiveRoots);
                SetField(Overlay, "_passiveFilledSlotRoots", passiveFilled);
                SetField(Overlay, "_passiveEmptySlotRoots", PassiveEmptyRoots);
                PassiveIcons = CreateImages(Root.transform, 5, "PassiveIcon_");
                SetField(Overlay, "_passiveIconImages", PassiveIcons);
                SetField(Overlay, "_passiveLevelTexts", CreateTexts(Root.transform, 5, "PassiveLevel_"));
                SynergyEmptyState = CreateText(Root.transform, "EmptyStateText");
                SetField(Overlay, "_synergyEmptyStateText", SynergyEmptyState);
                SetField(Overlay, "_synergyList", synergyList.GetComponent<RectTransform>());
                SetField(Overlay, "_synergyItemPrefab", AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_LizzoPV/Prefabs/UI/UI_PauseSynergyItem.prefab"));
                SetField(Overlay, "_lobbyButton", CreateButton(Root.transform, "LobbyButton"));
                SetField(Overlay, "_resumeButton", CreateButton(Root.transform, "ResumeButton"));
                Assert.IsTrue(Overlay.Configure(() => { }, () => { }));
                Assert.IsTrue(Overlay.Init());
            }

            public void Dispose() => UnityEngine.Object.DestroyImmediate(Root);

            static TMP_Text CreateText(Transform parent, string name)
            {
                GameObject text = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                text.transform.SetParent(parent, false);
                return text.GetComponent<TMP_Text>();
            }

            static TMP_Text[] CreateTexts(Transform parent, int count, string prefix)
            {
                TMP_Text[] texts = new TMP_Text[count];
                for (int i = 0; i < count; i++) texts[i] = CreateText(parent, prefix + i);
                return texts;
            }

            static GameObject[] CreateRoots(Transform parent, int count, string prefix)
            {
                GameObject[] roots = new GameObject[count];
                for (int i = 0; i < count; i++)
                {
                    roots[i] = new GameObject(prefix + i, typeof(RectTransform));
                    roots[i].transform.SetParent(parent, false);
                }
                return roots;
            }

            static GameObject[] CreateChildRoots(GameObject[] parents, string name)
            {
                GameObject[] roots = new GameObject[parents.Length];
                for (int i = 0; i < parents.Length; i++)
                {
                    roots[i] = new GameObject(name, typeof(RectTransform));
                    roots[i].transform.SetParent(parents[i].transform, false);
                }
                return roots;
            }

            static Image[] CreateImages(Transform parent, int count, string prefix)
            {
                Image[] images = new Image[count];
                for (int i = 0; i < count; i++)
                {
                    GameObject image = new GameObject(prefix + i, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    image.transform.SetParent(parent, false);
                    images[i] = image.GetComponent<Image>();
                }
                return images;
            }

            static Button CreateButton(Transform parent, string name)
            {
                GameObject button = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                button.transform.SetParent(parent, false);
                return button.GetComponent<Button>();
            }
        }
    }
}
