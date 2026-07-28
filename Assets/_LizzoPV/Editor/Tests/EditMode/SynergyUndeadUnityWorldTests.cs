using System;
using System.Reflection;
using Lizzo.PV.Combat;
using Lizzo.PV.Combat.Fields;
using Lizzo.PV.Combat.Projectiles;
using Lizzo.PV.Data;
using Lizzo.PV.Legion;
using Lizzo.PV.Legion.Synergy;
using Lizzo.PV.P0.Units;
using Lizzo.PV.P0.Visuals;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class SynergyUndeadUnityWorldTests
    {
        readonly System.Collections.Generic.List<UnityEngine.Object> _objects = new System.Collections.Generic.List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            for (int index = _objects.Count - 1; index >= 0; index--)
                UnityEngine.Object.DestroyImmediate(_objects[index]);
            _objects.Clear();
        }

        [Test]
        public void RearSpawn_UsesMoveDirectionThenSafeFallbackAndCancelsWhenNoPointExists()
        {
            TestFactory factory = new TestFactory(_objects);
            RuntimeObjectRegistry registry = new RuntimeObjectRegistry(factory);
            Anchor anchor = new Anchor(new Vector3(2.0f, 3.0f), Vector2.right, Vector3.up);
            SafeKnockbackWorld safeWorld = CreateSafeWorld(out BoxCollider2D boundary, Array.Empty<Collider2D>());
            boundary.size = new Vector2(20.0f, 20.0f);
            UndeadSummonUnityWorld world = new UndeadSummonUnityWorld(anchor, registry, factory, new CombatImmediateHitModule(), null, safeWorld);

            Assert.IsTrue(world.TryResolveRearSpawn(1.0f, 1.5f, out Vector3 direct));
            Assert.That(direct, Is.EqualTo(new Vector3(1.0f, 3.0f, 0.0f)));

            UndeadSummonUnityWorld formationWorld = new UndeadSummonUnityWorld(
                new Anchor(new Vector3(2.0f, 3.0f), Vector2.zero, Vector3.up), registry, factory, new CombatImmediateHitModule(), null, null);
            Assert.IsTrue(formationWorld.TryResolveRearSpawn(1.0f, 1.5f, out Vector3 formationRear));
            Assert.That(formationRear, Is.EqualTo(new Vector3(2.0f, 2.0f, 0.0f)));

            BoxCollider2D obstacle = CreateBox("Obstacle", direct, Vector2.one, false);
            safeWorld.ConfigureForRuntime(boundary, new Collider2D[] { obstacle });
            Assert.IsTrue(world.TryResolveRearSpawn(1.0f, 1.5f, out Vector3 fallback));
            Assert.That(fallback, Is.Not.EqualTo(direct));
            Assert.IsFalse(obstacle.OverlapPoint(fallback));

            boundary.size = Vector2.zero;
            Physics2D.SyncTransforms();
            Assert.IsFalse(world.TryResolveRearSpawn(1.0f, 1.5f, out _));
        }

        [Test]
        public void SpawnReleaseAndTargets_UseIndependentPooledAddressAndCachedWrappers()
        {
            TestFactory factory = new TestFactory(_objects);
            RuntimeObjectRegistry registry = new RuntimeObjectRegistry(factory);
            Anchor anchor = new Anchor(Vector3.zero, Vector2.zero, Vector3.up);
            UndeadSummonUnityWorld world = new UndeadSummonUnityWorld(anchor, registry, factory, new CombatImmediateHitModule(), null, null);
            SynergySummonData data = CreateProvider().GetSynergySummon("UNIT_SYNERGY_SKELETON_01");

            Assert.IsTrue(world.TrySpawn(new UndeadSummonSynergy.SpawnRequest(data, new Vector3(4.0f, 0.0f), 0.3f), out UndeadSummonSynergy.IActor actor));
            Assert.That(factory.Address, Is.EqualTo(UndeadSummonUnityWorld.SkeletonAddress));
            Assert.IsTrue(factory.Pooled);
            Assert.That(actor, Is.TypeOf<SynergySkeletonRuntime>());
            Assert.That(((SynergySkeletonRuntime)actor).Hp, Is.EqualTo(22));
            Assert.That(((SynergySkeletonRuntime)actor).transform.position, Is.EqualTo(new Vector3(4.0f, 0.0f)));

            GameObject enemyObject = new GameObject("Enemy");
            _objects.Add(enemyObject);
            MonsterController enemy = enemyObject.AddComponent<MonsterController>();
            registry.RegisterEnemy(enemy);
            var targets = new System.Collections.Generic.List<UndeadSummonSynergy.ITarget>();
            world.CollectTargets(targets);
            UndeadSummonSynergy.ITarget first = targets[0];
            world.CollectTargets(targets);
            Assert.AreSame(first, targets[0]);

            world.Release(actor);
            world.Release(actor);
            Assert.That(factory.ReleaseCount, Is.EqualTo(1));
        }

        [Test]
        public void Facade_ExposesOneRunOwnedCoreAndWorldSurface()
        {
            Assert.That(typeof(UndeadSummonRunModule), Is.Not.Null);
        }

        [Test]
        public void SpawnFailuresAndRelease_UpdateGridExactlyOnce()
        {
            TestFactory factory = new TestFactory(_objects);
            RuntimeObjectRegistry registry = new RuntimeObjectRegistry(factory);
            GridController grid = CreateGrid();
            UndeadSummonUnityWorld world = new UndeadSummonUnityWorld(new Anchor(Vector3.zero, Vector2.up, Vector3.up), registry, factory, new CombatImmediateHitModule(), grid, null);
            SynergySummonData data = CreateProvider().GetSynergySummon("UNIT_SYNERGY_SKELETON_01");

            factory.IncludeRuntime = false;
            Assert.IsFalse(world.TrySpawn(new UndeadSummonSynergy.SpawnRequest(data, Vector3.zero, 0.0f), out _));
            Assert.That(factory.ReleaseCount, Is.EqualTo(1));

            factory.IncludeRuntime = true;
            factory.ConfigureReady = false;
            Assert.IsFalse(world.TrySpawn(new UndeadSummonSynergy.SpawnRequest(data, Vector3.zero, 0.0f), out _));
            Assert.That(factory.ReleaseCount, Is.EqualTo(2));

            factory.ConfigureReady = true;
            Assert.IsTrue(world.TrySpawn(new UndeadSummonSynergy.SpawnRequest(data, Vector3.zero, 0.0f), out UndeadSummonSynergy.IActor actor));
            var gathered = new System.Collections.Generic.List<GameObject>();
            grid.GatherObjects(Vector3.zero, 1.0f, gathered);
            Assert.That(gathered, Has.Count.EqualTo(1));
            world.Release(actor);
            grid.GatherObjects(Vector3.zero, 1.0f, gathered);
            Assert.That(gathered, Is.Empty);
            Assert.That(factory.ReleaseCount, Is.EqualTo(3));
        }

        [Test]
        public void AdvanceAndBossAdapter_UseLiveCachedProductionTargets()
        {
            TestFactory factory = new TestFactory(_objects);
            RecordingHits hits = new RecordingHits();
            RuntimeObjectRegistry registry = new RuntimeObjectRegistry(factory);
            UndeadSummonUnityWorld world = new UndeadSummonUnityWorld(new Anchor(Vector3.zero, Vector2.up, Vector3.up), registry, factory, hits, null, null);
            SynergySummonData data = CreateProvider().GetSynergySummon("UNIT_SYNERGY_SKELETON_01");
            Assert.IsTrue(world.TrySpawn(new UndeadSummonSynergy.SpawnRequest(data, Vector3.zero, 0.0f), out UndeadSummonSynergy.IActor actor));

            GameObject enemyObject = new GameObject("Boss");
            _objects.Add(enemyObject);
            enemyObject.transform.position = new Vector3(1.5f, 0.0f);
            enemyObject.AddComponent<HungryGiantBehaviour>();
            MonsterController boss = enemyObject.AddComponent<MonsterController>();
            registry.RegisterEnemy(boss);
            var targets = new System.Collections.Generic.List<UndeadSummonSynergy.ITarget>();
            world.CollectTargets(targets);
            actor.SetTarget(targets[0]);
            world.AdvanceActors(0.1f, 0.1f);
            Assert.That(((SynergySkeletonRuntime)actor).transform.position.x, Is.EqualTo(0.28f).Within(0.001f));
            world.AdvanceActors(0.2f, 1.0f);
            world.AdvanceActors(0.3f, 0.1f);
            Assert.That(hits.Count, Is.EqualTo(1));

            Assert.IsTrue(world.TryGetCombatTarget(boss, out UndeadSummonSynergy.ICombatTarget first));
            Assert.IsTrue(world.TryGetCombatTarget(boss, out UndeadSummonSynergy.ICombatTarget second));
            Assert.AreSame(first, second);
            Assert.IsTrue(first.IsBoss);
            Assert.IsTrue(first.IsTargetable);
        }

        [Test]
        public void DynamicFacadeAnchorAndReset_ResolveLateCommanderAndReleaseOnce()
        {
            LocalDataProvider provider = CreateProvider();
            TestFactory factory = new TestFactory(_objects);
            RecordingHits hits = new RecordingHits();
            RuntimeObjectRegistry registry = new RuntimeObjectRegistry(factory);
            SynergyActivationState activations = new SynergyActivationState(provider);
            SynergyTriggerState triggers = new SynergyTriggerState(activations);
            ICombatProjectileModule projectiles = new CombatProjectileModule(factory, registry);
            ICombatPersistentFieldModule fields = new CombatPersistentFieldModule(new RegistryPersistentFieldTargetSource(registry), hits);
            PartyService party = new PartyService(provider, registry, factory, projectiles, hits, fields);
            using UndeadSummonRunModule facade = new UndeadSummonRunModule(provider, triggers, registry, party, factory, hits, null, null);

            GameObject commanderObject = new GameObject("LateCommander");
            _objects.Add(commanderObject);
            commanderObject.AddComponent<Rigidbody2D>();
            PlayerController commander = commanderObject.AddComponent<PlayerController>();
            registry.RegisterPlayer(commander);
            commander.SetMoveDirection(Vector2.right);
            activations.Refresh(CreateUndeadSlots());

            Assert.IsTrue(facade.TryResolvePending(0.0f, 1));
            Assert.That(facade.ActiveCount, Is.EqualTo(1));
            Assert.That(factory.LastInstance.transform.position, Is.EqualTo(new Vector3(-1.0f, 0.0f, 0.0f)));
            facade.ResetForResult();
            facade.ResetForResult();
            facade.Dispose();
            Assert.That(factory.ReleaseCount, Is.EqualTo(1));
            party.Dispose();
            triggers.Dispose();
            activations.Dispose();
        }

        SafeKnockbackWorld CreateSafeWorld(out BoxCollider2D boundary, Collider2D[] obstacles)
        {
            GameObject boundaryObject = new GameObject("Boundary");
            _objects.Add(boundaryObject);
            boundary = boundaryObject.AddComponent<BoxCollider2D>();
            boundary.isTrigger = true;
            GameObject worldObject = new GameObject("SafeWorld");
            _objects.Add(worldObject);
            SafeKnockbackWorld world = worldObject.AddComponent<SafeKnockbackWorld>();
            world.ConfigureForRuntime(boundary, obstacles);
            return world;
        }

        BoxCollider2D CreateBox(string name, Vector2 position, Vector2 size, bool trigger)
        {
            GameObject gameObject = new GameObject(name);
            _objects.Add(gameObject);
            gameObject.transform.position = position;
            BoxCollider2D collider = gameObject.AddComponent<BoxCollider2D>();
            collider.size = size;
            collider.isTrigger = trigger;
            return collider;
        }

        GridController CreateGrid()
        {
            GameObject gameObject = new GameObject("Grid");
            _objects.Add(gameObject);
            UnityEngine.Grid unityGrid = gameObject.AddComponent<UnityEngine.Grid>();
            GridController grid = gameObject.AddComponent<GridController>();
            typeof(GridController).GetField("_grid", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(grid, unityGrid);
            return grid;
        }

        static System.Collections.Generic.IReadOnlyList<SquadSlotState> CreateUndeadSlots()
        {
            return new[]
            {
                new SquadSlotState("squad_00", "necromancer", string.Empty, 1, 3, false, "necromancer"),
                new SquadSlotState("squad_01", "wraith_knight", string.Empty, 1, 3, false, "wraith_knight"),
                new SquadSlotState("squad_02", "skeleton_bomber", string.Empty, 1, 3, false, "skeleton_bomber"),
            };
        }

        static LocalDataProvider CreateProvider()
        {
            TestAssetService assets = new TestAssetService();
            TextAsset gameData = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_LizzoPV/Data/Runtime/GameData.xml");
            assets.Register("PlayerData.xml", gameData);
            LocalDataProvider provider = new LocalDataProvider(assets);
            Assert.IsTrue(provider.InitializeAsync().GetAwaiter().GetResult().Succeeded);
            return provider;
        }

        sealed class Anchor : UndeadSummonUnityWorld.ICommanderAnchor
        {
            readonly Vector3 _position;
            readonly Vector2 _moveDirection;
            readonly Vector3 _formationForward;

            public Anchor(Vector3 position, Vector2 moveDirection, Vector3 formationForward)
            {
                _position = position;
                _moveDirection = moveDirection;
                _formationForward = formationForward;
            }

            public bool TryGetPosition(out Vector3 position) { position = _position; return true; }
            public Vector2 MoveDirection => _moveDirection;
            public Vector3 ResolveFormationForward() => _formationForward;
        }

        sealed class TestFactory : IPrefabFactory
        {
            readonly System.Collections.Generic.List<UnityEngine.Object> _objects;
            public string Address { get; private set; }
            public bool Pooled { get; private set; }
            public int ReleaseCount { get; private set; }
            public bool IncludeRuntime { get; set; } = true;
            public bool ConfigureReady { get; set; } = true;
            public GameObject LastInstance { get; private set; }

            public TestFactory(System.Collections.Generic.List<UnityEngine.Object> objects) { _objects = objects; }

            public GameObject Spawn(string address, Transform parent = null, bool pooled = false)
            {
                Address = address;
                Pooled = pooled;
                GameObject instance = new GameObject("SynergySkeleton");
                _objects.Add(instance);
                LastInstance = instance;
                instance.transform.SetParent(parent);
                if (IncludeRuntime == false)
                    return instance;
                Rigidbody2D body = instance.AddComponent<Rigidbody2D>();
                BoxCollider2D bodyCollider = instance.AddComponent<BoxCollider2D>();
                BoxCollider2D combatCollider = instance.AddComponent<BoxCollider2D>();
                UnitColliderRefs colliders = instance.AddComponent<UnitColliderRefs>();
                HitFlash flash = instance.AddComponent<HitFlash>();
                UnitVisualDriver visual = instance.AddComponent<UnitVisualDriver>();
                SynergySkeletonRuntime actor = instance.AddComponent<SynergySkeletonRuntime>();
                if (ConfigureReady == false)
                    return instance;
                SetField(colliders, "_bodyCollider", bodyCollider);
                SetField(colliders, "_combatCollider", combatCollider);
                SetField(actor, "_body", body);
                SetField(actor, "_colliders", colliders);
                SetField(actor, "_hitFlash", flash);
                SetField(actor, "_visualDriver", visual);
                return instance;
            }

            public GameObject Rent(GameObject prefab, string poolKey, Transform parent = null) => null;
            public void Release(GameObject instance) { ReleaseCount++; }
            public void Clear() { }

            static void SetField(object target, string name, object value)
            {
                target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(target, value);
            }
        }

        sealed class RecordingHits : ICombatImmediateHitModule
        {
            public int Count { get; private set; }

            public bool TryApply(in CombatImmediateHitRequest request)
            {
                Count++;
                return true;
            }
        }
    }
}
