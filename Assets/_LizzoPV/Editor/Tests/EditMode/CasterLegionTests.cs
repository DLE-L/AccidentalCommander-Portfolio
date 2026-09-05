using Lizzo.PV.Gameplay.Run;
using NUnit.Framework;

namespace Lizzo.PV.EditorTests
{
    public sealed class CasterLegionTests
    {
        [Test]
        public void HerbalistFlaskDamagesAndAppliesVulnerabilityWithoutHealingCommander()
        {
            CombatResolver resolver = BuildWorld();
            resolver.Register(Enemy(10, new RunPoint(2.0f, 0.0f), 100));
            resolver.Register(Enemy(11, new RunPoint(2.5f, 0.0f), 100));
            resolver.ApplyDamage(new DamageRequest(10, 1, 50, CombatDamageKind.Direct));
            CasterLegionRuntime runtime = BuildRuntime(resolver);
            runtime.SetProgression(CasterLineage.Herbalist, 1);
            runtime.SetSlot(CasterLineage.Herbalist, 1, new RunPoint(-1.0f, 0.0f));

            Assert.That(runtime.TryCastBase(CasterLineage.Herbalist, 1), Is.True);

            CombatEffectsSnapshot combat = resolver.CreateSnapshot();
            Assert.That(combat.GetEntity(10).Health, Is.EqualTo(90));
            Assert.That(combat.GetEntity(11).Health, Is.EqualTo(90));
            Assert.That(combat.GetEntity(10).GetStatusCount(CombatStatusKind.Vulnerable), Is.EqualTo(1));
            Assert.That(combat.GetEntity(1).Health, Is.EqualTo(150));
        }

        [Test]
        public void PromotedHerbalistSpreadsVulnerabilityFromDeathWithinDepthLimit()
        {
            CombatResolver resolver = BuildWorld();
            resolver.Register(Enemy(10, new RunPoint(2.0f, 0.0f), 20));
            resolver.Register(Enemy(11, new RunPoint(3.5f, 0.0f), 100));
            resolver.Register(Enemy(12, new RunPoint(5.0f, 0.0f), 100));
            CasterLegionRuntime runtime = BuildRuntime(resolver);
            runtime.SetProgression(CasterLineage.Herbalist, 3);
            runtime.SetSlot(CasterLineage.Herbalist, 1, RunPoint.Zero);
            runtime.TryCastBase(CasterLineage.Herbalist, 1);
            resolver.ApplyDamage(new DamageRequest(10001, 10, 10, CombatDamageKind.Direct));

            Assert.That(runtime.TrySpreadVulnerability(10, reactionDepth: 0), Is.True);
            Assert.That(resolver.CreateSnapshot().GetEntity(11).GetStatusCount(CombatStatusKind.Vulnerable), Is.EqualTo(1));
            Assert.That(runtime.TrySpreadVulnerability(10, reactionDepth: 1), Is.False);
            Assert.That(resolver.CreateSnapshot().GetEntity(12).GetStatusCount(CombatStatusKind.Vulnerable), Is.EqualTo(0));
        }

        [Test]
        public void FireFieldsTickIndependentlyAndReplaceOldestAtCapacity()
        {
            CombatResolver resolver = BuildWorld();
            resolver.Register(Enemy(10, new RunPoint(2.0f, 0.0f), 200));
            resolver.Register(Enemy(11, new RunPoint(5.0f, 0.0f), 200));
            resolver.Register(Enemy(12, new RunPoint(7.0f, 0.0f), 200));
            CasterLegionRuntime runtime = BuildRuntime(resolver);
            runtime.SetProgression(CasterLineage.Fire, 2);
            runtime.SetSlot(CasterLineage.Fire, 1, RunPoint.Zero);
            runtime.SetSlot(CasterLineage.Fire, 2, RunPoint.Zero);

            runtime.TryCastBase(CasterLineage.Fire, 1);
            resolver.SetPosition(10, new RunPoint(9.0f, 0.0f));
            runtime.TryCastBase(CasterLineage.Fire, 2);
            resolver.SetPosition(11, new RunPoint(9.0f, 0.0f));
            runtime.TryCastBase(CasterLineage.Fire, 1);
            resolver.SetPosition(10, new RunPoint(2.0f, 0.0f));
            resolver.SetPosition(11, new RunPoint(5.0f, 0.0f));
            runtime.Advance(1.0f);

            Assert.That(runtime.CreateSnapshot().ActiveFieldCount, Is.EqualTo(2));
            Assert.That(resolver.CreateSnapshot().GetEntity(10).Health, Is.EqualTo(200));
            Assert.That(resolver.CreateSnapshot().GetEntity(11).Health, Is.EqualTo(195));
            Assert.That(resolver.CreateSnapshot().GetEntity(12).Health, Is.EqualTo(195));
        }

        [Test]
        public void FireSageIgnitionWaitsUntilAFieldExistsThenDamagesAndExtendsIt()
        {
            CombatResolver resolver = BuildWorld();
            resolver.Register(Enemy(10, new RunPoint(2.0f, 0.0f), 200));
            CasterLegionRuntime runtime = BuildRuntime(resolver);
            runtime.SetProgression(CasterLineage.Fire, 3);
            runtime.SetSlot(CasterLineage.Fire, 1, RunPoint.Zero);

            Assert.That(runtime.TryIgniteFields(), Is.False);
            CastFire(runtime, 1);
            CastFire(runtime, 1);
            Assert.That(runtime.CreateSnapshot().IgnitionReady, Is.True);
            float remainingBefore = runtime.CreateSnapshot().GetField(0).RemainingSeconds;
            Assert.That(runtime.TryIgniteFields(), Is.True);

            Assert.That(resolver.CreateSnapshot().GetEntity(10).Health, Is.LessThan(200));
            Assert.That(runtime.CreateSnapshot().GetField(0).RemainingSeconds, Is.GreaterThan(remainingBefore));
        }

        [Test]
        public void ChainLightningUsesUniqueNearestLinksAndShocksOnlyFirstTarget()
        {
            CombatResolver resolver = BuildWorld();
            resolver.Register(Enemy(10, new RunPoint(1.0f, 0.0f), 100));
            resolver.Register(Enemy(11, new RunPoint(2.0f, 0.0f), 100));
            resolver.Register(Enemy(12, new RunPoint(3.0f, 0.0f), 100));
            resolver.Register(Enemy(13, new RunPoint(7.0f, 0.0f), 100));
            CasterLegionRuntime runtime = BuildRuntime(resolver);
            runtime.SetProgression(CasterLineage.Lightning, 1);
            runtime.SetSlot(CasterLineage.Lightning, 1, RunPoint.Zero);

            runtime.TryCastBase(CasterLineage.Lightning, 1);

            CombatEffectsSnapshot combat = resolver.CreateSnapshot();
            Assert.That(combat.GetEntity(10).Health, Is.EqualTo(88));
            Assert.That(combat.GetEntity(11).Health, Is.EqualTo(88));
            Assert.That(combat.GetEntity(12).Health, Is.EqualTo(88));
            Assert.That(combat.GetEntity(13).Health, Is.EqualTo(100));
            Assert.That(combat.GetEntity(10).GetStatusCount(CombatStatusKind.Shock), Is.EqualTo(1));
            Assert.That(combat.GetEntity(11).GetStatusCount(CombatStatusKind.Shock), Is.EqualTo(0));
        }

        [Test]
        public void StormOverloadWaitsForShockedTargetsThenConsumesShockAndDealsLocalDamage()
        {
            CombatResolver resolver = BuildWorld();
            resolver.Register(Enemy(10, new RunPoint(1.0f, 0.0f), 200));
            resolver.Register(Enemy(11, new RunPoint(1.5f, 0.0f), 200));
            CasterLegionRuntime runtime = BuildRuntime(resolver);
            runtime.SetProgression(CasterLineage.Lightning, 3);
            runtime.SetSlot(CasterLineage.Lightning, 1, RunPoint.Zero);

            Assert.That(runtime.TryResolveShockOverload(), Is.False);
            runtime.TryCastBase(CasterLineage.Lightning, 1);
            runtime.TryCastBase(CasterLineage.Lightning, 1);
            Assert.That(runtime.CreateSnapshot().OverloadReady, Is.True);
            Assert.That(runtime.TryResolveShockOverload(), Is.True);

            CombatEffectsSnapshot combat = resolver.CreateSnapshot();
            Assert.That(combat.GetEntity(10).Health, Is.LessThan(168));
            Assert.That(combat.GetEntity(11).Health, Is.LessThan(176));
            Assert.That(combat.GetEntity(10).GetStatusCount(CombatStatusKind.Shock), Is.EqualTo(0));
        }

        [Test]
        public void TwelveCasterPassivesExposeEveryDeclaredModifier()
        {
            CasterLegionRuntime runtime = BuildRuntime(BuildWorld());
            foreach (CasterPassiveId passive in (CasterPassiveId[])System.Enum.GetValues(typeof(CasterPassiveId)))
                runtime.ApplyPassive(passive);

            CasterModifierSnapshot modifiers = runtime.CreateSnapshot().Modifiers;
            Assert.That(modifiers.AppliedPassiveCount, Is.EqualTo(12));
            Assert.That(modifiers.FlaskRadiusMultiplier, Is.GreaterThan(1.0f));
            Assert.That(modifiers.VulnerabilityMagnitudeBonus, Is.GreaterThan(0.0f));
            Assert.That(modifiers.VulnerabilityDurationMultiplier, Is.GreaterThan(1.0f));
            Assert.That(modifiers.SpreadTargetBonus, Is.GreaterThan(0));
            Assert.That(modifiers.FieldRadiusMultiplier, Is.GreaterThan(1.0f));
            Assert.That(modifiers.FieldDurationMultiplier, Is.GreaterThan(1.0f));
            Assert.That(modifiers.FieldTickIntervalMultiplier, Is.LessThan(1.0f));
            Assert.That(modifiers.FieldCapacityBonus, Is.GreaterThan(0));
            Assert.That(modifiers.ChainTargetBonus, Is.GreaterThan(0));
            Assert.That(modifiers.ChainDamageRetention, Is.GreaterThan(0.0f));
            Assert.That(modifiers.ShockDurationMultiplier, Is.GreaterThan(1.0f));
            Assert.That(modifiers.OverloadRadiusMultiplier, Is.GreaterThan(1.0f));
        }

        [Test]
        public void CasterCooldownStartsAtCastAndDoesNotBankActions()
        {
            CombatResolver resolver = BuildWorld();
            resolver.Register(Enemy(10, new RunPoint(1.0f, 0.0f), 100));
            CasterLegionRuntime runtime = BuildRuntime(resolver, attackPeriod: 1.0f);
            runtime.SetProgression(CasterLineage.Herbalist, 1);
            runtime.SetSlot(CasterLineage.Herbalist, 1, RunPoint.Zero);

            Assert.That(runtime.TryCastBase(CasterLineage.Herbalist, 1), Is.True);
            Assert.That(runtime.TryCastBase(CasterLineage.Herbalist, 1), Is.False);
            runtime.Advance(1.0f);
            Assert.That(runtime.TryCastBase(CasterLineage.Herbalist, 1), Is.True);
        }

        [Test]
        public void RunHostRoutesCasterCommandsAndPublishesCasterSnapshot()
        {
            RunDefinitionSnapshot definition = new RunDefinitionSnapshot(
                53,
                SwordVerticalDefinition.Disabled,
                FormationGrowthDefinition.Disabled,
                FrontlineLegionDefinition.Disabled,
                RangedLegionDefinition.Disabled,
                BuildDefinition());

            using (RunRuntimeHost host = RunCompositionRoot.Build(definition))
            {
                host.Start();
                host.Submit(RunCommand.ClearBlocker(SimulationBlocker.InitialRecruit));
                host.Submit(RunCommand.RegisterCombatEntity(
                    new CombatEntityDefinition(1, CombatEntityKind.Commander, 200, RunPoint.Zero)));
                host.Submit(RunCommand.RegisterCombatEntity(Enemy(10, new RunPoint(1.0f, 0.0f), 100)));
                host.Submit(RunCommand.SetCasterProgression(CasterLineage.Lightning, 1));
                host.Submit(RunCommand.SetCasterSlot(CasterLineage.Lightning, 1, RunPoint.Zero));
                host.Submit(RunCommand.CastCasterBase(CasterLineage.Lightning, 1));
                host.Advance(0.0f);

                Assert.That(host.CurrentSnapshot.CasterLegions.IsEnabled, Is.True);
                Assert.That(host.CurrentSnapshot.CombatEffects.GetEntity(10).Health, Is.EqualTo(88));
            }
        }

        private static CombatResolver BuildWorld()
        {
            CombatResolver resolver = new CombatResolver();
            resolver.Register(new CombatEntityDefinition(1, CombatEntityKind.Commander, 200, RunPoint.Zero));
            return resolver;
        }

        private static CombatEntityDefinition Enemy(int id, RunPoint position, int health)
        {
            return new CombatEntityDefinition(id, CombatEntityKind.NormalEnemy, health, position);
        }

        private static CasterLegionRuntime BuildRuntime(CombatResolver resolver, float attackPeriod = 0.0f)
        {
            return new CasterLegionRuntime(BuildDefinition(attackPeriod), resolver);
        }

        private static CasterLegionDefinition BuildDefinition(float attackPeriod = 0.0f)
        {
            return CasterLegionDefinition.Create(
                commanderEntityId: 1,
                targetRange: 8.0f,
                baseAttackPeriod: attackPeriod,
                HerbalistActionDefinition.Create(10, 1.0f, 1.2f, 3.0f, 2.0f, 2, 1),
                FireFieldActionDefinition.Create(5, 1.0f, 1.0f, 3.0f, 2, 2, 20, 1.5f),
                LightningActionDefinition.Create(12, 1.8f, 3, 0.75f, 2.0f, 2, 20, 1.0f, 3));
        }

        private static void CastFire(CasterLegionRuntime runtime, int memberIndex)
        {
            Assert.That(runtime.TryCastBase(CasterLineage.Fire, memberIndex), Is.True);
        }
    }
}
