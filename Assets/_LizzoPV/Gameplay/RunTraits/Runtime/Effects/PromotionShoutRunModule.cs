using System;
using Lizzo.PV.Gameplay.Diagnostics;
using Lizzo.PV.Legion;
using UnityEngine;

namespace Lizzo.PV.Gameplay.RunTraits
{
    internal sealed class PromotionShoutRunModule : IDisposable
    {
        public const float DurationSeconds = 5.0f;
        public const float AttackIntervalDivisor = 1.20f;

        readonly RuntimeObjectRegistry _registry;
        float _expiresAt;
        bool _active;
        bool _disposed;

        internal PromotionShoutRunModule(RuntimeObjectRegistry registry = null)
        {
            _registry = registry;
        }

        public float ExpiresAt => _expiresAt;

        public void OnPromotionCommitted(float now)
        {
            if (_disposed)
                return;

            _expiresAt = now + DurationSeconds;
            _active = true;
            if (_registry?.Player != null)
                RetroVfx.Spawn(
                    RetroVfxKind.PromotionShoutActivate,
                    _registry.Player.transform.position,
                    Vector3.up,
                    1.0f);
            Build1RuntimeDiagnostics.Log("trait_effect_applied",
                Build1RuntimeDiagnostics.Text("trait_id", RunTraitIds.PromotionShout),
                Build1RuntimeDiagnostics.Text("promotion_slot_id", "unavailable"),
                Build1RuntimeDiagnostics.Text("affected_living_count", "unavailable"),
                Build1RuntimeDiagnostics.Float("attack_speed_multiplier", AttackIntervalDivisor),
                Build1RuntimeDiagnostics.Float("duration", DurationSeconds));
        }

        public float GetAttackIntervalDivisor(float now)
        {
            ReportExpiry(now);
            return !_disposed
                && now < _expiresAt
                ? AttackIntervalDivisor
                : 1.0f;
        }

        internal bool TryGetActiveDurationRatio(float now, out float remainingRatio)
        {
            remainingRatio = 0.0f;
            if (_disposed || _active == false || now >= _expiresAt)
                return false;

            remainingRatio = Mathf.Clamp01((_expiresAt - now) / DurationSeconds);
            return true;
        }

        public void Reset()
        {
            if (_disposed == false)
            {
                _expiresAt = 0.0f;
                _active = false;
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _expiresAt = 0.0f;
            _active = false;
            _disposed = true;
        }

        private void ReportExpiry(float now)
        {
            if (_active == false || now < _expiresAt)
                return;

            _active = false;
            Build1RuntimeDiagnostics.Log("trait_effect_expired",
                Build1RuntimeDiagnostics.Text("trait_id", RunTraitIds.PromotionShout),
                Build1RuntimeDiagnostics.Text("reason", "duration"));
        }
    }
}
