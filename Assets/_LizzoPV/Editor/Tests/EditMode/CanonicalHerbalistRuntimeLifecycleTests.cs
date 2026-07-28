using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using Lizzo.PV.Legion;
using Lizzo.PV.Legion.Party.Roster;
using Lizzo.PV.P0.Presentation;
using Lizzo.PV.P0.Visuals;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class CanonicalHerbalistRuntimeLifecycleTests
    {
        private const string BaseId = "field_herbalist";
        private const string PromotedId = "battle_apothecary";
        private const string BaseAddress = "Lizzo/Characters/Companions/field_herbalist";

        private static readonly FieldInfo ActiveProviderField = typeof(PresentationCatalogProvider)
            .GetField("_active", BindingFlags.Static | BindingFlags.NonPublic);

        private PresentationCatalogProvider _previousProvider;
        private GameObject _providerRoot;
        private PresentationCatalog _testCatalog;

        [SetUp]
        public void SetUp()
        {
            _previousProvider = ActiveProviderField.GetValue(null) as PresentationCatalogProvider;
            InstallCatalogProvider();
        }

        [TearDown]
        public void TearDown()
        {
            if (_providerRoot != null)
                UnityEngine.Object.DestroyImmediate(_providerRoot);
            if (_testCatalog != null)
                UnityEngine.Object.DestroyImmediate(_testCatalog);
            ActiveProviderField.SetValue(null, _previousProvider);
            _providerRoot = null;
            _testCatalog = null;
            _previousProvider = null;
        }

        [Test]
        public void RecruitReinforcePromote_UsesCanonicalActorsRosterFormationAndCombat()
        {
            using CanonicalPartyFixture fixture = new CanonicalPartyFixture();
            PartyService party = fixture.Run.Party;

            Assert.IsTrue(party.RecruitCanonical(BaseId));
            Assert.AreEqual(1, party.ActiveCompanionCount);
            Assert.AreEqual(1, fixture.Factory.LiveInstances.Count);
            AssertSlot(party, 1, false);
            Assert.AreEqual(BaseId, fixture.Factory.LiveInstances[0].GetComponent<CompanionRuntime>().BaseUnitId);
            Assert.AreEqual(BaseId, fixture.Factory.LiveInstances[0].GetComponent<CompanionRuntime>().UnitId);

            Assert.IsTrue(party.RecruitCanonical(BaseId));
            Assert.AreEqual(2, party.ActiveCompanionCount);
            Assert.AreEqual(2, fixture.Factory.LiveInstances.Count);
            AssertSlot(party, 2, false);

            Assert.IsTrue(party.RecruitCanonical(BaseId));
            Assert.AreEqual(1, party.ActiveCompanionCount);
            Assert.AreEqual(1, fixture.Factory.LiveInstances.Count);
            AssertSlot(party, 3, true);

            CompanionRuntime promoted = fixture.Factory.LiveInstances[0].GetComponent<CompanionRuntime>();
            AllyCombat combat = promoted.GetComponent<AllyCombat>();
            Assert.AreEqual(BaseId, promoted.BaseUnitId);
            Assert.AreEqual(PromotedId, promoted.UnitId);
            Assert.AreEqual("rear_center_01", promoted.SlotId);
            Assert.AreEqual(1, promoted.GetComponentsInChildren<CompanionRuntime>(true).Length);
            Assert.AreEqual(2, promoted.transform.Find("SupportVisuals").childCount);
            Assert.AreEqual(12, combat.Damage);
            Assert.AreEqual(1.26f, combat.AttackPeriod, 0.0001f);
            Assert.AreEqual(6, combat.SecondaryHealAmount);
            Assert.AreEqual(5.0f, combat.SecondaryHealPeriod, 0.0001f);
            Assert.AreEqual(2, combat.SecondaryHealMaxTargets);
            Assert.AreEqual(0.60f, combat.SecondaryHealSecondTargetRatio, 0.0001f);
            Assert.IsTrue(combat.HasPromotedProjectileBounce);
            Assert.AreEqual(1.8f, combat.PromotedProjectileBounce.Radius, 0.0001f);
            Assert.AreEqual(1, combat.PromotedProjectileBounce.MaxTargets);
            Assert.AreEqual(0.60f, combat.PromotedProjectileBounce.DamageRatio, 0.0001f);
            Assert.AreEqual(BaseAddress, fixture.Factory.SpawnedAddresses[0]);
        }

        [Test]
        public void CatalogSpawnAndSetupFailures_DoNotCommitOrReplaceCanonicalRoster()
        {
            ClearCatalogProvider();
            using (CanonicalPartyFixture missingCatalog = new CanonicalPartyFixture())
            {
                LogAssert.Expect(LogType.Error, new Regex("^Canonical companion spawn failed: field_herbalist Canonical companion presentation is missing: field_herbalist$"));
                Assert.IsFalse(missingCatalog.Run.Party.RecruitCanonical(BaseId));
                AssertUnchanged(missingCatalog.Run.Party, missingCatalog.Factory, 0);
            }
            InstallCatalogProvider();

            using (CanonicalPartyFixture spawnFailure = new CanonicalPartyFixture())
            {
                spawnFailure.Factory.FailSpawn = true;
                LogAssert.Expect(LogType.Error, new Regex("^Canonical companion spawn failed: field_herbalist Companion prefab is missing: Lizzo/Characters/Companions/field_herbalist$"));
                Assert.IsFalse(spawnFailure.Run.Party.RecruitCanonical(BaseId));
                AssertUnchanged(spawnFailure.Run.Party, spawnFailure.Factory, 1);
                Assert.AreEqual(BaseAddress, spawnFailure.Factory.SpawnedAddresses[0]);
            }

            using (CanonicalPartyFixture setupFailure = new CanonicalPartyFixture())
            {
                setupFailure.Factory.RemoveVisualBeforeReturn = true;
                LogAssert.Expect(LogType.Error, new Regex("^Canonical companion spawn failed: field_herbalist Companion prefab is missing required component: CommanderAllyVisual$"));
                Assert.IsFalse(setupFailure.Run.Party.RecruitCanonical(BaseId));
                AssertUnchanged(setupFailure.Run.Party, setupFailure.Factory, 1);
            }
        }

        private static void AssertSlot(PartyService party, int unitCount, bool promoted)
        {
            Assert.IsTrue(TryGetSlot(party, out SquadSlotState slot));
            Assert.AreEqual(unitCount, slot.CurrentCount);
            Assert.AreEqual(promoted, slot.IsPromoted);
            Assert.AreEqual(promoted ? PromotedId : BaseId, slot.LeaderUnitId);
        }

        private static void AssertUnchanged(PartyService party, TestPrefabFactory factory, int spawnCalls)
        {
            Assert.AreEqual(0, party.ActiveCompanionCount);
            Assert.AreEqual(0, party.ActiveCompanionSlotCount);
            Assert.AreEqual(spawnCalls, factory.SpawnedAddresses.Count);
            Assert.AreEqual(0, factory.LiveInstances.Count);
            Assert.IsFalse(TryGetSlot(party, out _));
        }

        private static bool TryGetSlot(PartyService party, out SquadSlotState slot)
        {
            IReadOnlyList<SquadSlotState> snapshot = party.GetSquadSlotSnapshot();
            for (int i = 0; i < snapshot.Count; i++)
            {
                if (snapshot[i].BaseUnitId == BaseId)
                {
                    slot = snapshot[i];
                    return true;
                }
            }

            slot = default;
            return false;
        }

        private void InstallCatalogProvider()
        {
            if (_providerRoot != null)
                UnityEngine.Object.DestroyImmediate(_providerRoot);

            UnitPresentationSet units = AssetDatabase.LoadAssetAtPath<UnitPresentationSet>("Assets/_LizzoPV/Data/Presentation/UnitPresentationSet.asset");
            Assert.IsNotNull(units);
            _testCatalog = ScriptableObject.CreateInstance<PresentationCatalog>();
            _testCatalog.SetPresentationSetsForEditor(null, null, null, null, units);
            _providerRoot = new GameObject("CanonicalHerbalistPresentationCatalog");
            _providerRoot.SetActive(false);
            PresentationCatalogProvider provider = _providerRoot.AddComponent<PresentationCatalogProvider>();
            SerializedObject serializedProvider = new SerializedObject(provider);
            serializedProvider.FindProperty("_catalog").objectReferenceValue = _testCatalog;
            serializedProvider.ApplyModifiedPropertiesWithoutUndo();
            ActiveProviderField.SetValue(null, provider);
            Assert.IsTrue(PresentationCatalogProvider.TryGetUnit(BaseId, out _));
        }

        private static void ClearCatalogProvider()
        {
            ActiveProviderField.SetValue(null, null);
        }

        private sealed class CanonicalPartyFixture : IDisposable
        {
            private readonly GameObject _root;

            public CanonicalPartyFixture()
            {
                _root = new GameObject("CanonicalHerbalistRuntimeLifecycle");
                Transform poolRoot = new GameObject("PoolRoot").transform;
                poolRoot.SetParent(_root.transform, false);
                Data = new FakeDataProvider();
                Data.InitializeAsync().GetAwaiter().GetResult();
                Assets = new TestAssetService();
                App = new AppServices(Assets, Data);
                Factory = new TestPrefabFactory();
                Registry = new RuntimeObjectRegistry(Factory);
                Run = new RunServices(App, new Lizzo.PV.Flow.RunState(), Registry, new ObjectPoolService(poolRoot), Factory);
                RetroSfx.Configure(Assets);
                RetroVfx.Configure(Assets, Factory);
                AttackVisual.Configure(Factory);
                FloatingDamageText.Configure(Factory);
                CreatePlayer();
            }

            public TestAssetService Assets { get; }
            public FakeDataProvider Data { get; }
            public AppServices App { get; }
            public TestPrefabFactory Factory { get; }
            public RuntimeObjectRegistry Registry { get; }
            public RunServices Run { get; }

            public void Dispose()
            {
                Run.Dispose();
                FloatingDamageText.ClearServices();
                AttackVisual.ClearServices();
                RetroVfx.ClearServices();
                RetroSfx.ClearServices();
                App.ReleaseAll();
                UnityEngine.Object.DestroyImmediate(_root);
            }

            private void CreatePlayer()
            {
                GameObject playerObject = new GameObject("CanonicalHerbalistCommander");
                playerObject.transform.SetParent(_root.transform, false);
                PlayerController player = playerObject.AddComponent<PlayerController>();
                player.MaxHp = 100;
                player.Hp = 100;
                Registry.RegisterPlayer(player);
            }
        }

        private sealed class TestPrefabFactory : IPrefabFactory
        {
            private readonly List<GameObject> _instances = new List<GameObject>();

            public readonly List<string> SpawnedAddresses = new List<string>();
            public IReadOnlyList<GameObject> LiveInstances => _instances;
            public bool FailSpawn { get; set; }
            public bool RemoveVisualBeforeReturn { get; set; }

            public GameObject Spawn(string address, Transform parent = null, bool pooled = false)
            {
                SpawnedAddresses.Add(address);
                if (FailSpawn)
                    return null;

                string unitId = address == BaseAddress ? BaseId : PromotedId;
                UnitPresentationSet set = AssetDatabase.LoadAssetAtPath<UnitPresentationSet>("Assets/_LizzoPV/Data/Presentation/UnitPresentationSet.asset");
                if (set == null || set.TryGetEntry(unitId, out UnitPresentationSet.Entry entry) == false)
                    return null;

                GameObject instance = UnityEngine.Object.Instantiate(entry.Prefab, parent);
                _instances.Add(instance);
                if (RemoveVisualBeforeReturn)
                    UnityEngine.Object.DestroyImmediate(instance.GetComponent<CommanderAllyVisual>());
                return instance;
            }

            public GameObject Rent(GameObject prefab, string poolKey, Transform parent = null) => null;

            public void Release(GameObject instance)
            {
                _instances.Remove(instance);
                if (instance != null)
                    UnityEngine.Object.DestroyImmediate(instance);
            }

            public void Clear()
            {
                for (int i = _instances.Count - 1; i >= 0; i--)
                    Release(_instances[i]);
            }
        }
    }
}
