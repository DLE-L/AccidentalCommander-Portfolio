using System.Threading;
using System.Collections.Generic;
using System.Reflection;
using Cysharp.Threading.Tasks;
using Lizzo.PV.Combat.Projectiles;
using Lizzo.PV.Legion;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class CombatProjectileModuleTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();

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
                RetroVfxKind.ProjectileHit));

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
                RetroVfxKind.ProjectileHit);

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
                RetroVfxKind.ProjectileHit);

            projectile.Initialize(request);

            Assert.AreEqual("commander", projectile.Request.SourceId);
            Assert.AreEqual(RetroVfxKind.ProjectileHit, projectile.Request.StraightHitFeedback);
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
}
