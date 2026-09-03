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

        public static bool TryCardPortrait(string gameDataId, out Sprite sprite) =>
            TryResolve(_profile != null ? _profile.TryGetCardPortrait : null, gameDataId, out sprite);

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
        public static bool TryBuildSummaryPassiveIcon(string passiveId, out Sprite sprite) =>
            TryResolve(_profile != null ? _profile.TryGetBuildSummaryPassiveIcon : null, passiveId, out sprite);
        public static bool TryBuildSummarySynergyIcon(string synergyId, out Sprite sprite) =>
            TryResolve(_profile != null ? _profile.TryGetBuildSummarySynergyIcon : null, synergyId, out sprite);
        public static bool TryRewardIcon(string rewardId, out Sprite sprite) =>
            TryResolve(_profile != null ? _profile.TryGetRewardIcon : null, rewardId, out sprite);

        private delegate bool TryGetId(string gameDataId, out SpriteAssetId id);

        private static bool TryResolve(TryGetId getId, string gameDataId, out Sprite sprite)
        {
            sprite = null;
            return getId != null && _catalog != null && getId(gameDataId, out SpriteAssetId id) && _catalog.TryGet(id, out sprite);
        }
    }
}
