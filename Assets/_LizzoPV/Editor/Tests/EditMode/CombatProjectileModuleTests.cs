using Lizzo.PV.Combat;
using Lizzo.PV.Gameplay.Visuals;
using Lizzo.PV.Gameplay.Units;
using System.Threading;
using System.Collections.Generic;
using System.Reflection;
using Cysharp.Threading.Tasks;
using Lizzo.PV.Combat.Projectiles;
using Lizzo.PV.Flow;
using Lizzo.PV.Gameplay.World;
using Lizzo.PV.Legion;
using Lizzo.PV.Gameplay.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class CombatProjectileModuleTests
    {
        private readonly List<Object> _objects = new List<Object>();
        private static int _lifecycleProbeSerial;
        private float _originalTimeScale;

        [Test]
        public void HomingPayload_ExpiresWithoutRemoteImpact_AndClampsTravel()
        {
            var target = CreateTarget("RemoteTarget");
            target.transform.position = Vector3.right * 100;
            var projectile = Create<CombatProjectileController>("PayloadProjectile");
            var payload = new HomingPayloadProbe { Target = target };
            projectile.Initialize(CombatProjectileRequest.CreateHoming("payload", null, Vector3.zero, target,
                10, 10, 1, .08f, AttackVisualKind.SingleHit, homingPayload: payload));
            projectile.Advance(2f);
            Assert.That(projectile.IsReleased, Is.True);
            Assert.That(payload.HitCount, Is.Zero);
            Assert.That(projectile.transform.position.x, Is.EqualTo(10).Within(.001f));
        }

        [Test]
        public void HomingPayload_UsesReplacementTarget_AndImpactsOnceAfterArrival()
        {
            var original = CreateTarget("OriginalTarget");
            original.transform.position = Vector3.right * 3;
            var replacement = CreateTarget("ReplacementTarget");
            replacement.transform.position = Vector3.up * 2;
            var projectile = Create<CombatProjectileController>("PayloadProjectile");
            var payload = new HomingPayloadProbe { Target = replacement };
            projectile.Initialize(CombatProjectileRequest.CreateHoming("payload", null, Vector3.zero, original,
                10, 10, 1, .08f, AttackVisualKind.SingleHit, homingPayload: payload));
            projectile.Advance(0f);
            Assert.That(payload.HitCount, Is.Zero);
            projectile.Advance(.1f);
            Assert.That(payload.HitCount, Is.Zero);
            projectile.Advance(.1f);
            projectile.Advance(1f);
            Assert.That(payload.HitCount, Is.EqualTo(1));
            Assert.That(payload.HitTarget, Is.SameAs(replacement));
            Assert.That(projectile.IsReleased, Is.True);
        }

        private sealed class HomingPayloadProbe : ICombatHomingPayload
        {
            internal EnemyActor Target, HitTarget;
            internal int HitCount;
            public EnemyActor ResolveTarget(Vector3 position) => Target;
            public void ApplyHit(EnemyActor target, Vector3 position) { HitCount++; HitTarget = target; }
        }

        [SetUp]
        public void SetUp()
        {
            _originalTimeScale = Time.timeScale;
            LogAssert.ignoreFailingMessages = true;
            FloatingDamageText.Configure(new RecordingFactory());
            RetroVfx.Configure(new NullAssetService(), new RecordingFactory());
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = _originalTimeScale;
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

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void EveryAllyProjectile_UsesOneSharedHitAndPreservesAttribution(bool homing, bool attributed)
        {
            var target = CreateTarget("SharedHitTarget");
            var projectile = Create<CombatProjectileController>("SharedHitProjectile");
            var hits = new Lizzo.PV.Combat.CombatImmediateHitModule();
            int applied = 0;
            Lizzo.PV.Combat.CombatImmediateHitRequest observed = default;
            hits.Applied += request => { applied++; observed = request; };
            projectile.BindHitModule(hits);
            var attribution = attributed
                ? new Lizzo.PV.Combat.CountableKillAttribution(7, "test_owner", Lizzo.PV.Combat.CombatKillSourceCategory.CompanionOwnedAction)
                : default;
            var request = homing
                ? CombatProjectileRequest.CreateHoming("test_owner", null, Vector3.zero, target, 10, 2f, 2f, .1f, AttackVisualKind.SingleHit, killAttribution: attribution)
                : CombatProjectileRequest.CreateStraight("test_owner", null, Vector3.zero, Vector3.right, 10, 2f, 2f, RetroVfxKind.None, killAttribution: attribution);
            projectile.Initialize(request);
            Assert.That(projectile.TryHit(target), Is.True);
            Assert.That(target.Hp, Is.EqualTo(40));
            Assert.That(applied, Is.EqualTo(1));
            Assert.That(observed.KillAttribution.IsAttributable, Is.EqualTo(attributed));
            Assert.That(observed.KillAttribution.OwnerInstanceId, Is.EqualTo(attributed ? 7 : 0));
            Assert.That(projectile.TryHit(target), Is.False);
            Assert.That(applied, Is.EqualTo(1));
        }
        [Test]
        public void UnlimitedPiercing_HitsBeyondFourOnceAndClearsOnReuse()
        {
            var projectile = Create<CombatProjectileController>("UnlimitedWave");
            var request = CombatProjectileRequest.CreateStraight("wave", null, Vector3.zero, Vector3.right,
                10, 2f, 2f, RetroVfxKind.None, maxDistinctTargetHits: 0);
            Assert.That(request.IsValid, Is.True);
            projectile.Initialize(request);
            var targets = new EnemyActor[12];
            for (int i = 0; i < targets.Length; i++)
            {
                targets[i] = CreateTarget("Target" + i);
                Assert.That(projectile.TryHit(targets[i]), Is.True);
                Assert.That(projectile.TryHit(targets[i]), Is.False);
                Assert.That(targets[i].Hp, Is.EqualTo(40));
            }
            Assert.That(projectile.IsReleased, Is.False);
            projectile.Release();
            projectile.Initialize(request);
            Assert.That(projectile.TryHit(targets[0]), Is.True);
            Assert.That(targets[0].Hp, Is.EqualTo(30));
            projectile.Advance(2.1f);
            Assert.That(projectile.IsReleased, Is.True);
        }

        [Test]
        public void StraightRequest_WithFourTargetCapacity_DamagesDistinctTargetsInCollisionOrder()
        {
            EnemyActor first = CreateTarget("FirstTarget");
            EnemyActor second = CreateTarget("SecondTarget");
            EnemyActor third = CreateTarget("ThirdTarget");
            EnemyActor fourth = CreateTarget("FourthTarget");
            EnemyActor fifth = CreateTarget("FifthTarget");
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
            EnemyActor target = CreateTarget("LockedTarget");
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
            using RunState runState = new RunState();
            runState.Reset(1);
            runState.MarkLoaded();
            pause.Initialize(runState);
            Assert.IsTrue(runState.TryEnd(RunOutcome.Failure, 100));

            Assert.IsFalse(projectile.TryHit(target));
            Assert.AreEqual(50, target.Hp);
            Assert.IsFalse(projectile.IsReleased);
        }

        [Test]
        public void StraightRequest_Advances_HitsAndReleases()
        {
            EnemyActor target = Create<EnemyActor>("StraightTarget");
            target.gameObject.AddComponent<EnemyHealthBar>();
            target.RestoreHealth(50);

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
            Assert.IsTrue(projectile.TryHit(target));
            Assert.AreEqual(40, target.Hp);
            Assert.IsTrue(projectile.IsReleased);
        }

        [Test]
        public void HomingRequest_TracksTarget_ReachesAndReleases()
        {
            EnemyActor target = Create<EnemyActor>("HomingTarget");
            target.gameObject.AddComponent<EnemyHealthBar>();
            HitFlash hitFlash = target.gameObject.GetComponent<HitFlash>();
            typeof(EnemyActor).GetField("_hitFlash", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(target, hitFlash);
            target.transform.position = Vector3.right;
            target.RestoreHealth(50);

            CombatProjectileController projectile = Create<CombatProjectileController>("HomingProjectile");
            projectile.Initialize(CombatProjectileRequest.CreateHoming(
                "archer_01",
                null,
                Vector3.zero,
                target,
                10,
                10.0f,
                1.0f,
                0.05f,
                AttackVisualKind.ArcherHit));

            Assert.IsFalse(projectile.IsReleased);
            typeof(EnemyActor).GetField("_hitFlash", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(target, hitFlash);
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
        public void SharedShell_AppliesDistinctSpriteAndTintPerPresentationIdentity()
        {
            CombatProjectileController homingShell = CreateProjectileShell("HomingShell", straight: false);
            Sprite clericSprite = CreateSprite(Color.white);
            ProjectilePresentationCatalog presentationSet = ScriptableObject.CreateInstance<ProjectilePresentationCatalog>();
            presentationSet.SetVisualsForEditor(homingShell.gameObject, homingShell.gameObject, new[]
            {
                new ProjectilePresentationCatalog.VisualDefinition(
                    "dmg_cleric_bolt_v1", clericSprite, Color.red, Vector3.one, Vector3.zero),
                new ProjectilePresentationCatalog.VisualDefinition(
                    "dmg_falcon_arrow_v1", null, Color.blue, Vector3.one, Vector3.zero),
            });
            _objects.Add(presentationSet);

            ProjectileSelectionFactory factory = new ProjectileSelectionFactory();
            RuntimeObjectRegistry registry = new RuntimeObjectRegistry(factory);
            CombatProjectileModule module = new CombatProjectileModule(factory, registry, new Lizzo.PV.Combat.CombatImmediateHitModule(), presentationSet);
            EnemyActor clericTarget = CreateTarget("ClericTarget");
            EnemyActor falconTarget = CreateTarget("FalconTarget");
            clericTarget.transform.position = Vector3.right * 10.0f;
            falconTarget.transform.position = Vector3.up * 10.0f;

            Assert.IsTrue(module.TrySpawn(CombatProjectileRequest.CreateHoming(
                "cleric", null, Vector3.zero, clericTarget, 10, 1.0f, 2.0f, 0.01f,
                AttackVisualKind.ArcherHit, presentationId: "dmg_cleric_bolt_v1")));
            SpriteRenderer renderer = homingShell.GetComponentInChildren<SpriteRenderer>();
            Assert.AreSame(clericSprite, renderer.sprite);
            Assert.AreEqual(Color.red, renderer.color);
            homingShell.Release();

            Assert.IsTrue(module.TrySpawn(CombatProjectileRequest.CreateHoming(
                "falcon_archer", null, Vector3.zero, falconTarget, 10, 1.0f, 2.0f, 0.01f,
                AttackVisualKind.ArcherHit, presentationId: "dmg_falcon_arrow_v1")));

            Assert.IsNull(renderer.sprite);
            Assert.AreEqual(Color.blue, renderer.color);
            CollectionAssert.AreEqual(new[] { homingShell.gameObject, homingShell.gameObject }, factory.RentedPrefabs);
        }

        [Test]
        public void ReleaseAndRerent_SharedShellDoesNotRetainPriorVisualState()
        {
            CombatProjectileController straightShell = CreateProjectileShell("StraightShell", straight: true);
            Sprite firstSprite = CreateSprite(Color.white);
            ProjectilePresentationCatalog presentationSet = ScriptableObject.CreateInstance<ProjectilePresentationCatalog>();
            presentationSet.SetVisualsForEditor(straightShell.gameObject, straightShell.gameObject, new[]
            {
                new ProjectilePresentationCatalog.VisualDefinition(
                    "test_primary", firstSprite, Color.green, new Vector3(2.0f, 3.0f, 1.0f), new Vector3(0.0f, 0.0f, 25.0f)),
                new ProjectilePresentationCatalog.VisualDefinition(
                    "test_secondary", null, Color.white, Vector3.one, Vector3.zero),
            });
            _objects.Add(presentationSet);

            ProjectileSelectionFactory factory = new ProjectileSelectionFactory();
            CombatProjectileModule module = new CombatProjectileModule(
                factory,
                new RuntimeObjectRegistry(factory),
                new Lizzo.PV.Combat.CombatImmediateHitModule(), presentationSet);

            Assert.IsTrue(module.TrySpawn(CombatProjectileRequest.CreateStraight(
                "commander", null, Vector3.zero, Vector3.right, 1, 1.0f, 2.0f, RetroVfxKind.None,
                presentationId: "test_primary")));

            Transform visual = straightShell.transform.Find("Visual");
            SpriteRenderer renderer = visual.GetComponent<SpriteRenderer>();
            Assert.AreSame(firstSprite, renderer.sprite);
            Assert.AreEqual(Color.green, renderer.color);
            Assert.AreEqual(new Vector3(2.0f, 3.0f, 1.0f), visual.localScale);
            straightShell.Release();

            Assert.IsNull(renderer.sprite);
            Assert.AreEqual(Color.white, renderer.color);
            Assert.AreEqual(Vector3.one, visual.localScale);
            Assert.That(Quaternion.Angle(visual.localRotation, Quaternion.identity), Is.LessThan(0.01f));

            Assert.IsTrue(module.TrySpawn(CombatProjectileRequest.CreateStraight(
                "commander", null, Vector3.zero, Vector3.right, 1, 1.0f, 2.0f, RetroVfxKind.None,
                presentationId: "test_secondary")));
            Assert.IsNull(renderer.sprite);
            Assert.AreEqual(Color.white, renderer.color);
            Assert.AreEqual(Vector3.one, visual.localScale);
            Assert.That(Quaternion.Angle(visual.localRotation, Quaternion.identity), Is.LessThan(0.01f));
        }

        [Test]
        public void StraightProjectile_FacesMovementDirectionWithAuthoredCorrection()
        {
            CombatProjectileController projectile = CreateProjectileShell("DirectionalStraightProjectile", straight: true);
            ProjectilePresentationCatalog presentationSet = ScriptableObject.CreateInstance<ProjectilePresentationCatalog>();
            presentationSet.SetVisualsForEditor(projectile.gameObject, projectile.gameObject, new[]
            {
                new ProjectilePresentationCatalog.VisualDefinition(
                    "test_directional", null, Color.white, Vector3.one, new Vector3(0.0f, 0.0f, 20.0f)),
            });
            _objects.Add(presentationSet);

            ProjectileSelectionFactory factory = new ProjectileSelectionFactory();
            CombatProjectileModule module = new CombatProjectileModule(
                factory,
                new RuntimeObjectRegistry(factory),
                new Lizzo.PV.Combat.CombatImmediateHitModule(), presentationSet);

            Assert.IsTrue(module.TrySpawn(CombatProjectileRequest.CreateStraight(
                "commander",
                null,
                Vector3.zero,
                Vector3.up,
                1,
                1.0f,
                2.0f,
                RetroVfxKind.None,
                presentationId: "test_directional")));

            Quaternion expected = Quaternion.Euler(0.0f, 0.0f, 90.0f) * Quaternion.Euler(0.0f, 0.0f, 20.0f);
            Assert.That(Quaternion.Angle(projectile.transform.Find("Visual").localRotation, expected), Is.LessThan(0.01f));
        }

        [Test]
        public void HomingProjectile_UpdatesFacingWithMovementAndAuthoredCorrection()
        {
            CombatProjectileController projectile = CreateProjectileShell("DirectionalHomingProjectile", straight: false);
            EnemyActor target = CreateTarget("MovingHomingTarget");
            target.transform.position = Vector3.right * 10.0f;
            ProjectilePresentationCatalog presentationSet = ScriptableObject.CreateInstance<ProjectilePresentationCatalog>();
            presentationSet.SetVisualsForEditor(projectile.gameObject, projectile.gameObject, new[]
            {
                new ProjectilePresentationCatalog.VisualDefinition(
                    "dmg_cleric_bolt_v1", null, Color.white, Vector3.one, new Vector3(0.0f, 0.0f, 15.0f)),
            });
            _objects.Add(presentationSet);

            ProjectileSelectionFactory factory = new ProjectileSelectionFactory();
            CombatProjectileModule module = new CombatProjectileModule(
                factory,
                new RuntimeObjectRegistry(factory),
                new Lizzo.PV.Combat.CombatImmediateHitModule(), presentationSet);

            Assert.IsTrue(module.TrySpawn(CombatProjectileRequest.CreateHoming(
                "cleric",
                null,
                Vector3.zero,
                target,
                1,
                1.0f,
                20.0f,
                0.01f,
                AttackVisualKind.ArcherHit,
                presentationId: "dmg_cleric_bolt_v1")));

            target.transform.position = Vector3.up * 10.0f;
            Assert.IsTrue(projectile.Advance(0.1f));

            Quaternion expected = Quaternion.Euler(0.0f, 0.0f, 90.0f) * Quaternion.Euler(0.0f, 0.0f, 15.0f);
            Assert.That(Quaternion.Angle(projectile.transform.Find("Visual").localRotation, expected), Is.LessThan(0.01f));
        }

        [Test]
        public void InvalidRequest_IsRejectedByModule()
        {
            RecordingFactory factory = new RecordingFactory();
            RuntimeObjectRegistry registry = new RuntimeObjectRegistry(factory);
            CombatProjectileModule module = new CombatProjectileModule(factory, registry, new Lizzo.PV.Combat.CombatImmediateHitModule(), null);

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

        private T Create<T>(string name) where T : Component
        {
            GameObject gameObject = new GameObject(name);
            _objects.Add(gameObject);
            if (typeof(T) == typeof(EnemyActor)) gameObject.AddComponent<HitFlash>();
            var component = gameObject.AddComponent<T>();
            if (component is EnemyActor enemy)
                typeof(EnemyActor).GetField("_hitFlash", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(enemy, gameObject.GetComponent<HitFlash>());
            if (component is CombatProjectileController projectile)
                projectile.BindHitModule(new Lizzo.PV.Combat.CombatImmediateHitModule());
            return component;
        }

        private EnemyActor CreateTarget(string name)
        {
            EnemyActor target = Create<EnemyActor>(name);
            target.gameObject.AddComponent<EnemyHealthBar>();
            target.RestoreHealth(50);
            return target;
        }

        private CombatProjectileController CreateProjectileShell(string name, bool straight)
        {
            CombatProjectileController controller = Create<CombatProjectileController>(name);
            GameObject visualObject = new GameObject("Visual");
            _objects.Add(visualObject);
            visualObject.transform.SetParent(controller.transform, false);
            SpriteRenderer bodyRenderer = visualObject.AddComponent<SpriteRenderer>();
            typeof(CombatProjectileController).GetField("_visualRoot", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(controller, visualObject.transform);
            typeof(CombatProjectileController).GetField("_bodyRenderer", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(controller, bodyRenderer);

            if (straight)
            {
                CircleCollider2D hitCollider = controller.gameObject.AddComponent<CircleCollider2D>();
                hitCollider.isTrigger = true;
                VisibilityCullProbe visibilityProbe = controller.gameObject.AddComponent<VisibilityCullProbe>();
                typeof(CombatProjectileController).GetField("_hitCollider", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(controller, hitCollider);
                typeof(CombatProjectileController).GetField("_visibilityProbe", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(controller, visibilityProbe);
            }

            return controller;
        }

        private Sprite CreateSprite(Color color)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            Sprite sprite = Sprite.Create(texture, new Rect(0.0f, 0.0f, 1.0f, 1.0f), new Vector2(0.5f, 0.5f));
            _objects.Add(sprite);
            _objects.Add(texture);
            return sprite;
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
            public bool TryGetCached<T>(string address, out T asset) where T : Object
            {
                asset = null;
                return false;
            }
            public UniTask<T> LoadAsync<T>(string address, CancellationToken cancellationToken = default) where T : Object => default;
            public UniTask<AssetPreloadResult> PreloadLabelAsync<T>(string label, CancellationToken cancellationToken = default) where T : Object => default;
            public void Release(string address) { }
            public void ReleaseAll() { }
        }

        private sealed class ProjectileSelectionFactory : IPrefabFactory
        {
            public readonly List<GameObject> RentedPrefabs = new List<GameObject>();

            public GameObject Spawn(string address, Transform parent = null, bool pooled = false) => null;

            public GameObject Rent(GameObject prefab, string poolKey, Transform parent = null)
            {
                RentedPrefabs.Add(prefab);
                return prefab;
            }

            public void Release(GameObject instance) { }
            public void Clear() { }
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
