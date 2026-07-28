using Lizzo.PV.Legion;
using Lizzo.PV.Legion.Presentation;
using Lizzo.PV.Legion.Party.Roster;
using Lizzo.PV.P0.Presentation;
using NUnit.Framework;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class CanonicalWolfWraithRuntimeCohortTests
    {
        private static readonly FieldInfo ActiveProvider = typeof(PresentationCatalogProvider).GetField("_active", BindingFlags.Static | BindingFlags.NonPublic);
        private PresentationCatalogProvider _previous;
        private GameObject _providerRoot;
        private PresentationCatalog _catalog;

        [SetUp]
        public void SetUp()
        {
            _previous = ActiveProvider.GetValue(null) as PresentationCatalogProvider;
            UnitPresentationSet units = AssetDatabase.LoadAssetAtPath<UnitPresentationSet>("Assets/_LizzoPV/Data/Presentation/UnitPresentationSet.asset");
            OwnedSupportPresentationSet supports = AssetDatabase.LoadAssetAtPath<OwnedSupportPresentationSet>("Assets/_LizzoPV/Data/Presentation/OwnedSupportPresentationSet.asset");
            _catalog = ScriptableObject.CreateInstance<PresentationCatalog>();
            _catalog.SetPresentationSetsForEditor(null, null, null, null, units, supports);
            _providerRoot = new GameObject("WolfWraithCatalog");
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
            if (_providerRoot != null) Object.DestroyImmediate(_providerRoot);
            if (_catalog != null) Object.DestroyImmediate(_catalog);
            ActiveProvider.SetValue(null, _previous);
        }

        [TestCase("wolf_tamer", "beast_commander", true)]
        [TestCase("wraith_knight", "wraith_guardian", false)]
        public void RecruitReinforcePromote_UsesOneCanonicalPromotedActor(string baseUnitId, string promotedUnitId, bool expectsWolfPresenter)
        {
            using CanonicalTargetAreaRuntimeLifecycleTests.Fixture fixture = new CanonicalTargetAreaRuntimeLifecycleTests.Fixture();
            PartyService party = fixture.Run.Party;

            Assert.AreEqual(PartyRosterChangeResult.Recruit, party.PreviewCanonicalRecruit(baseUnitId));
            Assert.IsTrue(party.RecruitCanonical(baseUnitId));
            Assert.IsTrue(party.RecruitCanonical(baseUnitId));
            Assert.IsTrue(party.RecruitCanonical(baseUnitId));

            Assert.AreEqual(1, party.ActiveCompanionCount);
            Assert.AreEqual(1, fixture.Factory.LiveInstances.Count);
            CompanionRuntime runtime = fixture.Factory.LiveInstances[0].GetComponent<CompanionRuntime>();
            Assert.AreEqual(baseUnitId, runtime.BaseUnitId);
            Assert.AreEqual(promotedUnitId, runtime.UnitId);
            Assert.AreEqual(1, runtime.GetComponentsInChildren<CompanionRuntime>(true).Length);
            Assert.AreEqual(2, runtime.transform.Find("SupportVisuals").childCount);
            Assert.AreEqual(expectsWolfPresenter, runtime.GetComponent<OwnerBoundSupportPresenterBehaviour>() != null);

            AllyCombat combat = runtime.GetComponent<AllyCombat>();
            if (baseUnitId == "wolf_tamer")
            {
                Assert.AreEqual(2, combat.WolfOwnedProxySetup.HitCount);
                Assert.AreEqual(0.70f, combat.WolfOwnedProxySetup.PerHitDamageRatio, 0.0001f);
                Assert.IsTrue(runtime.GetComponent<OwnerBoundSupportPresenterBehaviour>().IsConfigured);
            }
            else
            {
                Assert.AreEqual(1.4f, combat.AttackRange, 0.0001f);
                Assert.AreEqual(75.0f, combat.AttackAngle, 0.0001f);
                Assert.IsTrue(combat.HasPersonalMitigation);
                Assert.AreEqual(1.0f, combat.PersonalIncomingDamageMultiplier, 0.0001f);
            }
        }

        [Test]
        public void FailedWolfSpawn_LeavesRosterAndActorsUnchanged()
        {
            using CanonicalTargetAreaRuntimeLifecycleTests.Fixture fixture = new CanonicalTargetAreaRuntimeLifecycleTests.Fixture();
            fixture.Factory.FailSpawn = true;
            LogAssert.Expect(LogType.Error, "Canonical companion spawn failed: wolf_tamer Companion prefab is missing: Lizzo/Characters/Companions/wolf_tamer");
            Assert.IsFalse(fixture.Run.Party.RecruitCanonical("wolf_tamer"));
            Assert.AreEqual(0, fixture.Run.Party.ActiveCompanionCount);
            Assert.AreEqual(0, fixture.Run.Party.ActiveCompanionSlotCount);
            Assert.AreEqual(0, fixture.Factory.LiveInstances.Count);
        }
    }
}
