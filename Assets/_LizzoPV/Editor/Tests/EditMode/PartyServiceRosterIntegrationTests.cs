using Lizzo.PV.Legion;
using Lizzo.PV.Legion.Party.Roster;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class PartyServiceRosterIntegrationTests
    {
        [Test]
        public void PartyService_ExposesCanonicalFixedRosterSnapshotAndCompatibilityPreviewMapping()
        {
            using ServiceTestFixture fixture = new ServiceTestFixture();
            PartyService party = fixture.Run.Party;

            Assert.AreEqual(0, party.ActiveCompanionSlotCount);
            Assert.AreEqual(7, party.ActiveCompanionSlotCap);
            Assert.AreEqual(7, party.FreeCompanionSlots);
            Assert.AreEqual(0, party.PromotionReadyCount);
            Assert.AreEqual(7, party.GetSquadSlotSnapshot().Count);
            Assert.AreEqual("squad_00", party.GetSquadSlotSnapshot()[0].SlotId);

            Assert.AreEqual(PartyRosterChangeResult.Recruit, party.PreviewRosterRecruit(CompanionKind.ShieldSoldier));
            Assert.AreEqual(PartyRosterChangeResult.Recruit, party.PreviewRosterRecruit(CompanionKind.Swordsman));
            Assert.AreEqual(PartyRosterChangeResult.Recruit, party.PreviewRosterRecruit(CompanionKind.Cleric));
            Assert.AreEqual(PartyRosterChangeResult.Recruit, party.PreviewRosterRecruit(CompanionKind.Archer));
            Assert.AreEqual(PartyRosterChangeResult.RejectedUnknown, party.PreviewRosterRecruit(CompanionKind.ShieldCaptain));
        }
    }
}
