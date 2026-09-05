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
        public void ConcurrencyLimitIsDefinitionDataRatherThanHardcoded()
        {
            SynergyRuntime sequential = BuildRuntime(1);
            ActivatePairAndTrio(sequential);
            sequential.TryQueue("guard_pair", 1);
            sequential.TryQueue("guard_trio", 2);
            Assert.That(sequential.TryStartNext(out _), Is.True);
            Assert.That(sequential.TryStartNext(out _), Is.False);

            SynergyRuntime parallel = BuildRuntime(2);
            ActivatePairAndTrio(parallel);
            parallel.TryQueue("guard_pair", 1);
            parallel.TryQueue("guard_trio", 2);
            Assert.That(parallel.TryStartNext(out _), Is.True);
            Assert.That(parallel.TryStartNext(out _), Is.True);
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

        [Test]
        public void RunGrowthAutomaticallyRefreshesSynergyActivationAndDigest()
        {
            SynergyRuntimeDefinition synergies = BuildRuntimeDefinition(1);
            FormationGrowthDefinition growth = new FormationGrowthDefinition(
                1,
                2.0f,
                0.35f,
                0.25f,
                0.30f,
                new[]
                {
                    new LegionGrowthDefinition("shield_guard", "shield_captain", 1.0f),
                    new LegionGrowthDefinition("sword_soldier", "sword_captain", 1.0f),
                });
            using RunRuntimeHost host = RunCompositionRoot.Build(new RunDefinitionSnapshot(
                91,
                SwordVerticalDefinition.Disabled,
                growth,
                FrontlineLegionDefinition.Disabled,
                RangedLegionDefinition.Disabled,
                CasterLegionDefinition.Disabled,
                SummonedLegionDefinition.Disabled,
                CommonPassiveDefinition.Disabled,
                synergies));
            host.Start();
            ChooseCard(host, "shield_guard");
            ulong before = host.CurrentSnapshot.StateDigest;

            host.Submit(RunCommand.ExperienceAbsorbed(1));
            host.Advance(0.0f);
            ChooseCard(host, "sword_soldier");

            Assert.That(host.CurrentSnapshot.Synergies.GetSynergy("guard_pair").IsActive, Is.True);
            Assert.That(host.CurrentSnapshot.StateDigest, Is.Not.EqualTo(before));
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

        private static void ChooseCard(RunRuntimeHost host, string cardId)
        {
            GrowthOfferSnapshot offer = host.CurrentSnapshot.FormationGrowth.ActiveOffer;
            for (int index = 0; index < offer.Count; index++)
            {
                if (offer.GetCardId(index) != cardId)
                    continue;
                host.Submit(RunCommand.ChooseGrowthOffer(index));
                host.Advance(0.0f);
                return;
            }
            Assert.Fail($"Missing growth card: {cardId}");
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
