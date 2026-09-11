using System;
using System.Collections.Generic;
using Lizzo.PV.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace Lizzo.PV.EditorTests
{
    public sealed class AssetCatalogCoreTests
    {
        readonly List<UnityEngine.Object> _ownedObjects = new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            for (int index = _ownedObjects.Count - 1; index >= 0; index--)
                UnityEngine.Object.DestroyImmediate(_ownedObjects[index]);
            _ownedObjects.Clear();
        }

        [Test]
        public void TypedIds_KeepNoneAndTypeBoundariesExplicit()
        {
            Assert.That(SpriteAssetId.None.IsNone, Is.True);
            Assert.That(new SpriteAssetId(12).Value, Is.EqualTo(12));
            Assert.That(new SpriteAssetId(12), Is.EqualTo(new SpriteAssetId(12)));
            Assert.That(new AudioAssetId(12), Is.Not.EqualTo((object)new SpriteAssetId(12)));
            Assert.That(typeof(SpriteAssetId).GetMethod("op_Implicit"), Is.Null);
            Assert.That(typeof(AudioAssetId).GetMethod("op_Implicit"), Is.Null);
            Assert.That(typeof(VfxAssetId).GetMethod("op_Implicit"), Is.Null);
            Assert.That(typeof(MotionAssetId).GetMethod("op_Implicit"), Is.Null);
        }

        [Test]
        public void SpriteRuntime_ResolvesRequiredAndOptionalAssets()
        {
            Sprite sprite = Own(Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.zero));
            SpriteCatalogSO catalog = Own(ScriptableObject.CreateInstance<SpriteCatalogSO>());
            catalog.SetEntriesForEditor(new SpriteCatalogEntry(
                new SpriteAssetId(1), "WhitePixel", "Single white pixel sprite.", sprite, SpriteCategory.Icon));
            SpriteCatalogRuntime runtime = new SpriteCatalogRuntime();

            Assert.That(runtime.Initialize(new[] { catalog }, out string issue), Is.True, issue);
            Assert.That(runtime.TryGet(SpriteAssetId.None, out _), Is.False);
            Assert.That(runtime.GetRequired(new SpriteAssetId(1)), Is.SameAs(sprite));
            Assert.Throws<KeyNotFoundException>(() => runtime.GetRequired(new SpriteAssetId(2)));
        }

        [Test]
        public void AudioRuntime_RejectsDuplicateIdsAndPreservesPreviousValidState()
        {
            AudioClip clip = Own(AudioClip.Create("click", 16, 1, 8000, false));
            AudioCatalogSO first = Own(ScriptableObject.CreateInstance<AudioCatalogSO>());
            AudioCatalogSO second = Own(ScriptableObject.CreateInstance<AudioCatalogSO>());
            first.SetEntriesForEditor(AudioEntry(11, "ClickA", clip));
            second.SetEntriesForEditor(AudioEntry(11, "ClickB", clip));
            AudioCatalogRuntime runtime = new AudioCatalogRuntime();

            Assert.That(runtime.Initialize(new[] { first }, out string issue), Is.True, issue);
            Assert.That(runtime.Initialize(new[] { first, second }, out issue), Is.False);
            StringAssert.Contains("Duplicate Audio Asset ID 11", issue);
            Assert.That(runtime.Count, Is.EqualTo(1));
            Assert.That(runtime.GetRequired(new AudioAssetId(11)), Is.SameAs(clip));
        }

        [Test]
        public void VfxRuntime_ReservesEmptySlotButRejectsNonPositiveId()
        {
            VfxCatalogSO missingAsset = Own(ScriptableObject.CreateInstance<VfxCatalogSO>());
            missingAsset.SetEntriesForEditor(new VfxCatalogEntry(
                new VfxAssetId(7), "HitBurst", "Short impact burst.", null, VfxPlaybackKind.OneShot));
            VfxCatalogRuntime runtime = new VfxCatalogRuntime();
            Assert.That(runtime.Initialize(new[] { missingAsset }, out string issue), Is.True);
            Assert.That(runtime.Count, Is.EqualTo(1));

            GameObject prefab = Own(new GameObject("HitBurst"));
            VfxCatalogSO invalidId = Own(ScriptableObject.CreateInstance<VfxCatalogSO>());
            invalidId.SetEntriesForEditor(new VfxCatalogEntry(
                VfxAssetId.None, "HitBurst", "Short impact burst.", prefab, VfxPlaybackKind.OneShot));
            Assert.That(runtime.Initialize(new[] { invalidId }, out issue), Is.False);
            StringAssert.Contains("invalid Asset ID 0", issue);
        }

        [Test]
        public void MotionRuntime_ResolvesAnimationClipAndReservesEmptySlot()
        {
            AnimationClip clip = Own(new AnimationClip());
            MotionCatalogSO missing = Own(ScriptableObject.CreateInstance<MotionCatalogSO>());
            missing.SetEntriesForEditor(new MotionCatalogEntry(
                new MotionAssetId(21), "Enter", "Panel enter motion.", null, MotionPlaybackKind.OneShot));
            MotionCatalogRuntime runtime = new MotionCatalogRuntime();

            Assert.That(runtime.Initialize(new[] { missing }, out string issue), Is.True);
            Assert.That(runtime.Count, Is.EqualTo(1));

            MotionCatalogSO valid = Own(ScriptableObject.CreateInstance<MotionCatalogSO>());
            valid.SetEntriesForEditor(new MotionCatalogEntry(
                new MotionAssetId(21), "Enter", "Panel enter motion.", clip, MotionPlaybackKind.OneShot));
            Assert.That(runtime.Initialize(new[] { valid }, out issue), Is.True, issue);
            Assert.That(runtime.GetRequired(new MotionAssetId(21)), Is.SameAs(clip));
            Assert.Throws<InvalidOperationException>(() => runtime.GetRequired(MotionAssetId.None));
        }

        T Own<T>(T value) where T : UnityEngine.Object
        {
            _ownedObjects.Add(value);
            return value;
        }

        static AudioCatalogEntry AudioEntry(int id, string name, AudioClip clip)
        {
            return new AudioCatalogEntry(
                new AudioAssetId(id), name, "Short UI click.", clip, AudioKind.UiSfx, false, null);
        }

    }
}
