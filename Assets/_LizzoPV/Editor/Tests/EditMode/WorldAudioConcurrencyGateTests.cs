using Lizzo.PV.Gameplay.PresentationRuntime;
using Lizzo.PV.Presentation;
using NUnit.Framework;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class WorldAudioConcurrencyGateTests
    {
        [Test]
        public void SameCue_InsideCooldown_PlaysOnlyOnce()
        {
            var gate = new WorldAudioConcurrencyGate(0.08f, 3);
            var cue = new AudioAssetId(14);

            Assert.That(gate.TryAcquire(cue, 1f, 10), Is.True);
            Assert.That(gate.TryAcquire(cue, 1.01f, 10), Is.False);
            Assert.That(gate.TryAcquire(cue, 1.081f, 11), Is.True);
        }

        [Test]
        public void DifferentCues_SameFrame_RespectStartBudget()
        {
            var gate = new WorldAudioConcurrencyGate(0.08f, 3);

            Assert.That(gate.TryAcquire(new AudioAssetId(1), 1f, 10), Is.True);
            Assert.That(gate.TryAcquire(new AudioAssetId(2), 1f, 10), Is.True);
            Assert.That(gate.TryAcquire(new AudioAssetId(3), 1f, 10), Is.True);
            Assert.That(gate.TryAcquire(new AudioAssetId(4), 1f, 10), Is.False);
            Assert.That(gate.TryAcquire(new AudioAssetId(4), 1.01f, 11), Is.True);
        }

        [Test]
        public void NoneCue_IsNeverAcquired()
        {
            var gate = new WorldAudioConcurrencyGate(0.08f, 3);

            Assert.That(gate.TryAcquire(default, 1f, 10), Is.False);
        }

        [Test]
        public void Reset_ClearsCooldownAndFrameBudget()
        {
            var gate = new WorldAudioConcurrencyGate(1f, 1);
            var cue = new AudioAssetId(14);
            Assert.That(gate.TryAcquire(cue, 1f, 10), Is.True);

            gate.Reset();

            Assert.That(gate.TryAcquire(cue, 1.01f, 10), Is.True);
        }
    }
}
