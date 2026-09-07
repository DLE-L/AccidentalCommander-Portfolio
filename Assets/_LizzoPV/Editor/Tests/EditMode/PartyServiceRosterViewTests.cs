using Lizzo.PV.Legion;
using Lizzo.PV.Legion.Party.Roster;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class PartyServiceRosterIntegrationTests
    {
        [Test]
        public void PartyService_ExposesCurrentRuntimeRosterSnapshotAndPreview()
        {
            using ServiceTestFixture fixture = new ServiceTestFixture();
            PartyService party = fixture.Run.Party;

            Assert.AreEqual(0, party.ActiveCompanionSlotCount);
            Assert.AreEqual(7, party.ActiveCompanionSlotCap);
            Assert.AreEqual(7, party.FreeCompanionSlots);
            Assert.AreEqual(0, party.ActiveCompanionCount);
            Assert.AreEqual(0, party.PromotionReadyCount);
            Assert.AreEqual(7, party.GetSquadSlotSnapshot().Count);
            Assert.AreEqual("squad_00", party.GetSquadSlotSnapshot()[0].SlotId);
            Assert.IsTrue(party.TryGetCanonicalCompanionProgress("shield_guard", out int ownedCount, out int previewCount));
            Assert.AreEqual(0, ownedCount);
            Assert.AreEqual(1, previewCount);
            Assert.IsFalse(party.TryGetCanonicalCompanionProgress("unknown", out _, out _));

            Assert.AreEqual(PartyRosterChangeResult.Recruit, party.PreviewCanonicalRecruit("shield_guard"));
            Assert.AreEqual(PartyRosterChangeResult.Recruit, party.PreviewCanonicalRecruit("sword_soldier"));
            Assert.AreEqual(PartyRosterChangeResult.Recruit, party.PreviewCanonicalRecruit("cleric"));
            Assert.AreEqual(PartyRosterChangeResult.Recruit, party.PreviewCanonicalRecruit("falcon_archer"));
            Assert.AreEqual(PartyRosterChangeResult.RejectedUnknown, party.PreviewCanonicalRecruit("unknown"));
        }
    }
}
