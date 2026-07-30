using System;
using System.Collections.Generic;
using System.Reflection;
using Lizzo.PV.Data;
using Lizzo.PV.Legion;
using Lizzo.PV.P0.Cards;
using Lizzo.PV.P0.Presentation;
using Lizzo.PV.P0.Visuals;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Lizzo.PV.EditorTests
{
    public sealed class FixedCardPoolCompatibilityRecruitTests
    {
        [SetUp]
        public void IgnoreAuthoredVisualLogNoise()
        {
            LogAssert.ignoreFailingMessages = true;
        }

        [OneTimeTearDown]
        public void RestoreAuthoredVisualLogHandling()
        {
            LogAssert.ignoreFailingMessages = false;
        }

        [TestCase(CardKind.AddShieldSoldier, "shield_guard", "shield_family")]
        [TestCase(CardKind.RecruitSwordsman, "sword_soldier", "sword_family")]
        [TestCase(CardKind.RecruitCleric, "cleric", "cleric_family")]
        public void TryApplyCard_CompatibilityCanonicalBaseUnit_RecruitsConfiguredRuntime(
            CardKind kind,
            string baseUnitId,
            string requiredFamilyTag)
        {
            using CompatibilityCardFixture fixture = new CompatibilityCardFixture();
            FixedCardPool.Configure(fixture.Run.Registry, fixture.Run.Party);
            CardEffectRuntime.Configure(fixture.Run.Registry, fixture.Run.Party);

            try
            {
                CardData card = new CardData(kind, "legacy", "legacy", CardHighlight.New, baseUnitId);

                Assert.IsTrue(FixedCardPool.TryApplyCard(card));
                Assert.AreEqual(1, fixture.Run.Party.ActiveCompanionSlotCount);
                Assert.AreEqual(1, fixture.Run.Party.ActiveCompanionCount);
                Assert.AreEqual(1, fixture.Factory.LiveCompanionCount);

                CompanionRuntime runtime = fixture.Factory.GetOnlyLiveCompanion();
                Assert.AreEqual(baseUnitId, runtime.BaseUnitId);
                Assert.IsFalse(string.IsNullOrEmpty(runtime.RosterSlotId));
                AssertMatchesActiveRosterSlot(fixture.Run.Party, runtime);
                StringAssert.Contains(requiredFamilyTag, runtime.FamilyTags);
            }
            finally
            {
                FixedCardPool.ClearServices();
                CardEffectRuntime.ClearServices();
            }
        }

        [Test]
        public void TryApplyCard_FalconCanonicalBaseUnit_StaysOnCanonicalRuntimePath()
        {
            using CompatibilityCardFixture fixture = new CompatibilityCardFixture();
            FixedCardPool.Configure(fixture.Run.Registry, fixture.Run.Party);
            CardEffectRuntime.Configure(fixture.Run.Registry, fixture.Run.Party);

            try
            {
                CardData card = new CardData(CardKind.RecruitArcher, "legacy", "legacy", CardHighlight.New, "falcon_archer");

                Assert.IsTrue(FixedCardPool.TryApplyCard(card));
                Assert.AreEqual(1, fixture.Run.Party.ActiveCompanionSlotCount);
                Assert.AreEqual(1, fixture.Factory.LiveCompanionCount);
                Assert.AreEqual("falcon_archer", fixture.Factory.GetOnlyLiveCompanion().BaseUnitId);
            }
            finally
            {
                FixedCardPool.ClearServices();
                CardEffectRuntime.ClearServices();
            }
        }

        [Test]
        public void TryApplyCard_CompatibilityReinforcement_ReusesImmutableRosterSlotId()
        {
            using CompatibilityCardFixture fixture = new CompatibilityCardFixture();
            FixedCardPool.Configure(fixture.Run.Registry, fixture.Run.Party);
            CardEffectRuntime.Configure(fixture.Run.Registry, fixture.Run.Party);

            try
            {
                CardData card = new CardData(CardKind.RecruitSwordsman, "legacy", "legacy", CardHighlight.New, "sword_soldier");
                Assert.IsTrue(FixedCardPool.TryApplyCard(card));
                CompanionRuntime first = fixture.Factory.GetOnlyLiveCompanion();
                Assert.IsTrue(FixedCardPool.TryApplyCard(card));

                List<CompanionRuntime> reinforcements = fixture.Factory.GetLiveCompanions("sword_soldier");
                Assert.AreEqual(2, reinforcements.Count);
                Assert.AreEqual(first.RosterSlotId, reinforcements[0].RosterSlotId);
                Assert.AreEqual(first.RosterSlotId, reinforcements[1].RosterSlotId);
                AssertMatchesActiveRosterSlot(fixture.Run.Party, reinforcements[0]);
            }
            finally
            {
                FixedCardPool.ClearServices();
                CardEffectRuntime.ClearServices();
            }
        }

        [TestCase(CardKind.RecruitSwordsman, "sword_soldier", "sword_captain", "sword_family")]
        [TestCase(CardKind.RecruitCleric, "cleric", "light_guide", "cleric_family")]
        public void TryApplyCard_CompatibilityPromotion_ReplacesPriorBaseActors(
            CardKind kind,
            string baseUnitId,
            string promotedUnitId,
            string requiredFamilyTag)
        {
            using CompatibilityCardFixture fixture = new CompatibilityCardFixture();
            FixedCardPool.Configure(fixture.Run.Registry, fixture.Run.Party);
            CardEffectRuntime.Configure(fixture.Run.Registry, fixture.Run.Party);

            try
            {
                CardData card = new CardData(kind, "legacy", "legacy", CardHighlight.New, baseUnitId);
                Assert.IsTrue(FixedCardPool.TryApplyCard(card));
                CompanionRuntime first = fixture.Factory.GetOnlyLiveCompanion();
                string rosterSlotId = first.RosterSlotId;

                Assert.IsTrue(FixedCardPool.TryApplyCard(card));
                Assert.IsTrue(FixedCardPool.TryApplyCard(card));

                Assert.AreEqual(1, fixture.Run.Party.ActiveCompanionCount);
                Assert.AreEqual(1, fixture.Factory.LiveCompanionCount);
                List<CompanionRuntime> sameBaseRuntimes = fixture.Factory.GetLiveCompanions(baseUnitId);
                Assert.AreEqual(1, sameBaseRuntimes.Count);
                CompanionRuntime promoted = sameBaseRuntimes[0];
                Assert.AreEqual(baseUnitId, promoted.UnitId);
                Assert.IsTrue(promoted.IsPromoted);
                Assert.AreEqual(rosterSlotId, promoted.RosterSlotId);
                AssertMatchesActiveRosterSlot(fixture.Run.Party, promoted);
                StringAssert.Contains(requiredFamilyTag, promoted.FamilyTags);
                Assert.IsTrue(fixture.Factory.SpawnedAddresses.Contains("Lizzo/Characters/Companions/" + promotedUnitId));
            }
            finally
            {
                FixedCardPool.ClearServices();
                CardEffectRuntime.ClearServices();
            }
        }

        [Test]
        public void TryApplyCard_ShieldPromotionReplacement_RetainsImmutableRosterSlotId()
        {
            using CompatibilityCardFixture fixture = new CompatibilityCardFixture();
            FixedCardPool.Configure(fixture.Run.Registry, fixture.Run.Party);
            CardEffectRuntime.Configure(fixture.Run.Registry, fixture.Run.Party);

            try
            {
                CardData card = new CardData(CardKind.AddShieldSoldier, "legacy", "legacy", CardHighlight.New, "shield_guard");
                Assert.IsTrue(FixedCardPool.TryApplyCard(card));
                CompanionRuntime first = fixture.Factory.GetOnlyLiveCompanion();
                Assert.IsTrue(FixedCardPool.TryApplyCard(card));
                Assert.IsTrue(FixedCardPool.TryApplyCard(card));

                CompanionRuntime captain = fixture.Factory.GetPromotedLiveCompanion();
                Assert.AreEqual(first.RosterSlotId, captain.RosterSlotId);
                AssertMatchesActiveRosterSlot(fixture.Run.Party, captain);
            }
            finally
            {
                FixedCardPool.ClearServices();
                CardEffectRuntime.ClearServices();
            }
        }

        static void AssertMatchesActiveRosterSlot(PartyService party, CompanionRuntime runtime)
        {
            string activeSlotId = null;
            IReadOnlyList<Lizzo.PV.Legion.SquadSlotState> snapshot = party.GetSquadSlotSnapshot();
            for (int i = 0; i < snapshot.Count; i++)
            {
                if (snapshot[i].IsActive == false)
                    continue;

                Assert.IsNull(activeSlotId);
                activeSlotId = snapshot[i].SlotId;
            }

            Assert.AreEqual(activeSlotId, runtime.RosterSlotId);
        }

        sealed class CompatibilityCardFixture : IDisposable
        {
            static readonly FieldInfo ActiveProvider = typeof(PresentationCatalogProvider).GetField("_active", BindingFlags.Static | BindingFlags.NonPublic);

            readonly GameObject _root = new GameObject("CompatibilityCardFixture");
            readonly PresentationCatalogProvider _previousProvider;
            readonly PresentationCatalog _catalog;
            readonly GameObject _providerRoot;
            readonly TestAssetService _assets = new TestAssetService();

            public readonly LocalDataProvider Data;
            public readonly CompatibilityCardFactory Factory = new CompatibilityCardFactory();
            public readonly AppServices App;
            public readonly RunServices Run;

            public CompatibilityCardFixture()
            {
                _previousProvider = ActiveProvider.GetValue(null) as PresentationCatalogProvider;
                UnitPresentationSet units = AssetDatabase.LoadAssetAtPath<UnitPresentationSet>("Assets/_LizzoPV/Data/Presentation/UnitPresentationSet.asset");
                OwnedSupportPresentationSet supports = AssetDatabase.LoadAssetAtPath<OwnedSupportPresentationSet>("Assets/_LizzoPV/Data/Presentation/OwnedSupportPresentationSet.asset");
                _catalog = ScriptableObject.CreateInstance<PresentationCatalog>();
                _catalog.SetPresentationSetsForEditor(null, null, null, null, units, supports);
                _providerRoot = new GameObject("CompatibilityCardCatalog");
                _providerRoot.SetActive(false);
                PresentationCatalogProvider provider = _providerRoot.AddComponent<PresentationCatalogProvider>();
                SerializedObject serialized = new SerializedObject(provider);
                serialized.FindProperty("_catalog").objectReferenceValue = _catalog;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                ActiveProvider.SetValue(null, provider);

                _assets.Register("PlayerData.xml", AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_LizzoPV/Data/Runtime/GameData.xml"));
                Data = new LocalDataProvider(_assets);
                Assert.That(Data.InitializeAsync().GetAwaiter().GetResult().Succeeded, Is.True);
                App = new AppServices(_assets, Data);
                Run = new RunServices(App, new Lizzo.PV.Flow.RunState(), new RuntimeObjectRegistry(Factory), new ObjectPoolService(new GameObject("CompatibilityCardPool").transform), Factory);
                RetroSfx.Configure(_assets);
                RetroVfx.Configure(_assets, Factory);
                AttackVisual.Configure(Factory);
                FloatingDamageText.Configure(Factory);
                PlayerController player = _root.AddComponent<PlayerController>();
                player.MaxHp = 100;
                player.Hp = 100;
                Run.Registry.RegisterPlayer(player);
            }

            public void Dispose()
            {
                Run.Dispose();
                FloatingDamageText.ClearServices();
                AttackVisual.ClearServices();
                RetroVfx.ClearServices();
                RetroSfx.ClearServices();
                App.ReleaseAll();
                Factory.Clear();
                if (_providerRoot != null)
                    UnityEngine.Object.DestroyImmediate(_providerRoot);
                if (_catalog != null)
                    UnityEngine.Object.DestroyImmediate(_catalog);
                ActiveProvider.SetValue(null, _previousProvider);
                UnityEngine.Object.DestroyImmediate(_root);
            }
        }

        sealed class CompatibilityCardFactory : IPrefabFactory
        {
            static readonly string[] RuntimeUnitIds = { "shield_guard", "shield_captain", "sword_soldier", "sword_captain", "cleric", "light_guide", "falcon_archer" };
            readonly UnitPresentationSet _units = AssetDatabase.LoadAssetAtPath<UnitPresentationSet>("Assets/_LizzoPV/Data/Presentation/UnitPresentationSet.asset");
            readonly List<GameObject> _liveInstances = new List<GameObject>();

            public readonly List<string> SpawnedAddresses = new List<string>();

            public int LiveCompanionCount
            {
                get
                {
                    int count = 0;
                    for (int i = 0; i < _liveInstances.Count; i++)
                    {
                        if (_liveInstances[i] != null && _liveInstances[i].GetComponent<CompanionRuntime>() != null)
                            count++;
                    }

                    return count;
                }
            }

            public GameObject Spawn(string address, Transform parent = null, bool pooled = false)
            {
                SpawnedAddresses.Add(address);
                if (address == "FloatingDamageText.prefab")
                {
                    GameObject label = new GameObject(address);
                    if (parent != null)
                        label.transform.SetParent(parent, false);
                    label.AddComponent<TextMeshPro>();
                    label.AddComponent<FloatingDamageText>();
                    _liveInstances.Add(label);
                    return label;
                }

                for (int i = 0; i < RuntimeUnitIds.Length; i++)
                {
                    if (_units.TryGetEntry(RuntimeUnitIds[i], out UnitPresentationSet.Entry entry) == false
                        || entry.AddressableKey != address)
                    {
                        continue;
                    }

                    GameObject instance = UnityEngine.Object.Instantiate(entry.Prefab, parent);
                    _liveInstances.Add(instance);
                    return instance;
                }

                return null;
            }

            public GameObject Rent(GameObject prefab, string poolKey, Transform parent = null) => Spawn(poolKey, parent, pooled: true);

            public void Release(GameObject instance)
            {
                _liveInstances.Remove(instance);
                if (instance != null)
                    UnityEngine.Object.DestroyImmediate(instance);
            }

            public void Clear()
            {
                for (int i = _liveInstances.Count - 1; i >= 0; i--)
                    Release(_liveInstances[i]);
            }

            public CompanionRuntime GetOnlyLiveCompanion()
            {
                CompanionRuntime runtime = null;
                for (int i = 0; i < _liveInstances.Count; i++)
                {
                    CompanionRuntime candidate = _liveInstances[i] == null ? null : _liveInstances[i].GetComponent<CompanionRuntime>();
                    if (candidate == null)
                        continue;

                    Assert.IsNull(runtime);
                    runtime = candidate;
                }

                Assert.IsNotNull(runtime);
                return runtime;
            }

            public List<CompanionRuntime> GetLiveCompanions(string baseUnitId)
            {
                List<CompanionRuntime> companions = new List<CompanionRuntime>();
                for (int i = 0; i < _liveInstances.Count; i++)
                {
                    CompanionRuntime runtime = _liveInstances[i] == null ? null : _liveInstances[i].GetComponent<CompanionRuntime>();
                    if (runtime != null && runtime.BaseUnitId == baseUnitId)
                        companions.Add(runtime);
                }

                return companions;
            }

            public CompanionRuntime GetPromotedLiveCompanion()
            {
                CompanionRuntime promoted = null;
                for (int i = 0; i < _liveInstances.Count; i++)
                {
                    CompanionRuntime runtime = _liveInstances[i] == null ? null : _liveInstances[i].GetComponent<CompanionRuntime>();
                    if (runtime == null || runtime.IsPromoted == false)
                        continue;

                    Assert.IsNull(promoted);
                    promoted = runtime;
                }

                Assert.IsNotNull(promoted);
                return promoted;
            }
        }
    }
}
