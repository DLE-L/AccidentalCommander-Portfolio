using System.Collections.Generic;
using System.Reflection;
using Lizzo.PV.Combat;
using Lizzo.PV.Combat.Fields;
using Lizzo.PV.Combat.Projectiles;
using Lizzo.PV.Data;
using Lizzo.PV.Flow;
using Lizzo.PV.Legion;
using Lizzo.PV.Legion.Combat;
using Lizzo.PV.Legion.Synergy;
using Lizzo.PV.P0.Presentation;
using Lizzo.PV.P0.Units;
using Lizzo.PV.P0.Visuals;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class SynergyMagicChainTests
    {
        private static readonly FieldInfo ActiveProvider =
            typeof(PresentationCatalogProvider).GetField(
                "_active",
                BindingFlags.Static | BindingFlags.NonPublic);
        private PresentationCatalogProvider _previousProvider;
        private GameObject _providerRoot;
        private PresentationCatalog _catalog;

        [SetUp]
        public void SetUp()
        {
            _previousProvider = ActiveProvider.GetValue(null) as PresentationCatalogProvider;
            UnitPresentationSet units = AssetDatabase.LoadAssetAtPath<UnitPresentationSet>("Assets/_LizzoPV/Gameplay/Presentation/Data/UnitPresentationSet.asset");
            _catalog = ScriptableObject.CreateInstance<PresentationCatalog>();
            _catalog.SetPresentationSetsForEditor(null, null, null, null, units);
            _providerRoot = new GameObject("MagicChainCatalog");
            _providerRoot.SetActive(false);
            PresentationCatalogProvider provider = _providerRoot.AddComponent<PresentationCatalogProvider>();
            SerializedObject serialized = new(provider);
            serialized.FindProperty("_catalog").objectReferenceValue = _catalog;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            ActiveProvider.SetValue(null, provider);
        }

        [TearDown]
        public void TearDown()
        {
            if (_providerRoot != null) Object.DestroyImmediate(_providerRoot);
            if (_catalog != null) Object.DestroyImmediate(_catalog);
            ActiveProvider.SetValue(null, _previousProvider);
        }

        [Test]
        public void Assignment_OrdersByAnchorDistanceThenSpawnSequence_AndFillsFiveWithNearest()
        {
            MagicChainCandidate[] candidates =
            {
                new(null, new Vector3(2, 0), 4, true),
                new(null, new Vector3(1, 0), 9, true),
                new(null, new Vector3(1, 0), 3, true),
            }
            ;
            List<MagicChainCandidate> assignments = new();

            MagicChainAssignmentRules.SelectAssignments(candidates, Vector3.zero, 5.0f, 5, assignments);

            Assert.That(assignments, Has.Count.EqualTo(5));
            Assert.That(assignments[0].SpawnSequence, Is.EqualTo(3));
            Assert.That(assignments[1].SpawnSequence, Is.EqualTo(9));
            Assert.That(assignments[2].SpawnSequence, Is.EqualTo(4));
            Assert.That(assignments[3].SpawnSequence, Is.EqualTo(3));
            Assert.That(assignments[4].SpawnSequence, Is.EqualTo(3));
        }

        [Test]
        public void CanonicalMagicCastStream_UsesFamilyTagsNumericCastIdsAndThresholdThree()
        {
            LocalDataProvider data = CreateProvider();
            RecordingFactory factory = new();
            RuntimeObjectRegistry registry = new(factory);
            using PartyService party = new(data, registry, factory, new NoProjectile(), new NoImmediate(), new NoField());
            using SynergyActivationState activations = new(data);
            using SynergyTriggerState triggers = new(activations);
            using CanonicalCompanionCastStream stream = new();
            using MagicChainSynergy magic = new(data, activations, triggers, party, registry, new NoProjectile(), stream);

            activations.Refresh(CreateSlots("fire_mage", "lightning_mage", "necromancer"));
            Assert.That(triggers.TryConsumePending(SynergyActivationIds.MagicChain, out _), Is.True);

            stream.TryEmit(new CanonicalCompanionCastIdentity(10, "squad_00", "fire_mage", "magic_family"), CanonicalCompanionActionKind.BasicAttack);
            stream.TryEmit(new CanonicalCompanionCastIdentity(11, "squad_01", "falcon_archer", "ranged_family"), CanonicalCompanionActionKind.BasicAttack);
            stream.TryEmit(new CanonicalCompanionCastIdentity(12, "squad_02", "lightning_mage", "magic_family"), CanonicalCompanionActionKind.ActiveSkill);
            stream.TryEmit(new CanonicalCompanionCastIdentity(13, "squad_02", "necromancer", "magic_family"), CanonicalCompanionActionKind.BasicAttack);

            Assert.That(triggers.GetCounter(SynergyActivationIds.MagicChain), Is.EqualTo(0));
            Assert.That(triggers.HasPending(SynergyActivationIds.MagicChain), Is.True);
        }

        [Test]
        public void PendingRoundWithoutRepresentativeOrTarget_IsConsumedWithoutProjectileRequests()
        {
            LocalDataProvider data = CreateProvider();
            RecordingFactory factory = new();
            RuntimeObjectRegistry registry = new(factory);
            using PartyService party = new(data, registry, factory, new NoProjectile(), new NoImmediate(), new NoField());
            using SynergyActivationState activations = new(data);
            using SynergyTriggerState triggers = new(activations);
            using CanonicalCompanionCastStream stream = new();
            using MagicChainSynergy magic = new(data, activations, triggers, party, registry, new NoProjectile(), stream);

            activations.Refresh(CreateSlots("fire_mage", "lightning_mage", "necromancer"));
            Assert.That(magic.TryResolvePending(), Is.True);
            Assert.That(magic.LastSpawnedProjectileCount, Is.Zero);
            Assert.That(triggers.HasPending(SynergyActivationIds.MagicChain), Is.False);
        }

        [Test]
        public void SynergyAttribution_IsRetainedForGenericConsumersButExcludedFromCompanionOnlyCredits()
        {
            using RunState state = new();
            CountableKillAttribution generic = default;
            int genericCount = 0;
            int companionCount = 0;
            state.KillAttributed += attribution => {
                generic = attribution;
                genericCount++;
            }
            ;
            state.CountableKillAttributed += _ => companionCount++;
            state.Reset(1);
            state.MarkLoaded();

            CountableKillAttribution attribution = new(0, SynergyActivationIds.MagicChain, CombatKillSourceCategory.SynergyAction);
            state.RegisterCountableKill(attribution);

            Assert.That(attribution.IsAttributable, Is.True);
            Assert.That(attribution.IsCountable, Is.False);
            Assert.That(genericCount, Is.EqualTo(1));
            Assert.That(companionCount, Is.Zero);
            Assert.That(generic.SourceId, Is.EqualTo(SynergyActivationIds.MagicChain));
            Assert.That(generic.Category, Is.EqualTo(CombatKillSourceCategory.SynergyAction));
        }

        [Test]
        public void MagicChainProjectileRuntimeFixture_RealModuleCreatesFiveExactHomingRequests()
        {
            using MagicChainProjectileRuntimeFixture fixture = new();
            MonsterController primary = fixture.CreateTarget(Vector3.right * 4.0f);
            CountableKillAttribution attribution = new(0, SynergyActivationIds.MagicChain, CombatKillSourceCategory.SynergyAction);
            for (int index = 0;
            index < 5;
            index++)
                Assert.That(fixture.Module.TrySpawn(fixture.CreateMagicRequest(primary, index == 4 ? 2 : 10, attribution)), Is.True);

            Assert.That(fixture.Factory.Projectiles, Has.Count.EqualTo(5));
            foreach (CombatProjectileController projectile in fixture.Factory.Projectiles)
            {
                Assert.That(projectile.Request.DeliveryMode, Is.EqualTo(CombatProjectileDeliveryMode.HomingTarget));
                Assert.That(projectile.Request.Lifetime, Is.EqualTo(2.0f));
                Assert.That(projectile.Request.SourceId, Is.EqualTo(SynergyActivationIds.MagicChain));
                Assert.That(projectile.Request.KillAttribution.Category, Is.EqualTo(CombatKillSourceCategory.SynergyAction));
                Assert.That(projectile.Request.Target, Is.SameAs(primary));
            }
            Assert.That(fixture.Factory.Projectiles[0].Request.Damage, Is.EqualTo(10));
            Assert.That(fixture.Factory.Projectiles[4].Request.Damage, Is.EqualTo(2));
        }

        [Test]
        public void MagicChainProjectileRuntimeFixture_TargetLossAndLifetimeReleaseWithoutRetarget()
        {
            using MagicChainProjectileRuntimeFixture fixture = new();
            CountableKillAttribution attribution = new(0, SynergyActivationIds.MagicChain, CombatKillSourceCategory.SynergyAction);
            MonsterController lost = fixture.CreateTarget(Vector3.right * 4.0f);
            Assert.That(fixture.Module.TrySpawn(fixture.CreateMagicRequest(lost, 10, attribution)), Is.True);
            CombatProjectileController lostProjectile = fixture.Factory.Projectiles[0];
            lost.enabled = false;
            Assert.That(lostProjectile.Advance(0.1f), Is.False);
            Assert.That(lostProjectile.IsReleased, Is.True);

            MonsterController live = fixture.CreateTarget(Vector3.right * 100.0f);
            Assert.That(fixture.Module.TrySpawn(fixture.CreateMagicRequest(live, 10, attribution)), Is.True);
            CombatProjectileController lifetimeProjectile = fixture.Factory.Projectiles[1];
            Assert.That(lifetimeProjectile.Advance(2.0f), Is.False);
            Assert.That(lifetimeProjectile.IsReleased, Is.True);
        }

        [Test]
        public void MagicChainProjectileRuntimeFixture_SourceSpecificReleaseLeavesUnrelatedProjectile()
        {
            using MagicChainProjectileRuntimeFixture fixture = new();
            MonsterController target = fixture.CreateTarget(Vector3.right * 4.0f);
            CountableKillAttribution attribution = new(0, SynergyActivationIds.MagicChain, CombatKillSourceCategory.SynergyAction);
            Assert.That(fixture.Module.TrySpawn(fixture.CreateMagicRequest(target, 10, attribution)), Is.True);
            Assert.That(fixture.Module.TrySpawn(CombatProjectileRequest.CreateHoming("commander", null, null, Vector3.zero, target, 10, 22.0f, 2.0f,
                0.08f, AttackVisualKind.SingleHit)), Is.True);

            fixture.Registry.ReleaseProjectilesBySourceId(SynergyActivationIds.MagicChain);

            Assert.That(fixture.Registry.Projectiles, Has.Count.EqualTo(1));
            foreach (CombatProjectileController projectile in fixture.Registry.Projectiles)
                Assert.That(projectile.Request.SourceId, Is.EqualTo("commander"));
        }

        [Test]
        public void CanonicalMagicActivation_ExecutorCreatesExactRealRequests_AndLethalControllerEmitsGenericSynergyKillOnly()
        {
            using CanonicalMagicExecutorFixture fixture = new();
            fixture.RecruitMagicSquads();
            MonsterController lethal = fixture.CreateTarget(Vector3.right, 10, false);
            MonsterController boss = fixture.CreateTarget(Vector3.right * 2.0f, 1000, true);
            fixture.CreateTarget(Vector3.right * 3.0f, 100, false);
            fixture.CreateTarget(Vector3.right * 4.0f, 100, false);
            fixture.CreateTarget(Vector3.right * 5.0f, 100, false);

            CountableKillAttribution generic = default;
            int genericCount = 0;
            int companionCount = 0;
            fixture.Run.State.KillAttributed += attribution => {
                generic = attribution;
                genericCount++;
            }
            ;
            fixture.Run.State.CountableKillAttributed += _ => companionCount++;

            Assert.That(fixture.Run.Synergies.IsActive(SynergyActivationIds.MagicChain), Is.True);
            Assert.That(fixture.Run.SynergyTriggers.HasPending(SynergyActivationIds.MagicChain), Is.True);
            Assert.That(fixture.Run.MagicChain.TryResolvePending(), Is.True);
            Assert.That(fixture.Run.MagicChain.LastSpawnedProjectileCount, Is.EqualTo(5));
            Assert.That(fixture.Factory.Projectiles, Has.Count.EqualTo(5));
            foreach (CombatProjectileController projectile in fixture.Factory.Projectiles)
            {
                Assert.That(projectile.Request.DeliveryMode, Is.EqualTo(CombatProjectileDeliveryMode.HomingTarget));
                Assert.That(projectile.Request.Lifetime, Is.EqualTo(2.0f));
                Assert.That(projectile.Request.SourceId, Is.EqualTo(SynergyActivationIds.MagicChain));
                Assert.That(projectile.Request.KillAttribution.Category, Is.EqualTo(CombatKillSourceCategory.SynergyAction));
            }
            CombatProjectileController lethalProjectile = fixture.Factory.Projectiles.Find(projectile => projectile.Request.Target == lethal);
            CombatProjectileController bossProjectile = fixture.Factory.Projectiles.Find(projectile => projectile.Request.Target == boss);
            Assert.That(lethalProjectile, Is.Not.Null);
            Assert.That(lethalProjectile.Request.Damage, Is.EqualTo(10));
            Assert.That(bossProjectile, Is.Not.Null);
            Assert.That(bossProjectile.Request.Damage, Is.EqualTo(2));

            lethal.transform.position = lethalProjectile.transform.position;
            Assert.That(lethalProjectile.Advance(0.0f), Is.False);
            Assert.That(genericCount, Is.EqualTo(1));
            Assert.That(companionCount, Is.Zero);
            Assert.That(generic.SourceId, Is.EqualTo(SynergyActivationIds.MagicChain));
            Assert.That(generic.Category, Is.EqualTo(CombatKillSourceCategory.SynergyAction));
        }

        static LocalDataProvider CreateProvider()
        {
            TestAssetService assets = new();
            assets.Register("PlayerData.xml", AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_LizzoPV/Gameplay/RunData/Data/GameData.xml"));
            LocalDataProvider provider = new(assets);
            Assert.That(provider.InitializeAsync().GetAwaiter().GetResult().Succeeded, Is.True);
            return provider;
        }

        static SquadSlotState[] CreateSlots(params string[] baseUnitIds)
        {
            SquadSlotState[] slots = new SquadSlotState[baseUnitIds.Length];
            for (int index = 0;
            index < baseUnitIds.Length;
            index++)
                slots[index] = new SquadSlotState($"squad_{index:00}", baseUnitIds[index], string.Empty, 1, 3, false, baseUnitIds[index]);
            return slots;
        }

        sealed class RecordingFactory : IPrefabFactory
        {
            public GameObject Spawn(string address, Transform parent = null, bool pooled = false) => null;
            public GameObject Rent(GameObject prefab, string poolKey, Transform parent = null) => null;
            public void Release(GameObject instance) {
            }
            public void Clear() {
            }
        }

        sealed class MagicChainProjectileRuntimeFixture : System.IDisposable
        {
            readonly List<GameObject> _objects = new();
            public readonly ProjectileFactory Factory;
            public readonly RuntimeObjectRegistry Registry;
            public readonly CombatProjectileModule Module;

            public MagicChainProjectileRuntimeFixture()
            {
                Factory = new ProjectileFactory(_objects);
                Registry = new RuntimeObjectRegistry(Factory);
                Module = new CombatProjectileModule(Factory, Registry);
            }

            public MonsterController CreateTarget(Vector3 position)
            {
                GameObject instance = new("MagicTarget");
                _objects.Add(instance);
                instance.transform.position = position;
                MonsterController target = instance.AddComponent<MonsterController>();
                EnemyHealthBar health = instance.AddComponent<EnemyHealthBar>();
                HitFlash flash = instance.AddComponent<HitFlash>();
                typeof(MonsterController).GetField("_healthBar", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, health);
                typeof(MonsterController).GetField("_hitFlash", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, flash);
                target.MaxHp = 1000;
                target.Hp = 1000;
                Registry.RegisterEnemy(target);
                return target;
            }

            public CombatProjectileRequest CreateMagicRequest(MonsterController target, int damage, CountableKillAttribution attribution) =>
                CombatProjectileRequest.CreateHoming(SynergyActivationIds.MagicChain, null, null, Vector3.zero, target, damage, 22.0f, 2.0f, 0.08f,
                    AttackVisualKind.SingleHit, killAttribution: attribution);

            public void Dispose()
            {
                for (int index = _objects.Count - 1;
                index >= 0;
                index--)
                    if (_objects[index] != null) Object.DestroyImmediate(_objects[index]);
            }
        }

        sealed class CanonicalMagicExecutorFixture : System.IDisposable
        {
            readonly List<GameObject> _objects = new();
            readonly TestAssetService _assets = new();
            public readonly CanonicalFactory Factory;
            public readonly LocalDataProvider Data;
            public readonly AppServices App;
            public readonly RunServices Run;

            public CanonicalMagicExecutorFixture()
            {
                _assets.Register("PlayerData.xml", AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_LizzoPV/Gameplay/RunData/Data/GameData.xml"));
                Data = new LocalDataProvider(_assets);
                Assert.That(Data.InitializeAsync().GetAwaiter().GetResult().Succeeded, Is.True);
                Factory = new CanonicalFactory(_objects);
                App = new AppServices(_assets, Data);
                RuntimeObjectRegistry registry = new(Factory);
                Run = new RunServices(App, new RunState(), registry, new ObjectPoolService(new GameObject("MagicChainPool").transform), Factory);
                Run.State.Reset(1);
                Run.State.MarkLoaded();
                RetroSfx.Configure(_assets);
                RetroVfx.Configure(_assets, Factory);
                AttackVisual.Configure(Factory);
                FloatingDamageText.Configure(Factory);
                GameObject playerObject = new("MagicChainPlayer");
                _objects.Add(playerObject);
                PlayerController player = playerObject.AddComponent<PlayerController>();
                player.MaxHp = 100;
                player.Hp = 100;
                Run.Registry.RegisterPlayer(player);
            }

            public void RecruitMagicSquads()
            {
                Assert.That(Run.Party.RecruitCanonical("fire_mage"), Is.True);
                Assert.That(Run.Party.RecruitCanonical("lightning_mage"), Is.True);
                Assert.That(Run.Party.RecruitCanonical("necromancer"), Is.True);
            }

            public MonsterController CreateTarget(Vector3 position, int hp, bool boss)
            {
                GameObject instance = new("CanonicalMagicTarget");
                _objects.Add(instance);
                instance.SetActive(false);
                instance.transform.position = position;
                if (boss) instance.AddComponent<HungryGiantBehaviour>();

                instance.AddComponent<Rigidbody2D>();
                CircleCollider2D bodyCollider = instance.AddComponent<CircleCollider2D>();
                bodyCollider.isTrigger = false;
                CircleCollider2D combatCollider = instance.AddComponent<CircleCollider2D>();
                combatCollider.isTrigger = true;
                UnitColliderRefs colliderRefs = instance.AddComponent<UnitColliderRefs>();
                typeof(UnitColliderRefs).GetField("_bodyCollider", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(colliderRefs, bodyCollider);
                typeof(UnitColliderRefs).GetField("_combatCollider", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(colliderRefs, combatCollider);
                EnemyHealthBar health = instance.AddComponent<EnemyHealthBar>();
                HitFlash flash = instance.AddComponent<HitFlash>();
                instance.AddComponent<UnitVisualDriver>();
                instance.AddComponent<PatternEnemyVisual>();
                MonsterController target = instance.AddComponent<MonsterController>();
                typeof(MonsterController).GetField("_healthBar", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, health);
                typeof(MonsterController).GetField("_hitFlash", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, flash);
                instance.SetActive(true);
                target.Initialize(Run);
                target.ResetForSpawn();
                target.MaxHp = hp;
                target.Hp = hp;
                Run.Registry.RegisterEnemy(target);
                return target;
            }

            public void Dispose()
            {
                Run.Dispose();
                FloatingDamageText.ClearServices();
                AttackVisual.ClearServices();
                RetroVfx.ClearServices();
                RetroSfx.ClearServices();
                App.ReleaseAll();
                for (int index = _objects.Count - 1;
                index >= 0;
                index--)
                    if (_objects[index] != null) Object.DestroyImmediate(_objects[index]);
            }
        }

        sealed class CanonicalFactory : IPrefabFactory
        {
            readonly List<GameObject> _objects;
            readonly UnitPresentationSet _units;
            public readonly List<CombatProjectileController> Projectiles = new();

            public CanonicalFactory(List<GameObject> objects)
            {
                _objects = objects;
                _units = AssetDatabase.LoadAssetAtPath<UnitPresentationSet>("Assets/_LizzoPV/Gameplay/Presentation/Data/UnitPresentationSet.asset");
            }

            public GameObject Spawn(string address, Transform parent = null, bool pooled = false)
            {
                if (address == "ArcherProjectileVisual.prefab")
                {
                    GameObject projectile = new("MagicChainProjectile");
                    if (parent != null) projectile.transform.SetParent(parent, false);
                    _objects.Add(projectile);
                    Projectiles.Add(projectile.AddComponent<CombatProjectileController>());
                    return projectile;
                }
                if (address == "FloatingDamageText.prefab")
                {
                    GameObject text = new("FloatingDamageText");
                    if (parent != null) text.transform.SetParent(parent, false);
                    _objects.Add(text);
                    text.AddComponent<TextMeshPro>();
                    text.AddComponent<FloatingDamageText>();
                    return text;
                }
                string id = address.Substring(address.LastIndexOf('/') + 1);
                if (_units == null || _units.TryGetEntry(id, out UnitPresentationSet.Entry entry) == false)
                    return null;
                GameObject instance = Object.Instantiate(entry.Prefab, parent);
                _objects.Add(instance);
                return instance;
            }

            public GameObject Rent(GameObject prefab, string poolKey, Transform parent = null) => null;
            public void Release(GameObject instance)
            {
                _objects.Remove(instance);
                if (instance != null) Object.DestroyImmediate(instance);
            }
            public void Clear()
            {
                for (int index = _objects.Count - 1;
                index >= 0;
                index--)
                    if (_objects[index] != null) Object.DestroyImmediate(_objects[index]);
                _objects.Clear();
            }
        }

        sealed class ProjectileFactory : IPrefabFactory
        {
            readonly List<GameObject> _objects;
            public readonly List<CombatProjectileController> Projectiles = new();
            public ProjectileFactory(List<GameObject> objects) => _objects = objects;
            public GameObject Spawn(string address, Transform parent = null, bool pooled = false)
            {
                if (address != "ArcherProjectileVisual.prefab") return null;
                GameObject instance = new("MagicProjectile");
                _objects.Add(instance);
                Projectiles.Add(instance.AddComponent<CombatProjectileController>());
                return instance;
            }
            public GameObject Rent(GameObject prefab, string poolKey, Transform parent = null) => null;
            public void Release(GameObject instance) {
                if (instance != null) Object.DestroyImmediate(instance);
            }
            public void Clear() {
            }
        }

        sealed class NoProjectile : ICombatProjectileModule {
            public bool TrySpawn(in CombatProjectileRequest request) => false;
        }
        sealed class NoImmediate : ICombatImmediateHitModule {
            public bool TryApply(in CombatImmediateHitRequest request) => false;
        }
        sealed class NoField : ICombatPersistentFieldModule
        {
            public int ActiveFieldCount => 0;
            public bool TrySpawn(in CombatPersistentFieldRequest request, float currentTime) => false;
            public void Tick(float currentTime) {
            }
            public void Reset() {
            }
            public void Dispose() {
            }
        }
    }
}
