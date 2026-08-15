using System;
using System.Collections.Generic;
using Lizzo.PV.Legion.RunCore;
using NUnit.Framework;

namespace Lizzo.PV.EditorTests
{
    public sealed class CompanionRunModuleS7StructureTests
    {
        [Test]
        public void EmptyActionSet_IsRejected()
        {
            ActionSet emptySet = new ActionSet("empty", 1.0f, Array.Empty<ActionStep>());
            FakeCatalog catalog = new FakeCatalog(new CompanionDefinition("empty", emptySet));
            FakeCombatWorld world = new FakeCombatWorld(new CompanionPoint(1.0f, 1.0f));
            using CompanionRunModule module = new CompanionRunModule(new RunCombatContext(7011UL, catalog, world));

            CompanionRosterCommandResult result = module.Submit(
                new CompanionRosterCommand(1L, CompanionRosterCommandKind.Recruit, "empty"));

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Rejection, Is.EqualTo(CompanionRosterRejection.UnsupportedDefinition));
        }

        [Test]
        public void PromotedSquad_ExecutesMixedDeliveriesPerMemberInAuthoredOrder_WithOneCommittedTargetAndCooldown()
        {
            CompanionPoint firstTarget = new CompanionPoint(2.5f, -1.25f);
            CompanionPoint secondTarget = new CompanionPoint(-4.0f, 3.0f);
            FakeCombatWorld world = new FakeCombatWorld(firstTarget, secondTarget);
            FakeCatalog catalog = CreateMixedDeliveryCatalog();
            using CompanionRunModule module = new CompanionRunModule(new RunCombatContext(7012UL, catalog, world));

            Assert.That(module.Submit(new CompanionRosterCommand(1L, CompanionRosterCommandKind.Recruit, "mixed")).Accepted, Is.True);
            Assert.That(module.Submit(new CompanionRosterCommand(2L, CompanionRosterCommandKind.Reinforce, "mixed")).Accepted, Is.True);
            Assert.That(module.Submit(new CompanionRosterCommand(3L, CompanionRosterCommandKind.Promote, "mixed")).Accepted, Is.True);
            _ = module.DrainEvents();

            Assert.That(module.Advance(new CompanionAdvanceRequest(1L, 5.0f)).Accepted, Is.True);
            for (long sequence = 2L; sequence <= 12L; sequence += 1L)
            {
                Assert.That(module.Advance(new CompanionAdvanceRequest(sequence, 0.01f)).Accepted, Is.True);
            }

            Assert.That(module.Advance(new CompanionAdvanceRequest(13L, 0.01f)).Accepted, Is.True);

            Assert.That(world.TargetSelectionCount, Is.EqualTo(1));
            Assert.That(world.Intents, Has.Count.EqualTo(12));

            string[] expectedEffectIds =
            {
                "mixed-direct",
                "mixed-projectile",
                "mixed-area",
                "mixed-spawned"
            };
            AttackDelivery[] expectedDeliveries =
            {
                AttackDelivery.Direct,
                AttackDelivery.Projectile,
                AttackDelivery.Area,
                AttackDelivery.SpawnedActor
            };

            for (int index = 0; index < world.Intents.Count; index += 1)
            {
                EffectIntent intent = world.Intents[index];
                int stepIndex = index % expectedEffectIds.Length;
                Assert.That(intent.ExecutionSequence, Is.EqualTo(index + 1L));
                Assert.That(intent.MemberOrder, Is.EqualTo(index / expectedEffectIds.Length));
                Assert.That(intent.EffectId, Is.EqualTo(expectedEffectIds[stepIndex]));
                Assert.That(intent.Delivery, Is.EqualTo(expectedDeliveries[stepIndex]));
                Assert.That(intent.TargetPosition.X, Is.EqualTo(firstTarget.X).Within(0.0001f));
                Assert.That(intent.TargetPosition.Y, Is.EqualTo(firstTarget.Y).Within(0.0001f));
            }

            CompanionRunSnapshot completed = module.CaptureSnapshot();
            Assert.That(completed.PendingDetachedExecutionCount, Is.EqualTo(0));
            Assert.That(completed.Squads[0].ActionPhase, Is.EqualTo(SquadActionPhase.Idle));
            Assert.That(completed.Squads[0].ActiveMemberOrder, Is.EqualTo(-1));
            Assert.That(completed.Squads[0].CommittedTargetPosition, Is.EqualTo(firstTarget));
            Assert.That(completed.Squads[0].CooldownRemainingSeconds, Is.GreaterThan(4.8f));

            Assert.That(module.Advance(new CompanionAdvanceRequest(14L, 4.0f)).Accepted, Is.True);
            Assert.That(world.TargetSelectionCount, Is.EqualTo(1));
            Assert.That(world.Intents, Has.Count.EqualTo(12));

            Assert.That(module.Advance(new CompanionAdvanceRequest(15L, 1.0f)).Accepted, Is.True);
            Assert.That(world.TargetSelectionCount, Is.EqualTo(2));
            Assert.That(world.Intents, Has.Count.EqualTo(13));
            Assert.That(world.Intents[12].MemberOrder, Is.EqualTo(0));
            Assert.That(world.Intents[12].EffectId, Is.EqualTo("mixed-direct"));
            Assert.That(world.Intents[12].TargetPosition.X, Is.EqualTo(secondTarget.X).Within(0.0001f));
            Assert.That(world.Intents[12].TargetPosition.Y, Is.EqualTo(secondTarget.Y).Within(0.0001f));
        }

        [Test]
        public void MixedMotionSteps_CompleteForEachMemberBeforeTheNextMemberStarts()
        {
            CompanionPoint committedTarget = new CompanionPoint(1.0f, 0.5f);
            FakeCombatWorld world = new FakeCombatWorld(committedTarget);
            ActionSet actionSet = new ActionSet(
                "mixed-motion",
                10.0f,
                new[]
                {
                    new ActionStep(
                        CombatMotion.Stationary,
                        AttackDelivery.Direct,
                        "stationary-direct",
                        3.0f,
                        "stationary-direct"),
                    new ActionStep(
                        CombatMotion.Excursion,
                        AttackDelivery.Area,
                        "excursion-area",
                        7.0f,
                        "excursion-area",
                        0.25f,
                        10.0f)
                });
            FakeCatalog catalog = new FakeCatalog(new CompanionDefinition("motion", actionSet));
            using CompanionRunModule module = new CompanionRunModule(new RunCombatContext(7013UL, catalog, world));

            Assert.That(module.Submit(new CompanionRosterCommand(1L, CompanionRosterCommandKind.Recruit, "motion")).Accepted, Is.True);
            Assert.That(module.Submit(new CompanionRosterCommand(2L, CompanionRosterCommandKind.Reinforce, "motion")).Accepted, Is.True);
            _ = module.DrainEvents();

            Assert.That(module.Advance(new CompanionAdvanceRequest(1L, 10.0f)).Accepted, Is.True);
            Assert.That(module.Advance(new CompanionAdvanceRequest(2L, 1.0f)).Accepted, Is.True);
            Assert.That(module.Advance(new CompanionAdvanceRequest(3L, 0.01f)).Accepted, Is.True);
            Assert.That(module.Advance(new CompanionAdvanceRequest(4L, 1.0f)).Accepted, Is.True);
            Assert.That(module.Advance(new CompanionAdvanceRequest(5L, 0.01f)).Accepted, Is.True);

            Assert.That(world.TargetSelectionCount, Is.EqualTo(1));
            Assert.That(world.Intents, Has.Count.EqualTo(4));
            AssertIntent(world.Intents[0], 0, "stationary-direct", CombatMotion.Stationary, AttackDelivery.Direct, committedTarget);
            AssertIntent(world.Intents[1], 0, "excursion-area", CombatMotion.Excursion, AttackDelivery.Area, committedTarget);
            AssertIntent(world.Intents[2], 1, "stationary-direct", CombatMotion.Stationary, AttackDelivery.Direct, committedTarget);
            AssertIntent(world.Intents[3], 1, "excursion-area", CombatMotion.Excursion, AttackDelivery.Area, committedTarget);

            SquadSnapshot completed = module.CaptureSnapshot().Squads[0];
            Assert.That(completed.ActionPhase, Is.EqualTo(SquadActionPhase.Idle));
            Assert.That(completed.ActiveMemberOrder, Is.EqualTo(-1));
            Assert.That(completed.CommittedTargetPosition, Is.EqualTo(committedTarget));
        }

        private static void AssertIntent(
            EffectIntent intent,
            int memberOrder,
            string effectId,
            CombatMotion motion,
            AttackDelivery delivery,
            CompanionPoint target)
        {
            Assert.That(intent.MemberOrder, Is.EqualTo(memberOrder));
            Assert.That(intent.EffectId, Is.EqualTo(effectId));
            Assert.That(intent.Motion, Is.EqualTo(motion));
            Assert.That(intent.Delivery, Is.EqualTo(delivery));
            Assert.That(intent.TargetPosition.X, Is.EqualTo(target.X).Within(0.0001f));
            Assert.That(intent.TargetPosition.Y, Is.EqualTo(target.Y).Within(0.0001f));
        }

        private static FakeCatalog CreateMixedDeliveryCatalog()
        {
            ActionSet baseSet = new ActionSet(
                "mixed-base",
                5.0f,
                new[]
                {
                    new ActionStep(
                        CombatMotion.Stationary,
                        AttackDelivery.Direct,
                        "base-direct",
                        1.0f,
                        "base-direct")
                });
            ActionSet promotedSet = new ActionSet(
                "mixed-promoted",
                5.0f,
                new[]
                {
                    new ActionStep(
                        CombatMotion.Stationary,
                        AttackDelivery.Direct,
                        "mixed-direct",
                        10.0f,
                        "mixed-direct"),
                    new ActionStep(
                        CombatMotion.Stationary,
                        AttackDelivery.Projectile,
                        "mixed-projectile",
                        11.0f,
                        "mixed-projectile"),
                    new ActionStep(
                        CombatMotion.Stationary,
                        AttackDelivery.Area,
                        "mixed-area",
                        12.0f,
                        "mixed-area"),
                    new ActionStep(
                        CombatMotion.Stationary,
                        AttackDelivery.SpawnedActor,
                        "mixed-spawned",
                        13.0f,
                        "mixed-spawned")
                });

            return new FakeCatalog(new CompanionDefinition("mixed", baseSet, promotedSet));
        }

        private sealed class FakeCatalog : ICompanionDefinitionCatalog
        {
            private readonly Dictionary<string, CompanionDefinition> _definitions =
                new Dictionary<string, CompanionDefinition>(StringComparer.Ordinal);

            internal FakeCatalog(params CompanionDefinition[] definitions)
            {
                foreach (CompanionDefinition definition in definitions)
                {
                    _definitions.Add(definition.CompanionId, definition);
                }
            }

            public bool TryGetDefinition(string companionId, out CompanionDefinition definition)
            {
                return _definitions.TryGetValue(companionId, out definition);
            }
        }

        private sealed class FakeCombatWorld : ICompanionCombatWorld
        {
            private readonly CompanionPoint[] _targets;

            internal FakeCombatWorld(params CompanionPoint[] targets)
            {
                _targets = targets;
            }

            internal int TargetSelectionCount { get; private set; }

            internal List<EffectIntent> Intents { get; } = new List<EffectIntent>();

            public bool TrySelectTargetPosition(out CompanionPoint targetPosition)
            {
                int targetIndex = Math.Min(TargetSelectionCount, _targets.Length - 1);
                targetPosition = _targets[targetIndex];
                TargetSelectionCount += 1;
                return true;
            }

            public EffectResolution Resolve(in EffectIntent intent)
            {
                Intents.Add(intent);
                return new EffectResolution(true, intent.EffectId, intent.SourceMagnitude, 1);
            }
        }
    }
}
