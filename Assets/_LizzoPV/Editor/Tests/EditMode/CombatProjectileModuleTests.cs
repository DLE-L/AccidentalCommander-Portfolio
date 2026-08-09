using System.Threading;
using System.Collections.Generic;
using System.Reflection;
using Cysharp.Threading.Tasks;
using Lizzo.PV.Combat.Projectiles;
using Lizzo.PV.Flow;
using Lizzo.PV.Legion;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class CombatProjectileModuleTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();
        private static int _lifecycleProbeSerial;

        [SetUp]
        public void SetUp()
        {
            LogAssert.ignoreFailingMessages = true;
            FloatingDamageText.Configure(new RecordingFactory());
            RetroVfx.Configure(new NullAssetService(), new RecordingFactory());
        }

        [TearDown]
        public void TearDown()
        {
            FloatingDamageText.ClearServices();
            RetroVfx.ClearServices();
            LogAssert.ignoreFailingMessages = false;
            for (int i = _objects.Count - 1; i >= 0; i--)
            {
                if (_objects[i] != null)
                    Object.DestroyImmediate(_objects[i]);
            }

            _objects.Clear();
        }

        [Test]
        public void StraightRequest_WithFourTargetCapacity_DamagesDistinctTargetsInCollisionOrder()
        {
            MonsterController first = CreateTarget("FirstTarget");
            MonsterController second = CreateTarget("SecondTarget");
            MonsterController third = CreateTarget("ThirdTarget");
            MonsterController fourth = CreateTarget("FourthTarget");
            MonsterController fifth = CreateTarget("FifthTarget");
            CombatProjectileController projectile = Create<CombatProjectileController>("PiercingProjectile");
            projectile.Initialize(CombatProjectileRequest.CreateStraight(
                "commander",
                null,
                Vector3.zero,
                Vector3.right,
                10,
                2.0f,
                2.0f,
                RetroVfxKind.None,
                maxDistinctTargetHits: 4));

            ExpectFloatingDamageTextLog();
            Assert.IsTrue(projectile.TryHit(first));
            Assert.IsFalse(projectile.TryHit(first));
            Assert.AreEqual(40, first.Hp);
            Assert.IsTrue(projectile.TryHit(second));
            Assert.IsTrue(projectile.TryHit(third));
            Assert.IsTrue(projectile.TryHit(fourth));

            Assert.IsTrue(projectile.IsReleased);
            Assert.IsFalse(projectile.TryHit(fifth));
            Assert.AreEqual(40, second.Hp);
            Assert.AreEqual(40, third.Hp);
            Assert.AreEqual(40, fourth.Hp);
            Assert.AreEqual(50, fifth.Hp);
        }

        [Test]
        public void StraightRequest_ResultLockPreventsHit()
        {
            MonsterController target = CreateTarget("LockedTarget");
            CombatProjectileController projectile = Create<CombatProjectileController>("LockedProjectile");
            projectile.Initialize(CombatProjectileRequest.CreateStraight(
                "commander",
                null,
                Vector3.zero,
                Vector3.right,
                10,
                2.0f,
                2.0f,
                RetroVfxKind.None));
            RunPauseController pause = Create<RunPauseController>("ResultPause");
            pause.Initialize();
            pause.MarkRunEnded();

            Assert.IsFalse(projectile.TryHit(target));
            Assert.AreEqual(50, target.Hp);
            Assert.IsFalse(projectile.IsReleased);
        }

        [Test]
        public void StraightRequest_Advances_HitsAndReleases()
        {
            MonsterController target = Create<MonsterController>("StraightTarget");
            target.gameObject.AddComponent<EnemyHealthBar>();
            target.Hp = 50;

            CombatProjectileController projectile = Create<CombatProjectileController>("StraightProjectile");
            projectile.Initialize(CombatProjectileRequest.CreateStraight(
                "commander",
                null,
                Vector3.zero,
                Vector3.right,
                10,
                2.0f,
                2.0f,
                RetroVfxKind.None));

            Assert.IsTrue(projectile.Advance(0.25f));
            Assert.That(projectile.transform.position.x, Is.EqualTo(0.5f).Within(0.0001f));
            LogAssert.Expect(LogType.Error, "[FloatingDamageText] Authored prefab is not cached: FloatingDamageText.prefab");
            Assert.IsTrue(projectile.TryHit(target));
            Assert.AreEqual(40, target.Hp);
            Assert.IsTrue(projectile.IsReleased);
        }

        [Test]
        public void HomingRequest_TracksTarget_ReachesAndReleases()
        {
            MonsterController target = Create<MonsterController>("HomingTarget");
            target.gameObject.AddComponent<EnemyHealthBar>();
            HitFlash hitFlash = target.gameObject.AddComponent<HitFlash>();
            typeof(MonsterController).GetField("_hitFlash", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(target, hitFlash);
            target.transform.position = Vector3.right;
            target.Hp = 50;

            CombatProjectileController projectile = Create<CombatProjectileController>("HomingProjectile");
            projectile.Initialize(CombatProjectileRequest.CreateHoming(
                "archer_01",
                null,
                null,
                Vector3.zero,
                target,
                10,
                10.0f,
                1.0f,
                0.05f,
                AttackVisualKind.ArcherHit));

            Assert.IsFalse(projectile.IsReleased);
            typeof(MonsterController).GetField("_hitFlash", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(target, hitFlash);
            LogAssert.Expect(LogType.Error, "[FloatingDamageText] Authored prefab is not cached: FloatingDamageText.prefab");
            Assert.IsFalse(projectile.Advance(0.1f));
            Assert.AreEqual(40, target.Hp);
            Assert.IsTrue(projectile.IsReleased);
        }

        [Test]
        public void ClearServices_ClearsBufferedDamageBeforeReconfigure()
        {
            Vector3 position = new Vector3(10000.0f + ++_lifecycleProbeSerial, 20000.0f, 0.0f);
            RecordingFactory firstFactory = new RecordingFactory();
            FloatingDamageText.Configure(firstFactory);
            LogAssert.Expect(LogType.Error, "[FloatingDamageText] Authored prefab is not cached: FloatingDamageText.prefab");
            FloatingDamageText.ShowEnemyDamage(position, 10);
            Assert.AreEqual(1, firstFactory.SpawnCount);

            FloatingDamageText.ClearServices();

            RecordingFactory secondFactory = new RecordingFactory();
            try
            {
                FloatingDamageText.Configure(secondFactory);
                LogAssert.Expect(LogType.Error, "[FloatingDamageText] Authored prefab is not cached: FloatingDamageText.prefab");
                FloatingDamageText.ShowEnemyDamage(position, 10);
                Assert.AreEqual(1, secondFactory.SpawnCount);
            }
            finally
            {
                FloatingDamageText.ClearServices();
            }
        }

        [Test]
        public void InvalidRequest_IsRejectedByModule()
        {
            RecordingFactory factory = new RecordingFactory();
            RuntimeObjectRegistry registry = new RuntimeObjectRegistry(factory);
            CombatProjectileModule module = new CombatProjectileModule(factory, registry);

            CombatProjectileRequest invalid = CombatProjectileRequest.CreateStraight(
                "commander",
                null,
                Vector3.up,
                Vector3.zero,
                0,
                10.0f,
                1.0f,
                RetroVfxKind.None);

            Assert.IsFalse(invalid.IsValid);
            Assert.IsFalse(module.TrySpawn(invalid));
            Assert.AreEqual(0, factory.SpawnCount);
        }

        [Test]
        public void EmptyVisual_IsAccepted_AndRequestIdentityIsPreserved()
        {
            CombatProjectileController projectile = Create<CombatProjectileController>("EmptyVisualProjectile");
            CombatProjectileRequest request = CombatProjectileRequest.CreateStraight(
                "commander",
                null,
                Vector3.zero,
                Vector3.up,
                3,
                1.0f,
                1.0f,
                RetroVfxKind.None);

            projectile.Initialize(request);

            Assert.AreEqual("commander", projectile.Request.SourceId);
            Assert.AreEqual(RetroVfxKind.None, projectile.Request.StraightHitFeedback);
            Assert.IsTrue(projectile.Advance(0.1f));
            Assert.That(projectile.transform.position.y, Is.EqualTo(0.1f).Within(0.0001f));
        }

        [Test]
        public void HomingRequest_CancelsWhenSourceGoesDown()
        {
            MonsterController target = Create<MonsterController>("LiveTarget");
            target.transform.position = Vector3.right * 5.0f;
            CompanionRuntime source = Create<CompanionRuntime>("ArcherSource");
            typeof(CompanionRuntime).GetProperty("IsDown", BindingFlags.Instance | BindingFlags.Public)
                .SetValue(source, true);

            CombatProjectileController projectile = Create<CombatProjectileController>("CancelledProjectile");
            projectile.Initialize(CombatProjectileRequest.CreateHoming(
                "archer_01",
                null,
                source,
                Vector3.zero,
                target,
                10,
                10.0f,
                1.0f,
                0.05f,
                AttackVisualKind.ArcherHit));

            Assert.IsFalse(projectile.Advance(0.1f));
            Assert.IsTrue(projectile.IsReleased);
            Assert.AreEqual(100, target.Hp);
        }

        private T Create<T>(string name) where T : Component
        {
            GameObject gameObject = new GameObject(name);
            _objects.Add(gameObject);
            return gameObject.AddComponent<T>();
        }

        private MonsterController CreateTarget(string name)
        {
            MonsterController target = Create<MonsterController>(name);
            target.gameObject.AddComponent<EnemyHealthBar>();
            target.Hp = 50;
            return target;
        }

        private static void ExpectFloatingDamageTextLog()
        {
            LogAssert.Expect(LogType.Error, "[FloatingDamageText] Authored prefab is not cached: FloatingDamageText.prefab");
        }

        private sealed class RecordingFactory : IPrefabFactory
        {
            public int SpawnCount { get; private set; }

            public GameObject Spawn(string address, Transform parent = null, bool pooled = false)
            {
                SpawnCount++;
                return null;
            }

            public GameObject Rent(GameObject prefab, string poolKey, Transform parent = null) => null;
            public void Release(GameObject instance) { }
            public void Clear() { }
        }

        private sealed class NullAssetService : IAssetService
        {
            public T GetCached<T>(string address) where T : Object => null;
            public UniTask<T> LoadAsync<T>(string address, CancellationToken cancellationToken = default) where T : Object => default;
            public UniTask<AssetPreloadResult> PreloadLabelAsync<T>(string label, CancellationToken cancellationToken = default) where T : Object => default;
            public void Release(string address) { }
            public void ReleaseAll() { }
        }
    }

    public sealed class CombatProjectilePoolLifecycleTests
    {
        [SetUp]
        public void SetUp()
        {
            LogAssert.ignoreFailingMessages = false;
            ProjectilePoolActivationProbe.ResetProbe();
        }

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = false;
            ProjectilePoolActivationProbe.ResetProbe();
        }

        [Test]
        public void RegistryClear_ReentrantProjectileReleaseReturnsExactlyOnce()
        {
            GameObject poolRoot = new GameObject("ProjectilePoolRoot");
            ObjectPoolService pool = new ObjectPoolService(poolRoot.transform);
            ReentrantPoolFactory factory = new ReentrantPoolFactory(pool, true);
            RuntimeObjectRegistry registry = new RuntimeObjectRegistry(factory);
            GameObject prefab = CreateInactiveProjectilePrefab("CommanderProjectile");

            GameObject instance = pool.Rent(prefab, "CommanderProjectile");
            CombatProjectileController projectile = instance.GetComponent<CombatProjectileController>();
            projectile.BindRegistry(registry);
            registry.RegisterProjectile(projectile);
            projectile.Initialize(CreateRequest("commander"));

            registry.Clear();

            Assert.That(factory.ReleaseCount, Is.EqualTo(1));
            Assert.That(registry.Projectiles.Count, Is.EqualTo(0));
            Assert.That(pool.ActiveCount, Is.EqualTo(0));

            Cleanup(poolRoot, prefab);
        }

        [Test]
        public void ReleaseProjectilesBySourceId_UsesStateSafeProjectileRelease()
        {
            GameObject poolRoot = new GameObject("ProjectilePoolRoot");
            ObjectPoolService pool = new ObjectPoolService(poolRoot.transform);
            ReentrantPoolFactory factory = new ReentrantPoolFactory(pool, true);
            RuntimeObjectRegistry registry = new RuntimeObjectRegistry(factory);
            GameObject prefab = CreateInactiveProjectilePrefab("ArcherProjectileVisual");

            GameObject instance = pool.Rent(prefab, "ArcherProjectileVisual");
            CombatProjectileController projectile = instance.GetComponent<CombatProjectileController>();
            projectile.BindRegistry(registry);
            registry.RegisterProjectile(projectile);
            projectile.Initialize(CreateRequest("magic_chain"));

            registry.ReleaseProjectilesBySourceId("magic_chain");

            Assert.That(factory.ReleaseCount, Is.EqualTo(1));
            Assert.That(registry.Projectiles.Count, Is.EqualTo(0));
            Assert.That(pool.ActiveCount, Is.EqualTo(0));

            Cleanup(poolRoot, prefab);
        }

        [Test]
        public void ParentlessRentReturn_RetainsStableBucketParent()
        {
            GameObject poolRoot = new GameObject("ProjectilePoolRoot");
            ObjectPoolService pool = new ObjectPoolService(poolRoot.transform);
            GameObject prefab = CreateActiveProjectilePrefab("WorldProjectile");
            ProjectilePoolActivationProbe.BeginRecording(null);

            GameObject instance = pool.Rent(prefab, "WorldProjectile");
            Transform bucketRoot = instance.transform.parent;
            Assert.That(bucketRoot, Is.Not.Null);
            Assert.That(bucketRoot.parent, Is.EqualTo(poolRoot.transform));
            Assert.That(ProjectilePoolActivationProbe.Count, Is.EqualTo(1));
            Assert.That(ProjectilePoolActivationProbe.FirstParent, Is.EqualTo(bucketRoot));

            Assert.IsTrue(pool.Return(instance));
            Assert.That(instance.transform.parent, Is.EqualTo(bucketRoot));
            Assert.IsFalse(instance.activeSelf);

            GameObject rerented = pool.Rent(prefab, "WorldProjectile");
            Assert.That(rerented.transform.parent, Is.EqualTo(bucketRoot));
            Assert.IsTrue(rerented.activeSelf);
            Assert.IsTrue(pool.Return(rerented));

            Cleanup(poolRoot, prefab);
        }

        [Test]
        public void ExplicitParentRent_AdjustsParentBeforeActivation()
        {
            GameObject poolRoot = new GameObject("ProjectilePoolRoot");
            ObjectPoolService pool = new ObjectPoolService(poolRoot.transform);
            GameObject prefab = CreateActiveProjectilePrefab("CardProjectile");
            GameObject parentObject = new GameObject("CardParent");
            ProjectilePoolActivationProbe.BeginRecording(parentObject.transform);

            GameObject instance = pool.Rent(prefab, "CardProjectile", parentObject.transform);

            Assert.That(instance.transform.parent, Is.EqualTo(parentObject.transform));
            Assert.IsTrue(instance.activeSelf);
            Assert.That(ProjectilePoolActivationProbe.Count, Is.EqualTo(1));
            Assert.That(ProjectilePoolActivationProbe.FirstParent, Is.EqualTo(parentObject.transform));
            Assert.That(ProjectilePoolActivationProbe.WrongParentCount, Is.EqualTo(0));
            Assert.IsTrue(pool.Return(instance));
            Assert.That(instance.transform.parent, Is.EqualTo(parentObject.transform));

            Cleanup(poolRoot, prefab);
            Object.DestroyImmediate(parentObject);
        }

        private static CombatProjectileRequest CreateRequest(string sourceId)
        {
            return CombatProjectileRequest.CreateStraight(
                sourceId,
                null,
                Vector3.zero,
                Vector3.right,
                1,
                1.0f,
                1.0f,
                RetroVfxKind.None);
        }

        private static GameObject CreateInactiveProjectilePrefab(string name)
        {
            GameObject prefab = new GameObject(name);
            prefab.SetActive(false);
            prefab.AddComponent<CombatProjectileController>();
            return prefab;
        }

        private static GameObject CreateActiveProjectilePrefab(string name)
        {
            GameObject prefab = new GameObject(name);
            prefab.AddComponent<CombatProjectileController>();
            prefab.AddComponent<ProjectilePoolActivationProbe>();
            return prefab;
        }

        private static void Cleanup(GameObject poolRoot, GameObject prefab)
        {
            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(poolRoot);
        }

        private sealed class ReentrantPoolFactory : IPrefabFactory
        {
            private readonly ObjectPoolService _pool;
            private readonly bool _triggerVisibilityExit;

            public ReentrantPoolFactory(ObjectPoolService pool, bool triggerVisibilityExit)
            {
                _pool = pool;
                _triggerVisibilityExit = triggerVisibilityExit;
            }

            public int ReleaseCount { get; private set; }

            public GameObject Spawn(string address, Transform parent = null, bool pooled = false) => null;

            public GameObject Rent(GameObject prefab, string poolKey, Transform parent = null)
            {
                return _pool.Rent(prefab, poolKey, parent);
            }

            public void Release(GameObject instance)
            {
                ReleaseCount++;
                if (_triggerVisibilityExit)
                    instance.GetComponent<CombatProjectileController>()?.OnVisibilityExit(null);
                _pool.Return(instance);
            }

            public void Clear() => _pool.Clear();
        }

    }

    [ExecuteAlways]
    public sealed class ProjectilePoolActivationProbe : MonoBehaviour
    {
        private static bool _recording;
        private static Transform _expectedParent;

        public static int Count { get; private set; }
        public static int WrongParentCount { get; private set; }
        public static Transform FirstParent { get; private set; }

        public static void ResetProbe()
        {
            _recording = false;
            _expectedParent = null;
            Count = 0;
            WrongParentCount = 0;
            FirstParent = null;
        }

        public static void BeginRecording(Transform expectedParent)
        {
            _expectedParent = expectedParent;
            _recording = true;
        }

        private void OnEnable()
        {
            if (!_recording)
                return;

            Count++;
            if (Count == 1)
                FirstParent = transform.parent;
            if (_expectedParent != null && transform.parent != _expectedParent)
                WrongParentCount++;
        }
    }
}
