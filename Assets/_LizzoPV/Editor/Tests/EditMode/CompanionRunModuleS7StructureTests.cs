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
        public void PromotedSwordSquad_ExecutesBaseSupportExcursionsThenPromotedLeaderSteps_WithOneTargetAndCadence()
        {
            CompanionPoint firstTarget = new CompanionPoint(2.5f, -1.25f);
            CompanionPoint secondTarget = new CompanionPoint(-4.0f, 3.0f);
            FakeCombatWorld world = new FakeCombatWorld(firstTarget, secondTarget);
            FakeCatalog catalog = CreateSwordRoleCatalog();
            using CompanionRunModule module = new CompanionRunModule(new RunCombatContext(7012UL, catalog, world));

            Assert.That(module.Submit(new CompanionRosterCommand(1L, CompanionRosterCommandKind.Recruit, "sword_soldier")).Accepted, Is.True);
            Assert.That(module.Submit(new CompanionRosterCommand(2L, CompanionRosterCommandKind.Reinforce, "sword_soldier")).Accepted, Is.True);
            Assert.That(module.Submit(new CompanionRosterCommand(3L, CompanionRosterCommandKind.Promote, "sword_soldier")).Accepted, Is.True);
            _ = module.DrainEvents();

            CompanionPoint formationAnchor = module.CaptureSnapshot().Squads[0].FormationAnchor;
            Assert.That(module.CaptureSnapshot().Squads[0].ActionSetId, Is.EqualTo("sword-promoted"));
            Assert.That(module.CaptureSnapshot().Squads[0].CooldownRemainingSeconds, Is.EqualTo(5.0f));
            Assert.That(module.Advance(new CompanionAdvanceRequest(1L, 5.0f)).Accepted, Is.True);
            Assert.That(module.Advance(new CompanionAdvanceRequest(2L, 1.0f)).Accepted, Is.True);
            Assert.That(module.Advance(new CompanionAdvanceRequest(3L, 1.0f)).Accepted, Is.True);
            Assert.That(module.Advance(new CompanionAdvanceRequest(4L, 0.01f)).Accepted, Is.True);
            Assert.That(module.Advance(new CompanionAdvanceRequest(5L, 0.01f)).Accepted, Is.True);

            Assert.That(world.TargetSelectionCount, Is.EqualTo(1));
            Assert.That(world.Intents, Has.Count.EqualTo(4));

            string[] expectedEffectIds =
            {
                "sword-support-slash",
                "sword-support-slash",
                "sword-leader-slash",
                "sword-leader-wave"
            };
            CombatMotion[] expectedMotions =
            {
                CombatMotion.Excursion,
                CombatMotion.Excursion,
                CombatMotion.Stationary,
                CombatMotion.Stationary
            };
            int[] expectedMembers = { 0, 1, 2, 2 };

            for (int index = 0; index < world.Intents.Count; index += 1)
            {
                EffectIntent intent = world.Intents[index];
                Assert.That(intent.ExecutionSequence, Is.EqualTo(index + 1L));
                Assert.That(intent.MemberOrder, Is.EqualTo(expectedMembers[index]));
                Assert.That(intent.EffectId, Is.EqualTo(expectedEffectIds[index]));
                Assert.That(intent.Motion, Is.EqualTo(expectedMotions[index]));
                Assert.That(intent.Delivery, Is.EqualTo(AttackDelivery.Direct));
                Assert.That(intent.TargetPosition.X, Is.EqualTo(firstTarget.X).Within(0.0001f));
                Assert.That(intent.TargetPosition.Y, Is.EqualTo(firstTarget.Y).Within(0.0001f));
            }

            float targetDeltaX = firstTarget.X - formationAnchor.X;
            float targetDeltaY = firstTarget.Y - formationAnchor.Y;
            float targetLength = MathF.Sqrt((targetDeltaX * targetDeltaX) + (targetDeltaY * targetDeltaY));
            float forwardX = targetDeltaX / targetLength;
            float forwardY = targetDeltaY / targetLength;
            CompanionPoint contactBase = new CompanionPoint(
                firstTarget.X - (forwardX * 0.9f),
                firstTarget.Y - (forwardY * 0.9f));
            float sideX = -forwardY;
            float sideY = forwardX;
            EffectIntent firstSupport = world.Intents[0];
            EffectIntent secondSupport = world.Intents[1];
            float firstLateral = ((firstSupport.SourcePosition.X - contactBase.X) * sideX)
                + ((firstSupport.SourcePosition.Y - contactBase.Y) * sideY);
            float secondLateral = ((secondSupport.SourcePosition.X - contactBase.X) * sideX)
                + ((secondSupport.SourcePosition.Y - contactBase.Y) * sideY);
            Assert.That(firstLateral, Is.EqualTo(-0.28f).Within(0.0001f));
            Assert.That(secondLateral, Is.EqualTo(0.28f).Within(0.0001f));
            Assert.That(Distance(firstSupport.SourcePosition, firstTarget), Is.LessThanOrEqualTo(1.1f));
            Assert.That(Distance(secondSupport.SourcePosition, firstTarget), Is.LessThanOrEqualTo(1.1f));
            Assert.That(firstSupport.SourcePosition, Is.Not.EqualTo(secondSupport.SourcePosition));

            CompanionRunSnapshot completed = module.CaptureSnapshot();
            Assert.That(completed.PendingDetachedExecutionCount, Is.EqualTo(0));
            Assert.That(completed.Squads[0].ActionPhase, Is.EqualTo(SquadActionPhase.Idle));
            Assert.That(completed.Squads[0].ActiveMemberOrder, Is.EqualTo(-1));
            Assert.That(completed.Squads[0].CommittedTargetPosition, Is.EqualTo(firstTarget));
            Assert.That(completed.Squads[0].CooldownRemainingSeconds, Is.GreaterThan(2.9f));

            Assert.That(module.Advance(new CompanionAdvanceRequest(6L, 2.9f)).Accepted, Is.True);
            Assert.That(world.TargetSelectionCount, Is.EqualTo(1));
            Assert.That(world.Intents, Has.Count.EqualTo(4));

            Assert.That(module.Advance(new CompanionAdvanceRequest(7L, 0.1f)).Accepted, Is.True);
            Assert.That(world.TargetSelectionCount, Is.EqualTo(2));
            Assert.That(world.Intents, Has.Count.EqualTo(4));
            Assert.That(module.CaptureSnapshot().Squads[0].CommittedTargetPosition.Value.X, Is.EqualTo(secondTarget.X).Within(0.0001f));
            Assert.That(module.CaptureSnapshot().Squads[0].CommittedTargetPosition.Value.Y, Is.EqualTo(secondTarget.Y).Within(0.0001f));
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

        private static FakeCatalog CreateSwordRoleCatalog()
        {
            ActionSet baseSet = new ActionSet(
                "sword-base",
                10.0f,
                new[]
                {
                    new ActionStep(
                        CombatMotion.Excursion,
                        AttackDelivery.Direct,
                        "sword-support-slash",
                        8.0f,
                        "sword-support-slash",
                        0.08f,
                        10.0f,
                        0.0f,
                        0.9f,
                        0.28f)
                });
            ActionSet promotedSet = new ActionSet(
                "sword-promoted",
                5.0f,
                new[]
                {
                    new ActionStep(
                        CombatMotion.Stationary,
                        AttackDelivery.Direct,
                        "sword-leader-slash",
                        14.0f,
                        "sword-leader-slash"),
                    new ActionStep(
                        CombatMotion.Stationary,
                        AttackDelivery.Direct,
                        "sword-leader-wave",
                        18.0f,
                        "sword-leader-wave")
                });

            return new FakeCatalog(new CompanionDefinition("sword_soldier", baseSet, promotedSet));
        }

        private static float Distance(CompanionPoint first, CompanionPoint second)
        {
            float dx = first.X - second.X;
            float dy = first.Y - second.Y;
            return MathF.Sqrt((dx * dx) + (dy * dy));
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
