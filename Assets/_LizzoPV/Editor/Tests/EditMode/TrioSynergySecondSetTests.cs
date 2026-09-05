using Lizzo.PV.Gameplay.Run;
using NUnit.Framework;

namespace Lizzo.PV.EditorTests
{
    public sealed class TrioSynergySecondSetTests
    {
        [Test]
        public void SecondSetDefinesTheRemainingFourTrioSequences()
        {
            TrioSynergyDefinitionSet set = TrioSynergyCatalog.CreateSecondSet(BuildBalance());

            Assert.That(set.Count, Is.EqualTo(4));
            Assert.That(set.Get(TrioSynergyId.UndeadMarch).GetStep(0).Kind, Is.EqualTo(TrioSynergyEffectKind.TombPath));
            Assert.That(set.Get(TrioSynergyId.AlchemyBombardment).GetStep(5).Kind, Is.EqualTo(TrioSynergyEffectKind.AlchemyFireField));
            Assert.That(set.Get(TrioSynergyId.AssaultCorps).GetStep(2).Kind, Is.EqualTo(TrioSynergyEffectKind.ShieldAfterimageCharge));
            Assert.That(set.Get(TrioSynergyId.SanctuaryGuard).GetStep(0).Kind, Is.EqualTo(TrioSynergyEffectKind.SpiritShieldBlock));
        }

        [Test]
        public void UndeadMarchRequiresThreeIndependentCounters()
        {
            TrioSynergyRuntime runtime = BuildRuntime(out _, LegionIds.WraithKnight, LegionIds.Necromancer, LegionIds.SkeletonScythe);
            Assert.That(runtime.TryQueue(TrioSynergyTrigger.ForSeparateCounters(
                11,
                TrioSynergyTriggerKind.UndeadCountersReady,
                RunPoint.Zero,
                true,
                false,
                true)), Is.False);
            Assert.That(runtime.TryQueue(TrioSynergyTrigger.ForSeparateCounters(
                12,
                TrioSynergyTriggerKind.UndeadCountersReady,
                RunPoint.Zero,
                true,
                true,
                true)), Is.True);
            Assert.That(runtime.TryStartNext(out TrioSynergyExecutionSnapshot execution), Is.True);
            Assert.That(execution.GetStep(1).Kind, Is.EqualTo(TrioSynergyEffectKind.WraithMarchDamageWeaken));
            Assert.That(execution.GetStep(2).Kind, Is.EqualTo(TrioSynergyEffectKind.GiantScytheReturn));
        }

        [Test]
        public void AlchemyBombardmentRequiresBaseVulnerabilityFireAndBombHit()
        {
            TrioSynergyRuntime runtime = BuildRuntime(out _, LegionIds.FieldHerbalist, LegionIds.Bombardier, LegionIds.FireMage);
            Assert.That(runtime.TryQueue(TrioSynergyTrigger.ForAlchemyBombHit(
                21,
                80,
                RunPoint.Zero,
                hadBaseVulnerability: true,
                insideBaseFire: false)), Is.False);
            Assert.That(runtime.TryQueue(TrioSynergyTrigger.ForAlchemyBombHit(
                22,
                80,
                RunPoint.Zero,
                hadBaseVulnerability: true,
                insideBaseFire: true)), Is.True);
            Assert.That(runtime.TryStartNext(out TrioSynergyExecutionSnapshot execution), Is.True);
            Assert.That(execution.GetStep(0).Kind, Is.EqualTo(TrioSynergyEffectKind.VolatilePotion));
            Assert.That(execution.GetStep(execution.StepCount - 1).Kind, Is.EqualTo(TrioSynergyEffectKind.AlchemyFireField));
        }

        [Test]
        public void AssaultCorpsRequiresShieldSwordLinkThenWolfBasicKill()
        {
            TrioSynergyRuntime runtime = BuildRuntime(out _, LegionIds.ShieldGuard, LegionIds.SwordSoldier, LegionIds.WolfTamer);
            Assert.That(runtime.TryQueue(TrioSynergyTrigger.ForLinkedKill(
                31,
                70,
                RunPoint.Zero,
                shieldHit: true,
                swordHit: false,
                wolfBasicKill: true)), Is.False);
            Assert.That(runtime.TryQueue(TrioSynergyTrigger.ForLinkedKill(
                32,
                70,
                RunPoint.Zero,
                shieldHit: true,
                swordHit: true,
                wolfBasicKill: true)), Is.True);
            Assert.That(runtime.TryStartNext(out TrioSynergyExecutionSnapshot execution), Is.True);
            Assert.That(execution.GetStep(2).Kind, Is.EqualTo(TrioSynergyEffectKind.ShieldAfterimageCharge));
            Assert.That(execution.GetStep(5).Kind, Is.EqualTo(TrioSynergyEffectKind.PackBiteHighestHealth));
        }

        [Test]
        public void SanctuaryGuardStoresOneChargeAndBlocksOnlyTheFirstDirectHit()
        {
            TrioSynergyRuntime runtime = BuildRuntime(out _, LegionIds.ShieldGuard, LegionIds.Cleric, LegionIds.WraithKnight);
            Assert.That(runtime.TryGrantSanctuaryCharge(), Is.True);
            Assert.That(runtime.TryGrantSanctuaryCharge(), Is.False);
            Assert.That(runtime.TryBlockWithSanctuary(
                41,
                attackerEntityId: 90,
                attackPoint: new RunPoint(1.0f, 0.0f),
                hitIndex: 0), Is.True);
            Assert.That(runtime.TryBlockWithSanctuary(
                42,
                attackerEntityId: 90,
                attackPoint: new RunPoint(1.0f, 0.0f),
                hitIndex: 1), Is.False);
            Assert.That(runtime.TryStartNext(out TrioSynergyExecutionSnapshot execution), Is.True);
            Assert.That(execution.GetStep(0).Kind, Is.EqualTo(TrioSynergyEffectKind.SpiritShieldBlock));
            Assert.That(execution.GetStep(1).Kind, Is.EqualTo(TrioSynergyEffectKind.WraithBind));
            Assert.That(execution.GetStep(2).Kind, Is.EqualTo(TrioSynergyEffectKind.HolyPillar));
        }

        private static TrioSynergyRuntime BuildRuntime(
            out SynergyRuntime scheduler,
            string first,
            string second,
            string third)
        {
            TrioSynergyDefinitionSet set = TrioSynergyCatalog.CreateSecondSet(BuildBalance());
            scheduler = new SynergyRuntime(set.CreateRuntimeDefinition(1));
            scheduler.SetLegionProgression(first, 1);
            scheduler.SetLegionProgression(second, 1);
            scheduler.SetLegionProgression(third, 1);
            return new TrioSynergyRuntime(set, scheduler);
        }

        private static TrioSynergySecondBalance BuildBalance()
        {
            return new TrioSynergySecondBalance(
                undeadMarchDamage: 10,
                undeadWeaken: 0.2f,
                undeadWeakenDuration: 2.0f,
                undeadScytheDamage: 20,
                alchemyVulnerability: 0.25f,
                alchemyVulnerabilityDuration: 2.0f,
                alchemyBombDamage: 10,
                alchemyExplosionDamage: 25,
                alchemyFieldDamage: 5,
                alchemyRadius: 2.0f,
                assaultShieldDamage: 8,
                assaultSwordDamage: 10,
                assaultBiteDamage: 25,
                assaultPathWidth: 3.0f,
                sanctuaryBindDuration: 1.0f,
                sanctuaryDamage: 20,
                sanctuaryRadius: 2.0f,
                cooldownSeconds: 3.0f);
        }
    }
}
