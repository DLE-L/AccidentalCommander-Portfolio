using System;
using Lizzo.PV.Flow;

namespace Lizzo.PV.P0.Cards
{
    internal static class CardOfferPoolResolver
    {
        private const int DefaultCardOptionCount = 3;
        private static CardPoolDefinition _pool;

        private static readonly CardKind[] DefaultLevelFivePlusRandomPool =
        {
            CardKind.SmallHeal,
            CardKind.BasicAttackUp,
            CardKind.AddShieldSoldier,
            CardKind.RecruitArcher,
            CardKind.MoveSpeedUp,
            CardKind.RecruitSwordsman,
            CardKind.LegionBanner,
            CardKind.RecruitCleric,
            CardKind.GuardShockwaveCrest,
        };

        private static readonly CardKind[] DefaultFallbackKinds =
        {
            CardKind.SmallHeal,
            CardKind.BasicAttackUp,
            CardKind.MoveSpeedUp,
            CardKind.LegionBanner,
            CardKind.GuardShockwaveCrest,
        };

        private static readonly CardKind[] DefaultSquadBucket =
        {
            CardKind.AddShieldSoldier,
            CardKind.RecruitSwordsman,
            CardKind.RecruitCleric,
            CardKind.RecruitArcher,
        };

        private static readonly CardKind[] DefaultUtilityBucket =
        {
            CardKind.SmallHeal,
            CardKind.BasicAttackUp,
            CardKind.MoveSpeedUp,
        };

        private static readonly CardKind[] DefaultPassiveBucketDefault =
        {
            CardKind.LegionBanner,
        };

        private static readonly CardKind[] DefaultPassiveBucketAfterShield =
        {
            CardKind.GuardShockwaveCrest,
            CardKind.LegionBanner,
        };

        internal static void Configure(CardPoolDefinition pool)
        {
            _pool = pool ?? throw new ArgumentNullException(nameof(pool));
        }

        internal static void Clear() => _pool = null;

        internal static int CardOptionCount => _pool != null
            ? _pool.CardOptionCount
            : DefaultCardOptionCount;

        internal static CardKind[] LevelFivePlusRandomPool =>
            _pool != null && HasItems(_pool.LevelFivePlusRandomPool)
                ? _pool.LevelFivePlusRandomPool
                : DefaultLevelFivePlusRandomPool;

        internal static CardKind[] FallbackKinds =>
            _pool != null && HasItems(_pool.FallbackKinds)
                ? _pool.FallbackKinds
                : DefaultFallbackKinds;

        internal static CardKind[] SquadBucket =>
            _pool != null && HasItems(_pool.SquadBucket)
                ? _pool.SquadBucket
                : DefaultSquadBucket;

        internal static CardKind[] UtilityBucket =>
            _pool != null && HasItems(_pool.UtilityBucket)
                ? _pool.UtilityBucket
                : DefaultUtilityBucket;

        internal static CardKind[] PassiveBucketDefault =>
            _pool != null && HasItems(_pool.PassiveBucketDefault)
                ? _pool.PassiveBucketDefault
                : DefaultPassiveBucketDefault;

        internal static CardKind[] PassiveBucketAfterShield =>
            _pool != null && HasItems(_pool.PassiveBucketAfterShield)
                ? _pool.PassiveBucketAfterShield
                : DefaultPassiveBucketAfterShield;

        internal static bool TryGetFixedOffer(int levelUpIndex, out CardKind[] fixedOffer)
        {
            if (_pool != null)
                return _pool.TryGetFixedOffer(levelUpIndex, out fixedOffer);

            fixedOffer = levelUpIndex switch
            {
                1 => new[] { CardKind.AddShieldSoldier, CardKind.SmallHeal, CardKind.BasicAttackUp },
                2 => new[] { CardKind.AddShieldSoldier, CardKind.RecruitArcher, CardKind.MoveSpeedUp },
                3 => new[] { CardKind.AddShieldSoldier, CardKind.RecruitSwordsman, CardKind.LegionBanner },
                4 => new[] { CardKind.RecruitCleric, CardKind.RecruitSwordsman, CardKind.GuardShockwaveCrest },
                _ => Array.Empty<CardKind>(),
            };
            return fixedOffer.Length > 0;
        }

        internal static bool ShouldUseFixedOffers(RunDefinition definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));
            if (definition.UsesGuidedCardOffers)
                return true;

            return _pool != null && _pool.AllowFixedOffersInNormal;
        }

        internal static bool IsCompanionCardAllowed(CardKind kind) =>
            _pool == null || _pool.IsCompanionCardAllowed(kind);

        internal static bool IsCurrentProductCardAvailable(CardKind kind)
        {
            return kind != CardKind.BasicAttackUp
                && kind != CardKind.LegionBanner;
        }

        private static bool HasItems(CardKind[] items)
        {
            return items != null && items.Length > 0;
        }
    }
}
