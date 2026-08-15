using System;
using System.Collections.Generic;
using Lizzo.PV.Legion.RunCore;
using NUnit.Framework;

namespace Lizzo.PV.EditorTests
{
    public sealed class CompanionRunModuleS1Tests
    {
        [Test]
        public void RecruitToDirectResolution_TraversesFacadeDeterministically()
        {
            FakeCatalog catalog = CreateSupportedCatalog();
            FakeCombatWorld world = new FakeCombatWorld(new CompanionPoint(3.5f, -2.0f));
            using CompanionRunModule module = new CompanionRunModule(new RunCombatContext(17UL, catalog, world));

            CompanionRosterCommandResult recruit = module.Submit(new CompanionRosterCommand(
                1L,
                CompanionRosterCommandKind.Recruit,
                " sword_soldier "));

            Assert.That(recruit.Accepted, Is.True);
            Assert.That(recruit.Rejection, Is.EqualTo(CompanionRosterRejection.None));
            Assert.That(recruit.SquadId, Is.EqualTo("squad-0"));
            Assert.That(recruit.SlotId, Is.Zero);

            CompanionRunSnapshot recruited = module.CaptureSnapshot();
            Assert.That(recruited.LastAcceptedCommandSequence, Is.EqualTo(1L));
            Assert.That(recruited.Squads, Has.Count.EqualTo(1));
            Assert.That(recruited.Squads[0].CompanionId, Is.EqualTo("sword_soldier"));
            Assert.That(recruited.Squads[0].ActionSetId, Is.EqualTo("sword-basic"));
            Assert.That(recruited.Squads[0].MemberCount, Is.EqualTo(1));
            Assert.That(recruited.Squads[0].Promoted, Is.False);
            Assert.That(recruited.Squads[0].CombatEligible, Is.True);
            Assert.That(recruited.Squads[0].CooldownRemainingSeconds, Is.EqualTo(1.0f).Within(0.0001f));

            CompanionAdvanceResult partial = module.Advance(new CompanionAdvanceRequest(1L, 0.4f));
            Assert.That(partial.Accepted, Is.True);
            Assert.That(partial.EffectsResolved, Is.Zero);
            Assert.That(world.Intents, Is.Empty);
            Assert.That(module.CaptureSnapshot().Squads[0].CooldownRemainingSeconds, Is.EqualTo(0.6f).Within(0.0001f));

            CompanionAdvanceResult completed = module.Advance(new CompanionAdvanceRequest(2L, 0.6f));
            Assert.That(completed.Accepted, Is.True);
            Assert.That(completed.Rejection, Is.EqualTo(CompanionAdvanceRejection.None));
            Assert.That(completed.ElapsedSeconds, Is.EqualTo(1.0f).Within(0.0001f));
            Assert.That(completed.EffectsResolved, Is.EqualTo(1));
            Assert.That(world.Intents, Has.Count.EqualTo(1));

            EffectIntent intent = world.Intents[0];
            Assert.That(intent.ExecutionSequence, Is.EqualTo(1L));
            Assert.That(intent.SquadId, Is.EqualTo("squad-0"));
            Assert.That(intent.SourceCompanionId, Is.EqualTo("sword_soldier"));
            Assert.That(intent.EffectId, Is.EqualTo("damage-basic"));
            Assert.That(intent.SourceMagnitude, Is.EqualTo(12.0f));
            Assert.That(intent.TargetPosition.X, Is.EqualTo(3.5f));
            Assert.That(intent.TargetPosition.Y, Is.EqualTo(-2.0f));
            Assert.That(intent.Motion, Is.EqualTo(CombatMotion.Stationary));
            Assert.That(intent.Delivery, Is.EqualTo(AttackDelivery.Direct));

            IReadOnlyList<CompanionRunEvent> events = module.DrainEvents();
            Assert.That(events.Count, Is.EqualTo(2));
            Assert.That(events[0].Order, Is.EqualTo(1L));
            Assert.That(events[0].Kind, Is.EqualTo(CompanionRunEventKind.SquadRecruited));
            Assert.That(events[0].PresentationCue.Value.PresentationId, Is.EqualTo("companion-recruited"));
            Assert.That(events[1].Order, Is.EqualTo(2L));
            Assert.That(events[1].Kind, Is.EqualTo(CompanionRunEventKind.EffectResolved));
            Assert.That(events[1].Resolution.Value.Applied, Is.True);
            Assert.That(events[1].Resolution.Value.AppliedMagnitude, Is.EqualTo(12.0f));
            Assert.That(events[1].Resolution.Value.AffectedTargetCount, Is.EqualTo(1));
            Assert.That(events[1].PresentationCue.Value.PresentationId, Is.EqualTo("slash-basic"));
            Assert.That(module.DrainEvents(), Is.Empty);
            Assert.That(module.CaptureSnapshot().Squads[0].CooldownRemainingSeconds, Is.EqualTo(1.0f).Within(0.0001f));
        }

        [Test]
        public void RejectedInputs_DoNotConsumeSequenceOrMutateState()
        {
            FakeCatalog catalog = CreateSupportedCatalog();
            FakeCombatWorld world = new FakeCombatWorld(new CompanionPoint(1.0f, 2.0f));
            using CompanionRunModule module = new CompanionRunModule(new RunCombatContext(23UL, catalog, world));

            CompanionRosterCommandResult blank = module.Submit(new CompanionRosterCommand(
                1L,
                CompanionRosterCommandKind.Recruit,
                "  "));
            AssertRejected(blank, CompanionRosterRejection.InvalidCompanionId);

            CompanionRosterCommandResult recruit = module.Submit(new CompanionRosterCommand(
                1L,
                CompanionRosterCommandKind.Recruit,
                "sword_soldier"));
            Assert.That(recruit.Accepted, Is.True);

            CompanionRosterCommandResult repeatedSequence = module.Submit(new CompanionRosterCommand(
                1L,
                CompanionRosterCommandKind.Recruit,
                "cleric"));
            AssertRejected(repeatedSequence, CompanionRosterRejection.InvalidSequence);

            CompanionRosterCommandResult unsupported = module.Submit(new CompanionRosterCommand(
                2L,
                (CompanionRosterCommandKind)999,
                "sword_soldier"));
            AssertRejected(unsupported, CompanionRosterRejection.UnsupportedCommand);

            string[] capacityIds = new[]
            {
                "cleric",
                "archer",
                "rogue",
                "wizard",
                "paladin",
                "bard",
                "druid"
            };

            for (int index = 0; index < capacityIds.Length - 1; index += 1)
            {
                CompanionRosterCommandResult capacityRecruit = module.Submit(new CompanionRosterCommand(
                    (long)(index + 2),
                    CompanionRosterCommandKind.Recruit,
                    capacityIds[index]));
                Assert.That(capacityRecruit.Accepted, Is.True);
            }

            CompanionRosterCommandResult capacity = module.Submit(new CompanionRosterCommand(
                8L,
                CompanionRosterCommandKind.Recruit,
                "sorcerer"));
            AssertRejected(capacity, CompanionRosterRejection.CapacityReached);

            CompanionRunSnapshot roster = module.CaptureSnapshot();
            Assert.That(roster.LastAcceptedCommandSequence, Is.EqualTo(7L));
            Assert.That(roster.Squads, Has.Count.EqualTo(7));

            using CompanionRunModule oneSquadModule = new CompanionRunModule(
                new RunCombatContext(24UL, CreateSupportedCatalog(), new FakeCombatWorld(new CompanionPoint(1.0f, 2.0f))));
            Assert.That(oneSquadModule.Submit(new CompanionRosterCommand(
                1L,
                CompanionRosterCommandKind.Recruit,
                "sword_soldier")).Accepted, Is.True);

            CompanionAdvanceResult invalidDelta = oneSquadModule.Advance(new CompanionAdvanceRequest(1L, float.NaN));
            Assert.That(invalidDelta.Accepted, Is.False);
            Assert.That(invalidDelta.Rejection, Is.EqualTo(CompanionAdvanceRejection.InvalidDelta));

            CompanionAdvanceResult accepted = oneSquadModule.Advance(new CompanionAdvanceRequest(1L, 1.0f));
            Assert.That(accepted.Accepted, Is.True);
            Assert.That(accepted.EffectsResolved, Is.EqualTo(1));

            CompanionAdvanceResult repeatedAdvance = oneSquadModule.Advance(new CompanionAdvanceRequest(1L, 1.0f));
            Assert.That(repeatedAdvance.Accepted, Is.False);
            Assert.That(repeatedAdvance.Rejection, Is.EqualTo(CompanionAdvanceRejection.InvalidSequence));
            Assert.That(oneSquadModule.CaptureSnapshot().LastAcceptedAdvanceSequence, Is.EqualTo(1L));
            Assert.That(oneSquadModule.CaptureSnapshot().Squads.Count, Is.EqualTo(1));
            IReadOnlyList<CompanionRunEvent> drained = oneSquadModule.DrainEvents();
            Assert.That(drained.Count, Is.GreaterThan(0));
        }

        [Test]
        public void UnsupportedDefinition_IsRejectedBeforeRosterMutation()
        {
            ActionSet unsupportedSet = new ActionSet(
                "projectile-set",
                1.0f,
                new[]
                {
                    new ActionStep(
                        CombatMotion.Stationary,
                        (AttackDelivery)999,
                        "damage-projectile",
                        12.0f,
                        "projectile-basic")
                });
            FakeCatalog catalog = new FakeCatalog(new CompanionDefinition("archer", unsupportedSet));
            FakeCombatWorld world = new FakeCombatWorld(new CompanionPoint(0.0f, 0.0f));
            using CompanionRunModule module = new CompanionRunModule(new RunCombatContext(31UL, catalog, world));

            CompanionRosterCommandResult result = module.Submit(new CompanionRosterCommand(
                1L,
                CompanionRosterCommandKind.Recruit,
                "archer"));

            AssertRejected(result, CompanionRosterRejection.UnsupportedDefinition);
            Assert.That(module.CaptureSnapshot().LastAcceptedCommandSequence, Is.Zero);
            Assert.That(module.CaptureSnapshot().Squads, Is.Empty);
            Assert.That(module.DrainEvents(), Is.Empty);
        }

        [Test]
        public void ResetAndDispose_OwnRunLifecycle()
        {
            FakeCatalog catalog = CreateSupportedCatalog();
            FakeCombatWorld world = new FakeCombatWorld(new CompanionPoint(0.0f, 0.0f));
            CompanionRunModule module = new CompanionRunModule(new RunCombatContext(47UL, catalog, world));
            Assert.That(module.Submit(new CompanionRosterCommand(
                1L,
                CompanionRosterCommandKind.Recruit,
                "sword_soldier")).Accepted, Is.True);
            Assert.That(module.Advance(new CompanionAdvanceRequest(1L, 1.0f)).Accepted, Is.True);

            module.Reset();

            CompanionRunSnapshot reset = module.CaptureSnapshot();
            Assert.That(reset.LastAcceptedCommandSequence, Is.Zero);
            Assert.That(reset.LastAcceptedAdvanceSequence, Is.Zero);
            Assert.That(reset.ElapsedSeconds, Is.Zero);
            Assert.That(reset.Squads, Is.Empty);
            Assert.That(module.DrainEvents(), Is.Empty);
            Assert.That(module.Submit(new CompanionRosterCommand(
                1L,
                CompanionRosterCommandKind.Recruit,
                "sword_soldier")).Accepted, Is.True);

            module.Dispose();
            module.Dispose();

            Assert.Throws<ObjectDisposedException>(() => module.Submit(new CompanionRosterCommand(
                2L,
                CompanionRosterCommandKind.Recruit,
                "cleric")));
            Assert.Throws<ObjectDisposedException>(() => module.Advance(new CompanionAdvanceRequest(1L, 1.0f)));
            Assert.Throws<ObjectDisposedException>(() => module.CaptureSnapshot());
            Assert.Throws<ObjectDisposedException>(() => module.DrainEvents());
            Assert.Throws<ObjectDisposedException>(() => module.Reset());
        }

        [Test]
        public void ExecutionSequence_IsMonotonicAcrossResolutionsAndResetsToOneAfterReset()
        {
            FakeCatalog catalog = CreateSupportedCatalog();
            FakeCombatWorld world = new FakeCombatWorld(new CompanionPoint(0.0f, 0.0f));
            using CompanionRunModule module = new CompanionRunModule(new RunCombatContext(59UL, catalog, world));

            Assert.That(module.Submit(new CompanionRosterCommand(
                1L,
                CompanionRosterCommandKind.Recruit,
                "sword_soldier")).Accepted, Is.True);

            Assert.That(module.Advance(new CompanionAdvanceRequest(1L, 1.0f)).Accepted, Is.True);
            Assert.That(world.Intents, Has.Count.EqualTo(1));
            Assert.That(world.Intents[0].ExecutionSequence, Is.EqualTo(1L));

            Assert.That(module.Advance(new CompanionAdvanceRequest(2L, 1.0f)).Accepted, Is.True);
            Assert.That(world.Intents, Has.Count.EqualTo(2));
            Assert.That(world.Intents[1].ExecutionSequence, Is.EqualTo(2L));

            module.Reset();

            Assert.That(module.Submit(new CompanionRosterCommand(
                1L,
                CompanionRosterCommandKind.Recruit,
                "sword_soldier")).Accepted, Is.True);
            Assert.That(module.Advance(new CompanionAdvanceRequest(1L, 1.0f)).Accepted, Is.True);
            Assert.That(world.Intents, Has.Count.EqualTo(3));
            Assert.That(world.Intents[2].ExecutionSequence, Is.EqualTo(1L));
        }

        private static FakeCatalog CreateSupportedCatalog()
        {
            ActionSet swordSet = new ActionSet(
                "sword-basic",
                1.0f,
                new[]
                {
                    new ActionStep(
                        CombatMotion.Stationary,
                        AttackDelivery.Direct,
                        "damage-basic",
                        12.0f,
                        "slash-basic")
                });
            ActionSet clericSet = new ActionSet(
                "cleric-basic",
                1.0f,
                new[]
                {
                    new ActionStep(
                        CombatMotion.Stationary,
                        AttackDelivery.Direct,
                        "heal-basic",
                        8.0f,
                        "heal-basic")
                });
            ActionSet sharedSet = new ActionSet(
                "shared-basic",
                1.0f,
                new[]
                {
                    new ActionStep(
                        CombatMotion.Stationary,
                        AttackDelivery.Direct,
                        "damage-basic",
                        12.0f,
                        "slash-basic")
                });
            return new FakeCatalog(
                new CompanionDefinition("sword_soldier", swordSet),
                new CompanionDefinition("cleric", clericSet),
                new CompanionDefinition("archer", sharedSet),
                new CompanionDefinition("rogue", sharedSet),
                new CompanionDefinition("wizard", sharedSet),
                new CompanionDefinition("paladin", sharedSet),
                new CompanionDefinition("bard", sharedSet),
                new CompanionDefinition("druid", sharedSet),
                new CompanionDefinition("sorcerer", sharedSet));
        }

        private static void AssertRejected(
            CompanionRosterCommandResult result,
            CompanionRosterRejection rejection)
        {
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Rejection, Is.EqualTo(rejection));
            Assert.That(result.SquadId, Is.Null);
            Assert.That(result.SlotId, Is.EqualTo(-1));
        }

        private sealed class FakeCatalog : ICompanionDefinitionCatalog
        {
            private readonly Dictionary<string, CompanionDefinition> _definitions =
                new Dictionary<string, CompanionDefinition>(StringComparer.Ordinal);

            internal FakeCatalog(params CompanionDefinition[] definitions)
            {
                foreach (CompanionDefinition definition in definitions)
                    _definitions.Add(definition.CompanionId, definition);
            }

            public bool TryGetDefinition(string companionId, out CompanionDefinition definition)
            {
                return _definitions.TryGetValue(companionId, out definition);
            }
        }

        private sealed class FakeCombatWorld : ICompanionCombatWorld
        {
            private readonly CompanionPoint _targetPosition;

            internal FakeCombatWorld(CompanionPoint targetPosition)
            {
                _targetPosition = targetPosition;
            }

            internal List<EffectIntent> Intents { get; } = new List<EffectIntent>();

            public bool TrySelectTargetPosition(out CompanionPoint targetPosition)
            {
                targetPosition = _targetPosition;
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
