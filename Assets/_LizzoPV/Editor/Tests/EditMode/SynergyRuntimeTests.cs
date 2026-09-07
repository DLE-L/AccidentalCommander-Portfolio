using Lizzo.PV.Gameplay.Run;
using NUnit.Framework;

namespace Lizzo.PV.EditorTests
{
    public sealed class SynergyRuntimeTests
    {
        [Test]
        public void PairActivationUsesPromotedRepresentativeAfterimage()
        {
            SynergyRuntime runtime = BuildRuntime(1);
            runtime.SetLegionProgression("shield_guard", 1);
            Assert.That(runtime.CreateSnapshot().GetSynergy("guard_pair").IsActive, Is.False);

            runtime.SetLegionProgression("sword_soldier", 1);
            SynergyStateSnapshot active = runtime.CreateSnapshot().GetSynergy("guard_pair");
            Assert.That(active.IsActive, Is.True);
            Assert.That(active.CasterUnitId, Is.EqualTo("shield_captain"));
            Assert.That(active.Presentation, Is.EqualTo(SynergyCasterPresentation.Afterimage));
        }

        [Test]
        public void TrioActivationUsesRepresentativeCastPresentation()
        {
            SynergyRuntime runtime = BuildRuntime(1);
            runtime.SetLegionProgression("shield_guard", 1);
            runtime.SetLegionProgression("sword_soldier", 2);
            runtime.SetLegionProgression("cleric", 1);

            SynergyStateSnapshot active = runtime.CreateSnapshot().GetSynergy("guard_trio");
            Assert.That(active.IsActive, Is.True);
            Assert.That(active.CasterUnitId, Is.EqualTo("shield_captain"));
            Assert.That(active.Presentation, Is.EqualTo(SynergyCasterPresentation.Representative));
        }

        [Test]
        public void StartedExecutionKeepsItsCasterAndPresentationSnapshot()
        {
            SynergyRuntime runtime = BuildRuntime(1);
            ActivatePair(runtime);
            Assert.That(runtime.TryQueue("guard_pair", 101), Is.True);
            Assert.That(runtime.TryStartNext(out SynergyExecutionSnapshot started), Is.True);

            runtime.SetLegionProgression("shield_guard", 3);
            SynergyExecutionSnapshot current = runtime.CreateSnapshot().GetExecution(started.ExecutionId);
            Assert.That(current.CasterUnitId, Is.EqualTo("shield_captain"));
            Assert.That(current.Presentation, Is.EqualTo(SynergyCasterPresentation.Afterimage));
        }

        [Test]
        public void PairAndTrioPresentationsUseIndependentConcurrencyBudgets()
        {
            SynergyRuntime runtime = BuildRuntime(1);
            ActivatePairAndTrio(runtime);
            runtime.TryQueue("guard_pair", 1);
            runtime.TryQueue("guard_trio", 2);

            Assert.That(runtime.TryStartNext(out SynergyExecutionSnapshot pair), Is.True);
            Assert.That(runtime.TryStartNext(out SynergyExecutionSnapshot trio), Is.True);
            Assert.That(pair.Tier, Is.EqualTo(SynergyTier.Pair));
            Assert.That(trio.Tier, Is.EqualTo(SynergyTier.Trio));
            Assert.That(runtime.CreateSnapshot().ActivePairExecutionCount, Is.EqualTo(1));
            Assert.That(runtime.CreateSnapshot().ActiveTrioExecutionCount, Is.EqualTo(1));
        }

        [Test]
        public void TierSpecificStartLeavesOtherTierQueuedOnSharedScheduler()
        {
            SynergyRuntime runtime = BuildRuntime(1);
            ActivatePairAndTrio(runtime);
            runtime.TryQueue("guard_pair", 1);
            runtime.TryQueue("guard_trio", 2);

            Assert.That(runtime.TryStartNext(SynergyTier.Trio, out SynergyExecutionSnapshot trio), Is.True);
            Assert.That(trio.SynergyId, Is.EqualTo("guard_trio"));
            Assert.That(runtime.CreateSnapshot().PendingCount, Is.EqualTo(1));
            Assert.That(runtime.TryStartNext(SynergyTier.Pair, out SynergyExecutionSnapshot pair), Is.True);
            Assert.That(pair.SynergyId, Is.EqualTo("guard_pair"));
        }

        [Test]
        public void MovementFollowupWaitsForEndWhileBossImmunityUsesSeparateResolution()
        {
            SynergyRuntime runtime = BuildRuntime(2);
            ActivatePairAndTrio(runtime);
            runtime.TryQueue("guard_pair", 11);
            runtime.TryQueue("guard_trio", 12);
            runtime.TryStartNext(out SynergyExecutionSnapshot moved);
            runtime.TryStartNext(out SynergyExecutionSnapshot immune);

            Assert.That(runtime.WaitForMovementEnd(moved.ExecutionId, 501), Is.True);
            Assert.That(runtime.WaitForMovementEnd(immune.ExecutionId, 900), Is.True);
            Assert.That(runtime.CreateSnapshot().GetExecution(moved.ExecutionId).Phase, Is.EqualTo(SynergyExecutionPhase.WaitingForMovement));

            Assert.That(runtime.ResolveMovementEnded(501), Is.True);
            SynergyExecutionSnapshot movedReady = runtime.CreateSnapshot().GetExecution(moved.ExecutionId);
            Assert.That(movedReady.Phase, Is.EqualTo(SynergyExecutionPhase.FollowupReady));
            Assert.That(movedReady.ControlResolution, Is.EqualTo(SynergyControlResolution.MovementEnded));

            Assert.That(runtime.ResolveMovementImmune(immune.ExecutionId, 900), Is.True);
            SynergyExecutionSnapshot immuneReady = runtime.CreateSnapshot().GetExecution(immune.ExecutionId);
            Assert.That(immuneReady.Phase, Is.EqualTo(SynergyExecutionPhase.FollowupReady));
            Assert.That(immuneReady.ControlResolution, Is.EqualTo(SynergyControlResolution.Immune));
        }

        [Test]
        public void TriggerDeduplicationAndCooldownPreventRepeatedExecution()
        {
            SynergyRuntime runtime = BuildRuntime(1);
            ActivatePair(runtime);
            Assert.That(runtime.TryQueue("guard_pair", 77), Is.True);
            Assert.That(runtime.TryQueue("guard_pair", 77), Is.False);
            runtime.TryStartNext(out SynergyExecutionSnapshot execution);
            runtime.CompleteExecution(execution.ExecutionId);
            Assert.That(runtime.TryQueue("guard_pair", 78), Is.False);

            runtime.Advance(2.0f);
            Assert.That(runtime.TryQueue("guard_pair", 78), Is.True);
        }

        private static SynergyRuntime BuildRuntime(int maxConcurrent)
        {
            return new SynergyRuntime(BuildRuntimeDefinition(maxConcurrent));
        }

        private static SynergyRuntimeDefinition BuildRuntimeDefinition(int maxConcurrent)
        {
            return new SynergyRuntimeDefinition(
                maxConcurrent,
                new[]
                {
                    new SynergyDefinition(
                        "guard_pair",
                        SynergyTier.Pair,
                        new[] { "shield_guard", "sword_soldier" },
                        "shield_captain",
                        SynergyCasterPresentation.Afterimage,
                        2.0f),
                    new SynergyDefinition(
                        "guard_trio",
                        SynergyTier.Trio,
                        new[] { "shield_guard", "sword_soldier", "cleric" },
                        "shield_captain",
                        SynergyCasterPresentation.Representative,
                        3.0f),
                });
        }

        private static void ActivatePair(SynergyRuntime runtime)
        {
            runtime.SetLegionProgression("shield_guard", 1);
            runtime.SetLegionProgression("sword_soldier", 1);
        }

        private static void ActivatePairAndTrio(SynergyRuntime runtime)
        {
            ActivatePair(runtime);
            runtime.SetLegionProgression("cleric", 1);
        }
    }
}
