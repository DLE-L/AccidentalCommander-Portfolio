using Lizzo.PV.Gameplay.Visuals;
using Lizzo.PV.Gameplay.Units;
using System.Collections.Generic;
using System.Reflection;
using Lizzo.PV.Combat;
using Lizzo.PV.Legion;
using NUnit.Framework;
using UnityEngine;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class CombatAreaAttackTests
    {
        private readonly List<GameObject> _objects = new();
        private RuntimeObjectRegistry _registry;
        private CombatAreaAttack _area;
        private Hits _hits;
        [SetUp] public void SetUp() { _registry = new RuntimeObjectRegistry(new Factory()); _hits = new Hits(); _area = new CombatAreaAttack(_registry, _hits); }
        [TearDown] public void TearDown() { foreach (var item in _objects) Object.DestroyImmediate(item); _objects.Clear(); }

        [Test]
        public void AllyArea_SortsCentersAndRejectsUnselectedOrWrongFactionRequests()
        {
            var far = Enemy("far", 1.5f); var near = Enemy("near", .5f); var outside = Enemy("outside", 3f);
            var targets = _area.SelectTargets(CombatImmediateHitFaction.Ally, Vector3.zero, 2f);
            Assert.That(targets, Is.EqualTo(new ICombatImmediateHitTarget[] { near, far }));
            var hit = Hit(near);
            Assert.That(_area.TryApply(0, in hit), Is.True);
            var wrongTarget = Hit(outside);
            Assert.That(_area.TryApply(0, in wrongTarget), Is.False);
            var wrongFaction = CombatImmediateHitRequest.CreateEnemyContact("enemy", near, Vector3.zero, Vector3.right, 6, "test", RetroVfxKind.None);
            Assert.That(_area.TryApply(0, in wrongFaction), Is.False);
            Assert.That(_hits.Count, Is.EqualTo(1));
        }

        [Test]
        public void EnemyArea_UsesCommanderHurtboxAndNeverSelectsEnemy()
        {
            Enemy("enemy", 0f);
            var playerRoot = Create("commander", 1.2f);
            var collider = playerRoot.AddComponent<CircleCollider2D>(); collider.radius = .25f; collider.isTrigger = true;
            var player = playerRoot.AddComponent<CommanderActor>(); player.RestoreHealth(100);
            typeof(CommanderActor).GetField("_combatCollider", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(player, collider);
            _registry.RegisterPlayer(player);
            Physics2D.SyncTransforms();
            Assert.That(_area.SelectTargets(CombatImmediateHitFaction.Enemy, Vector3.zero, 1f), Is.EqualTo(new ICombatImmediateHitTarget[] { player }));
            var request = CombatImmediateHitRequest.CreateEnemyContact("enemy", player, Vector3.zero, Vector3.right, 6, "area", RetroVfxKind.None);
            Assert.That(_area.TryApply(0, in request), Is.True);
            playerRoot.transform.position = Vector3.right * 1.26f;
            Physics2D.SyncTransforms();
            Assert.That(_area.SelectTargets(CombatImmediateHitFaction.Enemy, Vector3.zero, 1f), Is.Empty);
        }

        [Test]
        public void SelectionSnapshot_PreservesTargetsUntilReset()
        {
            var enemy = Enemy("selected", .5f);
            _area.SelectTargets(CombatImmediateHitFaction.Ally, Vector3.zero, 1f);
            enemy.transform.position = Vector3.right * 5;
            var request = Hit(enemy);
            Assert.That(_area.TryApply(0, in request), Is.True);
            _area.Reset();
            Assert.That(_area.TryApply(0, in request), Is.False);
            Assert.That(_area.SelectTargets(CombatImmediateHitFaction.Ally, Vector3.zero, 1f), Is.Empty);
        }

        private static CombatImmediateHitRequest Hit(EnemyActor target) => CombatImmediateHitRequest.CreateAllyDirectTarget("test", target, Vector3.zero, target.transform.position, 6, AttackVisualKind.AreaHit, false);
        private GameObject Create(string name, float x) { var item = new GameObject(name); item.transform.position = Vector3.right * x; _objects.Add(item); return item; }
        private EnemyActor Enemy(string name, float x) { var enemy = Create(name, x).AddComponent<EnemyActor>(); enemy.RestoreHealth(100); _registry.RegisterEnemy(enemy); return enemy; }
        private sealed class Hits : ICombatImmediateHitModule { public int Count; public bool TryApply(in CombatImmediateHitRequest request) { Count++; return true; } }
        private sealed class Factory : IPrefabFactory
        {
            public GameObject Spawn(string address, Transform parent = null, bool pooled = false) => null;
            public GameObject Rent(GameObject prefab, string poolKey, Transform parent = null) => null;
            public void Release(GameObject instance) { }
            public void Clear() { }
        }
    }
}
