using System;
using System.Collections.Generic;
using Lizzo.PV.Legion.RunCore;
using NUnit.Framework;

namespace Lizzo.PV.EditorTests
{
    public sealed class CompanionRunModuleS6Tests
    {
        [Test]
        public void CardInput_DerivesRecruitReinforceAndPromoteWithoutExposingRosterWrites()
        {
            using CompanionRunModule module = CreateModule();
            ICompanionCardInput input = new CompanionRunExternalAdapter(module);

            CompanionRosterCommandResult recruit = input.SubmitCard(1L, "sword_soldier");
            CompanionRosterCommandResult reinforce = input.SubmitCard(2L, " sword_soldier ");
            CompanionRosterCommandResult promote = input.SubmitCard(3L, "sword_soldier");
            CompanionRosterCommandResult maxed = input.SubmitCard(4L, "sword_soldier");
            CompanionRosterCommandResult unknown = input.SubmitCard(4L, "unknown");

            Assert.That(recruit.Accepted, Is.True);
            Assert.That(reinforce.Accepted, Is.True);
            Assert.That(promote.Accepted, Is.True);
            Assert.That(maxed.Accepted, Is.False);
            Assert.That(maxed.Rejection, Is.EqualTo(CompanionRosterRejection.InvalidRosterState));
            Assert.That(unknown.Accepted, Is.False);
            Assert.That(unknown.Rejection, Is.EqualTo(CompanionRosterRejection.DefinitionMissing));

            CompanionRunSnapshot snapshot = module.CaptureSnapshot();
            Assert.That(snapshot.LastAcceptedCommandSequence, Is.EqualTo(3L));
            Assert.That(snapshot.Squads.Count, Is.EqualTo(1));
            Assert.That(snapshot.Squads[0].MemberCount, Is.EqualTo(3));
            Assert.That(snapshot.Squads[0].Promoted, Is.True);
        }

        [Test]
        public void Output_PullsOneSnapshotAndOneSharedEventBatchWithoutConsumerWriteAuthority()
        {
            using CompanionRunModule module = CreateModule();
            CompanionRunExternalAdapter adapter = new CompanionRunExternalAdapter(module);
            ICompanionCardInput input = adapter;
            ICompanionRunOutput output = adapter;

            Assert.That(input.SubmitCard(1L, "sword_soldier").Accepted, Is.True);

            CompanionRunOutputBatch first = output.Pull();
            Assert.That(first.Snapshot.Squads.Count, Is.EqualTo(1));
            Assert.That(first.Snapshot.LastAcceptedCommandSequence, Is.EqualTo(1L));
            Assert.That(first.Events.Count, Is.EqualTo(1));
            Assert.That(first.Events[0].Kind, Is.EqualTo(CompanionRunEventKind.SquadRecruited));
            Assert.That(first.Events[0].CompanionId, Is.EqualTo("sword_soldier"));
            Assert.That(module.DrainEvents().Count, Is.EqualTo(0));

            CompanionRunOutputBatch second = output.Pull();
            Assert.That(second.Snapshot.Squads.Count, Is.EqualTo(1));
            Assert.That(second.Events.Count, Is.EqualTo(0));
            Assert.That(first.Events.Count, Is.EqualTo(1));
        }

        private static CompanionRunModule CreateModule()
        {
            ActionSet baseAction = new ActionSet(
                "sword-base",
                1.0f,
                new[]
                {
                    new ActionStep(
                        CombatMotion.Stationary,
                        AttackDelivery.Direct,
                        "sword-hit",
                        10.0f,
                        "sword-slash")
                });
            ActionSet promotedAction = new ActionSet(
                "sword-promoted",
                1.0f,
                new[]
                {
                    new ActionStep(
                        CombatMotion.Stationary,
                        AttackDelivery.Direct,
                        "captain-hit",
                        14.0f,
                        "captain-slash")
                });

            return new CompanionRunModule(
                new RunCombatContext(
                    1201UL,
                    new SingleDefinitionCatalog(
                        new CompanionDefinition("sword_soldier", baseAction, promotedAction)),
                    new UnusedCombatWorld()));
        }

        private sealed class SingleDefinitionCatalog : ICompanionDefinitionCatalog
        {
            private readonly CompanionDefinition _definition;

            internal SingleDefinitionCatalog(CompanionDefinition definition)
            {
                _definition = definition;
            }

            public bool TryGetDefinition(string companionId, out CompanionDefinition definition)
            {
                if (string.Equals(companionId, _definition.CompanionId, StringComparison.Ordinal))
                {
                    definition = _definition;
                    return true;
                }

                definition = null;
                return false;
            }
        }

        private sealed class UnusedCombatWorld : ICompanionCombatWorld
        {
            public bool TrySelectTargetPosition(out CompanionPoint targetPosition)
            {
                targetPosition = CompanionPoint.Zero;
                return false;
            }

            public EffectResolution Resolve(in EffectIntent intent)
            {
                throw new InvalidOperationException("S6 adapter tests do not advance combat.");
            }
        }
    }
}
