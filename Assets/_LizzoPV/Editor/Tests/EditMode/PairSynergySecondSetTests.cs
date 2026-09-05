using Lizzo.PV.Gameplay.Run;
using NUnit.Framework;

namespace Lizzo.PV.EditorTests
{
    public sealed class PairSynergySecondSetTests
    {
        [Test]
        public void SecondSetDefinesSixMorePairReactions()
        {
            PairSynergyDefinitionSet set = PairSynergyCatalog.CreateSecondSet(BuildBalance());

            Assert.That(set.Count, Is.EqualTo(6));
            Assert.That(set.Get(PairSynergyId.ThunderRite).GetStep(2).Kind, Is.EqualTo(PairSynergyEffectKind.LightningStrike));
            Assert.That(set.Get(PairSynergyId.ThunderRite).GetStep(3).Kind, Is.EqualTo(PairSynergyEffectKind.ChainLightning));
            Assert.That(set.Get(PairSynergyId.ConductiveHarvest).GetStep(0).Kind, Is.EqualTo(PairSynergyEffectKind.ElectrifiedReturnTrail));
            Assert.That(set.Get(PairSynergyId.HuntingHarvest).GetStep(0).Kind, Is.EqualTo(PairSynergyEffectKind.WolfAfterimageBite));
            Assert.That(set.Get(PairSynergyId.TrackingHunt).GetStep(2).Kind, Is.EqualTo(PairSynergyEffectKind.ResumeWolfChain));
            Assert.That(set.Get(PairSynergyId.TargetBombardment).MinimumBasicHitCount, Is.EqualTo(2));
            Assert.That(set.Get(PairSynergyId.CoverBombardment).GetStep(0).Kind, Is.EqualTo(PairSynergyEffectKind.WideDelayedBomb));
        }

        [Test]
        public void ThunderRiteNeedsBothBaseCurseAndBaseShock()
        {
            PairSynergyRuntime runtime = BuildRuntime(
                out SynergyRuntime scheduler,
                LegionIds.Necromancer,
                LegionIds.LightningMage);

            Assert.That(runtime.TryReact(PairSynergyTrigger.ForCursedShockDeath(
                101,
                90,
                RunPoint.Zero,
                hadBaseCurse: true,
                hadBaseShock: false), out _), Is.False);

            Assert.That(runtime.TryReact(PairSynergyTrigger.ForCursedShockDeath(
                102,
                90,
                RunPoint.Zero,
                hadBaseCurse: true,
                hadBaseShock: true), out PairSynergyReactionSnapshot reaction), Is.True);
            Assert.That(reaction.GetStep(0).Kind, Is.EqualTo(PairSynergyEffectKind.PullToCenter));
            Assert.That(reaction.GetStep(1).Kind, Is.EqualTo(PairSynergyEffectKind.AwaitMovementResolution));
            Assert.That(reaction.GetStep(2).Kind, Is.EqualTo(PairSynergyEffectKind.LightningStrike));
            Assert.That(reaction.GetStep(3).Kind, Is.EqualTo(PairSynergyEffectKind.ChainLightning));
            Assert.That(scheduler.CreateSnapshot().ActivePairExecutionCount, Is.EqualTo(1));
        }

        [Test]
        public void ConductiveHarvestChargesOnlyOnceForOneBasicScytheFlight()
        {
            PairSynergyRuntime runtime = BuildRuntime(
                out SynergyRuntime scheduler,
                LegionIds.LightningMage,
                LegionIds.SkeletonScythe);

            PairSynergyTrigger firstHit = PairSynergyTrigger.ForScytheOutboundShockHit(201, 80, RunPoint.Zero);
            Assert.That(runtime.TryReact(firstHit, out PairSynergyReactionSnapshot first), Is.True);
            runtime.Complete(first.ExecutionId);
            scheduler.Advance(1.0f);
            Assert.That(runtime.TryReact(firstHit, out _), Is.False);
        }

        [Test]
        public void HuntingHarvestRequiresASurvivingRoundTripCandidate()
        {
            PairSynergyRuntime runtime = BuildRuntime(
                out _,
                LegionIds.SkeletonScythe,
                LegionIds.WolfTamer);

            Assert.That(runtime.TryReact(PairSynergyTrigger.ForScytheRoundTripCandidate(
                301,
                70,
                new RunPoint(2.0f, 0.0f),
                survived: false), out _), Is.False);
            Assert.That(runtime.TryReact(PairSynergyTrigger.ForScytheRoundTripCandidate(
                302,
                70,
                new RunPoint(2.0f, 0.0f),
                survived: true), out PairSynergyReactionSnapshot reaction), Is.True);
            Assert.That(reaction.GetStep(0).TargetEntityId, Is.EqualTo(70));
        }

        [Test]
        public void TrackingHuntPausesOnlyTheFirstWolfChainTransition()
        {
            PairSynergyRuntime runtime = BuildRuntime(
                out _,
                LegionIds.WolfTamer,
                LegionIds.FalconArcher);

            Assert.That(runtime.TryReact(PairSynergyTrigger.ForWolfChainTransition(
                401,
                60,
                new RunPoint(3.0f, 0.0f),
                isFirstTransition: false), out _), Is.False);
            Assert.That(runtime.TryReact(PairSynergyTrigger.ForWolfChainTransition(
                402,
                60,
                new RunPoint(3.0f, 0.0f),
                isFirstTransition: true), out PairSynergyReactionSnapshot reaction), Is.True);
            Assert.That(reaction.GetStep(0).Kind, Is.EqualTo(PairSynergyEffectKind.DelayWolfChain));
            Assert.That(reaction.GetStep(1).Kind, Is.EqualTo(PairSynergyEffectKind.LocalArrowRain));
            Assert.That(reaction.GetStep(2).Kind, Is.EqualTo(PairSynergyEffectKind.ResumeWolfChain));
        }

        [Test]
        public void TargetBombardmentRequiresTwoBasicPiercesAndUsesFixedLastPoint()
        {
            PairSynergyRuntime runtime = BuildRuntime(
                out _,
                LegionIds.FalconArcher,
                LegionIds.Bombardier);
            RunPoint lastPoint = new RunPoint(5.0f, -1.0f);

            Assert.That(runtime.TryReact(PairSynergyTrigger.ForArrowCompleted(501, 50, lastPoint, piercedCount: 1), out _), Is.False);
            Assert.That(runtime.TryReact(PairSynergyTrigger.ForArrowCompleted(502, 50, lastPoint, piercedCount: 2), out PairSynergyReactionSnapshot reaction), Is.True);
            Assert.That(reaction.GetStep(1).Point, Is.EqualTo(lastPoint));
            Assert.That(reaction.GetStep(1).Kind, Is.EqualTo(PairSynergyEffectKind.PrecisionBomb));
        }

        [Test]
        public void OneShieldAttackCanIndependentlyFeedBothShieldPairSynergies()
        {
            PairSynergyDefinitionSet set = PairSynergyDefinitionSet.Combine(
                PairSynergyCatalog.CreateFirstSet(BuildFirstBalance()),
                PairSynergyCatalog.CreateSecondSet(BuildBalance()));
            Assert.That(set.Count, Is.EqualTo(12));
            SynergyRuntime scheduler = new SynergyRuntime(set.CreateRuntimeDefinition(2));
            PairSynergyRuntime runtime = new PairSynergyRuntime(set, scheduler);
            scheduler.SetLegionProgression(LegionIds.ShieldGuard, 1);
            scheduler.SetLegionProgression(LegionIds.SwordSoldier, 1);
            scheduler.SetLegionProgression(LegionIds.Bombardier, 1);

            Assert.That(runtime.TryReact(PairSynergyTrigger.ForShieldHit(
                601,
                new RunPoint(2.0f, 0.0f),
                ForcedMovementOutcome.Applied), out PairSynergyReactionSnapshot slash), Is.True);
            runtime.Complete(slash.ExecutionId);
            Assert.That(runtime.TryReact(PairSynergyTrigger.ForShieldReturnStarted(
                601,
                new RunPoint(2.0f, 0.0f),
                ForcedMovementOutcome.Applied), out PairSynergyReactionSnapshot bomb), Is.True);
            Assert.That(slash.SynergyId, Is.EqualTo("shield-breakthrough"));
            Assert.That(bomb.SynergyId, Is.EqualTo("cover-bombardment"));
        }

        private static PairSynergyRuntime BuildRuntime(
            out SynergyRuntime scheduler,
            string firstLegion,
            string secondLegion)
        {
            PairSynergyDefinitionSet set = PairSynergyCatalog.CreateSecondSet(BuildBalance());
            scheduler = new SynergyRuntime(set.CreateRuntimeDefinition(2));
            scheduler.SetLegionProgression(firstLegion, 1);
            scheduler.SetLegionProgression(secondLegion, 1);
            return new PairSynergyRuntime(set, scheduler);
        }

        private static PairSynergySecondBalance BuildBalance()
        {
            return new PairSynergySecondBalance(
                thunderPullRadius: 2.0f,
                thunderPullDistance: 1.0f,
                thunderDamage: 20,
                thunderChainLimit: 3,
                conductiveDamage: 8,
                conductiveHitLimit: 4,
                huntingBiteDamage: 25,
                huntingRange: 5.0f,
                arrowRainDamage: 18,
                arrowRainRadius: 1.2f,
                arrowRainDelay: 0.2f,
                precisionBombDamage: 30,
                precisionBombRadius: 1.0f,
                precisionBombDelay: 0.3f,
                coverBombDamage: 15,
                coverBombRadius: 2.0f,
                coverBombDelay: 0.4f,
                cooldownSeconds: 1.0f);
        }

        private static PairSynergyBalance BuildFirstBalance()
        {
            return new PairSynergyBalance(
                15, 1.5f, 12, 1.2f, 0.2f, 2.0f, 3, 0.2f, 2.0f,
                5, 10, 1.0f, 2.0f, 1.0f, 20, 1.5f, 1.0f);
        }
    }
}
