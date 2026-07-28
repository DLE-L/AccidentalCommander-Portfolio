using System;
using System.Collections.Generic;
using System.Reflection;
using Lizzo.PV.Legion;
using Lizzo.PV.P0.Presentation;
using Lizzo.PV.P0.Visuals;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class CanonicalNecromancerRuntimeCohortTests
    {
        private const string BaseId = "necromancer";
        private const string PromotedId = "dark_ritualist";
        private const string BaseAddress = "Lizzo/Characters/Companions/necromancer";
        private const string PromotedAddress = "Lizzo/Characters/Companions/dark_ritualist";

        private static readonly FieldInfo ActiveProviderField = typeof(PresentationCatalogProvider)
            .GetField("_active", BindingFlags.Static | BindingFlags.NonPublic);

        private PresentationCatalogProvider _previousProvider;
        private PresentationCatalog _catalog;
        private GameObject _providerRoot;

        [SetUp]
        public void SetUp()
        {
            _previousProvider = ActiveProviderField.GetValue(null) as PresentationCatalogProvider;
            UnitPresentationSet units = AssetDatabase.LoadAssetAtPath<UnitPresentationSet>("Assets/_LizzoPV/Data/Presentation/UnitPresentationSet.asset");
            Assert.IsNotNull(units);
            _catalog = ScriptableObject.CreateInstance<PresentationCatalog>();
            _catalog.SetPresentationSetsForEditor(null, null, null, null, units);
            _providerRoot = new GameObject("NecromancerPresentationCatalog");
            _providerRoot.SetActive(false);
            PresentationCatalogProvider provider = _providerRoot.AddComponent<PresentationCatalogProvider>();
            SerializedObject serialized = new SerializedObject(provider);
            serialized.FindProperty("_catalog").objectReferenceValue = _catalog;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            ActiveProviderField.SetValue(null, provider);
        }

        [TearDown]
        public void TearDown()
        {
            if (_providerRoot != null) UnityEngine.Object.DestroyImmediate(_providerRoot);
            if (_catalog != null) UnityEngine.Object.DestroyImmediate(_catalog);
            ActiveProviderField.SetValue(null, _previousProvider);
        }

        [Test]
        public void CanonicalRecruitReinforcePromote_UsesNecromancerCatalogActorsAndStableRosterSlot()
        {
            using Fixture fixture = new Fixture();
            PartyService party = fixture.Run.Party;

            Assert.AreEqual(Lizzo.PV.Legion.Party.Roster.PartyRosterChangeResult.Recruit, party.PreviewCanonicalRecruit(BaseId));
            Assert.IsTrue(party.RecruitCanonical(BaseId));
            Assert.AreEqual(1, party.ActiveCompanionCount);
            Assert.AreEqual(BaseAddress, fixture.Factory.SpawnedAddresses[0]);
            CompanionRuntime first = fixture.Factory.LiveInstances[0].GetComponent<CompanionRuntime>();
            Assert.AreEqual(BaseId, first.BaseUnitId);
            Assert.AreEqual("squad_00", first.RosterSlotId);

            Assert.IsTrue(party.RecruitCanonical(BaseId));
            Assert.AreEqual(2, party.ActiveCompanionCount);
            Assert.IsTrue(party.RecruitCanonical(BaseId));
            Assert.AreEqual(1, party.ActiveCompanionCount);
            CompanionRuntime promoted = fixture.Factory.LiveInstances[0].GetComponent<CompanionRuntime>();
            Assert.AreEqual(BaseId, promoted.BaseUnitId);
            Assert.AreEqual(PromotedId, promoted.UnitId);
            Assert.AreEqual("squad_00", promoted.RosterSlotId);
            Assert.AreEqual(PromotedAddress, fixture.Factory.SpawnedAddresses[2]);
            Assert.AreEqual(2, promoted.transform.Find("SupportVisuals").childCount);
            Assert.AreEqual(1, promoted.GetComponentsInChildren<CompanionRuntime>(true).Length);
            AllyCombat combat = promoted.GetComponent<AllyCombat>();
            Assert.AreEqual("necromancer", combat.CombatSourceId);
            Assert.AreEqual(14, combat.Damage);
            Assert.AreEqual(3.15f, combat.AttackPeriod, 0.0001f);
            Assert.AreEqual(5.3f, combat.AttackRange, 0.0001f);
            Assert.AreEqual(1, combat.MaxProjectileTargetCount);
            Assert.AreEqual(0.15f, combat.NoTargetRetrySeconds, 0.0001f);
        }

        [Test]
        public void Resolver_UsesExactBaseCurseSetupAndPromotedRangeSpecialization()
        {
            FakeDataProvider data = new FakeDataProvider();
            data.InitializeAsync().GetAwaiter().GetResult();
            CompanionProjectileCombatResolver resolver = new CompanionProjectileCombatResolver(data);
            Assert.IsTrue(resolver.TryResolve(BaseId, 1.0f, out CompanionProjectileCombatSetup baseSetup));
            Assert.AreEqual(8, baseSetup.Damage);
            Assert.AreEqual(3.0f, baseSetup.Period, 0.0001f);
            Assert.AreEqual(5.0f, baseSetup.Range, 0.0001f);
            Assert.AreEqual(AllyAttackStyle.TargetedProjectile, baseSetup.AttackStyle);
            Assert.AreEqual(1, baseSetup.MaxTargets);
            Assert.AreEqual(0.15f, baseSetup.NoTargetRetrySeconds, 0.0001f);
            Assert.AreEqual(5.3f, baseSetup.WithPromotedDarkRitualistRange().Range, 0.0001f);
        }

        [Test]
        public void Prefabs_ExposeCanonicalRuntimeContractAndPromotedSupportsStayPresentationOnly()
        {
            AssertPrefab("Assets/_LizzoPV/Prefabs/Characters/Companions/Necromancer.prefab", false);
            AssertPrefab("Assets/_LizzoPV/Prefabs/Characters/Companions/DarkRitualist.prefab", true);
        }

        private static void AssertPrefab(string path, bool promoted)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.IsNotNull(prefab);
            Assert.IsNotNull(prefab.GetComponent<AllyCombat>());
            Assert.IsNotNull(prefab.GetComponent<AllyFollower>());
            CompanionRuntime runtime = prefab.GetComponent<CompanionRuntime>();
            Assert.IsNotNull(runtime);
            Assert.IsNotNull(prefab.GetComponent<CompanionHealthBar>());
            Assert.IsNotNull(runtime.BodyCollider);
            Assert.IsNotNull(runtime.CombatCollider);
            Assert.AreEqual(promoted ? 2 : 0, prefab.transform.Find("SupportVisuals") == null ? 0 : prefab.transform.Find("SupportVisuals").childCount);
        }

        private sealed class Fixture : IDisposable
        {
            private readonly GameObject _root = new GameObject("NecromancerRuntimeFixture");
            public readonly TestFactory Factory = new TestFactory();
            public readonly RunServices Run;

            public Fixture()
            {
                Transform poolRoot = new GameObject("PoolRoot").transform;
                poolRoot.SetParent(_root.transform, false);
                FakeDataProvider data = new FakeDataProvider();
                data.InitializeAsync().GetAwaiter().GetResult();
                TestAssetService assets = new TestAssetService();
                AppServices app = new AppServices(assets, data);
                RuntimeObjectRegistry registry = new RuntimeObjectRegistry(Factory);
                Run = new RunServices(app, new Lizzo.PV.Flow.RunState(), registry, new ObjectPoolService(poolRoot), Factory);
                RetroSfx.Configure(assets); RetroVfx.Configure(assets, Factory); AttackVisual.Configure(Factory); FloatingDamageText.Configure(Factory);
                GameObject playerObject = new GameObject("Commander"); playerObject.transform.SetParent(_root.transform, false);
                PlayerController player = playerObject.AddComponent<PlayerController>(); player.MaxHp = 100; player.Hp = 100; registry.RegisterPlayer(player);
            }

            public void Dispose()
            {
                Run.Dispose();
                FloatingDamageText.ClearServices(); AttackVisual.ClearServices(); RetroVfx.ClearServices(); RetroSfx.ClearServices();
                UnityEngine.Object.DestroyImmediate(_root);
            }
        }

        private sealed class TestFactory : IPrefabFactory
        {
            public readonly List<string> SpawnedAddresses = new List<string>();
            public readonly List<GameObject> LiveInstances = new List<GameObject>();
            public GameObject Spawn(string address, Transform parent = null, bool pooled = false)
            {
                SpawnedAddresses.Add(address);
                string unitId = address == BaseAddress ? BaseId : PromotedId;
                UnitPresentationSet set = AssetDatabase.LoadAssetAtPath<UnitPresentationSet>("Assets/_LizzoPV/Data/Presentation/UnitPresentationSet.asset");
                if (set == null || set.TryGetEntry(unitId, out UnitPresentationSet.Entry entry) == false) return null;
                GameObject instance = UnityEngine.Object.Instantiate(entry.Prefab, parent); LiveInstances.Add(instance); return instance;
            }
            public GameObject Rent(GameObject prefab, string poolKey, Transform parent = null) => null;
            public void Release(GameObject instance) { LiveInstances.Remove(instance); if (instance != null) UnityEngine.Object.DestroyImmediate(instance); }
            public void Clear() { for (int i = LiveInstances.Count - 1; i >= 0; i--) Release(LiveInstances[i]); }
        }
    }
}
