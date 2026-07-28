using Lizzo.PV.Legion;
using Lizzo.PV.Legion.Party.Roster;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class CanonicalHerbalistRuntimeTracerTests
    {
        [Test]
        public void RuntimeSpec_ResolvesBaseAndPromotionWithoutLegacyUnitData()
        {
            using ServiceTestFixture fixture = new ServiceTestFixture();

            Assert.IsTrue(CompanionRuntimeSpec.TryCreate(fixture.Data, "field_herbalist", false, out CompanionRuntimeSpec baseSpec));
            Assert.AreEqual("field_herbalist", baseSpec.BaseUnitId);
            Assert.AreEqual("field_herbalist", baseSpec.PresentedUnitId);
            Assert.AreEqual(50, baseSpec.BaseHp);
            Assert.AreEqual(2.8f, baseSpec.MoveSpeed, 0.0001f);
            Assert.IsFalse(baseSpec.IsPromoted);

            Assert.IsTrue(CompanionRuntimeSpec.TryCreate(fixture.Data, "field_herbalist", true, out CompanionRuntimeSpec promotedSpec));
            Assert.AreEqual("field_herbalist", promotedSpec.BaseUnitId);
            Assert.AreEqual("battle_apothecary", promotedSpec.PresentedUnitId);
            Assert.AreEqual(fixture.Data.GetCompanionPromotion("battle_apothecary").DisplayName, promotedSpec.DisplayName);
            Assert.IsTrue(promotedSpec.IsPromoted);
            Assert.IsFalse(CompanionRuntimeSpec.TryCreate(fixture.Data, "battle_apothecary", false, out _));
        }

        [Test]
        public void CanonicalPreview_RejectsUnknownAndAcceptsHerbalistWithoutMutatingRoster()
        {
            using ServiceTestFixture fixture = new ServiceTestFixture();
            PartyService party = fixture.Run.Party;

            Assert.AreEqual(PartyRosterChangeResult.RejectedUnknown, party.PreviewCanonicalRecruit("battle_apothecary"));
            Assert.AreEqual(PartyRosterChangeResult.RejectedUnknown, party.PreviewCanonicalRecruit("unknown"));
            Assert.AreEqual(PartyRosterChangeResult.Recruit, party.PreviewCanonicalRecruit("field_herbalist"));
            Assert.IsTrue(party.CanRecruitCanonicalWithinSlotCap("field_herbalist"));
            Assert.AreEqual(0, party.ActiveCompanionSlotCount);
        }
    }
}
