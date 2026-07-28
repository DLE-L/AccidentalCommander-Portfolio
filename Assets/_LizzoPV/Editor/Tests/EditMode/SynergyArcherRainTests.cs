using System.Collections.Generic;
using Lizzo.PV.Legion.Synergy;
using NUnit.Framework;
using UnityEngine;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class SynergyArcherRainTests
    {
        [Test]
        public void DensestSelection_UsesClusterThenAnchorDistanceThenSpawnSequence()
        {
            List<ArcherRainCandidate> candidates = new()
            {
                new ArcherRainCandidate(null, new Vector3(4.0f, 0.0f), 9),
                new ArcherRainCandidate(null, new Vector3(4.5f, 0.0f), 8),
                new ArcherRainCandidate(null, new Vector3(0.0f, 4.0f), 3),
                new ArcherRainCandidate(null, new Vector3(0.5f, 4.0f), 2),
            };
            List<ArcherRainCandidate> selected = new();

            ArcherRainRules.SelectDensest(candidates, Vector3.zero, 1.8f, selected);

            Assert.That(selected, Has.Count.EqualTo(1));
            Assert.That(selected[0].SpawnSequence, Is.EqualTo(3));
        }

        [Test]
        public void ImpactOrdering_CapsByCenterDistanceThenSpawnSequenceWithoutDuplicates()
        {
            List<ArcherRainCandidate> selected = new();
            Vector3 center = Vector3.zero;

            ArcherRainRules.InsertImpactCandidate(new ArcherRainCandidate(null, new Vector3(2.0f, 0.0f), 9), center, 3, selected);
            ArcherRainRules.InsertImpactCandidate(new ArcherRainCandidate(null, new Vector3(1.0f, 0.0f), 8), center, 3, selected);
            ArcherRainRules.InsertImpactCandidate(new ArcherRainCandidate(null, new Vector3(-1.0f, 0.0f), 3), center, 3, selected);
            ArcherRainRules.InsertImpactCandidate(new ArcherRainCandidate(null, new Vector3(3.0f, 0.0f), 1), center, 3, selected);

            Assert.That(selected, Has.Count.EqualTo(3));
            Assert.That(selected[0].SpawnSequence, Is.EqualTo(3));
            Assert.That(selected[1].SpawnSequence, Is.EqualTo(8));
            Assert.That(selected[2].SpawnSequence, Is.EqualTo(9));
        }

        [Test]
        public void DelayedCast_LocksCenterAndCancelsOnReset()
        {
            ArcherRainDelayedCastState state = new();
            Vector3 center = new(3.0f, 2.0f, 0.0f);

            state.Begin(center, 10.0f);
            Assert.That(state.IsPending, Is.True);
            Assert.That(state.TryConsumeDue(10.39f, out _), Is.False);
            Assert.That(state.TryConsumeDue(10.4f, out Vector3 lockedCenter), Is.True);
            Assert.That(lockedCenter, Is.EqualTo(center));

            state.Begin(center, 20.0f);
            state.Reset();
            Assert.That(state.TryConsumeDue(21.0f, out _), Is.False);
        }
    }
}
