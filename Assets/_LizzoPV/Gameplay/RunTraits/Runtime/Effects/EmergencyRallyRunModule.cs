using System;
using System.Collections.Generic;
using Lizzo.PV.Gameplay.Diagnostics;
using UnityEngine;

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
        bool _active;
        bool _disposed;

        public float ExpiresAt => _expiresAt;
        public int RecipientCount => _remainingAbsorptionByRosterSlot.Count;

        public int GetRemainingAbsorption(string rosterSlotId)
        {
            return string.IsNullOrEmpty(rosterSlotId) == false && _remainingAbsorptionByRosterSlot.TryGetValue(rosterSlotId, out int remaining)
                ? remaining
                : 0;
        }

        public bool TryActivate(int currentHp, int maxHp, IReadOnlyList<string> rosterSlotIds, float now)
        {
            if (_disposed || _triggered || currentHp <= 0 || maxHp <= 0 || currentHp * 100 >= maxHp * 35)
                return false;

            _triggered = true;
            _active = true;
            _expiresAt = now + DurationSeconds;
            if (rosterSlotIds != null)
            {
                for (int index = 0; index < rosterSlotIds.Count; index++)
                {
                    string rosterSlotId = rosterSlotIds[index];
                    if (string.IsNullOrEmpty(rosterSlotId)
                        || _remainingAbsorptionByRosterSlot.ContainsKey(rosterSlotId))
                    {
                        continue;
                    }

                    _remainingAbsorptionByRosterSlot.Add(rosterSlotId, DamageAbsorptionPerRosterSlot);
                }
            }

            Build1RuntimeDiagnostics.Log("trait_effect_applied",
                Build1RuntimeDiagnostics.Text("trait_id", RunTraitIds.EmergencyRally),
                Build1RuntimeDiagnostics.Float("commander_hp_ratio", maxHp > 0 ? (float)currentHp / maxHp : 0.0f),
                Build1RuntimeDiagnostics.Int("target_count", RecipientCount),
                Build1RuntimeDiagnostics.Float("move_multiplier", MoveSpeedMultiplier),
                Build1RuntimeDiagnostics.Int("shield", DamageAbsorptionPerRosterSlot),
                Build1RuntimeDiagnostics.Float("duration", DurationSeconds));
            return true;
        }

        internal bool TryGetActiveDurationRatio(float now, out float remainingRatio)
        {
            remainingRatio = 0.0f;
            if (_disposed || _active == false || now >= _expiresAt)
                return false;

            remainingRatio = Mathf.Clamp01((_expiresAt - now) / DurationSeconds);
            return true;
        }

        public float GetMoveSpeedMultiplier(string rosterSlotId, float now)
        {
            ReportExpiry(now);
            return IsActiveFor(rosterSlotId, now) ? MoveSpeedMultiplier : 1.0f;
        }

        public int ResolvePostMitigationDamage(string rosterSlotId, int damage, float now, out int absorbedDamage)
        {
            absorbedDamage = 0;
            ReportExpiry(now);
            if (damage <= 0 || IsActiveFor(rosterSlotId, now) == false)
                return damage;

            int remaining = _remainingAbsorptionByRosterSlot[rosterSlotId];
            absorbedDamage = Math.Min(damage, remaining);
            remaining -= absorbedDamage;
            _remainingAbsorptionByRosterSlot[rosterSlotId] = remaining;

            if (absorbedDamage > 0)
            {
                Build1RuntimeDiagnostics.Log("trait_effect_applied",
                    Build1RuntimeDiagnostics.Text("trait_id", RunTraitIds.EmergencyRally),
                    Build1RuntimeDiagnostics.Text("roster_slot_id", rosterSlotId),
                    Build1RuntimeDiagnostics.Int("absorbed_damage", absorbedDamage),
                    Build1RuntimeDiagnostics.Int("remaining_pool", remaining));
            }

            return damage - absorbedDamage;
        }

        public void RemoveRecipient(string rosterSlotId)
        {
            if (string.IsNullOrEmpty(rosterSlotId) == false)
                _remainingAbsorptionByRosterSlot.Remove(rosterSlotId);

            Build1RuntimeDiagnostics.Log("trait_effect_expired",
                Build1RuntimeDiagnostics.Text("trait_id", RunTraitIds.EmergencyRally),
                Build1RuntimeDiagnostics.Text("roster_slot_id", rosterSlotId),
                Build1RuntimeDiagnostics.Text("reason", "recipient_down"));
        }

        public void Reset()
        {
            if (_disposed)
                return;

            _remainingAbsorptionByRosterSlot.Clear();
            _expiresAt = 0.0f;
            _triggered = false;
            _active = false;
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

        private void ReportExpiry(float now)
        {
            if (_active == false || now < _expiresAt)
                return;

            _active = false;
            Build1RuntimeDiagnostics.Log("trait_effect_expired",
                Build1RuntimeDiagnostics.Text("trait_id", RunTraitIds.EmergencyRally),
                Build1RuntimeDiagnostics.Text("reason", "duration"));
        }
    }
}
