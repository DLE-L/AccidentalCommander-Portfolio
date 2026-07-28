using Lizzo.PV.Combat;
using NUnit.Framework;
using UnityEngine;

namespace Lizzo.PV.EditorTests
{
    public sealed class SafeKnockbackWorldAndChargeSeamsTests
    {
        private readonly System.Collections.Generic.List<Object> _objects = new System.Collections.Generic.List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int index = _objects.Count - 1; index >= 0; index--)
                Object.DestroyImmediate(_objects[index]);

            _objects.Clear();
        }

        [Test]
        public void BoundaryAndEmptyMap_ClampByMoverExtentsWithoutChangingInteriorMotion()
        {
            SafeKnockbackWorld world = CreateWorld(out BoxCollider2D boundary, System.Array.Empty<Collider2D>());
            boundary.size = new Vector2(10.0f, 10.0f);
            BoxCollider2D mover = CreateBox("Mover", new Vector2(3.5f, 0.0f), new Vector2(2.0f, 2.0f), false);

            Assert.That(world.ResolveDisplacement(mover, Vector2.right * 3.0f).x, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(world.ExplicitObstacleCount, Is.EqualTo(0));

            mover.transform.position = Vector2.zero;
            Physics2D.SyncTransforms();
            Assert.That(world.ResolveDisplacement(mover, Vector2.right).x, Is.EqualTo(1.0f).Within(0.001f));
        }

        [Test]
        public void ExplicitObstacleCast_UsesSkinAndIgnoresTriggersActorsAndUnregisteredColliders()
        {
            BoxCollider2D obstacle = CreateBox("Obstacle", new Vector2(2.0f, 0.0f), Vector2.one, false);
            SafeKnockbackWorld world = CreateWorld(out BoxCollider2D boundary, new Collider2D[] { obstacle });
            boundary.size = new Vector2(20.0f, 20.0f);
            BoxCollider2D mover = CreateBox("Mover", Vector2.zero, Vector2.one, false);
            Rigidbody2D moverBody = mover.gameObject.AddComponent<Rigidbody2D>();
            moverBody.bodyType = RigidbodyType2D.Kinematic;
            moverBody.gravityScale = 0.0f;
            CreateBox("Trigger", new Vector2(1.0f, 1.0f), Vector2.one, true);
            CreateBox("Actor", new Vector2(1.0f, -1.0f), Vector2.one, false);
            Physics2D.SyncTransforms();

            float stopped = world.ResolveDisplacement(mover, Vector2.right * 5.0f).x;
            Assert.That(stopped, Is.GreaterThan(0.9f));
            Assert.That(stopped, Is.LessThan(1.5f));
        }

        [Test]
        public void NoInteriorRoom_ReturnsZeroDisplacement()
        {
            SafeKnockbackWorld world = CreateWorld(out BoxCollider2D boundary, System.Array.Empty<Collider2D>());
            boundary.size = new Vector2(2.0f, 2.0f);
            BoxCollider2D mover = CreateBox("Mover", Vector2.zero, new Vector2(2.0f, 2.0f), false);

            Assert.That(world.ResolveDisplacement(mover, Vector2.right), Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void ChargeCancellation_AlwaysCancelsAndOnlyStunsNonImmuneCharges()
        {
            ChargeCancellationResult normal = ChargeCancellationRules.Resolve(true, false, 0.8f);
            ChargeCancellationResult immune = ChargeCancellationRules.Resolve(true, true, 0.8f);

            Assert.IsTrue(normal.ChargeCancelled);
            Assert.IsTrue(normal.StunApplied);
            Assert.IsTrue(immune.ChargeCancelled);
            Assert.IsFalse(immune.StunApplied);
        }

        private SafeKnockbackWorld CreateWorld(out BoxCollider2D boundary, Collider2D[] obstacles)
        {
            GameObject boundaryObject = new GameObject("Boundary");
            _objects.Add(boundaryObject);
            boundary = boundaryObject.AddComponent<BoxCollider2D>();
            boundary.isTrigger = true;

            GameObject worldObject = new GameObject("SafeKnockbackWorld");
            _objects.Add(worldObject);
            SafeKnockbackWorld world = worldObject.AddComponent<SafeKnockbackWorld>();
            world.ConfigureForRuntime(boundary, obstacles);
            return world;
        }

        private BoxCollider2D CreateBox(string name, Vector2 position, Vector2 size, bool trigger)
        {
            GameObject gameObject = new GameObject(name);
            _objects.Add(gameObject);
            gameObject.transform.position = position;
            BoxCollider2D collider = gameObject.AddComponent<BoxCollider2D>();
            collider.size = size;
            collider.isTrigger = trigger;
            return collider;
        }
    }
}
