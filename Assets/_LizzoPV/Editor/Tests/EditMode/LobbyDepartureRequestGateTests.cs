using Lizzo.PV.Lobby;
using NUnit.Framework;

namespace Lizzo.PV.EditorTests
{
    public sealed class LobbyDepartureRequestGateTests
    {
        [Test]
        public void ReadyGateAcceptsOnlyOneStartRequest()
        {
            int requestCount = 0;
            LobbyDepartureRequestGate gate = new LobbyDepartureRequestGate(() =>
            {
                requestCount++;
                return true;
            });

            Assert.That(gate.TryStart(), Is.False);
            gate.SetReady();
            Assert.That(gate.TryStart(), Is.True);
            Assert.That(gate.TryStart(), Is.False);

            Assert.That(requestCount, Is.EqualTo(1));
            Assert.That(gate.State, Is.EqualTo(LobbyDepartureState.Starting));
        }

        [Test]
        public void RejectedStartRequestReturnsGateToReady()
        {
            LobbyDepartureRequestGate gate = new LobbyDepartureRequestGate(() => false);
            gate.SetReady();

            Assert.That(gate.TryStart(), Is.False);
            Assert.That(gate.State, Is.EqualTo(LobbyDepartureState.Ready));
        }
    }
}
