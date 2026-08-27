using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Lizzo.PV.Data;
using Lizzo.PV.Legion;
using Lizzo.PV.Legion.Presentation;
using Lizzo.PV.Legion.Party.Roster;
using Lizzo.PV.P0.Presentation;
using Lizzo.PV.P0.Units;
using Lizzo.PV.P0.Visuals;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Lizzo.PV.Tests.EditMode
{
    /// <summary>
    /// Current cross-companion runtime contract owner.  The matrix cases below
    /// exercise the public catalog, roster, spawn, promotion, and combat seams;
    /// the compatibility fixture at the end is retained only for existing
    /// synergy tests that share this in-memory public test setup.
    /// </summary>
    public sealed class CompanionRuntimeContractTests
    {
        private static readonly FieldInfo ActiveProvider = typeof(PresentationCatalogProvider)
            .GetField("_active", BindingFlags.Static | BindingFlags.NonPublic);

        private PresentationCatalogProvider _previousProvider;
        private PresentationCatalogProvider _activeTestProvider;
        private PresentationCatalog _catalog;
        private GameObject _providerRoot;

        [SetUp]
        public void SetUp()
        {
            _previousProvider = ActiveProvider.GetValue(null) as PresentationCatalogProvider;
            UnitPresentationSet units = AssetDatabase.LoadAssetAtPath<UnitPresentationSet>(
                "Assets/_LizzoPV/Gameplay/Presentation/Data/UnitPresentationSet.asset");
            OwnedSupportPresentationSet supports = AssetDatabase.LoadAssetAtPath<OwnedSupportPresentationSet>(
                "Assets/_LizzoPV/Gameplay/Legion/Data/Presentation/OwnedSupportPresentationSet.asset");
            Assert.IsNotNull(units);
            Assert.IsNotNull(supports);
            _catalog = ScriptableObject.CreateInstance<PresentationCatalog>();
            _catalog.SetPresentationSetsForEditor(null, units, supports);
            _providerRoot = new GameObject("CompanionRuntimeContractCatalog");
            _providerRoot.SetActive(false);
            PresentationCatalogProvider provider = _providerRoot.AddComponent<PresentationCatalogProvider>();
            SerializedObject serialized = new SerializedObject(provider);
            serialized.FindProperty("_catalog").objectReferenceValue = _catalog;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            ActiveProvider.SetValue(null, provider);
            _activeTestProvider = provider;
        }

        [TearDown]
        public void TearDown()
        {
            if (_providerRoot != null)
                UnityEngine.Object.DestroyImmediate(_providerRoot);
            if (_catalog != null)
                UnityEngine.Object.DestroyImmediate(_catalog);
            ActiveProvider.SetValue(null, _previousProvider);
            _providerRoot = null;
            _catalog = null;
            _previousProvider = null;
            _activeTestProvider = null;
        }

        [Test]
        public void ResolverContracts_PreserveMagicTargetAreaNecromancerAndHerbalistSpecializations()
        {
            using ServiceTestFixture fixture = new ServiceTestFixture();

            Assert.IsTrue(new CompanionPersistentFieldCombatResolver(fixture.Data)
                .TryResolve("fire_mage", 1.0f, out CompanionPersistentFieldCombatSetup fire));
            Assert.AreEqual(5, fire.Damage);
            Assert.AreEqual(3.2f, fire.Period, 0.0001f);
            Assert.AreEqual(1.6f, fire.Radius, 0.0001f);
            Assert.AreEqual(3.0f, fire.Duration, 0.0001f);
            Assert.IsTrue(new CompanionChainCombatResolver(fixture.Data)
                .TryResolve("lightning_mage", 1.0f, out CompanionChainCombatSetup lightning));
            Assert.AreEqual(12, lightning.Damage);
            Assert.AreEqual(2.6f, lightning.Period, 0.0001f);
            Assert.AreEqual(3, lightning.MaxTargets);

            Assert.IsTrue(new CompanionTargetAreaCombatResolver(fixture.Data)
                .TryResolve("bombardier", 1.0f, out CompanionTargetAreaCombatSetup bombardier));
            Assert.AreEqual(16, bombardier.Damage);
            Assert.AreEqual(2.2f, bombardier.Period, 0.0001f);
            Assert.AreEqual(5.0f, bombardier.Range, 0.0001f);
            Assert.AreEqual(1.6f, bombardier.Radius, 0.0001f);
            Assert.AreEqual(6, bombardier.MaxTargets);
            Assert.AreEqual(0.5f, bombardier.CastDelay, 0.0001f);

            Assert.IsTrue(new CompanionReturningAttackCombatResolver(fixture.Data)
                .TryResolve("skeleton_bomber", 1.0f, out CompanionReturningAttackCombatSetup skeleton));
            Assert.AreEqual(15, skeleton.Damage);
            Assert.AreEqual(4.8f, skeleton.Range, 0.0001f);
            Assert.AreEqual(0.75f, skeleton.Width, 0.0001f);
            Assert.AreEqual(4, skeleton.MaxTargetsPerPass);

            Assert.IsTrue(new CompanionProjectileCombatResolver(fixture.Data)
                .TryResolve("necromancer", 1.0f, out CompanionProjectileCombatSetup curse));
            Assert.AreEqual(8, curse.Damage);
            Assert.AreEqual(3.0f, curse.Period, 0.0001f);
            Assert.AreEqual(5.0f, curse.Range, 0.0001f);
            Assert.AreEqual(AllyAttackStyle.TargetedProjectile, curse.AttackStyle);
            Assert.AreEqual(1, curse.MaxTargets);
            Assert.AreEqual(CompanionEnemyStatusKind.Curse, curse.AppliedStatusKind);
            Assert.AreEqual(2.0f, curse.DeathReactionRadius, 0.0001f);
            Assert.AreEqual(4, curse.DeathReactionMaxTargets);
            Assert.AreEqual(5.0f, curse.Range, 0.0001f);
        }

        [Test]
        public void CompanionDamageEligibility_RejectsEnemyDamageUnderRevision5CombatRules()
        {
            GameObject companionObject = new GameObject("Revision5Companion");
            GameObject enemyObject = new GameObject("Revision5EnemyDamageSource");
            try
            {
                CompanionRuntime companion = companionObject.AddComponent<CompanionRuntime>();
                MonsterController enemy = enemyObject.AddComponent<MonsterController>();
                Type policy = typeof(CompanionRuntime).Assembly
                    .GetType("Lizzo.PV.Legion.CompanionDamageEligibility");
                Assert.IsNotNull(policy);
                MethodInfo canReceive = policy.GetMethod(
                    "CanReceive",
                    BindingFlags.Static | BindingFlags.NonPublic);
                Assert.IsNotNull(canReceive);

                Assert.IsFalse((bool)canReceive.Invoke(null, new object[] { companion, enemy }));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(companionObject);
                UnityEngine.Object.DestroyImmediate(enemyObject);
            }
        }

        [Test]
        public void HungryGiantAoe_TargetsCommanderWithoutEnumeratingCompanions()
        {
            string source = File.ReadAllText(
                "Assets/_LizzoPV/Gameplay/Enemies/Runtime/HungryGiantBehaviour.Aoe.cs");

            Assert.That(source, Does.Contain("player.TryApplyBossPatternDamage"));
            Assert.That(source, Does.Not.Contain("ActiveCompanions"));
            Assert.That(source, Does.Not.Contain("companion.TryApplyBossPatternDamage"));
        }

        [Test]
        public void HerbalistRuntimeSpecAndPreview_PreserveCanonicalIdentityWithoutRosterMutation()
        {
            using ServiceTestFixture fixture = new ServiceTestFixture();
            Assert.IsTrue(CompanionRuntimeSpec.TryCreate(fixture.Data, "field_herbalist", false,
                out CompanionRuntimeSpec baseSpec));
            Assert.AreEqual("field_herbalist", baseSpec.BaseUnitId);
            Assert.AreEqual("field_herbalist", baseSpec.PresentedUnitId);
            Assert.AreEqual(50, baseSpec.BaseHp);
            Assert.AreEqual(2.8f, baseSpec.MoveSpeed, 0.0001f);
            Assert.IsFalse(baseSpec.IsPromoted);
            Assert.IsTrue(CompanionRuntimeSpec.TryCreate(fixture.Data, "field_herbalist", true,
                out CompanionRuntimeSpec promotedSpec));
            Assert.AreEqual("battle_apothecary", promotedSpec.PresentedUnitId);
            Assert.IsTrue(promotedSpec.IsPromoted);
            Assert.IsFalse(CompanionRuntimeSpec.TryCreate(fixture.Data, "battle_apothecary", false, out _));

            Assert.AreEqual(PartyRosterChangeResult.RejectedUnknown,
                fixture.Run.Party.PreviewCanonicalRecruit("battle_apothecary"));
            Assert.AreEqual(PartyRosterChangeResult.RejectedUnknown,
                fixture.Run.Party.PreviewCanonicalRecruit("unknown"));
            Assert.AreEqual(PartyRosterChangeResult.Recruit,
                fixture.Run.Party.PreviewCanonicalRecruit("field_herbalist"));
            Assert.AreEqual(0, fixture.Run.Party.ActiveCompanionSlotCount);
        }

        [TestCase("FireMage", 0, false)]
        [TestCase("FireSage", 2, false)]
        [TestCase("LightningMage", 0, false)]
        [TestCase("StormMage", 2, false)]
        [TestCase("Necromancer", 0, false)]
        [TestCase("DarkRitualist", 2, false)]
        [TestCase("Bombardier", 0, false)]
        [TestCase("PowderCaptain", 2, false)]
        [TestCase("SkeletonBomber", 0, false)]
        [TestCase("BoneArtillery", 2, false)]
        [TestCase("FieldHerbalist", 0, false)]
        [TestCase("BattleApothecary", 2, false)]
        [TestCase("WolfTamer", 0, true)]
        [TestCase("BeastCommander", 2, true)]
        [TestCase("WraithKnight", 0, false)]
        [TestCase("WraithGuardian", 2, false)]
        public void CanonicalPrefabs_ExposeSharedRuntimeCompositionAndOwnedSupportContract(
            string prefabName, int supportCount, bool expectsWolfPresenter)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"Assets/_LizzoPV/Gameplay/Legion/Prefabs/Characters/Companions/{prefabName}.prefab");
            Assert.IsNotNull(prefab);
            Assert.IsNotNull(prefab.GetComponent<AllyCombat>());
            Assert.IsNotNull(prefab.GetComponent<AllyFollower>());
            CompanionRuntime runtime = prefab.GetComponent<CompanionRuntime>();
            Assert.IsNotNull(runtime);
            Assert.IsNotNull(prefab.GetComponent<CompanionHealthBar>());
            UnitColliderRefs colliderRefs = prefab.GetComponent<UnitColliderRefs>();
            Assert.IsNotNull(colliderRefs);
            Assert.IsNotNull(colliderRefs.BodyCollider);
            Assert.IsNotNull(colliderRefs.CombatCollider);
            Assert.AreSame(colliderRefs.BodyCollider, runtime.BodyCollider);
            Assert.AreSame(colliderRefs.CombatCollider, runtime.CombatCollider);
            Transform supports = prefab.transform.Find("SupportVisuals");
            Assert.AreEqual(supportCount, supports == null ? 0 : supports.childCount);
            Assert.AreEqual(expectsWolfPresenter,
                prefab.GetComponent("OwnerBoundSupportPresenterBehaviour") != null);
            Assert.AreEqual(0, GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(prefab));
        }

        [TestCase("fire_mage", "fire_sage")]
        [TestCase("lightning_mage", "storm_mage")]
        [TestCase("bombardier", "powder_captain")]
        [TestCase("skeleton_bomber", "bone_artillery")]
        [TestCase("necromancer", "dark_ritualist")]
        [TestCase("wolf_tamer", "beast_commander")]
        [TestCase("wraith_knight", "wraith_guardian")]
        [TestCase("field_herbalist", "battle_apothecary")]
        public void RecruitReinforcePromote_UsesOneStableCanonicalActor(
            string baseId, string promotedId)
        {
            using CanonicalTargetAreaRuntimeLifecycleTests.Fixture fixture =
                new CanonicalTargetAreaRuntimeLifecycleTests.Fixture();
            PartyService party = fixture.Run.Party;
            Assert.AreEqual(PartyRosterChangeResult.Recruit, party.PreviewCanonicalRecruit(baseId));
            Assert.IsTrue(party.RecruitCanonical(baseId));
            Assert.IsTrue(party.RecruitCanonical(baseId));
            Assert.IsTrue(party.RecruitCanonical(baseId));
            Assert.AreEqual(1, party.ActiveCompanionCount);
            Assert.AreEqual(1, fixture.Factory.LiveInstances.Count);
            CompanionRuntime runtime = fixture.Factory.LiveInstances[0].GetComponent<CompanionRuntime>();
            Assert.AreEqual(baseId, runtime.BaseUnitId);
            Assert.AreEqual(promotedId, runtime.UnitId);
            Assert.AreEqual(1, runtime.GetComponentsInChildren<CompanionRuntime>(true).Length);
            Assert.AreEqual(2, runtime.transform.Find("SupportVisuals").childCount);

            AllyCombat combat = runtime.GetComponent<AllyCombat>();
            switch (baseId)
            {
                case "fire_mage":
                    Assert.AreEqual(1.6f, combat.PersistentFieldSetup.Radius, 0.0001f);
                    Assert.AreEqual(3.0f, combat.PersistentFieldSetup.Duration, 0.0001f);
                    break;
                case "lightning_mage":
                    Assert.AreEqual(3, combat.ChainSetup.MaxTargets);
                    break;
                case "bombardier":
                    Assert.AreEqual(1.6f, combat.TargetAreaRadius, 0.0001f);
                    Assert.AreEqual(6, combat.TargetAreaMaxTargets);
                    Assert.AreEqual(0.0f, combat.TargetAreaNormalPush, 0.0001f);
                    break;
                case "skeleton_bomber":
                    Assert.AreEqual(4.8f, combat.AttackRange, 0.0001f);
                    Assert.AreEqual(0.0f, combat.TargetAreaRadius, 0.0001f);
                    Assert.IsFalse(combat.HasPromotedTargetAreaFollowUp);
                    break;
                case "necromancer":
                    Assert.AreEqual(14, combat.Damage);
                    Assert.AreEqual(3.15f, combat.AttackPeriod, 0.0001f);
                    Assert.AreEqual(5.0f, combat.AttackRange, 0.0001f);
                    break;
                case "wolf_tamer":
                    Assert.AreEqual(1, combat.WolfOwnedProxySetup.HitCount);
                    Assert.IsTrue(runtime.GetComponent<OwnerBoundSupportPresenterBehaviour>().IsConfigured);
                    break;
                case "wraith_knight":
                    Assert.AreEqual(1.2f, combat.AttackRange, 0.0001f);
                    Assert.AreEqual(60.0f, combat.AttackAngle, 0.0001f);
                    Assert.IsFalse(combat.HasPersonalMitigation);
                    break;
                case "field_herbalist":
                    Assert.AreEqual(12, combat.Damage);
                    Assert.AreEqual(AllyAttackStyle.TargetedArea, combat.AttackStyle);
                    Assert.AreEqual(1.2f, combat.TargetAreaRadius, 0.0001f);
                    Assert.AreEqual(CompanionEnemyStatusKind.Vulnerable, combat.TargetAreaStatusKind);
                    Assert.IsFalse(combat.HasPromotedProjectileBounce);
                    break;
            }
        }

        [TestCase("bombardier")]
        [TestCase("wolf_tamer")]
        [TestCase("field_herbalist")]
        public void FailedCanonicalSpawn_DoesNotCommitRosterOrActors(string baseId)
        {
            using CanonicalTargetAreaRuntimeLifecycleTests.Fixture fixture =
                new CanonicalTargetAreaRuntimeLifecycleTests.Fixture();
            fixture.Factory.FailSpawn = true;
            LogAssert.Expect(LogType.Error,
                $"Canonical companion spawn failed: {baseId} Companion prefab is missing: Lizzo/Characters/Companions/{baseId}");
            Assert.IsFalse(fixture.Run.Party.RecruitCanonical(baseId));
            Assert.AreEqual(0, fixture.Run.Party.ActiveCompanionCount);
            Assert.AreEqual(0, fixture.Run.Party.ActiveCompanionSlotCount);
            Assert.AreEqual(0, fixture.Factory.LiveInstances.Count);
        }

        [Test]
        public void MissingCatalogOrRequiredVisual_DoesNotCommitCanonicalRoster()
        {
            ActiveProvider.SetValue(null, null);
            using (CanonicalTargetAreaRuntimeLifecycleTests.Fixture missingCatalog =
                   new CanonicalTargetAreaRuntimeLifecycleTests.Fixture())
            {
                LogAssert.Expect(LogType.Error,
                    "Canonical companion spawn failed: field_herbalist Canonical companion presentation is missing: field_herbalist");
                Assert.IsFalse(missingCatalog.Run.Party.RecruitCanonical("field_herbalist"));
                AssertUnchanged(missingCatalog.Run.Party, missingCatalog.Factory, 0);
            }

            ActiveProvider.SetValue(null, _activeTestProvider);
            using (CanonicalTargetAreaRuntimeLifecycleTests.Fixture setupFailure =
                   new CanonicalTargetAreaRuntimeLifecycleTests.Fixture())
            {
                setupFailure.Factory.RemoveVisualBeforeReturn = true;
                LogAssert.Expect(LogType.Error,
                    "Canonical companion spawn failed: field_herbalist Companion prefab is missing required component: CommanderAllyVisual");
                Assert.IsFalse(setupFailure.Run.Party.RecruitCanonical("field_herbalist"));
                AssertUnchanged(setupFailure.Run.Party, setupFailure.Factory, 1);
            }
        }

        private static void AssertUnchanged(PartyService party,
            CanonicalTargetAreaRuntimeLifecycleTests.TestPrefabFactory factory, int spawnCalls)
        {
            Assert.AreEqual(0, party.ActiveCompanionCount);
            Assert.AreEqual(0, party.ActiveCompanionSlotCount);
            Assert.AreEqual(spawnCalls, factory.SpawnedAddresses.Count);
            Assert.AreEqual(0, factory.LiveInstances.Count);
        }

    }

    // Shared test fixture retained as a compatibility seam for the existing
    // synergy tests. It is no longer a test owner and contains no test cases.
    internal static class CanonicalTargetAreaRuntimeLifecycleTests
    {
        internal sealed class Fixture : IDisposable
        {
            private readonly GameObject _root = new GameObject("TargetAreaFixture");
            public readonly FakeDataProvider Data = new FakeDataProvider();
            public readonly TestPrefabFactory Factory = new TestPrefabFactory();
            public readonly AppServices App;
            public readonly RunServices Run;

            public Fixture()
            {
                Data.InitializeAsync().GetAwaiter().GetResult();
                TestAssetService assets = new TestAssetService();
                App = new AppServices(assets, Data);
                RuntimeObjectRegistry registry = new RuntimeObjectRegistry(Factory);
                Transform poolRoot = new GameObject("Pool").transform;
                poolRoot.SetParent(_root.transform, false);
                Run = new RunServices(App, new Lizzo.PV.Flow.RunState(), registry,
                    new ObjectPoolService(poolRoot), Factory);
                RetroSfx.Configure(assets);
                RetroVfx.Configure(assets, Factory);
                AttackVisual.Configure(Factory);
                FloatingDamageText.Configure(Factory);
                PlayerController player = _root.AddComponent<PlayerController>();
                player.MaxHp = 100;
                player.Hp = 100;
                registry.RegisterPlayer(player);
            }

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
        }

        internal sealed class TestPrefabFactory : IPrefabFactory
        {
            public bool FailSpawn;
            public bool RemoveVisualBeforeReturn;
            public readonly List<string> SpawnedAddresses = new List<string>();
            public readonly List<GameObject> LiveInstances = new List<GameObject>();

            public GameObject Spawn(string address, Transform parent = null, bool pooled = false)
            {
                SpawnedAddresses.Add(address);
                if (FailSpawn)
                    return null;
                string id = address.Substring(address.LastIndexOf('/') + 1);
                UnitPresentationSet set = AssetDatabase.LoadAssetAtPath<UnitPresentationSet>(
                    "Assets/_LizzoPV/Gameplay/Presentation/Data/UnitPresentationSet.asset");
                if (set == null || set.TryGetEntry(id, out UnitPresentationSet.Entry entry) == false)
                    return null;
                GameObject instance = UnityEngine.Object.Instantiate(entry.Prefab, parent);
                LiveInstances.Add(instance);
                if (RemoveVisualBeforeReturn)
                    UnityEngine.Object.DestroyImmediate(instance.GetComponent<CommanderAllyVisual>());
                return instance;
            }

            public GameObject Rent(GameObject prefab, string poolKey, Transform parent = null) => null;

            public void Release(GameObject instance)
            {
                LiveInstances.Remove(instance);
                if (instance != null)
                    UnityEngine.Object.DestroyImmediate(instance);
            }

            public void Clear()
            {
                for (int i = LiveInstances.Count - 1; i >= 0; i--)
                    Release(LiveInstances[i]);
            }
        }
    }
}
