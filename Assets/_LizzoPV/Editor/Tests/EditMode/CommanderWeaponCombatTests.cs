using System;
using System.Collections.Generic;
using System.Reflection;
using Lizzo.PV.Combat.Projectiles;
using Lizzo.PV.Data;
using Lizzo.PV.Flow;
using Lizzo.PV.Gameplay.World;
using Lizzo.PV.Legion;
using Lizzo.PV.P0.Units;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class CommanderWeaponCombatTests
    {
        [SetUp]
        public void SetUp()
        {
            LogAssert.ignoreFailingMessages = true;
            CommanderAttack.DebugAttackEnabled = true;
            CommanderAttack.DebugResetCounters();
        }

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = false;
            Time.timeScale = 1.0f;
        }

        [Test]
        public void PiercingSpear_FiresOneStraightShot_WithFullCurrentDamageAndFourTargetCapacity()
        {
            using AttackFixture fixture = new AttackFixture(CommanderWeaponId.PiercingSpear);
            fixture.Attack.SetPassiveDamageBonus(3);
            fixture.CreateTarget(new Vector3(3.0f, 4.0f, 0.0f));

            fixture.TriggerAttackCycle();

            Assert.That(fixture.Factory.Projectiles, Has.Count.EqualTo(1));
            CombatProjectileRequest request = fixture.Factory.Projectiles[0].Request;
            Assert.That(request.DeliveryMode, Is.EqualTo(CombatProjectileDeliveryMode.StraightCollision));
            Assert.That(request.Damage, Is.EqualTo(13));
            Assert.That(request.MaxDistinctTargetHits, Is.EqualTo(4));
            Assert.That(request.Speed, Is.EqualTo(10.0f));
            Assert.That(request.Lifetime, Is.EqualTo(10.0f));
        }

        [Test]
        public void RapidCrossbow_FiresThreeLockedDirectionShots_WithSplitCurrentDamage()
        {
            using AttackFixture fixture = new AttackFixture(CommanderWeaponId.RapidCrossbow);
            fixture.Attack.SetPassiveDamageBonus(3);
            fixture.CreateTarget(new Vector3(3.0f, 4.0f, 0.0f));

            fixture.TriggerAttackCycle();

            Assert.That(fixture.Factory.Projectiles, Has.Count.EqualTo(1));
            Assert.That(fixture.Factory.Projectiles[0].Request.Damage, Is.EqualTo(5));
            Assert.That(GetFloat(fixture.Attack, "_nextBurstShotTime") - Time.time, Is.EqualTo(0.12f).Within(0.03f));

            fixture.TriggerPendingBurstShot();
            fixture.TriggerPendingBurstShot();

            Assert.That(fixture.Factory.Projectiles, Has.Count.EqualTo(3));
            for (int i = 0; i < fixture.Factory.Projectiles.Count; i++)
            {
                CombatProjectileRequest request = fixture.Factory.Projectiles[i].Request;
                Assert.That(request.Direction.normalized.x, Is.EqualTo(0.6f).Within(0.0001f));
                Assert.That(request.Direction.normalized.y, Is.EqualTo(0.8f).Within(0.0001f));
                Assert.That(request.Damage, Is.EqualTo(5));
                Assert.That(request.MaxDistinctTargetHits, Is.EqualTo(1));
                Assert.That(request.Speed, Is.EqualTo(10.0f));
                Assert.That(request.Lifetime, Is.EqualTo(10.0f));
                Assert.That(request.StraightHitFeedback, Is.EqualTo(RetroVfxKind.ProjectileHit));
            }
        }

        [Test]
        public void RapidCrossbow_ResultLockCancelsPendingShots()
        {
            using AttackFixture fixture = new AttackFixture(CommanderWeaponId.RapidCrossbow);
            fixture.CreateTarget(Vector3.right);
            fixture.TriggerAttackCycle();
            RunPauseController pause = fixture.CreatePauseController();

            pause.MarkRunEnded();
            fixture.TriggerPendingBurstShot();

            Assert.That(fixture.Factory.Projectiles, Has.Count.EqualTo(1));
            Assert.That(GetInt(fixture.Attack, "_remainingBurstShots"), Is.EqualTo(0));
        }

        [Test]
        public void RapidCrossbow_PauseDefersPendingShot()
        {
            using AttackFixture fixture = new AttackFixture(CommanderWeaponId.RapidCrossbow);
            fixture.CreateTarget(Vector3.right);
            fixture.TriggerAttackCycle();
            RunPauseController pause = fixture.CreatePauseController();

            pause.ToggleUserPause();
            fixture.Tick();

            Assert.That(Time.timeScale, Is.EqualTo(0.0f));
            Assert.That(fixture.Factory.Projectiles, Has.Count.EqualTo(1));
            Assert.That(GetInt(fixture.Attack, "_remainingBurstShots"), Is.EqualTo(2));
        }

        [TestCase(CommanderWeaponId.None)]
        [TestCase(CommanderWeaponId.PiercingSpear)]
        [TestCase(CommanderWeaponId.BlastStaff)]
        public void NonRapidCrossbowWeapons_PreserveSingleShotCycle(CommanderWeaponId weapon)
        {
            using AttackFixture fixture = new AttackFixture(weapon);
            fixture.CreateTarget(Vector3.right);

            fixture.TriggerAttackCycle();

            Assert.That(fixture.Factory.Projectiles, Has.Count.EqualTo(1));
            Assert.That(fixture.Factory.Projectiles[0].Request.Damage, Is.EqualTo(10));
            Assert.That(GetInt(fixture.Attack, "_remainingBurstShots"), Is.EqualTo(0));
        }

        static int GetInt(object target, string name) => (int)GetField(target, name).GetValue(target);
        static float GetFloat(object target, string name) => (float)GetField(target, name).GetValue(target);

        static FieldInfo GetField(object target, string name)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing test seam: {name}");
            return field;
        }

        sealed class AttackFixture : IDisposable
        {
            readonly List<GameObject> _objects = new List<GameObject>();
            readonly RunServices _services;
            readonly RuntimeObjectRegistry _registry;

            public RecordingProjectileFactory Factory { get; }
            public PlayerController Player { get; }
            public CommanderAttack Attack { get; }

            public AttackFixture(CommanderWeaponId weapon)
            {
                GameObject root = CreateObject("CommanderWeaponCombatTests");
                Transform poolRoot = CreateObject("PoolRoot").transform;
                poolRoot.SetParent(root.transform, false);
                Factory = new RecordingProjectileFactory(_objects);
                FakeDataProvider data = new FakeDataProvider();
                data.InitializeAsync().GetAwaiter().GetResult();
                _registry = new RuntimeObjectRegistry(Factory);
                _services = new RunServices(
                    new AppServices(new TestAssetService(), data),
                    new RunState(),
                    _registry,
                    new ObjectPoolService(poolRoot),
                    Factory,
                    new RunContext(RunMode.Normal, weapon));

                GameObject playerObject = CreateObject("Player");
                Player = playerObject.AddComponent<PlayerController>();
                Attack = playerObject.AddComponent<CommanderAttack>();
                Player.Initialize(_services);
                Player.Hp = 100;
                Attack.Setup(Player);
            }

            public MonsterController CreateTarget(Vector3 position)
            {
                MonsterController monster = CreateObject("Target").AddComponent<MonsterController>();
                monster.transform.position = position;
                monster.Hp = 100;
                _registry.RegisterEnemy(monster);
                return monster;
            }

            public RunPauseController CreatePauseController()
            {
                RunPauseController controller = CreateObject("Pause").AddComponent<RunPauseController>();
                controller.Initialize();
                return controller;
            }

            public void TriggerAttackCycle()
            {
                SetPrivateField(Attack, "_nextAttackTime", -1.0f);
                InvokeUpdate();
            }

            public void TriggerPendingBurstShot()
            {
                SetPrivateField(Attack, "_nextBurstShotTime", -1.0f);
                InvokeUpdate();
            }

            public void Tick() => InvokeUpdate();

            public void Dispose()
            {
                _services.Dispose();
                for (int i = _objects.Count - 1; i >= 0; i--)
                {
                    if (_objects[i] != null)
                        UnityEngine.Object.DestroyImmediate(_objects[i]);
                }
            }

            void InvokeUpdate()
            {
                typeof(CommanderAttack).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(Attack, null);
            }

            GameObject CreateObject(string name)
            {
                GameObject gameObject = new GameObject(name);
                _objects.Add(gameObject);
                return gameObject;
            }

            static void SetPrivateField(object target, string name, object value)
            {
                GetField(target, name).SetValue(target, value);
            }
        }

        sealed class RecordingProjectileFactory : IPrefabFactory
        {
            readonly List<GameObject> _objects;

            public List<CombatProjectileController> Projectiles { get; } = new List<CombatProjectileController>();

            public RecordingProjectileFactory(List<GameObject> objects)
            {
                _objects = objects;
            }

            public GameObject Spawn(string address, Transform parent = null, bool pooled = false)
            {
                GameObject projectileObject = new GameObject(address);
                projectileObject.transform.SetParent(parent, false);
                CircleCollider2D collider = projectileObject.AddComponent<CircleCollider2D>();
                collider.isTrigger = true;
                VisibilityCullProbe probe = projectileObject.AddComponent<VisibilityCullProbe>();
                CombatProjectileController projectile = projectileObject.AddComponent<CombatProjectileController>();
                SetPrivateField(projectile, "_hitCollider", collider);
                SetPrivateField(projectile, "_visibilityProbe", probe);
                Projectiles.Add(projectile);
                _objects.Add(projectileObject);
                return projectileObject;
            }

            public GameObject Rent(GameObject prefab, string poolKey, Transform parent = null) => null;

            public void Release(GameObject instance)
            {
                if (instance == null)
                    return;

                instance.SetActive(false);
            }

            public void Clear() { }

            static void SetPrivateField(object target, string name, object value)
            {
                target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
            }
        }
    }
}
