using System;
using Lizzo.PV.Editor.Presentation;
using Lizzo.PV.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace Lizzo.PV.EditorTests
{
    public sealed class AssetCatalogIdReassignTests
    {
        private SpriteCatalogSO _spriteCatalog;
        private StartLoadingPresentationProfileSO _loadingProfile;
        private AudioCatalogSO _audioCatalog;

        [TearDown]
        public void TearDown()
        {
            if (_spriteCatalog != null)
            {
                UnityEngine.Object.DestroyImmediate(_spriteCatalog);
            }

            if (_loadingProfile != null)
            {
                UnityEngine.Object.DestroyImmediate(_loadingProfile);
            }

            if (_audioCatalog != null)
            {
                UnityEngine.Object.DestroyImmediate(_audioCatalog);
            }
        }

        [Test]
        public void Reassign_UpdatesCatalogAndEveryMatchingProfileReference()
        {
            _spriteCatalog = ScriptableObject.CreateInstance<SpriteCatalogSO>();
            _spriteCatalog.SetEntriesForEditor(new SpriteCatalogEntry(
                new SpriteAssetId(10), "LoadingBackground", "Loading background.", null, SpriteCategory.Background));
            _loadingProfile = CreateLoadingProfile(new SpriteAssetId(10));

            bool changed = AssetCatalogIdReassignService.TryReassignObjects(
                AssetCatalogKind.Sprite,
                10,
                100,
                new UnityEngine.Object[] { _spriteCatalog, _loadingProfile },
                out AssetIdReassignResult result,
                out string issue);

            Assert.That(changed, Is.True, issue);
            Assert.That(result.ChangedAssetCount, Is.EqualTo(2));
            Assert.That(result.ChangedPropertyCount, Is.EqualTo(2));
            Assert.That(_spriteCatalog.Entries[0].Id.Value, Is.EqualTo(100));
            Assert.That(_loadingProfile.BackgroundSpriteId.Value, Is.EqualTo(100));
            Assert.That(_loadingProfile.BarTrackSpriteId.Value, Is.EqualTo(11));
        }

        [Test]
        public void Reassign_NewIdCollisionAcrossAnotherAssetTypeStopsBeforeMutation()
        {
            _spriteCatalog = ScriptableObject.CreateInstance<SpriteCatalogSO>();
            _spriteCatalog.SetEntriesForEditor(new SpriteCatalogEntry(
                new SpriteAssetId(10), "LoadingBackground", "Loading background.", null, SpriteCategory.Background));
            _loadingProfile = CreateLoadingProfile(new SpriteAssetId(10));
            _audioCatalog = ScriptableObject.CreateInstance<AudioCatalogSO>();
            _audioCatalog.SetEntriesForEditor(new AudioCatalogEntry(
                new AudioAssetId(100), "Click", "Short click.", null, AudioKind.UiSfx, false, null));

            bool changed = AssetCatalogIdReassignService.TryReassignObjects(
                AssetCatalogKind.Sprite,
                10,
                100,
                new UnityEngine.Object[] { _spriteCatalog, _loadingProfile, _audioCatalog },
                out AssetIdReassignResult result,
                out string issue);

            Assert.That(changed, Is.False);
            Assert.That(result.ChangedPropertyCount, Is.Zero);
            Assert.That(issue, Does.Contain("already referenced"));
            Assert.That(_spriteCatalog.Entries[0].Id.Value, Is.EqualTo(10));
            Assert.That(_loadingProfile.BackgroundSpriteId.Value, Is.EqualTo(10));
        }

        [Test]
        public void Reassign_RejectsInvalidOrMissingSourceWithoutMutation()
        {
            _loadingProfile = CreateLoadingProfile(new SpriteAssetId(10));

            Assert.That(
                AssetCatalogIdReassignService.TryReassignObjects(
                    AssetCatalogKind.Sprite,
                    0,
                    20,
                    new UnityEngine.Object[] { _loadingProfile },
                    out _,
                    out string invalidIssue),
                Is.False);
            Assert.That(invalidIssue, Does.Contain("positive"));

            Assert.That(
                AssetCatalogIdReassignService.TryReassignObjects(
                    AssetCatalogKind.Sprite,
                    99,
                    20,
                    new UnityEngine.Object[] { _loadingProfile },
                    out _,
                    out string missingIssue),
                Is.False);
            Assert.That(missingIssue, Does.Contain("No SpriteAssetId reference"));
            Assert.That(_loadingProfile.BackgroundSpriteId.Value, Is.EqualTo(10));
        }

        private StartLoadingPresentationProfileSO CreateLoadingProfile(SpriteAssetId backgroundId)
        {
            StartLoadingPresentationProfileSO profile =
                ScriptableObject.CreateInstance<StartLoadingPresentationProfileSO>();
            profile.SetForEditor(
                backgroundId,
                new SpriteAssetId(11),
                new SpriteAssetId(12),
                new AudioAssetId(13),
                new MotionAssetId(14),
                new MotionAssetId(15),
                new MotionAssetId(16),
                0.5f,
                8f,
                0.1f);
            return profile;
        }
    }
}
