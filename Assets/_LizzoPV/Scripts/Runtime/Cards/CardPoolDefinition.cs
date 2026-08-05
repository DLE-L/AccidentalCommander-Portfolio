using System;
using UnityEngine;

namespace Lizzo.PV.P0.Cards
{
    public sealed class CardPoolDefinition : ScriptableObject
    {
        [Serializable]
        public sealed class FixedOffer
        {
            [SerializeField]
            private int _levelUpIndex;

            [SerializeField]
            private CardKind[] _cardKinds = Array.Empty<CardKind>();

            public FixedOffer(int levelUpIndex, CardKind[] cardKinds)
            {
                _levelUpIndex = Mathf.Max(1, levelUpIndex);
                _cardKinds = cardKinds ?? Array.Empty<CardKind>();
            }

            public int LevelUpIndex => _levelUpIndex;
            public CardKind[] CardKinds => _cardKinds;
        }

        [SerializeField]
        private int _cardOptionCount = 3;

        [SerializeField]
        private int _fillGuardLimit = 80;

        [SerializeField]
        private int _fullSlotPressureStartOffset = 2;

        [SerializeField]
        private FixedOffer[] _fixedOffers = Array.Empty<FixedOffer>();

        [SerializeField]
        private bool _allowFixedOffersInNormal;

        [SerializeField]
        private CardKind[] _levelFivePlusRandomPool = Array.Empty<CardKind>();

        [SerializeField]
        private CardKind[] _fallbackKinds = Array.Empty<CardKind>();

        [SerializeField]
        private CardKind[] _squadBucket = Array.Empty<CardKind>();

        [SerializeField]
        private CardKind[] _utilityBucket = Array.Empty<CardKind>();

        [SerializeField]
        private CardKind[] _passiveBucketDefault = Array.Empty<CardKind>();

        [SerializeField]
        private CardKind[] _passiveBucketAfterShield = Array.Empty<CardKind>();

        public int CardOptionCount => Mathf.Max(1, _cardOptionCount);
        public int FillGuardLimit => Mathf.Max(1, _fillGuardLimit);
        public int FullSlotPressureStartOffset => Mathf.Max(0, _fullSlotPressureStartOffset);
        public bool AllowFixedOffersInNormal => _allowFixedOffersInNormal;
        public CardKind[] LevelFivePlusRandomPool => _levelFivePlusRandomPool ?? Array.Empty<CardKind>();
        public CardKind[] FallbackKinds => _fallbackKinds ?? Array.Empty<CardKind>();
        public CardKind[] SquadBucket => _squadBucket ?? Array.Empty<CardKind>();
        public CardKind[] UtilityBucket => _utilityBucket ?? Array.Empty<CardKind>();
        public CardKind[] PassiveBucketDefault => _passiveBucketDefault ?? Array.Empty<CardKind>();
        public CardKind[] PassiveBucketAfterShield => _passiveBucketAfterShield ?? Array.Empty<CardKind>();

        public bool TryGetFixedOffer(int levelUpIndex, out CardKind[] cardKinds)
        {
            if (_fixedOffers != null)
            {
                for (int i = 0; i < _fixedOffers.Length; i++)
                {
                    FixedOffer offer = _fixedOffers[i];
                    if (offer != null && offer.LevelUpIndex == levelUpIndex)
                    {
                        cardKinds = offer.CardKinds;
                        return cardKinds != null && cardKinds.Length > 0;
                    }
                }
            }

            cardKinds = Array.Empty<CardKind>();
            return false;
        }

#if UNITY_EDITOR
        public void SetForEditor(
            int cardOptionCount,
            int fillGuardLimit,
            int fullSlotPressureStartOffset,
            FixedOffer[] fixedOffers,
            CardKind[] levelFivePlusRandomPool,
            CardKind[] fallbackKinds,
            CardKind[] squadBucket,
            CardKind[] utilityBucket,
            CardKind[] passiveBucketDefault,
            CardKind[] passiveBucketAfterShield,
            bool allowFixedOffersInNormal = false)
        {
            _cardOptionCount = Mathf.Max(1, cardOptionCount);
            _fillGuardLimit = Mathf.Max(1, fillGuardLimit);
            _fullSlotPressureStartOffset = Mathf.Max(0, fullSlotPressureStartOffset);
            _fixedOffers = fixedOffers ?? Array.Empty<FixedOffer>();
            _allowFixedOffersInNormal = allowFixedOffersInNormal;
            _levelFivePlusRandomPool = levelFivePlusRandomPool ?? Array.Empty<CardKind>();
            _fallbackKinds = fallbackKinds ?? Array.Empty<CardKind>();
            _squadBucket = squadBucket ?? Array.Empty<CardKind>();
            _utilityBucket = utilityBucket ?? Array.Empty<CardKind>();
            _passiveBucketDefault = passiveBucketDefault ?? Array.Empty<CardKind>();
            _passiveBucketAfterShield = passiveBucketAfterShield ?? Array.Empty<CardKind>();
        }
#endif
    }
}
