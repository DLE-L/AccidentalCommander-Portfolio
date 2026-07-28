using Lizzo.PV.Flow;
using NUnit.Framework;

namespace Lizzo.PV.EditorTests
{
    public sealed class RunLaunchStateTests
    {
        [Test]
        public void LaunchWithoutPreparedRequestDefaultsToNormal()
        {
            RunLaunchState state = new RunLaunchState();

            Assert.AreEqual(RunMode.Normal, state.ConsumeForLaunch().Mode);
        }

        [Test]
        public void PreparedTutorialRequestIsConsumedForGameplayLaunch()
        {
            RunLaunchState state = new RunLaunchState();
            state.Prepare(RunContext.Tutorial);

            Assert.AreEqual(RunMode.Tutorial, state.ConsumeForLaunch().Mode);
            Assert.AreEqual(RunMode.Normal, state.ConsumeForLaunch().Mode);
        }

        [Test]
        public void RetryPreparationPreservesCurrentRunMode()
        {
            RunLaunchState state = new RunLaunchState();
            state.Prepare(RunContext.Tutorial);
            state.ConsumeForLaunch();

            state.PrepareRetry();

            Assert.AreEqual(RunMode.Tutorial, state.ConsumeForLaunch().Mode);
        }
    }
}
