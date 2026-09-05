using Lizzo.PV.Gameplay.Run;
using NUnit.Framework;

namespace Lizzo.PV.EditorTests
{
    public sealed class RangedLegionTests
    {
        [Test]
        public void ArcherFiresFromCurrentSlotAlongFixedDirectionWithLimitedUniquePierces()
        {
            CombatResolver resolver = BuildWorld();
            resolver.Register(Enemy(10, new RunPoint(1.0f, 0.0f), 100));
            resolver.Register(Enemy(11, new RunPoint(2.0f, 0.0f), 100));
            resolver.Register(Enemy(12, new RunPoint(3.0f, 0.0f), 100));
            resolver.Register(Enemy(13, new RunPoint(1.0f, 1.0f), 100));
            RangedLegionRuntime runtime = BuildRuntime(resolver);
            runtime.SetProgression(RangedLineage.Archer, 1);
            runtime.SetSlot(RangedLineage.Archer, 1, new RunPoint(-1.0f, 0.0f));

            Assert.That(runtime.TryBeginBaseAttack(RangedLineage.Archer, 1), Is.True);
            RangedMemberSnapshot begun = runtime.CreateSnapshot().GetMember(RangedLineage.Archer, 1);
            Assert.That(begun.ActionOrigin, Is.EqualTo(new RunPoint(-1.0f, 0.0f)));
            runtime.ResolveArcherFlight(1);

            CombatEffectsSnapshot combat = resolver.CreateSnapshot();
            Assert.That(combat.GetEntity(10).Health, Is.EqualTo(90));
            Assert.That(combat.GetEntity(11).Health, Is.EqualTo(90));
            Assert.That(combat.GetEntity(12).Health, Is.EqualTo(100));
            Assert.That(combat.GetEntity(13).Health, Is.EqualTo(100));
        }

        [Test]
        public void FalconUsesBossEliteNormalThenHighestHealthPriorityAndDoesNotBlockArrows()
        {
            CombatResolver resolver = BuildWorld();
            resolver.Register(new CombatEntityDefinition(10, CombatEntityKind.NormalEnemy, 700, new RunPoint(1.0f, 0.0f)));
            resolver.Register(new CombatEntityDefinition(11, CombatEntityKind.EliteEnemy, 300, new RunPoint(2.0f, 0.0f)));
            resolver.Register(new CombatEntityDefinition(12, CombatEntityKind.BossEnemy, 500, new RunPoint(3.0f, 0.0f)));
            RangedLegionRuntime runtime = BuildRuntime(resolver);
            runtime.SetProgression(RangedLineage.Archer, 3);
            runtime.SetSlot(RangedLineage.Archer, 1, new RunPoint(-1.0f, 0.0f));
            FireArrow(runtime, 1);
            FireArrow(runtime, 1);

            Assert.That(runtime.TryLaunchFalcon(), Is.True);
            Assert.That(runtime.CreateSnapshot().FalconTargetId, Is.EqualTo(12));
            Assert.That(runtime.TryLaunchFalcon(), Is.False);
            Assert.That(runtime.TryBeginBaseAttack(RangedLineage.Archer, 1), Is.True);
            runtime.ResolveArcherFlight(1);
            runtime.CompleteFalcon(didHit: true);

            Assert.That(resolver.CreateSnapshot().GetEntity(12).Health, Is.EqualTo(470));
            Assert.That(runtime.CreateSnapshot().FalconActive, Is.False);
        }

        [Test]
        public void BombLocksDensestGroundPointThenWaitsForLandingAndFuse()
        {
            CombatResolver resolver = BuildWorld();
            resolver.Register(Enemy(10, new RunPoint(3.0f, 0.0f), 100));
            resolver.Register(Enemy(11, new RunPoint(3.4f, 0.0f), 100));
            resolver.Register(Enemy(12, new RunPoint(-3.0f, 0.0f), 100));
            RangedLegionRuntime runtime = BuildRuntime(resolver);
            runtime.SetProgression(RangedLineage.Bombardier, 1);
            runtime.SetSlot(RangedLineage.Bombardier, 1, new RunPoint(-1.0f, 0.0f));

            Assert.That(runtime.TryBeginBaseAttack(RangedLineage.Bombardier, 1), Is.True);
            Assert.That(runtime.CreateSnapshot().GetMember(RangedLineage.Bombardier, 1).LockedPoint.X, Is.GreaterThan(2.0f));
            runtime.ReportBombLanded(1);
            runtime.Advance(0.9f);
            Assert.That(resolver.CreateSnapshot().GetEntity(10).Health, Is.EqualTo(100));
            runtime.Advance(0.1f);

            Assert.That(resolver.CreateSnapshot().GetEntity(10).Health, Is.EqualTo(80));
            Assert.That(resolver.CreateSnapshot().GetEntity(11).Health, Is.EqualTo(80));
            Assert.That(resolver.CreateSnapshot().GetEntity(12).Health, Is.EqualTo(100));
        }

        [Test]
        public void PromotedClusterBombReplacesNextBaseBombAndOverlappingMinisEachDealDamage()
        {
            CombatResolver resolver = BuildWorld();
            resolver.Register(Enemy(10, new RunPoint(2.0f, 0.0f), 500));
            RangedLegionRuntime runtime = BuildRuntime(resolver);
            runtime.SetProgression(RangedLineage.Bombardier, 3);
            runtime.SetSlot(RangedLineage.Bombardier, 1, new RunPoint(-1.0f, 0.0f));
            ThrowBomb(runtime, 1);
            ThrowBomb(runtime, 1);

            Assert.That(runtime.CreateSnapshot().ClusterBombReady, Is.True);
            runtime.TryBeginBaseAttack(RangedLineage.Bombardier, 1);
            Assert.That(runtime.CreateSnapshot().GetMember(RangedLineage.Bombardier, 1).IsClusterBomb, Is.True);
            runtime.ReportBombLanded(1);
            runtime.Advance(1.0f);

            Assert.That(resolver.CreateSnapshot().GetEntity(10).Health, Is.EqualTo(420));
            Assert.That(runtime.CreateSnapshot().ClusterBombReady, Is.False);
        }

        [Test]
        public void ScytheHitsEachEnemyOnceOutboundAndOnceReturningToCurrentCasterSlot()
        {
            CombatResolver resolver = BuildWorld();
            resolver.Register(Enemy(10, new RunPoint(1.0f, 0.0f), 100));
            resolver.Register(Enemy(11, new RunPoint(2.0f, 0.0f), 100));
            RangedLegionRuntime runtime = BuildRuntime(resolver);
            runtime.SetProgression(RangedLineage.Scythe, 1);
            runtime.SetSlot(RangedLineage.Scythe, 1, new RunPoint(-1.0f, 0.0f));

            runtime.TryBeginBaseAttack(RangedLineage.Scythe, 1);
            runtime.ResolveScytheOutbound(1);
            runtime.SetSlot(RangedLineage.Scythe, 1, new RunPoint(-2.0f, 0.0f));
            runtime.ResolveScytheReturn(1);

            CombatEffectsSnapshot combat = resolver.CreateSnapshot();
            Assert.That(combat.GetEntity(10).Health, Is.EqualTo(76));
            Assert.That(combat.GetEntity(11).Health, Is.EqualTo(76));
            Assert.That(
                runtime.CreateSnapshot().GetMember(RangedLineage.Scythe, 1).CurrentPosition,
                Is.EqualTo(new RunPoint(-2.0f, 0.0f)));
        }

        [Test]
        public void GiantScytheFollowsCommanderCenterAndDoesNotBlockBaseScythe()
        {
            CombatResolver resolver = BuildWorld();
            resolver.Register(Enemy(10, new RunPoint(1.0f, 0.0f), 500));
            resolver.Register(Enemy(11, new RunPoint(2.0f, 0.0f), 500));
            RangedLegionRuntime runtime = BuildRuntime(resolver);
            runtime.SetProgression(RangedLineage.Scythe, 3);
            runtime.SetSlot(RangedLineage.Scythe, 1, new RunPoint(-1.0f, 0.0f));
            runtime.TryBeginBaseAttack(RangedLineage.Scythe, 1);
            runtime.ResolveScytheOutbound(1);
            runtime.ResolveScytheReturn(1);

            Assert.That(runtime.TryLaunchGiantScythe(), Is.True);
            resolver.SetPosition(1, new RunPoint(2.0f, 0.0f));
            Assert.That(runtime.TryBeginBaseAttack(RangedLineage.Scythe, 1), Is.True);
            runtime.ResolveGiantScytheOrbit();

            Assert.That(runtime.CreateSnapshot().GiantScytheActive, Is.False);
            Assert.That(runtime.CreateSnapshot().GiantScytheLastCenter, Is.EqualTo(new RunPoint(2.0f, 0.0f)));
            Assert.That(resolver.CreateSnapshot().GetEntity(11).Health, Is.LessThan(476));
        }

        [Test]
        public void TwelveRangedPassivesExposeEveryDeclaredBaseActionModifier()
        {
            RangedLegionRuntime runtime = BuildRuntime(BuildWorld());
            foreach (RangedPassiveId passive in (RangedPassiveId[])System.Enum.GetValues(typeof(RangedPassiveId)))
                runtime.ApplyPassive(passive);

            RangedModifierSnapshot modifiers = runtime.CreateSnapshot().Modifiers;
            Assert.That(modifiers.AppliedPassiveCount, Is.EqualTo(12));
            Assert.That(modifiers.ArrowDirections, Is.EqualTo(3));
            Assert.That(modifiers.ArrowVolleys, Is.EqualTo(2));
            Assert.That(modifiers.ArrowPierceBonus, Is.GreaterThan(0));
            Assert.That(modifiers.ArrowPenetrationDamageStep, Is.GreaterThan(0.0f));
            Assert.That(modifiers.BombCount, Is.EqualTo(2));
            Assert.That(modifiers.BombFuseMultiplier, Is.LessThan(1.0f));
            Assert.That(modifiers.BombFragmentCount, Is.GreaterThan(0));
            Assert.That(modifiers.BombCenterBonus, Is.GreaterThan(0.0f));
            Assert.That(modifiers.ScytheWidthMultiplier, Is.GreaterThan(1.0f));
            Assert.That(modifiers.ScytheReturnSpeedMultiplier, Is.GreaterThan(1.0f));
            Assert.That(modifiers.ScytheDirections, Is.EqualTo(2));
            Assert.That(modifiers.ScytheReturnDamageMultiplier, Is.GreaterThan(1.0f));
        }

        [Test]
        public void BaseAttackCooldownStartsAtActionStartAndDoesNotBankShots()
        {
            CombatResolver resolver = BuildWorld();
            resolver.Register(Enemy(10, new RunPoint(1.0f, 0.0f), 500));
            RangedLegionRuntime runtime = BuildRuntime(resolver, 1.0f);
            runtime.SetProgression(RangedLineage.Archer, 1);
            runtime.SetSlot(RangedLineage.Archer, 1, new RunPoint(-1.0f, 0.0f));

            FireArrow(runtime, 1);
            Assert.That(runtime.TryBeginBaseAttack(RangedLineage.Archer, 1), Is.False);
            runtime.Advance(1.0f);
            Assert.That(runtime.TryBeginBaseAttack(RangedLineage.Archer, 1), Is.True);
        }

        [Test]
        public void RunHostRoutesRangedCommandsAndIncludesRangedStateInSnapshot()
        {
            RunDefinitionSnapshot definition = new RunDefinitionSnapshot(
                41,
                SwordVerticalDefinition.Disabled,
                FormationGrowthDefinition.Disabled,
                FrontlineLegionDefinition.Disabled,
                BuildDefinition());

            using (RunRuntimeHost host = RunCompositionRoot.Build(definition))
            {
                Assert.That(host.Start(), Is.True);
                host.Submit(RunCommand.ClearBlocker(SimulationBlocker.InitialRecruit));
                host.Submit(RunCommand.RegisterCombatEntity(
                    new CombatEntityDefinition(1, CombatEntityKind.Commander, 200, RunPoint.Zero)));
                host.Submit(RunCommand.RegisterCombatEntity(Enemy(10, new RunPoint(1.0f, 0.0f), 100)));
                host.Submit(RunCommand.SetRangedProgression(RangedLineage.Archer, 1));
                host.Submit(RunCommand.SetRangedSlot(RangedLineage.Archer, 1, new RunPoint(-1.0f, 0.0f)));
                host.Submit(RunCommand.BeginRangedBaseAttack(RangedLineage.Archer, 1));
                host.Submit(RunCommand.ResolveArcherFlight(1));
                host.Advance(0.0f);

                Assert.That(host.CurrentSnapshot.RangedLegions.IsEnabled, Is.True);
                Assert.That(host.CurrentSnapshot.CombatEffects.GetEntity(10).Health, Is.EqualTo(90));
                Assert.That(host.CurrentSnapshot.StateDigest, Is.Not.EqualTo(0UL));
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

        private static RangedLegionRuntime BuildRuntime(CombatResolver resolver, float attackPeriod = 0.0f)
        {
            return new RangedLegionRuntime(BuildDefinition(attackPeriod), resolver);
        }

        private static RangedLegionDefinition BuildDefinition(float attackPeriod = 0.0f)
        {
            return RangedLegionDefinition.Create(
                commanderEntityId: 1,
                targetRange: 8.0f,
                baseAttackPeriod: attackPeriod,
                archerDamage: 10,
                arrowWidth: 0.2f,
                arrowPierceCount: 2,
                falconTrigger: 2,
                falconDamage: 30,
                bombDamage: 20,
                bombRadius: 1.0f,
                bombFuseSeconds: 1.0f,
                clusterTrigger: 2,
                clusterMiniDamage: 5,
                clusterMiniRadius: 0.75f,
                clusterMiniCount: 4,
                doubleBombDelay: 0.2f,
                scytheDamage: 12,
                scytheWidth: 0.3f,
                scytheDistance: 6.0f,
                giantScytheHitTrigger: 4,
                giantScytheDamage: 25,
                giantScytheRadius: 2.0f);
        }

        private static void FireArrow(RangedLegionRuntime runtime, int memberIndex)
        {
            Assert.That(runtime.TryBeginBaseAttack(RangedLineage.Archer, memberIndex), Is.True);
            runtime.ResolveArcherFlight(memberIndex);
        }

        private static void ThrowBomb(RangedLegionRuntime runtime, int memberIndex)
        {
            Assert.That(runtime.TryBeginBaseAttack(RangedLineage.Bombardier, memberIndex), Is.True);
            runtime.ReportBombLanded(memberIndex);
            runtime.Advance(1.0f);
        }
    }
}
