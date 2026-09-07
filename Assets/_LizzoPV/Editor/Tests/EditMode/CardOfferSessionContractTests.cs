using Lizzo.PV.Gameplay.CardOffer;
using Lizzo.PV.P0.Cards;
using Lizzo.PV.P0.Cards.CardOffer;
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
        public void ConfigureAndClearRun_RoutesConfigThroughRunRuntime()
        {
            _cardOffers.ConfigureCardOfferRun(
                "session-contract",
                0x17EUL,
                new FixedConfigSource(new CardOfferConfig("session_policy", "session_assignment")));

            Assert.That(_cardOffers.CardOfferPolicyVersion, Is.EqualTo("session_policy"));
            Assert.That(_cardOffers.CardOfferConfigAssignmentHash, Is.EqualTo("session_assignment"));

            _cardOffers.ClearServices();

            Assert.That(_cardOffers.CardOfferPolicyVersion, Is.EqualTo("legacy_compatibility"));
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
