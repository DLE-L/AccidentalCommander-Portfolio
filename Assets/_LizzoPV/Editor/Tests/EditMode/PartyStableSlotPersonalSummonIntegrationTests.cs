using System;
using System.Collections.Generic;
using System.Reflection;
using Lizzo.PV.Combat;
using Lizzo.PV.Combat.Fields;
using Lizzo.PV.Combat.Projectiles;
using Lizzo.PV.Combat.Summons;
using Lizzo.PV.Data;
using Lizzo.PV.Flow;
using Lizzo.PV.Legion;
using Lizzo.PV.P0.Presentation;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class PartyStableSlotPersonalSummonIntegrationTests
    {
        private static readonly FieldInfo ActiveProviderField = typeof(PresentationCatalogProvider)
            .GetField("_active", BindingFlags.Static | BindingFlags.NonPublic);

        private PresentationCatalogProvider _previousProvider;
        private PresentationCatalog _catalog;
        private GameObject _providerRoot;
        private GameObject _origin;

        [SetUp]
        public void SetUp()
        {
            _previousProvider = ActiveProviderField.GetValue(null) as PresentationCatalogProvider;
            OwnedSupportPresentationSet supports = AssetDatabase.LoadAssetAtPath<OwnedSupportPresentationSet>("Assets/_LizzoPV/Data/Presentation/OwnedSupportPresentationSet.asset");
            Assert.IsNotNull(supports);
            _catalog = ScriptableObject.CreateInstance<PresentationCatalog>();
            _catalog.SetPresentationSetsForEditor(null, null, null, null, null, supports);
            _providerRoot = new GameObject("StableSlotSummonCatalog");
            _providerRoot.SetActive(false);
            PresentationCatalogProvider provider = _providerRoot.AddComponent<PresentationCatalogProvider>();
            SerializedObject serialized = new SerializedObject(provider);
            serialized.FindProperty("_catalog").objectReferenceValue = _catalog;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            ActiveProviderField.SetValue(null, provider);
            _origin = new GameObject("NecromancerOrigin");
        }

        [TearDown]
        public void TearDown()
        {
            if (_origin != null) UnityEngine.Object.DestroyImmediate(_origin);
            if (_providerRoot != null) UnityEngine.Object.DestroyImmediate(_providerRoot);
            if (_catalog != null) UnityEngine.Object.DestroyImmediate(_catalog);
            ActiveProviderField.SetValue(null, _previousProvider);
        }

        [Test]
        public void StableSlotBridge_UsesRosterKeyDiscardsCapFullAndRequiresFreshThresholdAfterRelease()
        {
            using Fixture fixture = new Fixture();
            CountableKillAttribution attribution = CreateCompanionAttribution(101);

            for (int i = 0; i < 14; i++)
                Assert.IsFalse(fixture.Party.TryAdvanceNecromancerPersonalSummon(attribution, "squad_00", false, _origin.transform));
            Assert.IsTrue(fixture.Party.TryAdvanceNecromancerPersonalSummon(attribution, "squad_00", false, _origin.transform));
            Assert.AreEqual(1, fixture.Module.SpawnRequests.Count);
            Assert.AreEqual("squad_00", fixture.Module.SpawnRequests[0].OwnerKey);
            Assert.AreSame(_origin.transform, fixture.Module.SpawnRequests[0].SpawnOrigin);

            fixture.Module.SetActive("squad_00", "necromancer:UNIT_PERSONAL_SKELETON_01", 1);
            for (int i = 0; i < 15; i++)
                Assert.IsFalse(fixture.Party.TryAdvanceNecromancerPersonalSummon(attribution, "squad_00", false, _origin.transform));
            Assert.IsTrue(fixture.Party.TryGetNecromancerKillState("squad_00", out CountableKillThresholdState state));
            Assert.AreEqual(0, state.PendingCountableKills);

            fixture.Module.SetActive("squad_00", "necromancer:UNIT_PERSONAL_SKELETON_01", 0);
            for (int i = 0; i < 14; i++)
                Assert.IsFalse(fixture.Party.TryAdvanceNecromancerPersonalSummon(attribution, "squad_00", false, _origin.transform));
            Assert.IsTrue(fixture.Party.TryAdvanceNecromancerPersonalSummon(attribution, "squad_00", false, _origin.transform));
            Assert.AreEqual(2, fixture.Module.SpawnRequests.Count);
        }

        [Test]
        public void StableSlotBridge_PromotionPreservesStateAndSameSourceSlotsRemainIsolated()
        {
            using Fixture fixture = new Fixture();
            CountableKillAttribution attribution = CreateCompanionAttribution(202);
            for (int i = 0; i < 10; i++)
                fixture.Party.TryAdvanceNecromancerPersonalSummon(attribution, "squad_01", false, _origin.transform);
            for (int i = 0; i < 5; i++)
                Assert.IsFalse(fixture.Party.TryAdvanceNecromancerPersonalSummon(attribution, "squad_02", true, _origin.transform));
            Assert.IsTrue(fixture.Party.TryGetNecromancerKillState("squad_01", out CountableKillThresholdState first));
            Assert.AreEqual(10, first.PendingCountableKills);

            for (int i = 0; i < 4; i++)
                Assert.IsFalse(fixture.Party.TryAdvanceNecromancerPersonalSummon(attribution, "squad_01", true, _origin.transform));
            Assert.IsTrue(fixture.Party.TryAdvanceNecromancerPersonalSummon(attribution, "squad_01", true, _origin.transform));
            Assert.AreEqual(2, fixture.Module.SpawnRequests[0].ActiveCap);
            Assert.AreEqual("squad_01", fixture.Module.SpawnRequests[0].OwnerKey);
            Assert.IsTrue(fixture.Party.TryGetNecromancerKillState("squad_02", out CountableKillThresholdState second));
            Assert.AreEqual(5, second.PendingCountableKills);
        }

        [Test]
        public void StableSlotBridge_ResetClearsThresholdStateAndRejectsExcludedAttribution()
        {
            using Fixture fixture = new Fixture();
            CountableKillAttribution excluded = new CountableKillAttribution(1, "necromancer", CombatKillSourceCategory.PersonalSummon);
            Assert.IsFalse(fixture.Party.TryAdvanceNecromancerPersonalSummon(excluded, "squad_03", false, _origin.transform));
            Assert.IsFalse(fixture.Party.TryGetNecromancerKillState("squad_03", out _));

            fixture.Party.TryAdvanceNecromancerPersonalSummon(CreateCompanionAttribution(1), "squad_03", false, _origin.transform);
            Assert.IsTrue(fixture.Party.TryGetNecromancerKillState("squad_03", out _));
            fixture.Party.ResetRunState();
            Assert.IsFalse(fixture.Party.TryGetNecromancerKillState("squad_03", out _));
        }

        private sealed class Fixture : IDisposable
        {
            private readonly GameObject _root = new GameObject("StableSlotSummonFixture");
            public readonly RecordingPersonalSummonModule Module = new RecordingPersonalSummonModule();
            public readonly PartyService Party;

            public Fixture()
            {
                FakeDataProvider data = new FakeDataProvider();
                data.SetCompanionCombatProfile(new CompanionCombatProfileData
                {
                    UnitId = "necromancer", BaseHp = 50, MoveSpeed = 2.5f, BasicSkillId = "skill_curse_bolt", BasicEffectId = "dmg_curse_bolt_v1",
                    SecondarySkillId = "skill_personal_thrall", SecondaryRuleId = "personal_thrall_countable_kills", PromotionProfileId = "dark_ritualist", NoTargetRetrySeconds = 0.15f,
                });
                data.SetCompanionSummon(new CompanionSummonData
                {
                    Id = "UNIT_PERSONAL_SKELETON_01", OwnerUnitId = "necromancer", SkillId = "skill_personal_thrall", CountableKillThreshold = 15,
                    BaseActiveCap = 1, PromotedActiveCap = 2, Hp = 18, Damage = 4, AttackInterval = 1.3f, Range = 1.0f, MoveSpeed = 2.7f, AiScanInterval = 0.2f,
                    LifetimeRuleId = "battle_end_or_hp0", TargetRule = CombatTargetRule.Nearest, Tags = "summon_object,companion_tag=false,no_family_tag",
                    BossRuleId = "normal_target", StackRuleId = "separate_owner_cap", ResetRuleId = "battle_end", RemoteConfigKey = "rc_personal_skeleton_stats", DistinctFromSummonId = "UNIT_SYNERGY_SKELETON_01",
                });
                data.InitializeAsync().GetAwaiter().GetResult();
                RecordingFactory factory = new RecordingFactory();
                Party = new PartyService(data, new RuntimeObjectRegistry(factory), factory, new NoProjectileModule(), new NoImmediateHitModule(), new NoFieldModule(), new RunState(), Module);
            }

            public void Dispose()
            {
                Party.Dispose();
                UnityEngine.Object.DestroyImmediate(_root);
            }
        }

        private static CountableKillAttribution CreateCompanionAttribution(int ownerInstanceId)
        {
            return new CountableKillAttribution(ownerInstanceId, "necromancer", CombatKillSourceCategory.CompanionOwnedAction);
        }

        private sealed class RecordingPersonalSummonModule : ICompanionPersonalSummonModule
        {
            private readonly Dictionary<string, int> _active = new Dictionary<string, int>();
            public readonly List<PersonalSummonSpawnRequest> SpawnRequests = new List<PersonalSummonSpawnRequest>();
            public int ActiveCount { get; private set; }
            public bool TrySpawn(in PersonalSummonSpawnRequest request, float currentTime) { SpawnRequests.Add(request); return true; }
            public int GetActiveCount(string ownerKey, string sourceId) => _active.TryGetValue(ownerKey + ":" + sourceId, out int count) ? count : 0;
            public void SetActive(string ownerKey, string sourceId, int count) { _active[ownerKey + ":" + sourceId] = count; }
            public bool Release(PersonalSummonRuntime runtime) => false;
            public void Tick(float currentTime, float deltaTime) { }
            public void Reset() { _active.Clear(); }
            public void Dispose() { _active.Clear(); }
        }

        private sealed class RecordingFactory : IPrefabFactory
        {
            public GameObject Spawn(string address, Transform parent = null, bool pooled = false) => null;
            public GameObject Rent(GameObject prefab, string poolKey, Transform parent = null) => null;
            public void Release(GameObject instance) { }
            public void Clear() { }
        }
        private sealed class NoProjectileModule : ICombatProjectileModule { public bool TrySpawn(in CombatProjectileRequest request) => false; }
        private sealed class NoImmediateHitModule : ICombatImmediateHitModule { public bool TryApply(in CombatImmediateHitRequest request) => false; }
        private sealed class NoFieldModule : ICombatPersistentFieldModule
        {
            public int ActiveFieldCount => 0;
            public bool TrySpawn(in CombatPersistentFieldRequest request, float currentTime) => false;
            public void Tick(float currentTime) { }
            public void Reset() { }
            public void Dispose() { }
        }
    }
}
