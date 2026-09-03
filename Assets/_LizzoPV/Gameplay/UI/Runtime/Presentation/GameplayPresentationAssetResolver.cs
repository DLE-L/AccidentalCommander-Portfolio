using Lizzo.PV.Presentation;
using UnityEngine;

namespace Lizzo.PV.Gameplay.PresentationRuntime
{
    internal readonly struct GameplayPresentationAssetResolver
    {
        private readonly AssetCatalogBundleRuntime _catalogs;

        public GameplayPresentationAssetResolver(AssetCatalogBundleRuntime catalogs) => _catalogs = catalogs;

        public bool TrySprite(SpriteAssetId id, string field, out Sprite value, out string issue)
        {
            value = null;
            if (id.IsNone) { issue = string.Empty; return true; }
            if (_catalogs != null && _catalogs.SpriteCatalog.TryGet(id, out value)) { issue = string.Empty; return true; }
            issue = $"{field} references missing Sprite Asset ID {id.Value}.";
            return false;
        }

        public bool TryAudio(AudioAssetId id, string field, out AudioClip value, out string issue)
        {
            value = null;
            if (id.IsNone) { issue = string.Empty; return true; }
            if (_catalogs != null && _catalogs.AudioCatalog.TryGet(id, out value)) { issue = string.Empty; return true; }
            issue = $"{field} references missing Audio Asset ID {id.Value}.";
            return false;
        }

        public bool TryMotion(MotionAssetId id, string field, out AnimationClip value, out string issue)
        {
            value = null;
            if (id.IsNone) { issue = string.Empty; return true; }
            if (_catalogs != null && _catalogs.MotionCatalog.TryGet(id, out value)) { issue = string.Empty; return true; }
            issue = $"{field} references missing Motion Asset ID {id.Value}.";
            return false;
        }
    }
}
