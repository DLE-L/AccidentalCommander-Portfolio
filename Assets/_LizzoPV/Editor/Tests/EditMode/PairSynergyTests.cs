using Lizzo.PV.Gameplay.Run;
using NUnit.Framework;

namespace Lizzo.PV.EditorTests
{
    public sealed class PairSynergyTests
    {
        [Test]
        public void FirstSetDefinesSixPairReactionsWithAfterimageCasters()
        {
            PairSynergyDefinitionSet set = PairSynergyCatalog.CreateFirstSet(BuildBalance());

            Assert.That(set.Count, Is.EqualTo(6));
            Assert.That(set.Get(PairSynergyId.ShieldBreakthrough).TriggerKind, Is.EqualTo(PairSynergyTriggerKind.ShieldBasicHit));
            Assert.That(set.Get(PairSynergyId.VulnerableCut).RequiresVulnerable, Is.True);
            Assert.That(set.Get(PairSynergyId.WeakeningBrew).GetStep(1).Kind, Is.EqualTo(PairSynergyEffectKind.ApplyWeaken));
            Assert.That(set.Get(PairSynergyId.SoulGuard).RequiresAppliedCommanderDamage, Is.True);
            Assert.That(set.Get(PairSynergyId.CleansingFlame).GetStep(0).Kind, Is.EqualTo(PairSynergyEffectKind.CleansingReturnTrail));
            Assert.That(set.Get(PairSynergyId.CremationRite).GetStep(2).Kind, Is.EqualTo(PairSynergyEffectKind.FireBurst));

            for (int index = 0; index < set.Count; index++)
            {
                Assert.That(set.GetAt(index).RuntimeDefinition.Tier, Is.EqualTo(SynergyTier.Pair));
                Assert.That(set.GetAt(index).RuntimeDefinition.Presentation, Is.EqualTo(SynergyCasterPresentation.Afterimage));
            }
        }

        [Test]
        public void PairReactionAcceptsOnlyMatchingBasicActionAndActiveLegions()
        {
            PairSynergyDefinitionSet set = PairSynergyCatalog.CreateFirstSet(BuildBalance());
            SynergyRuntime scheduler = new SynergyRuntime(set.CreateRuntimeDefinition(2));
            PairSynergyRuntime runtime = new PairSynergyRuntime(set, scheduler);
            scheduler.SetLegionProgression(LegionIds.SwordSoldier, 1);
            scheduler.SetLegionProgression(LegionIds.FieldHerbalist, 1);

            PairSynergyTrigger invalidSource = PairSynergyTrigger.At(
                10,
                PairSynergyTriggerKind.SwordBasicAreaHit,
                SynergyTriggerSource.Synergy,
                new RunPoint(2.0f, 1.0f),
                hasVulnerableTarget: true);
            Assert.That(runtime.TryReact(invalidSource, out _), Is.False);

            PairSynergyTrigger valid = PairSynergyTrigger.At(
                11,
                PairSynergyTriggerKind.SwordBasicAreaHit,
                SynergyTriggerSource.BasicAction,
                new RunPoint(2.0f, 1.0f),
                hasVulnerableTarget: true);
            Assert.That(runtime.TryReact(valid, out PairSynergyReactionSnapshot reaction), Is.True);
            Assert.That(reaction.SynergyId, Is.EqualTo("vulnerable-cut"));
            Assert.That(reaction.Presentation, Is.EqualTo(SynergyCasterPresentation.Afterimage));
            Assert.That(reaction.GetStep(0).DelaySeconds, Is.GreaterThan(0.0f));
        }

        [Test]
        public void ShieldBreakthroughUsesImpactPointWhenTargetIsMovementImmune()
        {
            PairSynergyDefinitionSet set = PairSynergyCatalog.CreateFirstSet(BuildBalance());
            SynergyRuntime scheduler = new SynergyRuntime(set.CreateRuntimeDefinition(1));
            PairSynergyRuntime runtime = new PairSynergyRuntime(set, scheduler);
            scheduler.SetLegionProgression(LegionIds.ShieldGuard, 1);
            scheduler.SetLegionProgression(LegionIds.SwordSoldier, 1);

            RunPoint impact = new RunPoint(4.0f, -2.0f);
            PairSynergyTrigger trigger = PairSynergyTrigger.ForShieldHit(
                21,
                impact,
                ForcedMovementOutcome.Immune);

            Assert.That(runtime.TryReact(trigger, out PairSynergyReactionSnapshot reaction), Is.True);
            Assert.That(reaction.GetStep(0).Point, Is.EqualTo(impact));
            Assert.That(reaction.GetStep(0).Kind, Is.EqualTo(PairSynergyEffectKind.CrossSlash));
        }

        [Test]
        public void SoulGuardNeedsRealDamageAndConsumesOnlyBaseWeakenBeforeHealing()
        {
            PairSynergyDefinitionSet set = PairSynergyCatalog.CreateFirstSet(BuildBalance());
            SynergyRuntime scheduler = new SynergyRuntime(set.CreateRuntimeDefinition(1));
            PairSynergyRuntime runtime = new PairSynergyRuntime(set, scheduler);
            scheduler.SetLegionProgression(LegionIds.WraithKnight, 1);
            scheduler.SetLegionProgression(LegionIds.Cleric, 1);

            PairSynergyTrigger guarded = PairSynergyTrigger.ForCommanderDamage(
                31,
                attackerEntityId: 90,
                commanderEntityId: 1,
                appliedDamage: 0,
                hadBaseWeaken: true);
            Assert.That(runtime.TryReact(guarded, out _), Is.False);

            PairSynergyTrigger damaged = PairSynergyTrigger.ForCommanderDamage(
                32,
                attackerEntityId: 90,
                commanderEntityId: 1,
                appliedDamage: 20,
                hadBaseWeaken: true);
            Assert.That(runtime.TryReact(damaged, out PairSynergyReactionSnapshot reaction), Is.True);
            Assert.That(reaction.GetStep(0).Kind, Is.EqualTo(PairSynergyEffectKind.ConsumeBaseWeaken));
            Assert.That(reaction.GetStep(1).Kind, Is.EqualTo(PairSynergyEffectKind.SoulReturnHeal));
            Assert.That(reaction.GetStep(1).Magnitude, Is.LessThan(20.0f));
        }

        [Test]
        public void CremationRiteWaitsForMovementResolutionBeforeBurst()
        {
            PairSynergyDefinitionSet set = PairSynergyCatalog.CreateFirstSet(BuildBalance());
            SynergyRuntime scheduler = new SynergyRuntime(set.CreateRuntimeDefinition(1));
            PairSynergyRuntime runtime = new PairSynergyRuntime(set, scheduler);
            scheduler.SetLegionProgression(LegionIds.FireMage, 1);
            scheduler.SetLegionProgression(LegionIds.Necromancer, 1);

            PairSynergyTrigger trigger = PairSynergyTrigger.ForDeathInBaseFire(
                41,
                deadEntityId: 95,
                deathPoint: new RunPoint(3.0f, 0.0f),
                hadBaseCurse: true);
            Assert.That(runtime.TryReact(trigger, out PairSynergyReactionSnapshot reaction), Is.True);
            Assert.That(reaction.GetStep(0).Kind, Is.EqualTo(PairSynergyEffectKind.PullToCenter));
            Assert.That(reaction.GetStep(1).Kind, Is.EqualTo(PairSynergyEffectKind.AwaitMovementResolution));
            Assert.That(reaction.GetStep(2).Kind, Is.EqualTo(PairSynergyEffectKind.FireBurst));
        }

        private static PairSynergyBalance BuildBalance()
        {
            return new PairSynergyBalance(
                crossSlashDamage: 15,
                crossSlashRadius: 1.5f,
                vulnerableCutDamage: 12,
                vulnerableCutRadius: 1.2f,
                vulnerableCutDelay: 0.2f,
                mistRadius: 2.0f,
                mistTargetLimit: 3,
                weakenMagnitude: 0.2f,
                weakenDuration: 2.0f,
                soulHealing: 5,
                cleansingDamage: 10,
                cleansingWidth: 1.0f,
                cremationPullRadius: 2.0f,
                cremationPullDistance: 1.0f,
                cremationDamage: 20,
                cremationRadius: 1.5f,
                cooldownSeconds: 1.0f);
        }
    }
}
