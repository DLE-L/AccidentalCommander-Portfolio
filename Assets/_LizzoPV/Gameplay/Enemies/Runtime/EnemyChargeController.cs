using Lizzo.PV.Gameplay.Units;
using System;
using Lizzo.PV.Combat;
using Lizzo.PV.Data;
using Lizzo.PV.Flow;
using Lizzo.PV.Gameplay.Combat;
using Lizzo.PV.Gameplay.Telemetry;
using Lizzo.PV.Gameplay.Visuals;
using Lizzo.PV.Legion;
using UnityEngine;

namespace Lizzo.PV.Gameplay.Units
{
    [DisallowMultipleComponent]
    public sealed class EnemyChargeController : MonoBehaviour, IChargeCancelable, IRunFinalThreatBehaviour
    {
        [SerializeField] private EnemyChargeProfile _profile;
        [SerializeField] private SpriteRenderer _pathRenderer;
        [SerializeField] private bool _stunImmune;
        private EnemyChargePresentation _presentation;
        public event Action<EnemyActionFrame> FrameChanged;
        private EnemyActor _monster;
        private Rigidbody2D _body;
        private SpriteRenderer _sprite;
        private EnemyActionRunner _runner;
        private EnemyMovementMotor _motor;
        private Color _baseColor;
        private float _duration;
        private float _hitRadius;
        private float _stunRemaining;
        private bool _contactEnabled;
        public EnemyActionFrame CurrentFrame { get; private set; }
        public bool IsCharging => CurrentFrame.Phase == EnemyActionPhase.Executing && CurrentFrame.Remaining > 0f;
        public bool IsImpactGrace => CurrentFrame.Phase == EnemyActionPhase.ImpactGrace && CurrentFrame.Remaining > 0f;
        public bool IsChargeCancelable => _profile != null && _profile.AllowCancellation &&
            (IsCharging || IsImpactGrace || CurrentFrame.Phase == EnemyActionPhase.Warning && CurrentFrame.Remaining > 0f);
        public string ActiveDamagePatternId => IsImpactGrace ? _profile.ImpactGracePatternId : IsCharging ? _profile.PatternId : null;

        public void Setup(EnemyActor monster)
        {
            StopAttack();
            if (_profile == null)
                throw new InvalidOperationException("Charge enemy requires its attack profile.");
            _monster = monster != null ? monster : throw new ArgumentNullException(nameof(monster));
            _body = GetComponent<Rigidbody2D>() ?? throw new InvalidOperationException("Charge enemy requires Rigidbody2D.");
            _sprite = GetComponentInChildren<SpriteRenderer>();
            EnemyData data = monster.Data.GetEnemy(_profile.EnemyDataId)
                ?? throw new InvalidOperationException("Charge enemy data missing: " + _profile.EnemyDataId);
            EnemyRuntimeStats.ApplyTo(monster, data);
            _baseColor = data.Color;
            _duration = _profile.UseEnemyChargeTiming ? Mathf.Max(.1f, data.ChargeDuration) : _profile.DurationSeconds;
            float cooldown = _profile.UseEnemyChargeTiming ? Mathf.Max(.1f, data.ChargeCooldown) : _profile.CooldownSeconds;
            _hitRadius = _profile.HitRadius > 0f ? _profile.HitRadius : Mathf.Max(.1f, data.ContactRange * .5f);
            var loadout = GetComponent<EnemyAttackLoadout>() ?? throw new InvalidOperationException("Charge enemy requires EnemyAttackLoadout.");
            _runner = new EnemyActionRunner(loadout.Compose(new[] {
                new EnemyAttackDefinition(EnemyAttackKind.Charge, _profile.WarningSeconds, _duration,
                    monster.ExternalAttackRecoverySeconds, cooldown, _profile.InitialCooldown ? cooldown : 0f,
                    _profile.MinimumRange, _profile.Speed * _duration + _hitRadius, _profile.Speed, _profile.ImpactGraceSeconds)
            }, externalContact: true), data.MoveSpeed);
            _contactEnabled = loadout.Contains(EnemyAttackKind.Contact);
            _motor = new EnemyMovementMotor(monster, _body);
            _presentation?.Dispose();
            _presentation = new EnemyChargePresentation(this, _pathRenderer, _sprite, _profile, _baseColor, _profile.Speed * _duration, _hitRadius * 2f);
            _stunRemaining = 0f;
            if (_sprite != null) { _sprite.color = _baseColor; if (_profile.ChargeFeedback) _sprite.sortingOrder = SortingOrder.Unit; }
            monster.SetExternalMovement(true);
            monster.CreatureState = Define.CreatureState.Moving;
        }

        private void FixedUpdate()
        {
            if (RunPauseController.IsResultGameplayLocked || _runner == null || _monster == null) return;
            if (_monster.Hp <= 0) { StopAttack(); return; }
            Advance(Time.fixedDeltaTime);
        }

        private void Advance(float dt)
        {
            if (_monster.IsForcedMovementActive && _runner.Phase == EnemyActionPhase.Recovery)
            {
                Present(_runner.CancelActive());
                if (_profile.AllowCancellation) _monster.SetExternalAttackPreparationLocked(false);
                _motor.ApplyVelocity(Vector2.zero, false, dt);
                return;
            }
            if (_monster.AdvanceExternalAttackRecovery(dt)) return;
            if (_stunRemaining > 0f) { _stunRemaining = Mathf.Max(0f, _stunRemaining - dt); _motor.ApplyVelocity(Vector2.zero, false, dt); return; }
            if (_monster.CreatureState != Define.CreatureState.Moving)
            {
                if (_monster.IsForcedMovementActive) _motor.ApplyVelocity(Vector2.zero, false, dt);
                return;
            }
            if (_contactEnabled && _profile.ContactInterruptsCharge && _runner.Phase != EnemyActionPhase.Recovery
                && _monster.TryEnterContactAttack()) { Present(_runner.CancelActive()); return; }
#if UNITY_EDITOR
            if (_editorManualActions && _runner.Phase == EnemyActionPhase.Idle)
            { Present(_runner.Advance(dt, _monster.ReadExternalActionInput(), false)); _motor.ApplyVelocity(Vector2.zero, false, dt); return; }
#endif
            EnemyActionPhase previous = _runner.Phase;
            var frame = _runner.Advance(dt, _monster.ReadExternalActionInput());
            Present(frame);
            if (_contactEnabled && !_profile.ContactInterruptsCharge && previous == EnemyActionPhase.Warning) _monster.TryApplyContactDamageNow();
            Vector2 start = _body.position;
            Vector2 end = start + frame.Velocity * dt;
            _motor.ApplyVelocity(frame.Velocity, frame.Phase == EnemyActionPhase.Recovery, dt);
            if ((frame.Signals & EnemyActionSignals.ContactHit) != 0)
            {
                string pattern = previous == EnemyActionPhase.ImpactGrace ? _profile.ImpactGracePatternId : _profile.PatternId;
                var player = _monster.Registry.Player;
                if (EnemyChargeAttack.ResolveHit(_monster, player, EnemyChargeHitPolicy.SweptPathStop, start, end, _hitRadius, pattern) == EnemyChargeHitResponse.Stop)
                {
                    Present(_runner.CompleteActive());
                    _motor.ApplyVelocity(Vector2.zero, true, 0f);
                }
            }
            if (_contactEnabled && !_profile.ContactInterruptsCharge && previous == EnemyActionPhase.Idle && frame.Phase == EnemyActionPhase.Idle)
                _monster.TryApplyContactDamageNow(recoverAfterAttack: true);
        }

        private void Present(EnemyActionFrame frame)
        {
            CurrentFrame = frame;
            if ((frame.Signals & EnemyActionSignals.Started) != 0 && _profile.ChargeFeedback)
            {
                RunTelemetry.Log(RunTelemetry.ChargePathWarning, $"source_id={_profile.EnemyDataId}", $"pattern_id={_profile.PatternId}",
                    $"warning={_profile.WarningSeconds:0.##}", $"length={_profile.Speed * _duration:0.##}", $"trigger_distance={_profile.Speed * _duration + _hitRadius:0.##}");
            }
            if (frame.Direction.sqrMagnitude > 0f && frame.Phase != EnemyActionPhase.Idle) _monster.UpdateExternalMoveFacing(frame.Direction);
            else if (frame.Velocity.sqrMagnitude > 0f) _monster.UpdateExternalMoveFacing(frame.Velocity);
            FrameChanged?.Invoke(frame);
        }

        public ChargeCancellationResult CancelChargeAndApplyStun(float seconds)
        {
            var result = ChargeCancellationRules.Resolve(IsChargeCancelable, _stunImmune, seconds);
            if (!result.ChargeCancelled) return result;
            Present(_runner.CancelActive());
            _monster.SetExternalAttackPreparationLocked(false);
            if (result.StunApplied) _stunRemaining = Mathf.Max(_stunRemaining, seconds);
            return result;
        }
        private void StopAttack() { _runner?.Reset(); CurrentFrame = default; FrameChanged?.Invoke(default); _motor?.Release(); }
        private void OnDisable()
        {
            StopAttack();
            _presentation?.Dispose();
            _presentation = null;
            if (_monster != null) _monster.SetExternalMovement(false);
            _monster = null; _runner = null; _motor = null; _stunRemaining = 0f;
            if (_sprite != null) _sprite.color = Color.white;
        }
#if UNITY_EDITOR
        private bool _editorManualActions;
        public EnemyActionFrame EditorActionFrame => CurrentFrame;
        public EnemyActionRunner EditorRunner => _runner;
        public void SetEditorManualActions(bool manual) => _editorManualActions = manual;
        public bool RequestEditorAttack(EnemyAttackKind kind)
        {
            if (_runner == null || _monster.Hp <= 0 || _stunRemaining > 0f || _runner.Phase != EnemyActionPhase.Idle) return false;
            if (kind == EnemyAttackKind.Contact) return _contactEnabled && (_profile.ContactInterruptsCharge ? _monster.TryEnterContactAttack()
                : _monster.ReadExternalActionInput().ContactReady && _monster.TryApplyContactDamageNow(recoverAfterAttack: true));
            if (kind != EnemyAttackKind.Charge || !_runner.TryStartAttack(kind, _monster.ReadExternalActionInput(), out var frame)) return false;
            Present(frame);
            return (frame.Signals & EnemyActionSignals.Started) != 0;
        }
#endif
    }
}
