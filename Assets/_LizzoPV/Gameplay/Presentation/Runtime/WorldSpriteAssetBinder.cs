using Lizzo.PV.Presentation;
using UnityEngine;

namespace Lizzo.PV.Gameplay.PresentationRuntime
{
    [DisallowMultipleComponent]
    public sealed class WorldSpriteAssetBinder : MonoBehaviour
    {
        [SerializeField] private string _gameDataId;
        [SerializeField] private SpriteAssetId _spriteId;
        [SerializeField] private SpriteRenderer _target;

        public string GameDataId => _gameDataId;
        public SpriteAssetId SpriteId => _spriteId;

        private void Awake()
        {
            AssetCatalogBundleRuntime catalogs = AppBootstrap.Instance?.Services?.AssetCatalogs;
            if (catalogs == null || _target == null)
            {
                Debug.LogError($"[WorldSpriteAssetBinder] Required Sprite binding is missing: {_gameDataId}/{_spriteId.Value}.", this);
                enabled = false;
                return;
            }

            // Empty art slots are supported; never retain a previous pooled sprite.
            catalogs.SpriteCatalog.TryGet(_spriteId, out Sprite sprite);
            _target.sprite = sprite;
        }

#if UNITY_EDITOR
        public void SetForEditor(string gameDataId, SpriteAssetId spriteId, SpriteRenderer target)
        {
            _gameDataId = gameDataId;
            _spriteId = spriteId;
            _target = target;
        }
#endif
    }
}
