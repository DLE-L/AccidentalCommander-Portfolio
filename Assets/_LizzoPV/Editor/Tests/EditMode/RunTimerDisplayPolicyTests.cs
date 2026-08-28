using Lizzo.PV.Flow;
using Lizzo.PV.Gameplay.Route;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class RunTimerDisplayPolicyTests
    {
        [TestCase(0.0f, 180)]
        [TestCase(1.1f, 179)]
        [TestCase(180.0f, 0)]
        [TestCase(200.0f, 0)]
        public void TutorialDisplaysRemainingThreeMinuteRunTime(float elapsedSeconds, int expectedSeconds)
        {
            Assert.That(
                RunTimerDisplayPolicy.ResolveRemainingSeconds(
                    ResolveDefinition(RunMode.Tutorial),
                    elapsedSeconds),
                Is.EqualTo(expectedSeconds));
        }

        [Test]
        public void NormalDisplaysRemainingConfiguredStageTime()
        {
            Assert.That(
                RunTimerDisplayPolicy.ResolveRemainingSeconds(
                    ResolveDefinition(RunMode.Normal),
                    0.0f),
                Is.EqualTo(300));
        }

        private static RunDefinition ResolveDefinition(RunMode mode)
        {
            FakeDataProvider data = new FakeDataProvider();
            data.InitializeAsync().GetAwaiter().GetResult();
            return RunDefinitionResolver.Resolve(new RunContext(mode), data);
        }
    }
}
