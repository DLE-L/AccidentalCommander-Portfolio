using Lizzo.PV.Gameplay.Units;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Lizzo.PV.Combat;
using Lizzo.PV.Combat.Fields;
using Lizzo.PV.Combat.Projectiles;
using Lizzo.PV.Data;
using Lizzo.PV.Legion;
using Lizzo.PV.Legion.RunCore;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using UnityEngine;

namespace Lizzo.PV.EditorTests
{
    public sealed class CompanionChainAttackTests
    {
        private const string EffectId = "dmg_chain_lightning_v1";
        private readonly List<GameObject> _objects = new List<GameObject>();
        private ICompanionCombatWorld _world;
        private RuntimeObjectRegistry _registry;
        private CombatEffectData _effect;
        private Hits _hits;

        [SetUp]
        public void SetUp()
        {
            var data = new FakeDataProvider();
            data.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
            _effect = data.GetCombatEffect(EffectId);
            _effect.Range = 2f; _effect.Radius = .5f; _effect.Angle = 60f;
            _effect.MaxTargets = 3; _effect.Push = 0f; _effect.ChainDistance = 1.6f; _effect.DamageRetentionPerTarget = .5f; _effect.StatusKind = CompanionEnemyStatusKind.None;
            _registry = new RuntimeObjectRegistry(new Factory());
            _hits = new Hits();
            Type type = typeof(CompanionRunModule).Assembly.GetType("Lizzo.PV.Legion.RunCore.CompanionRuntimeCombatWorld", true);
            _world = (ICompanionCombatWorld)Activator.CreateInstance(type, BindingFlags.Instance | BindingFlags.NonPublic,
                null, new object[] { data, _registry, new Projectiles(), _hits, new Fields(), null, null }, null);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var item in _objects) UnityEngine.Object.DestroyImmediate(item);
            _objects.Clear();
        }

        [Test]
        public void Chain_PreservesHopRangeOrderRetentionAndCapacity()
        {
            var first = Enemy("first", Vector3.right);
            var second = Enemy("second", Vector3.right * 2.5f);
            var third = Enemy("third", Vector3.right * 4f);
            Enemy("capacity", Vector3.right * 5f);
            Enemy("outside", Vector3.left * 8f);
            var result = _world.Resolve(Intent());
            Assert.That(_hits.Targets, Is.EqualTo(new[] { first, second, third }));
            Assert.That(_hits.Damage, Is.EqualTo(new[] { 12, 6, 3 }));
            Assert.That(result.AffectedTargetCount, Is.EqualTo(3));
        }

        [Test]
        public void RejectedFirstHit_PreservesIndexDamageAndSelectedCapacity()
        {
            var first = Enemy("first", Vector3.right);
            var second = Enemy("second", Vector3.right * 2.5f);
            _hits.Reject = first;
            var result = _world.Resolve(Intent());
            Assert.That(_hits.Targets, Is.EqualTo(new[] { first, second }));
            Assert.That(_hits.Damage, Is.EqualTo(new[] { 12, 6 }));
            Assert.That(result.AffectedTargetCount, Is.EqualTo(1));
        }

        [Test]
        public void RepeatAndReset_RecollectMovedDeadAndDisconnectedTargets()
        {
            var first = Enemy("first", Vector3.right);
            var second = Enemy("second", Vector3.right * 2.5f);
            _world.Resolve(Intent());
            Assert.That(_hits.Targets, Is.EqualTo(new[] { first, second }));
            first.RestoreHealth(0);
            _hits.Targets.Clear();
            _world.Resolve(Intent());
            Assert.That(_hits.Targets, Is.Empty);
            second.transform.position = Vector3.right;
            _world.GetType().GetMethod("Reset", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_world, null);
            var result = _world.Resolve(Intent());
            Assert.That(_hits.Targets, Is.EqualTo(new[] { second }));
            Assert.That(result.AffectedTargetCount, Is.EqualTo(1));
        }

        private static EffectIntent Intent() => new EffectIntent(1, "squad", "lightning_mage", EffectId,
            12, new CompanionPoint(1, 0), CombatMotion.Excursion, AttackDelivery.Chain,
            0, EffectId, 0f, 1, 0, CompanionPoint.Zero);
        private EnemyActor Enemy(string name, Vector3 position)
        {
            var item = new GameObject(name);
            _objects.Add(item);
            item.transform.position = position;
            var enemy = item.AddComponent<EnemyActor>();
            enemy.RestoreHealth(100);
            _registry.RegisterEnemy(enemy);
            return enemy;
        }

        private sealed class Hits : ICombatImmediateHitModule
        {
            internal readonly List<EnemyActor> Targets = new List<EnemyActor>();
            internal readonly List<int> Damage = new List<int>();
            internal EnemyActor Reject;
            public bool TryApply(in CombatImmediateHitRequest request)
            {
                var target = (EnemyActor)request.Target;
                Targets.Add(target);
                Damage.Add(request.Damage);
                return target != Reject;
            }
        }
        private sealed class Factory : IPrefabFactory
        {
            public GameObject Spawn(string address, Transform parent = null, bool pooled = false) => null;
            public GameObject Rent(GameObject prefab, string poolKey, Transform parent = null) => null;
            public void Release(GameObject instance) { }
            public void Clear() { }
        }
        private sealed class Projectiles : ICombatProjectileModule
        {
            public bool TrySpawn(in CombatProjectileRequest request) => false;
        }
        private sealed class Fields : ICombatPersistentFieldModule
        {
            public int ActiveFieldCount => 0;
            public bool TrySpawn(in CombatPersistentFieldRequest request, float currentTime) => false;
            public bool TryIgnite(in CombatPersistentFieldIgnitionRequest request, float currentTime, out int count) { count = 0; return false; }
            public void Tick(float currentTime) { }
            public void Reset() { }
            public void Dispose() { }
        }
    }
}
