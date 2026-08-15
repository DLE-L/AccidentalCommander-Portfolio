using System;
using System.Collections.Generic;
using Lizzo.PV.Legion.RunCore;
using NUnit.Framework;

namespace Lizzo.PV.EditorTests
{
    public sealed class CompanionRunModuleS2Tests
    {
        [Test]
        public void Recruit_FillsDeterministicSevenSquads_AndRejectsEighthAsCapacity()
        {
            FakeCatalog catalog = CreateCapacityCatalog();
            FakeCombatWorld world = new FakeCombatWorld(CompanionPoint.Zero);
            using CompanionRunModule module = new CompanionRunModule(new RunCombatContext(401UL, catalog, world));

            string[] companionIds =
            {
                "sword_soldier",
                "cleric",
                "archer",
                "rogue",
                "wizard",
                "paladin",
                "bard"
            };

            for (int index = 0; index < 7; index += 1)
            {
                CompanionRosterCommandResult result = module.Submit(new CompanionRosterCommand(
                    (long)(index + 1),
                    CompanionRosterCommandKind.Recruit,
                    companionIds[index]));
                Assert.That(result.Accepted, Is.True);
                Assert.That(result.Rejection, Is.EqualTo(CompanionRosterRejection.None));
                Assert.That(result.SquadId, Is.EqualTo($"squad-{index}"));
                Assert.That(result.SlotId, Is.EqualTo(index));

                CompanionRunSnapshot snapshot = module.CaptureSnapshot();
                Assert.That(snapshot.Squads.Count, Is.EqualTo(index + 1));
                CompanionPoint[] expectedAnchors = GetFormationAnchors(index + 1);

                for (int squadIndex = 0; squadIndex < snapshot.Squads.Count; squadIndex += 1)
                {
                    Assert.That(snapshot.Squads[squadIndex].SquadId, Is.EqualTo($"squad-{squadIndex}"));
                    Assert.That(snapshot.Squads[squadIndex].SlotId, Is.EqualTo(squadIndex));
                    AssertPoint(snapshot.Squads[squadIndex].FormationAnchor, expectedAnchors[squadIndex]);
                }
            }

            CompanionRunSnapshot beforeCapacity = module.CaptureSnapshot();
            Assert.That(beforeCapacity.LastAcceptedCommandSequence, Is.EqualTo(7L));
            Assert.That(beforeCapacity.Squads.Count, Is.EqualTo(7));

            CompanionRosterCommandResult overflow = module.Submit(new CompanionRosterCommand(
                8L,
                CompanionRosterCommandKind.Recruit,
                "sorcerer"));
            Assert.That(overflow.Accepted, Is.False);
            Assert.That(overflow.Rejection, Is.EqualTo(CompanionRosterRejection.CapacityReached));
            Assert.That(overflow.SquadId, Is.Null);
            Assert.That(overflow.SlotId, Is.EqualTo(-1));
            Assert.That(module.CaptureSnapshot().LastAcceptedCommandSequence, Is.EqualTo(7L));
            Assert.That(module.CaptureSnapshot().Squads.Count, Is.EqualTo(7));
        }

        [Test]
        public void Recruit_Reinforce_Promote_TransitionsMembersAndActionSet()
        {
            FakeCatalog catalog = CreatePromotableCatalog();
            FakeCombatWorld world = new FakeCombatWorld(new CompanionPoint(0.0f, 0.0f));
            using CompanionRunModule module = new CompanionRunModule(new RunCombatContext(507UL, catalog, world));

            Assert.That(module.Submit(new CompanionRosterCommand(
                1L,
                CompanionRosterCommandKind.Recruit,
                "warden")).Accepted, Is.True);

            CompanionRunSnapshot recruited = module.CaptureSnapshot();
            Assert.That(recruited.Squads[0].CompanionId, Is.EqualTo("warden"));
            Assert.That(recruited.Squads[0].ActionSetId, Is.EqualTo("warden-base"));
            Assert.That(recruited.Squads[0].MemberCount, Is.EqualTo(1));
            Assert.That(recruited.Squads[0].Promoted, Is.False);
            AssertMemberLayout(recruited.Squads[0], GetMembersForCount(1));

            IReadOnlyList<CompanionRunEvent> recruitedEvents = module.DrainEvents();
            Assert.That(recruitedEvents.Count, Is.EqualTo(1));
            Assert.That(recruitedEvents[0].Kind, Is.EqualTo(CompanionRunEventKind.SquadRecruited));

            Assert.That(module.Submit(new CompanionRosterCommand(
                2L,
                CompanionRosterCommandKind.Reinforce,
                "warden")).Accepted, Is.True);

            CompanionRunSnapshot reinforced = module.CaptureSnapshot();
            Assert.That(reinforced.Squads[0].MemberCount, Is.EqualTo(2));
            Assert.That(reinforced.Squads[0].Promoted, Is.False);
            Assert.That(reinforced.Squads[0].ActionSetId, Is.EqualTo("warden-base"));
            AssertMemberLayout(reinforced.Squads[0], GetMembersForCount(2));

            IReadOnlyList<CompanionRunEvent> reinforceEvents = module.DrainEvents();
            Assert.That(reinforceEvents.Count, Is.EqualTo(1));
            Assert.That(reinforceEvents[0].Kind, Is.EqualTo(CompanionRunEventKind.SquadReinforced));
            Assert.That(reinforceEvents[0].Order, Is.EqualTo(2L));

            Assert.That(module.Submit(new CompanionRosterCommand(
                3L,
                CompanionRosterCommandKind.Promote,
                "warden")).Accepted, Is.True);

            CompanionRunSnapshot promoted = module.CaptureSnapshot();
            Assert.That(promoted.Squads[0].MemberCount, Is.EqualTo(3));
            Assert.That(promoted.Squads[0].Promoted, Is.True);
            Assert.That(promoted.Squads[0].ActionSetId, Is.EqualTo("warden-promoted"));
            AssertMemberLayout(promoted.Squads[0], GetMembersForCount(3));

            IReadOnlyList<CompanionRunEvent> promotedEvents = module.DrainEvents();
            Assert.That(promotedEvents.Count, Is.EqualTo(1));
            Assert.That(promotedEvents[0].Kind, Is.EqualTo(CompanionRunEventKind.SquadPromoted));
            Assert.That(promotedEvents[0].Order, Is.EqualTo(3L));
        }

        [Test]
        public void Recruit_Reinforce_Promote_WithSharedActionSet_AllowsGrowth()
        {
            FakeCatalog catalog = CreateCapacityCatalog();
            FakeCombatWorld world = new FakeCombatWorld(new CompanionPoint(0.0f, 0.0f));
            using CompanionRunModule module = new CompanionRunModule(new RunCombatContext(808UL, catalog, world));

            Assert.That(module.Submit(new CompanionRosterCommand(
                1L,
                CompanionRosterCommandKind.Recruit,
                "sword_soldier")).Accepted, Is.True);

            Assert.That(module.CaptureSnapshot().Squads[0].MemberCount, Is.EqualTo(1));
            Assert.That(module.CaptureSnapshot().Squads[0].Promoted, Is.False);
            Assert.That(module.CaptureSnapshot().Squads[0].ActionSetId, Is.EqualTo("shared-basic"));

            Assert.That(module.Submit(new CompanionRosterCommand(
                2L,
                CompanionRosterCommandKind.Reinforce,
                "sword_soldier")).Accepted, Is.True);

            Assert.That(module.CaptureSnapshot().Squads[0].MemberCount, Is.EqualTo(2));
            Assert.That(module.CaptureSnapshot().Squads[0].Promoted, Is.False);

            Assert.That(module.Submit(new CompanionRosterCommand(
                3L,
                CompanionRosterCommandKind.Promote,
                "sword_soldier")).Accepted, Is.True);

            CompanionRunSnapshot promoted = module.CaptureSnapshot();
            Assert.That(promoted.Squads[0].MemberCount, Is.EqualTo(3));
            Assert.That(promoted.Squads[0].Promoted, Is.True);
            Assert.That(promoted.Squads[0].ActionSetId, Is.EqualTo("shared-basic"));
        }

        [Test]
        public void RosterStateRejections_PreserveSequenceAndSnapshot()
        {
            FakeCatalog catalog = CreatePromotableCatalog();
            FakeCombatWorld world = new FakeCombatWorld(new CompanionPoint(0.0f, 0.0f));
            using CompanionRunModule module = new CompanionRunModule(new RunCombatContext(619UL, catalog, world));

            CompanionRosterCommandResult missing = module.Submit(new CompanionRosterCommand(
                1L,
                CompanionRosterCommandKind.Reinforce,
                "warden"));
            Assert.That(missing.Accepted, Is.False);
            Assert.That(missing.Rejection, Is.EqualTo(CompanionRosterRejection.SquadMissing));

            CompanionRunSnapshot missingSnapshot = module.CaptureSnapshot();
            Assert.That(missingSnapshot.LastAcceptedCommandSequence, Is.Zero);
            Assert.That(missingSnapshot.Squads, Is.Empty);

            Assert.That(module.Submit(new CompanionRosterCommand(
                1L,
                CompanionRosterCommandKind.Recruit,
                "warden")).Accepted, Is.True);
            Assert.That(module.CaptureSnapshot().LastAcceptedCommandSequence, Is.EqualTo(1L));

            CompanionRosterCommandResult invalidPromote = module.Submit(new CompanionRosterCommand(
                2L,
                CompanionRosterCommandKind.Promote,
                "warden"));
            Assert.That(invalidPromote.Accepted, Is.False);
            Assert.That(invalidPromote.Rejection, Is.EqualTo(CompanionRosterRejection.InvalidRosterState));

            CompanionRunSnapshot invalidPromoteSnapshot = module.CaptureSnapshot();
            Assert.That(invalidPromoteSnapshot.LastAcceptedCommandSequence, Is.EqualTo(1L));
            Assert.That(invalidPromoteSnapshot.Squads[0].MemberCount, Is.EqualTo(1));

            Assert.That(module.Submit(new CompanionRosterCommand(
                2L,
                CompanionRosterCommandKind.Reinforce,
                "warden")).Accepted, Is.True);

            Assert.That(module.CaptureSnapshot().LastAcceptedCommandSequence, Is.EqualTo(2L));

            CompanionRosterCommandResult invalidReinforceAtTwo = module.Submit(new CompanionRosterCommand(
                3L,
                CompanionRosterCommandKind.Reinforce,
                "warden"));
            Assert.That(invalidReinforceAtTwo.Accepted, Is.False);
            Assert.That(invalidReinforceAtTwo.Rejection, Is.EqualTo(CompanionRosterRejection.InvalidRosterState));

            CompanionRunSnapshot invalidReinforceSnapshot = module.CaptureSnapshot();
            Assert.That(invalidReinforceSnapshot.LastAcceptedCommandSequence, Is.EqualTo(2L));
            Assert.That(invalidReinforceSnapshot.Squads[0].MemberCount, Is.EqualTo(2));

            Assert.That(module.Submit(new CompanionRosterCommand(
                3L,
                CompanionRosterCommandKind.Promote,
                "warden")).Accepted, Is.True);
            CompanionRunSnapshot promoted = module.CaptureSnapshot();
            Assert.That(promoted.LastAcceptedCommandSequence, Is.EqualTo(3L));
            Assert.That(promoted.Squads[0].MemberCount, Is.EqualTo(3));
            Assert.That(promoted.Squads[0].Promoted, Is.True);

            CompanionRosterCommandResult invalidPromoteAtCapacity = module.Submit(new CompanionRosterCommand(
                4L,
                CompanionRosterCommandKind.Promote,
                "warden"));
            Assert.That(invalidPromoteAtCapacity.Accepted, Is.False);
            Assert.That(invalidPromoteAtCapacity.Rejection, Is.EqualTo(CompanionRosterRejection.InvalidRosterState));
            CompanionRunSnapshot afterFinalInvalid = module.CaptureSnapshot();
            Assert.That(afterFinalInvalid.LastAcceptedCommandSequence, Is.EqualTo(3L));
            Assert.That(afterFinalInvalid.Squads[0].MemberCount, Is.EqualTo(3));
        }

        private static CompanionPoint[] GetFormationAnchors(int count)
        {
            switch (count)
            {
                case 1:
                    return new[]
                    {
                        new CompanionPoint(0.0f, 1.25f)
                    };
                case 2:
                    return new[]
                    {
                        new CompanionPoint(-0.65f, 1.20f),
                        new CompanionPoint(0.65f, 1.20f)
                    };
                case 3:
                    return new[]
                    {
                        new CompanionPoint(0.0f, 1.30f),
                        new CompanionPoint(-0.75f, 0.65f),
                        new CompanionPoint(0.75f, 0.65f)
                    };
                case 4:
                    return new[]
                    {
                        new CompanionPoint(-0.65f, 1.25f),
                        new CompanionPoint(0.65f, 1.25f),
                        new CompanionPoint(-0.85f, 0.45f),
                        new CompanionPoint(0.85f, 0.45f)
                    };
                case 5:
                    return new[]
                    {
                        new CompanionPoint(0.0f, 1.35f),
                        new CompanionPoint(-0.75f, 0.95f),
                        new CompanionPoint(0.75f, 0.95f),
                        new CompanionPoint(-0.95f, 0.30f),
                        new CompanionPoint(0.95f, 0.30f)
                    };
                case 6:
                    return new[]
                    {
                        new CompanionPoint(-0.55f, 1.40f),
                        new CompanionPoint(0.55f, 1.40f),
                        new CompanionPoint(-0.85f, 0.75f),
                        new CompanionPoint(0.85f, 0.75f),
                        new CompanionPoint(-1.05f, 0.15f),
                        new CompanionPoint(1.05f, 0.15f)
                    };
                case 7:
                    return new[]
                    {
                        new CompanionPoint(0.0f, 1.50f),
                        new CompanionPoint(-0.65f, 1.05f),
                        new CompanionPoint(0.65f, 1.05f),
                        new CompanionPoint(-0.95f, 0.55f),
                        new CompanionPoint(0.95f, 0.55f),
                        new CompanionPoint(-1.15f, 0.0f),
                        new CompanionPoint(1.15f, 0.0f)
                    };
            }

            return Array.Empty<CompanionPoint>();
        }

        private static CompanionMemberSnapshot[] GetMembersForCount(int count)
        {
            if (count == 1)
            {
                return new[]
                {
                    new CompanionMemberSnapshot(0, false, new CompanionPoint(0.0f, 0.0f))
                };
            }

            if (count == 2)
            {
                return new[]
                {
                    new CompanionMemberSnapshot(0, false, new CompanionPoint(-0.22f, 0.0f)),
                    new CompanionMemberSnapshot(1, false, new CompanionPoint(0.22f, 0.0f))
                };
            }

            return new[]
            {
                new CompanionMemberSnapshot(0, false, new CompanionPoint(-0.22f, -0.14f)),
                new CompanionMemberSnapshot(1, false, new CompanionPoint(0.22f, -0.14f)),
                new CompanionMemberSnapshot(2, true, new CompanionPoint(0.0f, 0.22f))
            };
        }

        private static void AssertPoint(CompanionPoint actual, CompanionPoint expected)
        {
            Assert.That(actual.X, Is.EqualTo(expected.X).Within(0.0001f));
            Assert.That(actual.Y, Is.EqualTo(expected.Y).Within(0.0001f));
        }

        private static void AssertMemberLayout(
            SquadSnapshot squadSnapshot,
            CompanionMemberSnapshot[] expectedMembers)
        {
            Assert.That(squadSnapshot.Members.Count, Is.EqualTo(expectedMembers.Length));
            for (int index = 0; index < expectedMembers.Length; index += 1)
            {
                CompanionMemberSnapshot actual = squadSnapshot.Members[index];
                CompanionMemberSnapshot expected = expectedMembers[index];
                Assert.That(actual.MemberOrder, Is.EqualTo(expected.MemberOrder));
                Assert.That(actual.IsPromotedLeader, Is.EqualTo(expected.IsPromotedLeader));
                AssertPoint(actual.LocalOffset, expected.LocalOffset);
            }
        }

        private static FakeCatalog CreateCapacityCatalog()
        {
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
                new CompanionDefinition("sword_soldier", sharedSet),
                new CompanionDefinition("cleric", sharedSet),
                new CompanionDefinition("archer", sharedSet),
                new CompanionDefinition("rogue", sharedSet),
                new CompanionDefinition("wizard", sharedSet),
                new CompanionDefinition("paladin", sharedSet),
                new CompanionDefinition("bard", sharedSet),
                new CompanionDefinition("sorcerer", sharedSet),
                new CompanionDefinition("druid", sharedSet));
        }

        private static FakeCatalog CreatePromotableCatalog()
        {
            ActionSet baseActionSet = new ActionSet(
                "warden-base",
                1.0f,
                new[]
                {
                    new ActionStep(
                        CombatMotion.Stationary,
                        AttackDelivery.Direct,
                        "warden-damage",
                        8.0f,
                        "warden-slash")
                });
            ActionSet promotedActionSet = new ActionSet(
                "warden-promoted",
                1.0f,
                new[]
                {
                    new ActionStep(
                        CombatMotion.Stationary,
                        AttackDelivery.Direct,
                        "warden-damage-strong",
                        14.0f,
                        "warden-flash")
                });

            return new FakeCatalog(
                new CompanionDefinition(
                    "warden",
                    baseActionSet,
                    promotedActionSet),
                new CompanionDefinition("sword_soldier", promotedActionSet, promotedActionSet),
                new CompanionDefinition("cleric", promotedActionSet, promotedActionSet),
                new CompanionDefinition("archer", promotedActionSet, promotedActionSet));
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
