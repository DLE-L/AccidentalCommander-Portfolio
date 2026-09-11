using Lizzo.PV.Legion;
using Lizzo.PV.Legion.RunCore;
using NUnit.Framework;
using UnityEngine;

namespace Lizzo.PV.EditorTests
{
    public sealed class CompanionSanctuaryConnectionTests
    {
        private sealed class Clock : ICompanionRunClock { public bool Paused; public bool IsPaused => Paused; }
        private sealed class World : ICompanionCombatWorld
        {
            public int Hits;
            public bool TrySelectTargetPosition(out CompanionPoint point) { point = new CompanionPoint(1, 0); return true; }
            public EffectResolution Resolve(in EffectIntent intent) { Hits++; return new EffectResolution(true, intent.EffectId, 1, 1); }
        }
        private sealed class Catalog : ICompanionDefinitionCatalog
        {
            private readonly float _cooldown, _duration;
            public Catalog(float cooldown = 10, float duration = 0) { _cooldown = cooldown; _duration = duration; }
            public bool TryGetDefinition(string id, out CompanionDefinition definition)
            {
                var set = new ActionSet(id, _cooldown, new[] { new ActionStep(CombatMotion.Stationary,
                    AttackDelivery.Direct, "test-hit", 1, "test-hit", _duration, 0) });
                definition = new CompanionDefinition(id, set); return true;
            }
        }

        [Test]
        public void CommanderInside_AcceleratesEverySquadEvenWhenTheirAnchorsAreOutside()
        {
            var sanctuary = new CompanionSanctuaryRuntimeState();
            sanctuary.Begin(Vector3.zero, 0, new CompanionSanctuarySetup("test", 3, .01f, 4, 1.25f));
            using var module = new CompanionRunModule(new RunCombatContext(1, new Catalog(), new World(), new Clock(), null,
                position => sanctuary.GetAttackIntervalDivisor(new Vector3(position.X, position.Y), 0)));
            module.Submit(new CompanionRosterCommand(1, CompanionRosterCommandKind.Recruit, "cleric"));
            module.Submit(new CompanionRosterCommand(2, CompanionRosterCommandKind.Recruit, "sword_soldier"));
            module.Advance(new CompanionAdvanceRequest(1, .4f, CompanionPoint.Zero));
            foreach (var squad in module.CaptureSnapshot().Squads)
            {
                Assert.That(squad.CooldownRemainingSeconds, Is.EqualTo(9.5f).Within(.0001f));
                Assert.That(sanctuary.GetAttackIntervalDivisor(new Vector3(squad.FormationAnchor.X, squad.FormationAnchor.Y), 0), Is.EqualTo(1f));
            }
            module.Advance(new CompanionAdvanceRequest(2, .4f, new CompanionPoint(10, 0)));
            foreach (var squad in module.CaptureSnapshot().Squads)
                Assert.That(squad.CooldownRemainingSeconds, Is.EqualTo(9.1f).Within(.0001f));
        }

        [Test]
        public void ExpiryResetAndPause_DoNotLeaveAccelerationBehind()
        {
            var sanctuary = new CompanionSanctuaryRuntimeState(); var clock = new Clock(); float now = 0;
            var setup = new CompanionSanctuarySetup("test", 3, 2.5f, 4, 1.25f);
            sanctuary.Begin(Vector3.zero, now, setup);
            using var module = new CompanionRunModule(new RunCombatContext(1, new Catalog(), new World(), clock, null,
                position => sanctuary.GetAttackIntervalDivisor(new Vector3(position.X, position.Y), now)));
            module.Submit(new CompanionRosterCommand(1, CompanionRosterCommandKind.Recruit, "cleric"));
            module.Advance(new CompanionAdvanceRequest(1, .4f));
            clock.Paused = true; module.Advance(new CompanionAdvanceRequest(2, 1f)); clock.Paused = false;
            Assert.That(module.CaptureSnapshot().Squads[0].CooldownRemainingSeconds, Is.EqualTo(9.5f).Within(.0001f));
            now = 4; module.Advance(new CompanionAdvanceRequest(3, .4f));
            Assert.That(module.CaptureSnapshot().Squads[0].CooldownRemainingSeconds, Is.EqualTo(9.1f).Within(.0001f));
            sanctuary.Begin(Vector3.zero, now, setup); sanctuary.Reset();
            module.Advance(new CompanionAdvanceRequest(4, .4f));
            Assert.That(module.CaptureSnapshot().Squads[0].CooldownRemainingSeconds, Is.EqualTo(8.7f).Within(.0001f));
        }

        [Test]
        public void AcceleratedCooldownOverflow_LeavesActionDurationInRealSeconds()
        {
            var world = new World();
            using var module = new CompanionRunModule(new RunCombatContext(1, new Catalog(.5f, .08f), world, new Clock(), null, _ => 2f));
            module.Submit(new CompanionRosterCommand(1, CompanionRosterCommandKind.Recruit, "cleric"));
            module.Advance(new CompanionAdvanceRequest(1, .3f));
            Assert.That(world.Hits, Is.EqualTo(0), "Only .05 real seconds remain after the cooldown.");
            module.Advance(new CompanionAdvanceRequest(2, .031f));
            Assert.That(world.Hits, Is.EqualTo(1));
        }
    }
}
