using System.Collections.Generic;
using Lizzo.PV.Gameplay.Run.M2;
using NUnit.Framework;
using UnityEngine;

namespace Lizzo.PV.EditorTests
{
    public sealed class M2SwordVerticalUnityBridgeTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            for (int index = _objects.Count - 1; index >= 0; index--)
            {
                if (_objects[index] != null)
                    Object.DestroyImmediate(_objects[index]);
            }
            _objects.Clear();
        }

        [Test]
        public void RigidbodyBridgeMovesToLockedPointAndLatestSlotThenDisablesKilledEnemyCollider()
        {
            using RunRuntimeHost host = BuildStartedHost();
            Rigidbody2D commander = CreateBody("Commander", Vector2.zero, true, out CircleCollider2D commanderCollider);
            Rigidbody2D sword = CreateBody("Sword", new Vector2(-1.0f, 0.0f), false, out _);
            Rigidbody2D enemy = CreateBody("Enemy", new Vector2(1.0f, 0.0f), true, out CircleCollider2D enemyCollider);
            Transform slot = CreateTransform("SwordSlot", new Vector2(-1.0f, 0.0f));
            M2SwordVerticalUnityBridge bridge = CreateBridge();

            bridge.Bind(host, commander, commanderCollider, sword, slot);
            bridge.RegisterEnemy(101, enemy, enemyCollider, 10, 5, 10);
            bridge.FixedStep(0.0f);
            Assert.That(host.CurrentSnapshot.SwordVertical.Phase, Is.EqualTo(SwordActionPhase.Approaching));

            RunUntilPhase(bridge, host, SwordActionPhase.Returning);
            Assert.That(host.CurrentSnapshot.SwordVertical.GetEnemyHealth(101), Is.Zero);
            Assert.That(enemyCollider.enabled, Is.False);

            slot.position = new Vector3(-2.0f, 0.0f, 0.0f);
            RunUntilPhase(bridge, host, SwordActionPhase.Idle);
            Assert.That(sword.position.x, Is.EqualTo(-2.0f).Within(0.001f));
            Assert.That(sword.position.y, Is.Zero.Within(0.001f));
        }

        [Test]
        public void ColliderOverlapFeedsContactThroughTheSamePerEnemyCooldown()
        {
            using RunRuntimeHost host = BuildStartedHost();
            Rigidbody2D commander = CreateBody("Commander", Vector2.zero, true, out CircleCollider2D commanderCollider);
            Rigidbody2D sword = CreateBody("Sword", new Vector2(-1.0f, 0.0f), false, out _);
            Rigidbody2D enemy = CreateBody("Enemy", Vector2.zero, true, out CircleCollider2D enemyCollider);
            Transform slot = CreateTransform("SwordSlot", new Vector2(-1.0f, 0.0f));
            M2SwordVerticalUnityBridge bridge = CreateBridge();

            bridge.Bind(host, commander, commanderCollider, sword, slot);
            bridge.RegisterEnemy(202, enemy, enemyCollider, 1000, 20, 0);
            bridge.FixedStep(0.0f);
            bridge.FixedStep(0.0f);

            Assert.That(host.CurrentSnapshot.SwordVertical.CommanderHealth, Is.EqualTo(80));

            bridge.FixedStep(0.50f);
            Assert.That(host.CurrentSnapshot.SwordVertical.CommanderHealth, Is.EqualTo(60));
        }

        private M2SwordVerticalUnityBridge CreateBridge()
        {
            GameObject owner = new GameObject("M2SwordVerticalUnityBridge");
            _objects.Add(owner);
            return owner.AddComponent<M2SwordVerticalUnityBridge>();
        }

        private Rigidbody2D CreateBody(
            string name,
            Vector2 position,
            bool withCollider,
            out CircleCollider2D collider)
        {
            GameObject owner = new GameObject(name);
            _objects.Add(owner);
            owner.transform.position = position;
            Rigidbody2D body = owner.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0.0f;
            collider = withCollider ? owner.AddComponent<CircleCollider2D>() : null;
            if (collider != null)
                collider.radius = 0.25f;
            return body;
        }

        private Transform CreateTransform(string name, Vector2 position)
        {
            GameObject owner = new GameObject(name);
            _objects.Add(owner);
            owner.transform.position = position;
            return owner.transform;
        }

        private static RunRuntimeHost BuildStartedHost()
        {
            SwordVerticalDefinition vertical = new SwordVerticalDefinition(
                100,
                RunPoint.Zero,
                new RunPoint(-1.0f, 0.0f),
                5.0f,
                20.0f,
                0.6f,
                10,
                0.01f,
                0.50f,
                10,
                0.30f,
                3,
                5);
            RunRuntimeHost host = RunCompositionRoot.Build(new RunDefinitionSnapshot(77, vertical));
            host.Start();
            host.Submit(RunCommand.ChooseSwordGrowthCard());
            host.Advance(0.0f);
            return host;
        }

        private static void RunUntilPhase(
            M2SwordVerticalUnityBridge bridge,
            RunRuntimeHost host,
            SwordActionPhase expected)
        {
            for (int index = 0; index < 20 && host.CurrentSnapshot.SwordVertical.Phase != expected; index++)
                bridge.FixedStep(0.02f);
            Assert.That(host.CurrentSnapshot.SwordVertical.Phase, Is.EqualTo(expected));
        }
    }
}
