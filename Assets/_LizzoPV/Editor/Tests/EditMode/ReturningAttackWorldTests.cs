using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Lizzo.PV.Combat;
using Lizzo.PV.Combat.Fields;
using Lizzo.PV.Combat.Projectiles;
using Lizzo.PV.Legion;
using Lizzo.PV.Legion.RunCore;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using UnityEngine;

namespace Lizzo.PV.EditorTests
{
    public sealed class ReturningAttackWorldTests
    {
        private const string EffectId = "dmg_skeleton_scythe_throw_v1";
        private const string UnitId = "skeleton_scythe_thrower";
        private const BindingFlags Internal = BindingFlags.Instance | BindingFlags.NonPublic;
        private readonly List<GameObject> _objects = new List<GameObject>();
        private object _world;
        private RuntimeObjectRegistry _registry;
        private Hits _hits;

        [SetUp]
        public void SetUp()
        {
            var data = new FakeDataProvider();
            data.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
            _registry = new RuntimeObjectRegistry(new Factory());
            _hits = new Hits();
            Type type = typeof(CompanionRunModule).Assembly.GetType("Lizzo.PV.Legion.RunCore.CompanionRuntimeCombatWorld", true);
            _world = Activator.CreateInstance(type, Internal, null,
                new object[] { data, _registry, new Projectiles(), _hits, new Fields(), null }, null);
        }

        [TearDown]
        public void TearDown()
        {
            Call("CancelReturningFlights");
            foreach (GameObject item in _objects)
                UnityEngine.Object.DestroyImmediate(item);
            _objects.Clear();
        }

        [Test]
        public void HitsOnlyTraversedSegments_AndReturnsToTheMovedOwner()
        {
            MonsterController outbound = Enemy("outbound", new Vector3(2, 0));
            MonsterController returning = Enemy("new-return-path", new Vector3(2, 2));
            Enemy("beyond-visible-endpoint", new Vector3(5.2f, 0));
            Transform owner = ObjectAt("owner", Vector3.zero).transform;
            EffectIntent intent = Intent(1, 0);
            Call("CommitReturningFlight", intent);
            var cue = new PresentationCue(EffectId, "squad", 0, CompanionPoint.Zero,
                new CompanionPoint(4, 0), AttackDelivery.ReturningProjectile, 1);
            var flight = (ReturningAttackFlight)Call("BindReturningFlight", cue, owner);
            Assert.That(flight, Is.Not.Null);

            Call("AdvanceReturningFlights", 0.25f);
            Assert.That(_hits.Targets, Is.Empty, "No ahead-of-projectile line damage");
            Call("AdvanceReturningFlights", 0.25f);
            Assert.That(_hits.Targets, Is.EqualTo(new[] { outbound }));
            Call("AdvanceReturningFlights", 0.5f);
            EffectResolution result = ((ICompanionCombatWorld)_world).Resolve(in intent);
            Assert.That(result.AffectedTargetCount, Is.EqualTo(1));
            Assert.That(result.FollowUps.Count, Is.EqualTo(1));

            owner.position = new Vector3(0, 4);
            Call("AdvanceReturningFlights", 0.5f);
            Assert.That(flight.Position, Is.EqualTo(new Vector3(2, 2)));
            Assert.That(_hits.Targets, Is.EqualTo(new[] { outbound, returning }));
            Call("AdvanceReturningFlights", 0.5f);
            EffectIntent returnIntent = Intent(2, 1);
            result = ((ICompanionCombatWorld)_world).Resolve(in returnIntent);
            Assert.That(result.AffectedTargetCount, Is.EqualTo(1));
            Assert.That(flight.Position, Is.EqualTo(owner.position));
            Assert.That(flight.IsComplete, Is.True);
            Assert.That(_hits.Targets, Has.Count.EqualTo(2));
        }

        [Test]
        public void LethalHitCancellation_DoesNotContinueOtherFlightsOrScheduleReturn()
        {
            Enemy("lethal", new Vector3(2, 0));
            EffectIntent intent = Intent(1, 0);
            Call("CommitReturningFlight", intent);
            _hits.OnHit = () => Call("CancelReturningFlights");
            EffectResolution result = ((ICompanionCombatWorld)_world).Resolve(in intent);
            Assert.That(_hits.Targets, Has.Count.EqualTo(1));
            Assert.That(result.FollowUps, Is.Empty);
            Call("AdvanceReturningFlights", 1.0f);
            Assert.That(_hits.Targets, Has.Count.EqualTo(1));
        }

        [Test]
        public void ResultCancellation_StopsInFlightDamageAndCompletesTheViewState()
        {
            Enemy("ahead", new Vector3(2, 0));
            EffectIntent intent = Intent(1, 0);
            Call("CommitReturningFlight", intent);
            var cue = new PresentationCue(EffectId, "squad", 0, CompanionPoint.Zero,
                new CompanionPoint(4, 0), AttackDelivery.ReturningProjectile, 1);
            var flight = (ReturningAttackFlight)Call("BindReturningFlight", cue, null);
            Call("AdvanceReturningFlights", 0.1f);
            Call("CancelReturningFlights");
            Call("AdvanceReturningFlights", 1.0f);
            Assert.That(flight.IsComplete, Is.True);
            Assert.That(_hits.Targets, Is.Empty);
        }

        private object Call(string name, params object[] args) => _world.GetType().GetMethod(name, Internal).Invoke(_world, args);
        private static EffectIntent Intent(long sequence, int depth) => new EffectIntent(sequence,
            "squad", UnitId, EffectId, 15, new CompanionPoint(4, 0), CombatMotion.Stationary,
            AttackDelivery.ReturningProjectile, 0, EffectId, 1, 1, depth, CompanionPoint.Zero);
        private GameObject ObjectAt(string name, Vector3 position)
        {
            var result = new GameObject(name);
            result.transform.position = position;
            _objects.Add(result);
            return result;
        }
        private MonsterController Enemy(string name, Vector3 position)
        {
            MonsterController enemy = ObjectAt(name, position).AddComponent<MonsterController>();
            enemy.Hp = 100;
            _registry.RegisterEnemy(enemy);
            return enemy;
        }
        private sealed class Hits : ICombatImmediateHitModule
        {
            internal readonly List<MonsterController> Targets = new List<MonsterController>();
            internal Action OnHit;
            public bool TryApply(in CombatImmediateHitRequest request)
            {
                Targets.Add((MonsterController)request.Target);
                OnHit?.Invoke();
                return true;
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
