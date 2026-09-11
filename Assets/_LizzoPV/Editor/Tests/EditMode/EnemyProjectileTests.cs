using Lizzo.PV.Combat;
using Lizzo.PV.Gameplay.Visuals;
using Lizzo.PV.Gameplay.Units;
using System.Collections.Generic;
using System.Reflection;
using Lizzo.PV.Combat.Projectiles;
using Lizzo.PV.Legion;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class EnemyProjectileTests
    {
        private readonly List<GameObject> _objects = new();
        private ServiceTestFixture _services;
        private EnemyActor _source;
        private CommanderActor _player;

        [SetUp]
        public void SetUp()
        {
            // Match existing projectile fixtures: fake assets omit optional feedback resources.
            LogAssert.ignoreFailingMessages = true;
            _services = new ServiceTestFixture();
            FloatingDamageText.Configure(new ServiceTestFixture.RecordingPrefabFactory());
            var player = Create("Commander");
            player.AddComponent<SpriteRenderer>(); player.AddComponent<HitFlash>();
            var hurtbox = player.AddComponent<CircleCollider2D>(); hurtbox.isTrigger = true; hurtbox.radius = .2f;
            _player = player.AddComponent<CommanderActor>(); _services.Run.BindCommander(_player);
            typeof(CommanderActor).GetField("_combatCollider", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_player, hurtbox);
            _player.ResetHealth(100);
            _services.Run.Registry.RegisterPlayer(_player);
            _source = Create("Enemy").AddComponent<EnemyActor>(); _services.Run.BindEnemy(_source); _source.RestoreHealth(100);
        }

        [TearDown]
        public void TearDown()
        {
            _services.Dispose();
            FloatingDamageText.ClearServices();
            foreach (var item in _objects) if (item != null) Object.DestroyImmediate(item);
            _objects.Clear();
            LogAssert.ignoreFailingMessages = false;
        }

        [Test]
        public void EnemyStraight_SweepsCommanderOnceAndNeverDamagesEnemy()
        {
            var arrow = Arrow(CombatProjectileFaction.Enemy);
            Assert.That(arrow.TryHit(_source), Is.False);
            LogAssert.Expect(LogType.Error, "[FloatingDamageText] Authored prefab is not cached: FloatingDamageText.prefab");
            arrow.Advance(.5f);
            Assert.That(_player.Hp, Is.EqualTo(90));
            Assert.That(_source.Hp, Is.EqualTo(100));
            Assert.That(arrow.IsReleased, Is.True);
            arrow.Advance(.5f);
            Assert.That(_player.Hp, Is.EqualTo(90));
        }

        [Test]
        public void AllyStraight_DoesNotHitCommander()
        {
            var arrow = Arrow(CombatProjectileFaction.Ally);
            arrow.Advance(.5f);
            Assert.That(_player.Hp, Is.EqualTo(100));
            Assert.That(arrow.IsReleased, Is.False);
        }

        [Test]
        public void EnemyStraight_MissesOutsideHurtboxAndExpires()
        {
            _player.transform.position = Vector3.up;
            Physics2D.SyncTransforms();
            var arrow = Arrow(CombatProjectileFaction.Enemy);
            arrow.Advance(.5f);
            Assert.That(_player.Hp, Is.EqualTo(100));
            Assert.That(arrow.IsReleased, Is.False);
            arrow.Advance(2f);
            Assert.That(arrow.IsReleased, Is.True);
        }

        [Test]
        public void EnemyStraight_InvulnerabilityBlocksDamageButConsumesSecondArrow()
        {
            LogAssert.Expect(LogType.Error, "[FloatingDamageText] Authored prefab is not cached: FloatingDamageText.prefab");
            Arrow(CombatProjectileFaction.Enemy).Advance(.5f);
            var second = Arrow(CombatProjectileFaction.Enemy);
            second.Advance(.5f);
            Assert.That(_player.Hp, Is.EqualTo(90));
            Assert.That(second.IsReleased, Is.True);
        }

        [Test]
        public void EnemyStraight_DisabledSourceReleasesWithoutDamage()
        {
            var arrow = Arrow(CombatProjectileFaction.Enemy);
            _source.gameObject.SetActive(false);
            arrow.Advance(.5f);
            Assert.That(_player.Hp, Is.EqualTo(100));
            Assert.That(arrow.IsReleased, Is.True);
        }

        [Test]
        public void EnemyStraight_DoesNotHitBeyondLifetimeWithLargeStep()
        {
            _player.transform.position = Vector3.right * 20;
            Physics2D.SyncTransforms();
            var arrow = Arrow(CombatProjectileFaction.Enemy);
            arrow.Advance(3f);
            Assert.That(_player.Hp, Is.EqualTo(100));
            Assert.That(arrow.IsReleased, Is.True);
        }

        [Test]
        public void EnemyRequest_RejectsMissingSourceAndUnsupportedImpactArea()
        {
            var missing = CombatProjectileRequest.CreateStraight("enemy", null, Vector3.zero, Vector3.right,
                10, 8, 2, RetroVfxKind.None, CombatProjectileFaction.Enemy);
            var impact = CombatProjectileRequest.CreateStraight("enemy", _source, Vector3.zero, Vector3.right,
                10, 8, 2, RetroVfxKind.None, CombatProjectileFaction.Enemy, impactRadius: 1, impactMaxTargets: 1);
            Assert.That(missing.IsValid, Is.False);
            Assert.That(impact.IsValid, Is.False);
        }

        private GameObject Create(string name) { var item = new GameObject(name); _objects.Add(item); return item; }
        private CombatProjectileController Arrow(CombatProjectileFaction faction)
        {
            var root = Create("Arrow");
            var collider = root.AddComponent<CircleCollider2D>(); collider.isTrigger = true;
            var arrow = root.AddComponent<CombatProjectileController>();
            typeof(CombatProjectileController).GetField("_hitCollider", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(arrow, collider);
            arrow.BindRegistry(_services.Run.Registry);
            arrow.BindHitModule(_services.Run.ImmediateHitModule);
            _services.Run.Registry.RegisterProjectile(arrow);
            arrow.Initialize(CombatProjectileRequest.CreateStraight("test_enemy_arrow", _source,
                Vector3.left * 2, Vector3.right, 10, 8, 2, RetroVfxKind.None, faction,
                presentationId: "dmg_falcon_arrow_v1"));
            return arrow;
        }
    }
}
