using Lizzo.PV.Gameplay.Run;
using NUnit.Framework;

namespace Lizzo.PV.EditorTests
{
    public sealed class SummonedLegionTests
    {
        [Test]
        public void WolfTamerStaysInSlotWhileWolfProxyAttacksLowestHealthTarget()
        {
            CombatResolver resolver = BuildWorld();
            resolver.Register(Enemy(10, new RunPoint(2.0f, 0.0f), 100));
            resolver.Register(Enemy(11, new RunPoint(3.0f, 0.0f), 60));
            SummonedLegionRuntime runtime = BuildRuntime(resolver);
            runtime.SetProgression(SummonedLineage.WolfTamer, 1);
            RunPoint slot = new RunPoint(-2.0f, 0.0f);
            runtime.SetSlot(SummonedLineage.WolfTamer, 1, slot);

            Assert.That(runtime.TryBeginWolfAttack(1), Is.True);
            SummonedMemberSnapshot begun = runtime.CreateSnapshot().GetMember(SummonedLineage.WolfTamer, 1);
            Assert.That(begun.CurrentPosition, Is.EqualTo(slot));
            Assert.That(begun.ProxyTargetId, Is.EqualTo(11));
            Assert.That(resolver.CreateSnapshot().GetEntity(11).Health, Is.EqualTo(60));
            runtime.ResolveWolfImpact(1);

            Assert.That(resolver.CreateSnapshot().GetEntity(11).Health, Is.EqualTo(45));
            Assert.That(runtime.CreateSnapshot().GetMember(SummonedLineage.WolfTamer, 1).CurrentPosition, Is.EqualTo(slot));
        }

        [Test]
        public void WolfProxyChainsOnlyAfterAConfirmedKill()
        {
            CombatResolver resolver = BuildWorld();
            resolver.Register(Enemy(10, new RunPoint(1.0f, 0.0f), 10));
            resolver.Register(Enemy(11, new RunPoint(2.0f, 0.0f), 100));
            SummonedLegionRuntime runtime = BuildRuntime(resolver);
            runtime.SetProgression(SummonedLineage.WolfTamer, 1);
            runtime.SetSlot(SummonedLineage.WolfTamer, 1, RunPoint.Zero);

            runtime.TryBeginWolfAttack(1);
            Assert.That(runtime.ResolveWolfImpact(1), Is.True);
            Assert.That(runtime.CreateSnapshot().GetMember(SummonedLineage.WolfTamer, 1).ProxyTargetId, Is.EqualTo(11));
            Assert.That(runtime.ResolveWolfImpact(1), Is.False);
            Assert.That(resolver.CreateSnapshot().GetEntity(11).Health, Is.EqualTo(85));
        }

        [Test]
        public void BeastCommanderPackIsIndependentFromBaseWolfAndUsesThreeProxies()
        {
            CombatResolver resolver = BuildWorld();
            resolver.Register(Enemy(10, new RunPoint(1.0f, 0.0f), 10));
            resolver.Register(Enemy(11, new RunPoint(2.0f, 0.0f), 200));
            SummonedLegionRuntime runtime = BuildRuntime(resolver);
            runtime.SetProgression(SummonedLineage.WolfTamer, 3);
            runtime.SetSlot(SummonedLineage.WolfTamer, 1, RunPoint.Zero);
            KillWithWolf(runtime, 1);
            resolver.Register(Enemy(12, new RunPoint(3.0f, 0.0f), 10));
            KillWithWolf(runtime, 1);

            Assert.That(runtime.TryLaunchWolfPack(), Is.True);
            Assert.That(runtime.TryBeginWolfAttack(1), Is.True);
            runtime.ResolveWolfPack();

            Assert.That(runtime.CreateSnapshot().WolfPackCastCount, Is.EqualTo(1));
            Assert.That(runtime.CreateSnapshot().WolfPackActive, Is.False);
            Assert.That(resolver.CreateSnapshot().GetEntity(11).Health, Is.LessThan(170));
        }

        [Test]
        public void WraithSlashLocksLocationThenReturnsToLatestSlotAndAppliesWeakening()
        {
            CombatResolver resolver = BuildWorld();
            resolver.Register(Enemy(10, new RunPoint(1.0f, 0.0f), 100));
            SummonedLegionRuntime runtime = BuildRuntime(resolver);
            runtime.SetProgression(SummonedLineage.WraithKnight, 1);
            runtime.SetSlot(SummonedLineage.WraithKnight, 1, new RunPoint(-1.0f, 0.0f));

            runtime.TryBeginWraithSlash(1);
            runtime.ResolveWraithSlash(1);
            runtime.SetSlot(SummonedLineage.WraithKnight, 1, new RunPoint(-2.0f, 0.0f));
            runtime.CompleteWraithReturn(1);

            CombatEffectsSnapshot combat = resolver.CreateSnapshot();
            Assert.That(combat.GetEntity(10).Health, Is.EqualTo(86));
            Assert.That(combat.GetEntity(10).GetStatusCount(CombatStatusKind.Weakened), Is.EqualTo(1));
            Assert.That(runtime.CreateSnapshot().GetMember(SummonedLineage.WraithKnight, 1).CurrentPosition, Is.EqualTo(new RunPoint(-2.0f, 0.0f)));
        }

        [Test]
        public void WraithGuardianPatrolWaitsForNearbyEnemyAndUsesCommanderCenter()
        {
            CombatResolver resolver = BuildWorld();
            resolver.Register(Enemy(10, new RunPoint(1.0f, 0.0f), 100));
            SummonedLegionRuntime runtime = BuildRuntime(resolver);
            runtime.SetProgression(SummonedLineage.WraithKnight, 3);
            runtime.SetSlot(SummonedLineage.WraithKnight, 1, RunPoint.Zero);
            ResolveWraith(runtime, 1);
            ResolveWraith(runtime, 1);

            resolver.SetPosition(10, new RunPoint(7.0f, 0.0f));
            Assert.That(runtime.TryResolveWraithPatrol(), Is.False);
            resolver.SetPosition(10, new RunPoint(1.0f, 0.0f));
            Assert.That(runtime.TryResolveWraithPatrol(), Is.True);
            Assert.That(runtime.CreateSnapshot().LastPatrolCenter, Is.EqualTo(RunPoint.Zero));
            Assert.That(resolver.CreateSnapshot().GetEntity(10).Health, Is.EqualTo(52));
        }

        [Test]
        public void NecromancerCurseDeathPullUsesMovementLockAndBossImmunity()
        {
            CombatResolver resolver = BuildWorld();
            resolver.Register(Enemy(10, new RunPoint(2.0f, 0.0f), 8));
            resolver.Register(Enemy(11, new RunPoint(4.0f, 0.0f), 100));
            resolver.Register(new CombatEntityDefinition(12, CombatEntityKind.BossEnemy, 100, new RunPoint(4.0f, 1.0f)));
            SummonedLegionRuntime runtime = BuildRuntime(resolver);
            runtime.SetProgression(SummonedLineage.Necromancer, 3);
            runtime.SetSlot(SummonedLineage.Necromancer, 1, RunPoint.Zero);

            runtime.TryCastCurseBolt(1);
            Assert.That(runtime.TryResolveCurseDeath(10), Is.True);

            CombatEffectsSnapshot combat = resolver.CreateSnapshot();
            Assert.That(combat.GetEntity(11).IsForcedMovementLocked, Is.True);
            Assert.That(combat.GetEntity(11).ForcedMovementDestination.X, Is.LessThan(4.0f));
            Assert.That(combat.GetEntity(12).IsForcedMovementLocked, Is.False);
        }

        [Test]
        public void DarkRitualSkeletonGroupAttacksDuringDurationWithoutMovingNecromancer()
        {
            CombatResolver resolver = BuildWorld();
            resolver.Register(Enemy(10, new RunPoint(1.0f, 0.0f), 8));
            resolver.Register(Enemy(11, new RunPoint(2.0f, 0.0f), 200));
            SummonedLegionRuntime runtime = BuildRuntime(resolver);
            runtime.SetProgression(SummonedLineage.Necromancer, 3);
            runtime.SetSlot(SummonedLineage.Necromancer, 1, new RunPoint(-1.0f, 0.0f));
            KillWithCurse(runtime, 1, 10);
            resolver.Register(Enemy(12, new RunPoint(3.0f, 0.0f), 8));
            KillWithCurse(runtime, 1, 12);

            Assert.That(runtime.TryBeginDarkRitual(), Is.True);
            runtime.Advance(1.0f);

            SummonedLegionSnapshot snapshot = runtime.CreateSnapshot();
            Assert.That(snapshot.ActiveSkeletonCount, Is.EqualTo(3));
            Assert.That(snapshot.GetMember(SummonedLineage.Necromancer, 1).CurrentPosition, Is.EqualTo(new RunPoint(-1.0f, 0.0f)));
            Assert.That(resolver.CreateSnapshot().GetEntity(11).Health, Is.EqualTo(185));
        }

        [Test]
        public void TwelveSummonedPassivesExposeEveryDeclaredModifier()
        {
            SummonedLegionRuntime runtime = BuildRuntime(BuildWorld());
            foreach (SummonedPassiveId passive in (SummonedPassiveId[])System.Enum.GetValues(typeof(SummonedPassiveId)))
                runtime.ApplyPassive(passive);

            SummonedModifierSnapshot modifiers = runtime.CreateSnapshot().Modifiers;
            Assert.That(modifiers.AppliedPassiveCount, Is.EqualTo(12));
            Assert.That(modifiers.WolfDamageMultiplier, Is.GreaterThan(1.0f));
            Assert.That(modifiers.WolfChainBonus, Is.GreaterThan(0));
            Assert.That(modifiers.WolfExecutionThreshold, Is.GreaterThan(0.0f));
            Assert.That(modifiers.PackDamageMultiplier, Is.GreaterThan(1.0f));
            Assert.That(modifiers.WraithRadiusMultiplier, Is.GreaterThan(1.0f));
            Assert.That(modifiers.WeakenMagnitudeBonus, Is.GreaterThan(0.0f));
            Assert.That(modifiers.WeakenDurationMultiplier, Is.GreaterThan(1.0f));
            Assert.That(modifiers.PatrolRadiusMultiplier, Is.GreaterThan(1.0f));
            Assert.That(modifiers.CurseDurationMultiplier, Is.GreaterThan(1.0f));
            Assert.That(modifiers.PullDistanceMultiplier, Is.GreaterThan(1.0f));
            Assert.That(modifiers.SkeletonCountBonus, Is.GreaterThan(0));
            Assert.That(modifiers.RitualDurationMultiplier, Is.GreaterThan(1.0f));
        }

        [Test]
        public void RunHostRoutesSummonedCommandsAndPublishesSnapshot()
        {
            RunDefinitionSnapshot definition = new RunDefinitionSnapshot(
                67,
                SwordVerticalDefinition.Disabled,
                FormationGrowthDefinition.Disabled,
                FrontlineLegionDefinition.Disabled,
                RangedLegionDefinition.Disabled,
                CasterLegionDefinition.Disabled,
                BuildDefinition());
            using (RunRuntimeHost host = RunCompositionRoot.Build(definition))
            {
                host.Start();
                host.Submit(RunCommand.ClearBlocker(SimulationBlocker.InitialRecruit));
                host.Submit(RunCommand.RegisterCombatEntity(new CombatEntityDefinition(1, CombatEntityKind.Commander, 200, RunPoint.Zero)));
                host.Submit(RunCommand.RegisterCombatEntity(Enemy(10, new RunPoint(1.0f, 0.0f), 100)));
                host.Submit(RunCommand.SetSummonedProgression(SummonedLineage.WolfTamer, 1));
                host.Submit(RunCommand.SetSummonedSlot(SummonedLineage.WolfTamer, 1, RunPoint.Zero));
                host.Submit(RunCommand.BeginWolfAttack(1));
                host.Submit(RunCommand.ResolveWolfImpact(1));
                host.Advance(0.0f);

                Assert.That(host.CurrentSnapshot.SummonedLegions.IsEnabled, Is.True);
                Assert.That(host.CurrentSnapshot.CombatEffects.GetEntity(10).Health, Is.EqualTo(85));
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

        private static SummonedLegionRuntime BuildRuntime(CombatResolver resolver)
        {
            return new SummonedLegionRuntime(BuildDefinition(), resolver);
        }

        private static SummonedLegionDefinition BuildDefinition()
        {
            return SummonedLegionDefinition.Create(
                1,
                8.0f,
                0.0f,
                WolfProxyDefinition.Create(15, 1, 2, 3, 10),
                WraithActionDefinition.Create(14, 1.0f, 0.3f, 3.0f, 2, 20, 2.0f),
                NecromancerActionDefinition.Create(8, 1.0f, 4.0f, 3.0f, 1.0f, 1.0f, 2, 3, 5, 1.0f, 3.0f));
        }

        private static void KillWithWolf(SummonedLegionRuntime runtime, int memberIndex)
        {
            Assert.That(runtime.TryBeginWolfAttack(memberIndex), Is.True);
            while (runtime.ResolveWolfImpact(memberIndex))
            {
            }
        }

        private static void ResolveWraith(SummonedLegionRuntime runtime, int memberIndex)
        {
            Assert.That(runtime.TryBeginWraithSlash(memberIndex), Is.True);
            runtime.ResolveWraithSlash(memberIndex);
            runtime.CompleteWraithReturn(memberIndex);
        }

        private static void KillWithCurse(SummonedLegionRuntime runtime, int memberIndex, int targetId)
        {
            Assert.That(runtime.TryCastCurseBolt(memberIndex), Is.True);
            Assert.That(runtime.TryResolveCurseDeath(targetId), Is.True);
        }
    }
}
