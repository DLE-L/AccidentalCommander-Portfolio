using System;
using System.Collections.Generic;
using Lizzo.PV.Data;

namespace Lizzo.PV.Legion.Synergy
{
    public sealed class MixedCommandSynergy : IDisposable
    {
        public interface IWorld
        {
            void CollectCompanions(List<ICompanion> results);
        }

        public interface ICompanion
        {
            string StableIdentity { get; }
            bool IsLiving { get; }
            bool IsCommander { get; }
            bool HasCompanionTag { get; }
        }

        const string EffectId = "EFFECT_MIXED_COMMAND";
        const string SynergyId = "synergy_mixed_command";

        readonly SynergyTriggerState _triggers;
        readonly IWorld _world;
        readonly SynergyEffectData _effect;
        readonly List<ICompanion> _companions = new List<ICompanion>(8);
        readonly List<ICompanion> _targets = new List<ICompanion>(8);

        float _expiresAt;
        bool _disposed;

        public MixedCommandSynergy(IDataProvider data, SynergyTriggerState triggers, IWorld world)
        {
            _triggers = triggers ?? throw new ArgumentNullException(nameof(triggers));
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _effect = data?.GetSynergyEffect(EffectId) ??
                throw new InvalidOperationException("Canonical Mixed Command synergy effect data is missing.");
            if (_effect.SynergyId != SynergyId
                || _effect.CadenceSeconds != 15.0f
                || _effect.DurationSeconds != 5.0f
                || _effect.AttackIntervalDivisor != 1.15f
                || _effect.MoveSpeedMultiplier != 1.15f
                || _effect.DamageTakenMultiplier != 0.0f
                || _effect.DamageReduction != 0.0f
                || _effect.Radius != 0.0f
                || _effect.TotalDamageReductionCap != 0.0f
                || _effect.KnockdownImmunity
                || _effect.AllAliveCompanions == false
                || _effect.CompanionsOnly == false
                || _effect.CommanderExcluded
                || _effect.ExcludesCompanionTagFalseSummons == false
                || _effect.SameSourceRefresh == false
                || _effect.NumericStackingAllowed
                || _effect.ZoneMembership
                || _effect.LeaveRemoves
                || _effect.NewReplacesOld
                || _effect.ActivationRuleId != "all_alive_companions"
                || _effect.StackRuleId != "same_source_refresh_no_multiplier_stack"
                || _effect.RemoteConfigKey != "rc_mixed_command_multiplier")
                throw new InvalidOperationException("Canonical Mixed Command synergy effect data is invalid.");
        }

        public int ActiveTargetCount => _targets.Count;
        public float ExpiresAt => _expiresAt;

        public bool TryResolvePending(float now)
        {
            if (_disposed || _triggers.TryConsumePending(SynergyId, out _) == false)
                return false;

            _companions.Clear();
            _world.CollectCompanions(_companions);
            _targets.Clear();
            for (int index = 0; index < _companions.Count; index++)
            {
                ICompanion candidate = _companions[index];
                if (IsEligible(candidate))
                    _targets.Add(candidate);
            }

            _expiresAt = _targets.Count > 0 ? now + _effect.DurationSeconds : 0.0f;
            return true;
        }

        public void Tick(float now)
        {
            if (_disposed)
                return;

            if (_expiresAt > 0.0f && now >= _expiresAt)
            {
                ClearTargets();
                return;
            }

            for (int index = _targets.Count - 1; index >= 0; index--)
            {
                if (_targets[index].IsLiving == false)
                    _targets.RemoveAt(index);
            }

            if (_targets.Count == 0)
                _expiresAt = 0.0f;
        }

        public float GetAttackIntervalDivisor(ICompanion companion)
        {
            return IsAffected(companion) ? _effect.AttackIntervalDivisor : 1.0f;
        }

        public float GetMoveSpeedMultiplier(ICompanion companion)
        {
            return IsAffected(companion) ? _effect.MoveSpeedMultiplier : 1.0f;
        }

        public void Reset()
        {
            ClearTargets();
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            Reset();
            _companions.Clear();
            _disposed = true;
        }

        bool IsAffected(ICompanion companion)
        {
            if (_disposed || IsEligible(companion) == false)
                return false;

            for (int index = 0; index < _targets.Count; index++)
            {
                ICompanion target = _targets[index];
                if (target.IsLiving && target.StableIdentity == companion.StableIdentity)
                    return true;
            }

            return false;
        }

        void ClearTargets()
        {
            _targets.Clear();
            _expiresAt = 0.0f;
        }

        static bool IsEligible(ICompanion companion)
        {
            return companion != null
                && companion.IsLiving
                && companion.IsCommander == false
                && companion.HasCompanionTag;
        }
    }
}
