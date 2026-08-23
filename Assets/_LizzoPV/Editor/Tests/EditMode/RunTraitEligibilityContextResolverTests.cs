using System;
using System.Reflection;
using Lizzo.PV.Gameplay.RunTraits;
using Lizzo.PV.Legion;
using Lizzo.PV.Legion.Party.Roster;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class RunTraitEligibilityContextResolverTests
    {
        [Test]
        public void Resolve_UsesRosterFamiliesPromotionStateAndPresentationInputs()
        {
            using ServiceTestFixture fixture = new();
            PartyRosterState roster = GetRoster(fixture.Run.Party);
            Assert.AreEqual(PartyRosterChangeResult.Recruit, roster.TryAdd("bombardier"));

            RunTraitEligibilityContext recruitContext = Resolve(
                fixture.Run,
                emergencyRallyActivated: true,
                secondsUntilBossSpawn: 75.0f,
                isPresentationSafe: false);

            Assert.IsTrue(recruitContext.ExplosiveFamilyOwned);
            Assert.IsFalse(recruitContext.HasReadySynergy);
            Assert.IsTrue(recruitContext.HasPromotionOpportunity);
            Assert.IsTrue(recruitContext.EmergencyRallyActivated);
            Assert.AreEqual(75.0f, recruitContext.SecondsUntilBossSpawn);
            Assert.AreEqual(1, recruitContext.ActiveSquadCount);
            Assert.IsFalse(recruitContext.IsPresentationSafe);

            Assert.AreEqual(PartyRosterChangeResult.Reinforce, roster.TryAdd("bombardier"));
            Assert.AreEqual(PartyRosterChangeResult.Promote, roster.TryAdd("bombardier"));
            RunTraitEligibilityContext promotedContext = Resolve(
                fixture.Run,
                emergencyRallyActivated: false,
                secondsUntilBossSpawn: -1.0f,
                isPresentationSafe: true);

            Assert.IsFalse(promotedContext.HasPromotionOpportunity);
            Assert.AreEqual(0.0f, promotedContext.SecondsUntilBossSpawn);
            Assert.IsTrue(promotedContext.IsPresentationSafe);
        }

        static PartyRosterState GetRoster(PartyService party)
        {
            FieldInfo field = typeof(PartyService).GetField("_roster", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, "Missing PartyService roster test field.");
            return (PartyRosterState)field.GetValue(party);
        }

        static RunTraitEligibilityContext Resolve(
            RunServices services,
            bool emergencyRallyActivated,
            float secondsUntilBossSpawn,
            bool isPresentationSafe)
        {
            Type resolver = typeof(RunTraitEligibilityContext).Assembly.GetType(
                "Lizzo.PV.Gameplay.RunTraits.RunTraitEligibilityContextResolver");
            Assert.IsNotNull(resolver, "Missing RunTraitEligibilityContextResolver test type.");
            MethodInfo method = resolver.GetMethod("Resolve", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(method, "Missing run-trait eligibility resolver test method.");
            return (RunTraitEligibilityContext)method.Invoke(
                null,
                new object[] { services, emergencyRallyActivated, secondsUntilBossSpawn, isPresentationSafe });
        }
    }
}
