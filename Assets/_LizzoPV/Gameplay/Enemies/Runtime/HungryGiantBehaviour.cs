using Lizzo.PV.P0.Combat;
using Lizzo.PV.Data;
using Lizzo.PV.Gameplay.Telemetry;
using UnityEngine;

namespace Lizzo.PV.P0.Units
{
    public sealed partial class HungryGiantBehaviour : MonoBehaviour, IRunFinalThreatBehaviour
    {
        private const float BOSS_CHARGE_SPEED = 1.9f;
        private const float BOSS_CHARGE_DURATION_SECONDS = 1.2f;
        private const float BOSS_CHARGE_PATH_WIDTH = 1.25f;
        private const float MIN_CHARGE_DISTANCE_SQR = 1.5f * 1.5f;
        private const float BOSS_AOE_RADIUS = 1.65f;
        private const float BOSS_AOE_COOLDOWN_SECONDS = 7.0f;
        private const float BOSS_AOE_INITIAL_DELAY_SECONDS = 3.0f;
        private const float BOSS_AOE_TRIGGER_DISTANCE = 6.0f;
        private const float BOSS_AOE_WARNING_BONUS_SECONDS = 0.35f;
        private const float BOSS_AOE_IMPACT_LINGER_SECONDS = 0.24f;
        private const float BOSS_STAGGER_SECONDS = 1.2f;
        private const float BOSS_STAGGER_DAMAGE_MULTIPLIER = 1.18f;
        public const string BossAoePatternId = CombatIds.BossAoeSlam;

        private static readonly Color HungryGiantColor = new Color(0.45f, 0.08f, 0.08f, 1.0f);
        private static readonly Color ChargeWarningColor = new Color(0.95f, 0.28f, 0.12f, 1.0f);
        private static readonly Color ChargeColor = new Color(0.85f, 0.02f, 0.02f, 1.0f);
        private static readonly Color ChargePathColor = new Color(1.0f, 0.18f, 0.04f, 0.34f);
        private static readonly Color BossStaggerColor = new Color(1.0f, 0.82f, 0.18f, 1.0f);
        private static readonly Color BossStaggerLabelColor = new Color(1.0f, 0.92f, 0.24f, 1.0f);

        [SerializeField] private SpriteRenderer _chargePathRenderer;
        private readonly ChargePathWarning _chargePathWarning = new ChargePathWarning();
        private MonsterController _monster;
        private Rigidbody2D _rigidbody;
        private SpriteRenderer _spriteRenderer;
        private Collider2D _combatCollider;
        [Header("Authored Visual References")]
        [SerializeField] private SpriteRenderer _aoeWarningRenderer;
        private Vector2 _chargeDirection;
        private Vector2 _aoeCenter;
        private Color _baseColor = HungryGiantColor;
        private float _moveSpeed = 1.2f;
        private float _chargeCooldownSeconds = 7.0f;
        private float _bossWarningTime = 1.0f;
        private int _bossAttack = 25;
        private float _chargeCooldownRemaining;
        private float _chargeWarningRemaining;
        private float _chargeWarningDuration;
        private float _chargeWarningElapsed;
        private float _chargeTimeRemaining;
        private float _aoeCooldownRemaining;
        private float _aoeWarningRemaining;
        private float _aoeWarningDuration;
        private float _aoeImpactRemaining;
        private float _staggerRemaining;
        private string _staggerPatternId = string.Empty;
        private bool _isSetup;
        private bool _isAoeDamageFrame;
        private bool _deathTelegraphCleared;

        public static HungryGiantBehaviour Current { get; private set; }
        public bool IsCharging => _chargeTimeRemaining > 0.0f;
        public bool IsAoeDamageFrame => _isAoeDamageFrame;
        public bool IsStaggered => _staggerRemaining > 0.0f;

    }
}
