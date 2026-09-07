using System.Collections.Generic;
using Lizzo.PV.Legion.Combat;
using NUnit.Framework;

namespace Lizzo.PV.EditorTests
{
    public sealed class CanonicalCompanionCastStreamTests
    {
        [Test]
        public void StreamIdsAndRosterIdentity_AreMonotonicAndResetRestartsAtOne_WhileDisposeUnsubscribes()
        {
            CanonicalCompanionCastStream stream = new CanonicalCompanionCastStream();
            List<CanonicalCompanionCastCompleted> events = new List<CanonicalCompanionCastCompleted>();
            stream.Completed += events.Add;
            CanonicalCompanionCastIdentity identity = new CanonicalCompanionCastIdentity(
                77,
                "squad_02",
                "fire_mage",
                "magic_family");

            Assert.That(stream.TryEmit(identity, CanonicalCompanionActionKind.BasicAttack), Is.True);
            Assert.That(stream.TryEmit(identity, CanonicalCompanionActionKind.ActiveSkill), Is.True);
            Assert.That(events[0].CastId, Is.EqualTo(1));
            Assert.That(events[1].CastId, Is.EqualTo(2));
            Assert.That(events[0].RosterSlotId, Is.EqualTo("squad_02"));

            stream.Reset();
            Assert.That(stream.TryEmit(identity, CanonicalCompanionActionKind.BasicAttack), Is.True);
            Assert.That(events[2].CastId, Is.EqualTo(1));

            stream.Dispose();
            Assert.That(stream.TryEmit(identity, CanonicalCompanionActionKind.BasicAttack), Is.False);
        }
    }
}
