using Lizzo.PV.Gameplay.Run.M2;
using NUnit.Framework;

namespace Lizzo.PV.EditorTests
{
    public sealed class M2FrontlineLegionTests
    {
        [Test]
        public void SwordLocksEnemyPositionHitsAreaThereAndReturnsToLatestSlot()
        {
            CombatResolver resolver = BuildWorld();
            resolver.Register(Enemy(10, new RunPoint(1.0f, 0.0f), 100));
            resolver.Register(Enemy(11, new RunPoint(1.3f, 0.0f), 100));
            FrontlineLegionRuntime runtime = BuildRuntime(resolver);
            runtime.SetProgression(FrontlineLineage.Sword, 1);
            runtime.SetSlot(FrontlineLineage.Sword, 1, new RunPoint(-1.0f, 0.0f));

            Assert.That(runtime.TryBeginBaseAttack(FrontlineLineage.Sword, 1), Is.True);
            FrontlineMemberSnapshot begun = runtime.CreateSnapshot().GetMember(FrontlineLineage.Sword, 1);
            Assert.That(begun.LockedTargetPoint, Is.EqualTo(new RunPoint(1.0f, 0.0f)));
            Assert.That(begun.ActionOrigin, Is.EqualTo(new RunPoint(-1.0f, 0.0f)));

            resolver.SetPosition(10, new RunPoint(4.0f, 0.0f));
            runtime.ResolveBaseImpact(FrontlineLineage.Sword, 1);
            Assert.That(resolver.CreateSnapshot().GetEntity(10).Health, Is.EqualTo(100));
            Assert.That(resolver.CreateSnapshot().GetEntity(11).Health, Is.EqualTo(80));

            runtime.SetSlot(FrontlineLineage.Sword, 1, new RunPoint(-2.0f, 0.0f));
            runtime.CompleteReturn(FrontlineLineage.Sword, 1);
            Assert.That(
                runtime.CreateSnapshot().GetMember(FrontlineLineage.Sword, 1).CurrentPosition,
                Is.EqualTo(new RunPoint(-2.0f, 0.0f)));
        }

        [Test]
        public void PromotionDoesNotRetroactivelyCountRunningSwordActionAndCrescentStartsAfterReturn()
        {
            CombatResolver resolver = BuildWorld();
            resolver.Register(Enemy(10, new RunPoint(1.0f, 0.0f), 500));
            FrontlineLegionRuntime runtime = BuildRuntime(resolver);
            runtime.SetProgression(FrontlineLineage.Sword, 2);
            runtime.SetSlot(FrontlineLineage.Sword, 1, new RunPoint(-1.0f, 0.0f));
            runtime.TryBeginBaseAttack(FrontlineLineage.Sword, 1);
            runtime.SetProgression(FrontlineLineage.Sword, 3);
            runtime.ResolveBaseImpact(FrontlineLineage.Sword, 1);
            runtime.CompleteReturn(FrontlineLineage.Sword, 1);
            Assert.That(runtime.CreateSnapshot().SwordPromotionProgress, Is.Zero);

            ExecuteMelee(runtime, FrontlineLineage.Sword, 1);
            Assert.That(runtime.CreateSnapshot().SwordCrescentCastCount, Is.Zero);
            runtime.TryBeginBaseAttack(FrontlineLineage.Sword, 1);
            runtime.ResolveBaseImpact(FrontlineLineage.Sword, 1);
            Assert.That(runtime.CreateSnapshot().SwordCrescentCastCount, Is.Zero);
            runtime.CompleteReturn(FrontlineLineage.Sword, 1);

            Assert.That(runtime.CreateSnapshot().SwordCrescentCastCount, Is.EqualTo(1));
        }

        [Test]
        public void ShieldChoosesClosestThreatAndPushesAwayFromCommander()
        {
            CombatResolver resolver = BuildWorld();
            resolver.Register(Enemy(10, new RunPoint(2.0f, 0.0f), 100));
            resolver.Register(Enemy(11, new RunPoint(-1.0f, 0.0f), 100));
            FrontlineLegionRuntime runtime = BuildRuntime(resolver);
            runtime.SetProgression(FrontlineLineage.Shield, 1);
            runtime.SetSlot(FrontlineLineage.Shield, 1, new RunPoint(0.0f, -1.0f));

            Assert.That(runtime.TryBeginBaseAttack(FrontlineLineage.Shield, 1), Is.True);
            FrontlineMemberSnapshot begun = runtime.CreateSnapshot().GetMember(FrontlineLineage.Shield, 1);
            Assert.That(begun.LockedTargetId, Is.EqualTo(11));
            Assert.That(begun.ActionPoint.X, Is.LessThan(0.0f));

            runtime.ResolveBaseImpact(FrontlineLineage.Shield, 1);
            CombatEntitySnapshot pushed = resolver.CreateSnapshot().GetEntity(11);
            Assert.That(pushed.Health, Is.EqualTo(92));
            Assert.That(pushed.IsForcedMovementLocked, Is.True);
            Assert.That(pushed.ForcedMovementDestination.X, Is.LessThan(-1.0f));
        }

        [Test]
        public void ShieldPromotionWaitsForEnemyAndStartsNextPeriodAtCastCompletion()
        {
            CombatResolver resolver = BuildWorld();
            FrontlineLegionRuntime runtime = BuildRuntime(resolver);
            runtime.SetProgression(FrontlineLineage.Shield, 3);
            runtime.Advance(3.0f);

            Assert.That(runtime.TryCastShieldShockwave(), Is.False);
            Assert.That(runtime.CreateSnapshot().ShieldSpecialReady, Is.True);

            resolver.Register(Enemy(10, new RunPoint(1.0f, 0.0f), 100));
            Assert.That(runtime.TryCastShieldShockwave(), Is.True);
            Assert.That(runtime.CreateSnapshot().ShieldShockwaveCastCount, Is.EqualTo(1));
            Assert.That(runtime.TryCastShieldShockwave(), Is.False);
            runtime.Advance(2.9f);
            Assert.That(runtime.CreateSnapshot().ShieldSpecialReady, Is.False);
            runtime.Advance(0.1f);
            Assert.That(runtime.CreateSnapshot().ShieldSpecialReady, Is.True);
        }

        [Test]
        public void ClericAllowsOneLightUntilReturnAndHealsOnlyAfterSuccessfulHitReturns()
        {
            CombatResolver resolver = BuildWorld(commanderHealth: 100);
            resolver.Register(Enemy(10, new RunPoint(2.0f, 0.0f), 100));
            resolver.ApplyDamage(new DamageRequest(10, 1, 40.0f, CombatDamageKind.Direct));
            FrontlineLegionRuntime runtime = BuildRuntime(resolver);
            runtime.SetProgression(FrontlineLineage.Cleric, 1);
            runtime.SetSlot(FrontlineLineage.Cleric, 1, new RunPoint(-1.0f, 0.0f));

            Assert.That(runtime.TryBeginBaseAttack(FrontlineLineage.Cleric, 1), Is.True);
            Assert.That(runtime.TryBeginBaseAttack(FrontlineLineage.Cleric, 1), Is.False);
            runtime.ResolveClericImpact(1, didHit: true);
            Assert.That(resolver.CreateSnapshot().GetEntity(1).Health, Is.EqualTo(60));
            runtime.CompleteClericReturn(1);
            Assert.That(resolver.CreateSnapshot().GetEntity(1).Health, Is.EqualTo(72));

            Assert.That(runtime.TryBeginBaseAttack(FrontlineLineage.Cleric, 1), Is.True);
            runtime.ResolveClericImpact(1, didHit: false);
            runtime.CompleteClericReturn(1);
            Assert.That(resolver.CreateSnapshot().GetEntity(1).Health, Is.EqualTo(72));
        }

        [Test]
        public void PromotedClericReplacesSingleSanctuaryAfterSuccessfulReturnsOnly()
        {
            CombatResolver resolver = BuildWorld();
            resolver.Register(Enemy(10, new RunPoint(2.0f, 0.0f), 500));
            FrontlineLegionRuntime runtime = BuildRuntime(resolver);
            runtime.SetProgression(FrontlineLineage.Cleric, 3);
            runtime.SetSlot(FrontlineLineage.Cleric, 1, new RunPoint(-1.0f, 0.0f));

            ExecuteCleric(runtime, 1, didHit: false);
            Assert.That(runtime.CreateSnapshot().SanctuaryRevision, Is.Zero);
            ExecuteCleric(runtime, 1, didHit: true);
            ExecuteCleric(runtime, 1, didHit: true);
            FrontlineLegionSnapshot first = runtime.CreateSnapshot();
            Assert.That(first.SanctuaryRevision, Is.EqualTo(1));
            Assert.That(first.SanctuaryPosition, Is.EqualTo(RunPoint.Zero));

            resolver.SetPosition(1, new RunPoint(3.0f, 2.0f));
            ExecuteCleric(runtime, 1, didHit: true);
            ExecuteCleric(runtime, 1, didHit: true);
            FrontlineLegionSnapshot replaced = runtime.CreateSnapshot();
            Assert.That(replaced.SanctuaryRevision, Is.EqualTo(2));
            Assert.That(replaced.SanctuaryPosition, Is.EqualTo(new RunPoint(3.0f, 2.0f)));
        }

        [Test]
        public void TwelveDedicatedPassivesModifyOnlyTheirDeclaredBaseActionFields()
        {
            CombatResolver resolver = BuildWorld();
            FrontlineLegionRuntime runtime = BuildRuntime(resolver);
            foreach (FrontlinePassiveId passive in (FrontlinePassiveId[])System.Enum.GetValues(typeof(FrontlinePassiveId)))
                runtime.ApplyPassive(passive);

            FrontlineModifierSnapshot modifiers = runtime.CreateSnapshot().Modifiers;
            Assert.That(modifiers.AppliedPassiveCount, Is.EqualTo(12));
            Assert.That(modifiers.SwordRadiusMultiplier, Is.GreaterThan(1.0f));
            Assert.That(modifiers.SwordFocusMultiplier, Is.GreaterThan(1.0f));
            Assert.That(modifiers.SwordAfterimageCount, Is.EqualTo(1));
            Assert.That(modifiers.SwordMovementMultiplier, Is.GreaterThan(1.0f));
            Assert.That(modifiers.ShieldRadiusMultiplier, Is.GreaterThan(1.0f));
            Assert.That(modifiers.ShieldPushMultiplier, Is.GreaterThan(1.0f));
            Assert.That(modifiers.ShieldReturnTrailCount, Is.EqualTo(1));
            Assert.That(modifiers.ShieldCloseDamageMultiplier, Is.GreaterThan(1.0f));
            Assert.That(modifiers.ClericPierceBonus, Is.EqualTo(1));
            Assert.That(modifiers.ClericProjectileCount, Is.EqualTo(2));
            Assert.That(modifiers.ClericReturnSpeedMultiplier, Is.GreaterThan(1.0f));
            Assert.That(modifiers.ClericHealingMultiplier, Is.GreaterThan(1.0f));
        }

        [Test]
        public void ClericSplitAndPierceDamageTwoAdditionalTargetsButHealOnce()
        {
            CombatResolver resolver = BuildWorld(commanderHealth: 100);
            resolver.Register(Enemy(10, new RunPoint(1.0f, 0.0f), 100));
            resolver.Register(Enemy(11, new RunPoint(1.2f, 0.2f), 100));
            resolver.Register(Enemy(12, new RunPoint(1.4f, -0.2f), 100));
            resolver.ApplyDamage(new DamageRequest(10, 1, 40.0f, CombatDamageKind.Direct));
            FrontlineLegionRuntime runtime = BuildRuntime(resolver);
            runtime.SetProgression(FrontlineLineage.Cleric, 1);
            runtime.SetSlot(FrontlineLineage.Cleric, 1, new RunPoint(-1.0f, 0.0f));
            runtime.ApplyPassive(FrontlinePassiveId.ClericSplitLight);
            runtime.ApplyPassive(FrontlinePassiveId.ClericPiercingLight);

            ExecuteCleric(runtime, 1, didHit: true);

            CombatEffectsSnapshot combat = resolver.CreateSnapshot();
            Assert.That(combat.GetEntity(10).Health, Is.EqualTo(90));
            Assert.That(combat.GetEntity(11).Health, Is.EqualTo(90));
            Assert.That(combat.GetEntity(12).Health, Is.EqualTo(90));
            Assert.That(combat.GetEntity(1).Health, Is.EqualTo(72));
        }

        [Test]
        public void ShieldReturnTrailDamagesWithoutAddingAnotherPush()
        {
            CombatResolver resolver = BuildWorld();
            resolver.Register(Enemy(10, new RunPoint(0.2f, 0.0f), 100));
            resolver.Register(Enemy(11, new RunPoint(-1.2f, 0.0f), 100));
            FrontlineLegionRuntime runtime = BuildRuntime(resolver);
            runtime.SetProgression(FrontlineLineage.Shield, 1);
            runtime.SetSlot(FrontlineLineage.Shield, 1, new RunPoint(-2.0f, 0.0f));
            runtime.ApplyPassive(FrontlinePassiveId.ShieldReturnTrail);

            runtime.TryBeginBaseAttack(FrontlineLineage.Shield, 1);
            runtime.ResolveBaseImpact(FrontlineLineage.Shield, 1);
            runtime.CompleteReturn(FrontlineLineage.Shield, 1);

            CombatEntitySnapshot trailTarget = resolver.CreateSnapshot().GetEntity(11);
            Assert.That(trailTarget.Health, Is.EqualTo(96));
            Assert.That(trailTarget.IsForcedMovementLocked, Is.False);
        }

        [Test]
        public void CompositionRootProcessesFrontlineCommandsThroughSharedCombatResolver()
        {
            using RunRuntimeHost host = RunCompositionRoot.Build(new RunDefinitionSnapshot(
                77,
                SwordVerticalDefinition.Disabled,
                FormationGrowthDefinition.Disabled,
                BuildDefinition()));
            host.Start();
            host.Submit(RunCommand.ClearBlocker(SimulationBlocker.InitialRecruit));
            host.Submit(RunCommand.RegisterCombatEntity(
                new CombatEntityDefinition(1, CombatEntityKind.Commander, 200, RunPoint.Zero)));
            host.Submit(RunCommand.RegisterCombatEntity(Enemy(10, new RunPoint(1.0f, 0.0f), 100)));
            host.Submit(RunCommand.SetFrontlineProgression(FrontlineLineage.Sword, 1));
            host.Submit(RunCommand.SetFrontlineSlot(
                FrontlineLineage.Sword,
                1,
                new RunPoint(-1.0f, 0.0f)));
            host.Submit(RunCommand.BeginFrontlineBaseAttack(FrontlineLineage.Sword, 1));
            host.Advance(0.0f);

            host.Submit(RunCommand.ResolveFrontlineBaseImpact(FrontlineLineage.Sword, 1));
            host.Submit(RunCommand.CompleteFrontlineReturn(FrontlineLineage.Sword, 1));
            host.Advance(0.0f);

            Assert.That(host.CurrentSnapshot.FrontlineLegions.IsEnabled, Is.True);
            Assert.That(host.CurrentSnapshot.CombatEffects.GetEntity(10).Health, Is.EqualTo(80));
            Assert.That(
                host.CurrentSnapshot.FrontlineLegions.GetMember(FrontlineLineage.Sword, 1).CurrentPosition,
                Is.EqualTo(new RunPoint(-1.0f, 0.0f)));
        }

        [Test]
        public void FormationGrowthAutomaticallyUpdatesFrontlineProgressionAndSlot()
        {
            FormationGrowthDefinition growth = new FormationGrowthDefinition(
                1,
                2.0f,
                0.35f,
                0.25f,
                0.30f,
                new[] { new LegionGrowthDefinition("sword_soldier", "sword_captain", 1.0f) });
            using RunRuntimeHost host = RunCompositionRoot.Build(new RunDefinitionSnapshot(
                79,
                SwordVerticalDefinition.Disabled,
                growth,
                BuildDefinition()));
            host.Start();
            host.Submit(RunCommand.RegisterCombatEntity(
                new CombatEntityDefinition(1, CombatEntityKind.Commander, 200, RunPoint.Zero)));
            host.Submit(RunCommand.ChooseGrowthOffer(0));
            host.Advance(0.0f);

            FrontlineLegionSnapshot frontline = host.CurrentSnapshot.FrontlineLegions;
            FormationSlotSnapshot formationSlot = host.CurrentSnapshot.FormationGrowth.GetSlot("A3");
            Assert.That(frontline.GetProgression(FrontlineLineage.Sword), Is.EqualTo(1));
            Assert.That(frontline.GetMember(FrontlineLineage.Sword, 1).IsActive, Is.True);
            Assert.That(frontline.GetMember(FrontlineLineage.Sword, 1).Slot, Is.EqualTo(formationSlot.LocalOffset));
        }

        [Test]
        public void BaseAttackPeriodStartsAtActionStartAndDoesNotBankExtraAttacks()
        {
            CombatResolver resolver = BuildWorld();
            resolver.Register(Enemy(10, new RunPoint(1.0f, 0.0f), 500));
            FrontlineLegionRuntime runtime = new FrontlineLegionRuntime(BuildDefinition(1.0f), resolver);
            runtime.SetProgression(FrontlineLineage.Sword, 1);
            runtime.SetSlot(FrontlineLineage.Sword, 1, new RunPoint(-1.0f, 0.0f));

            ExecuteMelee(runtime, FrontlineLineage.Sword, 1);
            Assert.That(runtime.TryBeginBaseAttack(FrontlineLineage.Sword, 1), Is.False);
            runtime.Advance(1.0f);
            Assert.That(runtime.TryBeginBaseAttack(FrontlineLineage.Sword, 1), Is.True);
        }

        [Test]
        public void SwordCrescentChoosesTheLineThatHitsTheMostEnemies()
        {
            CombatResolver resolver = BuildWorld();
            resolver.Register(Enemy(10, new RunPoint(1.0f, 0.0f), 500));
            resolver.Register(Enemy(11, new RunPoint(2.0f, 0.0f), 500));
            resolver.Register(Enemy(12, new RunPoint(0.0f, 2.0f), 500));
            FrontlineLegionRuntime runtime = BuildRuntime(resolver);
            runtime.SetProgression(FrontlineLineage.Sword, 3);
            runtime.SetSlot(FrontlineLineage.Sword, 1, new RunPoint(-1.0f, 0.0f));

            ExecuteMelee(runtime, FrontlineLineage.Sword, 1);
            ExecuteMelee(runtime, FrontlineLineage.Sword, 1);

            CombatEffectsSnapshot combat = resolver.CreateSnapshot();
            Assert.That(combat.GetEntity(11).Health, Is.EqualTo(485));
            Assert.That(combat.GetEntity(12).Health, Is.EqualTo(500));
        }

        private static CombatResolver BuildWorld(int commanderHealth = 200)
        {
            CombatResolver resolver = new CombatResolver();
            resolver.Register(new CombatEntityDefinition(
                1,
                CombatEntityKind.Commander,
                commanderHealth,
                RunPoint.Zero));
            return resolver;
        }

        private static CombatEntityDefinition Enemy(int id, RunPoint position, int health)
        {
            return new CombatEntityDefinition(id, CombatEntityKind.NormalEnemy, health, position);
        }

        private static FrontlineLegionRuntime BuildRuntime(CombatResolver resolver)
        {
            return new FrontlineLegionRuntime(BuildDefinition(), resolver);
        }

        private static FrontlineLegionDefinition BuildDefinition(float baseAttackPeriod = 0.0f)
        {
            return FrontlineLegionDefinition.Create(
                    commanderEntityId: 1,
                    targetRange: 6.0f,
                    movementSpeed: 8.0f,
                    baseAttackPeriod: baseAttackPeriod,
                    swordDamage: 20,
                    swordRadius: 0.5f,
                    swordPromotionTrigger: 2,
                    swordCrescentDamage: 15,
                    shieldDamage: 8,
                    shieldRadius: 0.75f,
                    shieldPushDistance: 2.0f,
                    shieldSpecialPeriod: 3.0f,
                    shieldSpecialDamage: 5,
                    shieldSpecialRadius: 2.5f,
                    clericDamage: 10,
                    clericHealing: 12,
                    clericPromotionTrigger: 2,
                    sanctuaryRadius: 2.0f,
                    sanctuaryAttackSpeedMultiplier: 1.25f);
        }

        private static void ExecuteMelee(
            FrontlineLegionRuntime runtime,
            FrontlineLineage lineage,
            int memberIndex)
        {
            Assert.That(runtime.TryBeginBaseAttack(lineage, memberIndex), Is.True);
            runtime.ResolveBaseImpact(lineage, memberIndex);
            runtime.CompleteReturn(lineage, memberIndex);
        }

        private static void ExecuteCleric(
            FrontlineLegionRuntime runtime,
            int memberIndex,
            bool didHit)
        {
            Assert.That(runtime.TryBeginBaseAttack(FrontlineLineage.Cleric, memberIndex), Is.True);
            runtime.ResolveClericImpact(memberIndex, didHit);
            runtime.CompleteClericReturn(memberIndex);
        }
    }
}
