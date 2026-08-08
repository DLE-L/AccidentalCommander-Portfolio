using System;
using System.Collections.Generic;

namespace Lizzo.PV.Gameplay.RunTraits
{
    internal sealed class EmergencyRallyRunModule : IDisposable
    {
        public const float DurationSeconds = 4.0f;
        public const float MoveSpeedMultiplier = 1.25f;
        public const int DamageAbsorptionPerRosterSlot = 20;

        readonly Dictionary<string, int> _remainingAbsorptionByRosterSlot = new Dictionary<string, int>(StringComparer.Ordinal);
        float _expiresAt;
        bool _triggered;
        bool _disposed;

        public bool TryActivate(int currentHp, int maxHp, IReadOnlyList<string> rosterSlotIds, float now)
        {
            if (_disposed || _triggered || currentHp <= 0 || maxHp <= 0 || currentHp * 100 >= maxHp * 35)
                return false;

            _triggered = true;
            _expiresAt = now + DurationSeconds;
            if (rosterSlotIds == null)
                return true;

            for (int index = 0; index < rosterSlotIds.Count; index++)
            {
                string rosterSlotId = rosterSlotIds[index];
                if (string.IsNullOrEmpty(rosterSlotId) || _remainingAbsorptionByRosterSlot.ContainsKey(rosterSlotId))
                    continue;

                _remainingAbsorptionByRosterSlot.Add(rosterSlotId, DamageAbsorptionPerRosterSlot);
            }

            return true;
        }

        public float GetMoveSpeedMultiplier(string rosterSlotId, float now)
        {
            return IsActiveFor(rosterSlotId, now) ? MoveSpeedMultiplier : 1.0f;
        }

        public int ResolvePostMitigationDamage(string rosterSlotId, int damage, float now, out int absorbedDamage)
        {
            absorbedDamage = 0;
            if (damage <= 0 || IsActiveFor(rosterSlotId, now) == false)
                return damage;

            int remaining = _remainingAbsorptionByRosterSlot[rosterSlotId];
            absorbedDamage = Math.Min(damage, remaining);
            remaining -= absorbedDamage;
            _remainingAbsorptionByRosterSlot[rosterSlotId] = remaining;

            return damage - absorbedDamage;
        }

        public void RemoveRecipient(string rosterSlotId)
        {
            if (string.IsNullOrEmpty(rosterSlotId) == false)
                _remainingAbsorptionByRosterSlot.Remove(rosterSlotId);
        }

        public void Reset()
        {
            if (_disposed)
                return;

            _remainingAbsorptionByRosterSlot.Clear();
            _expiresAt = 0.0f;
            _triggered = false;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            Reset();
            _disposed = true;
        }

        bool IsActiveFor(string rosterSlotId, float now)
        {
            if (_disposed || string.IsNullOrEmpty(rosterSlotId) || now >= _expiresAt)
            {
                if (_disposed == false && now >= _expiresAt)
                    _remainingAbsorptionByRosterSlot.Clear();
                return false;
            }

            return _remainingAbsorptionByRosterSlot.ContainsKey(rosterSlotId);
        }
    }
}
