using System;
using System.Collections.Generic;
using System.Reflection;
using Lizzo.PV.Combat.Projectiles;
using Lizzo.PV.Data;
using Lizzo.PV.Legion;
using Lizzo.PV.Legion.Combat;
using Lizzo.PV.P0.Presentation;
using Lizzo.PV.P0.Visuals;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class CanonicalCompanionCastCompleteTests
    {
        static readonly FieldInfo ActiveProvider = typeof(PresentationCatalogProvider).GetField("_active", BindingFlags.Static | BindingFlags.NonPublic);
        PresentationCatalogProvider _previous;
        PresentationCatalog _catalog;
        GameObject _providerRoot;

        [SetUp]
        public void SetUp()
        {
            _previous = ActiveProvider.GetValue(null) as PresentationCatalogProvider;
            UnitPresentationSet units = AssetDatabase.LoadAssetAtPath<UnitPresentationSet>("Assets/_LizzoPV/Data/Presentation/UnitPresentationSet.asset");
            _catalog = ScriptableObject.CreateInstance<PresentationCatalog>();
            _catalog.SetPresentationSetsForEditor(null, null, null, null, units);
            _providerRoot = new GameObject("CastCatalog");
            _providerRoot.SetActive(false);
            PresentationCatalogProvider provider = _providerRoot.AddComponent<PresentationCatalogProvider>();
            SerializedObject so = new SerializedObject(provider);
            so.FindProperty("_catalog").objectReferenceValue = _catalog;
            so.ApplyModifiedPropertiesWithoutUndo();
            ActiveProvider.SetValue(null, provider);
        }

        [TearDown]
        public void TearDown()
        {
            if (_providerRoot != null) UnityEngine.Object.DestroyImmediate(_providerRoot);
            if (_catalog != null) UnityEngine.Object.DestroyImmediate(_catalog);
            ActiveProvider.SetValue(null, _previous);
        }

        [Test]
        public void ConfiguredPrimaryProjectile_SuccessEmitsExactlyOne_AndSpawnFailureEmitsZero()
        {
            using CanonicalCombatCastFixture fixture = new();
            AllyCombat combat = fixture.Recruit("necromancer");
            fixture.AddEnemy(new Vector3(1, 0));
            combat.TryAdvanceCanonicalCastForTests(Time.time + 10.0f);
            Assert.That(fixture.Events, Has.Count.EqualTo(1));
            fixture.Factory.FailProjectile = true;
            combat.TryAdvanceCanonicalCastForTests(Time.time + 20.0f);
            Assert.That(fixture.Events, Has.Count.EqualTo(1));
        }

        [Test]
        public void ConfiguredMeleeAndChain_MultiTargetEmitExactlyOnePerAcceptedCast()
        {
            using CanonicalCombatCastFixture fixture = new();
            AllyCombat melee = fixture.Recruit("wraith_knight");
            fixture.AddEnemy(melee.transform.position + Vector3.right * 0.8f);
            fixture.AddEnemy(melee.transform.position + new Vector3(0.8f, 0.2f, 0));
            melee.TryAdvanceCanonicalCastForTests(Time.time + 10.0f);
            Assert.That(fixture.Events, Has.Count.EqualTo(1));
            AllyCombat chain = fixture.Recruit("lightning_mage");
            chain.TryAdvanceCanonicalCastForTests(Time.time + 20.0f);
            Assert.That(fixture.Events, Has.Count.EqualTo(2));
        }

        [Test]
        public void ConfiguredPersistentFieldAndTargetArea_OriginCastEmitsOne_DelayedImpactEmitsZeroAdditional()
        {
            using CanonicalCombatCastFixture fixture = new();
            AllyCombat field = fixture.Recruit("fire_mage");
            fixture.AddEnemy(new Vector3(1, 0));
            field.TryAdvanceCanonicalCastForTests(Time.time + 10.0f);
            Assert.That(fixture.Events, Has.Count.EqualTo(1));
            AllyCombat area = fixture.Recruit("bombardier");
            area.TryAdvanceCanonicalCastForTests(Time.time + 20.0f);
            Assert.That(fixture.Events, Has.Count.EqualTo(2));
            area.TryAdvanceCanonicalCastForTests(Time.time + 21.0f);
            Assert.That(fixture.Events, Has.Count.EqualTo(2));
        }

        [Test]
        public void ConfiguredSecondaryHeal_ActualAcceptedHealEmitsOne_NoTargetOrDownOrCancelledEmitsZero()
        {
            using CanonicalCombatCastFixture fixture = new();
            AllyCombat heal = fixture.Recruit("field_herbalist");
            heal.TryAdvanceCanonicalCastForTests(Time.time + 10.0f);
            Assert.That(fixture.Events, Is.Empty);
            fixture.DamageCommander();
            heal.TryAdvanceCanonicalCastForTests(Time.time + 20.0f);
            Assert.That(fixture.Events, Has.Count.EqualTo(1));
            Assert.That(fixture.Events[0].ActionKind, Is.EqualTo(CanonicalCompanionActionKind.ActiveSkill));
            heal.SetDown(true);
            fixture.DamageCommander();
            heal.TryAdvanceCanonicalCastForTests(Time.time + 30.0f);
            Assert.That(fixture.Events, Has.Count.EqualTo(1));
        }

        [Test]
        public void StreamIdsAndRosterIdentity_AreMonotonicAndResetRestartsAtOne_WhileDisposeUnsubscribes()
        {
            CanonicalCompanionCastStream stream = new();
            List<CanonicalCompanionCastCompleted> events = new();
            stream.Completed += events.Add;
            CanonicalCompanionCastIdentity identity = new(77, "squad_02", "fire_mage", "magic_family");
            Assert.IsTrue(stream.TryEmit(identity, CanonicalCompanionActionKind.BasicAttack));
            Assert.IsTrue(stream.TryEmit(identity, CanonicalCompanionActionKind.ActiveSkill));
            Assert.That(events[0].CastId, Is.EqualTo(1));
            Assert.That(events[1].CastId, Is.EqualTo(2));
            Assert.That(events[0].RosterSlotId, Is.EqualTo("squad_02"));
            stream.Reset();
            Assert.IsTrue(stream.TryEmit(identity, CanonicalCompanionActionKind.BasicAttack));
            Assert.That(events[2].CastId, Is.EqualTo(1));
            stream.Dispose();
            Assert.IsFalse(stream.TryEmit(identity, CanonicalCompanionActionKind.BasicAttack));
        }

        sealed class CanonicalCombatCastFixture : IDisposable
        {
            readonly GameObject _root = new("CastFixture");
            public readonly TestFactory Factory = new();
            public readonly RunServices Run;
            public readonly List<CanonicalCompanionCastCompleted> Events = new();
            public readonly PlayerController Player;
            public CanonicalCombatCastFixture()
            {
                TestAssetService assets = new();
                assets.Register("PlayerData.xml", AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_LizzoPV/Data/Runtime/GameData.xml"));
                LocalDataProvider data = new(assets);
                Assert.IsTrue(data.InitializeAsync().GetAwaiter().GetResult().Succeeded);
                AppServices app = new(assets, data);
                RuntimeObjectRegistry registry = new(Factory);
                Run = new RunServices(app, new Lizzo.PV.Flow.RunState(), registry, new ObjectPoolService(new GameObject("Pool").transform), Factory);
                RetroSfx.Configure(assets);
                RetroVfx.Configure(assets, Factory);
                AttackVisual.Configure(Factory);
                FloatingDamageText.Configure(Factory);
                Player = _root.AddComponent<PlayerController>();
                Player.MaxHp = 100;
                Player.Hp = 100;
                registry.RegisterPlayer(Player);
                Run.CanonicalCompanionCasts.Completed += Events.Add;
            }
            public AllyCombat Recruit(string id) {
                Assert.IsTrue(Run.Party.RecruitCanonical(id));
                return Factory.Live[Factory.Live.Count - 1].GetComponent<AllyCombat>();
            }
            public void AddEnemy(Vector3 point)
            {
                GameObject go = new("enemy");
                go.transform.position = point;
                MonsterController monster = go.AddComponent<MonsterController>();
                EnemyHealthBar healthBar = go.AddComponent<EnemyHealthBar>();
                HitFlash hitFlash = go.AddComponent<HitFlash>();
                typeof(MonsterController).GetField("_healthBar", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(monster, healthBar);
                typeof(MonsterController).GetField("_hitFlash", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(monster, hitFlash);
                monster.MaxHp = 100;
                monster.Hp = 100;
                Run.Registry.RegisterEnemy(monster);
            }
            public void DamageCommander() {
                Player.Hp = Mathf.Max(1, Player.MaxHp - 10);
            }
            public void Dispose() {
                Run.CanonicalCompanionCasts.Completed -= Events.Add;
                Run.Dispose();
                FloatingDamageText.ClearServices();
                AttackVisual.ClearServices();
                RetroVfx.ClearServices();
                RetroSfx.ClearServices();
                UnityEngine.Object.DestroyImmediate(_root);
            }
        }

        sealed class TestFactory : IPrefabFactory
        {
            public bool FailProjectile;
            public readonly List<GameObject> Live = new();
            public GameObject Spawn(string address, Transform parent = null, bool pooled = false)
            {
                if (address == "ArcherProjectileVisual.prefab") {
                    if (FailProjectile) return null;
                    GameObject projectile = new("projectile");
                    projectile.AddComponent<CombatProjectileController>();
                    return projectile;
                }
                if (address == "FloatingDamageText.prefab") {
                    GameObject floatingText = new("FloatingDamageText");
                    floatingText.AddComponent<TextMeshPro>();
                    floatingText.AddComponent<FloatingDamageText>();
                    Live.Add(floatingText);
                    return floatingText;
                }
                UnitPresentationSet set = AssetDatabase.LoadAssetAtPath<UnitPresentationSet>("Assets/_LizzoPV/Data/Presentation/UnitPresentationSet.asset");
                string id = address.Substring(address.LastIndexOf('/') + 1);
                if (set == null || set.TryGetEntry(id, out UnitPresentationSet.Entry entry) == false) return null;
                GameObject instance = UnityEngine.Object.Instantiate(entry.Prefab, parent);
                Live.Add(instance);
                return instance;
            }
            public GameObject Rent(GameObject prefab, string poolKey, Transform parent = null) => null;
            public void Release(GameObject instance) {
                Live.Remove(instance);
                if (instance != null) UnityEngine.Object.DestroyImmediate(instance);
            }
            public void Clear() {
                for (int i = Live.Count - 1;
                i >= 0;
                i--) Release(Live[i]);
            }
        }
    }
}
