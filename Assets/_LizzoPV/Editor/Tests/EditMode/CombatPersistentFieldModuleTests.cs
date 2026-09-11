using Lizzo.PV.Gameplay.Units;
using System.Collections.Generic;
using Lizzo.PV.Combat;
using Lizzo.PV.Combat.Fields;
using NUnit.Framework;
using UnityEngine;

namespace Lizzo.PV.EditorTests
{
    public sealed class CombatPersistentFieldModuleTests
    {
        [Test]
        public void ResetInsideSpawnObserver_DoesNotDeliverStaleCreationToLaterObservers()
        {
            var hits = new HitModule();
            var module = new CombatPersistentFieldModule(new TargetSource(new Target()), hits);
            var visible = new HashSet<long>();
            module.Changed += (change, field) => { if (change == CombatFieldChange.Created) module.Reset(); };
            module.Changed += (change, field) => { if (change == CombatFieldChange.Created) visible.Add(field.Id); else if (change == CombatFieldChange.Removed) visible.Remove(field.Id); };
            module.TrySpawn(CombatPersistentFieldRequest.CreateAllyDamage("fire", 1, Vector3.zero, 5, 1, 1, 3, 8, 2), 0);
            Assert.That(visible, Is.Empty);
            Assert.That(module.ActiveFieldCount, Is.Zero);
            Assert.That(hits.HitCount, Is.Zero);
        }

        [Test]
        public void Ignition_ExtendsEveryInRangeField_AndPresentationEndsOnReplacementExpiryOrReset()
        {
            var hits = new HitModule();
            var module = new CombatPersistentFieldModule(new TargetSource(new Target()), hits);
            var visible = new Dictionary<long, CombatFieldSnapshot>();
            int ignitions = 0;
            module.Changed += (change, field) =>
            {
                if (change == CombatFieldChange.Removed) visible.Remove(field.Id);
                else visible[field.Id] = field;
                if (change == CombatFieldChange.Ignited) ignitions++;
            };
            var request = CombatPersistentFieldRequest.CreateAllyDamage("fire_mage", 1, Vector3.zero, 5, .8f, 1, 3, 8, 3);
            for (int i = 0; i < 4; i++) module.TrySpawn(request, 0);
            Assert.That(visible.Count, Is.EqualTo(3));
            Assert.That(visible.ContainsKey(1), Is.False, "Oldest field and its presentation must be replaced together.");
            var ignition = CombatPersistentFieldIgnitionRequest.CreateAllyIgnition("sage", "ignite", "fire_mage", Vector3.zero, 4.8f, 8, 1.5f, 2);
            Assert.That(module.TryIgnite(ignition, 2, out var count), Is.True);
            Assert.That(count, Is.EqualTo(3), "The extra-field passive must also receive ignition.");
            Assert.That(ignitions, Is.EqualTo(3));
            Assert.That(hits.LastRequest.Damage, Is.EqualTo(8));
            module.Tick(3.1f);
            Assert.That(visible.Count, Is.EqualTo(3), "Visuals must survive the old unextended deadline.");
            module.Tick(4.6f);
            Assert.That(visible, Is.Empty);
            Assert.That(module.ActiveFieldCount, Is.Zero);
            module.TrySpawn(request, 5);
            module.Reset();
            Assert.That(visible, Is.Empty);
        }

        [Test]
        public void ResetDuringIgnition_RemovesPresentationWithoutRestoringAnExpiredField()
        {
            var hits = new HitModule();
            var module = new CombatPersistentFieldModule(new TargetSource(new Target()), hits);
            int visible = 0;
            module.Changed += (change, field) => { if (change == CombatFieldChange.Created) visible++; else if (change == CombatFieldChange.Removed) visible--; };
            var request = CombatPersistentFieldRequest.CreateAllyDamage("fire_mage", 1, Vector3.zero, 5, .8f, 1, 3, 8, 3);
            module.TrySpawn(request, 0);
            hits.OnHit = module.Reset;
            var ignition = CombatPersistentFieldIgnitionRequest.CreateAllyIgnition("sage", "ignite", "fire_mage", Vector3.zero, 4.8f, 8, 1.5f, 2);
            Assert.DoesNotThrow(() => module.TryIgnite(ignition, 1, out _));
            Assert.That(module.ActiveFieldCount, Is.Zero);
            Assert.That(visible, Is.Zero);
        }

        [Test]
        public void Spawn_ImmediatelyDamagesTargetsAlreadyInsideField()
        {
            var target = new Target();
            var targetSource = new TargetSource(target);
            var hitModule = new HitModule();
            var module = new CombatPersistentFieldModule(targetSource, hitModule);
            CombatPersistentFieldRequest request = CombatPersistentFieldRequest.CreateAllyDamage(
                "fire_mage",
                "dot_fire_field_v1",
                1,
                Vector3.zero,
                5,
                1.6f,
                1.0f,
                3.0f,
                8,
                2);

            Assert.That(module.TrySpawn(in request, 10.0f), Is.True);
            Assert.That(hitModule.HitCount, Is.EqualTo(1));
            Assert.That(hitModule.LastRequest.SourceId, Is.EqualTo("fire_mage"));
            Assert.That(hitModule.LastRequest.Damage, Is.EqualTo(5));

            module.Tick(10.5f);
            Assert.That(hitModule.HitCount, Is.EqualTo(1));
            module.Tick(11.0f);
            Assert.That(hitModule.HitCount, Is.EqualTo(2));
        }

        [Test]
        public void ResetDuringTick_StopsFurtherDamageWithoutWritingRemovedField()
        {
            var hits = new HitModule();
            var module = new CombatPersistentFieldModule(new TargetSource(new Target()), hits);
            var request = CombatPersistentFieldRequest.CreateAllyDamage("fire", 1, Vector3.zero, 5, 1, 1, 3, 2, 2);
            module.TrySpawn(in request, 0);
            hits.OnHit = module.Reset;
            Assert.DoesNotThrow(() => module.Tick(1));
            Assert.That(module.ActiveFieldCount, Is.Zero);
            Assert.That(hits.HitCount, Is.EqualTo(2));
            module.Tick(2);
            Assert.That(hits.HitCount, Is.EqualTo(2));
        }

        [Test]
        public void EnemyField_SelectsCommanderTicksExpiresAndRejectsAllyIgnition()
        {
            var sourceRoot = new GameObject("FieldSource");
            var playerRoot = new GameObject("FieldCommander");
            try
            {
                var source = sourceRoot.AddComponent<EnemyActor>();
                var collider = playerRoot.AddComponent<CircleCollider2D>(); collider.radius = .25f; collider.isTrigger = true;
                var player = playerRoot.AddComponent<CommanderActor>(); player.RestoreHealth(100);
                typeof(CommanderActor).GetField("_combatCollider", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(player, collider);
                var targets = new MixedTargetSource(player);
                var hits = new HitModule();
                var module = new CombatPersistentFieldModule(targets, hits);
                var request = CombatPersistentFieldRequest.CreateEnemyDamage("enemy_field", "fire", source, Vector3.zero, 5, 1, 1, 2.5f);
                Assert.That(module.TrySpawn(in request, 10), Is.True);
                Assert.That(hits.HitCount, Is.EqualTo(1));
                Assert.That(hits.LastRequest.Target, Is.SameAs(player));
                Assert.That(hits.LastRequest.Faction, Is.EqualTo(CombatImmediateHitFaction.Enemy));
                Assert.That(hits.LastRequest.Source, Is.SameAs(source));
                module.Tick(11); module.Tick(12);
                Assert.That(hits.HitCount, Is.EqualTo(3));
                var ignition = CombatPersistentFieldIgnitionRequest.CreateAllyIgnition("mage", "ignite", "enemy_field", Vector3.zero, 5, 10, 1, 2);
                Assert.That(module.TryIgnite(in ignition, 12, out int count), Is.False);
                Assert.That(count, Is.Zero);
                module.Tick(13);
                Assert.That(module.ActiveFieldCount, Is.Zero);
                Assert.That(hits.HitCount, Is.EqualTo(3));
            }
            finally { Object.DestroyImmediate(playerRoot); Object.DestroyImmediate(sourceRoot); }
        }

        [Test]
        public void EnemyField_DisabledSourceIsRemoved()
        {
            var root = new GameObject("FieldSource");
            try
            {
                var source = root.AddComponent<EnemyActor>();
                var module = new CombatPersistentFieldModule(new TargetSource(new Target()), new HitModule());
                var request = CombatPersistentFieldRequest.CreateEnemyDamage("enemy", "fire", source, Vector3.zero, 5, 1, 1, 3);
                module.TrySpawn(in request, 0);
                root.SetActive(false);
                module.Tick(.1f);
                Assert.That(module.ActiveFieldCount, Is.Zero);
            }
            finally { Object.DestroyImmediate(root); }
        }

        private sealed class MixedTargetSource : ICombatPersistentFieldTargetSource
        {
            private readonly CommanderActor _player;
            internal MixedTargetSource(CommanderActor player) { _player = player; }
            public void CollectTargets(Vector3 center, List<CombatPersistentFieldTarget> destination)
            {
                destination.Add(new CombatPersistentFieldTarget(new Target(), center, 1));
                destination.Add(new CombatPersistentFieldTarget(_player, _player.transform.position, 2));
            }
        }
        private sealed class TargetSource : ICombatPersistentFieldTargetSource
        {
            private readonly Target _target;

            internal TargetSource(Target target)
            {
                _target = target;
            }

            public void CollectTargets(Vector3 center, List<CombatPersistentFieldTarget> destination)
            {
                destination.Add(new CombatPersistentFieldTarget(_target, center, 1));
            }
        }

        private sealed class HitModule : ICombatImmediateHitModule
        {
            internal System.Action OnHit;
            internal int HitCount { get; private set; }
            internal CombatImmediateHitRequest LastRequest { get; private set; }

            public bool TryApply(in CombatImmediateHitRequest request)
            {
                HitCount++;
                LastRequest = request;
                OnHit?.Invoke();
                return true;
            }
        }

        private sealed class Target : ICombatImmediateHitTarget
        {
            public CombatImmediateHitFaction Faction => CombatImmediateHitFaction.Enemy;
            public bool IsAlive => true;
            public bool TryReceiveImmediateHit(in CombatImmediateHitRequest request) => true;
        }
    }
}
