using System;
using System.Collections.Generic;
using System.Reflection;
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
    public sealed class CanonicalTargetAreaRuntimeLifecycleTests
    {
        private static readonly FieldInfo ActiveProvider = typeof(PresentationCatalogProvider).GetField("_active", BindingFlags.Static | BindingFlags.NonPublic);
        private PresentationCatalogProvider _previous;
        private GameObject _providerRoot;
        private PresentationCatalog _catalog;

        [SetUp] public void SetUp() { _previous = ActiveProvider.GetValue(null) as PresentationCatalogProvider; InstallCatalog(); }
        [TearDown] public void TearDown() { if (_providerRoot != null) UnityEngine.Object.DestroyImmediate(_providerRoot); if (_catalog != null) UnityEngine.Object.DestroyImmediate(_catalog); ActiveProvider.SetValue(null, _previous); }

        [TestCase("bombardier", "powder_captain", "Lizzo/Characters/Companions/bombardier", 2.0f, 8, 0.4f)]
        [TestCase("skeleton_bomber", "bone_artillery", "Lizzo/Characters/Companions/skeleton_bomber", 1.5f, 6, 0.0f)]
        public void RecruitReinforcePromote_CommitsOnlyAfterCanonicalTargetAreaSpawn(string baseId, string promotedId, string baseAddress, float expectedRadius, int expectedMaxTargets, float expectedPush)
        {
            using Fixture fixture = new Fixture();
            PartyService party = fixture.Run.Party;
            Assert.AreEqual(PartyRosterChangeResult.Recruit, party.PreviewCanonicalRecruit(baseId));
            Assert.IsTrue(party.RecruitCanonical(baseId));
            Assert.AreEqual(1, party.ActiveCompanionCount);
            Assert.AreEqual(baseAddress, fixture.Factory.SpawnedAddresses[0]);
            Assert.IsTrue(party.RecruitCanonical(baseId));
            Assert.AreEqual(2, party.ActiveCompanionCount);
            Assert.IsTrue(party.RecruitCanonical(baseId));
            Assert.AreEqual(1, party.ActiveCompanionCount);
            Assert.AreEqual(1, fixture.Factory.LiveInstances.Count);
            CompanionRuntime runtime = fixture.Factory.LiveInstances[0].GetComponent<CompanionRuntime>();
            Assert.AreEqual(baseId, runtime.BaseUnitId);
            Assert.AreEqual(promotedId, runtime.UnitId);
            Assert.AreEqual(1, runtime.GetComponentsInChildren<CompanionRuntime>(true).Length);
            Assert.AreEqual(2, runtime.transform.Find("SupportVisuals").childCount);
            Assert.IsTrue(party.GetSquadSlotSnapshot()[0].IsPromoted);
            AllyCombat combat = runtime.GetComponent<AllyCombat>();
            Assert.AreEqual(expectedRadius, combat.TargetAreaRadius, 0.0001f);
            Assert.AreEqual(expectedMaxTargets, combat.TargetAreaMaxTargets);
            Assert.AreEqual(expectedPush, combat.TargetAreaNormalPush, 0.0001f);
            if (baseId == "skeleton_bomber") Assert.IsTrue(combat.HasPromotedTargetAreaFollowUp);
        }

        [Test]
        public void FailedSpawn_DoesNotCommitBombardierRoster()
        {
            using Fixture fixture = new Fixture();
            fixture.Factory.FailSpawn = true;
            LogAssert.Expect(LogType.Error, "Canonical companion spawn failed: bombardier Companion prefab is missing: Lizzo/Characters/Companions/bombardier");
            Assert.IsFalse(fixture.Run.Party.RecruitCanonical("bombardier"));
            Assert.AreEqual(0, fixture.Run.Party.ActiveCompanionCount);
            Assert.AreEqual(0, fixture.Run.Party.ActiveCompanionSlotCount);
        }

        private void InstallCatalog()
        {
            UnitPresentationSet units = AssetDatabase.LoadAssetAtPath<UnitPresentationSet>("Assets/_LizzoPV/Data/Presentation/UnitPresentationSet.asset");
            OwnedSupportPresentationSet supports = AssetDatabase.LoadAssetAtPath<OwnedSupportPresentationSet>("Assets/_LizzoPV/Data/Presentation/OwnedSupportPresentationSet.asset");
            _catalog = ScriptableObject.CreateInstance<PresentationCatalog>(); _catalog.SetPresentationSetsForEditor(null, null, null, null, units, supports);
            _providerRoot = new GameObject("TargetAreaCatalog"); _providerRoot.SetActive(false);
            PresentationCatalogProvider provider = _providerRoot.AddComponent<PresentationCatalogProvider>();
            SerializedObject so = new SerializedObject(provider); so.FindProperty("_catalog").objectReferenceValue = _catalog; so.ApplyModifiedPropertiesWithoutUndo(); ActiveProvider.SetValue(null, provider);
        }

        internal sealed class Fixture : IDisposable
        {
            private readonly GameObject _root = new GameObject("TargetAreaFixture");
            public readonly FakeDataProvider Data = new FakeDataProvider(); public readonly TestPrefabFactory Factory = new TestPrefabFactory(); public readonly AppServices App; public readonly RunServices Run;
            public Fixture() { Data.InitializeAsync().GetAwaiter().GetResult(); var assets=new TestAssetService(); App=new AppServices(assets,Data); var registry=new RuntimeObjectRegistry(Factory); Run=new RunServices(App,new Lizzo.PV.Flow.RunState(),registry,new ObjectPoolService(new GameObject("Pool").transform),Factory); RetroSfx.Configure(assets); RetroVfx.Configure(assets,Factory); AttackVisual.Configure(Factory); FloatingDamageText.Configure(Factory); var player=_root.AddComponent<PlayerController>(); player.MaxHp=100; player.Hp=100; registry.RegisterPlayer(player); }
            public void Dispose(){ Run.Dispose(); FloatingDamageText.ClearServices(); AttackVisual.ClearServices(); RetroVfx.ClearServices(); RetroSfx.ClearServices(); App.ReleaseAll(); UnityEngine.Object.DestroyImmediate(_root); }
        }
        internal sealed class TestPrefabFactory : IPrefabFactory
        {
            public bool FailSpawn; public readonly List<string> SpawnedAddresses=new List<string>(); public readonly List<GameObject> LiveInstances=new List<GameObject>();
            public GameObject Spawn(string address, Transform parent=null, bool pooled=false){ SpawnedAddresses.Add(address); if(FailSpawn)return null; string id=address.Substring(address.LastIndexOf('/')+1); UnitPresentationSet set=AssetDatabase.LoadAssetAtPath<UnitPresentationSet>("Assets/_LizzoPV/Data/Presentation/UnitPresentationSet.asset"); if(!set.TryGetEntry(id,out UnitPresentationSet.Entry entry)) return null; var instance=UnityEngine.Object.Instantiate(entry.Prefab,parent); LiveInstances.Add(instance); return instance; }
            public GameObject Rent(GameObject prefab,string poolKey,Transform parent=null)=>null;
            public void Release(GameObject instance){ LiveInstances.Remove(instance); if(instance!=null)UnityEngine.Object.DestroyImmediate(instance); }
            public void Clear(){ for(int i=LiveInstances.Count-1;i>=0;i--)Release(LiveInstances[i]); }
        }
    }
}
