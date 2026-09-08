using System;
using System.Collections.Generic;
using Lizzo.PV.Legion;
using Lizzo.PV.Legion.Party.Roster;
using Lizzo.PV.Legion.RunCore;
using Lizzo.PV.Gameplay.Telemetry;
using NUnit.Framework;

namespace Lizzo.PV.EditorTests
{
    public sealed class CompanionRunModuleS6Tests
    {
        [Test]
        public void CardInput_DerivesRecruitReinforceAndPromoteWithoutExposingRosterWrites()
        {
            RunTelemetry.BeginRun();
            using CompanionRunModule module = CreateModule();
            ICompanionCardInput input = new CompanionRunExternalAdapter(module);
            IPartyRosterRuntimeView rosterView = (IPartyRosterRuntimeView)input;

            CompanionRosterCommandResult recruit = input.SubmitCard(1L, "sword_soldier");
            Assert.That(rosterView.ActiveCompanionSlotCount, Is.EqualTo(1));
            Assert.That(rosterView.ActiveCompanionCount, Is.EqualTo(1));
            Assert.That(rosterView.PromotionReadyCount, Is.Zero);
            CompanionRosterCommandResult reinforce = input.SubmitCard(2L, " sword_soldier ");
            Assert.That(rosterView.ActiveCompanionCount, Is.EqualTo(2));
            Assert.That(rosterView.PromotionReadyCount, Is.EqualTo(1));
            CompanionRosterCommandResult promote = input.SubmitCard(3L, "sword_soldier");
            Assert.That(rosterView.ActiveCompanionCount, Is.EqualTo(3));
            Assert.That(rosterView.PromotionReadyCount, Is.Zero);
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
            Assert.That(rosterView.TryGetSlot("sword_soldier", out SquadSlotState slot), Is.True);
            Assert.That(slot.CurrentCount, Is.EqualTo(3));
            Assert.That(slot.IsPromoted, Is.True);
            Assert.That(RunTelemetry.GetCount("companion_recruit"), Is.EqualTo(1));
            Assert.That(RunTelemetry.GetCount("companion_reinforce"), Is.EqualTo(1));
            Assert.That(RunTelemetry.GetCount("companion_promotion"), Is.EqualTo(1));
            Assert.That(RunTelemetry.GetCount(RunTelemetry.FirstRecruit), Is.EqualTo(1));
            Assert.That(RunTelemetry.GetCount(RunTelemetry.FirstPromotion), Is.EqualTo(1));
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

        [Test]
        public void CompatibilitySnapshot_InitializesSevenFixedSlotsAndKeepsPriorSnapshotsDetached()
        {
            using CompanionRunModule module = CreateModule();
            CompanionRunExternalAdapter adapter = new CompanionRunExternalAdapter(module);

            IReadOnlyList<SquadSlotState> emptySnapshot = adapter.GetSquadSlotSnapshot();
            Assert.That(emptySnapshot.Count, Is.EqualTo(7));
            for (int index = 0; index < emptySnapshot.Count; index += 1)
            {
                SquadSlotState slot = emptySnapshot[index];
                Assert.That(slot.SlotId, Is.EqualTo($"squad_{index:00}"));
                Assert.That(slot.IsActive, Is.False);
                Assert.That(slot.CurrentCount, Is.Zero);
                Assert.That(slot.MaxCount, Is.EqualTo(3));
                Assert.That(slot.BaseUnitId, Is.Empty);
                Assert.That(slot.LeaderUnitId, Is.Empty);
            }

            Assert.That(adapter.SubmitCard(1L, "sword_soldier").Accepted, Is.True);
            IReadOnlyList<SquadSlotState> recruitedSnapshot = adapter.GetSquadSlotSnapshot();
            Assert.That(recruitedSnapshot.Count, Is.EqualTo(7));
            Assert.That(recruitedSnapshot[0].SlotId, Is.EqualTo("squad_00"));
            Assert.That(recruitedSnapshot[0].BaseUnitId, Is.EqualTo("sword_soldier"));
            Assert.That(recruitedSnapshot[0].LeaderUnitId, Is.EqualTo("sword_soldier"));
            Assert.That(recruitedSnapshot[0].CurrentCount, Is.EqualTo(1));
            Assert.That(recruitedSnapshot[0].MaxCount, Is.EqualTo(3));
            Assert.That(recruitedSnapshot[0].IsActive, Is.True);
            Assert.That(recruitedSnapshot[1].SlotId, Is.EqualTo("squad_01"));
            Assert.That(recruitedSnapshot[1].IsActive, Is.False);
            Assert.That(recruitedSnapshot[1].MaxCount, Is.EqualTo(3));
            Assert.That(emptySnapshot[0].IsActive, Is.False);
        }

        [Test]
        public void RosterReadModel_RefreshesAfterDirectModuleMutationAndReset()
        {
            using CompanionRunModule module = CreateModule();
            CompanionRunExternalAdapter adapter = new CompanionRunExternalAdapter(module);
            IReadOnlyList<SquadSlotState> emptySnapshot = adapter.GetSquadSlotSnapshot();

            Assert.That(module.Submit(new CompanionRosterCommand(
                1L,
                CompanionRosterCommandKind.Recruit,
                "sword_soldier")).Accepted, Is.True);
            Assert.That(adapter.ActiveCompanionSlotCount, Is.EqualTo(1));
            Assert.That(adapter.GetSquadSlotSnapshot()[0].IsActive, Is.True);

            module.Reset();

            Assert.That(adapter.ActiveCompanionSlotCount, Is.Zero);
            Assert.That(adapter.GetSquadSlotSnapshot()[0].IsActive, Is.False);
            Assert.That(emptySnapshot[0].IsActive, Is.False);
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
