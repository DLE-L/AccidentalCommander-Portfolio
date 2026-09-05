using System;
using Lizzo.PV.Gameplay.Run;
using NUnit.Framework;

namespace Lizzo.PV.EditorTests
{
    public sealed class RunKernelTests
    {
        [Test]
        public void InitialRecruitAndStackedBlockersGateSimulationTime()
        {
            using RunRuntimeHost host = RunCompositionRoot.Build(new RunDefinitionSnapshot(1847));

            Assert.That(host.Start(), Is.True);
            Assert.That(host.CurrentSnapshot.ActiveBlockers, Is.EqualTo(SimulationBlocker.InitialRecruit));
            Assert.That(host.CurrentSnapshot.ElapsedSeconds, Is.Zero);

            host.Submit(RunCommand.ClearBlocker(SimulationBlocker.InitialRecruit));
            host.Advance(1.0f);
            Assert.That(host.CurrentSnapshot.ElapsedSeconds, Is.EqualTo(1.0f));

            host.Submit(RunCommand.AddBlocker(SimulationBlocker.GrowthSelection));
            host.Submit(RunCommand.AddBlocker(SimulationBlocker.BackgroundPause));
            host.Advance(1.0f);
            Assert.That(host.CurrentSnapshot.ElapsedSeconds, Is.EqualTo(1.0f));

            host.Submit(RunCommand.ClearBlocker(SimulationBlocker.GrowthSelection));
            host.Advance(1.0f);
            Assert.That(host.CurrentSnapshot.ElapsedSeconds, Is.EqualTo(1.0f));

            host.Submit(RunCommand.ClearBlocker(SimulationBlocker.BackgroundPause));
            host.Advance(0.5f);
            Assert.That(host.CurrentSnapshot.ElapsedSeconds, Is.EqualTo(1.5f));
        }

        [Test]
        public void BossAndCommanderDeathOnSameStampCommitsVictoryOnce()
        {
            using RunRuntimeHost host = RunCompositionRoot.Build(new RunDefinitionSnapshot(9));
            host.Start();
            host.Submit(RunCommand.ClearBlocker(SimulationBlocker.InitialRecruit));
            host.Advance(0.0f);

            host.Submit(RunCommand.CommanderDefeated());
            host.Submit(RunCommand.BossDefeated());
            host.Advance(0.0f);

            RunRuntimeSnapshot result = host.CurrentSnapshot;
            Assert.That(result.Outcome, Is.EqualTo(RunSessionOutcome.Victory));
            Assert.That(
                (result.ActiveBlockers & SimulationBlocker.ResultLock) != 0,
                Is.True);

            host.Submit(RunCommand.CommanderDefeated());
            host.Submit(RunCommand.ClearBlocker(SimulationBlocker.ResultLock));
            host.Advance(3.0f);

            Assert.That(host.CurrentSnapshot.Outcome, Is.EqualTo(RunSessionOutcome.Victory));
            Assert.That(host.CurrentSnapshot.ElapsedSeconds, Is.EqualTo(result.ElapsedSeconds));
            Assert.That(host.CurrentSnapshot.ResultCommitCount, Is.EqualTo(1));
        }

        [Test]
        public void SameSeedAndCommandsProduceSameStateDigest()
        {
            using RunRuntimeHost first = BuildAdvancedHost(77);
            using RunRuntimeHost second = BuildAdvancedHost(77);

            Assert.That(first.CurrentSnapshot.StateDigest, Is.EqualTo(second.CurrentSnapshot.StateDigest));
            Assert.That(first.CurrentSnapshot.ResolutionStamp, Is.EqualTo(second.CurrentSnapshot.ResolutionStamp));
            Assert.That(first.CurrentSnapshot.ElapsedSeconds, Is.EqualTo(2.25f));
        }

        [Test]
        public void HostOwnsOneLifecycleAndRejectsUseAfterDispose()
        {
            RunRuntimeHost host = RunCompositionRoot.Build(new RunDefinitionSnapshot(1));

            Assert.That(host.Start(), Is.True);
            Assert.That(host.Start(), Is.False);

            host.Dispose();
            host.Dispose();

            Assert.Throws<ObjectDisposedException>(() => host.Submit(RunCommand.Abandon()));
            Assert.Throws<ObjectDisposedException>(() => host.Advance(0.0f));
            Assert.Throws<ObjectDisposedException>(() => _ = host.CurrentSnapshot);
        }

        private static RunRuntimeHost BuildAdvancedHost(int seed)
        {
            RunRuntimeHost host = RunCompositionRoot.Build(new RunDefinitionSnapshot(seed));
            host.Start();
            host.Submit(RunCommand.ClearBlocker(SimulationBlocker.InitialRecruit));
            host.Advance(1.25f);
            host.Submit(RunCommand.AddBlocker(SimulationBlocker.UserPause));
            host.Advance(5.0f);
            host.Submit(RunCommand.ClearBlocker(SimulationBlocker.UserPause));
            host.Advance(1.0f);
            return host;
        }
    }
}
