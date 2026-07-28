using System.Collections.Generic;
using Lizzo.PV.Combat;
using Lizzo.PV.Combat.Summons;
using Lizzo.PV.Data;
using Lizzo.PV.Legion;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class CompanionPersonalSummonModuleTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            for (int i = _objects.Count - 1; i >= 0; i--)
            {
                if (_objects[i] != null)
                    Object.DestroyImmediate(_objects[i]);
            }

            _objects.Clear();
        }

        [Test]
        public void Prefab_ProvidesExplicitPersonalSummonRuntimeContract()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_LizzoPV/Prefabs/Characters/Supports/PersonalSkeletonSummon.prefab");
            Assert.IsNotNull(prefab);
            PersonalSummonRuntime runtime = prefab.GetComponent<PersonalSummonRuntime>();
            Assert.IsNotNull(runtime);
            Assert.IsNotNull(runtime.GetComponent<Rigidbody2D>());
            Assert.IsNotNull(runtime.CombatCollider);
            Assert.IsFalse(runtime.IsCompanionOwned);
            Assert.IsFalse(runtime.HasRosterIdentity);
            Assert.IsFalse(runtime.HasFamilyTag);
            Assert.AreEqual("Visual", prefab.transform.GetChild(0).name);
        }

        [Test]
        public void Module_SpawnsAtOwner_EnforcesCap_AndDispatchesNearestAtCadence()
        {
            GameObject owner = CreateObject("Owner", Vector3.zero);
            RecordingEnemy farther = CreateEnemy("Far", new Vector3(0.9f, 0.0f, 0.0f));
            RecordingEnemy nearer = CreateEnemy("Near", new Vector3(0.5f, 0.0f, 0.0f));
            RecordingFactory factory = new RecordingFactory(LoadPrefab());
            CompanionPersonalSummonModule module = new CompanionPersonalSummonModule(
                factory,
                null,
                new RecordingTargetSource(farther, nearer),
                new CombatImmediateHitModule());

            PersonalSummonSpawnRequest request = CreateRequest(owner.transform);
            Assert.IsTrue(module.TrySpawn(request, 0.0f));
            Assert.IsFalse(module.TrySpawn(request, 0.0f));
            Assert.AreEqual(1, module.ActiveCount);
            Assert.AreEqual(owner.transform.position, factory.LastSpawn.transform.position);

            module.Tick(0.0f, 0.2f);
            Assert.AreEqual(1, nearer.HitCount);
            Assert.AreEqual(0, farther.HitCount);
            Assert.AreEqual(4, nearer.LastRequest.Damage);
            Assert.AreEqual("necromancer:UNIT_PERSONAL_SKELETON_01", nearer.LastRequest.SourceId);

            module.Tick(1.29f, 0.2f);
            Assert.AreEqual(1, nearer.HitCount);
            module.Tick(1.3f, 0.2f);
            Assert.AreEqual(2, nearer.HitCount);
        }

        [Test]
        public void Module_ReleaseOnHpZero_ResetAndFailedSpawn_LeavesNoActor()
        {
            GameObject owner = CreateObject("Owner", Vector3.zero);
            RecordingEnemy target = CreateEnemy("Target", Vector3.zero);
            RecordingFactory factory = new RecordingFactory(LoadPrefab());
            CompanionPersonalSummonModule module = new CompanionPersonalSummonModule(
                factory,
                null,
                new RecordingTargetSource(target),
                new CombatImmediateHitModule());

            Assert.IsTrue(module.TrySpawn(CreateRequest(owner.transform), 0.0f));
            PersonalSummonRuntime runtime = factory.LastSpawn.GetComponent<PersonalSummonRuntime>();
            Assert.IsTrue(new CombatImmediateHitModule().TryApply(CombatImmediateHitRequest.CreateEnemyContact(
                "test_enemy", runtime, Vector3.zero, Vector3.right, 18, "contact", RetroVfxKind.EnemyContactHit)));
            module.Tick(0.1f, 0.1f);
            Assert.AreEqual(0, module.ActiveCount);
            Assert.AreEqual(1, factory.ReleaseCount);

            factory.ReturnNull = true;
            Assert.IsFalse(module.TrySpawn(CreateRequest(owner.transform), 1.0f));
            Assert.AreEqual(0, module.ActiveCount);
            module.Reset();
            Assert.AreEqual(0, module.ActiveCount);
        }

        [Test]
        public void Module_UsesStableOwnerKeyAcrossOriginReplacementAndRequestedCap()
        {
            GameObject firstOrigin = CreateObject("FirstOrigin", Vector3.zero);
            GameObject promotedOrigin = CreateObject("PromotedOrigin", new Vector3(2.0f, 0.0f, 0.0f));
            RecordingFactory factory = new RecordingFactory(LoadPrefab());
            CompanionPersonalSummonModule module = new CompanionPersonalSummonModule(
                factory,
                null,
                new RecordingTargetSource(),
                new CombatImmediateHitModule());

            PersonalSummonSpawnRequest baseRequest = CreateRequest("squad_03", firstOrigin.transform, 1);
            PersonalSummonSpawnRequest promotedRequest = CreateRequest("squad_03", promotedOrigin.transform, 2);

            Assert.IsTrue(module.TrySpawn(baseRequest, 0.0f));
            Assert.AreEqual(1, module.GetActiveCount("squad_03", "necromancer:UNIT_PERSONAL_SKELETON_01"));
            Assert.IsTrue(module.TrySpawn(promotedRequest, 0.0f));
            Assert.AreEqual(promotedOrigin.transform.position, factory.LastSpawn.transform.position);
            Assert.AreEqual(2, module.GetActiveCount("squad_03", "necromancer:UNIT_PERSONAL_SKELETON_01"));
            Assert.IsFalse(module.TrySpawn(promotedRequest, 0.0f));
        }

        [Test]
        public void RunServices_OwnsOnePersonalSummonModule_AndRegistryDoesNot()
        {
            using ServiceTestFixture fixture = new ServiceTestFixture();
            Assert.IsNotNull(fixture.Run.PersonalSummonModule);
            Assert.IsNull(typeof(RuntimeObjectRegistry).GetProperty("PersonalSummonModule"));
        }

        private PersonalSummonSpawnRequest CreateRequest(Transform owner)
        {
            return CreateRequest("1", owner, 1);
        }

        private PersonalSummonSpawnRequest CreateRequest(string ownerKey, Transform owner, int activeCap)
        {
            CompanionSummonData data = new CompanionSummonData
            {
                Id = "UNIT_PERSONAL_SKELETON_01",
                OwnerUnitId = "necromancer",
                BaseActiveCap = 1,
                PromotedActiveCap = 2,
                Hp = 18,
                Damage = 4,
                AttackInterval = 1.3f,
                Range = 1.0f,
                MoveSpeed = 2.7f,
                AiScanInterval = 0.2f,
            };
            return new PersonalSummonSpawnRequest(ownerKey, "necromancer:UNIT_PERSONAL_SKELETON_01", owner,
                "Lizzo/Characters/Supports/UNIT_PERSONAL_SKELETON_01", new CompanionPersonalSummonSetup(data), activeCap);
        }

        private GameObject LoadPrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_LizzoPV/Prefabs/Characters/Supports/PersonalSkeletonSummon.prefab");
            Assert.IsNotNull(prefab);
            return prefab;
        }

        private GameObject CreateObject(string name, Vector3 position)
        {
            GameObject value = new GameObject(name);
            value.transform.position = position;
            _objects.Add(value);
            return value;
        }

        private RecordingEnemy CreateEnemy(string name, Vector3 position)
        {
            return CreateObject(name, position).AddComponent<RecordingEnemy>();
        }

        private sealed class RecordingFactory : IPrefabFactory
        {
            private readonly GameObject _prefab;
            public GameObject LastSpawn { get; private set; }
            public int ReleaseCount { get; private set; }
            public bool ReturnNull { get; set; }

            public RecordingFactory(GameObject prefab) => _prefab = prefab;
            public GameObject Spawn(string address, Transform parent = null, bool pooled = false)
            {
                if (ReturnNull)
                    return null;
                LastSpawn = Object.Instantiate(_prefab, parent);
                return LastSpawn;
            }
            public GameObject Rent(GameObject prefab, string poolKey, Transform parent = null) => Spawn(poolKey, parent);
            public void Release(GameObject instance)
            {
                ReleaseCount++;
                Object.DestroyImmediate(instance);
            }
            public void Clear() { }
        }

        private sealed class RecordingTargetSource : ICompanionPersonalSummonTargetSource
        {
            private readonly RecordingEnemy[] _targets;
            public RecordingTargetSource(params RecordingEnemy[] targets) => _targets = targets;
            public void CollectTargets(List<PersonalSummonTarget> destination)
            {
                for (int i = 0; i < _targets.Length; i++)
                {
                    RecordingEnemy target = _targets[i];
                    if (target != null)
                        destination.Add(new PersonalSummonTarget(target, target.transform.position, target.GetInstanceID()));
                }
            }
        }

        private sealed class RecordingEnemy : MonoBehaviour, ICombatImmediateHitTarget
        {
            public int HitCount { get; private set; }
            public CombatImmediateHitRequest LastRequest { get; private set; }
            CombatImmediateHitFaction ICombatImmediateHitTarget.Faction => CombatImmediateHitFaction.Enemy;
            bool ICombatImmediateHitTarget.IsAlive => true;
            void ICombatImmediateHitTarget.ReceiveImmediateHit(in CombatImmediateHitRequest request)
            {
                HitCount++;
                LastRequest = request;
            }
        }
    }
}
