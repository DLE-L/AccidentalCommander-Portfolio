using Lizzo.PV.Gameplay.Units;
using UnityEngine;

namespace Lizzo.PV.Gameplay.Units
{
    internal enum EnemyChargeHitPolicy { ContactContinue, SweptPathStop }
    internal enum EnemyChargeHitResponse { None, Continue, Stop }

    // Owns committed charge clocks and hit policy. Selection/cooldowns, movement application
    // and authored visuals stay with the runner/adapters.
    internal sealed class EnemyChargeAttack
    {
        private EnemyAttackDefinition _definition;
        private Vector2 _direction, _center;
        private float _remaining;
        internal EnemyActionPhase Phase { get; private set; }

        internal EnemyActionFrame Start(in EnemyAttackDefinition definition, in EnemyActionInput input)
        {
            _definition = definition;
            _direction = (input.TargetPosition - input.Position).normalized;
            _center = input.TargetPosition;
            Phase = EnemyActionPhase.Warning;
            _remaining = definition.WarningSeconds;
            return Frame(EnemyActionSignals.Started, Vector2.zero);
        }

        // The runner validates delta and target availability before advancing an active charge.
        internal EnemyActionFrame Advance(float deltaSeconds)
        {
            _remaining = Mathf.Max(0f, _remaining - deltaSeconds);
            if (Phase == EnemyActionPhase.Warning)
            {
                if (_remaining > 0f) return Frame(EnemyActionSignals.None, Vector2.zero);
                Phase = EnemyActionPhase.Executing;
                _remaining = _definition.ExecutionSeconds;
                return Frame(EnemyActionSignals.WarningEnded, Vector2.zero);
            }
            if (Phase == EnemyActionPhase.Recovery)
            {
                if (_remaining <= 0f) Phase = EnemyActionPhase.Idle;
                return Frame(EnemyActionSignals.None, Vector2.zero);
            }

            // Keep the entire final execution frame's velocity, including a step that overruns
            // the duration. Existing motors and hit sweeps depend on this fixed-step contract.
            Vector2 velocity = Phase == EnemyActionPhase.Executing ? _direction * _definition.Speed : Vector2.zero;
            var signals = EnemyActionSignals.ContactHit;
            if (_remaining <= 0f)
            {
                if (Phase == EnemyActionPhase.Executing && _definition.ImpactGraceSeconds > 0f)
                {
                    Phase = EnemyActionPhase.ImpactGrace;
                    _remaining = _definition.ImpactGraceSeconds;
                }
                else
                {
                    BeginRecovery();
                    signals |= EnemyActionSignals.Completed;
                }
            }
            return Frame(signals, velocity);
        }

        internal EnemyActionFrame Complete()
        {
            if (Phase == EnemyActionPhase.Idle || Phase == EnemyActionPhase.Recovery)
                return Frame(EnemyActionSignals.None, Vector2.zero);
            BeginRecovery();
            return Frame(EnemyActionSignals.Completed, Vector2.zero);
        }

        internal EnemyActionFrame Cancel()
        {
            bool active = Phase != EnemyActionPhase.Idle;
            Phase = EnemyActionPhase.Idle;
            _remaining = 0f;
            return Frame(active ? EnemyActionSignals.Cancelled : EnemyActionSignals.None, Vector2.zero);
        }

        internal void Reset()
        {
            Phase = EnemyActionPhase.Idle;
            _remaining = 0f;
            _direction = _center = Vector2.zero;
        }

        internal EnemyActionFrame Frame(EnemyActionSignals signals, Vector2 velocity)
        {
            float progress = Phase == EnemyActionPhase.Warning && _definition.WarningSeconds > 0f
                ? 1f - _remaining / _definition.WarningSeconds : 1f;
            return new EnemyActionFrame(EnemyAttackKind.Charge, Phase, signals, velocity,
                _direction, _center, _remaining, progress);
        }

        internal static EnemyChargeHitResponse ResolveHit(EnemyActor monster, CommanderActor player,
            EnemyChargeHitPolicy policy, Vector2 segmentStart = default, Vector2 segmentEnd = default,
            float hitRadius = 0f, string patternId = null)
        {
            if (policy == EnemyChargeHitPolicy.ContactContinue)
            {
                // Contact can be accepted while its damage cooldown blocks a hit. Continue is
                // a movement response, not a claim that HP changed.
                return monster != null && monster.TryApplyContactDamageNow()
                    ? EnemyChargeHitResponse.Continue : EnemyChargeHitResponse.None;
            }
            if (policy != EnemyChargeHitPolicy.SweptPathStop)
                throw new System.ArgumentOutOfRangeException(nameof(policy));
            if (player == null || player.Hp <= 0 || monster == null) return EnemyChargeHitResponse.None;
            if (!player.IsHurtboxOverlappingCapsule(segmentStart, segmentEnd, hitRadius)) return EnemyChargeHitResponse.None;
            EnemyRuntimeStats stats = monster.RuntimeStats;
            int damage = stats == null ? 2 : stats.ChargeDamage;
            return player.TryApplyEnemyPatternDamage(monster, damage, patternId)
                ? EnemyChargeHitResponse.Stop : EnemyChargeHitResponse.None;
        }

        private void BeginRecovery()
        {
            _remaining = _definition.RecoverySeconds;
            Phase = _remaining > 0f ? EnemyActionPhase.Recovery : EnemyActionPhase.Idle;
        }
    }
}
