using System;
using UnityEngine;

namespace Lizzo.PV.Presentation
{
    public enum HitStopGrade
    {
        None,
        Light,
        Medium,
        Heavy,
    }

    public enum StatusReactionKind
    {
        Consumed,
        TargetDeath,
    }

    public enum OrbVisualTier
    {
        Small,
        Medium,
        Large,
    }

    [Serializable]
    public struct CompanionId : IEquatable<CompanionId>
    {
        [SerializeField] private string _value;
        public CompanionId(string value) => _value = value;
        public static CompanionId None => default;
        public string Value => _value;
        public bool IsNone => string.IsNullOrWhiteSpace(_value);
        public bool Equals(CompanionId other) => string.Equals(_value, other._value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is CompanionId other && Equals(other);
        public override int GetHashCode() => _value == null ? 0 : StringComparer.Ordinal.GetHashCode(_value);
        public override string ToString() => _value ?? string.Empty;
        public static bool operator ==(CompanionId left, CompanionId right) => left.Equals(right);
        public static bool operator !=(CompanionId left, CompanionId right) => !left.Equals(right);
    }

    [Serializable]
    public struct StatusId : IEquatable<StatusId>
    {
        [SerializeField] private string _value;
        public StatusId(string value) => _value = value;
        public static StatusId None => default;
        public string Value => _value;
        public bool IsNone => string.IsNullOrWhiteSpace(_value);
        public bool Equals(StatusId other) => string.Equals(_value, other._value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is StatusId other && Equals(other);
        public override int GetHashCode() => _value == null ? 0 : StringComparer.Ordinal.GetHashCode(_value);
        public override string ToString() => _value ?? string.Empty;
        public static bool operator ==(StatusId left, StatusId right) => left.Equals(right);
        public static bool operator !=(StatusId left, StatusId right) => !left.Equals(right);
    }

    [Serializable]
    public struct AttackId : IEquatable<AttackId>
    {
        [SerializeField] private string _value;
        public AttackId(string value) => _value = value;
        public static AttackId None => default;
        public string Value => _value;
        public bool IsNone => string.IsNullOrWhiteSpace(_value);
        public bool Equals(AttackId other) => string.Equals(_value, other._value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is AttackId other && Equals(other);
        public override int GetHashCode() => _value == null ? 0 : StringComparer.Ordinal.GetHashCode(_value);
        public override string ToString() => _value ?? string.Empty;
        public static bool operator ==(AttackId left, AttackId right) => left.Equals(right);
        public static bool operator !=(AttackId left, AttackId right) => !left.Equals(right);
    }

    [Serializable]
    public struct EnemyAttackId : IEquatable<EnemyAttackId>
    {
        [SerializeField] private string _value;
        public EnemyAttackId(string value) => _value = value;
        public static EnemyAttackId None => default;
        public string Value => _value;
        public bool IsNone => string.IsNullOrWhiteSpace(_value);
        public bool Equals(EnemyAttackId other) => string.Equals(_value, other._value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is EnemyAttackId other && Equals(other);
        public override int GetHashCode() => _value == null ? 0 : StringComparer.Ordinal.GetHashCode(_value);
        public override string ToString() => _value ?? string.Empty;
        public static bool operator ==(EnemyAttackId left, EnemyAttackId right) => left.Equals(right);
        public static bool operator !=(EnemyAttackId left, EnemyAttackId right) => !left.Equals(right);
    }

    [Serializable]
    public struct EnemyId : IEquatable<EnemyId>
    {
        [SerializeField] private string _value;
        public EnemyId(string value) => _value = value;
        public static EnemyId None => default;
        public string Value => _value;
        public bool IsNone => string.IsNullOrWhiteSpace(_value);
        public bool Equals(EnemyId other) => string.Equals(_value, other._value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is EnemyId other && Equals(other);
        public override int GetHashCode() => _value == null ? 0 : StringComparer.Ordinal.GetHashCode(_value);
        public override string ToString() => _value ?? string.Empty;
        public static bool operator ==(EnemyId left, EnemyId right) => left.Equals(right);
        public static bool operator !=(EnemyId left, EnemyId right) => !left.Equals(right);
    }

    [Serializable]
    public struct CombatImpactKind : IEquatable<CombatImpactKind>
    {
        [SerializeField] private string _value;
        public CombatImpactKind(string value) => _value = value;
        public static CombatImpactKind None => default;
        public string Value => _value;
        public bool IsNone => string.IsNullOrWhiteSpace(_value);
        public bool Equals(CombatImpactKind other) => string.Equals(_value, other._value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is CombatImpactKind other && Equals(other);
        public override int GetHashCode() => _value == null ? 0 : StringComparer.Ordinal.GetHashCode(_value);
        public override string ToString() => _value ?? string.Empty;
        public static bool operator ==(CombatImpactKind left, CombatImpactKind right) => left.Equals(right);
        public static bool operator !=(CombatImpactKind left, CombatImpactKind right) => !left.Equals(right);
    }
}
