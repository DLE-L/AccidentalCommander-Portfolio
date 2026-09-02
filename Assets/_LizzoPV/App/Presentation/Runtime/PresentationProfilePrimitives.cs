using System;
using UnityEngine;

namespace Lizzo.PV.Presentation
{
    public enum ControlStyleRole
    {
        PrimaryButton,
        DestructiveButton,
        IconButton,
        ChoiceCard,
    }

    [Serializable]
    public struct LocalizationKey : IEquatable<LocalizationKey>
    {
        [SerializeField] private string _value;

        public LocalizationKey(string value)
        {
            _value = value;
        }

        public static LocalizationKey None => default;
        public string Value => _value;
        public bool IsNone => string.IsNullOrWhiteSpace(_value);
        public bool Equals(LocalizationKey other) => string.Equals(_value, other._value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is LocalizationKey other && Equals(other);
        public override int GetHashCode() => _value == null ? 0 : StringComparer.Ordinal.GetHashCode(_value);
        public override string ToString() => _value ?? string.Empty;
        public static bool operator ==(LocalizationKey left, LocalizationKey right) => left.Equals(right);
        public static bool operator !=(LocalizationKey left, LocalizationKey right) => !left.Equals(right);
    }
}
