using Lizzo.PV.P0.Cards;
using Lizzo.PV.P0.Cards.CardOffer;
using NUnit.Framework;

namespace Lizzo.PV.EditorTests
{
    public sealed class CardOfferSessionContractTests
    {
        [SetUp]
        public void SetUp()
        {
            FixedCardPool.ClearServices();
            FixedCardPool.ResetRunState();
        }

        [TearDown]
        public void TearDown()
        {
            FixedCardPool.ClearServices();
        }

        [Test]
        public void ResetRunState_ExposesFreshCompatibilityState()
        {
            Assert.That(FixedCardPool.CurrentLevelUpCount, Is.Zero);
            Assert.That(FixedCardPool.RemainingRefreshCount, Is.EqualTo(FixedCardPool.MaxRefreshCount));
            Assert.That(FixedCardPool.ActiveCardOfferSnapshot, Is.Null);
            Assert.That(FixedCardPool.MaxBuildComplete, Is.False);
            Assert.That(FixedCardPool.TryRequestBuildCompleteBanner(), Is.False);
        }

        [Test]
        public void ConfigureAndClearRun_RoutesConfigThroughCompatibilityFacade()
        {
            FixedCardPool.ConfigureCardOfferRun(
                "session-contract",
                0x17EUL,
                new FixedConfigSource(new CardOfferConfig("session_policy", "session_assignment")));

            Assert.That(FixedCardPool.CardOfferPolicyVersion, Is.EqualTo("session_policy"));
            Assert.That(FixedCardPool.CardOfferConfigAssignmentHash, Is.EqualTo("session_assignment"));

            FixedCardPool.ClearServices();

            Assert.That(FixedCardPool.CardOfferPolicyVersion, Is.EqualTo("legacy_compatibility"));
            Assert.That(FixedCardPool.CardOfferConfigAssignmentHash, Is.EqualTo("local"));
            Assert.That(FixedCardPool.ActiveCardOfferSnapshot, Is.Null);
            Assert.That(FixedCardPool.MaxBuildComplete, Is.False);
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
