using Lizzo.PV.P0.Combat;
using Lizzo.PV.Legion;
using Lizzo.PV.P0.Units;
using Lizzo.PV.P0.Visuals;
using UnityEngine;



public partial class MonsterController : CreatureController
{
	const float DEFAULT_CONTACT_PADDING = 0.001f;
	const float DIE_DESPAWN_DELAY = 1.25f;
	const float SHIELD_ORC_FRONT_DAMAGE_MULTIPLIER = 0.95f;
	const float SHIELD_ORC_FRONT_DOT_THRESHOLD = 0.35f;
	const float MIN_SMOOTH_KNOCKBACK_DURATION = 0.06f;
	const string SMALL_GOBLIN_ID = CombatIds.SmallGoblin;
	const string HUNGRY_WOLF_ID = CombatIds.HungryWolf;
	const string ELITE_RED_CHARGER_ID = CombatIds.EliteRedCharger;

	Define.CreatureState _creatureState = Define.CreatureState.Moving;

	protected Animator _animator;
	Rigidbody2D _body;
	UnitColliderRefs _colliderRefs;
	EnemyRuntimeStats _runtimeStats;
	UnitVisualRole _unitVisual;
	PatternEnemyVisual _patternVisual;
	HitFlash _hitFlash;
	EnemyHealthBar _healthBar;
	WolfDashBehaviour _wolfDash;
	HungryGiantBehaviour _hungryGiant;
	RedChargerBehaviour _redCharger;
	SpriteRenderer _fallbackSpriteRenderer;
	Collider2D[] _allColliders;
	SpriteRenderer[] _resettableSpriteRenderers;
	PlayerController _cachedContactPlayer;
	Collider2D _cachedPlayerCombatCollider;
	string _lastAnimatorState = string.Empty;
	string _lastFacingDirection = "S";
	Vector3 _lastFacingVector = Vector3.down;
	Vector3 _smoothKnockbackDirection;
	Vector3 _externalAttackPoseDirection;
	float _nextAttackTime;
	float _despawnAt;
	float _bossClearResultAt;
	float _externalAttackPoseUntil;
	float _smoothKnockbackRemainingDistance;
	float _smoothKnockbackDuration;
	float _smoothKnockbackElapsed;
	bool _isDead;
	bool _usesExternalMovement;
	bool _shieldOrcHitFeedbackLogged;
	bool _shieldOrcBreakFeedbackShown;
	bool _shouldShowBossClearResult;
	bool _bossClearResultShown;

	public string EnemyId => _runtimeStats?.Data?.Id ?? gameObject.name;
	public string EnemyType => _runtimeStats?.Data?.Type ?? string.Empty;
	public bool IsBoss => _hungryGiant != null;
	public bool IsShieldOrcEnemy => EnemyId == CombatIds.ShieldOrc;
	public EnemyRuntimeStats RuntimeStats => _runtimeStats;
	public HitFlash HitFlash => _hitFlash;
	public EnemyHealthBar HealthBar => _healthBar;
	public WolfDashBehaviour WolfDash => _wolfDash;
	public Vector3 LastFacingVector => _lastFacingVector;

	public void RefreshRuntimeStatsCache(EnemyRuntimeStats runtimeStats)
	{
		_runtimeStats = runtimeStats;
	}

    public override bool Init()
    {
        base.Init();

        EnsureRuntimeComponents();
        ObjectType = Define.ObjectType.Monster;
        _isDead = false;
        CreatureState = Define.CreatureState.Moving;

        return true;
    }

    public override void ResetForSpawn()
    {
        EnsureRuntimeComponents();
        StopDotDamage();

        ObjectType = Define.ObjectType.Monster;
        MaxHp = MaxHp <= 0 ? 100 : MaxHp;
        Hp = MaxHp;
        _isDead = false;
        _nextAttackTime = 0.0f;
        _despawnAt = 0.0f;
        _bossClearResultAt = 0.0f;
        _usesExternalMovement = false;
        _shieldOrcHitFeedbackLogged = false;
        _shieldOrcBreakFeedbackShown = false;
        _shouldShowBossClearResult = false;
        _bossClearResultShown = false;
        _cachedContactPlayer = null;
        _cachedPlayerCombatCollider = null;
        _smoothKnockbackDirection = Vector3.zero;
        _smoothKnockbackRemainingDistance = 0.0f;
        _smoothKnockbackDuration = 0.0f;
        _smoothKnockbackElapsed = 0.0f;
        _lastAnimatorState = string.Empty;
        _lastFacingDirection = ResolveInitialFacingDirection();
        _lastFacingVector = ResolveInitialFacingVector();
        CreatureState = Define.CreatureState.Moving;

        if (_body != null)
        {
            _body.simulated = true;
            _body.linearVelocity = Vector2.zero;
            _body.angularVelocity = 0.0f;
        }

        SetCollidersEnabled(true);
        ResetSpriteRenderers();

        if (_healthBar != null)
            _healthBar.ResetForSpawn();
    }

}
