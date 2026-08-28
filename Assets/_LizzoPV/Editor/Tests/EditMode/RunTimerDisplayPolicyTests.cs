using Lizzo.PV.Data;
using Lizzo.PV.Flow;
using Lizzo.PV.Gameplay.Route;
using NUnit.Framework;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class RunTimerDisplayPolicyTests
    {
        readonly RunTuningData _normalTuning = new RunTuningData
        {
            StageDurationSeconds = 300.0f,
        };

        [TestCase(0.0f, 180)]
        [TestCase(1.1f, 179)]
        [TestCase(180.0f, 0)]
        [TestCase(200.0f, 0)]
        public void TutorialDisplaysRemainingThreeMinuteRunTime(float elapsedSeconds, int expectedSeconds)
        {
            Assert.That(
                RunTimerDisplayPolicy.ResolveRemainingSeconds(
                    RunContext.Tutorial,
                    _normalTuning,
                    elapsedSeconds),
                Is.EqualTo(expectedSeconds));
        }

        [Test]
        public void NormalDisplaysRemainingConfiguredStageTime()
        {
            Assert.That(
                RunTimerDisplayPolicy.ResolveRemainingSeconds(
                    RunContext.Normal,
                    _normalTuning,
                    0.0f),
                Is.EqualTo(300));
        }
    }
}
