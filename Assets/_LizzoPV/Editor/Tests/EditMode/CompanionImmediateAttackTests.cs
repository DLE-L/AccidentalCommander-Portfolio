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
    public sealed class CompanionImmediateAttackTests
    {
        private const string EffectId = "dmg_sword_slash_v1";
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
            _effect.MaxTargets = 2; _effect.Push = 0f;
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

        [TestCase(false)]
        [TestCase(true)]
        public void ImmediateHit_PreservesShapeDamageAndTargetOrder(bool area)
        {
            var first = Enemy("first", new Vector3(1, 0));
            var second = Enemy("second", new Vector3(1.2f, .1f));
            Enemy("behind", new Vector3(-1, 0));
            Enemy("outside", new Vector3(3, 0));
            var intent = Intent(area);
            var result = _world.Resolve(in intent);
            Assert.That(_hits.Targets, Is.EqualTo(new[] { first, second }));
            Assert.That(_hits.Damage, Is.EqualTo(new[] { 12, 12 }));
            Assert.That(result.AffectedTargetCount, Is.EqualTo(2));
        }

        [Test]
        public void RejectedHit_DoesNotConsumeSuccessfulTargetLimit()
        {
            _effect.MaxTargets = 1;
            var first = Enemy("rejected", new Vector3(.8f, 0));
            var second = Enemy("accepted", new Vector3(1.2f, 0));
            _hits.Reject = first;
            var intent = Intent(false);
            var result = _world.Resolve(in intent);
            Assert.That(_hits.Targets, Is.EqualTo(new[] { first, second }));
            Assert.That(result.AffectedTargetCount, Is.EqualTo(1));
        }

        [Test]
        public void RepeatedAttackAndReset_RecollectMovedAndDeadTargets()
        {
            var first = Enemy("first", Vector3.right);
            var second = Enemy("second", Vector3.right * 3);
            var intent = Intent(false);
            _world.Resolve(in intent);
            Assert.That(_hits.Targets, Is.EqualTo(new[] { first }));
            first.RestoreHealth(0);
            second.transform.position = Vector3.right;
            _hits.Targets.Clear();
            _world.GetType().GetMethod("Reset", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_world, null);
            var result = _world.Resolve(in intent);
            Assert.That(_hits.Targets, Is.EqualTo(new[] { second }));
            Assert.That(result.AffectedTargetCount, Is.EqualTo(1));
        }

        private static EffectIntent Intent(bool area) => new EffectIntent(1, "squad", "sword_soldier", EffectId,
            12, new CompanionPoint(1, 0), CombatMotion.Excursion, area ? AttackDelivery.Area : AttackDelivery.Direct,
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
