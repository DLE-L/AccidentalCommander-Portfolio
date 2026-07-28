using System.Collections.Generic;
using Lizzo.PV.Gameplay.Commander;
using NUnit.Framework;
using UnityEngine;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class CommanderMovementMotorTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();
        private SimulationMode2D _previousSimulationMode;

        [SetUp]
        public void SetUp()
        {
            _previousSimulationMode = Physics2D.simulationMode;
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = _objects.Count - 1; i >= 0; i--)
            {
                if (_objects[i] != null)
                    Object.DestroyImmediate(_objects[i]);
            }

            _objects.Clear();
            Physics2D.simulationMode = _previousSimulationMode;
        }

        [Test]
        public void SetDirection_NormalizesNonZeroInput_AndResetClearsIt()
        {
            CommanderMovementMotor motor = CreateMotor(out _, out _, out _);

            motor.SetDirection(new Vector2(3.0f, 4.0f));
            Assert.That(motor.Direction.x, Is.EqualTo(0.6f).Within(0.0001f));
            Assert.That(motor.Direction.y, Is.EqualTo(0.8f).Within(0.0001f));

            motor.SetDirection(Vector2.zero);
            Assert.AreEqual(Vector2.zero, motor.Direction);

            motor.SetDirection(Vector2.up);
            motor.ResetForSpawn();
            Assert.AreEqual(Vector2.zero, motor.Direction);
        }

        [Test]
        public void ConfigureAndAdvance_WithRigidbody_PreservesPhysicsMotionContract()
        {
            CommanderMovementMotor motor = CreateMotor(out _, out Rigidbody2D body, out _);
            body.gravityScale = 3.0f;
            body.freezeRotation = false;
            body.simulated = false;
            body.linearVelocity = Vector2.one;
            body.angularVelocity = 8.0f;

            motor.ConfigureRigidbody();
            motor.ResetForSpawn();
            motor.SetDirection(Vector2.right);
            Physics2D.simulationMode = SimulationMode2D.Script;
            motor.Advance(5.0f, 0.2f);
            Physics2D.Simulate(0.2f);

            Assert.AreEqual(0.0f, body.gravityScale);
            Assert.IsTrue(body.freezeRotation);
            Assert.IsTrue(body.simulated);
            Assert.AreEqual(Vector2.zero, body.linearVelocity);
            Assert.AreEqual(0.0f, body.angularVelocity);
            Assert.That(body.position.x, Is.EqualTo(1.0f).Within(0.0001f));
        }

        [Test]
        public void Advance_WithoutRigidbody_UsesTransformFallback_AndRotatesIndicator()
        {
            CommanderMovementMotor motor = CreateMotor(out Transform root, out _, out Transform indicator, includeBody: false);

            motor.SetDirection(Vector2.right);
            motor.Advance(2.0f, 0.5f);

            Assert.That(root.position.x, Is.EqualTo(1.0f).Within(0.0001f));
            Assert.That(root.position.y, Is.EqualTo(0.0f).Within(0.0001f));
            Assert.That(Mathf.DeltaAngle(indicator.eulerAngles.z, -90.0f), Is.EqualTo(0.0f).Within(0.0001f));
        }

        private CommanderMovementMotor CreateMotor(
            out Transform root,
            out Rigidbody2D body,
            out Transform indicator,
            bool includeBody = true)
        {
            GameObject rootObject = new GameObject("CommanderMovementMotorTest");
            _objects.Add(rootObject);
            root = rootObject.transform;
            body = includeBody ? rootObject.AddComponent<Rigidbody2D>() : null;

            GameObject indicatorObject = new GameObject("Indicator");
            _objects.Add(indicatorObject);
            indicator = indicatorObject.transform;

            return new CommanderMovementMotor(root, body, indicator);
        }
    }
}
