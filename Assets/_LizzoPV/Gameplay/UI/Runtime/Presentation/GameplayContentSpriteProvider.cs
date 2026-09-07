using Lizzo.PV.Presentation;
using UnityEngine;

namespace Lizzo.PV.Gameplay
{
    public static class GameplayContentSpriteProvider
    {
        private static GameplayContentSpriteProfileSO _profile;
        private static SpriteCatalogRuntime _catalog;

        public static bool Configure(GameplayContentSpriteProfileSO profile, AssetCatalogBundleRuntime catalogs, out string issue)
        {
            if (profile == null || catalogs == null)
            {
                issue = "Content Sprite Profile and Asset Catalogs are required.";
                return false;
            }
            if (!profile.TryValidate(out issue))
                return false;

            _profile = profile;
            _catalog = catalogs.SpriteCatalog;
            issue = string.Empty;
            return true;
        }

        public static void Clear(GameplayContentSpriteProfileSO owner)
        {
            if (_profile != owner)
                return;
            _profile = null;
            _catalog = null;
        }

        public static bool TryCardPortrait(string gameDataId, out Sprite sprite)
        {
            if (TryResolve(_profile != null ? _profile.TryGetCardPortrait : null, gameDataId, out sprite))
                return true;

            string fallbackId = ResolvePassiveIconFallback(gameDataId);
            return string.IsNullOrEmpty(fallbackId) == false
                && TryResolve(_profile != null ? _profile.TryGetCardPortrait : null, fallbackId, out sprite);
        }

        public static bool TryCardSynergy(string cardGameDataId, out string synergyId, out Sprite sprite)
        {
            sprite = null;
            if (_profile == null || _catalog == null
                || !_profile.TryGetCardSynergy(cardGameDataId, out synergyId, out SpriteAssetId id))
            {
                synergyId = string.Empty;
                return false;
            }
            return _catalog.TryGet(id, out sprite);
        }

        public static bool TryNotificationSynergyIcon(string synergyId, out Sprite sprite) =>
            TryResolve(_profile != null ? _profile.TryGetNotificationSynergyIcon : null, synergyId, out sprite);
        public static bool TryBuildSummaryCompanionIcon(string companionId, out Sprite sprite) =>
            TryResolve(_profile != null ? _profile.TryGetBuildSummaryCompanionIcon : null, companionId, out sprite);
        public static bool TryBuildSummaryPassiveIcon(string passiveId, out Sprite sprite)
        {
            if (TryResolve(_profile != null ? _profile.TryGetBuildSummaryPassiveIcon : null, passiveId, out sprite))
                return true;

            string fallbackId = ResolvePassiveIconFallback(passiveId);
            return string.IsNullOrEmpty(fallbackId) == false
                && TryResolve(_profile != null ? _profile.TryGetBuildSummaryPassiveIcon : null, fallbackId, out sprite);
        }
        public static bool TryBuildSummarySynergyIcon(string synergyId, out Sprite sprite) =>
            TryResolve(_profile != null ? _profile.TryGetBuildSummarySynergyIcon : null, synergyId, out sprite);
        public static bool TryRewardIcon(string rewardId, out Sprite sprite) =>
            TryResolve(_profile != null ? _profile.TryGetRewardIcon : null, rewardId, out sprite);

        private delegate bool TryGetId(string gameDataId, out SpriteAssetId id);

        private static string ResolvePassiveIconFallback(string passiveId)
        {
            if (Lizzo.PV.Gameplay.CardOffer.CompanionPassiveCatalog.TryGet(passiveId, out Lizzo.PV.Gameplay.CardOffer.CompanionPassiveCatalogEntry entry) == false)
                return null;

            if (entry.IsCommon)
                return "passive_standard_bearer";

            return entry.RequiredLineageId switch
            {
                "sword_soldier" or "wraith_knight" => "passive_sword_greatsword",
                "shield_guard" => "passive_shield_wide_strike",
                "falcon_archer" or "bombardier" or "skeleton_scythe_thrower" => "passive_archer_multi_shot",
                "cleric" or "field_herbalist" => "passive_cleric_full_prayer",
                _ => "passive_standard_bearer",
            };
        }

        private static bool TryResolve(TryGetId getId, string gameDataId, out Sprite sprite)
        {
            sprite = null;
            return getId != null && _catalog != null && getId(gameDataId, out SpriteAssetId id) && _catalog.TryGet(id, out sprite);
        }
    }
}
