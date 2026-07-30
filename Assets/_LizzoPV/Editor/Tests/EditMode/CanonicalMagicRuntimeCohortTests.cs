using Lizzo.PV.Legion;
using Lizzo.PV.Legion.Party.Roster;
using Lizzo.PV.P0.Units;
using Lizzo.PV.P0.Presentation;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using System.Reflection;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class CanonicalMagicRuntimeCohortTests
    {
        private static readonly FieldInfo ActiveProvider = typeof(PresentationCatalogProvider).GetField("_active", BindingFlags.Static | BindingFlags.NonPublic);
        private PresentationCatalogProvider _previous;
        private GameObject _providerRoot;
        private PresentationCatalog _catalog;

        [SetUp] public void SetUp(){ _previous=ActiveProvider.GetValue(null) as PresentationCatalogProvider; UnitPresentationSet units=AssetDatabase.LoadAssetAtPath<UnitPresentationSet>("Assets/_LizzoPV/Data/Presentation/UnitPresentationSet.asset"); _catalog=ScriptableObject.CreateInstance<PresentationCatalog>(); _catalog.SetPresentationSetsForEditor(null,null,null,null,units); _providerRoot=new GameObject("MagicCatalog"); _providerRoot.SetActive(false); var provider=_providerRoot.AddComponent<PresentationCatalogProvider>(); var so=new SerializedObject(provider); so.FindProperty("_catalog").objectReferenceValue=_catalog; so.ApplyModifiedPropertiesWithoutUndo(); ActiveProvider.SetValue(null,provider); }
        [TearDown] public void TearDown(){ if(_providerRoot!=null) Object.DestroyImmediate(_providerRoot); if(_catalog!=null) Object.DestroyImmediate(_catalog); ActiveProvider.SetValue(null,_previous); }
        [Test]
        public void Resolvers_KeepCanonicalBaseAndPromotionMagicContracts()
        {
            using ServiceTestFixture fixture = new ServiceTestFixture();
            Assert.IsTrue(new CompanionPersistentFieldCombatResolver(fixture.Data).TryResolve("fire_mage", 1.0f, out CompanionPersistentFieldCombatSetup fire));
            Assert.AreEqual(5, fire.Damage); Assert.AreEqual(3.2f, fire.Period, 0.0001f); Assert.AreEqual(1.6f, fire.Radius, 0.0001f); Assert.AreEqual(3.0f, fire.Duration, 0.0001f);
            CompanionPersistentFieldCombatSetup sage = fire.WithPromotedFireSageField(); Assert.AreEqual(1.8f, sage.Radius, 0.0001f); Assert.AreEqual(4.0f, sage.Duration, 0.0001f);
            Assert.IsTrue(new CompanionChainCombatResolver(fixture.Data).TryResolve("lightning_mage", 1.0f, out CompanionChainCombatSetup lightning));
            Assert.AreEqual(12, lightning.Damage); Assert.AreEqual(2.6f, lightning.Period, 0.0001f); Assert.AreEqual(3, lightning.MaxTargets);
            Assert.AreEqual(5, lightning.WithPromotedStormMageChain().MaxTargets);
        }

        [Test]
        public void CanonicalMagicPrefabs_HaveRuntimeContractAndPresentationOnlySupports()
        {
            AssertPrefab("FireMage", 0); AssertPrefab("FireSage", 2); AssertPrefab("LightningMage", 0); AssertPrefab("StormMage", 2);
        }

        [TestCase("fire_mage", "fire_sage", 1.8f, 4.0f, 0)]
        [TestCase("lightning_mage", "storm_mage", 0.0f, 0.0f, 5)]
        public void RecruitReinforcePromote_UsesCanonicalMagicActor(string baseId, string promotedId, float expectedRadius, float expectedDuration, int expectedChainTargets)
        {
            using var fixture = new CanonicalTargetAreaRuntimeLifecycleTests.Fixture();
            PartyService party=fixture.Run.Party;
            Assert.AreEqual(PartyRosterChangeResult.Recruit, party.PreviewCanonicalRecruit(baseId));
            Assert.IsTrue(party.RecruitCanonical(baseId)); Assert.IsTrue(party.RecruitCanonical(baseId)); Assert.IsTrue(party.RecruitCanonical(baseId));
            Assert.AreEqual(1, party.ActiveCompanionCount); Assert.AreEqual(1, fixture.Factory.LiveInstances.Count); var runtime=fixture.Factory.LiveInstances[0].GetComponent<CompanionRuntime>(); Assert.AreEqual(promotedId,runtime.UnitId); Assert.AreEqual(2,runtime.transform.Find("SupportVisuals").childCount);
            var combat=runtime.GetComponent<AllyCombat>(); if(baseId=="fire_mage"){ Assert.AreEqual(expectedRadius,combat.PersistentFieldSetup.Radius,0.0001f); Assert.AreEqual(expectedDuration,combat.PersistentFieldSetup.Duration,0.0001f); } else Assert.AreEqual(expectedChainTargets,combat.ChainSetup.MaxTargets);
        }

        private static void AssertPrefab(string name, int supportCount)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/_LizzoPV/Prefabs/Characters/Companions/{name}.prefab");
            Assert.IsNotNull(prefab); Assert.IsNotNull(prefab.GetComponent<AllyCombat>()); Assert.IsNotNull(prefab.GetComponent<AllyFollower>()); Assert.IsNotNull(prefab.GetComponent<CompanionRuntime>()); Assert.IsNotNull(prefab.GetComponent<CompanionHealthBar>());
            UnitColliderRefs refs=prefab.GetComponent<UnitColliderRefs>(); CompanionRuntime runtime=prefab.GetComponent<CompanionRuntime>(); Assert.IsNotNull(refs); Assert.AreSame(refs.BodyCollider,runtime.BodyCollider); Assert.AreSame(refs.CombatCollider,runtime.CombatCollider);
            Transform supports=prefab.transform.Find("SupportVisuals"); Assert.AreEqual(supportCount, supports == null ? 0 : supports.childCount); Assert.AreEqual(0, GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(prefab));
        }
    }
}
