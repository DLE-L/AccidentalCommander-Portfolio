using Lizzo.PV.Gameplay.Run;
using NUnit.Framework;

namespace Lizzo.PV.EditorTests
{
    public sealed class TrioSynergyTests
    {
        [Test]
        public void FirstSetDefinesFourRepresentativeTrioSequences()
        {
            TrioSynergyDefinitionSet set = TrioSynergyCatalog.CreateFirstSet(BuildBalance());

            Assert.That(set.Count, Is.EqualTo(4));
            Assert.That(set.Get(TrioSynergyId.GuardCorps).RuntimeDefinition.TriggerPriority, Is.EqualTo(SynergyTriggerPriority.Periodic));
            Assert.That(set.Get(TrioSynergyId.RangedBarrage).RuntimeDefinition.TriggerPriority, Is.EqualTo(SynergyTriggerPriority.Cumulative));
            Assert.That(set.Get(TrioSynergyId.MagicRite).RuntimeDefinition.TriggerPriority, Is.EqualTo(SynergyTriggerPriority.ConditionReactive));
            Assert.That(set.Get(TrioSynergyId.TrackingParty).RuntimeDefinition.Presentation, Is.EqualTo(SynergyCasterPresentation.Representative));
        }

        [Test]
        public void GuardCorpsPushesAwayThenDealsLocalSwordHitsAndHeals()
        {
            TrioSynergyRuntime runtime = BuildRuntime(out _, LegionIds.ShieldGuard, LegionIds.SwordSoldier, LegionIds.Cleric);
            TrioSynergyTrigger trigger = TrioSynergyTrigger.ForPeriodicDirection(
                11,
                TrioSynergyTriggerKind.GuardPeriodReady,
                new RunPoint(0.0f, 1.0f),
                affectedTargetCount: 3);

            Assert.That(runtime.TryQueue(trigger), Is.True);
            Assert.That(runtime.TryStartNext(out TrioSynergyExecutionSnapshot execution), Is.True);
            Assert.That(execution.GetStep(0).Kind, Is.EqualTo(TrioSynergyEffectKind.DirectionalShieldWave));
            Assert.That(execution.GetStep(1).Kind, Is.EqualTo(TrioSynergyEffectKind.AwaitMovementResolution));
            Assert.That(execution.GetStep(2).Kind, Is.EqualTo(TrioSynergyEffectKind.LocalSwordHits));
            Assert.That(execution.GetStep(3).Kind, Is.EqualTo(TrioSynergyEffectKind.HolyReturnHeal));
            Assert.That(execution.GetStep(2).TargetCount, Is.EqualTo(3));
        }

        [Test]
        public void RangedBarrageRequiresEachLineagesOwnCounter()
        {
            TrioSynergyRuntime runtime = BuildRuntime(out _, LegionIds.FalconArcher, LegionIds.Bombardier, LegionIds.SkeletonScythe);

            Assert.That(runtime.TryQueue(TrioSynergyTrigger.ForSeparateCounters(
                21,
                TrioSynergyTriggerKind.RangedCountersReady,
                RunPoint.Zero,
                firstReady: true,
                secondReady: true,
                thirdReady: false)), Is.False);
            Assert.That(runtime.TryQueue(TrioSynergyTrigger.ForSeparateCounters(
                22,
                TrioSynergyTriggerKind.RangedCountersReady,
                new RunPoint(4.0f, 0.0f),
                firstReady: true,
                secondReady: true,
                thirdReady: true)), Is.True);
            Assert.That(runtime.TryStartNext(out TrioSynergyExecutionSnapshot execution), Is.True);
            Assert.That(execution.GetStep(0).Kind, Is.EqualTo(TrioSynergyEffectKind.PiercingVolley));
            Assert.That(execution.GetStep(1).Kind, Is.EqualTo(TrioSynergyEffectKind.GiantScytheRoundTrip));
            Assert.That(execution.GetStep(2).Kind, Is.EqualTo(TrioSynergyEffectKind.SequentialBombardment));
        }

        [Test]
        public void MagicRiteRequiresBaseCurseShockAndDeathInsideBaseFire()
        {
            TrioSynergyRuntime runtime = BuildRuntime(out _, LegionIds.FireMage, LegionIds.LightningMage, LegionIds.Necromancer);

            Assert.That(runtime.TryQueue(TrioSynergyTrigger.ForCompoundDeath(
                31,
                TrioSynergyTriggerKind.MagicCompoundDeath,
                RunPoint.Zero,
                hadBaseCurse: true,
                hadBaseShock: false,
                insideBaseFire: true)), Is.False);
            Assert.That(runtime.TryQueue(TrioSynergyTrigger.ForCompoundDeath(
                32,
                TrioSynergyTriggerKind.MagicCompoundDeath,
                RunPoint.Zero,
                hadBaseCurse: true,
                hadBaseShock: true,
                insideBaseFire: true)), Is.True);
            Assert.That(runtime.TryStartNext(out TrioSynergyExecutionSnapshot execution), Is.True);
            Assert.That(execution.GetStep(0).Kind, Is.EqualTo(TrioSynergyEffectKind.RitualPull));
            Assert.That(execution.GetStep(execution.StepCount - 1).Kind, Is.EqualTo(TrioSynergyEffectKind.RitualExplosion));
        }

        [Test]
        public void SchedulerStartsConditionThenCumulativeThenPeriodicAtSameTime()
        {
            SynergyRuntime runtime = new SynergyRuntime(new SynergyRuntimeDefinition(1, 1, new[]
            {
                Definition("periodic", SynergyTriggerPriority.Periodic),
                Definition("cumulative", SynergyTriggerPriority.Cumulative),
                Definition("condition", SynergyTriggerPriority.ConditionReactive),
            }));
            runtime.SetLegionProgression("a", 1);
            runtime.SetLegionProgression("b", 1);
            runtime.SetLegionProgression("c", 1);
            runtime.TryQueue("periodic", 1);
            runtime.TryQueue("cumulative", 2);
            runtime.TryQueue("condition", 3);

            Assert.That(runtime.TryStartNext(out SynergyExecutionSnapshot first), Is.True);
            Assert.That(first.SynergyId, Is.EqualTo("condition"));
            runtime.CompleteExecution(first.ExecutionId);
            Assert.That(runtime.TryStartNext(out SynergyExecutionSnapshot second), Is.True);
            Assert.That(second.SynergyId, Is.EqualTo("cumulative"));
            runtime.CompleteExecution(second.ExecutionId);
            Assert.That(runtime.TryStartNext(out SynergyExecutionSnapshot third), Is.True);
            Assert.That(third.SynergyId, Is.EqualTo("periodic"));
        }

        private static TrioSynergyRuntime BuildRuntime(
            out SynergyRuntime scheduler,
            string first,
            string second,
            string third)
        {
            TrioSynergyDefinitionSet set = TrioSynergyCatalog.CreateFirstSet(BuildBalance());
            scheduler = new SynergyRuntime(set.CreateRuntimeDefinition(1));
            scheduler.SetLegionProgression(first, 1);
            scheduler.SetLegionProgression(second, 1);
            scheduler.SetLegionProgression(third, 1);
            return new TrioSynergyRuntime(set, scheduler);
        }

        private static SynergyDefinition Definition(string id, SynergyTriggerPriority priority)
        {
            return new SynergyDefinition(
                id,
                SynergyTier.Trio,
                new[] { "a", "b", "c" },
                "promoted",
                SynergyCasterPresentation.Representative,
                0.0f,
                priority);
        }

        private static TrioSynergyBalance BuildBalance()
        {
            return new TrioSynergyBalance(
                guardWaveDamage: 10,
                guardWaveRadius: 3.0f,
                guardPushDistance: 1.0f,
                guardSwordDamage: 12,
                guardHealing: 5,
                barrageArrowDamage: 10,
                barrageScytheDamage: 15,
                barrageBombDamage: 20,
                barrageWidth: 3.0f,
                ritualPullDistance: 1.0f,
                ritualRadius: 2.0f,
                ritualFireDamage: 8,
                ritualLightningDamage: 15,
                ritualExplosionDamage: 25,
                lureVulnerability: 0.2f,
                lureDuration: 2.0f,
                huntArrowDamage: 10,
                huntBiteDamage: 25,
                huntRadius: 2.0f,
                cooldownSeconds: 3.0f);
        }
    }
}
