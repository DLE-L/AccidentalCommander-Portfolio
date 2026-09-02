using System;
using UnityEngine;

namespace Lizzo.PV.Presentation
{
    [Serializable]
    public struct SpriteAssetId : IEquatable<SpriteAssetId>
    {
        [SerializeField] private int _value;

        public SpriteAssetId(int value) => _value = value;
        public static SpriteAssetId None => default;
        public int Value => _value;
        public bool IsNone => _value == 0;
        public bool Equals(SpriteAssetId other) => _value == other._value;
        public override bool Equals(object obj) => obj is SpriteAssetId other && Equals(other);
        public override int GetHashCode() => _value;
        public override string ToString() => _value.ToString();
        public static bool operator ==(SpriteAssetId left, SpriteAssetId right) => left.Equals(right);
        public static bool operator !=(SpriteAssetId left, SpriteAssetId right) => !left.Equals(right);
    }

    [Serializable]
    public struct AudioAssetId : IEquatable<AudioAssetId>
    {
        [SerializeField] private int _value;

        public AudioAssetId(int value) => _value = value;
        public static AudioAssetId None => default;
        public int Value => _value;
        public bool IsNone => _value == 0;
        public bool Equals(AudioAssetId other) => _value == other._value;
        public override bool Equals(object obj) => obj is AudioAssetId other && Equals(other);
        public override int GetHashCode() => _value;
        public override string ToString() => _value.ToString();
        public static bool operator ==(AudioAssetId left, AudioAssetId right) => left.Equals(right);
        public static bool operator !=(AudioAssetId left, AudioAssetId right) => !left.Equals(right);
    }

    [Serializable]
    public struct VfxAssetId : IEquatable<VfxAssetId>
    {
        [SerializeField] private int _value;

        public VfxAssetId(int value) => _value = value;
        public static VfxAssetId None => default;
        public int Value => _value;
        public bool IsNone => _value == 0;
        public bool Equals(VfxAssetId other) => _value == other._value;
        public override bool Equals(object obj) => obj is VfxAssetId other && Equals(other);
        public override int GetHashCode() => _value;
        public override string ToString() => _value.ToString();
        public static bool operator ==(VfxAssetId left, VfxAssetId right) => left.Equals(right);
        public static bool operator !=(VfxAssetId left, VfxAssetId right) => !left.Equals(right);
    }

    [Serializable]
    public struct MotionAssetId : IEquatable<MotionAssetId>
    {
        [SerializeField] private int _value;

        public MotionAssetId(int value) => _value = value;
        public static MotionAssetId None => default;
        public int Value => _value;
        public bool IsNone => _value == 0;
        public bool Equals(MotionAssetId other) => _value == other._value;
        public override bool Equals(object obj) => obj is MotionAssetId other && Equals(other);
        public override int GetHashCode() => _value;
        public override string ToString() => _value.ToString();
        public static bool operator ==(MotionAssetId left, MotionAssetId right) => left.Equals(right);
        public static bool operator !=(MotionAssetId left, MotionAssetId right) => !left.Equals(right);
    }
}
