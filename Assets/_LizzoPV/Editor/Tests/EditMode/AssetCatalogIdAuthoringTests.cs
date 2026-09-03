using System;
using System.Collections.Generic;
using Lizzo.PV.Editor.Presentation;
using Lizzo.PV.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace Lizzo.PV.EditorTests
{
    public sealed class AssetCatalogIdAuthoringTests
    {
        private readonly List<UnityEngine.Object> _ownedObjects = new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            for (int i = _ownedObjects.Count - 1; i >= 0; i--)
            {
                if (_ownedObjects[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(_ownedObjects[i]);
                }
            }

            _ownedObjects.Clear();
        }

        [Test]
        public void Build_DetectsDuplicateIdsAcrossCatalogTypes()
        {
            SpriteCatalogSO sprites = Own(ScriptableObject.CreateInstance<SpriteCatalogSO>());
            sprites.SetEntriesForEditor(new SpriteCatalogEntry(
                new SpriteAssetId(41), "StageIcon", "Stage icon.", null, SpriteCategory.Icon));
            AudioCatalogSO audio = Own(ScriptableObject.CreateInstance<AudioCatalogSO>());
            audio.SetEntriesForEditor(new AudioCatalogEntry(
                new AudioAssetId(41), "LobbyBgm", "Lobby BGM.", null, AudioKind.Bgm, true, null));

            AssetCatalogIdReport report = AssetCatalogIdIndex.Build(
                new[] { sprites },
                new[] { audio },
                Array.Empty<VfxCatalogSO>(),
                Array.Empty<MotionCatalogSO>());

            Assert.That(report.IsValid, Is.False);
            Assert.That(report.Issues.Count, Is.EqualTo(1));
            Assert.That(report.Issues[0].Kind, Is.EqualTo(AssetCatalogIdIssueKind.Duplicate));
            Assert.That(report.Issues[0].Id, Is.EqualTo(41));
            Assert.That(report.Issues[0].Records.Count, Is.EqualTo(2));
        }

        [Test]
        public void Build_ReportsNonPositiveIdsAndKeepsNextIdAboveMaximum()
        {
            VfxCatalogSO vfx = Own(ScriptableObject.CreateInstance<VfxCatalogSO>());
            vfx.SetEntriesForEditor(
                new VfxCatalogEntry(new VfxAssetId(0), "Missing", "Invalid none ID.", null, VfxPlaybackKind.OneShot),
                new VfxCatalogEntry(new VfxAssetId(-3), "Negative", "Invalid negative ID.", null, VfxPlaybackKind.OneShot),
                new VfxCatalogEntry(new VfxAssetId(9), "Hit", "Valid hit VFX.", null, VfxPlaybackKind.OneShot));

            AssetCatalogIdReport report = AssetCatalogIdIndex.Build(
                Array.Empty<SpriteCatalogSO>(),
                Array.Empty<AudioCatalogSO>(),
                new[] { vfx },
                Array.Empty<MotionCatalogSO>());

            Assert.That(report.IsValid, Is.False);
            Assert.That(report.Issues.Count, Is.EqualTo(2));
            Assert.That(report.NextAvailableId, Is.EqualTo(10));
        }

        [Test]
        public void Build_UsesOneGlobalSequenceAcrossAllFourCatalogs()
        {
            SpriteCatalogSO sprites = Own(ScriptableObject.CreateInstance<SpriteCatalogSO>());
            sprites.SetEntriesForEditor(new SpriteCatalogEntry(
                new SpriteAssetId(2), "Portrait", "Portrait.", null, SpriteCategory.Portrait));
            AudioCatalogSO audio = Own(ScriptableObject.CreateInstance<AudioCatalogSO>());
            audio.SetEntriesForEditor(new AudioCatalogEntry(
                new AudioAssetId(5), "Click", "Click.", null, AudioKind.UiSfx, false, null));
            VfxCatalogSO vfx = Own(ScriptableObject.CreateInstance<VfxCatalogSO>());
            vfx.SetEntriesForEditor(new VfxCatalogEntry(
                new VfxAssetId(12), "Burst", "Burst.", null, VfxPlaybackKind.OneShot));
            MotionCatalogSO motion = Own(ScriptableObject.CreateInstance<MotionCatalogSO>());
            motion.SetEntriesForEditor(new MotionCatalogEntry(
                new MotionAssetId(8), "Bounce", "Bounce.", null, MotionPlaybackKind.OneShot));

            AssetCatalogIdReport report = AssetCatalogIdIndex.Build(
                new[] { sprites }, new[] { audio }, new[] { vfx }, new[] { motion });

            Assert.That(report.IsValid, Is.True);
            Assert.That(report.Records.Count, Is.EqualTo(4));
            Assert.That(report.NextAvailableId, Is.EqualTo(13));
        }

        [Test]
        public void Build_EmptyCatalogSetStartsAtOne()
        {
            AssetCatalogIdReport report = AssetCatalogIdIndex.Build(
                Array.Empty<SpriteCatalogSO>(),
                Array.Empty<AudioCatalogSO>(),
                Array.Empty<VfxCatalogSO>(),
                Array.Empty<MotionCatalogSO>());

            Assert.That(report.IsValid, Is.True);
            Assert.That(report.NextAvailableId, Is.EqualTo(1));
        }

        [Test]
        public void Build_IntRangeExhaustionIsExplicit()
        {
            AudioCatalogSO audio = Own(ScriptableObject.CreateInstance<AudioCatalogSO>());
            audio.SetEntriesForEditor(new AudioCatalogEntry(
                new AudioAssetId(int.MaxValue), "Last", "Last positive int ID.", null, AudioKind.Stinger, false, null));

            AssetCatalogIdReport report = AssetCatalogIdIndex.Build(
                Array.Empty<SpriteCatalogSO>(),
                new[] { audio },
                Array.Empty<VfxCatalogSO>(),
                Array.Empty<MotionCatalogSO>());

            Assert.That(report.IsValid, Is.False);
            Assert.That(report.NextAvailableId, Is.Zero);
            Assert.That(report.Issues[0].Kind, Is.EqualTo(AssetCatalogIdIssueKind.Exhausted));
        }

        private T Own<T>(T value) where T : UnityEngine.Object
        {
            _ownedObjects.Add(value);
            return value;
        }
    }
}
