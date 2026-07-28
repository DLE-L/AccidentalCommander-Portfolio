using System;
using System.Collections.Generic;
using Lizzo.PV.Data;
using UnityEngine;

namespace Lizzo.PV.Legion.Synergy
{
    public sealed class HealingBondSynergy : IDisposable
    {
        public interface IWorld
        {
            void CollectCompanions(List<ICompanion> results);
        }

        public interface ICompanion
        {
            string StableIdentity { get; }
            float HealthRatio { get; }
            Vector3 Position { get; }
            bool IsCommander { get; }
            bool IsLiving { get; }
        }

        const string EffectId = "EFFECT_HEALING_BOND_DR";
        const string SynergyId = "synergy_healing_bond";

        readonly SynergyTriggerState _triggers;
        readonly IWorld _world;
        readonly SynergyEffectData _effect;
        readonly List<ICompanion> _companions = new List<ICompanion>(8);

        ICompanion _pendingOrigin;
        Vector3 _pendingOriginPosition;
        ICompanion _zoneOrigin;
        Vector3 _zoneCenter;
        float _zoneExpiresAt;
        bool _hasPendingOrigin;
        bool _hasActiveZone;
        bool _disposed;

        public HealingBondSynergy(IDataProvider data, SynergyTriggerState triggers, IWorld world)
        {
            _triggers = triggers ?? throw new ArgumentNullException(nameof(triggers));
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _effect = data?.GetSynergyEffect(EffectId) ??
                throw new InvalidOperationException("Canonical Healing Bond synergy effect data is missing.");
            if (_effect.SynergyId != SynergyId
                || _effect.DamageTakenMultiplier != 0.80f
                || _effect.DurationSeconds != 3.0f
                || _effect.Radius != 2.5f
                || _effect.KnockdownImmunity == false
                || _effect.CompanionsOnly == false
                || _effect.CommanderExcluded == false
                || _effect.NewReplacesOld == false
                || _effect.NumericStackingAllowed)
                throw new InvalidOperationException("Canonical Healing Bond synergy effect data is invalid.");
        }

        public bool HasActiveZone => _hasActiveZone;
        public int ActiveZoneCount => _hasActiveZone ? 1 : 0;
        public ICompanion ZoneOrigin => _zoneOrigin;
        public Vector3 ZoneCenter => _zoneCenter;
        public float ZoneExpiresAt => _zoneExpiresAt;

        public bool ReportHealing(ICompanion origin, in SynergyHealingEvent healingEvent)
        {
            if (_disposed || origin == null || origin.IsLiving == false || IsEligible(healingEvent) == false)
                return false;

            bool queued = _triggers.ReportHealing(healingEvent);
            if (queued || _triggers.HasPending(SynergyId))
            {
                _pendingOrigin = origin;
                _pendingOriginPosition = origin.Position;
                _hasPendingOrigin = true;
            }

            return queued;
        }

        public bool TryResolvePending(float now)
        {
            if (_disposed || _triggers.TryConsumePending(SynergyId, out _) == false)
                return false;

            ICompanion origin = _hasPendingOrigin ? _pendingOrigin : FindLowestLivingCompanion();
            Vector3 originPosition = _hasPendingOrigin ? _pendingOriginPosition : origin != null ? origin.Position : default;
            _pendingOrigin = null;
            _hasPendingOrigin = false;
            if (origin == null)
                return true;

            _zoneOrigin = origin;
            _zoneCenter = originPosition;
            _zoneExpiresAt = now + _effect.DurationSeconds;
            _hasActiveZone = true;
            return true;
        }

        public void Tick(float now)
        {
            if (_hasActiveZone && now >= _zoneExpiresAt)
                ClearZone();
        }

        public bool IsAffected(ICompanion companion)
        {
            if (_hasActiveZone == false || companion == null || companion.IsLiving == false || companion.IsCommander)
                return false;

            return (companion.Position - _zoneCenter).sqrMagnitude <= _effect.Radius * _effect.Radius;
        }

        public float GetDamageTakenMultiplier(ICompanion companion)
        {
            return IsAffected(companion) ? _effect.DamageTakenMultiplier : 1.0f;
        }

        public bool HasKnockdownImmunity(ICompanion companion)
        {
            return IsAffected(companion) && _effect.KnockdownImmunity;
        }

        public void Reset()
        {
            _pendingOrigin = null;
            _pendingOriginPosition = default;
            _hasPendingOrigin = false;
            ClearZone();
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            Reset();
            _companions.Clear();
            _disposed = true;
        }

        ICompanion FindLowestLivingCompanion()
        {
            _companions.Clear();
            _world.CollectCompanions(_companions);
            ICompanion selected = null;
            for (int index = 0; index < _companions.Count; index++)
            {
                ICompanion candidate = _companions[index];
                if (candidate == null || candidate.IsLiving == false || candidate.IsCommander)
                    continue;

                if (selected == null
                    || candidate.HealthRatio < selected.HealthRatio
                    || (candidate.HealthRatio == selected.HealthRatio
                        && string.CompareOrdinal(candidate.StableIdentity, selected.StableIdentity) < 0))
                    selected = candidate;
            }

            return selected;
        }

        void ClearZone()
        {
            _zoneOrigin = null;
            _zoneCenter = default;
            _zoneExpiresAt = 0.0f;
            _hasActiveZone = false;
        }

        static bool IsEligible(in SynergyHealingEvent healingEvent)
        {
            return healingEvent.IsHealingSkillTag
                && healingEvent.EffectiveHealAmount >= 1
                && healingEvent.IsOverheal == false
                && healingEvent.IsShield == false
                && healingEvent.IsReviveRestore == false;
        }
    }
}
