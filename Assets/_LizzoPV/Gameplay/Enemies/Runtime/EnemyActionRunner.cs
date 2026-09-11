using System;
using UnityEngine;

namespace Lizzo.PV.Gameplay.Units
{
    public enum EnemyAttackKind { Contact, Charge, Area }
    public enum EnemyActionPhase { Idle, Warning, Executing, Recovery, ImpactGrace }

    [Flags]
    public enum EnemyActionSignals
    {
        None = 0, Started = 1, WarningEnded = 2, ContactHit = 4,
        AreaHit = 8, Completed = 16, Cancelled = 32,
    }

    public readonly struct EnemyAttackDefinition
    {
        public readonly EnemyAttackKind Kind;
        public readonly float WarningSeconds, ExecutionSeconds, RecoverySeconds;
        public readonly float CooldownSeconds, InitialCooldownSeconds, MinimumRange, MaximumRange, Speed;
        public readonly float ImpactGraceSeconds;

        public EnemyAttackDefinition(EnemyAttackKind kind, float warningSeconds, float executionSeconds,
            float recoverySeconds, float cooldownSeconds, float initialCooldownSeconds,
            float minimumRange, float maximumRange, float speed = 0f, float impactGraceSeconds = 0f)
        {
            Kind = kind;
            WarningSeconds = warningSeconds; ExecutionSeconds = executionSeconds; RecoverySeconds = recoverySeconds;
            CooldownSeconds = cooldownSeconds; InitialCooldownSeconds = initialCooldownSeconds;
            MinimumRange = minimumRange; MaximumRange = maximumRange; Speed = speed;
            ImpactGraceSeconds = impactGraceSeconds;
        }
    }

    public readonly struct EnemyActionInput
    {
        public readonly Vector2 Position, TargetPosition;
        public readonly bool HasTarget, ContactReady, ContactInRange;
        public EnemyActionInput(Vector2 position, Vector2 targetPosition, bool hasTarget, bool contactReady, bool contactInRange)
        {
            Position = position; TargetPosition = targetPosition; HasTarget = hasTarget;
            ContactReady = contactReady; ContactInRange = contactInRange;
        }
    }

    public readonly struct EnemyActionFrame
    {
        public readonly EnemyAttackKind Kind;
        public readonly EnemyActionPhase Phase;
        public readonly EnemyActionSignals Signals;
        public readonly Vector2 Velocity, Direction, Center;
        public readonly float Remaining, WarningProgress;
        public bool HoldsPosition => Phase == EnemyActionPhase.Warning || Phase == EnemyActionPhase.Recovery;
        public EnemyActionFrame(EnemyAttackKind kind, EnemyActionPhase phase, EnemyActionSignals signals,
            Vector2 velocity, Vector2 direction, Vector2 center, float remaining, float warningProgress)
        {
            Kind = kind; Phase = phase; Signals = signals; Velocity = velocity;
            Direction = direction; Center = center; Remaining = remaining; WarningProgress = warningProgress;
        }
    }

    // Owns ordered selection and cooldowns; a committed charge delegates its clocks to EnemyChargeAttack.
    public sealed class EnemyActionRunner
    {
        private readonly EnemyAttackDefinition[] _attacks;
        private readonly float[] _cooldowns;
        private readonly float _chaseSpeed;
        private int _activeIndex = -1;
        private float _remaining;
        private Vector2 _direction, _center;
        private readonly EnemyChargeAttack _charge = new EnemyChargeAttack();
        private EnemyActionPhase _phase;
        private bool HasChargeState => _activeIndex >= 0 && ActiveKind == EnemyAttackKind.Charge;
        public EnemyActionPhase Phase => HasChargeState ? _charge.Phase : _phase;
        public EnemyAttackKind ActiveKind { get; private set; }

        public EnemyActionRunner(EnemyAttackDefinition[] attacks, float chaseSpeed)
        {
            // No scheduled attacks is valid for actors whose only attack is external contact.
            if (attacks == null) throw new ArgumentException("An attack list is required.", nameof(attacks));
            ValidateNonNegative(chaseSpeed);
            _attacks = (EnemyAttackDefinition[])attacks.Clone();
            _cooldowns = new float[attacks.Length];
            _chaseSpeed = chaseSpeed;
            for (int i = 0; i < _attacks.Length; i++)
            {
                var definition = _attacks[i];
                ValidateNonNegative(definition.WarningSeconds); ValidateNonNegative(definition.ExecutionSeconds);
                ValidateNonNegative(definition.RecoverySeconds); ValidateNonNegative(definition.CooldownSeconds);
                ValidateNonNegative(definition.InitialCooldownSeconds); ValidateNonNegative(definition.MinimumRange);
                ValidateNonNegative(definition.MaximumRange); ValidateNonNegative(definition.Speed);
                ValidateNonNegative(definition.ImpactGraceSeconds);
                if (definition.MaximumRange < definition.MinimumRange || !Enum.IsDefined(typeof(EnemyAttackKind), definition.Kind))
                    throw new ArgumentException("Invalid attack definition.", nameof(attacks));
                for (int j = 0; j < i; j++) if (_attacks[j].Kind == definition.Kind)
                    throw new ArgumentException("Each attack kind must have one definition.", nameof(attacks));
            }
            Reset();
        }

        public void Reset()
        {
            _charge.Reset();
            _phase = EnemyActionPhase.Idle; _activeIndex = -1; _remaining = 0f;
            _direction = _center = Vector2.zero;
            for (int i = 0; i < _cooldowns.Length; i++) _cooldowns[i] = _attacks[i].InitialCooldownSeconds;
        }

        // Explicit scripted requests and normal selection enter through the same state transition.
        public EnemyActionFrame CancelActive()
        {
            if (HasChargeState) return _charge.Cancel();
            bool active = Phase != EnemyActionPhase.Idle;
            _phase = EnemyActionPhase.Idle; _remaining = 0f;
            return Frame(active ? EnemyActionSignals.Cancelled : EnemyActionSignals.None, Vector2.zero);
        }

        public EnemyActionFrame CompleteActive()
        {
            if (HasChargeState) return _charge.Complete();
            if (Phase == EnemyActionPhase.Idle || Phase == EnemyActionPhase.Recovery)
                return Frame(EnemyActionSignals.None, Vector2.zero);
            BeginRecovery();
            return Frame(EnemyActionSignals.Completed, Vector2.zero);
        }

        public EnemyActionFrame StartAttack(EnemyAttackKind kind, in EnemyActionInput input)
        {
            if (Phase != EnemyActionPhase.Idle || !input.HasTarget) return Frame(EnemyActionSignals.None, Vector2.zero);
            for (int i = 0; i < _attacks.Length; i++)
            {
                if (_attacks[i].Kind != kind) continue;
                _activeIndex = i; ActiveKind = kind; _phase = EnemyActionPhase.Warning;
                _direction = (input.TargetPosition - input.Position).normalized;
                _center = input.TargetPosition;
                _remaining = _attacks[i].WarningSeconds;
                if (kind == EnemyAttackKind.Charge)
                {
                    _cooldowns[i] = _attacks[i].CooldownSeconds;
                    return _charge.Start(_attacks[i], input);
                }
                return Frame(EnemyActionSignals.Started, Vector2.zero);
            }
            throw new ArgumentException("The requested attack is not in this actor's list.", nameof(kind));
        }

        public bool TryStartAttack(EnemyAttackKind kind, in EnemyActionInput input, out EnemyActionFrame frame)
        {
            frame = Frame(EnemyActionSignals.None, Vector2.zero);
            if (Phase != EnemyActionPhase.Idle || !input.HasTarget) return false;
            float distance = Vector2.Distance(input.Position, input.TargetPosition);
            for (int i = 0; i < _attacks.Length; i++)
            {
                if (_attacks[i].Kind != kind || !IsEligible(i, distance, input)) continue;
                frame = StartAttack(kind, input);
                return true;
            }
            return false;
        }

        private bool IsEligible(int index, float distance, in EnemyActionInput input)
        {
            var definition = _attacks[index];
            return _cooldowns[index] <= 0f && distance >= definition.MinimumRange && distance <= definition.MaximumRange
                && (definition.Kind != EnemyAttackKind.Contact || (input.ContactReady && input.ContactInRange));
        }

        public EnemyActionFrame Advance(float deltaSeconds, in EnemyActionInput input, bool allowSelection = true)
        {
            ValidateNonNegative(deltaSeconds);
            if (deltaSeconds == 0f) return Frame(EnemyActionSignals.None, Vector2.zero);
            if (!input.HasTarget) return CancelActive();
            if (HasChargeState && Phase != EnemyActionPhase.Idle) return _charge.Advance(deltaSeconds);
            if (Phase == EnemyActionPhase.Recovery)
            {
                _remaining = Mathf.Max(0f, _remaining - deltaSeconds);
                if (_remaining <= 0f) _phase = EnemyActionPhase.Idle;
                return Frame(EnemyActionSignals.None, Vector2.zero);
            }
            if (Phase == EnemyActionPhase.Warning)
            {
                if (ActiveKind == EnemyAttackKind.Contact && !input.ContactInRange)
                {
                    _phase = EnemyActionPhase.Idle; _remaining = 0f;
                    return Frame(EnemyActionSignals.Cancelled, Vector2.zero);
                }
                _remaining = Mathf.Max(0f, _remaining - deltaSeconds);
                if (_remaining > 0f) return Frame(EnemyActionSignals.None, Vector2.zero);
                var hit = ActiveKind == EnemyAttackKind.Area ? EnemyActionSignals.AreaHit : EnemyActionSignals.ContactHit;
                if (ActiveKind == EnemyAttackKind.Area) _cooldowns[_activeIndex] = _attacks[_activeIndex].CooldownSeconds;
                BeginRecovery();
                return Frame(EnemyActionSignals.WarningEnded | hit | EnemyActionSignals.Completed, Vector2.zero);
            }
            Vector2 direction = input.TargetPosition - input.Position;
            if (allowSelection && direction.sqrMagnitude <= 0.0001f) return Frame(EnemyActionSignals.None, Vector2.zero);
            float distance = direction.magnitude;
            for (int i = 0; i < _attacks.Length; i++)
            {
                // Preserve ordered cooldown clocks: a selected higher-priority action stops this pass.
                _cooldowns[i] = Mathf.Max(0f, _cooldowns[i] - deltaSeconds);
                var definition = _attacks[i];
                if (!allowSelection || !IsEligible(i, distance, input)) continue;
                return StartAttack(definition.Kind, input);
            }
            return Frame(EnemyActionSignals.None, allowSelection ? direction.normalized * _chaseSpeed : Vector2.zero);
        }

        private void BeginRecovery()
        {
            _remaining = _attacks[_activeIndex].RecoverySeconds;
            _phase = _remaining > 0f ? EnemyActionPhase.Recovery : EnemyActionPhase.Idle;
        }

        private EnemyActionFrame Frame(EnemyActionSignals signals, Vector2 velocity)
        {
            if (HasChargeState) return _charge.Frame(signals, velocity);
            float duration = _activeIndex < 0 ? 0f : _attacks[_activeIndex].WarningSeconds;
            float progress = Phase == EnemyActionPhase.Warning && duration > 0f ? 1f - _remaining / duration : 1f;
            return new EnemyActionFrame(ActiveKind, Phase, signals, velocity, _direction, _center, _remaining, progress);
        }

        private static void ValidateNonNegative(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f)
                throw new ArgumentOutOfRangeException(nameof(value));
        }
    }
}
