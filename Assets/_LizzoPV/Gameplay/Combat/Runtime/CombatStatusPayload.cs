using Lizzo.PV.Data;

namespace Lizzo.PV.Combat
{
    public readonly struct CombatStatusPayload
    {
        public CombatStatusPayload(
            CompanionEnemyStatusKind kind,
            CompanionStatusSource source,
            float magnitude,
            float duration)
        {
            Kind = kind;
            Source = source;
            Magnitude = magnitude;
            Duration = duration;
        }

        public CompanionEnemyStatusKind Kind { get; }
        public CompanionStatusSource Source { get; }
        public float Magnitude { get; }
        public float Duration { get; }

        public bool IsConfigured => Kind != CompanionEnemyStatusKind.None
            && Source.IsValid
            && Magnitude > 0.0f
            && Duration > 0.0f;
    }
}
