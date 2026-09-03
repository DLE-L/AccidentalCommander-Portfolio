using System.Collections.Generic;
using Lizzo.PV.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace Lizzo.PV.EditorTests
{
    public sealed class AssetCatalogBundleRuntimeTests
    {
        private readonly List<Object> _ownedObjects = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int index = _ownedObjects.Count - 1; index >= 0; index--)
            {
                if (_ownedObjects[index] != null)
                {
                    Object.DestroyImmediate(_ownedObjects[index]);
                }
            }

            _ownedObjects.Clear();
        }

        [Test]
        public void Acquire_ActivatesAllCatalogKindsFromOneBundle()
        {
            Sprite sprite = CreateSprite();
            AudioClip audioClip = Own(AudioClip.Create("Bgm", 16, 1, 8000, false));
            GameObject vfxPrefab = Own(new GameObject("BurstVfx"));
            AnimationClip motionClip = Own(new AnimationClip { name = "Bounce" });
            SpriteCatalogSO sprites = CreateSpriteCatalog(1, "Portrait", sprite);
            AudioCatalogSO audio = CreateAudioCatalog(2, "LobbyBgm", audioClip);
            VfxCatalogSO vfx = CreateVfxCatalog(3, "Burst", vfxPrefab);
            MotionCatalogSO motion = CreateMotionCatalog(4, "Bounce", motionClip);
            AssetCatalogBundleSO bundle = CreateBundle("Lobby", sprites, audio, vfx, motion);
            var runtime = new AssetCatalogBundleRuntime();

            bool acquired = runtime.Acquire(bundle, out AssetCatalogBundleLease lease, out string issue);

            Assert.That(acquired, Is.True, issue);
            Assert.That(runtime.ActiveBundleCount, Is.EqualTo(1));
            Assert.That(runtime.SpriteCatalog.GetRequired(new SpriteAssetId(1)), Is.SameAs(sprite));
            Assert.That(runtime.AudioCatalog.GetRequired(new AudioAssetId(2)), Is.SameAs(audioClip));
            Assert.That(runtime.VfxCatalog.GetRequired(new VfxAssetId(3)), Is.SameAs(vfxPrefab));
            Assert.That(runtime.MotionCatalog.GetRequired(new MotionAssetId(4)), Is.SameAs(motionClip));
            lease.Dispose();
        }

        [Test]
        public void AcquireSameBundle_ReferenceCountsUntilLastLeaseIsReleased()
        {
            Sprite sprite = CreateSprite();
            SpriteCatalogSO sprites = CreateSpriteCatalog(10, "Icon", sprite);
            AssetCatalogBundleSO bundle = CreateBundle("Shared", sprites, null, null, null);
            var runtime = new AssetCatalogBundleRuntime();

            Assert.That(runtime.Acquire(bundle, out AssetCatalogBundleLease first, out string firstIssue), Is.True, firstIssue);
            Assert.That(runtime.Acquire(bundle, out AssetCatalogBundleLease second, out string secondIssue), Is.True, secondIssue);
            Assert.That(runtime.GetReferenceCount(bundle), Is.EqualTo(2));

            first.Dispose();
            Assert.That(runtime.GetReferenceCount(bundle), Is.EqualTo(1));
            Assert.That(runtime.SpriteCatalog.TryGet(new SpriteAssetId(10), out _), Is.True);

            second.Dispose();
            Assert.That(runtime.GetReferenceCount(bundle), Is.Zero);
            Assert.That(runtime.ActiveBundleCount, Is.Zero);
            Assert.That(runtime.SpriteCatalog.TryGet(new SpriteAssetId(10), out _), Is.False);
        }

        [Test]
        public void Acquire_SharedCatalogAcrossBundlesIsMergedOnce()
        {
            SpriteCatalogSO sharedCatalog = CreateSpriteCatalog(20, "SharedIcon", CreateSprite());
            AssetCatalogBundleSO lobby = CreateBundle("Lobby", sharedCatalog, null, null, null);
            AssetCatalogBundleSO gameplay = CreateBundle("Gameplay", sharedCatalog, null, null, null);
            var runtime = new AssetCatalogBundleRuntime();

            Assert.That(runtime.Acquire(lobby, out AssetCatalogBundleLease lobbyLease, out string lobbyIssue), Is.True, lobbyIssue);
            Assert.That(runtime.Acquire(gameplay, out AssetCatalogBundleLease gameplayLease, out string gameplayIssue), Is.True, gameplayIssue);
            Assert.That(runtime.ActiveBundleCount, Is.EqualTo(2));
            Assert.That(runtime.SpriteCatalog.Count, Is.EqualTo(1));

            lobbyLease.Dispose();
            Assert.That(runtime.SpriteCatalog.TryGet(new SpriteAssetId(20), out _), Is.True);
            gameplayLease.Dispose();
        }

        [Test]
        public void Acquire_CrossTypeIdConflictFailsWithoutReplacingActiveCatalogs()
        {
            Sprite sprite = CreateSprite();
            SpriteCatalogSO sprites = CreateSpriteCatalog(30, "ActiveIcon", sprite);
            AssetCatalogBundleSO active = CreateBundle("Active", sprites, null, null, null);
            AudioCatalogSO conflictingAudio = CreateAudioCatalog(
                30,
                "ConflictingSfx",
                Own(AudioClip.Create("Conflict", 16, 1, 8000, false)));
            AssetCatalogBundleSO conflict = CreateBundle("Conflict", null, conflictingAudio, null, null);
            var runtime = new AssetCatalogBundleRuntime();
            Assert.That(runtime.Acquire(active, out AssetCatalogBundleLease activeLease, out string activeIssue), Is.True, activeIssue);

            bool acquired = runtime.Acquire(conflict, out AssetCatalogBundleLease conflictLease, out string issue);

            Assert.That(acquired, Is.False);
            Assert.That(conflictLease, Is.Null);
            Assert.That(issue, Does.Contain("Asset ID 30"));
            Assert.That(runtime.ActiveBundleCount, Is.EqualTo(1));
            Assert.That(runtime.SpriteCatalog.GetRequired(new SpriteAssetId(30)), Is.SameAs(sprite));
            activeLease.Dispose();
        }

        [Test]
        public void Acquire_DuplicateBundleKeyFailsWithoutChangingReferenceCounts()
        {
            AssetCatalogBundleSO firstBundle = CreateBundle(
                "Gameplay", CreateSpriteCatalog(40, "First", CreateSprite()), null, null, null);
            AssetCatalogBundleSO duplicateKeyBundle = CreateBundle(
                "Gameplay", CreateSpriteCatalog(41, "Second", CreateSprite()), null, null, null);
            var runtime = new AssetCatalogBundleRuntime();
            Assert.That(runtime.Acquire(firstBundle, out AssetCatalogBundleLease firstLease, out string firstIssue), Is.True, firstIssue);

            bool acquired = runtime.Acquire(duplicateKeyBundle, out AssetCatalogBundleLease duplicateLease, out string issue);

            Assert.That(acquired, Is.False);
            Assert.That(duplicateLease, Is.Null);
            Assert.That(issue, Does.Contain("Bundle Key 'Gameplay'"));
            Assert.That(runtime.GetReferenceCount(firstBundle), Is.EqualTo(1));
            Assert.That(runtime.GetReferenceCount(duplicateKeyBundle), Is.Zero);
            firstLease.Dispose();
        }

        private AssetCatalogBundleSO CreateBundle(
            string key,
            SpriteCatalogSO sprites,
            AudioCatalogSO audio,
            VfxCatalogSO vfx,
            MotionCatalogSO motion)
        {
            AssetCatalogBundleSO bundle = Own(ScriptableObject.CreateInstance<AssetCatalogBundleSO>());
            bundle.SetForEditor(
                key,
                sprites == null ? null : new[] { sprites },
                audio == null ? null : new[] { audio },
                vfx == null ? null : new[] { vfx },
                motion == null ? null : new[] { motion });
            return bundle;
        }

        private SpriteCatalogSO CreateSpriteCatalog(int id, string name, Sprite asset)
        {
            SpriteCatalogSO catalog = Own(ScriptableObject.CreateInstance<SpriteCatalogSO>());
            catalog.SetEntriesForEditor(new SpriteCatalogEntry(new SpriteAssetId(id), name, name, asset, SpriteCategory.Icon));
            return catalog;
        }

        private AudioCatalogSO CreateAudioCatalog(int id, string name, AudioClip asset)
        {
            AudioCatalogSO catalog = Own(ScriptableObject.CreateInstance<AudioCatalogSO>());
            catalog.SetEntriesForEditor(new AudioCatalogEntry(new AudioAssetId(id), name, name, asset, AudioKind.UiSfx, false, null));
            return catalog;
        }

        private VfxCatalogSO CreateVfxCatalog(int id, string name, GameObject asset)
        {
            VfxCatalogSO catalog = Own(ScriptableObject.CreateInstance<VfxCatalogSO>());
            catalog.SetEntriesForEditor(new VfxCatalogEntry(new VfxAssetId(id), name, name, asset, VfxPlaybackKind.OneShot));
            return catalog;
        }

        private MotionCatalogSO CreateMotionCatalog(int id, string name, AnimationClip asset)
        {
            MotionCatalogSO catalog = Own(ScriptableObject.CreateInstance<MotionCatalogSO>());
            catalog.SetEntriesForEditor(new MotionCatalogEntry(new MotionAssetId(id), name, name, asset, MotionPlaybackKind.OneShot));
            return catalog;
        }

        private Sprite CreateSprite()
        {
            Texture2D texture = Own(new Texture2D(2, 2));
            return Own(Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), Vector2.one * 0.5f));
        }

        private T Own<T>(T value) where T : Object
        {
            _ownedObjects.Add(value);
            return value;
        }
    }
}
