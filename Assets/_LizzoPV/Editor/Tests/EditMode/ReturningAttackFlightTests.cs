using Lizzo.PV.Legion;
using NUnit.Framework;
using UnityEngine;

namespace Lizzo.PV.EditorTests
{
    public sealed class ReturningAttackFlightTests
    {
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
    }
}
