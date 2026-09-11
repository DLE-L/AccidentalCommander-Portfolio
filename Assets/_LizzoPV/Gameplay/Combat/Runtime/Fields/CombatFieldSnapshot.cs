using UnityEngine;

namespace Lizzo.PV.Combat.Fields
{
    public enum CombatFieldChange { Created, Ignited, Removed }

    public readonly struct CombatFieldSnapshot
    {
        public readonly long Id;
        public readonly string SourceId;
        public readonly string EffectId;
        public readonly Vector3 Center;
        public readonly float Radius;
        public readonly float ExpiresAt;

        public CombatFieldSnapshot(long id, string sourceId, string effectId, Vector3 center, float radius, float expiresAt)
        {
            Id = id; SourceId = sourceId; EffectId = effectId;
            Center = center; Radius = radius; ExpiresAt = expiresAt;
        }
    }
}
