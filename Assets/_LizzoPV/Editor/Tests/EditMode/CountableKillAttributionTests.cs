using NUnit.Framework;
using Lizzo.PV.Combat;
using Lizzo.PV.Flow;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class CountableKillAttributionTests
    {
        [Test]
        public void Attribution_RequiresExplicitCompanionOwnedOwnerAndSource()
        {
            Assert.IsTrue(new CountableKillAttribution(101, "necromancer", CombatKillSourceCategory.CompanionOwnedAction).IsCountable);
            Assert.IsFalse(new CountableKillAttribution(0, "necromancer", CombatKillSourceCategory.CompanionOwnedAction).IsCountable);
            Assert.IsFalse(new CountableKillAttribution(101, string.Empty, CombatKillSourceCategory.CompanionOwnedAction).IsCountable);
            Assert.IsFalse(new CountableKillAttribution(101, "necromancer", CombatKillSourceCategory.PersonalSummon).IsCountable);
            Assert.IsFalse(new CountableKillAttribution(101, "necromancer", CombatKillSourceCategory.SynergySummon).IsCountable);
            Assert.IsFalse(default(CountableKillAttribution).IsCountable);
        }

        [Test]
        public void RunState_EmitsOneExplicitOwnerAttributionWithoutChangingGlobalKillCount()
        {
            RunState state = new RunState();
            CountableKillAttribution received = default;
            int events = 0;
            state.CountableKillAttributed += attribution =>
            {
                received = attribution;
                events++;
            };

            state.Reset(1);
            state.MarkLoaded();
            state.RegisterKill();
            state.RegisterCountableKill(new CountableKillAttribution(101, "necromancer", CombatKillSourceCategory.CompanionOwnedAction));

            Assert.AreEqual(1, state.KillCount);
            Assert.AreEqual(1, events);
            Assert.AreEqual(101, received.OwnerInstanceId);
            Assert.AreEqual("necromancer", received.SourceId);
            Assert.AreEqual(CombatKillSourceCategory.CompanionOwnedAction, received.Category);
        }

        [Test]
        public void RunState_SeparatesOwnersAndRejectsExcludedOrResetStaleAttribution()
        {
            RunState state = new RunState();
            int events = 0;
            int lastOwner = 0;
            state.CountableKillAttributed += attribution =>
            {
                events++;
                lastOwner = attribution.OwnerInstanceId;
            };

            state.Reset(1);
            state.MarkLoaded();
            state.RegisterCountableKill(new CountableKillAttribution(101, "necromancer", CombatKillSourceCategory.CompanionOwnedAction));
            state.RegisterCountableKill(new CountableKillAttribution(202, "necromancer", CombatKillSourceCategory.CompanionOwnedAction));
            state.RegisterCountableKill(new CountableKillAttribution(202, "personal_skeleton", CombatKillSourceCategory.PersonalSummon));
            state.RegisterCountableKill(new CountableKillAttribution(202, "synergy_skeleton", CombatKillSourceCategory.SynergySummon));
            state.RegisterCountableKill(default);

            Assert.AreEqual(2, events);
            Assert.AreEqual(202, lastOwner);

            state.Reset(1);
            state.MarkLoaded();
            state.RegisterCountableKill(default);
            Assert.AreEqual(2, events);
        }
    }
}
