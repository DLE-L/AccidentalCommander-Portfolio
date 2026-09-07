using Lizzo.PV.Gameplay.Combat;
using Lizzo.PV.Legion;
using Lizzo.PV.P0.Units;
using Lizzo.PV.P0.Visuals;
using Lizzo.PV.Data;
using UnityEngine;



public partial class MonsterController : CreatureController, Lizzo.PV.Combat.ICombatImmediateHitTarget
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
	Lizzo.PV.Combat.CountableKillAttribution _lethalKillAttribution;
	long _spawnSequence;
	float _synergySlowMultiplier = 1.0f;
	float _synergySlowUntil;
	bool _bleedImmune;
	bool _hasAuthoredLocalScale;
	Vector3 _authoredLocalScale;
	EnemyEncounterRank _encounterRank = EnemyEncounterRank.TemplateDefault;
	readonly CompanionEnemyStatusState _companionEnemyStatuses = new CompanionEnemyStatusState();

	public string EnemyId => _runtimeStats?.Data?.Id ?? gameObject.name;
	public EnemyEncounterRank EncounterRank => _encounterRank == EnemyEncounterRank.TemplateDefault
		? ResolveTemplateEncounterRank()
		: _encounterRank;
	public string EnemyType => EncounterRank switch
	{
		EnemyEncounterRank.Boss => "boss",
		EnemyEncounterRank.Elite => "elite",
		EnemyEncounterRank.Normal => "normal",
		_ => _runtimeStats?.Data?.Type ?? string.Empty,
	};
	public bool IsBoss => EncounterRank == EnemyEncounterRank.Boss;
	public bool IsElite => EncounterRank == EnemyEncounterRank.Elite;
	public bool IsForcedMovementActive => _smoothKnockbackRemainingDistance > 0.0f;
	public float RemainingForcedMovementDistance => _smoothKnockbackRemainingDistance;
	public bool IsShieldOrcEnemy => EnemyId == CombatIds.ShieldOrc;
	public EnemyRuntimeStats RuntimeStats => _runtimeStats;
	public HitFlash HitFlash => _hitFlash;
	public EnemyHealthBar HealthBar => _healthBar;
	public WolfDashBehaviour WolfDash => _wolfDash;
	public Vector3 LastFacingVector => _lastFacingVector;
	public long SpawnSequence => _spawnSequence;
	public bool IsBleedImmune => _bleedImmune;
	public float CurrentSlowMultiplier => Mathf.Min(
		Time.time < _synergySlowUntil ? _synergySlowMultiplier : 1.0f,
		_companionEnemyStatuses.ResolveMovementSpeedMultiplier(Time.time));
	Lizzo.PV.Combat.CombatImmediateHitFaction Lizzo.PV.Combat.ICombatImmediateHitTarget.Faction => Lizzo.PV.Combat.CombatImmediateHitFaction.Enemy;
	bool Lizzo.PV.Combat.ICombatImmediateHitTarget.IsAlive => this != null && isActiveAndEnabled && Hp > 0;

	public void RefreshRuntimeStatsCache(EnemyRuntimeStats runtimeStats)
	{
		_runtimeStats = runtimeStats;
	}

    public override bool Init()
    {
        base.Init();

		CaptureAuthoredLocalScale();
        EnsureRuntimeComponents();
        ObjectType = Define.ObjectType.Monster;
        _isDead = false;
        CreatureState = Define.CreatureState.Moving;

        return true;
    }

    public override void ResetForSpawn()
    {
		CaptureAuthoredLocalScale();
		_encounterRank = EnemyEncounterRank.TemplateDefault;
		transform.localScale = _authoredLocalScale;
        EnsureRuntimeComponents();

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
		_lethalKillAttribution = default;
		_spawnSequence = 0;
		_synergySlowMultiplier = 1.0f;
		_synergySlowUntil = 0.0f;
		_bleedImmune = false;
		_companionEnemyStatuses.Reset();
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

	internal void AssignSpawnSequence(long sequence)
	{
		if (sequence <= 0) throw new System.ArgumentOutOfRangeException(nameof(sequence));
		_spawnSequence = sequence;
	}

	internal void ClearSpawnSequence() => _spawnSequence = 0;

	public bool ApplySynergySlow(string sourceId, float multiplier, float duration, float currentTime)
	{
		if (string.IsNullOrEmpty(sourceId) || duration <= 0.0f) return false;
		_synergySlowMultiplier = Mathf.Clamp(multiplier, 0.50f, 1.0f);
		_synergySlowUntil = currentTime + duration;
		return true;
	}

	public void ConfigureBleedImmunityForRuntime(bool immune) => _bleedImmune = immune;

	public void ConfigureEncounterRank(EnemyEncounterRank encounterRank, float scaleMultiplier)
	{
		if (encounterRank < EnemyEncounterRank.TemplateDefault || encounterRank > EnemyEncounterRank.Boss)
			throw new System.ArgumentOutOfRangeException(nameof(encounterRank));
		if (scaleMultiplier <= 0.0f)
			throw new System.ArgumentOutOfRangeException(nameof(scaleMultiplier));

		CaptureAuthoredLocalScale();
		_encounterRank = encounterRank;
		transform.localScale = _authoredLocalScale * scaleMultiplier;

		if (_healthBar == null)
			return;
		if (IsBoss)
		{
			EnemyHealthBar.RemoveFrom(transform);
			return;
		}

		bool alwaysVisible = IsElite || IsShieldOrcEnemy;
		_healthBar.Refresh(this, alwaysVisible, visibleSeconds: 0.0f);
	}

	public void ClearSynergySlow(string sourceId)
	{
		if (string.IsNullOrEmpty(sourceId)) return;
		_synergySlowMultiplier = 1.0f;
		_synergySlowUntil = 0.0f;
	}

	void CaptureAuthoredLocalScale()
	{
		if (_hasAuthoredLocalScale)
			return;

		_authoredLocalScale = transform.localScale;
		_hasAuthoredLocalScale = true;
	}

	EnemyEncounterRank ResolveTemplateEncounterRank()
	{
		return (_runtimeStats?.Data?.Type ?? string.Empty).ToLowerInvariant() switch
		{
			"boss" => EnemyEncounterRank.Boss,
			"elite" => EnemyEncounterRank.Elite,
			_ => EnemyEncounterRank.Normal,
		};
	}

	public bool ApplyCompanionStatus(
		Lizzo.PV.Data.CompanionEnemyStatusKind statusKind,
		CompanionStatusSource source,
		float magnitude,
		float duration,
		float currentTime)
	{
		bool applied = statusKind switch
		{
			Lizzo.PV.Data.CompanionEnemyStatusKind.Vulnerable =>
				_companionEnemyStatuses.ApplyVulnerable(source, magnitude, duration, currentTime),
			Lizzo.PV.Data.CompanionEnemyStatusKind.Shock =>
				_companionEnemyStatuses.ApplyShock(source, magnitude, duration, currentTime),
			Lizzo.PV.Data.CompanionEnemyStatusKind.Weakening =>
				_companionEnemyStatuses.ApplyWeakening(source, magnitude, duration, currentTime),
			Lizzo.PV.Data.CompanionEnemyStatusKind.Curse =>
				_companionEnemyStatuses.ApplyCurse(source, duration, currentTime),
			_ => false,
		};
		if (applied)
		{
			Services?.WorldFeedback?.TryPresentStatusApplied(
				statusKind,
				transform.position,
				GetInstanceID());
		}
		return applied;
	}

	public float ResolveCompanionIncomingDamageMultiplier(float currentTime)
	{
		return _companionEnemyStatuses.ResolveIncomingDamageMultiplier(currentTime);
	}

	public float ResolveCompanionMovementSpeedMultiplier(float currentTime)
	{
		return _companionEnemyStatuses.ResolveMovementSpeedMultiplier(currentTime);
	}

	public bool HasCompanionShockFrom(string unitId, float currentTime)
	{
		return _companionEnemyStatuses.HasShockFrom(unitId, currentTime);
	}

	public bool TryConsumeCompanionShock(float currentTime, out CompanionStatusSource source)
	{
		bool consumed = _companionEnemyStatuses.TryConsumeShock(currentTime, out source);
		if (consumed)
		{
			Services?.WorldFeedback?.TryPresentStatusReaction(
				Lizzo.PV.Data.CompanionEnemyStatusKind.Shock,
				Lizzo.PV.Presentation.StatusReactionKind.Consumed,
				transform.position,
				GetInstanceID());
		}
		return consumed;
	}

	public int ResolveCommanderIncomingDamage(int damage, float currentTime)
	{
		_lastOutgoingDamageWasWeakened = false;
		if (damage <= 0)
		{
			return damage;
		}
		if (_companionEnemyStatuses.TryConsumeWeakeningMultiplier(currentTime, out float multiplier) == false)
			return damage;
		_lastOutgoingDamageWasWeakened = true;

		Services?.WorldFeedback?.TryPresentStatusReaction(
			Lizzo.PV.Data.CompanionEnemyStatusKind.Weakening,
			Lizzo.PV.Presentation.StatusReactionKind.Consumed,
			transform.position,
			GetInstanceID());

		return Mathf.Max(1, Mathf.RoundToInt(damage * multiplier));
	}

	bool _lastOutgoingDamageWasWeakened;

	public bool ConsumeLastOutgoingDamageWeakeningMarker()
	{
		bool consumed = _lastOutgoingDamageWasWeakened;
		_lastOutgoingDamageWasWeakened = false;
		return consumed;
	}

}
