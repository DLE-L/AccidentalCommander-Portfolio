using Lizzo.PV.Gameplay.Units;
using System;
using UnityEngine;
using Lizzo.PV.Combat;
using Lizzo.PV.Data;
using Lizzo.PV.Flow;
using Lizzo.PV.Gameplay.Combat;
using Lizzo.PV.Gameplay.Diagnostics;
using Lizzo.PV.Gameplay.Telemetry;
using Lizzo.PV.Gameplay.Visuals;
using Lizzo.PV.Legion;
namespace Lizzo.PV.Gameplay.Units
{
    [DisallowMultipleComponent]
    public sealed class EnemyBossController : MonoBehaviour, IRunFinalThreatBehaviour
    {
        private const float BOSS_CHARGE_SPEED = 1.9f;
        private const float BOSS_CHARGE_DURATION_SECONDS = 1.2f;
        private const float BOSS_CHARGE_PATH_WIDTH = 1.25f;
        private const float MIN_CHARGE_DISTANCE_SQR = 1.5f * 1.5f;
        private const float BOSS_AOE_COOLDOWN_SECONDS = 7.0f;
        private const float BOSS_AOE_INITIAL_DELAY_SECONDS = 3.0f;
        private const float BOSS_AOE_TRIGGER_DISTANCE = 6.0f;
        private const float BOSS_AOE_WARNING_BONUS_SECONDS = 0.35f;
        public const string BossAoePatternId = CombatIds.BossAoeSlam;

        private static readonly Color HungryGiantColor = new Color(0.45f, 0.08f, 0.08f, 1.0f);
        private static readonly Color ChargeWarningColor = new Color(0.95f, 0.28f, 0.12f, 1.0f);
        private static readonly Color ChargeColor = new Color(0.85f, 0.02f, 0.02f, 1.0f);
        private static readonly Color ChargePathColor = new Color(1.0f, 0.18f, 0.04f, 0.34f);

        [SerializeField] private SpriteRenderer _chargePathRenderer;
        [SerializeField, Min(0.05f)] private float _contactAttackWindupSeconds = 0.28f;
        private readonly ChargePathWarning _chargePathWarning = new ChargePathWarning();
        private BossChargePresentation _presentation;
        public event Action<Vector2, Vector2, float> ChargeWarningChanged;
        public event Action ChargeWarningHidden;
        public event Action<EnemyActionFrame> FrameChanged;
        [SerializeField] private EnemyAreaAttack _areaAttack;
        [SerializeField] private BossVulnerabilityWindow _vulnerability;
        private EnemyActor _monster;
        private Rigidbody2D _rigidbody;
        private SpriteRenderer _spriteRenderer;
        private Collider2D _combatCollider;
        private Vector2 _chargeDirection;
        private Color _baseColor = HungryGiantColor;
        private float _moveSpeed = 1.2f;
        private float _chargeCooldownSeconds = 7.0f;
        private float _bossWarningTime = 1.0f;
        private int _bossAttack = 25;
        private bool _isSetup;
        private bool _deathTelegraphCleared;

        public bool IsCharging => _runner != null && CurrentFrame.Kind == EnemyAttackKind.Charge
            && CurrentFrame.Phase == EnemyActionPhase.Executing && CurrentFrame.Remaining > 0f;
        public bool IsAoeDamageFrame => _areaAttack != null && _areaAttack.IsDamageFrame;
        public bool IsStaggered => _vulnerability != null && _vulnerability.IsStaggered;


        public void Setup(EnemyActor monster)
        {
            StopAttack();
            _chargePathWarning.Bind(_chargePathRenderer);

            _monster = monster;

            if (_rigidbody == null)
                _rigidbody = GetComponent<Rigidbody2D>();
            if (_spriteRenderer == null)
                _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            _combatCollider = ResolveCombatCollider();
            _chargeDirection = Vector2.zero;
            _bossWarningTime = _monster.Tuning.Boss1WarningTime;
            _bossAttack = _monster.Tuning.Boss1Atk;
            _vulnerability.ClearBossStagger();
            _deathTelegraphCleared = false;
            _isSetup = true;

            EnemyData data = _monster.Data.GetEnemy(CombatIds.BossHungryGiant);
            if (data != null)
            {
                EnemyRuntimeStats.ApplyTo(monster, data);
                _combatCollider = ResolveCombatCollider();
                _moveSpeed = data.MoveSpeed;
                _chargeCooldownSeconds = Mathf.Max(0.1f, data.ChargeCooldown);
                _baseColor = data.Color;
            }

            ValidateBossCombatCollider();
            if (_areaAttack == null || _vulnerability == null) throw new System.InvalidOperationException("Boss requires area attack and vulnerability components.");
            _areaAttack.Setup(monster);
            _vulnerability.Setup(monster);

            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = _baseColor;
                _spriteRenderer.sortingOrder = SortingOrder.Unit;
            }

            _presentation?.Dispose();
            _presentation = new BossChargePresentation(this, _chargePathWarning, _spriteRenderer, _baseColor);
            _monster.ResetHealth(_monster.Tuning.Boss1Hp);
            _monster.SetExternalMovement(true);
            _monster.CreatureState = Define.CreatureState.Moving;
            InitializeRunner();

            EnemyHealthBar.RemoveFrom(transform);
            CombatRuntimeDiagnostics.Log(
                "boss_hp_initialized",
                CombatRuntimeDiagnostics.Text("boss_id", CombatIds.BossHungryGiant),
                CombatRuntimeDiagnostics.Int("max_hp", _monster.MaxHp),
                CombatRuntimeDiagnostics.Text("hud_bind", "unavailable"));
            CombatRuntimeDiagnostics.Log(
                "boss_spawn",
                CombatRuntimeDiagnostics.Text("boss_id", CombatIds.BossHungryGiant),
                CombatRuntimeDiagnostics.Float("position_x", transform.position.x),
                CombatRuntimeDiagnostics.Float("position_y", transform.position.y),
                CombatRuntimeDiagnostics.Int("hp", _monster.Hp),
                CombatRuntimeDiagnostics.Int("attack", _monster.RuntimeStats?.AttackDamage ?? 2),
                CombatRuntimeDiagnostics.Float("move_speed", _moveSpeed));
}

        private void OnDisable()
        {
            StopAttack();
            _runner = null; _motor = null;
            _areaAttack?.ResetAttack();

            if (_monster != null)
            {
				_monster.SetExternalAttackPreparationLocked(false);
                _monster.SetExternalMovement(false);
			}

            _presentation?.Dispose();
            _presentation = null;
            _isSetup = false;
            _monster = null;
            _chargeDirection = Vector2.zero;
            ClearDeathTelegraphs();

            if (_spriteRenderer != null)
                _spriteRenderer.color = Color.white;
        }

        private void ClearDeathTelegraphs()
        {
            if (_deathTelegraphCleared)
                return;

            _deathTelegraphCleared = true;
            _vulnerability.ClearBossStagger();
			_monster?.SetExternalAttackPreparationLocked(false);
            ChargeWarningHidden?.Invoke();
            _areaAttack.HideBossAoeWarning();
        }

        private void ValidateBossCombatCollider()
        {
            if (_combatCollider == null)
            {
                Debug.LogError("Hungry Giant prefab is missing required CombatCollider.", this);
                return;
            }

            if (_combatCollider.isTrigger == false)
                Debug.LogError("Hungry Giant CombatCollider must be trigger.", this);
        }

        private Collider2D ResolveCombatCollider()
        {
            return _monster == null ? null : _monster.CombatCollider;
        }


        private void BeginBossChargeWarning(Vector2 direction)
        {
			_monster?.SetExternalAttackPreparationLocked(true);
            _chargeDirection = direction.normalized;
            float warningDuration = Mathf.Clamp(_bossWarningTime, 0.9f, 1.1f);
            ShowBossChargePath(0f);
            CombatRuntimeDiagnostics.Log(
                "boss_telegraph",
                CombatRuntimeDiagnostics.Text("boss_id", CombatIds.BossHungryGiant),
                CombatRuntimeDiagnostics.Text("attack_type", "charge"),
                CombatRuntimeDiagnostics.Float("warning_seconds", warningDuration),
                CombatRuntimeDiagnostics.Text("geometry", "path"),
                CombatRuntimeDiagnostics.Float("range", GetBossChargePathLength()),
                CombatRuntimeDiagnostics.Float("width", BOSS_CHARGE_PATH_WIDTH),
                CombatRuntimeDiagnostics.Int("damage", _monster?.RuntimeStats?.AttackDamage ?? 2));
            RunTelemetry.Log(
                RunTelemetry.ChargePathWarning,
                $"source_id={CombatIds.BossHungryGiant}",
                $"pattern_id={CombatIds.BossSlowCharge}",
                $"warning={warningDuration:0.##}",
                $"length={GetBossChargePathLength():0.##}");
            RunTelemetry.Log(
                RunTelemetry.BossPatternWarningShow,
                $"source_id={CombatIds.BossHungryGiant}",
                $"pattern_id={CombatIds.BossSlowCharge}",
                $"warning={warningDuration:0.##}",
                $"width={BOSS_CHARGE_PATH_WIDTH:0.##}",
                $"length={GetBossChargePathLength():0.##}");
        }

        private void EndBossCharge()
        {
            ChargeWarningHidden?.Invoke();
            _vulnerability.BeginBossStagger(CombatIds.BossSlowCharge);
            CombatRuntimeDiagnostics.Log(
                "boss_attack_resolved",
                CombatRuntimeDiagnostics.Text("boss_id", CombatIds.BossHungryGiant),
                CombatRuntimeDiagnostics.Text("attack_type", "charge"),
                CombatRuntimeDiagnostics.Bool("resolved", true),
                CombatRuntimeDiagnostics.Text("affected_count", "unavailable"));
        }

        private void ShowBossChargePath(float progress)
        {
            ChargeWarningChanged?.Invoke(GetBossChargeOrigin(), _chargeDirection, GetBossChargePathLength() * Mathf.Clamp01(progress));
        }

        private Vector2 GetBossChargeOrigin()
        {
            if (_combatCollider == null)
                return transform.position;

            return _combatCollider.transform.TransformPoint(_combatCollider.offset);
        }

        private float GetBossChargePathLength()
        {
            return BOSS_CHARGE_SPEED * BOSS_CHARGE_DURATION_SECONDS;
        }


        private void FixedUpdate()
        {
            if (RunPauseController.IsResultGameplayLocked) return;
            if (!_isSetup || _monster == null) return;
            if (_monster.Hp <= 0)
            {
                StopAttack();
                ClearDeathTelegraphs();
                return;
            }
            _areaAttack.UpdateBossAoeImpact();
            _vulnerability.UpdateBossStagger();
            Advance(Time.fixedDeltaTime);
        }
        private EnemyMovementMotor _motor;
        private EnemyActionRunner _runner;
        internal EnemyActionFrame CurrentFrame { get; private set; }
#if UNITY_EDITOR
        public EnemyActionRunner EditorRunner => _runner;
        public EnemyActionFrame EditorActionFrame => CurrentFrame;
#endif

        private void InitializeRunner()
        {

            _motor = new EnemyMovementMotor(_monster, _rigidbody);
            float recovery = _monster.ExternalAttackRecoverySeconds;
            var loadout = GetComponent<EnemyAttackLoadout>();
            if (loadout == null) throw new System.InvalidOperationException("Hungry Giant requires its authored EnemyAttackLoadout.");
            _runner = new EnemyActionRunner(loadout.Compose(new[] {
                new EnemyAttackDefinition(EnemyAttackKind.Charge,
                    Mathf.Clamp(_bossWarningTime, .9f, 1.1f), BOSS_CHARGE_DURATION_SECONDS,
                    recovery, _chargeCooldownSeconds, 0f, Mathf.Sqrt(MIN_CHARGE_DISTANCE_SQR),
                    BOSS_CHARGE_SPEED * BOSS_CHARGE_DURATION_SECONDS + BOSS_CHARGE_PATH_WIDTH * .5f, BOSS_CHARGE_SPEED),
                new EnemyAttackDefinition(EnemyAttackKind.Area,
                    Mathf.Clamp(_bossWarningTime + BOSS_AOE_WARNING_BONUS_SECONDS, 1.25f, 1.45f),
                    0f, recovery, BOSS_AOE_COOLDOWN_SECONDS, BOSS_AOE_INITIAL_DELAY_SECONDS, 0f, BOSS_AOE_TRIGGER_DISTANCE),
                new EnemyAttackDefinition(EnemyAttackKind.Contact, _contactAttackWindupSeconds,
                    0f, recovery, 0f, 0f, 0f, float.MaxValue),
            }), _moveSpeed);
        }

        internal void Advance(float deltaSeconds)
        {
            if (_monster.IsForcedMovementActive)
            {
                _runner?.Reset();
                CurrentFrame = default;
                ChargeWarningHidden?.Invoke();
                _areaAttack.HideBossAoeWarning();
                _motor.Apply(default, deltaSeconds);
                return;
            }
            // Explicit contact requests share the actor contact module and its recovery hold.
            if (_monster.AdvanceExternalAttackRecovery(deltaSeconds)) return;
            if (_monster.CreatureState != Define.CreatureState.Moving) return;
#if UNITY_EDITOR
            if (_editorManualActions && _runner.Phase == EnemyActionPhase.Idle)
            {
                ApplyFrame(_runner.Advance(deltaSeconds, _monster.ReadExternalActionInput(), false), deltaSeconds);
                return;
            }
#endif
            EnemyActionInput input = _monster.ReadExternalActionInput();
            EnemyActionFrame frame = _runner.Advance(deltaSeconds, input);
            ApplyFrame(frame, deltaSeconds);
        }

        internal void ApplyFrame(EnemyActionFrame frame, float deltaSeconds)
        {
            CurrentFrame = frame;
            bool charge = frame.Kind == EnemyAttackKind.Charge;
            bool area = frame.Kind == EnemyAttackKind.Area;
            if (charge) _chargeDirection = frame.Direction;

            if ((frame.Signals & EnemyActionSignals.Started) != 0)
            {
                if (charge) BeginBossChargeWarning(frame.Direction);
                else if (area) _areaAttack.BeginBossAoeWarning(frame.Center);
                else _monster.PlayExternalAttackPose(frame.Direction,
                    _contactAttackWindupSeconds + _monster.ExternalAttackRecoverySeconds);
            }
            if (frame.Phase == EnemyActionPhase.Warning)
            {
                if (charge)
                {
                    _monster.UpdateExternalMoveFacing(frame.Direction);
                    ShowBossChargePath(frame.WarningProgress);
                }
                else if (area) _areaAttack.PresentBossAoeWarning(frame.WarningProgress);
            }
            if (charge && (frame.Signals & EnemyActionSignals.WarningEnded) != 0) ChargeWarningHidden?.Invoke();
            if (frame.Velocity.sqrMagnitude > 0.000001f) _monster.UpdateExternalMoveFacing(frame.Velocity);

            if ((frame.Signals & EnemyActionSignals.ContactHit) != 0)
            {
                if (charge)
                    EnemyChargeAttack.ResolveHit(_monster, _monster.Registry.Player,
                        EnemyChargeHitPolicy.ContactContinue);
                else _monster.TryApplyContactDamageNow();
            }
            if ((frame.Signals & EnemyActionSignals.AreaHit) != 0) { _areaAttack.ApplyBossAoeDamageFrame(); _vulnerability.BeginBossStagger(BossAoePatternId); }
            if (RunPauseController.IsResultGameplayLocked || _monster.Hp <= 0) { StopAttack(); return; }
            if (charge && (frame.Signals & EnemyActionSignals.Completed) != 0) EndBossCharge();
            if ((frame.Signals & EnemyActionSignals.Cancelled) != 0)
            {
                ChargeWarningHidden?.Invoke();
                _areaAttack.HideBossAoeWarning();
            }
            _motor.Apply(frame, deltaSeconds);
            FrameChanged?.Invoke(frame);
        }

        private void StopAttack()
        {
            _runner?.Reset();
            CurrentFrame = default;
            _motor?.Release();
        }
#if UNITY_EDITOR
        private bool _editorManualActions;
        public void SetEditorManualActions(bool manual) => _editorManualActions = manual;
        public bool RequestEditorAttack(EnemyAttackKind kind)
        {
            if (_runner == null || _monster == null || _monster.Hp <= 0 || _runner.Phase != EnemyActionPhase.Idle) return false;
            var input = _monster.ReadExternalActionInput();
            if (!input.HasTarget || (kind == EnemyAttackKind.Contact && !input.ContactInRange)) return false;
            if (!_runner.TryStartAttack(kind, input, out var frame)) return false;
            ApplyFrame(frame, 0f);
            return (frame.Signals & EnemyActionSignals.Started) != 0;
        }
#endif
    }
}
