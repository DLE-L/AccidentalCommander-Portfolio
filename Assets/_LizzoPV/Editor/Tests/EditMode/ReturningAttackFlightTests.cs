using Lizzo.PV.Legion;
using NUnit.Framework;
using UnityEngine;

namespace Lizzo.PV.EditorTests
{
    public sealed class ReturningAttackFlightTests
    {
        [TestCase(.25f)]
        [TestCase(12f)]
        public void FixedDistance_UsesDirectionRatherThanTargetDistance(float targetDistance)
        {
            var flight = new ReturningAttackFlight(Vector3.zero, Vector3.up * targetDistance, 1f, fixedOutboundDistance: 4.8f);
            flight.Advance(1f, Vector3.right * 10f);
            Assert.That(flight.Position, Is.EqualTo(Vector3.up * 4.8f));
        }

        [Test]
        public void FixedDistance_RemainsRelativeToBoundLaunchPosition()
        {
            var flight = new ReturningAttackFlight(Vector3.zero, Vector3.up, 1f, fixedOutboundDistance: 4.8f);
            var launch = Vector3.left;
            typeof(ReturningAttackFlight).GetMethod("SetLaunchPosition", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(flight, new object[] { launch });
            flight.Advance(1f, launch);
            Assert.That(Vector3.Distance(flight.Position, launch), Is.EqualTo(4.8f).Within(.0001f));
            Assert.That(Vector3.Dot((flight.Position - launch).normalized, (Vector3.up - launch).normalized), Is.EqualTo(1f).Within(.0001f));
        }

        [Test]
        public void MovingOwner_ChangesSweptReturnPathWithoutExtendingDuration()
        {
            var flight = new ReturningAttackFlight(Vector3.zero, new Vector3(4, 0), 0.4f);
            flight.Advance(0.4f, Vector3.zero);
            Assert.That(flight.Position, Is.EqualTo(new Vector3(4, 0)));
            flight.BeginReturn(1.0f);
            flight.Advance(0.5f, new Vector3(0, 2));
            Assert.That(flight.PreviousPosition, Is.EqualTo(new Vector3(4, 0)));
            Assert.That(flight.Position, Is.EqualTo(new Vector3(2, 1)));
            flight.Advance(0.5f, new Vector3(0, 4));
            Assert.That(flight.PreviousPosition, Is.EqualTo(new Vector3(2, 1)));
            Assert.That(flight.Position, Is.EqualTo(new Vector3(0, 4)));
        }

        [Test]
        public void PauseAndCompletion_DoNotAdvanceOrDealAdditionalHits()
        {
            var flight = new ReturningAttackFlight(Vector3.zero, Vector3.right * 4, 1);
            flight.Advance(0.25f, Vector3.zero);
            Assert.That(flight.Position, Is.EqualTo(Vector3.right));
            flight.Advance(0, Vector3.up);
            Assert.That(flight.Position, Is.EqualTo(Vector3.right));
            flight.Complete();
            flight.Advance(1, Vector3.up);
            Assert.That(flight.Position, Is.EqualTo(Vector3.right));
            Assert.That(flight.RegisterHit(10, 3), Is.False);
        }

        [Test]
        public void TargetMayBeHitOncePerLeg_AndCapacityResetsOnReturn()
        {
            var flight = new ReturningAttackFlight(Vector3.zero, Vector3.right, 1);
            Assert.That(flight.RegisterHit(10, 1), Is.True);
            Assert.That(flight.RegisterHit(10, 1), Is.False);
            Assert.That(flight.RegisterHit(20, 1), Is.False);
            flight.BeginReturn(1);
            Assert.That(flight.RegisterHit(10, 1), Is.True);
            Assert.That(flight.RegisterHit(10, 1), Is.False);
        }

        [Test]
        public void PointBlankTarget_StillUsesConfiguredMinimumOutboundDistance()
        {
            var flight = new ReturningAttackFlight(
                Vector3.zero,
                Vector3.right * 0.25f,
                1.0f,
                minimumOutboundDistance: 2.4f);

            flight.Advance(1.0f, Vector3.zero);

            Assert.That(flight.Position, Is.EqualTo(Vector3.right * 2.4f));
        }
    }
}
