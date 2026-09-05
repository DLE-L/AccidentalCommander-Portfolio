using System.Collections.Generic;
using Lizzo.PV.Gameplay.Run.M2;
using NUnit.Framework;
using UnityEngine;

namespace Lizzo.PV.EditorTests
{
    public sealed class M2CombatEffectsHostTests
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
        public void HostCommandsExposeCombatSnapshotAndRemainSeedDeterministic()
        {
            using RunRuntimeHost first = BuildStartedHost(33);
            using RunRuntimeHost second = BuildStartedHost(33);

            SubmitSameCombat(first);
            SubmitSameCombat(second);
            first.Advance(0.0f);
            second.Advance(0.0f);

            Assert.That(first.CurrentSnapshot.CombatEffects.GetEntity(2).Health, Is.EqualTo(85));
            Assert.That(first.CurrentSnapshot.StateDigest, Is.EqualTo(second.CurrentSnapshot.StateDigest));
        }

        [Test]
        public void SessionBlockerPausesStatusAndMovementTimeoutsUntilSimulationResumes()
        {
            using RunRuntimeHost host = BuildStartedHost(44);
            RegisterPair(host);
            host.Submit(RunCommand.ApplyCombatStatus(
                new StatusRequest(2, 1, CombatStatusKind.Curse, 1.0f, 5.0f)));
            host.Submit(RunCommand.ResolveForcedMovement(new[]
            {
                new ForcedMovementRequest(2, 1, ForcedMovementKind.Push, new RunPoint(1.0f, 0.0f), 2.0f, 1, 1.0f, 4.0f),
            }));
            host.Advance(0.0f);

            host.Submit(RunCommand.AddBlocker(SimulationBlocker.UserPause));
            host.Advance(2.0f);
            CombatEntitySnapshot paused = host.CurrentSnapshot.CombatEffects.GetEntity(1);
            Assert.That(paused.GetStatusRemaining(CombatStatusKind.Curse, 2), Is.EqualTo(5.0f));
            Assert.That(paused.IsForcedMovementLocked, Is.True);

            host.Submit(RunCommand.ClearBlocker(SimulationBlocker.UserPause));
            host.Advance(2.0f);
            CombatEntitySnapshot resumed = host.CurrentSnapshot.CombatEffects.GetEntity(1);
            Assert.That(resumed.GetStatusRemaining(CombatStatusKind.Curse, 2), Is.EqualTo(3.0f).Within(0.001f));
            Assert.That(resumed.IsForcedMovementLocked, Is.True);
        }

        [Test]
        public void StateDigestChangesWhenOnlyStatusStateDiffers()
        {
            using RunRuntimeHost withStatus = BuildStartedHost(47);
            using RunRuntimeHost withoutStatus = BuildStartedHost(47);
            RegisterPair(withStatus);
            RegisterPair(withoutStatus);
            withStatus.Advance(0.0f);
            withoutStatus.Advance(0.0f);

            withStatus.Submit(RunCommand.ApplyCombatStatus(
                new StatusRequest(2, 1, CombatStatusKind.Shock, 1.0f, 4.0f)));
            withStatus.Advance(0.0f);
            withoutStatus.Advance(0.0f);

            Assert.That(withStatus.CurrentSnapshot.StateDigest, Is.Not.EqualTo(withoutStatus.CurrentSnapshot.StateDigest));
        }

        [Test]
        public void RigidbodyBridgeUsesAuthoredMassAndDampingAndReportsCollisionEnd()
        {
            using RunRuntimeHost lightHost = BuildMovingHost(51);
            using RunRuntimeHost heavyHost = BuildMovingHost(52);
            Rigidbody2D light = CreateBody("Light", 1.0f, 3.0f, out CircleCollider2D lightCollider);
            Rigidbody2D heavy = CreateBody("Heavy", 2.0f, 7.0f, out CircleCollider2D heavyCollider);
            M2ForcedMovementUnityBridge lightBridge = CreateBridge("LightBridge");
            M2ForcedMovementUnityBridge heavyBridge = CreateBridge("HeavyBridge");
            lightBridge.Bind(lightHost, 1, light, lightCollider);
            heavyBridge.Bind(heavyHost, 1, heavy, heavyCollider);

            lightBridge.FixedStep(0.02f);
            heavyBridge.FixedStep(0.02f);

            Assert.That(light.linearVelocity.x, Is.EqualTo(10.0f).Within(0.001f));
            Assert.That(heavy.linearVelocity.x, Is.EqualTo(5.0f).Within(0.001f));
            Assert.That(light.linearDamping, Is.EqualTo(3.0f));
            Assert.That(heavy.linearDamping, Is.EqualTo(7.0f));
            Assert.That(lightHost.CurrentSnapshot.ElapsedSeconds, Is.Zero);
            Assert.That(heavyHost.CurrentSnapshot.ElapsedSeconds, Is.Zero);

            lightBridge.ReportCollision();
            CombatEntitySnapshot ended = lightHost.CurrentSnapshot.CombatEffects.GetEntity(1);
            Assert.That(ended.IsForcedMovementLocked, Is.False);
            Assert.That(ended.LastForcedMovementEndReason, Is.EqualTo(ForcedMovementEndReason.Collision));

            heavy.linearVelocity = Vector2.zero;
            heavyBridge.FixedStep(0.0f);
            CombatEntitySnapshot stopped = heavyHost.CurrentSnapshot.CombatEffects.GetEntity(1);
            Assert.That(stopped.IsForcedMovementLocked, Is.False);
            Assert.That(stopped.LastForcedMovementEndReason, Is.EqualTo(ForcedMovementEndReason.Stopped));
        }

        private static RunRuntimeHost BuildStartedHost(int seed)
        {
            RunRuntimeHost host = RunCompositionRoot.Build(new RunDefinitionSnapshot(seed));
            host.Start();
            host.Submit(RunCommand.ClearBlocker(SimulationBlocker.InitialRecruit));
            host.Advance(0.0f);
            return host;
        }

        private static RunRuntimeHost BuildMovingHost(int seed)
        {
            RunRuntimeHost host = BuildStartedHost(seed);
            RegisterPair(host);
            host.Submit(RunCommand.ResolveForcedMovement(new[]
            {
                new ForcedMovementRequest(2, 1, ForcedMovementKind.Push, new RunPoint(1.0f, 0.0f), 5.0f, 1, 10.0f, 3.0f),
            }));
            host.Advance(0.0f);
            return host;
        }

        private static void RegisterPair(RunRuntimeHost host)
        {
            host.Submit(RunCommand.RegisterCombatEntity(
                new CombatEntityDefinition(1, CombatEntityKind.NormalEnemy, 100, RunPoint.Zero)));
            host.Submit(RunCommand.RegisterCombatEntity(
                new CombatEntityDefinition(2, CombatEntityKind.Commander, 100, RunPoint.Zero)));
        }

        private static void SubmitSameCombat(RunRuntimeHost host)
        {
            RegisterPair(host);
            host.Submit(RunCommand.ApplyCombatStatus(
                new StatusRequest(1, 2, CombatStatusKind.Vulnerable, 1.5f, 3.0f)));
            host.Submit(RunCommand.ResolveCombatDamage(
                new DamageRequest(1, 2, 10.0f, CombatDamageKind.Contact)));
        }

        private Rigidbody2D CreateBody(
            string name,
            float mass,
            float damping,
            out CircleCollider2D collider)
        {
            GameObject owner = new GameObject(name);
            _objects.Add(owner);
            Rigidbody2D body = owner.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = 0.0f;
            body.mass = mass;
            body.linearDamping = damping;
            collider = owner.AddComponent<CircleCollider2D>();
            collider.isTrigger = false;
            return body;
        }

        private M2ForcedMovementUnityBridge CreateBridge(string name)
        {
            GameObject owner = new GameObject(name);
            _objects.Add(owner);
            return owner.AddComponent<M2ForcedMovementUnityBridge>();
        }
    }
}
