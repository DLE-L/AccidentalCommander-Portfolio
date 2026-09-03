using System;
using System.Collections.Generic;

namespace Lizzo.PV.Presentation
{
    public sealed class AssetCatalogBundleLease : IDisposable
    {
        private AssetCatalogBundleRuntime _owner;
        private AssetCatalogBundleSO _bundle;

        internal AssetCatalogBundleLease(AssetCatalogBundleRuntime owner, AssetCatalogBundleSO bundle)
        {
            _owner = owner;
            _bundle = bundle;
        }

        public bool IsReleased => _owner == null;

        public void Dispose()
        {
            if (_owner == null)
            {
                return;
            }

            if (!_owner.Release(_bundle, out string issue))
            {
                UnityEngine.Debug.LogError($"[AssetCatalogBundleLease] Release failed: {issue}");
                return;
            }

            _owner = null;
            _bundle = null;
        }
    }

    public sealed class AssetCatalogBundleRuntime
    {
        private readonly Dictionary<AssetCatalogBundleSO, int> _referenceCounts =
            new Dictionary<AssetCatalogBundleSO, int>();
        private readonly Dictionary<string, AssetCatalogBundleSO> _bundlesByKey =
            new Dictionary<string, AssetCatalogBundleSO>(StringComparer.Ordinal);

        public AssetCatalogBundleRuntime()
        {
            SpriteCatalog = new SpriteCatalogRuntime();
            AudioCatalog = new AudioCatalogRuntime();
            VfxCatalog = new VfxCatalogRuntime();
            MotionCatalog = new MotionCatalogRuntime();
        }

        public SpriteCatalogRuntime SpriteCatalog { get; private set; }
        public AudioCatalogRuntime AudioCatalog { get; private set; }
        public VfxCatalogRuntime VfxCatalog { get; private set; }
        public MotionCatalogRuntime MotionCatalog { get; private set; }
        public int ActiveBundleCount => _referenceCounts.Count;

        public bool Acquire(AssetCatalogBundleSO bundle, out AssetCatalogBundleLease lease, out string issue)
        {
            lease = null;
            if (bundle == null)
            {
                issue = "Asset catalog bundle is null.";
                return false;
            }

            if (_referenceCounts.TryGetValue(bundle, out int referenceCount))
            {
                _referenceCounts[bundle] = referenceCount + 1;
                lease = new AssetCatalogBundleLease(this, bundle);
                issue = string.Empty;
                return true;
            }

            if (string.IsNullOrWhiteSpace(bundle.BundleKey))
            {
                issue = "Asset catalog bundle requires a Bundle Key.";
                return false;
            }

            if (_bundlesByKey.TryGetValue(bundle.BundleKey, out AssetCatalogBundleSO existingBundle)
                && existingBundle != bundle)
            {
                issue = $"Asset catalog Bundle Key '{bundle.BundleKey}' is already active on another bundle.";
                return false;
            }

            var candidateBundles = new List<AssetCatalogBundleSO>(_referenceCounts.Count + 1);
            candidateBundles.AddRange(_referenceCounts.Keys);
            candidateBundles.Add(bundle);

            if (!TryBuild(candidateBundles, out CatalogSet candidate, out issue))
            {
                return false;
            }

            _referenceCounts.Add(bundle, 1);
            _bundlesByKey.Add(bundle.BundleKey, bundle);
            Apply(candidate);
            lease = new AssetCatalogBundleLease(this, bundle);
            issue = string.Empty;
            return true;
        }

        public bool Release(AssetCatalogBundleSO bundle, out string issue)
        {
            if (bundle == null || !_referenceCounts.TryGetValue(bundle, out int referenceCount))
            {
                issue = "Asset catalog bundle is not active.";
                return false;
            }

            if (referenceCount > 1)
            {
                _referenceCounts[bundle] = referenceCount - 1;
                issue = string.Empty;
                return true;
            }

            var candidateBundles = new List<AssetCatalogBundleSO>(_referenceCounts.Count - 1);
            foreach (AssetCatalogBundleSO activeBundle in _referenceCounts.Keys)
            {
                if (activeBundle != bundle)
                {
                    candidateBundles.Add(activeBundle);
                }
            }

            if (!TryBuild(candidateBundles, out CatalogSet candidate, out issue))
            {
                return false;
            }

            _referenceCounts.Remove(bundle);
            _bundlesByKey.Remove(bundle.BundleKey);
            Apply(candidate);
            issue = string.Empty;
            return true;
        }

        public int GetReferenceCount(AssetCatalogBundleSO bundle)
        {
            return bundle != null && _referenceCounts.TryGetValue(bundle, out int count) ? count : 0;
        }

        private void Apply(CatalogSet candidate)
        {
            SpriteCatalog = candidate.Sprites;
            AudioCatalog = candidate.Audio;
            VfxCatalog = candidate.Vfx;
            MotionCatalog = candidate.Motion;
        }

        private static bool TryBuild(IReadOnlyList<AssetCatalogBundleSO> bundles, out CatalogSet result, out string issue)
        {
            var spriteCatalogs = new List<SpriteCatalogSO>();
            var audioCatalogs = new List<AudioCatalogSO>();
            var vfxCatalogs = new List<VfxCatalogSO>();
            var motionCatalogs = new List<MotionCatalogSO>();
            var seenSpriteCatalogs = new HashSet<SpriteCatalogSO>();
            var seenAudioCatalogs = new HashSet<AudioCatalogSO>();
            var seenVfxCatalogs = new HashSet<VfxCatalogSO>();
            var seenMotionCatalogs = new HashSet<MotionCatalogSO>();

            for (int index = 0; index < bundles.Count; index++)
            {
                AssetCatalogBundleSO bundle = bundles[index];
                if (bundle == null)
                {
                    result = default;
                    issue = "Active asset catalog bundle is null.";
                    return false;
                }

                if (!AppendUnique(bundle.SpriteCatalogs, seenSpriteCatalogs, spriteCatalogs, "Sprite", bundle.BundleKey, out issue)
                    || !AppendUnique(bundle.AudioCatalogs, seenAudioCatalogs, audioCatalogs, "Audio", bundle.BundleKey, out issue)
                    || !AppendUnique(bundle.VfxCatalogs, seenVfxCatalogs, vfxCatalogs, "VFX", bundle.BundleKey, out issue)
                    || !AppendUnique(bundle.MotionCatalogs, seenMotionCatalogs, motionCatalogs, "Motion", bundle.BundleKey, out issue))
                {
                    result = default;
                    return false;
                }
            }

            if (!ValidateGlobalIds(spriteCatalogs, audioCatalogs, vfxCatalogs, motionCatalogs, out issue))
            {
                result = default;
                return false;
            }

            var sprites = new SpriteCatalogRuntime();
            var audio = new AudioCatalogRuntime();
            var vfx = new VfxCatalogRuntime();
            var motion = new MotionCatalogRuntime();
            if (!sprites.Initialize(spriteCatalogs, out issue)
                || !audio.Initialize(audioCatalogs, out issue)
                || !vfx.Initialize(vfxCatalogs, out issue)
                || !motion.Initialize(motionCatalogs, out issue))
            {
                result = default;
                return false;
            }

            result = new CatalogSet(sprites, audio, vfx, motion);
            issue = string.Empty;
            return true;
        }

        private static bool AppendUnique<TCatalog>(
            IReadOnlyList<TCatalog> source,
            HashSet<TCatalog> seen,
            List<TCatalog> destination,
            string catalogKind,
            string bundleKey,
            out string issue)
            where TCatalog : UnityEngine.Object
        {
            if (source == null)
            {
                issue = $"Bundle '{bundleKey}' has a null {catalogKind} catalog collection.";
                return false;
            }

            for (int index = 0; index < source.Count; index++)
            {
                TCatalog catalog = source[index];
                if (catalog == null)
                {
                    issue = $"Bundle '{bundleKey}' has a null {catalogKind} catalog at index {index}.";
                    return false;
                }

                if (seen.Add(catalog))
                {
                    destination.Add(catalog);
                }
            }

            issue = string.Empty;
            return true;
        }

        private static bool ValidateGlobalIds(
            IReadOnlyList<SpriteCatalogSO> spriteCatalogs,
            IReadOnlyList<AudioCatalogSO> audioCatalogs,
            IReadOnlyList<VfxCatalogSO> vfxCatalogs,
            IReadOnlyList<MotionCatalogSO> motionCatalogs,
            out string issue)
        {
            var owners = new Dictionary<int, string>();
            for (int catalogIndex = 0; catalogIndex < spriteCatalogs.Count; catalogIndex++)
            {
                IReadOnlyList<SpriteCatalogEntry> entries = spriteCatalogs[catalogIndex].Entries;
                for (int entryIndex = 0; entryIndex < entries.Count; entryIndex++)
                {
                    SpriteCatalogEntry entry = entries[entryIndex];
                    if (entry != null && !TryAddId(owners, entry.Id.Value, "Sprite", entry.Name, out issue))
                    {
                        return false;
                    }
                }
            }

            for (int catalogIndex = 0; catalogIndex < audioCatalogs.Count; catalogIndex++)
            {
                IReadOnlyList<AudioCatalogEntry> entries = audioCatalogs[catalogIndex].Entries;
                for (int entryIndex = 0; entryIndex < entries.Count; entryIndex++)
                {
                    AudioCatalogEntry entry = entries[entryIndex];
                    if (entry != null && !TryAddId(owners, entry.Id.Value, "Audio", entry.Name, out issue))
                    {
                        return false;
                    }
                }
            }

            for (int catalogIndex = 0; catalogIndex < vfxCatalogs.Count; catalogIndex++)
            {
                IReadOnlyList<VfxCatalogEntry> entries = vfxCatalogs[catalogIndex].Entries;
                for (int entryIndex = 0; entryIndex < entries.Count; entryIndex++)
                {
                    VfxCatalogEntry entry = entries[entryIndex];
                    if (entry != null && !TryAddId(owners, entry.Id.Value, "VFX", entry.Name, out issue))
                    {
                        return false;
                    }
                }
            }

            for (int catalogIndex = 0; catalogIndex < motionCatalogs.Count; catalogIndex++)
            {
                IReadOnlyList<MotionCatalogEntry> entries = motionCatalogs[catalogIndex].Entries;
                for (int entryIndex = 0; entryIndex < entries.Count; entryIndex++)
                {
                    MotionCatalogEntry entry = entries[entryIndex];
                    if (entry != null && !TryAddId(owners, entry.Id.Value, "Motion", entry.Name, out issue))
                    {
                        return false;
                    }
                }
            }

            issue = string.Empty;
            return true;
        }

        private static bool TryAddId(Dictionary<int, string> owners, int id, string kind, string name, out string issue)
        {
            string owner = $"{kind}:{name}";
            if (id > 0 && owners.TryAdd(id, owner))
            {
                issue = string.Empty;
                return true;
            }

            if (id <= 0)
            {
                issue = $"{owner} has invalid Asset ID {id}.";
                return false;
            }

            issue = $"Asset ID {id} is shared by {owners[id]} and {owner}.";
            return false;
        }

        private readonly struct CatalogSet
        {
            public CatalogSet(
                SpriteCatalogRuntime sprites,
                AudioCatalogRuntime audio,
                VfxCatalogRuntime vfx,
                MotionCatalogRuntime motion)
            {
                Sprites = sprites;
                Audio = audio;
                Vfx = vfx;
                Motion = motion;
            }

            public SpriteCatalogRuntime Sprites { get; }
            public AudioCatalogRuntime Audio { get; }
            public VfxCatalogRuntime Vfx { get; }
            public MotionCatalogRuntime Motion { get; }
        }
    }
}
