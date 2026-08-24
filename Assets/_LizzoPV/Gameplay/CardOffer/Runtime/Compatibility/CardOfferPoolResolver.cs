using System;
using Lizzo.PV.Flow;

namespace Lizzo.PV.P0.Cards
{
    internal static class CardOfferPoolResolver
    {
        private const int DefaultCardOptionCount = 3;

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

        internal static int CardOptionCount => CardCatalogProvider.TryGetPool(out CardPoolDefinition pool)
            ? pool.CardOptionCount
            : DefaultCardOptionCount;

        internal static CardKind[] LevelFivePlusRandomPool =>
            CardCatalogProvider.TryGetPool(out CardPoolDefinition pool)
            && HasItems(pool.LevelFivePlusRandomPool)
                ? pool.LevelFivePlusRandomPool
                : DefaultLevelFivePlusRandomPool;

        internal static CardKind[] FallbackKinds =>
            CardCatalogProvider.TryGetPool(out CardPoolDefinition pool)
            && HasItems(pool.FallbackKinds)
                ? pool.FallbackKinds
                : DefaultFallbackKinds;

        internal static CardKind[] SquadBucket =>
            CardCatalogProvider.TryGetPool(out CardPoolDefinition pool)
            && HasItems(pool.SquadBucket)
                ? pool.SquadBucket
                : DefaultSquadBucket;

        internal static CardKind[] UtilityBucket =>
            CardCatalogProvider.TryGetPool(out CardPoolDefinition pool)
            && HasItems(pool.UtilityBucket)
                ? pool.UtilityBucket
                : DefaultUtilityBucket;

        internal static CardKind[] PassiveBucketDefault =>
            CardCatalogProvider.TryGetPool(out CardPoolDefinition pool)
            && HasItems(pool.PassiveBucketDefault)
                ? pool.PassiveBucketDefault
                : DefaultPassiveBucketDefault;

        internal static CardKind[] PassiveBucketAfterShield =>
            CardCatalogProvider.TryGetPool(out CardPoolDefinition pool)
            && HasItems(pool.PassiveBucketAfterShield)
                ? pool.PassiveBucketAfterShield
                : DefaultPassiveBucketAfterShield;

        internal static bool TryGetFixedOffer(int levelUpIndex, out CardKind[] fixedOffer)
        {
            if (CardCatalogProvider.TryGetPool(out CardPoolDefinition pool))
                return pool.TryGetFixedOffer(levelUpIndex, out fixedOffer);

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

        internal static bool ShouldUseFixedOffers(RunContext context)
        {
            if (context.IsTutorial)
                return true;

            return context.IsNormal
                && CardCatalogProvider.TryGetPool(out CardPoolDefinition pool)
                && pool.AllowFixedOffersInNormal;
        }

        private static bool HasItems(CardKind[] items)
        {
            return items != null && items.Length > 0;
        }
    }
}
