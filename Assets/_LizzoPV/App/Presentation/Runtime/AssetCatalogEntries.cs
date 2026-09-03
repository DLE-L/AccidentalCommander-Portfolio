using System;
using UnityEngine;
using UnityEngine.Audio;

namespace Lizzo.PV.Presentation
{
    public enum SpriteCategory
    {
        Icon,
        Portrait,
        Background,
        Overlay,
        World,
    }

    public enum AudioKind
    {
        Bgm,
        Ambience,
        Stinger,
        UiSfx,
        WorldSfx,
    }

    public enum VfxPlaybackKind
    {
        OneShot,
        Loop,
    }

    public enum MotionPlaybackKind
    {
        OneShot,
        Loop,
        State,
    }

    [Serializable]
    public sealed class SpriteCatalogEntry
    {
        [SerializeField] private SpriteAssetId _id;
        [SerializeField] private string _name;
        [SerializeField] private string _description;
        [SerializeField] private Sprite _asset;
        [SerializeField] private SpriteCategory _category;

        public SpriteCatalogEntry(SpriteAssetId id, string name, string description, Sprite asset, SpriteCategory category)
        {
            _id = id;
            _name = name;
            _description = description;
            _asset = asset;
            _category = category;
        }

        public SpriteAssetId Id => _id;
        public string Name => _name;
        public string Description => _description;
        public Sprite Asset => _asset;
        public SpriteCategory Category => _category;
    }

    [Serializable]
    public sealed class AudioCatalogEntry
    {
        [SerializeField] private AudioAssetId _id;
        [SerializeField] private string _name;
        [SerializeField] private string _description;
        [SerializeField] private AudioClip _asset;
        [SerializeField] private AudioKind _kind;
        [SerializeField] private bool _loop;
        [SerializeField] private AudioMixerGroup _defaultMixerBus;

        public AudioCatalogEntry(
            AudioAssetId id,
            string name,
            string description,
            AudioClip asset,
            AudioKind kind,
            bool loop,
            AudioMixerGroup defaultMixerBus)
        {
            _id = id;
            _name = name;
            _description = description;
            _asset = asset;
            _kind = kind;
            _loop = loop;
            _defaultMixerBus = defaultMixerBus;
        }

        public AudioAssetId Id => _id;
        public string Name => _name;
        public string Description => _description;
        public AudioClip Asset => _asset;
        public AudioKind Kind => _kind;
        public bool Loop => _loop;
        public AudioMixerGroup DefaultMixerBus => _defaultMixerBus;
    }

    [Serializable]
    public sealed class VfxCatalogEntry
    {
        [SerializeField] private VfxAssetId _id;
        [SerializeField] private string _name;
        [SerializeField] private string _description;
        [SerializeField] private GameObject _asset;
        [SerializeField] private VfxPlaybackKind _playbackKind;

        public VfxCatalogEntry(VfxAssetId id, string name, string description, GameObject asset, VfxPlaybackKind playbackKind)
        {
            _id = id;
            _name = name;
            _description = description;
            _asset = asset;
            _playbackKind = playbackKind;
        }

        public VfxAssetId Id => _id;
        public string Name => _name;
        public string Description => _description;
        public GameObject Asset => _asset;
        public VfxPlaybackKind PlaybackKind => _playbackKind;
    }

    [Serializable]
    public sealed class MotionCatalogEntry
    {
        [SerializeField] private MotionAssetId _id;
        [SerializeField] private string _name;
        [SerializeField] private string _description;
        [SerializeField] private AnimationClip _animationClip;
        [SerializeField] private MotionPlaybackKind _playbackKind;

        public MotionCatalogEntry(
            MotionAssetId id,
            string name,
            string description,
            AnimationClip animationClip,
            MotionPlaybackKind playbackKind)
        {
            _id = id;
            _name = name;
            _description = description;
            _animationClip = animationClip;
            _playbackKind = playbackKind;
        }

        public MotionAssetId Id => _id;
        public string Name => _name;
        public string Description => _description;
        public AnimationClip AnimationClip => _animationClip;
        public AnimationClip Asset => _animationClip;
        public MotionPlaybackKind PlaybackKind => _playbackKind;
    }
}
