using System;
using System.Collections.Generic;
using System.Reflection;
using Lizzo.PV.Legion.RunCore;
using NUnit.Framework;

namespace Lizzo.PV.EditorTests
{
    public sealed class CompanionRunModuleS3Tests
    {
        [Test]
        public void IndependentMemberMode_ResolvesEveryReinforcedMemberInTheSameAdvance()
        {
            FakeCombatWorld world = new FakeCombatWorld(new CompanionPoint(1.5f, 2.0f));
            FakeCatalog catalog = CreateStationaryPromotableCatalog();
            using CompanionRunModule module = new CompanionRunModule(
                new RunCombatContext(700UL, catalog, world, independentMemberActions: true));

            Assert.That(module.Submit(new CompanionRosterCommand(1L, CompanionRosterCommandKind.Recruit, "warden")).Accepted, Is.True);
            Assert.That(module.Submit(new CompanionRosterCommand(2L, CompanionRosterCommandKind.Reinforce, "warden")).Accepted, Is.True);
            Assert.That(module.Submit(new CompanionRosterCommand(3L, CompanionRosterCommandKind.Promote, "warden")).Accepted, Is.True);

            CompanionAdvanceResult result = module.Advance(new CompanionAdvanceRequest(4L, 2.0f));

            Assert.That(result.Accepted, Is.True);
            Assert.That(result.EffectsResolved, Is.EqualTo(3));
            Assert.That(world.Intents, Has.Count.EqualTo(3));
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, world.Intents.ConvertAll(intent => intent.MemberOrder));
        }

        [Test]
        public void IndependentMemberMode_ExposesEveryExcursionMemberActionInSnapshot()
        {
            FakeCombatWorld world = new FakeCombatWorld(new CompanionPoint(4.0f, 5.0f));
            FakeCatalog catalog = CreateExcursionCatalog();
            using CompanionRunModule module = new CompanionRunModule(
                new RunCombatContext(699UL, catalog, world, independentMemberActions: true));

            Assert.That(module.Submit(new CompanionRosterCommand(1L, CompanionRosterCommandKind.Recruit, "scout")).Accepted, Is.True);
            Assert.That(module.Submit(new CompanionRosterCommand(2L, CompanionRosterCommandKind.Reinforce, "scout")).Accepted, Is.True);
            Assert.That(module.Submit(new CompanionRosterCommand(3L, CompanionRosterCommandKind.Promote, "scout")).Accepted, Is.True);

            Assert.That(module.Advance(new CompanionAdvanceRequest(4L, 1.0f)).Accepted, Is.True);

            SquadSnapshot snapshot = module.CaptureSnapshot().Squads[0];
            Assert.That(snapshot.Members.Count, Is.EqualTo(3));
            for (int index = 0; index < snapshot.Members.Count; index++)
            {
                Assert.That(snapshot.Members[index].MemberOrder, Is.EqualTo(index));
                Assert.That(snapshot.Members[index].ActionPhase, Is.EqualTo(SquadActionPhase.Approaching));
                Assert.That(snapshot.Members[index].CommittedTargetPosition, Is.EqualTo(new CompanionPoint(4.0f, 5.0f)));
            }
        }

        [Test]
        public void IndependentMemberMode_AvoidsSharedTargetsWhenAlternativesExist()
        {
            ReservableCombatWorld world = new ReservableCombatWorld(
                new CompanionPoint(1.0f, 0.0f),
                new CompanionPoint(2.0f, 0.0f),
                new CompanionPoint(3.0f, 0.0f));
            ActionStep step = new ActionStep(
                CombatMotion.Stationary,
                AttackDelivery.Direct,
                "shield-hit",
                10.0f,
                "shield-hit",
                0.0f,
                0.0f,
                0.0f,
                0.0f,
                0.0f,
                3.0f,
                0.0f,
                0.0f,
                true);
            ActionSet set = new ActionSet("shield-base", 1.0f, new[] { step });
            using CompanionRunModule module = new CompanionRunModule(
                new RunCombatContext(
                    698UL,
                    new FakeCatalog(new CompanionDefinition("shield_guard", set)),
                    world,
                    independentMemberActions: true));

            Assert.That(module.Submit(new CompanionRosterCommand(1L, CompanionRosterCommandKind.Recruit, "shield_guard")).Accepted, Is.True);
            Assert.That(module.Submit(new CompanionRosterCommand(2L, CompanionRosterCommandKind.Reinforce, "shield_guard")).Accepted, Is.True);
            Assert.That(module.Submit(new CompanionRosterCommand(3L, CompanionRosterCommandKind.Promote, "shield_guard")).Accepted, Is.True);

            CompanionAdvanceResult result = module.Advance(new CompanionAdvanceRequest(4L, 1.0f));

            Assert.That(result.EffectsResolved, Is.EqualTo(3));
            CollectionAssert.AreEqual(
                new[] { 1.0f, 2.0f, 3.0f },
                world.Intents.ConvertAll(intent => intent.TargetPosition.X));
        }

        [Test]
        public void ExcursionMaxDeparture_StopsAtConfiguredDistanceFromFormationAnchor()
        {
            FakeCombatWorld world = new FakeCombatWorld(new CompanionPoint(4.0f, 0.6875f));
            ActionStep step = new ActionStep(
                CombatMotion.Excursion,
                AttackDelivery.Direct,
                "shield-hit",
                10.0f,
                "shield-hit",
                actionDurationSeconds: 5.0f,
                excursionSpeed: 2.8f,
                deliveryDelaySeconds: 0.0f,
                excursionStandOffDistance: 0.0f,
                excursionLateralOffset: 0.0f,
                targetAcquisitionRange: 5.0f,
                returnSpeed: 2.8f,
                excursionMaxDepartureDistance: 2.0f,
                avoidSharedTarget: true);
            ActionSet set = new ActionSet("shield-base", 1.0f, new[] { step });
            using CompanionRunModule module = new CompanionRunModule(
                new RunCombatContext(
                    697UL,
                    new FakeCatalog(new CompanionDefinition("shield_guard", set)),
                    world));

            Assert.That(module.Submit(new CompanionRosterCommand(1L, CompanionRosterCommandKind.Recruit, "shield_guard")).Accepted, Is.True);
            Assert.That(module.Advance(new CompanionAdvanceRequest(2L, 1.0f)).Accepted, Is.True);
            Assert.That(module.Advance(new CompanionAdvanceRequest(3L, 1.0f)).Accepted, Is.True);

            SquadSnapshot snapshot = module.CaptureSnapshot().Squads[0];
            Assert.That(snapshot.ActionPhase, Is.EqualTo(SquadActionPhase.Acting));
            Assert.That(snapshot.ActiveMemberPosition.X - snapshot.FormationAnchor.X, Is.EqualTo(2.0f).Within(0.0001f));
            Assert.That(snapshot.ActiveMemberPosition.Y, Is.EqualTo(snapshot.FormationAnchor.Y).Within(0.0001f));
        }

        [Test]
        public void TutorialShieldPush_DeduplicatesPerTargetWithinOneAdvanceOnly()
        {
            Type type = typeof(CompanionRunModule).Assembly.GetType(
                "Lizzo.PV.Legion.RunCore.CompanionShieldPushDeduplicator",
                throwOnError: true);
            object deduplicator = Activator.CreateInstance(type, nonPublic: true);
            MethodInfo beginAdvance = type.GetMethod(
                "BeginAdvance",
                BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo shouldApply = type.GetMethod(
                "ShouldApply",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(beginAdvance, Is.Not.Null);
            Assert.That(shouldApply, Is.Not.Null);

            beginAdvance.Invoke(deduplicator, null);
            Assert.That(shouldApply.Invoke(deduplicator, new object[] { true, "shield_guard", 11 }), Is.True);
            Assert.That(shouldApply.Invoke(deduplicator, new object[] { true, "shield_guard", 11 }), Is.False);
            Assert.That(shouldApply.Invoke(deduplicator, new object[] { true, "shield_guard", 12 }), Is.True);

            beginAdvance.Invoke(deduplicator, null);
            Assert.That(shouldApply.Invoke(deduplicator, new object[] { true, "shield_guard", 11 }), Is.True);
            Assert.That(shouldApply.Invoke(deduplicator, new object[] { false, "shield_guard", 11 }), Is.True);
            Assert.That(shouldApply.Invoke(deduplicator, new object[] { true, "sword_soldier", 11 }), Is.True);
        }

        [Test]
        public void PromotedStationarySquad_ResolvesMembersSequentiallyAcrossAdvances_WithSharedCooldownAndCommittedTarget()
        {
            FakeCombatWorld world = new FakeCombatWorld(new CompanionPoint(1.5f, 2.0f));
            FakeCatalog catalog = CreateStationaryPromotableCatalog();
            using CompanionRunModule module = new CompanionRunModule(new RunCombatContext(701UL, catalog, world));

            Assert.That(module.Submit(new CompanionRosterCommand(1L, CompanionRosterCommandKind.Recruit, "warden")).Accepted, Is.True);
            Assert.That(module.Submit(new CompanionRosterCommand(2L, CompanionRosterCommandKind.Reinforce, "warden")).Accepted, Is.True);
            Assert.That(module.Submit(new CompanionRosterCommand(3L, CompanionRosterCommandKind.Promote, "warden")).Accepted, Is.True);

            Assert.That(module.CaptureSnapshot().Squads[0].MemberCount, Is.EqualTo(3));
            Assert.That(module.CaptureSnapshot().Squads[0].Members.Count, Is.EqualTo(3));
            Assert.That(module.CaptureSnapshot().Squads[0].ActionPhase, Is.EqualTo(SquadActionPhase.Idle));

            for (int i = 0; i < 3; i += 1)
            {
                CompanionAdvanceResult result = module.Advance(new CompanionAdvanceRequest((long)(i + 4), 2.0f));
                Assert.That(result.Accepted, Is.True);
                Assert.That(result.EffectsResolved, Is.EqualTo(1));
                Assert.That(world.Intents.Count, Is.EqualTo(i + 1));

                EffectIntent intent = world.Intents[i];
                Assert.That(intent.ExecutionSequence, Is.EqualTo((long)(i + 1)));
                Assert.That(intent.MemberOrder, Is.EqualTo(i));
                Assert.That(intent.Motion, Is.EqualTo(CombatMotion.Stationary));
                Assert.That(intent.TargetPosition.X, Is.EqualTo(1.5f).Within(0.0001f));
                Assert.That(intent.TargetPosition.Y, Is.EqualTo(2.0f).Within(0.0001f));

                SquadSnapshot snapshot = module.CaptureSnapshot().Squads[0];
                Assert.That(snapshot.CommittedTargetPosition.HasValue, Is.True);
                Assert.That(snapshot.CommittedTargetPosition.Value.X, Is.EqualTo(1.5f).Within(0.0001f));
                Assert.That(snapshot.CommittedTargetPosition.Value.Y, Is.EqualTo(2.0f).Within(0.0001f));
            }

            SquadSnapshot afterThird = module.CaptureSnapshot().Squads[0];
            Assert.That(afterThird.ActionPhase, Is.EqualTo(SquadActionPhase.Idle));
            Assert.That(afterThird.ActiveMemberOrder, Is.EqualTo(-1));
            Assert.That(afterThird.CooldownRemainingSeconds, Is.EqualTo(0.0f));

            CompanionAdvanceResult nextCycle = module.Advance(new CompanionAdvanceRequest(7L, 0.25f));
            Assert.That(nextCycle.Accepted, Is.True);
            Assert.That(nextCycle.EffectsResolved, Is.EqualTo(1));
            Assert.That(world.Intents, Has.Count.EqualTo(4));
            Assert.That(world.Intents[3].ExecutionSequence, Is.EqualTo(4L));
            Assert.That(world.Intents[3].MemberOrder, Is.EqualTo(0));
        }

        [Test]
        public void ExcursionSquad_ReflowsWithCommanderAndKeepsCommittedTargetUntilReturningToLatestAnchor()
        {
            FakeCombatWorld world = new FakeCombatWorld(
                new CompanionPoint(4.0f, 5.0f),
                new[] { false });
            FakeCatalog catalog = CreateExcursionCatalog();
            using CompanionRunModule module = new CompanionRunModule(new RunCombatContext(777UL, catalog, world));

            Assert.That(module.Submit(new CompanionRosterCommand(1L, CompanionRosterCommandKind.Recruit, "scout")).Accepted, Is.True);

            Assert.That(module.Advance(new CompanionAdvanceRequest(
                1L,
                1.0f,
                new CompanionPoint(4.0f, 5.0f))).Accepted, Is.True);
            Assert.That(world.Intents, Has.Count.EqualTo(0));
            Assert.That(module.CaptureSnapshot().Squads[0].ActionPhase, Is.EqualTo(SquadActionPhase.Approaching));

            CompanionRunSnapshot afterAction = module.CaptureSnapshot();
            Assert.That(afterAction.Squads[0].ActionPhase, Is.EqualTo(SquadActionPhase.Approaching));
            Assert.That(afterAction.Squads[0].FormationAnchor.X, Is.EqualTo(4.0f).Within(0.0001f));
            Assert.That(afterAction.Squads[0].FormationAnchor.Y, Is.EqualTo(5.6875f).Within(0.0001f));
            Assert.That(afterAction.Squads[0].CommittedTargetPosition, Is.Not.Null);
            Assert.That(afterAction.Squads[0].CommittedTargetPosition.Value.X, Is.EqualTo(4.0f).Within(0.0001f));
            Assert.That(afterAction.Squads[0].CommittedTargetPosition.Value.Y, Is.EqualTo(5.0f).Within(0.0001f));

            Assert.That(world.Intents, Has.Count.EqualTo(0));
            Assert.That(world.ResolveResults, Has.Count.EqualTo(0));
            Assert.That(
                module.Advance(new CompanionAdvanceRequest(
                    2L,
                    1.0f,
                    new CompanionPoint(7.0f, 8.0f))).Accepted,
                Is.True);
            Assert.That(world.Intents, Has.Count.EqualTo(1));
            Assert.That(world.ResolveResults, Has.Count.EqualTo(1));
            Assert.That(world.ResolveResults[0].Applied, Is.False);
            Assert.That(world.Intents[0].Motion, Is.EqualTo(CombatMotion.Excursion));
            Assert.That(world.Intents[0].MemberOrder, Is.EqualTo(0));
            afterAction = module.CaptureSnapshot();
            Assert.That(afterAction.Squads[0].FormationAnchor.X, Is.EqualTo(7.0f).Within(0.0001f));
            Assert.That(afterAction.Squads[0].FormationAnchor.Y, Is.EqualTo(8.6875f).Within(0.0001f));
            Assert.That(world.Intents[0].TargetPosition.X, Is.EqualTo(4.0f).Within(0.0001f));
            Assert.That(world.Intents[0].TargetPosition.Y, Is.EqualTo(5.0f).Within(0.0001f));
            Assert.That(afterAction.Squads[0].ActionPhase, Is.EqualTo(SquadActionPhase.Returning));
            Assert.That(afterAction.Squads[0].CooldownRemainingSeconds, Is.EqualTo(0.0f).Within(0.0001f));

            Assert.That(
                module.Advance(new CompanionAdvanceRequest(
                    3L,
                    3.0f,
                    new CompanionPoint(7.0f, 8.0f))).Accepted,
                Is.True);
            Assert.That(module.CaptureSnapshot().Squads[0].ActionPhase, Is.EqualTo(SquadActionPhase.Idle));
            Assert.That(module.CaptureSnapshot().Squads[0].ActiveMemberOrder, Is.EqualTo(-1));

            Assert.That(
                module.CaptureSnapshot().Squads[0].ActiveMemberPosition.X,
                Is.EqualTo(7.0f).Within(0.0001f));
            Assert.That(
                module.CaptureSnapshot().Squads[0].ActiveMemberPosition.Y,
                Is.EqualTo(8.6875f).Within(0.0001f));

            CompanionPoint committed = afterAction.Squads[0].CommittedTargetPosition.Value;
            Assert.That(module.CaptureSnapshot().Squads[0].CommittedTargetPosition, Is.Not.Null);
            Assert.That(module.CaptureSnapshot().Squads[0].CommittedTargetPosition.Value.X, Is.EqualTo(committed.X).Within(0.0001f));
            Assert.That(module.CaptureSnapshot().Squads[0].CommittedTargetPosition.Value.Y, Is.EqualTo(committed.Y).Within(0.0001f));
        }

        [Test]
        public void CancelActiveActions_SnapsActiveMembersToLatestAnchorsAndClearsState()
        {
            FakeCombatWorld world = new FakeCombatWorld(new CompanionPoint(8.0f, 0.0f));
            FakeCatalog catalog = CreateExcursionCatalog();
            using CompanionRunModule module = new CompanionRunModule(new RunCombatContext(888UL, catalog, world));

            Assert.That(module.Submit(new CompanionRosterCommand(1L, CompanionRosterCommandKind.Recruit, "scout")).Accepted, Is.True);
            Assert.That(module.Advance(new CompanionAdvanceRequest(1L, 1.0f)).Accepted, Is.True);

            Assert.That(module.Submit(new CompanionRosterCommand(
                2L,
                CompanionRosterCommandKind.Recruit,
                "healer")).Accepted, Is.True);
            Assert.That(module.CaptureSnapshot().Squads[0].ActionPhase, Is.EqualTo(SquadActionPhase.Approaching));
            _ = module.DrainEvents();

            module.CancelActiveActions();
            SquadSnapshot snapshot = module.CaptureSnapshot().Squads[0];
            Assert.That(snapshot.ActionPhase, Is.EqualTo(SquadActionPhase.Idle));
            Assert.That(snapshot.ActiveMemberOrder, Is.EqualTo(-1));
            Assert.That(snapshot.ActiveMemberPosition.X, Is.EqualTo(0.0f).Within(0.0001f));
            Assert.That(snapshot.ActiveMemberPosition.Y, Is.EqualTo(0.6875f).Within(0.0001f));
            Assert.That(snapshot.CommittedTargetPosition.HasValue, Is.False);
            Assert.That(module.CaptureSnapshot().Squads, Has.Count.EqualTo(2));
            Assert.That(snapshot.CooldownRemainingSeconds, Is.GreaterThan(0.0f));
            Assert.That(module.DrainEvents(), Is.Empty);
        }

        private static FakeCatalog CreateStationaryPromotableCatalog()
        {
            ActionStep baseStep = new ActionStep(
                CombatMotion.Stationary,
                AttackDelivery.Direct,
                "warden-basic",
                10.0f,
                "warden-basic",
                0.0f,
                0.0f);
            ActionStep promotedStep = new ActionStep(
                CombatMotion.Stationary,
                AttackDelivery.Direct,
                "warden-promoted",
                12.0f,
                "warden-promoted",
                0.0f,
                0.0f);

            ActionSet baseSet = new ActionSet("warden-base", 1.5f, new[] { baseStep });
            ActionSet promotedSet = new ActionSet("warden-promoted", 1.5f, new[] { promotedStep });

            return new FakeCatalog(
                new CompanionDefinition(
                    "warden",
                    baseSet,
                    promotedSet),
                new CompanionDefinition("healer", baseSet),
                new CompanionDefinition("guard", promotedSet));
        }

        private static FakeCatalog CreateExcursionCatalog()
        {
            ActionStep excursion = new ActionStep(
                CombatMotion.Excursion,
                AttackDelivery.Direct,
                "scout-damage",
                9.0f,
                "scout-slash",
                0.25f,
                1.5f);

            ActionSet excursionSet = new ActionSet("scout-excursion", 1.0f, new[] { excursion });
            ActionSet healerSet = new ActionSet(
                "healer-basic",
                1.0f,
                new[]
                {
                    new ActionStep(
                        CombatMotion.Stationary,
                        AttackDelivery.Direct,
                        "heal-basic",
                        4.0f,
                        "heal-basic")
                });

            return new FakeCatalog(
                new CompanionDefinition("scout", excursionSet),
                new CompanionDefinition("healer", healerSet));
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
            private readonly CompanionPoint _targetPosition;
            private readonly Queue<bool> _applyResultSequence;

            internal FakeCombatWorld(CompanionPoint targetPosition, IEnumerable<bool> applyResults = null)
            {
                _targetPosition = targetPosition;
                _applyResultSequence = new Queue<bool>(applyResults ?? Array.Empty<bool>());
            }

            internal List<EffectIntent> Intents { get; } = new List<EffectIntent>();

            internal List<EffectResolution> ResolveResults { get; } = new List<EffectResolution>();

            public bool TrySelectTargetPosition(out CompanionPoint targetPosition)
            {
                targetPosition = _targetPosition;
                return true;
            }

            public EffectResolution Resolve(in EffectIntent intent)
            {
                Intents.Add(intent);
                bool applied = true;
                if (_applyResultSequence.Count > 0)
                {
                    applied = _applyResultSequence.Dequeue();
                }

                EffectResolution resolution = new EffectResolution(
                    applied,
                    intent.EffectId,
                    intent.SourceMagnitude,
                    applied ? 1 : 0);
                ResolveResults.Add(resolution);
                return resolution;
            }
        }

        private sealed class ReservableCombatWorld :
            ICompanionCombatWorld,
            ICompanionTargetReservationWorld
        {
            private readonly CompanionPoint[] _targets;
            private readonly HashSet<int> _reserved = new HashSet<int>();

            internal ReservableCombatWorld(params CompanionPoint[] targets)
            {
                _targets = targets;
            }

            internal List<EffectIntent> Intents { get; } = new List<EffectIntent>();

            public void BeginTargetReservationScope()
            {
                _reserved.Clear();
            }

            public void ReserveTargetPosition(CompanionPoint targetPosition)
            {
                for (int index = 0; index < _targets.Length; index++)
                {
                    if (_targets[index].X == targetPosition.X && _targets[index].Y == targetPosition.Y)
                    {
                        _reserved.Add(index);
                        return;
                    }
                }
            }

            public bool TrySelectUnreservedTargetPosition(
                CompanionPoint origin,
                float maxRange,
                out CompanionPoint targetPosition)
            {
                for (int index = 0; index < _targets.Length; index++)
                {
                    if (_reserved.Add(index))
                    {
                        targetPosition = _targets[index];
                        return true;
                    }
                }

                return TrySelectTargetPosition(out targetPosition);
            }

            public bool TrySelectTargetPosition(out CompanionPoint targetPosition)
            {
                targetPosition = _targets[0];
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
