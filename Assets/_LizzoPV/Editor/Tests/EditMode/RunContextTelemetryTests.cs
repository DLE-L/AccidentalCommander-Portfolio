using Lizzo.PV.Flow;
using Lizzo.PV.P0.Telemetry;
using NUnit.Framework;

namespace Lizzo.PV.EditorTests
{
    public sealed class RunContextTelemetryTests
    {
        [Test]
        public void NormalRunIdentifiesModeWithoutEmittingTutorialStart()
        {
            P0Telemetry.BeginRun(RunMode.Normal);

            Assert.IsFalse(P0Telemetry.TryGetEventSnapshot(P0Telemetry.TutorialStart, out _));
            Assert.IsTrue(P0Telemetry.TryGetEventSnapshot(P0Telemetry.RunStart, out P0Telemetry.EventSnapshot snapshot));
            StringAssert.Contains("run_mode=normal", snapshot.LastParametersText);
        }

        [Test]
        public void TutorialRunPreservesTutorialStartAndIdentifiesMode()
        {
            P0Telemetry.BeginRun(RunMode.Tutorial);

            Assert.IsTrue(P0Telemetry.TryGetEventSnapshot(P0Telemetry.TutorialStart, out _));
            Assert.IsTrue(P0Telemetry.TryGetEventSnapshot(P0Telemetry.RunStart, out P0Telemetry.EventSnapshot snapshot));
            StringAssert.Contains("run_mode=tutorial", snapshot.LastParametersText);
        }
    }
}
