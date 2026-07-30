using System;
using System.Collections.Generic;
using Lizzo.PV.Data;
using Lizzo.PV.Legion;
using Lizzo.PV.Legion.Synergy;
using Lizzo.PV.P0.Cards;
using Lizzo.PV.P0.Presentation;
using Lizzo.PV.Tests.Support;
using Lizzo.PV.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class PauseBuildSummaryPresentationResolverTests
    {
        static readonly System.Reflection.FieldInfo ActiveProvider = typeof(PresentationCatalogProvider)
            .GetField("_active", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

        PresentationCatalogProvider _previousProvider;
        PresentationCatalog _catalog;
        GameObject _providerRoot;
        Sprite _icon;

        [SetUp]
        public void SetUp()
        {
            _previousProvider = ActiveProvider.GetValue(null) as PresentationCatalogProvider;
            _icon = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
            _catalog = ScriptableObject.CreateInstance<PresentationCatalog>();
            _providerRoot = new GameObject("PauseBuildSummaryCatalog");
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
            if (_providerRoot != null)
                UnityEngine.Object.DestroyImmediate(_providerRoot);
            if (_catalog != null)
                UnityEngine.Object.DestroyImmediate(_catalog);
            if (_icon != null)
                UnityEngine.Object.DestroyImmediate(_icon);
            ActiveProvider.SetValue(null, _previousProvider);
        }

        [Test]
        public void CanonicalPassiveRosterResolvesFiveIconsAndLevels()
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

            CardPresentationSet cards = CreateCardPresentationSet(passiveIds.Length);
            _catalog.SetPresentationSetsForEditor(null, null, cards, null, null);
            PassiveRosterState roster = new PassiveRosterState();
            for (int i = 0; i < passiveIds.Length; i++)
                Assert.IsTrue(roster.TryApply(data.GetPassive(passiveIds[i]), out _));

            List<PauseCompanionPresentation> companions = new List<PauseCompanionPresentation>();
            List<PausePassivePresentation> passives = new List<PausePassivePresentation>();
            List<PauseSynergyPresentation> synergies = new List<PauseSynergyPresentation>();
            PauseBuildSummaryPresentationResolver.Fill(
                Array.Empty<SquadSlotState>(),
                roster,
                null,
                data,
                companions,
                passives,
                synergies,
                7,
                5,
                null);

            Assert.That(passives, Has.Count.EqualTo(5));
            for (int i = 0; i < passives.Count; i++)
            {
                Assert.That(passives[i].Icon, Is.SameAs(_icon));
                Assert.That(passives[i].Level, Is.EqualTo(1));
            }
        }

        [Test]
        public void ProjectProvider_ResolvesCanonicalSynergyDisplayNamesAndGuardTrio()
        {
            LocalDataProvider data = CreateProjectProvider();
            string[] ids =
            {
                SynergyActivationIds.GuardShockwave, SynergyActivationIds.ArcherRain,
                SynergyActivationIds.MagicChain, SynergyActivationIds.ExplosionChain,
                SynergyActivationIds.BeastHunt, SynergyActivationIds.UndeadSummon,
                SynergyActivationIds.HealingBond, SynergyActivationIds.MixedCommand,
            };
            string[] displayNames =
            {
                "근위대", "사격대", "마법단", "폭발단",
                "야수단", "망자단", "치유 결속", "연합 지휘",
            };
            for (int i = 0; i < ids.Length; i++)
            {
                SynergyData synergy = data.GetSynergy(ids[i]);
                Assert.IsNotNull(synergy, ids[i]);
                Assert.That(synergy.DisplayName, Is.EqualTo(displayNames[i]));
            }

            SynergyActivationState activations = new SynergyActivationState(data);
            activations.Refresh(new[]
            {
                new SquadSlotState("shield_slot", "shield_guard", string.Empty, 1, 3, false, "shield_guard"),
                new SquadSlotState("sword_slot", "sword_soldier", string.Empty, 1, 3, false, "sword_soldier"),
                new SquadSlotState("cleric_slot", "cleric", string.Empty, 1, 3, false, "cleric"),
            });

            List<PauseCompanionPresentation> companions = new List<PauseCompanionPresentation>();
            List<PausePassivePresentation> passives = new List<PausePassivePresentation>();
            List<PauseSynergyPresentation> synergies = new List<PauseSynergyPresentation>();
            PauseBuildSummaryPresentationResolver.Fill(
                Array.Empty<SquadSlotState>(),
                null,
                activations,
                data,
                companions,
                passives,
                synergies,
                7,
                5,
                null);

            Assert.That(activations.ActiveCount, Is.EqualTo(1));
            Assert.That(synergies, Has.Count.EqualTo(1));
            Assert.That(synergies[0].Id, Is.EqualTo(SynergyActivationIds.GuardShockwave));
            Assert.That(synergies[0].DisplayName, Is.EqualTo("근위대"));
        }

        [Test]
        public void FallbackProvider_PreservesLegacyGuardAndCanonicalSynergyNames()
        {
            LogAssert.Expect(LogType.Error, "[LocalDataProvider] Local data asset was not available. address=PlayerData.xml");
            LocalDataProvider data = new LocalDataProvider(new TestAssetService());
            Assert.IsTrue(data.InitializeAsync().GetAwaiter().GetResult().Succeeded);

            Assert.That(data.GetSynergy("guard_squad").DisplayName, Is.EqualTo("근위대"));
            Assert.That(data.GetSynergy(SynergyActivationIds.ArcherRain).DisplayName, Is.EqualTo("사격대"));
            Assert.That(data.GetSynergy(SynergyActivationIds.MixedCommand).DisplayName, Is.EqualTo("연합 지휘"));
        }

        [Test]
        public void MissingPassivePresentationUsesProvisionalLevelWithoutError()
        {
            FakeDataProvider data = new FakeDataProvider();
            data.InitializeAsync().GetAwaiter().GetResult();
            PassiveData passive = CreatePassive("passive_melee_training");
            data.SetPassive(passive);
            PassiveRosterState roster = new PassiveRosterState();
            Assert.IsTrue(roster.TryApply(passive, out _));
            List<PauseCompanionPresentation> companions = new List<PauseCompanionPresentation>();
            List<PausePassivePresentation> passives = new List<PausePassivePresentation>();
            List<PauseSynergyPresentation> synergies = new List<PauseSynergyPresentation>();
            PauseBuildSummaryPresentationResolver.Fill(
                Array.Empty<SquadSlotState>(), roster, null, data,
                companions, passives, synergies, 7, 5, null);

            Assert.That(passives, Has.Count.EqualTo(1));
            Assert.That(passives[0].Icon, Is.Null);
            Assert.That(passives[0].Level, Is.EqualTo(1));
        }

        [Test]
        public void MissingPassiveDataLogsErrorAndLeavesThatItemBlank()
        {
            FakeDataProvider data = new FakeDataProvider();
            data.InitializeAsync().GetAwaiter().GetResult();
            PassiveData passive = CreatePassive("unknown_passive");
            PassiveRosterState roster = new PassiveRosterState();
            Assert.IsTrue(roster.TryApply(passive, out _));
            LogAssert.Expect(LogType.Error, "[PauseBuildSummaryPresentationResolver] Missing passive data: unknown_passive");

            List<PauseCompanionPresentation> companions = new List<PauseCompanionPresentation>();
            List<PausePassivePresentation> passives = new List<PausePassivePresentation>();
            List<PauseSynergyPresentation> synergies = new List<PauseSynergyPresentation>();
            PauseBuildSummaryPresentationResolver.Fill(
                Array.Empty<SquadSlotState>(), roster, null, data,
                companions, passives, synergies, 7, 5, null);

            Assert.That(passives, Has.Count.EqualTo(1));
            Assert.That(passives[0].Icon, Is.Null);
            Assert.That(passives[0].Level, Is.EqualTo(0));
        }

        static PassiveData CreatePassive(string id)
        {
            return new PassiveData
            {
                Id = id,
                Category = "test",
                EligibleTarget = "test",
                EffectId = "test_effect",
                ValueType = "test",
                Level1Value = 1f,
                Level2Value = 2f,
                Level3Value = 3f,
                StackRule = "test",
                TitleKo = id,
                TitleEn = id,
                DescriptionTemplateKo = id,
                OfferWeightRule = "test",
                Prohibition = "test",
            };
        }

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
            TextAsset gameData = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_LizzoPV/Data/Runtime/GameData.xml");
            Assert.IsNotNull(gameData);
            assets.Register("PlayerData.xml", gameData);
            LocalDataProvider data = new LocalDataProvider(assets);
            Assert.IsTrue(data.InitializeAsync().GetAwaiter().GetResult().Succeeded);
            return data;
        }
    }
}
