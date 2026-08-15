using System;
using System.Collections.Generic;
using Lizzo.PV.Legion.RunCore;
using NUnit.Framework;

namespace Lizzo.PV.EditorTests
{
    public sealed class CompanionRunModuleS4Tests
    {
        [Test]
        public void FourDeliveries_DirectResolvesImmediately_OthersDetachAndKeepSourceSnapshot()
        {
            FakeCombatWorld world = new FakeCombatWorld(new CompanionPoint(0.0f, 1.0f));
            FakeCatalog catalog = CreateDeliveryCatalog();
            using CompanionRunModule module = new CompanionRunModule(new RunCombatContext(901UL, catalog, world));

            Assert.That(
                module.Submit(new CompanionRosterCommand(1L, CompanionRosterCommandKind.Recruit, "direct")).Accepted,
                Is.True);
            Assert.That(
                module.Submit(new CompanionRosterCommand(2L, CompanionRosterCommandKind.Recruit, "projectile")).Accepted,
                Is.True);
            Assert.That(
                module.Submit(new CompanionRosterCommand(3L, CompanionRosterCommandKind.Recruit, "area")).Accepted,
                Is.True);
            Assert.That(
                module.Submit(new CompanionRosterCommand(4L, CompanionRosterCommandKind.Recruit, "spawned")).Accepted,
                Is.True);

            _ = module.DrainEvents();

            Assert.That(
                module.Advance(new CompanionAdvanceRequest(5L, 10.0f)).Accepted,
                Is.True);
            Assert.That(world.Intents.Count, Is.EqualTo(1));
            Assert.That(world.Intents[0].Delivery, Is.EqualTo(AttackDelivery.Direct));

            CompanionRunSnapshot afterFirstAdvance = module.CaptureSnapshot();
            Assert.That(afterFirstAdvance.PendingDetachedExecutionCount, Is.EqualTo(3));
            Assert.That(afterFirstAdvance.DroppedChainRequestCount, Is.EqualTo(0));

            IReadOnlyList<CompanionRunEvent> firstAdvanceEvents = module.DrainEvents();
            Assert.That(firstAdvanceEvents.Count, Is.EqualTo(1));
            Assert.That(firstAdvanceEvents[0].Kind, Is.EqualTo(CompanionRunEventKind.EffectResolved));
            Assert.That(firstAdvanceEvents[0].CompanionId, Is.EqualTo("direct"));

            Assert.That(
                module.Submit(new CompanionRosterCommand(6L, CompanionRosterCommandKind.Reinforce, "projectile")).Accepted,
                Is.True);
            Assert.That(
                module.Submit(new CompanionRosterCommand(7L, CompanionRosterCommandKind.Promote, "projectile")).Accepted,
                Is.True);

            IReadOnlyList<CompanionRunEvent> reinforceAndPromoteEvents = module.DrainEvents();
            Assert.That(reinforceAndPromoteEvents.Count, Is.EqualTo(2));
            Assert.That(reinforceAndPromoteEvents[0].Kind, Is.EqualTo(CompanionRunEventKind.SquadReinforced));
            Assert.That(reinforceAndPromoteEvents[1].Kind, Is.EqualTo(CompanionRunEventKind.SquadPromoted));

            CompanionAdvanceResult secondAdvance = module.Advance(new CompanionAdvanceRequest(8L, 0.5f));
            Assert.That(secondAdvance.EffectsResolved, Is.EqualTo(3));
            Assert.That(world.Intents.Count, Is.EqualTo(4));
            Assert.That(afterFirstAdvance.PendingDetachedExecutionCount, Is.EqualTo(3));

            Assert.That(world.Intents[1].SourceCompanionId, Is.EqualTo("projectile"));
            Assert.That(world.Intents[1].EffectId, Is.EqualTo("projectile-damage"));
            Assert.That(world.Intents[1].PresentationCueId, Is.EqualTo("projectile-hitzone"));
            Assert.That(world.Intents[1].Delivery, Is.EqualTo(AttackDelivery.Projectile));
            Assert.That(world.Intents[1].DeliveryDelaySeconds, Is.EqualTo(0.5f));
            Assert.That(world.Intents[1].MemberOrder, Is.EqualTo(0));
            Assert.That(world.Intents[1].Motion, Is.EqualTo(CombatMotion.Stationary));

            Assert.That(world.Intents[2].SourceCompanionId, Is.EqualTo("area"));
            Assert.That(world.Intents[2].Delivery, Is.EqualTo(AttackDelivery.Area));

            Assert.That(world.Intents[3].SourceCompanionId, Is.EqualTo("spawned"));
            Assert.That(world.Intents[3].Delivery, Is.EqualTo(AttackDelivery.SpawnedActor));

            Assert.That(world.Intents[1].EffectId, Is.EqualTo("projectile-damage"));
            Assert.That(world.Intents[1].SourceMagnitude, Is.EqualTo(11.0f).Within(0.0001f));

            CompanionRunSnapshot afterSecondAdvance = module.CaptureSnapshot();
            Assert.That(afterSecondAdvance.PendingDetachedExecutionCount, Is.EqualTo(0));

            IReadOnlyList<CompanionRunEvent> events = module.DrainEvents();
            Assert.That(events.Count, Is.EqualTo(3));
            Assert.That(events[0].Kind, Is.EqualTo(CompanionRunEventKind.EffectResolved));
            Assert.That(events[1].Kind, Is.EqualTo(CompanionRunEventKind.EffectResolved));
            Assert.That(events[2].Kind, Is.EqualTo(CompanionRunEventKind.EffectResolved));
            bool foundProjectilePresentation = false;
            for (int index = 0; index < events.Count; index += 1)
            {
                CompanionRunEvent evt = events[index];
                if (evt.Kind != CompanionRunEventKind.EffectResolved)
                {
                    continue;
                }

                if (evt.CompanionId == "projectile" && evt.PresentationCue.HasValue)
                {
                    foundProjectilePresentation = true;
                    Assert.That(evt.PresentationCue.Value.PresentationId, Is.EqualTo("projectile-hitzone"));
                }
            }

            Assert.That(foundProjectilePresentation, Is.True);
        }

        [Test]
        public void PauseFreezesSquadAndDetachedProgress()
        {
            FakeRunClock clock = new FakeRunClock();
            FakeCombatWorld world = new FakeCombatWorld(new CompanionPoint(0.0f, 1.0f));
            using CompanionRunModule module = new CompanionRunModule(
                new RunCombatContext(902UL, CreatePauseCatalog(), world, clock));

            Assert.That(
                module.Submit(new CompanionRosterCommand(1L, CompanionRosterCommandKind.Recruit, "projectile")).Accepted,
                Is.True);
            _ = module.DrainEvents();

            Assert.That(module.Advance(new CompanionAdvanceRequest(2L, 10.0f)).Accepted, Is.True);
            CompanionRunSnapshot afterFirstAdvance = module.CaptureSnapshot();
            Assert.That(afterFirstAdvance.Squads.Count, Is.EqualTo(1));
            Assert.That(afterFirstAdvance.PendingDetachedExecutionCount, Is.EqualTo(1));
            float elapsedAtPauseStart = afterFirstAdvance.ElapsedSeconds;
            float cooldownAtPauseStart = afterFirstAdvance.Squads[0].CooldownRemainingSeconds;
            int pendingAtPauseStart = afterFirstAdvance.PendingDetachedExecutionCount;
            int worldResolveCount = world.ResolveResults.Count;

            clock.IsPaused = true;
            Assert.That(
                module.Advance(new CompanionAdvanceRequest(3L, 5.0f)).Accepted,
                Is.True);
            CompanionRunSnapshot duringPause = module.CaptureSnapshot();
            Assert.That(duringPause.ElapsedSeconds, Is.EqualTo(elapsedAtPauseStart));
            Assert.That(duringPause.PendingDetachedExecutionCount, Is.EqualTo(pendingAtPauseStart));
            Assert.That(duringPause.Squads[0].CooldownRemainingSeconds, Is.EqualTo(cooldownAtPauseStart));
            Assert.That(world.ResolveResults.Count, Is.EqualTo(worldResolveCount));

            IReadOnlyList<CompanionRunEvent> pauseEvents = module.DrainEvents();
            Assert.That(pauseEvents.Count, Is.EqualTo(0));

            clock.IsPaused = false;
            Assert.That(module.Advance(new CompanionAdvanceRequest(4L, 1.0f)).Accepted, Is.True);
            Assert.That(world.ResolveResults.Count, Is.EqualTo(worldResolveCount + 1));

            CompanionRunSnapshot afterUnpause = module.CaptureSnapshot();
            Assert.That(afterUnpause.PendingDetachedExecutionCount, Is.EqualTo(0));
        }

        [Test]
        public void ResetClearsPendingExecutionAndRestartsRunIdentity()
        {
            FakeCombatWorld world = new FakeCombatWorld(new CompanionPoint(0.0f, 1.0f));
            using CompanionRunModule module = new CompanionRunModule(new RunCombatContext(903UL, CreateResetCatalog(), world));

            Assert.That(
                module.Submit(new CompanionRosterCommand(1L, CompanionRosterCommandKind.Recruit, "spawned")).Accepted,
                Is.True);
            _ = module.DrainEvents();

            Assert.That(module.Advance(new CompanionAdvanceRequest(1L, 10.0f)).Accepted, Is.True);
            CompanionRunSnapshot queued = module.CaptureSnapshot();
            Assert.That(queued.PendingDetachedExecutionCount, Is.EqualTo(1));
            int beforeResetResolveCount = world.ResolveResults.Count;
            int beforeResetIntentCount = world.Intents.Count;

            module.Reset();
            CompanionRunSnapshot resetSnapshot = module.CaptureSnapshot();
            Assert.That(resetSnapshot.Squads.Count, Is.EqualTo(0));
            Assert.That(resetSnapshot.ElapsedSeconds, Is.EqualTo(0.0f));
            Assert.That(resetSnapshot.PendingDetachedExecutionCount, Is.EqualTo(0));
            Assert.That(resetSnapshot.DroppedChainRequestCount, Is.EqualTo(0));
            Assert.That(resetSnapshot.LastAcceptedCommandSequence, Is.EqualTo(0L));
            Assert.That(resetSnapshot.LastAcceptedAdvanceSequence, Is.EqualTo(0L));
            Assert.That(module.DrainEvents().Count, Is.EqualTo(0));

            Assert.That(
                module.Submit(new CompanionRosterCommand(1L, CompanionRosterCommandKind.Recruit, "direct")).Accepted,
                Is.True);
            CompanionAdvanceResult directAdvance = module.Advance(new CompanionAdvanceRequest(1L, 10.0f));
            Assert.That(directAdvance.EffectsResolved, Is.EqualTo(1));
            Assert.That(world.ResolveResults.Count, Is.EqualTo(beforeResetResolveCount + 1));
            Assert.That(world.Intents.Count, Is.EqualTo(beforeResetIntentCount + 1));
            Assert.That(world.Intents[beforeResetIntentCount].ExecutionSequence, Is.EqualTo(1L));
        }

        [Test]
        public void FollowUpsAreDeferredAndBoundedByChainDepth()
        {
            FakeCombatWorld world = new FakeCombatWorld(new CompanionPoint(0.0f, 1.0f), emitFollowUps: true);
            using CompanionRunModule module = new CompanionRunModule(new RunCombatContext(904UL, CreateFollowUpCatalog(), world));

            Assert.That(
                module.Submit(new CompanionRosterCommand(1L, CompanionRosterCommandKind.Recruit, "direct")).Accepted,
                Is.True);
            _ = module.DrainEvents();

            Assert.That(module.Advance(new CompanionAdvanceRequest(2L, 100.0f)).Accepted, Is.True);
            CompanionRunSnapshot first = module.CaptureSnapshot();
            Assert.That(first.PendingDetachedExecutionCount, Is.EqualTo(1));
            Assert.That(world.Intents.Count, Is.EqualTo(1));
            Assert.That(world.Intents[0].ExecutionSequence, Is.EqualTo(1L));

            Assert.That(module.Advance(new CompanionAdvanceRequest(3L, 1.0f)).Accepted, Is.True);
            Assert.That(world.Intents.Count, Is.EqualTo(2));
            Assert.That(world.Intents[1].ExecutionSequence, Is.EqualTo(2L));
            Assert.That(world.Intents[1].RootExecutionSequence, Is.EqualTo(1L));
            Assert.That(world.Intents[1].ChainDepth, Is.EqualTo(1));

            Assert.That(module.Advance(new CompanionAdvanceRequest(4L, 1.0f)).Accepted, Is.True);
            Assert.That(world.Intents.Count, Is.EqualTo(3));
            Assert.That(world.Intents[2].ExecutionSequence, Is.EqualTo(3L));
            Assert.That(world.Intents[2].RootExecutionSequence, Is.EqualTo(1L));
            Assert.That(world.Intents[2].ChainDepth, Is.EqualTo(2));

            Assert.That(module.Advance(new CompanionAdvanceRequest(5L, 1.0f)).Accepted, Is.True);
            Assert.That(world.Intents.Count, Is.EqualTo(4));
            Assert.That(world.Intents[3].ExecutionSequence, Is.EqualTo(4L));
            Assert.That(world.Intents[3].RootExecutionSequence, Is.EqualTo(1L));
            Assert.That(world.Intents[3].ChainDepth, Is.EqualTo(3));

            CompanionRunSnapshot final = module.CaptureSnapshot();
            Assert.That(final.PendingDetachedExecutionCount, Is.EqualTo(0));
            Assert.That(final.DroppedChainRequestCount, Is.EqualTo(1));

            CompanionAdvanceResult extra = module.Advance(new CompanionAdvanceRequest(6L, 1.0f));
            Assert.That(extra.EffectsResolved, Is.EqualTo(0));
            Assert.That(world.Intents.Count, Is.EqualTo(4));
        }

        private static FakeCatalog CreateDeliveryCatalog()
        {
            ActionStep directStep = new ActionStep(
                CombatMotion.Stationary,
                AttackDelivery.Direct,
                "direct-damage",
                10.0f,
                "direct-arc");
            ActionSet directSet = new ActionSet("direct-set", 10.0f, new[] { directStep });

            ActionStep projectileBase = new ActionStep(
                CombatMotion.Stationary,
                AttackDelivery.Projectile,
                "projectile-damage",
                11.0f,
                "projectile-hitzone",
                0.0f,
                0.0f,
                0.5f);
            ActionStep projectilePromoted = new ActionStep(
                CombatMotion.Stationary,
                AttackDelivery.Projectile,
                "projectile-damage-promoted",
                18.0f,
                "projectile-promoted",
                0.0f,
                0.0f,
                0.5f);
            ActionSet projectileBaseSet = new ActionSet("projectile-base-set", 10.0f, new[] { projectileBase });
            ActionSet projectilePromotedSet = new ActionSet("projectile-promoted-set", 10.0f, new[] { projectilePromoted });

            ActionStep areaStep = new ActionStep(
                CombatMotion.Stationary,
                AttackDelivery.Area,
                "area-shell",
                9.0f,
                "area-impact",
                0.0f,
                0.0f,
                0.5f);
            ActionSet areaSet = new ActionSet("area-set", 10.0f, new[] { areaStep });

            ActionStep spawnedStep = new ActionStep(
                CombatMotion.Stationary,
                AttackDelivery.SpawnedActor,
                "spawn-wave",
                8.0f,
                "spawn-veil",
                0.0f,
                0.0f,
                0.5f);
            ActionSet spawnedSet = new ActionSet("spawn-set", 10.0f, new[] { spawnedStep });

            return new FakeCatalog(
                new CompanionDefinition("direct", directSet),
                new CompanionDefinition("projectile", projectileBaseSet, projectilePromotedSet),
                new CompanionDefinition("area", areaSet),
                new CompanionDefinition("spawned", spawnedSet));
        }

        private static FakeCatalog CreatePauseCatalog()
        {
            ActionStep projectileStep = new ActionStep(
                CombatMotion.Stationary,
                AttackDelivery.Projectile,
                "pause-delay",
                7.0f,
                "pause-cue",
                0.0f,
                0.0f,
                1.0f);
            ActionSet projectileSet = new ActionSet("pause-set", 10.0f, new[] { projectileStep });
            return new FakeCatalog(new CompanionDefinition("projectile", projectileSet));
        }

        private static FakeCatalog CreateResetCatalog()
        {
            ActionStep delayedSpawn = new ActionStep(
                CombatMotion.Stationary,
                AttackDelivery.SpawnedActor,
                "queued-spawn",
                8.0f,
                "queued-cue",
                0.0f,
                0.0f,
                1.0f);
            ActionSet spawnedSet = new ActionSet("reset-spawned-set", 10.0f, new[] { delayedSpawn });

            ActionStep directStep = new ActionStep(
                CombatMotion.Stationary,
                AttackDelivery.Direct,
                "reset-direct",
                10.0f,
                "reset-direct-cue");
            ActionSet directSet = new ActionSet("reset-direct-set", 10.0f, new[] { directStep });

            return new FakeCatalog(
                new CompanionDefinition("spawned", spawnedSet),
                new CompanionDefinition("direct", directSet));
        }

        private static FakeCatalog CreateFollowUpCatalog()
        {
            ActionStep directStep = new ActionStep(
                CombatMotion.Stationary,
                AttackDelivery.Direct,
                "chain-root",
                6.0f,
                "chain-root-cue");
            ActionSet directSet = new ActionSet("followup-set", 100.0f, new[] { directStep });
            return new FakeCatalog(new CompanionDefinition("direct", directSet));
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

        private sealed class FakeRunClock : ICompanionRunClock
        {
            public bool IsPaused { get; set; }
        }

        private sealed class FakeCombatWorld : ICompanionCombatWorld
        {
            private readonly CompanionPoint _targetPosition;
            private readonly Queue<bool> _applyResultSequence;
            private readonly bool _emitFollowUps;
            private int _resolutionCount;

            internal FakeCombatWorld(
                CompanionPoint targetPosition,
                IEnumerable<bool> applyResults = null,
                bool emitFollowUps = false)
            {
                _targetPosition = targetPosition;
                _applyResultSequence = new Queue<bool>(applyResults ?? Array.Empty<bool>());
                _emitFollowUps = emitFollowUps;
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

                IndependentEffectRequest[] followUps = Array.Empty<IndependentEffectRequest>();
                if (_emitFollowUps)
                {
                    followUps = new[]
                    {
                        new IndependentEffectRequest(
                            intent.SquadId,
                            intent.SourceCompanionId,
                            "chain-" + _resolutionCount,
                            intent.SourceMagnitude,
                            intent.TargetPosition,
                            intent.Motion,
                            AttackDelivery.Direct,
                            intent.MemberOrder,
                            intent.PresentationCueId,
                            intent.DeliveryDelaySeconds)
                    };
                }

                _resolutionCount += 1;
                EffectResolution resolution = new EffectResolution(
                    applied,
                    intent.EffectId,
                    intent.SourceMagnitude,
                    applied ? 1 : 0,
                    followUps);
                ResolveResults.Add(resolution);
                return resolution;
            }
        }
    }
}
