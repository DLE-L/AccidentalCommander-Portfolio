using Lizzo.PV.Gameplay.Run;
using NUnit.Framework;

namespace Lizzo.PV.EditorTests
{
    public sealed class RunObservabilityTests
    {
        [Test]
        public void BufferKeepsDeterministicOrderedCombatEvidenceWithoutGrowing()
        {
            RunEventBuffer buffer = new RunEventBuffer(3);
            buffer.Record(0.0f, RunEventKind.RunStarted);
            buffer.Record(1.0f, RunEventKind.EnemySpawned, "grunt", 3);
            buffer.Record(2.0f, RunEventKind.SynergyStarted, "shield-breakthrough");
            buffer.Record(3.0f, RunEventKind.ExperienceAbsorbed, value: 10);

            Assert.That(buffer.Count, Is.EqualTo(3));
            Assert.That(buffer.GetOldest(0).Kind, Is.EqualTo(RunEventKind.EnemySpawned));
            Assert.That(buffer.GetOldest(2).Value, Is.EqualTo(10));
            Assert.That(buffer.GetOldest(2).Sequence, Is.EqualTo(4));
        }
    }
}
