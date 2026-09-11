using Lizzo.PV.Gameplay.CardOffer;
using Lizzo.PV.Gameplay.CardOffer;
using Lizzo.PV.Gameplay.CardOffer;
using NUnit.Framework;

namespace Lizzo.PV.EditorTests
{
    public sealed class CardOfferSessionContractTests
    {
        CardOfferRuntime _cardOffers;

        [SetUp]
        public void SetUp()
        {
            _cardOffers = new CardOfferRuntime();
            _cardOffers.ResetRunState();
        }

        [TearDown]
        public void TearDown()
        {
            _cardOffers.Dispose();
        }

        [Test]
        public void ResetRunState_ExposesFreshRunState()
        {
            Assert.That(_cardOffers.CurrentLevelUpCount, Is.Zero);
            Assert.That(_cardOffers.RemainingRefreshCount, Is.EqualTo(CardOfferRuntime.MaxRefreshCount));
            Assert.That(_cardOffers.ActiveCardOfferSnapshot, Is.Null);
            Assert.That(_cardOffers.MaxBuildComplete, Is.False);
            Assert.That(_cardOffers.TryRequestBuildCompleteBanner(), Is.False);
        }

        [Test]
        public void RejectedApplication_DoesNotConsumeOfferOrEmitSelection()
        {
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            object session = typeof(CardOfferRuntime).GetField("_session", flags).GetValue(_cardOffers);
            var state = (CardOfferRunState)session.GetType().GetProperty("RunState", flags).GetValue(session);
            var result = DeterministicCardOfferService.Generate(state,
                new[] { new CardOfferCandidate(CardKind.RecruitSwordsman, "sword_soldier", 1f) },
                1, 17UL, CardOfferConfig.Standard, "rejected");
            int selected = 0;
            _cardOffers.Selected += _ => selected++;
            // No companion application dependency is configured, so rejection is intentional.
            Assert.That(_cardOffers.TrySelect(new CardData(CardKind.RecruitSwordsman, "Recruit", "", CardHighlight.None,
                canonicalBaseUnitId: "sword_soldier")), Is.False);
            Assert.That(selected, Is.Zero);
            Assert.That(DeterministicCardOfferService.TryCommitSelection(state, result.Snapshot.OfferIdentity, 0, out _), Is.True,
                "An application rejection must leave the same offer available for retry.");
        }

        [Test]
        public void ConfigureAndClearRun_RoutesConfigThroughRunRuntime()
        {
            _cardOffers.ConfigureCardOfferRun(
                "session-contract",
                0x17EUL,
                new FixedConfigSource(new CardOfferConfig("session_policy", "session_assignment")));

            Assert.That(_cardOffers.CardOfferPolicyVersion, Is.EqualTo("session_policy"));
            Assert.That(_cardOffers.CardOfferConfigAssignmentHash, Is.EqualTo("session_assignment"));

            _cardOffers.ClearServices();

            Assert.That(_cardOffers.CardOfferPolicyVersion, Is.EqualTo("standard_v1"));
            Assert.That(_cardOffers.CardOfferConfigAssignmentHash, Is.EqualTo("local"));
            Assert.That(_cardOffers.ActiveCardOfferSnapshot, Is.Null);
            Assert.That(_cardOffers.MaxBuildComplete, Is.False);
        }

        private sealed class FixedConfigSource : ICardOfferConfigSource
        {
            private readonly CardOfferConfig _config;

            internal FixedConfigSource(CardOfferConfig config)
            {
                _config = config;
            }

            public CardOfferConfig GetCurrent()
            {
                return _config;
            }
        }
    }
}
